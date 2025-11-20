namespace Game.UI;

public static class DialogAction
{
	public const string kYes = "Common.DIALOG_ACTION[Yes]";

	public const string kNo = "Common.DIALOG_ACTION[No]";

	public static string GetId(string value)
	{
		return "Common.DIALOG_ACTION[" + value + "]";
	}
}
