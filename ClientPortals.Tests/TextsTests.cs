using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ClientPortals;
using Xunit;

public class TextsTests
{
    // Abenaki falls back to English on purpose
    [Fact]
    public void EveryGameLanguageHasARow() => Assert.Empty(Texts.GameLanguages.Except(Texts.Rows.Keys).Except(new[] { "Abenaki" }));

    [Fact]
    public void EveryLanguageHasEveryText()
    {
        foreach (var row in Texts.Rows)
        {
            Assert.True(row.Value.Length == Texts.Keys.Length, $"{row.Key}: {row.Value.Length} texts for {Texts.Keys.Length} keys");
            Assert.DoesNotContain(row.Value, string.IsNullOrWhiteSpace);
        }
    }

    [Fact]
    public void PlaceholdersAreKept()
    {
        var english = Texts.Rows["English"];
        foreach (var row in Texts.Rows)
            for (int i = 0; i < english.Length; i++)
                Assert.True(english[i].Contains("$1") == row.Value[i].Contains("$1"), $"{row.Key} '{Texts.Keys[i]}': $1 placeholder");
    }

    [Fact]
    public void NoOtherDollarSigns()
    {
        // the game's localization would read "$word" as a token of its own
        foreach (var row in Texts.Rows)
            foreach (var text in row.Value)
                Assert.DoesNotMatch(@"\$(?!1)", text);
    }

    [Fact]
    public void TranslationsDifferFromEnglish()
    {
        // a row pasted from English by mistake would show here (a few words like "Portal" may match)
        var english = Texts.Rows["English"];
        foreach (var row in Texts.Rows.Where(r => r.Key != "English"))
        {
            int same = row.Value.Where((t, i) => t == english[i]).Count();
            Assert.True(same <= 2, $"{row.Key}: {same} texts are the English ones");
        }
    }

    [Fact]
    public void UnknownLanguagesGetEnglish()
    {
        var words = Texts.WordsFor("Abenaki").ToDictionary(w => w.Key, w => w.Value);
        Assert.Equal("Portals", words["title"]);
        Assert.Equal(Texts.Keys.Length, words.Count);
    }

    [Fact]
    public void EveryKeyTheCodeUsesExists()
    {
        string src = Path.Combine(Path.GetDirectoryName(ThisFile()), "..", "ClientPortals", "src");
        var used = Directory.GetFiles(src, "*.cs")
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\bT\(""(\w+)""").Cast<Match>())
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.True(used.Count > 20);
        Assert.Empty(used.Except(Texts.Keys));
        // and every key is used
        Assert.Empty(Texts.Keys.Except(used).Except(new[] { "sort_name", "sort_nearest", "sort_farthest" }));
    }

    static string ThisFile([CallerFilePath] string path = "") => path;
}
