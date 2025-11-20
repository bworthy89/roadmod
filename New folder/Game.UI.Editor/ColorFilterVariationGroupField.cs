using System;
using System.Linq;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class ColorFilterVariationGroupField : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return delegate(IValueAccessor accessor)
		{
			if (ColorPropertiesFieldUtils.TryGetColorProperties(accessor, out var colorProperties))
			{
				string[] lastVariationGroups = Array.Empty<string>();
				int itemsVersion = 0;
				return new DropdownField<string>
				{
					displayName = "Variation Group",
					accessor = new DelegateAccessor<string>(() => ((string)accessor.GetValue()) ?? string.Empty, accessor.SetValue),
					itemsAccessor = new DelegateAccessor<DropdownItem<string>[]>(() => ColorPropertiesFieldUtils.GetVariationGroupItems(colorProperties)),
					itemsVersion = delegate
					{
						ColorPropertiesFieldUtils.EnsureVariationGroups(colorProperties);
						if (!ColorPropertiesFieldUtils.VariationGroupsChanged(colorProperties.m_VariationGroups, lastVariationGroups))
						{
							return itemsVersion;
						}
						ColorPropertiesFieldUtils.EnsureColorFilterVariationGroup(accessor, colorProperties.m_VariationGroups);
						lastVariationGroups = colorProperties.m_VariationGroups.Select((ColorProperties.VariationGroup group) => group.m_Name).ToArray();
						return itemsVersion++;
					},
					hidden = () => colorProperties.m_VariationGroups == null || colorProperties.m_VariationGroups.Count == 0,
					path = "m_VariationGroups"
				};
			}
			return new StringInputField
			{
				displayName = "Variation Group",
				accessor = new CastAccessor<string>(accessor),
				path = "m_VariationGroups"
			};
		};
	}
}
