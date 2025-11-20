using System;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class Compass : Widget
{
	private float m_Angle;

	public Func<float> angle;

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		float num = angle?.Invoke() ?? 0f;
		if (num != m_Angle)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Angle = num;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("angle");
		writer.Write(m_Angle);
	}
}
