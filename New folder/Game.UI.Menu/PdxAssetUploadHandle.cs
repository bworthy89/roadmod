using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Colossal.AssetPipeline.Diagnostic;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Game.AssetPipeline;
using Game.Assets;
using Game.PSI;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Entities;

namespace Game.UI.Menu;

public class PdxAssetUploadHandle
{
	private ILog log = LogManager.GetLogger("AssetUpload");

	private PdxSdkPlatform m_Manager = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");

	private List<AssetData> m_Screenshots = new List<AssetData>();

	private List<AssetData> m_Assets = new List<AssetData>();

	private List<AssetData> m_OriginalPreviews = new List<AssetData>();

	private Dictionary<AssetData, AssetData> m_WIPAssets = new Dictionary<AssetData, AssetData>();

	private List<AssetData> m_AdditionalAssets = new List<AssetData>();

	private HashSet<AssetData> m_CachedAssetDependencies = new HashSet<AssetData>();

	public Action onSocialProfileSynced;

	public AssetData mainAsset { get; private set; }

	public IReadOnlyList<AssetData> assets => m_Assets;

	public IReadOnlyList<AssetData> additionalAssets => m_AdditionalAssets;

	public HashSet<AssetData> cachedDependencies => m_CachedAssetDependencies;

	public bool hasPrefabAssets { get; private set; }

	public IEnumerable<AssetData> allAssets
	{
		get
		{
			foreach (AssetData asset in assets)
			{
				yield return asset;
			}
			foreach (AssetData additionalAsset in additionalAssets)
			{
				yield return additionalAsset;
			}
		}
	}

	public IReadOnlyList<AssetData> screenshots => m_Screenshots;

	public AssetData preview { get; private set; }

	public IReadOnlyList<AssetData> originalPreviews => m_OriginalPreviews;

	public IModsUploadSupport.ModInfo modInfo { get; set; }

	public bool updateExisting { get; set; }

	public int processVT { get; set; } = -1;

	public bool packThumbnailsAtlas { get; set; }

	public List<IModsUploadSupport.ModInfo> authorMods { get; private set; } = new List<IModsUploadSupport.ModInfo>();

	public IModsUploadSupport.ModTag[] availableTags { get; private set; } = Array.Empty<IModsUploadSupport.ModTag>();

	public IModsUploadSupport.DLCTag[] availableDLCs { get; private set; } = Array.Empty<IModsUploadSupport.DLCTag>();

	public HashSet<string> typeTags { get; private set; } = new HashSet<string>();

	public HashSet<string> tags { get; private set; } = new HashSet<string>();

	public List<string> additionalTags { get; private set; } = new List<string>();

	public int tagCount => tags.Count + additionalTags.Count;

	public bool binaryPackAssets { get; set; } = true;

	public IModsUploadSupport.SocialProfile socialProfile { get; private set; }

	public bool LoggedIn()
	{
		return m_Manager?.cachedLoggedIn ?? false;
	}

	public PdxAssetUploadHandle()
	{
		Initialize();
	}

	public PdxAssetUploadHandle(AssetData mainAsset, params AssetData[] assets)
	{
		this.mainAsset = mainAsset;
		if (mainAsset != null)
		{
			m_Assets.Add(mainAsset);
		}
		m_Assets.AddRange(assets);
		Initialize();
	}

	private void Initialize()
	{
		m_Manager = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
		PlatformManager.instance.onPlatformRegistered += delegate(IPlatformServiceIntegration psi)
		{
			if (psi is PdxSdkPlatform manager)
			{
				m_Manager = manager;
			}
		};
		InitializePreviews();
		IModsUploadSupport.ModInfo modInfo = this.modInfo;
		modInfo.Clear();
		modInfo.m_RecommendedGameVersion = $"{Version.current.majorVersion}.{Version.current.minorVersion}.*";
		modInfo.m_DisplayName = mainAsset?.name;
		modInfo.m_ExternalLinks.Add(new IModsUploadSupport.ExternalLinkData
		{
			m_Type = IModsUploadSupport.ExternalLinkInfo.kAcceptedTypes[0].m_Type,
			m_URL = string.Empty
		});
		this.modInfo = modInfo;
		RebuildDependencyCache();
	}

