using Colossal.Annotations;
using Game.UI.Localization;

namespace Game.UI;

public class MessageDialog : ConfirmationDialogBase
{
	public MessageDialog(LocalizedString? title, LocalizedString message, LocalizedString confirmAction, [CanBeNull] params LocalizedString[] otherActions)
		: base(title, message, null, copyButton: false, confirmAction, null, otherActions)
	{
	}

	public MessageDialog(LocalizedString? title, LocalizedString message, LocalizedString? details, bool copyButton, LocalizedString confirmAction, [CanBeNull] params LocalizedString[] otherActions)
		: base(title, message, details, copyButton, confirmAction, null, otherActions)
	{
	}
}
