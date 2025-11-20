using Colossal.UI.Binding;
using Game.Reflection;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Editor;

public class TimeOfDayWeightsChart : Widget
{
	private float4 m_Value;

	public float min { get; set; }

	public float max { get; set; }

	public ITypedValueAccessor<float4> accessor { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		float4 @float = math.unlerp(min, max, accessor.GetTypedValue());
		if (!object.Equals(@float, m_Value))
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Value = @float;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		writer.Write(m_Value);
	}
}
