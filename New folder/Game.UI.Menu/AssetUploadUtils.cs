using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.UI;
using Game.Assets;
using Game.Prefabs;
using Game.UI.Editor;
using UnityEngine;

namespace Game.UI.Menu;

public static class AssetUploadUtils
{
	private static ILog sLog = LogManager.GetLogger("AssetUpload");

	public static IModsUploadSupport.ExternalLinkData defaultExternalLink => new IModsUploadSupport.ExternalLinkData
	{
		m_Type = IModsUploadSupport.ExternalLinkInfo.kAcceptedTypes[0].m_Type,
		m_URL = string.Empty
	};

	public static bool LockLinkType(string url, out string type)
	{
		url = url.ToLower().Trim();
		if (!url.StartsWith("https://"))
		{
			url = "https://" + url;
		}
		IModsUploadSupport.ExternalLinkInfo[] kAcceptedTypes = IModsUploadSupport.ExternalLinkInfo.kAcceptedTypes;
		for (int i = 0; i < kAcceptedTypes.Length; i++)
		{
			IModsUploadSupport.ExternalLinkInfo externalLinkInfo = kAcceptedTypes[i];
			if (externalLinkInfo.m_Regex.IsMatch(url))
			{
				type = externalLinkInfo.m_Type;
				return true;
			}
		}
		type = null;
		return false;
	}

	public static bool ValidateExternalLink(IModsUploadSupport.ExternalLinkData link)
	{
		if (string.IsNullOrWhiteSpace(link.m_URL))
		{
			return true;
		}
		if (LockLinkType(link.m_URL, out var type) && type == link.m_Type)
		{
			return true;
		}
		return false;
	}

