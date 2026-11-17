using System.Diagnostics;

namespace Metered.Tests.Repository;

/// <summary>The repository as git sees it, for the guards. From Cornflower's MicrosoftFileGuard.</summary>
internal static class RepositoryFiles
{
    /// <summary>The files git would commit: tracked, plus untracked files that aren't ignored.</summary>
    internal static IEnumerable<(string Path, Func<Stream> Open)> All()
    {
        var root = FindRoot();
        var git = new ProcessStartInfo("git", "ls-files -z --cached --others --exclude-standard")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(git) ?? throw new InvalidOperationException("git didn't start");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git ls-files exited with {process.ExitCode}");

        foreach (var path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var full = Path.Combine(root, path);
            // A tracked file deleted from the working tree is listed but isn't there to commit.
            if (File.Exists(full))
                yield return (path, () => File.OpenRead(full));
        }
    }

    /// <summary>The repository's root: the folder above the tests' binaries that holds <c>Metered.sln</c>.</summary>
    internal static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Metered.sln")))
                return dir.FullName;

        throw new InvalidOperationException("Metered.sln not found above " + AppContext.BaseDirectory);
    }
}
