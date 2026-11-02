namespace Renamer;

public class RenameInfo
{
	public bool Success { get; set; } = false;
	public List<RenamedFile> RenamedFiles { get; set; } = [];
	public string? Error { get; set; } = null;
}
