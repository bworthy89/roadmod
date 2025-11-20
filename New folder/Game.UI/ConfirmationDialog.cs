using Colossal.Annotations;
using Game.UI.Localization;

namespace Game.UI;

public class ConfirmationDialog : ConfirmationDialogBase
{
	public ConfirmationDialog(LocalizedString? title, LocalizedString message, LocalizedString confirmAction, LocalizedString? cancelAction, [CanBeNull] params LocalizedString[] otherActions)
		: base(title, message, null, copyButton: false, confirmAction, cancelAction, otherActions)
	{
	}

	public ConfirmationDialog(LocalizedString? title, LocalizedString message, LocalizedString? details, bool copyButton, LocalizedString confirmAction, LocalizedString? cancelAction, [CanBeNull] params LocalizedString[] otherActions)
		: base(title, message, details, copyButton, confirmAction, cancelAction, otherActions)
	{
	}
}