	public async Task<IModsUploadSupport.ModOperationResult> BeginSubmit()
	{
		IModsUploadSupport.ModOperationResult result = await m_Manager.GetNewModUploadFolder(this.modInfo);
		if (!result.m_Success)
		{
			return result;
		}
		this.modInfo = result.m_ModInfo;
		HashSet<IModsUploadSupport.ModInfo.ModDependency> hashSet = new HashSet<IModsUploadSupport.ModInfo.ModDependency>();
		var (flag, text) = CopyFiles(hashSet);
		if (!flag)
		{
			log.Error(text);
			Cleanup();
			return new IModsUploadSupport.ModOperationResult
			{
				m_ModInfo = this.modInfo,
				m_Success = false,
				m_Error = new IModsUploadSupport.ModError
				{
					m_Details = text
				}
			};
		}
		IModsUploadSupport.ModInfo modInfo = this.modInfo;
		modInfo.m_ModDependencies = hashSet.ToArray();
		modInfo.m_Tags = CollectTags();
		this.modInfo = modInfo;
		return new IModsUploadSupport.ModOperationResult
		{
			m_Error = default(IModsUploadSupport.ModError),
			m_ModInfo = this.modInfo,
			m_Success = true
		};
	}

	public async Task<IModsUploadSupport.ModOperationResult> FinalizeSubmit()
	{
		IModsUploadSupport.ModOperationResult modOperationResult = ((!updateExisting) ? (await m_Manager.Publish(modInfo)) : (await m_Manager.UpdateExisting(modInfo)));
		IModsUploadSupport.ModOperationResult result = modOperationResult;
		modInfo = result.m_ModInfo;
		Cleanup();
		return result;
	}

	public void ShowModsUIProfilePage()
	{
		m_Manager.onModsUIClosed += OnModsUIClosed;
		m_Manager.ShowModsUIProfilePage();
	}

	private void OnModsUIClosed()
	{
		m_Manager.onModsUIClosed -= OnModsUIClosed;
		RefreshSocialProfile();
	}

	private async void RefreshSocialProfile()
	{
		IModsUploadSupport.SocialProfile socialProfileResult = await m_Manager.GetSocialProfile();
		GameManager.instance.RunOnMainThread(delegate
		{
			socialProfile = socialProfileResult;
			onSocialProfileSynced?.Invoke();
		});
	}

	public void ExcludeSourceTextures(IEnumerable<SurfaceAsset> surfaces, ILocalAssetDatabase database)
	{
		Dictionary<TextureAsset, List<SurfaceAsset>> dictionary = new Dictionary<TextureAsset, List<SurfaceAsset>>();
		Dictionary<TextureAsset, List<SurfaceAsset>> dictionary2 = new Dictionary<TextureAsset, List<SurfaceAsset>>();
		foreach (SurfaceAsset surface in surfaces)
		{
			surface.LoadProperties(useVT: true);
			if (surface.isVTMaterial)
			{
				foreach (KeyValuePair<string, TextureAsset> texture in surface.textures)
				{
					if (surface.IsHandledByVirtualTexturing(texture))
					{
						AddReferenceTo(dictionary, texture.Value, surface);
					}
					else
					{
						AddReferenceTo(dictionary2, texture.Value, surface);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<string, TextureAsset> texture2 in surface.textures)
				{
					AddReferenceTo(dictionary2, texture2.Value, surface);
				}
			}
			surface.Unload();
		}
		List<TextureAsset> list = database.GetAssets(default(SearchFilter<TextureAsset>)).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			TextureAsset textureAsset = list[i];
			if (dictionary.ContainsKey(textureAsset))
			{
				if (dictionary2.ContainsKey(textureAsset))
				{
					log.WarnFormat("Texture {0} is referenced {1} times by VT materials and {2} times by non VT materials. It will be duplicated on disk.", textureAsset, dictionary[textureAsset].Count, dictionary2[textureAsset].Count);
					log.InfoFormat("Detail for {0}:\nvt: {1}\nnon vt: {2}", textureAsset, string.Join(", ", dictionary[textureAsset]), string.Join(", ", dictionary2[textureAsset]));
				}
				else
				{
					log.InfoFormat($"Deleting {textureAsset}");
					textureAsset.Delete();
				}
			}
		}
		static void AddReferenceTo(Dictionary<TextureAsset, List<SurfaceAsset>> references, TextureAsset texture, SurfaceAsset surface)
		{
			if (!references.TryGetValue(texture, out var value))
			{
				value = new List<SurfaceAsset>();
				references.Add(texture, value);
			}
			value.Add(surface);
		}
	}

