using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game.Assets;
using Game.Prefabs;

namespace Game.PSI;

public static class ModTags
{
	private static ILog sLog = LogManager.GetLogger("Platforms");

	public static readonly int kMaxTags = 10;

	private static readonly Type[] sExcludePropTypes = new Type[4]
	{
		typeof(PillarObject),
		typeof(TreeObject),
		typeof(PlantObject),
		typeof(NetObject)
	};

	public static void GetTags(AssetData asset, HashSet<string> tags, HashSet<string> typeTags, HashSet<string> validTags)
	{
		GetAssetTypeTags(asset, tags, typeTags, validTags);
		if (asset is MapMetadata map)
		{
			GetMapTags(map, tags, typeTags, validTags);
		}
		else if (asset is SaveGameMetadata save)
		{
			GetSaveTags(save, tags, typeTags, validTags);
		}
		else if (asset is PrefabAsset prefabAsset && prefabAsset.Load() is PrefabBase prefab)
		{
			GetPrefabTags(prefab, tags, typeTags, validTags);
		}
		tags.UnionWith(typeTags);
		while (tags.Count > kMaxTags)
		{
			string text = tags.FirstOrDefault((string tag) => !typeTags.Contains(tag));
			if (text != null)
			{
				tags.Remove(text);
			}
			else
			{
				text = tags.FirstOrDefault();
				if (text == null)
				{
					break;
				}
				tags.Remove(text);
				typeTags.Remove(text);
			}
			sLog.WarnFormat("Generated mod tags for {0} exceed max count, removing {1}", asset.name, text);
		}
	}

	private static void GetMapTags(MapMetadata map, HashSet<string> tags, HashSet<string> typeTags, HashSet<string> validTags)
	{
		if (validTags.Contains(map.target.theme))
		{
			tags.Add(map.target.theme);
		}
	}

	private static void GetSaveTags(SaveGameMetadata save, HashSet<string> tags, HashSet<string> typeTags, HashSet<string> validTags)
	{
		if (validTags.Contains(save.target.theme))
		{
			tags.Add(save.target.theme);
		}
	}

	private static void GetPrefabTags(PrefabBase prefab, HashSet<string> tags, HashSet<string> typeTags, HashSet<string> validTags)
	{
		foreach (string componentTag in GetComponentTags(prefab, validTags, typeof(PrefabBase)))
		{
			typeTags.Add(componentTag);
		}
		foreach (ComponentBase component in prefab.components)
		{
			foreach (string componentTag2 in GetComponentTags(component, validTags, typeof(ComponentBase)))
			{
				tags.Add(componentTag2);
			}
		}
	}

	private static IEnumerable<string> GetComponentTags(ComponentBase component, HashSet<string> validTags, Type terminateAtType)
	{
		Type type = component.GetType();
		while (type != terminateAtType && type != null)
		{
			if (!type.IsDefined(typeof(ExcludeGeneratedModTagAttribute), inherit: false))
			{
				string text = type.Name.Replace("Prefab", string.Empty).Replace("Object", string.Empty);
				if (validTags.Contains(text))
				{
					yield return text;
				}
			}
			type = type.BaseType;
		}
		foreach (string modTag in component.modTags)
		{
			if (validTags.Contains(modTag))
			{
				yield return modTag;
			}
		}
	}

	private static void GetAssetTypeTags(AssetData assetData, HashSet<string> tags, HashSet<string> typeTags, HashSet<string> validTags)
	{
		Type type = assetData.GetType();
		while (type != typeof(AssetData))
		{
			if (!type.IsDefined(typeof(ExcludeGeneratedModTagAttribute), inherit: false))
			{
				string item = type.Name.Replace("Metadata", "").Replace("Asset", "");
				if (validTags.Contains(item))
				{
					typeTags.Add(item);
				}
			}
			type = type.BaseType;
		}
		foreach (string modTag in assetData.modTags)
		{
			if (validTags.Contains(modTag))
			{
				typeTags.Add(modTag);
			}
		}
	}

	public static IEnumerable<string> GetEnumFlagTags<T>(T value, T defaultValue) where T : Enum
	{
		bool flag = false;
		foreach (T value2 in Enum.GetValues(typeof(T)))
		{
			object flag2 = value2;
			if (value.HasFlag((Enum)flag2))
			{
				yield return value.ToString();
				flag = true;
			}
		}
		if (!flag)
		{
			yield return defaultValue.ToString();
		}
	}

	public static bool IsProp(PrefabBase prefab)
	{
		if (prefab.GetType() == typeof(StaticObjectPrefab))
		{
			Type[] array = sExcludePropTypes;
			foreach (Type type in array)
			{
				if (prefab.TryGet(type, out var _))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}
}
