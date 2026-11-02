using System.CommandLine;

namespace Renamer;

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
		Option<bool> dryRunOption = new("--dry-run", "-n")
		{
			Description = "Показать результат без фактического переименования файлов",
		};

		RootCommand rootCommand = new("Renamer - программа, меняющая название файлов в папке, заменяя указанные символы на ваши");

		Command renameCommand = new("rename", "Переименовать файлы и показать результат.")
		{
			dirOption,
			modeOption,
			symbolsOption,
			replacingCharsOption,
			allDirectoriesOption,
			dryRunOption
		};
		rootCommand.Subcommands.Add(renameCommand);

		renameCommand.SetAction(parseResult => {
			bool dryRun = parseResult.GetValue(dryRunOption);
			var info = FileRenamer.Rename(
				parseResult.GetValue(dirOption)!,
				parseResult.GetValue(modeOption),
				parseResult.GetValue(symbolsOption)!,
				parseResult.GetValue(replacingCharsOption)!,
				parseResult.GetValue(allDirectoriesOption),
				dryRun);
			if (!info.Success)
			{
				Console.WriteLine($"Ошибка на файле {info.RenamedFiles.Last().OldName}: {info.Error}");
			}
			else
			{
				string header = dryRun
					? $"Предпросмотр (dry-run): было бы переименовано {info.RenamedFiles.Count} файлов, файлы не изменялись"
					: $"Успех! Переименовано {info.RenamedFiles.Count} файлов";
				Console.WriteLine(header);
				foreach (RenamedFile file in info.RenamedFiles)
				{
					Console.WriteLine(file.ToString());
				}
			}
			return 0;
		});

		return rootCommand.Parse(args).Invoke();
	}
}
