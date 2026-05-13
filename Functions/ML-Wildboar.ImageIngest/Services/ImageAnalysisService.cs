using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ML_Wildboar.ImageIngest.Settings;
using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.ImageIngest.Services;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ImageAnalysisService> _logger;

    // Static system prompt — cached on first call, reused across invocations within the 5-min TTL.
    private const string SystemPrompt = """
        Du är ett bildanalyssystem för viltkameror placerade i svenska skogar och på åkrar.
        Bilderna är ofta tagna nattetid i svartvitt IR-läge — använd då INTE färg som
        identifikationsledtråd. Förlita dig istället på siluett, storlek, hållning,
        proportioner och päls-/horn-struktur. Många bilder har en infobar med tidsstämpel,
        temperatur eller kameranummer inbränd i kanten — IGNORERA helt denna text och
        återge den inte i något fält.

        Följ stegen i ordning. Tänk igenom varje steg innan du svarar.

        Steg 1 — Sök igenom hela bilden
        Granska systematiskt — rörelser, former, ögon, ben, svans, päls, fjädrar.
        Om INGA djur, människor, husdjur eller fordon syns: sätt isEmpty = true,
        detections = [], och alla containsX-flaggor till false.

        Steg 2 — Klassificera vad du ser
        Avgör först VAD som finns i bilden innan du räknar.

        Vilda djur går i "detections"-listan. Använd ETT av följande svenska namn:
        - vildsvin
        - rådjur
        - älg
        - räv
        - hare
        - grävling
        - dovhjort
        - lo
        - varg
        - fågel  ← använd för ALLA fågelarter
        - okänt  ← när du inte kan avgöra arten med rimlig säkerhet

        Föredra "okänt" framför att gissa. Det är bättre att markera ett djur som okänt
        än att felidentifiera det.

        Människor, husdjur och fordon hör INTE hemma i detections — de markeras istället
        med toppnivåflaggorna containsHuman, containsDomestic, containsVehicle.
        - containsHuman: människor till fots, cyklister, jägare etc.
        - containsDomestic: hundar, katter, tamboskap (kor, får, hästar)
        - containsVehicle: bilar, traktorer, fyrhjulingar, mopeder

        Flera arter i samma bild är vanligt — gör INTE antagandet att alla djur är av
        samma art. Skapa ett separat detection-objekt per art.

        Steg 3 — Motivera artvalet (per detection)
        För varje art du har identifierat, skriv FÖRST en kort motivering (max 25 ord) i
        fältet "reasoning". Beskriv vilka SYNLIGA visuella drag (siluett, storlek, päls,
        hållning, synliga kroppsdelar) som leder till artvalet. Hänvisa INTE till generell
        kunskap om arten — bara till vad du faktiskt ser i bilden. Skriv motiveringen
        INNAN du fastställer slutgiltigt artval och konfidens — låt det du ser styra
        beslutet, inte tvärtom.

        Steg 4 — Räkna per art
        För varje art, räkna individerna. En individ räknas så snart NÅGON synlig kroppsdel
        finns i bilden (även bara horn, ben eller huvud bakom vegetation). Om fler än 10
        individer av samma art syns, ange count: 10.

        Steg 5 — Bedöm artkonfidens (per detection)
        Detta gäller ENBART hur säker du är på artbestämningen, inte bildens skick.
        - "hög": tydliga, otvetydiga drag — felidentifiering är osannolik
        - "medel": troligt rätt art men någon tvetydighet finns
        - "låg": mycket osäker — överväg "okänt" istället

        Steg 6 — Bedöm bildkvalitet (toppnivå)
        Bildkvalitet är oberoende av hur säker du är på arten.
        - "god": tydlig bild, gott ljus, inget i vägen för motivet
        - "medel": acceptabel men begränsad av ljus, oskärpa eller delvis skymd vy
        - "dålig": kraftigt försvårad — kraftig oskärpa, överexponering, regn på linsen,
                   nästan helt skymt motiv

        Steg 7 — Bestäm tid på dygnet och väder
        timeOfDay (välj exakt ett):
        - "dag"      — dagsljus, färgbild
        - "skymning" — gryning/skymning eller svagt ljus
        - "natt"     — IR/svartvit bild, mörker

        weather (välj exakt ett):
        - "klart"
        - "mulet"
        - "regn"
        - "dimma"
        - "snö"
        - "okänt"   ← om vädret inte går att avgöra (t.ex. mörk IR-bild)

        Steg 8 — Beskrivning
        Skriv en kort beskrivning av scenen på svenska, max 30 ord. Beskriv
        djurens beteende kortfattat. Upprepa INTE infobar-text, tidsstämplar eller
        kameranamn.

        Returnera ENBART ett JSON-objekt — ingen markdown, inga kodblock, ingen
        förklarande text före eller efter:
        {
          "isEmpty": false,
          "timeOfDay": "dag|skymning|natt",
          "weather": "klart|mulet|regn|dimma|snö|okänt",
          "imageQuality": "god|medel|dålig",
          "containsHuman": false,
          "containsDomestic": false,
          "containsVehicle": false,
          "description": "...",
          "detections": [
            { "reasoning": "...", "species": "...", "count": N, "confidence": "hög|medel|låg" }
          ]
        }

        Exempel 1 — tom nattbild:
        {
          "isEmpty": true,
          "timeOfDay": "natt",
          "weather": "okänt",
          "imageQuality": "medel",
          "containsHuman": false,
          "containsDomestic": false,
          "containsVehicle": false,
          "description": "Tomt skogsbryn i IR-belysning, ingen aktivitet syns.",
          "detections": []
        }

        Exempel 2 — ensamt vildsvin i dagsljus:
        {
          "isEmpty": false,
          "timeOfDay": "dag",
          "weather": "mulet",
          "imageQuality": "god",
          "containsHuman": false,
          "containsDomestic": false,
          "containsVehicle": false,
          "description": "Ett vuxet vildsvin bökar i marken vid skogskanten.",
          "detections": [
            {
              "reasoning": "Kraftig kompakt kropp utan synlig hals, mörk borst, kort tryne, lågt liggande huvud — typisk vildsvinssiluett.",
              "species": "vildsvin",
              "count": 1,
              "confidence": "hög"
            }
          ]
        }

        Exempel 3 — flera arter, IR-bild, osäker identifiering:
        {
          "isEmpty": false,
          "timeOfDay": "natt",
          "weather": "okänt",
          "imageQuality": "dålig",
          "containsHuman": false,
          "containsDomestic": false,
          "containsVehicle": false,
          "description": "Två rådjur betar nära kameran, en fågel flyger förbi i bakgrunden.",
          "detections": [
            {
              "reasoning": "Smala långa ben, kort svans, stora öron, slank kropp i typisk beteställning — stämmer med rådjur.",
              "species": "rådjur",
              "count": 2,
              "confidence": "medel"
            },
            {
              "reasoning": "Vingar utbredda i flykt, fågelsiluett i bakgrunden, för otydlig för artbestämning.",
              "species": "fågel",
              "count": 1,
              "confidence": "låg"
            }
          ]
        }
        """;

    public ImageAnalysisService(
        IOptions<AnthropicSettings> settings,
        ILogger<ImageAnalysisService> logger)
    {
        _client = new AnthropicClient { ApiKey = settings.Value.ApiKey };
        _logger = logger;
    }

    public async Task<ImageAnalysisResult> AnalyzeAsync(byte[] imageData)
    {
        var base64Image = Convert.ToBase64String(imageData);

        var delays = new[] { TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120) };
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await CallClaudeAsync(base64Image);
            }
            catch (Anthropic.Exceptions.AnthropicRateLimitException) when (attempt < delays.Length)
            {
                _logger.LogWarning("Rate limited by Claude API, waiting {delay}s before retry {attempt}",
                    delays[attempt].TotalSeconds, attempt + 1);
                await Task.Delay(delays[attempt]);
            }
        }
    }

    private async Task<ImageAnalysisResult> CallClaudeAsync(string base64Image)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = 1024,
            System = new List<TextBlockParam>
            {
                new()
                {
                    Text = SystemPrompt,
                    CacheControl = new CacheControlEphemeral()
                }
            },
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam
                        {
                            Source = new Base64ImageSource
                            {
                                MediaType = "image/jpeg",
                                Data = base64Image
                            }
                        },
                        new TextBlockParam { Text = "Analysera denna bild från en viltkamera." }
                    }
                }
            ]
        });

        _logger.LogInformation(
            "Claude usage — input: {input}, output: {output}, cache_created: {cacheCreated}, cache_read: {cacheRead}",
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.Usage.CacheCreationInputTokens,
            response.Usage.CacheReadInputTokens);

        var rawJson = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Claude returned no text content");

        // Strip markdown code fences if present.
        var json = rawJson.Trim();
        if (json.StartsWith("```"))
        {
            json = json.Substring(json.IndexOf('\n') + 1);
            json = json.Substring(0, json.LastIndexOf("```")).Trim();
        }

        var parsed = JsonSerializer.Deserialize<ClaudeAnalysisResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Claude response as JSON");

        return new ImageAnalysisResult(
            IsEmpty: parsed.IsEmpty,
            TimeOfDay: parsed.TimeOfDay ?? string.Empty,
            Weather: parsed.Weather ?? string.Empty,
            ImageQuality: parsed.ImageQuality ?? string.Empty,
            ContainsHuman: parsed.ContainsHuman,
            ContainsDomestic: parsed.ContainsDomestic,
            ContainsVehicle: parsed.ContainsVehicle,
            Description: parsed.Description ?? string.Empty,
            Detections: parsed.Detections ?? [],
            RawJson: json
        );
    }

    private record ClaudeAnalysisResponse(
        [property: JsonPropertyName("isEmpty")] bool IsEmpty,
        [property: JsonPropertyName("timeOfDay")] string? TimeOfDay,
        [property: JsonPropertyName("weather")] string? Weather,
        [property: JsonPropertyName("imageQuality")] string? ImageQuality,
        [property: JsonPropertyName("containsHuman")] bool ContainsHuman,
        [property: JsonPropertyName("containsDomestic")] bool ContainsDomestic,
        [property: JsonPropertyName("containsVehicle")] bool ContainsVehicle,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("detections")] List<AnimalDetection>? Detections
    );
}