	private (bool, string) CopyFiles(HashSet<IModsUploadSupport.ModInfo.ModDependency> externalReferences)
	{
		string absoluteContentPath = GetAbsoluteContentPath();
		if (!LongDirectory.Exists(absoluteContentPath))
		{
			LongDirectory.CreateDirectory(absoluteContentPath);
		}
		if (allAssets.Any())
		{
			using ILocalAssetDatabase localAssetDatabase = AssetDatabase.GetTransient(0L);
			Dictionary<AssetData, AssetData> dictionary = new Dictionary<AssetData, AssetData>();
			foreach (AssetData allAsset in allAssets)
			{
				log.VerboseFormat("Copying {0} to {1}. Processed {2} references.", allAsset, localAssetDatabase, dictionary.Count);
				AssetUploadUtils.CopyAsset(allAsset, localAssetDatabase, dictionary, externalReferences, allAsset == mainAsset, binaryPackAssets, modInfo.m_PublishedID);
			}
			if (processVT > -1)
			{
				SimulationSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystem>();
				float selectedSpeed = existingSystemManaged.selectedSpeed;
				int mipBias = processVT;
				Report report = new Report();
				Report.ImportStep report2 = report.AddImportStep("Convert Selected VT");
				List<SurfaceAsset> list = localAssetDatabase.GetAssets(default(SearchFilter<SurfaceAsset>)).ToList();
				AssetImportPipeline.ConvertSurfacesToVT(list, list, writeVTSettings: false, 512, 3, mipBias, force: false, report2);
				AssetImportPipeline.BuildMidMipsCache(list, 512, 3, localAssetDatabase);
				ExcludeSourceTextures(list, localAssetDatabase);
				report.Log(log, Severity.Verbose);
				existingSystemManaged.selectedSpeed = selectedSpeed;
			}
			if (packThumbnailsAtlas)
			{
				AssetUploadUtils.CreateThumbnailAtlas(dictionary, localAssetDatabase);
			}
			PackageAsset packageAsset = AssetDatabase<ParadoxMods>.instance.AddAsset(AssetDataPath.Create(modInfo.GetContentPath(), modInfo.m_DisplayName), localAssetDatabase);
			packageAsset.Save();
			m_WIPAssets.Add(packageAsset, packageAsset);
		}
		Dictionary<AssetData, string> processed = new Dictionary<AssetData, string>();
		CopyPreview(processed);
		for (int i = 0; i < screenshots.Count; i++)
		{
			CopyScreenshot(screenshots[i], i, processed);
		}
		return (true, null);
	}

	public void Cleanup()
	{
		foreach (KeyValuePair<AssetData, AssetData> wIPAsset in m_WIPAssets)
		{
			AssetDatabase<ParadoxMods>.instance.DeleteAsset(wIPAsset.Value);
		}
		string absoluteContentPath = GetAbsoluteContentPath();
		if (LongDirectory.Exists(absoluteContentPath))
		{
			LongDirectory.Delete(absoluteContentPath, recursive: true);
		}
		m_WIPAssets.Clear();
	}

	public async Task SyncPlatformData()
	{
		Task<List<IModsUploadSupport.ModInfo>> modsTask = m_Manager.ListAllModsByMe(typeTags.ToArray());
		Task<(IModsUploadSupport.ModTag[], IModsUploadSupport.DLCTag[])> tagsTask = m_Manager.GetTags();
		Task<IModsUploadSupport.SocialProfile> socialProfileTask = m_Manager.GetSocialProfile();
		await Task.WhenAll(modsTask, tagsTask, socialProfileTask);
		GameManager.instance.RunOnMainThread(delegate
		{
			authorMods = modsTask.Result;
			authorMods?.Sort((IModsUploadSupport.ModInfo a, IModsUploadSupport.ModInfo b) => string.Compare(a.m_DisplayName, b.m_DisplayName, StringComparison.OrdinalIgnoreCase));
			availableTags = tagsTask.Result.Item1;
			availableDLCs = tagsTask.Result.Item2;
			socialProfile = socialProfileTask.Result;
			HashSet<string> validTags = new HashSet<string>(availableTags.Select((IModsUploadSupport.ModTag tag) => tag.m_Id));
			(HashSet<string>, HashSet<string>) tuple = GetTags(mainAsset, validTags);
			(tags, _) = tuple;
			HashSet<string> hashSet = (typeTags = tuple.Item2);
		});
	}

