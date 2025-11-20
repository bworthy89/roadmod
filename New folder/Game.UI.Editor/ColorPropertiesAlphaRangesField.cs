using System;
using System.Collections.Generic;
using Game.Reflection;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Editor;

public class ColorPropertiesAlphaRangesField : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return Create(memberType, attributes);
	}

	public static FieldBuilder Create(Type memberType, object[] attributes, string tooltipFormat = null)
	{
		return delegate(IValueAccessor accessor)
		{
			Column column = new Column();
			column.children = new List<IWidget>();
			column.path = "m_AlphaRanges";
			column.children.Add(new IntInputField
			{
				displayName = "Alpha 0 Randomness",
				accessor = new DelegateAccessor<int>(() => ((int3)accessor.GetValue()).x, delegate(int value)
				{
					int3 @int = (int3)accessor.GetValue();
					@int.x = value;
					accessor.SetValue(@int);
				}),
				min = 0,
				max = 100,
				path = "x",
				tooltip = ((tooltipFormat != null) ? string.Format(tooltipFormat, "x") : "Editor.TOOLTIP[Game.Prefabs.ColorProperties.m_AlphaRanges.x]")
			});
			column.children.Add(new IntInputField
			{
				displayName = "Alpha 1 Randomness",
				accessor = new DelegateAccessor<int>(() => ((int3)accessor.GetValue()).y, delegate(int value)
				{
					int3 @int = (int3)accessor.GetValue();
					@int.y = value;
					accessor.SetValue(@int);
				}),
				min = 0,
				max = 100,
				path = "y",
				tooltip = ((tooltipFormat != null) ? string.Format(tooltipFormat, "y") : "Editor.TOOLTIP[Game.Prefabs.ColorProperties.m_AlphaRanges.y]")
			});
			column.children.Add(new IntInputField
			{
				displayName = "Alpha 2 Randomness",
				accessor = new DelegateAccessor<int>(() => ((int3)accessor.GetValue()).z, delegate(int value)
				{
					int3 @int = (int3)accessor.GetValue();
					@int.z = value;
					accessor.SetValue(@int);
				}),
				min = 0,
				max = 100,
				path = "z",
				tooltip = ((tooltipFormat != null) ? string.Format(tooltipFormat, "z") : "Editor.TOOLTIP[Game.Prefabs.ColorProperties.m_AlphaRanges.z]")
			});
			return column;
		};
	}
}
