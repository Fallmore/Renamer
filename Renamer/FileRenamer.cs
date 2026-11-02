using System.Buffers;

namespace Renamer;

public class FileRenamer
{
	public static RenameInfo Rename(DirectoryInfo dir, Mode mode, string[] symbols, string replacingChars, bool allDirectories, bool dryRun)
	{
		RenameInfo info = new();

		var files = dir.GetFiles("", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		SearchValues<string> searchValues = SearchValues.Create(symbols, StringComparison.Ordinal);
		HashSet<string> occupied = new(StringComparer.OrdinalIgnoreCase);

		foreach (var file in files)
		{
			if (!file.Name.ContainsAny(searchValues)) continue;

			RenamedFile renamed = new() { OldName = Path.GetFileNameWithoutExtension(file.Name) };
			string tempName = Replace(renamed.OldName, replacingChars, mode, symbols);

			int count = 0;
			string newName = tempName;
			if (newName == renamed.OldName) continue;

			string targetPath = Path.Combine(file.DirectoryName!, newName + file.Extension);
			while (File.Exists(targetPath) || occupied.Contains(targetPath))
			{
				newName = tempName + string.Concat(" (", ++count, ")");
				targetPath = Path.Combine(file.DirectoryName!, newName + file.Extension);
			}
			occupied.Add(targetPath);

			try
			{
				renamed.NewName = newName;
				if (!dryRun)
				{
					File.Move(file.FullName, targetPath);
				}
				info.RenamedFiles.Add(renamed);
			}
			catch (Exception e)
			{
				info.Error = e.Message;
				info.RenamedFiles.Add(renamed);
				return info;
			}
		}

		info.Success = true;
		return info;
	}

	public static string Replace(string name, string replacingChars, Mode mode, string[] searchValues)
	{
		string newName = name;
		foreach (var val in searchValues)
		{
			if (val.Length == 0) continue;

			int searchFrom = 0;
			int index;
			while ((index = newName.IndexOf(val, searchFrom, StringComparison.Ordinal)) != -1)
			{
				switch (mode)
				{
					case Mode.OnlySymbol:
						newName = newName[..index] + replacingChars + newName[(index + val.Length)..];
						searchFrom = index + replacingChars.Length;
						break;
					case Mode.BeforeSymbol:
						newName = replacingChars + newName[(index + 1)..];
						searchFrom = replacingChars.Length;
						break;
					case Mode.AfterSymbol:
						newName = newName[..index] + replacingChars;
						searchFrom = newName.Length;
						break;
				}
			}
		}

		return newName.Trim();
	}
}
