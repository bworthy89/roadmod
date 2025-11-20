namespace Game.UI;

public static class DialogTitle
{
	public const string kWarning = "Common.DIALOG_TITLE[Warning]";

	public static string GetId(string value)
	{
		return "Common.DIALOG_TITLE[" + value + "]";
	}
}
