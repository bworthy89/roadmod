using System;
using Game.Reflection;

namespace Game.UI.Widgets;

public class UIntFieldBuilders : IFieldBuilderFactory
{
	private static readonly uint kGlobalValueRange = 10000000u;

	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(uint))
		{
			uint min = 0u;
			uint max = ((!EditorGenerator.sBypassValueLimits) ? kGlobalValueRange : uint.MaxValue);
			uint step = WidgetAttributeUtils.GetNumberStep(attributes, 1u);
			if (WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max) && !WidgetAttributeUtils.RequiresInputField(attributes))
			{
				string unit = WidgetAttributeUtils.GetNumberUnit(attributes);
				return (IValueAccessor accessor) => new UIntSliderField
				{
					min = min,
					max = max,
					step = step,
					unit = unit,
					accessor = new CastAccessor<uint>(accessor)
				};
			}
			return (IValueAccessor accessor) => new UIntInputField
			{
				min = min,
				max = max,
				step = step,
				accessor = new CastAccessor<uint>(accessor)
			};
		}
		return null;
	}
}
