namespace ML_Wildboar.PromptEval.Cli.Commands;

internal sealed class ListCommand(string repoRoot)
{
    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: eval list (prompts | datasets | runs)");
            return 2;
        }

        return args[0] switch
        {
            "prompts"  => ListPrompts(),
            "datasets" => ListDatasets(),
            "runs"     => ListRuns(),
            _ => Unknown(args[0]),
        };
    }

    private int Unknown(string what)
    {
        Console.Error.WriteLine($"Unknown list target: {what}. Expected one of: prompts, datasets, runs.");
        return 2;
    }

    private int ListPrompts()
    {
        var dir = Path.Combine(repoRoot, "prompts");
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("(no prompts/ directory)");
            return 0;
        }
        foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(p => p))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var size = new FileInfo(file).Length;
            Console.WriteLine($"  {name,-32}  {size,6} bytes");
        }
        return 0;
    }

    private int ListDatasets()
    {
        var dir = Path.Combine(repoRoot, "Evals", "datasets");
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("(no Evals/datasets/ directory)");
            return 0;
        }
        foreach (var subdir in Directory.EnumerateDirectories(dir).OrderBy(p => p))
        {
            var manifest = Path.Combine(subdir, "dataset.json");
            if (!File.Exists(manifest)) continue;
            var name = Path.GetFileName(subdir);
            var imageDir = Path.Combine(subdir, "images");
            var imageCount = Directory.Exists(imageDir)
                ? Directory.EnumerateFiles(imageDir).Count()
                : 0;
            Console.WriteLine($"  {name,-32}  {imageCount,4} images on disk");
        }
        return 0;
    }

    private int ListRuns()
    {
        var dir = Path.Combine(repoRoot, "Evals", "runs");
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("(no Evals/runs/ directory)");
            return 0;
        }
        foreach (var subdir in Directory.EnumerateDirectories(dir).OrderByDescending(p => p))
        {
            Console.WriteLine($"  {Path.GetFileName(subdir)}");
        }
        return 0;
    }
}
