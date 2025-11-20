using System;
using Colossal.Mathematics;
using Game.Reflection;

namespace Game.UI.Widgets;

public class BoundsFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(Bounds1))
		{
			float min = float.MinValue;
			float max = float.MaxValue;
			float step = WidgetAttributeUtils.GetNumberStep(attributes, 0.01f);
			bool allowMinGreaterMax = WidgetAttributeUtils.AllowsMinGreaterMax(attributes);
			if (WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max) && !WidgetAttributeUtils.RequiresInputField(attributes))
			{
				return (IValueAccessor accessor) => new Bounds1SliderField
				{
					min = min,
					max = max,
					step = step,
					allowMinGreaterMax = allowMinGreaterMax,
					accessor = new CastAccessor<Bounds1>(accessor)
				};
			}
			return (IValueAccessor accessor) => new Bounds1InputField
			{
				min = min,
				max = max,
				step = step,
				allowMinGreaterMax = allowMinGreaterMax,
				accessor = new CastAccessor<Bounds1>(accessor)
			};
		}
		if (memberType == typeof(Bounds2))
		{
			bool allowMinGreaterMax2 = WidgetAttributeUtils.AllowsMinGreaterMax(attributes);
			return (IValueAccessor accessor) => new Bounds2InputField
			{
				allowMinGreaterMax = allowMinGreaterMax2,
				accessor = new CastAccessor<Bounds2>(accessor)
			};
		}
		if (memberType == typeof(Bounds3))
		{
			bool allowMinGreaterMax3 = WidgetAttributeUtils.AllowsMinGreaterMax(attributes);
			return (IValueAccessor accessor) => new Bounds3InputField
			{
				allowMinGreaterMax = allowMinGreaterMax3,
				accessor = new CastAccessor<Bounds3>(accessor)
			};
		}
		return null;
	}
}
