namespace Renamer.Tests;

[TestFixture]
public class FileRenamerRenameTests
{
	private string _dir = "";

	[SetUp]
	public void SetUp()
	{
		_dir = Path.Combine(Path.GetTempPath(), "renamer-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_dir);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_dir))
		{
			Directory.Delete(_dir, true);
		}
	}

	private string CreateFile(string name, string? subDirectory = null)
	{
		string directory = subDirectory is null ? _dir : Path.Combine(_dir, subDirectory);
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, name);
		File.WriteAllText(path, "test");
		return path;
	}

	[Test]
	public void Rename_ReplacesSymbol_AndKeepsExtension()
	{
		string oldPath = CreateFile("My photo.jpg");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, false);

		Assert.That(info.Success, Is.True);
		Assert.That(info.RenamedFiles, Has.Count.EqualTo(1));
		Assert.That(info.RenamedFiles[0].OldName, Is.EqualTo("My photo"));
		Assert.That(info.RenamedFiles[0].NewName, Is.EqualTo("My_photo"));
		Assert.That(File.Exists(Path.Combine(_dir, "My_photo.jpg")), Is.True);
		Assert.That(File.Exists(oldPath), Is.False);
	}

	[Test]
	public void Rename_SkipsFilesWithoutChanges()
	{
		CreateFile("clean.txt");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, false);

		Assert.That(info.Success, Is.True);
		Assert.That(info.RenamedFiles, Is.Empty);
		Assert.That(File.Exists(Path.Combine(_dir, "clean.txt")), Is.True);
	}

	[Test]
	public void Rename_AddsSuffix_WhenNewNameIsTaken()
	{
		CreateFile("a_b.txt");
		CreateFile("a b.txt");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, false);

		Assert.That(info.Success, Is.True);
		string[] names = Directory.GetFiles(_dir).Select(f => Path.GetFileName(f)!).ToArray();
		Assert.That(names, Is.EquivalentTo(new[] { "a_b.txt", "a_b (1).txt" }));
	}

	[Test]
	public void Rename_FileWithoutExtension_DoesNotFail()
	{
		CreateFile("no ext file");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, false);

		Assert.That(info.Success, Is.True, info.Error);
		Assert.That(info.RenamedFiles, Has.Count.EqualTo(1));
		Assert.That(File.Exists(Path.Combine(_dir, "no_ext_file")), Is.True);
	}

	[Test]
	public void Rename_SearchesSubdirectories_OnlyWithFlag()
	{
		CreateFile("inner file.txt", "sub");

		RenameInfo withoutFlag = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, false);
		Assert.That(withoutFlag.Success, Is.True);
		Assert.That(withoutFlag.RenamedFiles, Is.Empty);
		Assert.That(File.Exists(Path.Combine(_dir, "sub", "inner file.txt")), Is.True);

		RenameInfo withFlag = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", true, false);
		Assert.That(withFlag.Success, Is.True);
		Assert.That(withFlag.RenamedFiles, Has.Count.EqualTo(1));
		Assert.That(File.Exists(Path.Combine(_dir, "sub", "inner_file.txt")), Is.True);
	}

	[Test]
	public void DryRun_ReportsResult_ButKeepsFilesIntact()
	{
		CreateFile("My photo.jpg");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, true);

		Assert.That(info.Success, Is.True);
		Assert.That(info.RenamedFiles, Has.Count.EqualTo(1));
		Assert.That(info.RenamedFiles[0].NewName, Is.EqualTo("My_photo"));
		Assert.That(File.Exists(Path.Combine(_dir, "My photo.jpg")), Is.True, "dry-run не должен переименовывать файлы");
		Assert.That(File.Exists(Path.Combine(_dir, "My_photo.jpg")), Is.False);
	}

	[Test]
	public void DryRun_TakesExistingFilesIntoAccount_WhenCheckingCollisions()
	{
		CreateFile("a_b.txt");
		CreateFile("a b.txt");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, true);

		Assert.That(info.Success, Is.True);
		Assert.That(info.RenamedFiles, Has.Count.EqualTo(1));
		Assert.That(info.RenamedFiles[0].NewName, Is.EqualTo("a_b (1)"), "учитывался только существующий файл");
	}

	[Test]
	public void DryRun_ReportsSeveralPlannedRenames_WithoutDuplicates()
	{
		CreateFile("a b.txt");
		CreateFile("c d.txt");

		RenameInfo info = FileRenamer.Rename(new DirectoryInfo(_dir), Mode.OnlySymbol, [" "], "_", false, true);

		Assert.That(info.Success, Is.True);
		string[] planned = info.RenamedFiles.Select(f => f.NewName).ToArray();
		Assert.That(planned, Is.EqualTo(new[] { "a_b", "c_d" }));
		Assert.That(planned.Distinct().Count(), Is.EqualTo(planned.Length));
	}
}
