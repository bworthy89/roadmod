namespace Game.UI;

public class ParadoxCloudConflictResolutionDialog : ConfirmationDialog
{
	private const string kTitle = "Common.DIALOG_TITLE[PdxSdkCloudConflict]";

	private const string kMessage = "Common.DIALOG_MESSAGE[PdxSdkCloudConflict]";

	private const string kUseLocal = "Common.DIALOG_ACTION[UseLocal]";

	private const string kUseCloud = "Common.DIALOG_ACTION[UseCloud]";

	protected override string skin => "Paradox";

	public ParadoxCloudConflictResolutionDialog()
		: base("Common.DIALOG_TITLE[PdxSdkCloudConflict]", "Common.DIALOG_MESSAGE[PdxSdkCloudConflict]", "Common.DIALOG_ACTION[UseCloud]", null, "Common.DIALOG_ACTION[UseLocal]")
	{
	}
}
