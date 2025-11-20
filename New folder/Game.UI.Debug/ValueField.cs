using Game.UI.Widgets;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI.Debug;

public class ValueField : Game.UI.Widgets.ValueField
{
	private DebugUI.Value m_DebugWidget;

	private object m_ObjectValue;

	private string m_StringValue;

	private float m_Timer;

	public override string propertiesTypeName => typeof(Game.UI.Widgets.ValueField).FullName;

	public ValueField(DebugUI.Value debugWidget)
	{
		m_DebugWidget = debugWidget;
	}

	protected override WidgetChanges Update()
	{
		m_Timer -= Time.deltaTime;
		if (m_Timer <= 0f)
		{
			m_Timer = m_DebugWidget.refreshRate;
			object value = m_DebugWidget.GetValue();
			if (!object.Equals(value, m_ObjectValue))
			{
				m_ObjectValue = value;
				m_StringValue = m_DebugWidget.FormatString(m_ObjectValue);
			}
		}
		return base.Update();
	}

	public override string GetValue()
	{
		return m_StringValue ?? string.Empty;
	}
}
