namespace ML_Wildboar.PromptEval.Cli;

internal static class CliArgs
{
    public static string? Flag(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }

    public static string RequireFlag(string[] args, string name)
        => Flag(args, name)
        ?? throw new ArgumentException($"Missing required flag: {name}");

    public static string RequirePositional(string[] args, int index, string description)
    {
        var positionals = args.Where((a, i) => !a.StartsWith("--") && (i == 0 || !args[i - 1].StartsWith("--"))).ToList();
        if (positionals.Count <= index)
            throw new ArgumentException($"Missing positional argument: {description}");
        return positionals[index];
    }
}
