#:property PublishAot=false
// Checks this skill repository: token-budget line counts, text formatting and markdown rules.
// Run through check-skill.bat. Args: <repoRoot>
try
{
    if (args.Length < 1) return Fail("Usage: check-skill.bat");
    var root = Path.GetFullPath(args[0]);
    if (!File.Exists(Path.Combine(root, "SKILL.md"))) return Fail($"Not a skill repository: {root}");

    var problems = new List<string>();
    var filesChecked = 0;
    foreach (var file in Enumerate(root))
    {
        if (!IsText(file)) continue;
        filesChecked++;
        var relative = Path.GetRelativePath(root, file);
        var text = File.ReadAllText(file);
        CheckFormatting(relative, text, problems);
        CheckLineLimit(relative, text, problems);
        if (relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) CheckMarkdown(root, relative, text, problems);
    }
    CheckReferencesLinked(root, problems);

    if (problems.Count > 0)
    {
        Console.Error.WriteLine($"[check-skill] FAILED: {problems.Count} problem(s)");
        foreach (var problem in problems.Take(20)) Console.Error.WriteLine($"      {problem}");
        if (problems.Count > 20) Console.Error.WriteLine($"      ... and {problems.Count - 20} more");
        return 1;
    }

    Console.WriteLine($"[check-skill] OK: {filesChecked} files checked");
    return 0;
}
catch (Exception ex)
{
    return Fail(ex.Message);
}

static int Fail(string message)
{
    Console.Error.WriteLine($"[check-skill] FAILED: {message}");
    return 1;
}

// Skill docs: keep the context an agent loads small. Template code: hard maximums from code-patterns.md.
static void CheckLineLimit(string relative, string text, List<string> problems)
{
    var path = relative.Replace('\\', '/');
    var name = Path.GetFileName(path);
    (int Max, string What)? limit = null;

    if (path == "SKILL.md") limit = (120, "SKILL.md budget");
    else if (path == "README.md") limit = (100, "readme budget");
    else if (path.StartsWith("references/") && path.EndsWith(".md")) limit = (450, "reference budget");
    else if (path.StartsWith("assets/template/"))
    {
        if (name.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)) limit = (150, "code-behind hard max");
        else if (name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) limit = null;   // generated
        else if (name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)) limit = (300, "view hard max");
        else if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            limit = name.EndsWith("ViewModel.cs", StringComparison.OrdinalIgnoreCase) ? (350, "ViewModel hard max") : (400, "class hard max");
        else if (name.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)
                 || name.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)) limit = (60, "script hard max");
    }

    if (limit is { } check)
    {
        var lines = CountLines(text);
        if (lines > check.Max) problems.Add($"{relative}: {lines} lines over the {check.Max}-line {check.What}");
    }
}

static void CheckFormatting(string relative, string text, List<string> problems)
{
    if (text.Length == 0) return;
    var path = relative.Replace('\\', '/');

    if (!text.EndsWith('\n')) problems.Add($"{relative}: no final newline");

    var crlf = CountOccurrences(text, "\r\n");
    var loneLf = text.Count(c => c == '\n') - crlf;
    if (crlf > 0 && loneLf > 0) problems.Add($"{relative}: mixed line endings");
    if (text.Count(c => c == '\r') > crlf) problems.Add($"{relative}: stray carriage return");
    if ((path.Contains(".githooks/") || path.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)) && crlf > 0)
        problems.Add($"{relative}: hooks and .sh scripts must use LF endings");

    var lines = text.Split('\n');
    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i].TrimEnd('\r');
        // Markdown may keep trailing spaces (editorconfig disables trimming there).
        if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && (line.EndsWith(' ') || line.EndsWith('\t')))
        {
            problems.Add($"{relative}:{i + 1}: trailing whitespace");
            break;
        }
    }

    if (HasSpaceIndentation(path))
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('\t'))
            {
                problems.Add($"{relative}:{i + 1}: tab indentation");
                break;
            }
        }
    }
}

// Plain ASCII prose; a line holding Arabic letters is real sample data and keeps its punctuation.
// Docs over 100 lines open with a "## Contents" list; relative .md links must resolve.
static void CheckMarkdown(string root, string relative, string text, List<string> problems)
{
    var lines = text.Split('\n');
    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        if (line.Any(c => c is >= '؀' and <= 'ۿ')) continue;
        var bad = line.FirstOrDefault(c => c > '\u007F');
        if (bad != default)
        {
            problems.Add($"{relative}:{i + 1}: non-ASCII character U+{(int)bad:X4}");
            break;
        }
    }

    if (CountLines(text) > 100 && !lines.Take(15).Any(l => l.TrimEnd('\r') == "## Contents"))
        problems.Add($"{relative}: over 100 lines without a '## Contents' heading in the first 15 lines");

    var folder = Path.GetDirectoryName(Path.Combine(root, relative))!;
    foreach (System.Text.RegularExpressions.Match link in
             System.Text.RegularExpressions.Regex.Matches(text, @"\]\(([^)#:\s]+\.md)(#[^)]*)?\)"))
    {
        var target = Path.Combine(folder, link.Groups[1].Value);
        if (!File.Exists(target) && !File.Exists(InCommonLayer(root, target)))
            problems.Add($"{relative}: broken link {link.Groups[1].Value}");
    }
}

// A UI layer (assets/template/wpf, ...) is copied over common, so its docs may link common's files.
static string InCommonLayer(string root, string path)
{
    var parts = Path.GetRelativePath(Path.Combine(root, "assets", "template"), path)
        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Length > 1 && parts[0] != ".."
        ? Path.Combine([root, "assets", "template", "common", .. parts[1..]])
        : path;
}

// Every reference must be linked straight from SKILL.md (one level deep).
static void CheckReferencesLinked(string root, List<string> problems)
{
    var references = Path.Combine(root, "references");
    if (!Directory.Exists(references)) return;
    var skill = File.ReadAllText(Path.Combine(root, "SKILL.md"));
    foreach (var file in Directory.EnumerateFiles(references, "*.md"))
    {
        var name = Path.GetFileName(file);
        if (!skill.Contains($"](references/{name})", StringComparison.Ordinal))
            problems.Add($"SKILL.md: no link to references/{name}");
    }
}

static int CountLines(string text)
{
    var lines = text.Count(c => c == '\n');
    return text.EndsWith('\n') ? lines : lines + 1;
}

static int CountOccurrences(string text, string value)
{
    var count = 0;
    for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0;
         index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        count++;
    return count;
}

static bool HasSpaceIndentation(string path) =>
    path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".wxs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".wxl", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".wixproj", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);

static IEnumerable<string> Enumerate(string root) =>
    Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is ".git" or "bin" or "obj" or "artifacts" or "TestResults"));

static bool IsText(string path)
{
    if (Path.GetFileName(path) == "pre-commit") return true;
    string[] extensions = [".cs", ".xaml", ".csproj", ".props", ".slnx", ".json", ".md", ".bat", ".resx", ".sql",
        ".wxs", ".wxl", ".wixproj", ".manifest", ".txt", ".rtf", ".editorconfig", ".gitattributes", ".gitignore",
        ".yml", ".yaml", ".sh"];
    var name = Path.GetFileName(path);
    var extension = name.StartsWith('.') ? name : Path.GetExtension(name);
    return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}