	public async Task<IModsUploadSupport.ModInfo> GetExistingInfo()
	{
		return await m_Manager.GetDetails(modInfo);
	}

	public async Task<(bool, IModsUploadSupport.ModLocalData)> GetLocalData(int id)
	{
		var (success, localData) = await m_Manager.GetLocalData(id);
		if (success && AssetDatabase<ParadoxMods>.instance.dataSource is ParadoxModsDataSource paradoxModsDataSource)
		{
			await paradoxModsDataSource.PopulateMetadata(localData.m_AbsolutePath);
		}
		return (success, localData);
	}

	private void CopyPreview(Dictionary<AssetData, string> processed)
	{
		if (!(preview == null))
		{
			string text = CopyMetadata(preview, "preview", processed);
			if (text != null)
			{
				IModsUploadSupport.ModInfo modInfo = this.modInfo;
				modInfo.m_ThumbnailFilename = text;
				this.modInfo = modInfo;
			}
		}
	}

	private string CopyMetadata(AssetData asset, string name, Dictionary<AssetData, string> processed)
	{
		if (processed.TryGetValue(asset, out var value))
		{
			return value;
		}
		using (ILocalAssetDatabase database = AssetDatabase.GetTransient(0L))
		{
			try
			{
				AssetData assetData = AssetUploadUtils.CopyPreviewImage(asset, database, name);
				string filename = GetFilename(assetData);
				string absoluteMetadataPath = GetAbsoluteMetadataPath();
				if (!LongDirectory.Exists(absoluteMetadataPath))
				{
					LongDirectory.CreateDirectory(absoluteMetadataPath);
				}
				value = Path.Combine(absoluteMetadataPath, filename).Replace("\\", "/");
				using FileStream destination = LongFile.Create(value);
				using Stream stream = assetData.GetReadStream();
				stream.CopyTo(destination);
			}
			catch (Exception exception)
			{
				log.Error(exception);
				return null;
			}
		}
		processed[asset] = value;
		return value;
	}

	public void AddScreenshot(AssetData asset)
	{
		m_Screenshots.Add(asset);
	}

	public void RemoveScreenshot(AssetData asset)
	{
		m_Screenshots.Remove(asset);
	}

	public void ClearScreenshots()
	{
		m_Screenshots.Clear();
	}

	public void SetPreview(AssetData asset)
	{
		preview = asset;
	}

	private void CopyScreenshot(AssetData asset, int index, Dictionary<AssetData, string> processed)
	{
		string text = CopyMetadata(asset, $"screenshot{index}", processed);
		if (text != null)
		{
			IModsUploadSupport.ModInfo modInfo = this.modInfo;
			if (!modInfo.m_ScreenshotFileNames.Contains(text))
			{
				modInfo.m_ScreenshotFileNames.Add(text);
			}
			this.modInfo = modInfo;
		}
	}

	public void SetPreviewsFromExisting(IModsUploadSupport.ModLocalData localData)
	{
		if (localData.m_ThumbnailFilename != null)
		{
			string thumbnailPath = Path.GetFullPath(Path.Combine(localData.m_AbsolutePath, localData.m_ThumbnailFilename));
			if (AssetDatabase<ParadoxMods>.instance.TryGetAsset(SearchFilter<ImageAsset>.ByCondition((ImageAsset candidate) => FindByPath(candidate, thumbnailPath)), out var asset))
			{
				SetPreview(asset);
			}
		}
		if (localData.m_ScreenshotFilenames == null)
		{
			return;
		}
		ClearScreenshots();
		string[] screenshotFilenames = localData.m_ScreenshotFilenames;
		foreach (string path in screenshotFilenames)
		{
			string screenshotPath = Path.GetFullPath(Path.Combine(localData.m_AbsolutePath, path));
			if (AssetDatabase<ParadoxMods>.instance.TryGetAsset(SearchFilter<ImageAsset>.ByCondition((ImageAsset candidate) => FindByPath(candidate, screenshotPath)), out var asset2))
			{
				AddScreenshot(asset2);
			}
		}
	}

