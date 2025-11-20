using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Widgets;
using UnityEngine;

namespace Game.UI.Editor;

public static class ColorPropertiesFieldUtils
{
	private const int kChannelCount = 3;

	public static void EnsureColorsArray(ColorProperties.VariationSet variationSet)
	{
		if (variationSet.m_Colors == null || variationSet.m_Colors.Length != 3)
		{
			Color[] colors = variationSet.m_Colors;
			variationSet.m_Colors = new Color[3];
			for (int i = 0; i < 3; i++)
			{
				variationSet.m_Colors[i] = ((colors != null && colors.Length > i) ? colors[i] : Color.white);
			}
		}
	}

	public static void EnsureVariationGroups(ColorProperties colorProperties)
	{
		if (colorProperties.m_VariationGroups == null)
		{
			colorProperties.m_VariationGroups = new List<ColorProperties.VariationGroup>();
		}
		if (colorProperties.m_VariationGroups.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(colorProperties.m_VariationGroups.Count);
		foreach (ColorProperties.VariationGroup variationGroup in colorProperties.m_VariationGroups)
		{
			string text = ((!string.IsNullOrEmpty(variationGroup.m_Name)) ? variationGroup.m_Name : "Variation Group");
			string text2 = text;
			int num = 1;
			while (hashSet.Contains(text2))
			{
				text2 = $"{text} ({num})";
				num++;
			}
			variationGroup.m_Name = text2;
			hashSet.Add(text2);
		}
	}

	public static void EnsureVariationSetGroup(ColorProperties.VariationSet variationSet, List<ColorProperties.VariationGroup> variationGroups)
	{
		if (!variationGroups.Exists((ColorProperties.VariationGroup group) => group.m_Name == variationSet.m_VariationGroup))
		{
			variationSet.m_VariationGroup = ((variationGroups.Count > 0) ? variationGroups[0].m_Name : null);
		}
	}

	public static void EnsureColorFilterVariationGroup(IValueAccessor accessor, List<ColorProperties.VariationGroup> variationGroups)
	{
		if (!variationGroups.Exists((ColorProperties.VariationGroup group) => group.m_Name == accessor.GetValue().ToString()))
		{
			accessor.SetValue((variationGroups.Count > 0) ? variationGroups[0].m_Name : null);
		}
	}

	public static DropdownItem<string>[] GetVariationGroupItems(ColorProperties colorProperties)
	{
		return colorProperties.m_VariationGroups.Select((ColorProperties.VariationGroup group) => new DropdownItem<string>
		{
			displayName = group.m_Name,
			value = group.m_Name
		}).ToArray();
	}

	public static bool VariationGroupsChanged(List<ColorProperties.VariationGroup> variationGroups, string[] lastVariationGroups)
	{
		if (variationGroups.Count != lastVariationGroups.Length)
		{
			return true;
		}
		int count = variationGroups.Count;
		for (int i = 0; i < count; i++)
		{
			if (variationGroups[i].m_Name != lastVariationGroups[i])
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetColorProperties(IValueAccessor accessor, out ColorProperties colorProperties)
	{
		IValueAccessor valueAccessor = accessor;
		int num = 0;
		while (valueAccessor != null)
		{
			if (valueAccessor.GetValue() is ComponentBase componentBase)
			{
				colorProperties = componentBase.GetComponent<ColorProperties>();
				return colorProperties != null;
			}
			valueAccessor = valueAccessor.parent;
			num++;
			if (num > 20)
			{
				break;
			}
		}
		colorProperties = null;
		return false;
	}
}
