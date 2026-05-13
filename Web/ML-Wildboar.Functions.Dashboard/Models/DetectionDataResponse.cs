namespace ML_Wildboar.Functions.Dashboard.Models;

public record DashboardDataResponse(
    List<DailyAgg> DailyAgg,
    List<HourlyAgg> HourlyAgg,
    int TotalImages,
    int WithAnimals,
    int Empty
);

public record DailyAgg(
    string Date,
    int Total,
    int Empty,
    int WithAnimals,
    Dictionary<string, int> BySpecies
);

public record HourlyAgg(
    int Hour,
    int Total,
    Dictionary<string, int> BySpecies
);
