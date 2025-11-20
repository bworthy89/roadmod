namespace Game.UI;

public static class DialogMessage
{
	public const string kBulldozer = "Common.DIALOG_MESSAGE[Bulldozer]";

	public const string kProgressLoss = "Common.DIALOG_MESSAGE[ProgressLoss]";

	public const string kOverwriteSave = "Common.DIALOG_MESSAGE[Overwrite]";

	public const string kOverwriteMap = "Common.DIALOG_MESSAGE[OverwriteMap]";

	public const string kOverwriteAsset = "Common.DIALOG_MESSAGE[OverwriteAsset]";

	public const string kConfirmWipe = "Common.DIALOG_MESSAGE[ConfirmRemoteStorageWipe]";

	public const string kDisableAchievements = "Common.DIALOG_MESSAGE[DisableAchievements]";

	public static string GetId(string value)
	{
		return "Common.DIALOG_MESSAGE[" + value + "]";
	}
}
