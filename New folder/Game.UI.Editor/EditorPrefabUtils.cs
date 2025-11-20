using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.UI;
using Game.Prefabs;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public static class EditorPrefabUtils
{
	public struct IconInfo
	{
		public ImageAsset m_Asset;

		public string m_URI;

		public FieldInfo m_Field;

		public ComponentBase m_Component;
	}

	private static readonly Dictionary<Type, string[]> s_PrefabTypes = new Dictionary<Type, string[]>();

	private static readonly Dictionary<Type, string[]> s_PrefabTags = new Dictionary<Type, string[]>();

	public static readonly LocalizedString kNone = LocalizedString.Id("Editor.NONE_VALUE");

	public static string GetPrefabTypeName(Type type)
	{
		return type.FullName;
	}

	[CanBeNull]
	public static PrefabBase GetPrefabByID([CanBeNull] string prefabID)
	{
		return null;
	}

	[CanBeNull]
	public static T GetPrefabByID<T>([CanBeNull] string prefabID) where T : PrefabBase
	{
		return null;
	}

	[CanBeNull]
	public static string GetPrefabID(PrefabBase prefab)
	{
		if (prefab != null && AssetDatabase.global.resources.prefabsMap.TryGetGuid(prefab, out var id))
		{
			return id;
		}
		return null;
	}

	public static string[] GetPrefabTypes(Type type)
	{
		if (s_PrefabTypes.TryGetValue(type, out var value))
		{
			return value;
		}
		List<string> list = new List<string>();
		Type type2 = type;
		while (type2 != null && type2 != typeof(PrefabBase) && typeof(PrefabBase).IsAssignableFrom(type2))
		{
			list.Add(GetPrefabTypeName(type2));
			type2 = type2.BaseType;
		}
		return s_PrefabTypes[type] = list.ToArray();
	}

	public static string[] GetPrefabTags(Type type)
	{
		if (s_PrefabTags.TryGetValue(type, out var value))
		{
			return value;
		}
		List<string> list = new List<string>();
		Type type2 = type;
		while (type2 != null && type2 != typeof(PrefabBase) && typeof(PrefabBase).IsAssignableFrom(type2))
		{
			string text = type2.Name;
			if (text.Length > 6 && text.EndsWith("Prefab"))
			{
				text = text.Substring(0, text.Length - 6);
			}
			list.Add(text.ToLowerInvariant());
			type2 = type2.BaseType;
		}
		return s_PrefabTags[type] = list.ToArray();
	}

	public static void SavePrefab(PrefabBase prefab)
	{
		(prefab.asset ?? AssetDatabase.user.AddAsset(AssetDataPath.Create("StreamingData~/" + prefab.name, prefab.name ?? ""), prefab)).Save();
	}

	public static IEnumerable<LocaleAsset> GetLocaleAssets(PrefabBase prefab)
	{
		if (!(prefab.asset != null) || prefab.asset.database == AssetDatabase.game)
		{
			yield break;
		}
		foreach (LocaleAsset asset in AssetDatabase.global.GetAssets(SearchFilter<LocaleAsset>.ByCondition((LocaleAsset a) => a.subPath == prefab.asset.subPath)))
		{
			yield return asset;
		}
	}

	public static IEnumerable<IconInfo> GetIcons(PrefabBase prefab)
	{
		foreach (ComponentBase comp in prefab.components)
		{
			FieldInfo[] fields = comp.GetType().GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType != typeof(string))
				{
					continue;
				}
				CustomFieldAttribute customAttribute = fieldInfo.GetCustomAttribute<CustomFieldAttribute>();
				if (customAttribute != null && !(customAttribute.Factory != typeof(UIIconField)))
				{
					string text = (string)fieldInfo.GetValue(comp);
					if (UIExtensions.TryGetImageAsset(text, out var imageAsset))
					{
						yield return new IconInfo
						{
							m_Asset = imageAsset,
							m_URI = text,
							m_Field = fieldInfo,
							m_Component = comp
						};
					}
				}
			}
		}
	}

	public static LocalizedString GetPrefabLabel(PrefabBase prefab)
	{
		if (prefab == null)
		{
			return kNone;
		}
		if (prefab.asset != null)
		{
			SourceMeta meta = prefab.asset.GetMeta();
			if (prefab.asset.database == AssetDatabase<ParadoxMods>.instance)
			{
				return LocalizedString.Value($"{prefab.name} - ({meta.platformID})");
			}
			if (prefab.asset.database == AssetDatabase.user)
			{
				string text = (meta.packaged ? meta.packageName : prefab.asset.name);
				return LocalizedString.Value(prefab.name + " - (" + text + ")");
			}
		}
		return LocalizedString.Value(prefab.name);
	}

	public static IEnumerable<AssetItem> GetUserImages()
	{
		yield return new AssetItem
		{
			guid = default(Hash128),
			displayName = kNone
		};
		foreach (ImageAsset asset in AssetDatabase.global.GetAssets(SearchFilter<ImageAsset>.ByCondition((ImageAsset a) => a.GetMeta().subPath?.StartsWith(ScreenUtility.kScreenshotDirectory) ?? false)))
		{
			using (asset)
			{
				yield return new AssetItem
				{
					guid = asset.id,
					fileName = asset.name,
					displayName = asset.name,
					image = asset.ToUri()
				};
			}
		}
	}
}
