using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Widgets;
using UnityEngine;

namespace Game.UI.Editor;

public class ColorVariationField : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return delegate(IValueAccessor accessor)
		{
			if (!ColorPropertiesFieldUtils.TryGetColorProperties(accessor, out var colorProperties))
			{
				return (IWidget)null;
			}
			ColorProperties.VariationSet variationSet = (ColorProperties.VariationSet)accessor.GetValue();
			Column column = new Column
			{
				children = new List<IWidget>()
			};
			ColorPropertiesFieldUtils.EnsureColorsArray(variationSet);
			for (int i = 0; i < variationSet.m_Colors.Length; i++)
			{
				CastAccessor<Color> accessor2 = new CastAccessor<Color>(new ListElementAccessor<Color[]>(new DelegateAccessor<Color[]>(() => variationSet.m_Colors, delegate(Color[] value)
				{
					variationSet.m_Colors = value;
				}), typeof(Color), i));
				column.children.Add(new ColorField
				{
					accessor = accessor2,
					displayName = $"Channel {i}",
					path = $"ColorProperties.m_ColorVariations[{colorProperties.m_ColorVariations.IndexOf(variationSet)}].m_Colors[{i}]",
					tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationSet.m_Colors]"
				});
			}
			ColorPropertiesFieldUtils.EnsureVariationGroups(colorProperties);
			ColorPropertiesFieldUtils.EnsureVariationSetGroup(variationSet, colorProperties.m_VariationGroups);
			string[] lastVariationGroups = Array.Empty<string>();
			int itemsVersion = 0;
			column.children.Add(new DropdownField<string>
			{
				displayName = "Variation Group",
				accessor = new DelegateAccessor<string>(() => variationSet.m_VariationGroup ?? string.Empty, delegate(string value)
				{
					variationSet.m_VariationGroup = value;
				}),
				itemsAccessor = new DelegateAccessor<DropdownItem<string>[]>(() => ColorPropertiesFieldUtils.GetVariationGroupItems(colorProperties)),
				itemsVersion = delegate
				{
					ColorPropertiesFieldUtils.EnsureVariationGroups(colorProperties);
					if (!ColorPropertiesFieldUtils.VariationGroupsChanged(colorProperties.m_VariationGroups, lastVariationGroups))
					{
						return itemsVersion;
					}
					ColorPropertiesFieldUtils.EnsureVariationSetGroup(variationSet, colorProperties.m_VariationGroups);
					lastVariationGroups = colorProperties.m_VariationGroups.Select((ColorProperties.VariationGroup group) => group.m_Name).ToArray();
					return itemsVersion++;
				},
				hidden = () => colorProperties.m_VariationGroups == null || colorProperties.m_VariationGroups.Count == 0,
				path = "m_VariationGroup",
				tooltip = "Editor.TOOLTIP[Game.Prefabs.ColorProperties+VariationSet.m_VariationGroup]"
			});
			return column;
		};
	}
}
