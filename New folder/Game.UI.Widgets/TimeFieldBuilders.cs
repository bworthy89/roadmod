using System;
using Colossal.Mathematics;
using Game.Reflection;

namespace Game.UI.Widgets;

public class TimeFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(float))
		{
			if (WidgetAttributeUtils.IsTimeField(attributes))
			{
				float min = 0f;
				float max = 1f;
				WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max);
				return (IValueAccessor accessor) => new TimeSliderField
				{
					min = min,
					max = max,
					accessor = new CastAccessor<float>(accessor)
				};
			}
		}
		else if (memberType == typeof(Bounds1) && WidgetAttributeUtils.IsTimeField(attributes))
		{
			float min2 = 0f;
			float max2 = 1f;
			WidgetAttributeUtils.GetNumberRange(attributes, ref min2, ref max2);
			bool allowMinGreaterMax = WidgetAttributeUtils.AllowsMinGreaterMax(attributes);
			return (IValueAccessor accessor) => new TimeBoundsSliderField
			{
				min = min2,
				max = max2,
				allowMinGreaterMax = allowMinGreaterMax,
				accessor = new CastAccessor<Bounds1>(accessor)
			};
		}
		return null;
	}
}
