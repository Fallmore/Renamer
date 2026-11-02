namespace Renamer.Tests;

[TestFixture]
public class FileRenamerReplaceTests
{
	private static string ReplaceTerminating(string name, string replacingChars, Mode mode, string[] symbols)
	{
		Task<string> task = Task.Run(() => FileRenamer.Replace(name, replacingChars, mode, symbols));
		if (!task.Wait(TimeSpan.FromSeconds(2)))
		{
			Assert.Fail("Replace не завершился за 2 секунды — вероятно, бесконечный цикл");
		}
		return task.Result;
	}

	[Test]
	public void OnlySymbol_ReplacesAllOccurrences()
	{
		Assert.That(ReplaceTerminating("a b c", "_", Mode.OnlySymbol, [" "]), Is.EqualTo("a_b_c"));
	}

	[Test]
	public void OnlySymbol_ReplacesSeveralSymbols()
	{
		Assert.That(ReplaceTerminating("a.b,c", "", Mode.OnlySymbol, [".", ","]), Is.EqualTo("abc"));
	}

	[Test]
	public void OnlySymbol_RemovesConsecutiveOccurrences()
	{
		// Регрессия: сдвиг поиска на 1 символ после удаления пропускал соседний символ.
		Assert.That(ReplaceTerminating("aab", "", Mode.OnlySymbol, ["a"]), Is.EqualTo("b"));
	}

	[Test]
	public void OnlySymbol_ReplacementContainingSymbol_Terminates()
	{
		// Регрессия: замена символа на него же зацикливала обработку.
		Assert.That(ReplaceTerminating("a", "aa", Mode.OnlySymbol, ["a"]), Is.EqualTo("aa"));
	}

	[Test]
	public void EmptySymbol_IsIgnored()
	{
		Assert.That(ReplaceTerminating("abc", "x", Mode.OnlySymbol, [""]), Is.EqualTo("abc"));
	}

	[Test]
	public void BeforeSymbol_RemovesEverythingBeforeSymbol()
	{
		Assert.That(ReplaceTerminating("My file", "", Mode.BeforeSymbol, [" "]), Is.EqualTo("file"));
	}

	[Test]
	public void BeforeSymbol_InsertsReplacement()
	{
		Assert.That(ReplaceTerminating("My file", "-", Mode.BeforeSymbol, [" "]), Is.EqualTo("-file"));
	}

	[Test]
	public void AfterSymbol_TruncatesName()
	{
		Assert.That(ReplaceTerminating("My file", "", Mode.AfterSymbol, [" "]), Is.EqualTo("My"));
	}

	[Test]
	public void AfterSymbol_AppendsReplacement()
	{
		Assert.That(ReplaceTerminating("My file", "x", Mode.AfterSymbol, [" "]), Is.EqualTo("Myx"));
	}

	[Test]
	public void Result_IsTrimmed()
	{
		Assert.That(ReplaceTerminating(" a ", "x", Mode.OnlySymbol, ["-"]), Is.EqualTo("a"));
	}

	[Test]
	public void NoMatch_ReturnsOriginalName()
	{
		Assert.That(ReplaceTerminating("file", "_", Mode.OnlySymbol, ["-"]), Is.EqualTo("file"));
	}
}
