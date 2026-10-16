namespace __Product__.Presentation.Tests;

// One token layer (ui-frameworks.md section 7): literal colors live only in Themes/Exceptions.axaml.
public sealed class TokenRuleTests
{
    private static readonly Regex LiteralColor = new(@"""#[0-9A-Fa-f]{3,8}""", RegexOptions.Compiled);

    [Fact]
    public void Literal_colors_appear_only_in_the_exceptions_dictionary()
    {
        var offenders = TestPaths.SourceFiles(".axaml")
            .Where(f => !f.EndsWith($"Themes{Path.DirectorySeparatorChar}Exceptions.axaml", StringComparison.Ordinal))
            .Where(f => LiteralColor.IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName);
        Assert.Empty(offenders);
    }
}