	public void AddAdditionalAsset(AssetData asset)
	{
		m_AdditionalAssets.Add(asset);
		RebuildDependencyCache();
	}

	public void RemoveAdditionalAsset(AssetData asset)
	{
		m_AdditionalAssets.Remove(asset);
		RebuildDependencyCache();
	}

	private bool FindByPath(ImageAsset candidate, string imagePath)
	{
		string fullPath = Path.GetFullPath(candidate.GetMeta().path);
		return imagePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase);
	}

	private static (HashSet<string>, HashSet<string>) GetTags(AssetData asset, HashSet<string> validTags)
	{
		HashSet<string> item = new HashSet<string>();
		HashSet<string> item2 = new HashSet<string>();
		if (asset != null)
		{
			ModTags.GetTags(asset, item2, item, validTags);
		}
		return (item2, item);
	}

	private void InitializePreviews()
	{
		if (AssetUploadUtils.TryGetPreview(mainAsset, out var result))
		{
			preview = result;
		}
		else
		{
			preview = MenuHelpers.defaultPreview;
		}
		HashSet<AssetData> hashSet = new HashSet<AssetData>();
		foreach (AssetData asset in assets)
		{
			if (AssetUploadUtils.TryGetPreview(asset, out var result2))
			{
				hashSet.Add(result2);
			}
		}
		m_OriginalPreviews.AddRange(hashSet);
		m_Screenshots.AddRange(hashSet);
	}

	private void InitializeContentPrerequisite()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (AssetData asset in assets)
		{
			if (asset is MapMetadata mapMetadata && mapMetadata.target.contentPrerequisites != null)
			{
				string[] contentPrerequisites = mapMetadata.target.contentPrerequisites;
				foreach (string item in contentPrerequisites)
				{
					hashSet.Add(item);
				}
			}
			if (asset is SaveGameMetadata saveGameMetadata && saveGameMetadata.target.contentPrerequisites != null)
			{
				string[] contentPrerequisites = saveGameMetadata.target.contentPrerequisites;
				foreach (string item2 in contentPrerequisites)
				{
					hashSet.Add(item2);
				}
			}
		}
		IModsUploadSupport.ModInfo modInfo = this.modInfo;
		modInfo.m_DLCDependencies = hashSet.ToArray();
		this.modInfo = modInfo;
	}

	private string GetFilename(AssetData asset)
	{
		SourceMeta meta = asset.GetMeta();
		return meta.fileName + meta.extension;
	}

	public void AddAdditionalTag(string tag)
	{
		additionalTags.Add(tag);
	}

	public void RemoveAdditionalTag(string tag)
	{
		additionalTags.Remove(tag);
	}

	private string[] CollectTags()
	{
		HashSet<string> hashSet = new HashSet<string>(tags);
		foreach (string additionalTag in additionalTags)
		{
			hashSet.Add(additionalTag);
		}
		return hashSet.ToArray();
	}

	public string GetAbsoluteContentPath()
	{
		return Path.Combine(m_Manager.modsRootPath, modInfo.GetContentPath()).Replace("\\", "/");
	}

	private string GetAbsoluteMetadataPath()
	{
		return Path.Combine(m_Manager.modsRootPath, modInfo.GetMetadataPath()).Replace("\\", "/");
	}

	private void RebuildDependencyCache()
	{
		m_CachedAssetDependencies.Clear();
		hasPrefabAssets = false;
		foreach (AssetData allAsset in allAssets)
		{
			if (allAsset is PrefabAsset prefabAsset)
			{
				AssetUploadUtils.CollectPrefabAssetDependencies(prefabAsset, m_CachedAssetDependencies, allAsset == mainAsset);
				hasPrefabAssets = true;
			}
			m_CachedAssetDependencies.Add(allAsset);
		}
		InitializeContentPrerequisite();
	}
}
