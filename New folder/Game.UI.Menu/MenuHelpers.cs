using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.Assets;
using Game.SceneFlow;

namespace Game.UI.Menu;

public static class MenuHelpers
{
	public class SaveGamePreviewSettings
	{
		public bool stylized { get; set; }

		public float stylizedRadius { get; set; }

		public TextureAsset overlayImage { get; set; }

		public SaveGamePreviewSettings()
		{
			SetDefaults();
		}

		public void SetDefaults()
		{
			stylized = false;
			stylizedRadius = 0f;
			overlayImage = null;
		}

		public void FromUri(UrlQuery query)
		{
			if (query.Read("stylized", out bool result))
			{
				stylized = result;
			}
			if (query.Read("stylizedRadius", out float result2))
			{
				stylizedRadius = result2;
			}
			if (query.ReadAsset<TextureAsset>("overlayImage", out var result3))
			{
				overlayImage = result3;
			}
		}

		public string ToUri()
		{
			return $"stylized={stylized}&stylizedRadius={stylizedRadius}&overlayImage={overlayImage?.id.guid}";
		}
	}

	private static ILog log = LogManager.GetLogger("SceneFlow");

	public const int kPreviewWidth = 680;

	public const int kPreviewHeight = 383;

	public static TextureAsset defaultPreview => AssetDatabase.game.GetAsset<TextureAsset>("cc1e5421d5a16f15bbd580cffdbee7d4");

	public static TextureAsset defaultThumbnail => AssetDatabase.game.GetAsset<TextureAsset>("735aa687f0dd7cda5e7d1aa4c4987b26");

	public static bool hasPreviouslySavedGame => GameManager.instance.settings.userState.lastSaveGameMetadata?.isValidSaveGame ?? false;

	public static SaveGameMetadata GetLastModifiedSave()
	{
		SaveGameMetadata result = null;
		DateTime dateTime = DateTime.MinValue;
		foreach (SaveGameMetadata asset in AssetDatabase.global.GetAssets(default(SearchFilter<SaveGameMetadata>)))
		{
			DateTime lastModified = asset.target.lastModified;
			if (lastModified > dateTime)
			{
				dateTime = lastModified;
				result = asset;
			}
		}
		return result;
	}

	public static void UpdateMeta<T>(ValueBinding<List<T>> binding, Func<Metadata<T>, bool> filter = null) where T : IContentPrerequisite
	{
		List<T> value = binding.value;
		value.Clear();
		foreach (Metadata<T> asset in AssetDatabase.global.GetAssets(default(SearchFilter<Metadata<T>>)))
		{
			try
			{
				if (filter == null || filter(asset))
				{
					value.Add(asset.target);
				}
			}
			catch (Exception exception)
			{
				log.WarnFormat(exception, "An error occured while updating {0}", asset);
			}
		}
		binding.TriggerUpdate();
	}

	public static List<string> GetAvailableCloudTargets()
	{
		return (from x in AssetDatabase.global.GetAvailableRemoteStorages()
			select x.name).ToList();
	}

	public static (string name, ILocalAssetDatabase db) GetSanitizedCloudTarget(string cloudTarget)
	{
		(string, ILocalAssetDatabase) result = default((string, ILocalAssetDatabase));
		foreach (var availableRemoteStorage in AssetDatabase.global.GetAvailableRemoteStorages())
		{
			if (availableRemoteStorage.name == cloudTarget)
			{
				return availableRemoteStorage;
			}
			if (availableRemoteStorage.name == "Local")
			{
				result = availableRemoteStorage;
			}
		}
		return result;
	}
}
