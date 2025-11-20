using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Reflection;
using Game.Rendering;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Editor;

public class VariationGroupField : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return delegate(IValueAccessor accessor)
		{
			Column column = new Column
			{
				children = new List<IWidget>()
			};
			ColorProperties.VariationGroup variationGroup = accessor.GetValue() as ColorProperties.VariationGroup;
			if (variationGroup == null)
			{
				return column;
			}
			column.children.Add(new StringInputField
			{
				displayName = "Name",
				accessor = new DelegateAccessor<string>(() => variationGroup.m_Name, delegate(string value)
				{
					variationGroup.m_Name = value;
				}),
				path = "m_Name",
				tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_Name]"
			});
			column.children.Add(new IntInputField
			{
				displayName = "Probability",
				accessor = new DelegateAccessor<int>(() => variationGroup.m_Probability, delegate(int value)
				{
					variationGroup.m_Probability = value;
				}),
				min = 0,
				max = 100,
				path = "m_Probability",
				tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_Probability]"
			});
			NamedWidgetWithTooltip namedWidgetWithTooltip = (NamedWidgetWithTooltip)EditorGenerator.kFactories.Find((IFieldBuilderFactory factory) => factory is EnumFieldBuilders).TryCreate(typeof(ColorSyncFlags), typeof(ColorSyncFlags).GetCustomAttributes(inherit: false))(new DelegateAccessor<ColorSyncFlags>(() => variationGroup.m_MeshSyncMode, delegate(ColorSyncFlags value)
			{
				variationGroup.m_MeshSyncMode = value;
			}));
			namedWidgetWithTooltip.displayName = "Mesh Sync Mode";
			namedWidgetWithTooltip.path = "m_MeshSyncMode";
			namedWidgetWithTooltip.tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_MeshSyncMode]";
			column.children.Add(namedWidgetWithTooltip);
			column.children.Add(new ToggleField
			{
				displayName = "Override Randomness",
				accessor = new DelegateAccessor<bool>(() => variationGroup.m_OverrideRandomness, delegate(bool value)
				{
					variationGroup.m_OverrideRandomness = value;
				}),
				path = "m_OverrideRandomness",
				tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_OverrideRandomness]"
			});
			IWidget widget = ColorPropertiesVariationRangesField.Create(typeof(int3), Array.Empty<object>(), "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_VariationRanges.{0}]")(new DelegateAccessor<int3>(() => variationGroup.m_VariationRanges, delegate(int3 value)
			{
				variationGroup.m_VariationRanges = value;
			}));
			((Widget)widget).hidden = () => !variationGroup.m_OverrideRandomness;
			column.children.Add(widget);
			IWidget widget2 = ColorPropertiesAlphaRangesField.Create(typeof(int3), Array.Empty<object>(), "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationGroup.m_AlphaRanges.{0}]")(new DelegateAccessor<int3>(() => variationGroup.m_AlphaRanges, delegate(int3 value)
			{
				variationGroup.m_AlphaRanges = value;
			}));
			((Widget)widget2).hidden = () => !variationGroup.m_OverrideRandomness;
			column.children.Add(widget2);
			return column;
		};
	}
}
