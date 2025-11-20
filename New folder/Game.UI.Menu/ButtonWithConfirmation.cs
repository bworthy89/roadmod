using Colossal.UI.Binding;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Menu;

public class ButtonWithConfirmation : Button
{
	private LocalizedString? m_ConfirmationMessage;

	public LocalizedString? confirmationMessage
	{
		get
		{
			return m_ConfirmationMessage;
		}
		set
		{
			if (!m_ConfirmationMessage.Equals(value))
			{
				m_ConfirmationMessage = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("confirmationMessage");
		writer.Write(confirmationMessage);
	}
}