	public static bool ValidateExternalLinks(IEnumerable<IModsUploadSupport.ExternalLinkData> links)
	{
		foreach (IModsUploadSupport.ExternalLinkData link in links)
		{
			if (!ValidateExternalLink(link))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ValidateForumLink(string link)
	{
		if (string.IsNullOrWhiteSpace(link))
		{
			return true;
		}
		return link.ToLower().Contains("paradoxplaza.com");
	}

	public static AssetData CopyPreviewImage(AssetData asset, ILocalAssetDatabase database, AssetDataPath path)
	{
		try
		{
			if (asset is ImageAsset imageAsset)
			{
				return imageAsset.Save(ImageAsset.FileFormat.JPG, path, database);
			}
			if (asset is TextureAsset textureAsset)
			{
				return textureAsset.SaveAsImageAsset(ImageAsset.FileFormat.JPG, path, database);
			}
		}
		catch (Exception exception)
		{
			sLog.Error(exception);
		}
		return null;
	}

	public static void CopyAsset(AssetData asset, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed, HashSet<IModsUploadSupport.ModInfo.ModDependency> externalReferences, bool copyReverseDependencies, bool binaryPackAssets, int platformID = 0)
	{
		if (asset is MapMetadata metadata)
		{
			CopyMap(metadata, database, processed);
		}
		else if (asset is SaveGameMetadata metadata2)
		{
			CopySave(metadata2, database, processed);
		}
		else if (asset is PrefabAsset prefabAsset)
		{
			CopyPrefab(prefabAsset, database, processed, externalReferences, copyReverseDependencies, binaryPackAssets, platformID);
		}
		else
		{
			CopyAssetGeneric(asset, database, processed, keepGuid: true);
		}
	}

	public static void CopyMap(MapMetadata metadata, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed)
	{
		MapInfo mapInfo = metadata.target.Copy();
		MapData mapData = CopyAssetGeneric(mapInfo.mapData, database, processed);
		mapInfo.mapData = mapData;
		if (metadata.target.preview != null)
		{
			TextureAsset preview = CopyAssetGeneric(mapInfo.preview, database, processed);
			mapInfo.preview = preview;
		}
		if (metadata.target.thumbnail != null)
		{
			TextureAsset thumbnail = CopyAssetGeneric(mapInfo.thumbnail, database, processed);
			mapInfo.thumbnail = thumbnail;
		}
		if (mapInfo.localeAssets != null)
		{
			LocaleAsset[] array = new LocaleAsset[mapInfo.localeAssets.Length];
			for (int i = 0; i < mapInfo.localeAssets.Length; i++)
			{
				array[i] = CopyAssetGeneric(mapInfo.localeAssets[i], database, processed);
			}
			mapInfo.localeAssets = array;
		}
		if (mapInfo.climate != null)
		{
			mapInfo.climate = CopyAssetGeneric(mapInfo.climate, database, processed);
		}
		MapMetadata mapMetadata = (mapInfo.metaData = CopyAssetGeneric(metadata, database, processed));
		mapMetadata.target = mapInfo;
		mapMetadata.Save();
	}

	public static void CopySave(SaveGameMetadata metadata, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed)
	{
		SaveInfo saveInfo = metadata.target.Copy();
		SaveGameData saveGameData = CopyAssetGeneric(saveInfo.saveGameData, database, processed);
		saveInfo.saveGameData = saveGameData;
		if (metadata.target.preview != null)
		{
			TextureAsset preview = CopyAssetGeneric(saveInfo.preview, database, processed);
			saveInfo.preview = preview;
		}
		SaveGameMetadata saveGameMetadata = (saveInfo.metaData = CopyAssetGeneric(metadata, database, processed));
		saveGameMetadata.target = saveInfo;
		saveGameMetadata.Save();
	}

	public static void CopyPrefab(PrefabAsset prefabAsset, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed, HashSet<IModsUploadSupport.ModInfo.ModDependency> externalReferences, bool copyReverseDependencies, bool binaryPackAssets, int platformID = 0)
	{
		HashSet<AssetData> hashSet = new HashSet<AssetData>();
		CollectPrefabAssetDependencies(prefabAsset, hashSet, copyReverseDependencies);
		foreach (AssetData item in hashSet)
		{
			if (processed.ContainsKey(item))
			{
				continue;
			}
			SourceMeta meta = item.GetMeta();
			if (meta.platformID > 0 && meta.platformID != platformID)
			{
				externalReferences.Add(new IModsUploadSupport.ModInfo.ModDependency
				{
					m_Id = meta.platformID,
					m_Version = meta.platformVersion
				});
				continue;
			}
			AssetData value;
			if (binaryPackAssets && item is PrefabAsset prefabAsset2)
			{
				PrefabBase obj = (PrefabBase)prefabAsset2.Load();
				PrefabBase data = obj.Clone(obj.name);
				PrefabAsset prefabAsset3 = database.AddAsset<PrefabAsset, ScriptableObject>(item.name, data, item.id);
				prefabAsset3.Save(ContentType.Binary, includeUnityDependencies: false, force: true);
				value = prefabAsset3;
			}
			else
			{
				value = CopyAssetGeneric(item, database, processed, !(item is LocaleAsset));
			}
			processed[item] = value;
		}
	}

	public static T CopyAssetGeneric<T>(T asset, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed, bool keepGuid = false) where T : AssetData
	{
		if (processed.TryGetValue(asset, out var value))
		{
			return value as T;
		}
		using Stream stream = asset.GetReadStream();
		T val = database.AddAsset<T>(asset.name, keepGuid ? asset.id : default(Identifier));
		using (Stream destination = val.GetWriteStream())
		{
			stream.CopyTo(destination);
		}
		processed.Add(asset, val);
		return val;
	}

	public static AssetData CopyAssetGeneric(AssetData asset, ILocalAssetDatabase database, Dictionary<AssetData, AssetData> processed, bool keepGuid = false)
	{
		if (processed.TryGetValue(asset, out var value))
		{
			return value;
		}
		using Stream stream = asset.GetReadStream();
		value = database.AddAsset(asset.name, asset.GetType(), keepGuid ? asset.id : default(Identifier)) as AssetData;
		using (Stream destination = value.GetWriteStream())
		{
			stream.CopyTo(destination);
		}
		processed.Add(asset, value);
		return value;
	}

	public static bool TryGetPreview(AssetData asset, out AssetData result)
	{
		if (asset is SaveGameMetadata saveGameMetadata)
		{
			result = saveGameMetadata.target.preview;
			return result != null;
		}
		if (asset is MapMetadata mapMetadata)
		{
			result = mapMetadata.target.preview;
			return result != null;
		}
		if (asset is PrefabAsset prefabAsset && prefabAsset.Load() is PrefabBase prefabBase && prefabBase.TryGet<UIObject>(out var component) && UIExtensions.TryGetImageAsset(component.m_Icon, out var imageAsset))
		{
			result = imageAsset;
			return true;
		}
		result = null;
		return false;
	}

	public static string GetImageURI(AssetData asset)
	{
		if (asset is ImageAsset asset2)
		{
			return asset2.ToUri();
		}
		if (asset is TextureAsset asset3)
		{
			return asset3.ToUri();
		}
		return MenuHelpers.defaultPreview.ToUri();
	}

	public static void CollectPrefabAssetDependencies(PrefabAsset prefabAsset, HashSet<AssetData> dependencies, bool collectReverseDependencies)
	{
		HashSet<PrefabBase> hashSet = new HashSet<PrefabBase>();
		CollectPrefabDependencies(prefabAsset.Load() as PrefabBase, hashSet, collectReverseDependencies);
		foreach (PrefabBase item in hashSet)
		{
			List<AssetData> list = new List<AssetData>();
			GetAssets(item, list);
			foreach (AssetData item2 in list)
			{
				if (item2.database != AssetDatabase.game)
				{
					dependencies.Add(item2);
				}
			}
		}
	}

	public static void CollectPrefabDependencies(PrefabBase mainPrefab, HashSet<PrefabBase> prefabs, bool collectReverseDependencies)
	{
		if (prefabs.Contains(mainPrefab))
		{
			return;
		}
		Stack<PrefabBase> stack = new Stack<PrefabBase>();
		stack.Push(mainPrefab);
		prefabs.Add(mainPrefab);
		PrefabBase result;
		while (stack.TryPop(out result))
		{
			List<PrefabBase> list = new List<PrefabBase>();
			List<ComponentBase> list2 = new List<ComponentBase>();
			result.GetComponents(list2);
			foreach (ComponentBase item in list2)
			{
				item.GetDependencies(list);
			}
			if (collectReverseDependencies)
			{
				CollectExtraPrefabDependencies(result, mainPrefab, list);
			}
			foreach (PrefabBase item2 in list)
			{
				if (item2 != null && item2.asset != null && item2.asset.database != AssetDatabase.game && !prefabs.Contains(item2))
				{
					stack.Push(item2);
					prefabs.Add(item2);
				}
			}
		}
	}

	private static void CollectExtraPrefabDependencies(PrefabBase prefab, PrefabBase mainPrefab, List<PrefabBase> prefabDependencies)
	{
		if (prefab is ZonePrefab zonePrefab && (prefab == mainPrefab || (prefab.TryGet<AssetPackItem>(out var component) && component.m_Packs != null && component.m_Packs.Contains(mainPrefab))))
		{
			foreach (PrefabAsset asset in AssetDatabase.global.GetAssets(default(SearchFilter<PrefabAsset>)))
			{
				if (asset.Load() is PrefabBase prefabBase && prefabBase.TryGet<SpawnableBuilding>(out var component2) && component2.m_ZoneType == zonePrefab)
				{
					prefabDependencies.Add(prefabBase);
				}
			}
			return;
		}
		if (!(prefab is AssetPackPrefab value) || !(prefab == mainPrefab))
		{
			return;
		}
		foreach (PrefabAsset asset2 in AssetDatabase.global.GetAssets(default(SearchFilter<PrefabAsset>)))
		{
			if (asset2.Load() is PrefabBase prefabBase2 && prefabBase2.TryGet<AssetPackItem>(out var component3) && component3.m_Packs != null && component3.m_Packs.Contains(value))
			{
				prefabDependencies.Add(prefabBase2);
			}
		}
	}

	private static void GetAssets(PrefabBase prefab, List<AssetData> assets)
	{
		assets.Add(prefab.asset);
		if (prefab is RenderPrefab renderPrefab)
		{
			assets.Add(renderPrefab.geometryAsset);
			foreach (SurfaceAsset surfaceAsset in renderPrefab.surfaceAssets)
			{
				assets.Add(surfaceAsset);
				surfaceAsset.LoadProperties(useVT: false);
				foreach (TextureAsset value in surfaceAsset.textures.Values)
				{
					assets.Add(value);
				}
			}
		}
		foreach (LocaleAsset localeAsset in EditorPrefabUtils.GetLocaleAssets(prefab))
		{
			assets.Add(localeAsset);
		}
		foreach (EditorPrefabUtils.IconInfo icon in EditorPrefabUtils.GetIcons(prefab))
		{
			assets.Add(icon.m_Asset);
		}
	}

	public static void CreateThumbnailAtlas(Dictionary<AssetData, AssetData> processed, ILocalAssetDatabase database)
	{
		AtlasFrame atlasFrame = new AtlasFrame(0, 0, rotations: false, 0);
		HashSet<AssetData> hashSet = new HashSet<AssetData>();
		foreach (AssetData key in processed.Keys)
		{
			if (!(key is PrefabAsset prefabAsset))
			{
				continue;
			}
			foreach (EditorPrefabUtils.IconInfo icon in EditorPrefabUtils.GetIcons((PrefabBase)prefabAsset.Load()))
			{
				if (processed.TryGetValue(icon.m_Asset, out var value))
				{
					PrefabAsset prefabAsset2 = (PrefabAsset)processed[prefabAsset];
					ComponentBase componentExactly = ((PrefabBase)prefabAsset2.Load()).GetComponentExactly(icon.m_Component.GetType());
					UnityEngine.Debug.Log($"{prefabAsset2.name}: {value.name}\n{icon.m_Field.DeclaringType?.Name}.{icon.m_Field.Name}: {icon.m_Field.GetValue(componentExactly)}");
					if (atlasFrame.TryAdd(value.name, ((ImageAsset)value).Load()))
					{
						icon.m_Field.SetValue(componentExactly, "thumbnail://insert thumbnail URI here");
						hashSet.Add(value);
						prefabAsset2.Save(force: true);
					}
				}
			}
		}
		if (hashSet.Count <= 0)
		{
			return;
		}
		database.AddAsset("ThumbnailAtlas", atlasFrame);
		foreach (AssetData item in hashSet)
		{
			database.DeleteAsset(item);
		}
	}
}
