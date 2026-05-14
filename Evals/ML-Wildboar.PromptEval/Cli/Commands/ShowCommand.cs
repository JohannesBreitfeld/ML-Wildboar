namespace ML_Wildboar.PromptEval.Cli.Commands;

internal sealed class ShowCommand(string repoRoot)
{
    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: eval show <run-id>");
            return 2;
        }

        var runId = args[0];
        var runDir = Path.Combine(repoRoot, "Evals", "runs", runId);
        var reportPath = Path.Combine(runDir, "report.md");
        if (!File.Exists(reportPath))
        {
            Console.Error.WriteLine($"Report not found: {reportPath}");
            return 1;
        }
        Console.Write(File.ReadAllText(reportPath));
        return 0;
    }
}
