using ML_Wildboar.PromptEval.Cli.Commands;

namespace ML_Wildboar.PromptEval;

internal static class Program
{
    private const string UsageText = """
        eval — prompt evaluation tool for ML-Wildboar

        Usage:
          eval run --prompt <name> --dataset <name> [--label <label>]
            Run a prompt against a dataset. Writes runs/<timestamp>_<prompt>_<dataset>/.
            --label optional human-readable suffix added to the run id.

          eval list prompts | datasets | runs
            List available items.

          eval show <run-id>
            Print a run's report to stdout.

          eval diff <run-a> <run-b>
            Show what got better/worse between two runs.

          eval --help
            Show this help.

        Environment:
          ANTHROPIC_API_KEY    Required for 'run'. Can also be passed via --api-key.
        """;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(UsageText);
            return 0;
        }

        try
        {
            var repoRoot = FindRepoRoot();
            var rest = args[1..];
            return args[0] switch
            {
                "run"  => await new RunCommand(repoRoot).ExecuteAsync(rest),
                "list" => new ListCommand(repoRoot).Execute(rest),
                "show" => new ShowCommand(repoRoot).Execute(rest),
                "diff" => new DiffCommand(repoRoot).Execute(rest),
                _      => UnknownCommand(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int UnknownCommand(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}\n");
        Console.Error.WriteLine(UsageText);
        return 2;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.slnx").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not find repo root: no *.slnx file in cwd or any parent. Run from inside the repo.");
    }
}
