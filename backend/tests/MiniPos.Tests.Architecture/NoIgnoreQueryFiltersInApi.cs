using System.Text;

namespace MiniPos.Tests.Architecture;

public class NoIgnoreQueryFiltersInApi
{
    [Fact]
    public void Api_must_not_call_IgnoreQueryFilters()
    {
        var repoRoot = MultiTenancyConventions.FindRepoRoot();
        var apiDir = Path.Combine(repoRoot, "backend/src/MiniPos.Api");

        var offendingFiles = new List<string>();
        var sb = new StringBuilder("The following MiniPos.Api source files contain 'IgnoreQueryFilters':");

        foreach (var file in Directory.EnumerateFiles(apiDir, "*.cs", SearchOption.AllDirectories))
        {
            if (File.ReadAllText(file).Contains("IgnoreQueryFilters"))
            {
                offendingFiles.Add(file);
            }
        }

        foreach (var f in offendingFiles)
        {
            sb.AppendLine();
            sb.Append("  - ").Append(f);
        }

        offendingFiles.Should().BeEmpty(sb.ToString());
    }
}
