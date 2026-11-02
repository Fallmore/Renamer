using System.Buffers;
using System.CommandLine;

internal class Program
{
	private static int Main(string[] args)
	{
		Option<DirectoryInfo> dirOption = new("--dir", "-d")
		{
			Description = "Папка, в которой находятся файлы",
			Required = true,
			CustomParser = result =>
			{
				DirectoryInfo directory = new(result.Tokens.Single().Value);
				if (!directory.Exists)
				{
					result.AddError("Указанной папки не существует");
				}
				return directory;
			}
		};
		Option<Mode> modeOption = new("--mode", "-m")
		{
			Description = "Режим замены найденного символа. При любом режиме найденный символ заменяется",
			DefaultValueFactory = parseResult => Mode.OnlySymbol
		};
		Option<string[]> symbolsOption = new("--symbols", "-s")
		{
			Description = "Символы, которые необходимо найти в названии файла",
			Required = true,
			AllowMultipleArgumentsPerToken = true,
		};
		Option<string> replacingCharsOption = new("--replace", "-r")
		{
			Description = "Чем заменить найденные символы: по умолчанию пустота",
			DefaultValueFactory = parseResult => ""
		};
		Option<bool> allDirectoriesOption = new("--all-directories", "-ad")
		{
			Description = "Включать ли файлы всех подпапок: по умолчанию файлы только указанной папки",
		};

		RootCommand rootCommand = new("Renamer - программа, меняющая название файлов в папке, заменяя указанные символы на ваши");

		Command renameCommand = new("rename", "Переименовать файлы и показать результат.")
		{
			dirOption,
			modeOption,
			symbolsOption,
			replacingCharsOption,
			allDirectoriesOption
		};
		rootCommand.Subcommands.Add(renameCommand);

		renameCommand.SetAction(parseResult => {
			var info = Rename(
				parseResult.GetValue(dirOption)!,
				parseResult.GetValue(modeOption),
				parseResult.GetValue(symbolsOption)!,
				parseResult.GetValue(replacingCharsOption)!,
				parseResult.GetValue(allDirectoriesOption));
			if (!info.Success)
			{
				Console.WriteLine($"Ошибка на файле {info.RenamedFiles.Last().OldName}: {info.Error}");
			}
			else
			{
				Console.WriteLine($"Успех! Переименовано {info.RenamedFiles.Count} файлов");
				foreach (RenamedFile file in info.RenamedFiles)
				{
					Console.WriteLine(file.ToString());
				}
			}
			return 0;
		});

		return rootCommand.Parse(args).Invoke();
	}

	private static RenameInfo Rename(DirectoryInfo dir, Mode mode, string[] symbols, string replacingChars, bool allDirectories)
	{
		RenameInfo info = new();
		
		var files = dir.GetFiles("", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		SearchValues<string> searchValues = SearchValues.Create(symbols, StringComparison.Ordinal);

		foreach (var file in files)
		{
			if (!file.Name.ContainsAny(searchValues)) continue;
			
			RenamedFile renamed = new() { OldName = file.Name.Replace(file.Extension, "")};
			string tempName = Replace(renamed.OldName, replacingChars, mode, symbols);

			int count = 0;
			string newName = tempName;
			if (newName == renamed.OldName) continue;

			while (File.Exists(Path.Combine(file.DirectoryName!, newName + file.Extension)))
			{
				newName = tempName + string.Concat(" (", ++count, ")");
			}

			try
			{
				renamed.NewName = newName;
				File.Move(file.FullName, Path.Combine(file.DirectoryName!, newName + file.Extension));
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

	private static string Replace(string name, string replacingChars, Mode mode, string[] searchValues)
	{
		string newName = name;
		foreach (var val in searchValues)
		{
			int index = newName.IndexOf(val);
			if (index <= 0) continue;

			switch (mode)
			{
				case Mode.OnlySymbol:
					newName = newName[..index] + replacingChars + newName[(index + val.Length)..];
					break;
				case Mode.BeforeSymbol:
					newName = replacingChars + newName[(index + 1)..];
					break;
				case Mode.AfterSymbol:
					newName = newName[..index] + replacingChars;
					break;
			}
		}

		return newName.Trim();
	}

	enum Mode
	{
		OnlySymbol,
		BeforeSymbol,
		AfterSymbol
	}

	private class RenameInfo
	{
		public bool Success { get; set; } = false;
		public List<RenamedFile> RenamedFiles { get; set; } = [];
		public string? Error { get; set; } = null;
	}

	private class RenamedFile
	{
		public string OldName { get; set; } = "";
		public string NewName { get; set; } = "";

		public override string ToString()
		{
			return $"{OldName}\n\t=>\t{NewName}";
		}
	}
}