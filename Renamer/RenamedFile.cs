namespace Renamer;

public class RenamedFile
{
	public string OldName { get; set; } = "";
	public string NewName { get; set; } = "";

	public override string ToString()
	{
		return $"{OldName}\n\t=>\t{NewName}";
	}
}
