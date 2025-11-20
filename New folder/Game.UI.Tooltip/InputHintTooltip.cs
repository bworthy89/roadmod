using Colossal.UI.Binding;
using Game.Input;
using Game.UI.Widgets;

namespace Game.UI.Tooltip;

public class InputHintTooltip : Widget
{
	private const string kInputHint = "hint";

	public ProxyAction m_Action;

	private InputHintBindings.InputHint m_Hint;

	private InputManager.DeviceType m_Device;

	public InputHintTooltip(ProxyAction action, InputManager.DeviceType device)
	{
		m_Action = action;
		m_Device = device;
		base.path = action.title + m_Device;
		Refresh();
	}

	public void Refresh()
	{
		if (m_Hint == null || m_Hint.name != (m_Action.displayOverride?.displayName ?? m_Action.title))
		{
			m_Hint = InputHintBindings.InputHint.Create(m_Action);
			InputHintBindings.CollectHintItems(m_Hint, m_Action, m_Device, m_Action.displayOverride?.transform ?? UIBaseInputAction.Transform.None);
			SetPropertiesChanged();
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("hint");
		writer.Write(m_Hint);
	}
}
