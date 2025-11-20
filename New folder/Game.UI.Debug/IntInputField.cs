using Game.Debug;
using Game.UI.Widgets;

namespace Game.UI.Debug;

public class IntInputField : Game.UI.Widgets.IntInputField
{
	private Game.Debug.IntInputField m_DebugWidget;

	private string m_StringValue;

	private int m_IntValue;

	public override string propertiesTypeName => typeof(Game.UI.Widgets.IntInputField).FullName;

	public IntInputField(Game.Debug.IntInputField debugWidget)
	{
		m_DebugWidget = debugWidget;
	}

	protected override WidgetChanges Update()
	{
		string value = m_DebugWidget.GetValue();
		if (!string.Equals(m_StringValue, value))
		{
			m_StringValue = value;
			int.TryParse(value, out m_IntValue);
		}
		return base.Update();
	}

	public override int GetValue()
	{
		return m_IntValue;
	}

	public override void SetValue(int value)
	{
		m_DebugWidget.SetValue(value.ToString());
	}
}
