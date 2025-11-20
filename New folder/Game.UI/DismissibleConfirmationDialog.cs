using Colossal.Annotations;
using Game.UI.Localization;

namespace Game.UI;

public class DismissibleConfirmationDialog : ConfirmationDialogBase
{
	protected override bool dismissible => true;

	public DismissibleConfirmationDialog(LocalizedString? title, LocalizedString message, LocalizedString confirmAction, LocalizedString? cancelAction, [CanBeNull] params LocalizedString[] otherActions)
		: base(title, message, null, copyButton: false, confirmAction, cancelAction, otherActions)
	{
	}
}
