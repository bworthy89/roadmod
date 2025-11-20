using System;
using System.Reflection;
using Game.Reflection;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Editor;

public class TimeOfDayWeightsFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(float4))
		{
			float min = 0f;
			float max = 1f;
			WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max);
			float step = WidgetAttributeUtils.GetNumberStep(attributes, 0.1f);
			FieldInfo xField = typeof(float4).GetField("x");
			FieldInfo yField = typeof(float4).GetField("y");
			FieldInfo zField = typeof(float4).GetField("z");
			FieldInfo wField = typeof(float4).GetField("w");
			return (IValueAccessor accessor) => new Group
			{
				children = new IWidget[5]
				{
					new FloatSliderField
					{
						path = "x",
						displayName = "Night",
						min = min,
						max = max,
						fractionDigits = 1,
						step = step,
						accessor = new CastAccessor<double>(new FieldAccessor(accessor, xField), ToFloat, FromFloat)
					},
					new FloatSliderField
					{
						path = "y",
						displayName = "Morning",
						min = min,
						max = max,
						fractionDigits = 1,
						step = step,
						accessor = new CastAccessor<double>(new FieldAccessor(accessor, yField), ToFloat, FromFloat)
					},
					new FloatSliderField
					{
						path = "z",
						displayName = "Day",
						min = min,
						max = max,
						fractionDigits = 1,
						step = step,
						accessor = new CastAccessor<double>(new FieldAccessor(accessor, zField), ToFloat, FromFloat)
					},
					new FloatSliderField
					{
						path = "w",
						displayName = "Evening",
						min = min,
						max = max,
						fractionDigits = 1,
						step = step,
						accessor = new CastAccessor<double>(new FieldAccessor(accessor, wField), ToFloat, FromFloat)
					},
					new TimeOfDayWeightsChart
					{
						min = min,
						max = max,
						accessor = new CastAccessor<float4>(accessor)
					}
				}
			};
		}
		return null;
		static object FromFloat(double value)
		{
			return (float)value;
		}
		static double ToFloat(object value)
		{
			return (float)value;
		}
	}
}
