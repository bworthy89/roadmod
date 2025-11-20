using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Collectors;
using Colossal.AssetPipeline.Diagnostic;
using Colossal.AssetPipeline.Importers;
using Colossal.AssetPipeline.PostProcessors;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game.AssetPipeline;
using Game.Prefabs;
using Game.Reflection;
using Game.Settings;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class AssetImportPanel : EditorPanelSystemBase
{
	public class PrefabFactory : IPrefabFactory
	{
		private readonly List<(PrefabBase prefab, string source)> m_RootPrefabs = new List<(PrefabBase, string)>();

		private readonly List<PrefabBase> m_CreatedPrefabs = new List<PrefabBase>();

		public IReadOnlyList<(PrefabBase prefab, string source)> rootPrefabs => m_RootPrefabs;

		public IReadOnlyList<PrefabBase> prefabs => m_CreatedPrefabs;

		public T CreatePrefab<T>(string sourcePath, string rootMeshName, int lodLevel) where T : PrefabBase
		{
			T val = LoadOrCreateAsset<T>(sourcePath, rootMeshName);
			val.name = rootMeshName;
			if (lodLevel == 0)
			{
				m_RootPrefabs.Add((val, sourcePath));
			}
			m_CreatedPrefabs.Add(val);
			return val;
		}

		private T LoadOrCreateAsset<T>(string sourcePath, string rootMeshName) where T : PrefabBase
		{
			string subPath = "StreamingData~/" + sourcePath + "/" + Path.GetFileName(rootMeshName);
			if (AssetDatabase.user.TryGetAsset(SearchFilter<PrefabAsset>.ByCondition((PrefabAsset prefabAsset) => PathCompare(subPath, prefabAsset.subPath + "/" + prefabAsset.name)), out var asset) && asset.Load() is T result)
			{
				return result;
			}
			return ScriptableObject.CreateInstance<T>();
		}

		private static bool PathCompare(string subPath1, string subPath2)
		{
			return string.Compare(Path.GetFullPath(subPath1).TrimEnd('\\'), Path.GetFullPath(subPath2).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) == 0;
		}
	}

	private string m_SelectedProjectRoot;

	private string m_SelectedDirectory;

	private bool m_ProjectRootSelected;

	private DirectoryPickerButton m_OpenProjectRootButton;

	private DirectoryPickerButton m_OpenSelectedAssetPathButton;

	private List<FileItem> m_Assets;

	private List<FileItem> m_CachedAssets;

	private FilePickerAdapter m_Adapter;

	private ItemPicker<FileItem> m_AssetList;

	private ItemPickerFooter m_ItemPickerFooter;

	private PrefabBase m_SelectedTemplate;

	private Button m_ImportButton;

	private bool m_Importing;

	private bool importing
	{
		get
		{
			return m_Importing;
		}
		set
		{
			m_Importing = value;
			m_ImportButton.displayName = (value ? "Editor.IMPORTING" : "Editor.IMPORT");
		}
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		base.activeSubPanel = null;
		m_Assets = GetAssets().ToList();
		m_Adapter = new FilePickerAdapter(m_Assets);
		FilePickerAdapter adapter = m_Adapter;
		adapter.EventItemSelected = (Action<FileItem>)Delegate.Combine(adapter.EventItemSelected, new Action<FileItem>(OnAssetSelected));
		m_OpenProjectRootButton = new DirectoryPickerButton
		{
			displayName = "Editor.PROJECT_ROOT",
			action = OpenDirectory,
			tooltip = "Editor.PROJECT_ROOT_TOOLTIP",
			uiTag = "UITagPrefab:SelectProjectRoot"
		};
		m_OpenSelectedAssetPathButton = new DirectoryPickerButton
		{
			displayName = "Editor.SELECTED_ASSETS",
			action = OpenAssetSubDirectory,
			disabled = IsSelectedAssetFolderDisabled,
			tooltip = "Editor.SELECTED_ASSETS_TOOLTIP",
			uiTag = "UITagPrefab:SelectAssets"
		};
		m_AssetList = new ItemPicker<FileItem>
		{
			adapter = m_Adapter
		};
		m_ItemPickerFooter = new ItemPickerFooter
		{
			adapter = m_Adapter
		};
		title = "Editor.TOOL[AssetImportTool]";
		IWidget[] obj = new IWidget[7]
		{
			m_OpenProjectRootButton,
			m_OpenSelectedAssetPathButton,
			m_AssetList,
			m_ItemPickerFooter,
			new PopupValueField<PrefabBase>
			{
				displayName = "Editor.SELECT_TEMPLATE",
				uiTag = "UITagPrefab:SelectTemplate",
				accessor = new DelegateAccessor<PrefabBase>(() => m_SelectedTemplate, delegate(PrefabBase prefab)
				{
					m_SelectedTemplate = prefab;
				}),
				disabled = IsImportDisabled,
				popup = new PrefabPickerPopup(typeof(ObjectGeometryPrefab))
				{
					nullable = true
				}
			},
			null,
			null
		};
		Button obj2 = new Button
		{
			displayName = "Editor.IMPORT",
			action = ImportAssets,
			disabled = IsImportDisabled,
			tooltip = "Import selected assets",
			uiTag = "UITagPrefab:ImportButton"
		};
		Button button = obj2;
		m_ImportButton = obj2;
		obj[5] = button;
		obj[6] = new ImageField
		{
			m_URI = "Media/Menu/InstaLOD-Logo-BW-WhiteOnBlack.svg",
			m_Label = "Editor.INSTALOD_LABEL"
		};
		children = obj;
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		m_SelectedProjectRoot = editorSettings?.lastSelectedProjectRootDirectory;
		m_SelectedDirectory = editorSettings?.lastSelectedImportDirectory;
		try
		{
			if (!string.IsNullOrEmpty(m_SelectedProjectRoot))
			{
				OnSelectProjectRoot(m_SelectedProjectRoot + "/");
				if (!string.IsNullOrEmpty(m_SelectedDirectory))
				{
					OnSelectDirectory(m_SelectedDirectory + "/");
				}
			}
		}
		catch (Exception exception)
		{
			base.log.Error(exception, "Exception occured while trying to select project root or import directory " + m_SelectedProjectRoot + ", " + m_SelectedDirectory);
			m_SelectedProjectRoot = string.Empty;
			m_SelectedDirectory = string.Empty;
			OnSelectProjectRoot(m_SelectedProjectRoot + "/");
			OnSelectDirectory(m_SelectedDirectory + "/");
		}
	}

	private void OnAssetSelected(FileItem item)
	{
		UnityEngine.Debug.Log("Asset selected " + item.displayName);
	}

	private void OpenDirectory()
	{
		base.activeSubPanel = new DirectoryBrowserPanel(m_SelectedProjectRoot, null, OnSelectProjectRoot, CloseDirectoryBrowser);
	}

	private void OpenAssetSubDirectory()
	{
		if (string.IsNullOrEmpty(m_SelectedDirectory) || !m_SelectedDirectory.StartsWith(m_SelectedProjectRoot))
		{
			m_SelectedDirectory = m_SelectedProjectRoot;
		}
		base.activeSubPanel = new DirectoryBrowserPanel(m_SelectedDirectory, m_SelectedProjectRoot, OnSelectDirectory, CloseDirectoryBrowser);
	}

	private void CloseDirectoryBrowser()
	{
		CloseSubPanel();
	}

	private void OnLoadAsset(Guid guid)
	{
		CloseSubPanel();
	}

	private void OnSelectProjectRoot(string directory)
	{
		CloseSubPanel();
		string text = directory.Remove(directory.Length - 1);
		if (text != m_SelectedProjectRoot)
		{
			m_SelectedDirectory = null;
			m_OpenSelectedAssetPathButton.displayValue = "";
			m_OpenSelectedAssetPathButton.tooltip = "Select asset folder";
		}
		if (text.LastIndexOf('/') != -1)
		{
			m_OpenProjectRootButton.displayValue = ".." + text.Substring(text.LastIndexOf('/')) + "/";
		}
		else
		{
			m_OpenProjectRootButton.displayValue = text;
		}
		m_OpenProjectRootButton.tooltip = text;
		m_SelectedProjectRoot = text;
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.lastSelectedProjectRootDirectory = m_SelectedProjectRoot;
			editorSettings.ApplyAndSave();
		}
		m_Assets.Clear();
		m_Adapter = new FilePickerAdapter(m_Assets);
		FilePickerAdapter adapter = m_Adapter;
		adapter.EventItemSelected = (Action<FileItem>)Delegate.Combine(adapter.EventItemSelected, new Action<FileItem>(OnAssetSelected));
		m_AssetList.adapter = m_Adapter;
		m_ItemPickerFooter.adapter = m_Adapter;
		m_ProjectRootSelected = true;
	}

	private void OnSelectDirectory(string directory)
	{
		CloseSubPanel();
		string text = directory.Remove(directory.Length - 1);
		if (text.LastIndexOf('/') != -1)
		{
			m_OpenSelectedAssetPathButton.displayValue = ".." + text.Substring(text.LastIndexOf('/')) + "/";
		}
		else
		{
			m_OpenSelectedAssetPathButton.displayValue = text;
		}
		m_OpenSelectedAssetPathButton.tooltip = text;
		m_SelectedDirectory = text;
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.lastSelectedImportDirectory = m_SelectedDirectory;
			editorSettings.ApplyAndSave();
		}
		m_Assets = GetAssets().ToList();
		m_Adapter = new FilePickerAdapter(m_Assets);
		FilePickerAdapter adapter = m_Adapter;
		adapter.EventItemSelected = (Action<FileItem>)Delegate.Combine(adapter.EventItemSelected, new Action<FileItem>(OnAssetSelected));
		m_AssetList.adapter = m_Adapter;
		m_ItemPickerFooter.adapter = m_Adapter;
	}

	private IEnumerable<FileItem> GetAssets()
	{
		if (m_SelectedDirectory == null)
		{
			yield break;
		}
		PostProcessorCache.CachePostProcessors();
		ImporterCache.CacheSupportedExtensions();
		Report report = new Report();
		IDictionary<SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset>, Colossal.AssetPipeline.Settings> dictionary = AssetImportPipeline.CollectDataToImport(m_SelectedProjectRoot, new string[1] { m_SelectedDirectory }, report);
		foreach (KeyValuePair<SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset>, Colossal.AssetPipeline.Settings> item in dictionary)
		{
			foreach (SourceAssetCollector.Asset item2 in item.Key)
			{
				yield return new FileItem
				{
					path = item2.path,
					displayName = item2.name,
					tooltip = item2.path
				};
			}
		}
	}

	private bool IsImportDisabled()
	{
		if (!importing)
		{
			return !m_Assets.Any();
		}
		return true;
	}

	private bool IsSelectedAssetFolderDisabled()
	{
		if (!string.IsNullOrEmpty(m_SelectedProjectRoot))
		{
			return !m_ProjectRootSelected;
		}
		return true;
	}

	private static bool ReportProgress(string title, string info, float progress)
	{
		if (progress == 1f)
		{
			UnityEngine.Debug.Log("Import completed");
		}
		return false;
	}

	private async void ImportAssets()
	{
		try
		{
			importing = true;
			await ImportAssets(m_SelectedProjectRoot, m_SelectedDirectory, m_SelectedTemplate, base.World, base.log);
		}
		finally
		{
			importing = false;
		}
	}

	public static async Task<PrefabFactory> ImportAssets(string selectedProjectRoot, string selectedDirectory, PrefabBase selectedTemplate, World world, ILog log)
	{
		PrefabFactory prefabFactory = null;
		try
		{
			EditorSettings editorSettings = SharedSettings.instance?.editor;
			if (selectedProjectRoot == null)
			{
				throw new Exception("The path must contains ProjectFiles to act as the root folder of the art assets");
			}
			if (AssetImportPipeline.IsArtRootPath(selectedProjectRoot, new string[1] { selectedDirectory }, out var artProjectPath, out var artProjectRelativePaths))
			{
				prefabFactory = new PrefabFactory();
				AssetImportPipeline.useParallelImport = editorSettings?.useParallelImport ?? true;
				AssetImportPipeline.targetDatabase = AssetDatabase.user;
				TextureImporter.overrideCompressionEffort = ((!(editorSettings?.lowQualityTextureCompression ?? true)) ? (-1) : 0);
				await AssetImportPipeline.ImportPath(artProjectPath, artProjectRelativePaths, ImportMode.All, convertToVT: false, ReportProgress, prefabFactory);
				PrefabSystem orCreateSystemManaged = world.GetOrCreateSystemManaged<PrefabSystem>();
				ToolSystem orCreateSystemManaged2 = world.GetOrCreateSystemManaged<ToolSystem>();
				foreach (var rootPrefab in prefabFactory.rootPrefabs)
				{
					log.InfoFormat("Root prefab: {0} ({1})", rootPrefab.prefab.name, rootPrefab.source);
					string fileName = Path.GetFileName(rootPrefab.source);
					string subPath = "StreamingData~/" + fileName;
					if (selectedTemplate != null)
					{
						PrefabBase prefabBase = selectedTemplate.Clone(fileName);
						if (prefabBase is ObjectGeometryPrefab objectGeometryPrefab)
						{
							objectGeometryPrefab.m_Meshes = new ObjectMeshInfo[1]
							{
								new ObjectMeshInfo
								{
									m_Mesh = (rootPrefab.prefab as RenderPrefabBase)
								}
							};
						}
						prefabBase.Remove<ObjectSubObjects>();
						prefabBase.Remove<ObjectSubAreas>();
						prefabBase.Remove<ObjectSubLanes>();
						prefabBase.Remove<ObjectSubNets>();
						prefabBase.Remove<NetSubObjects>();
						prefabBase.Remove<AreaSubObjects>();
						prefabBase.Remove<EffectSource>();
						prefabBase.Remove<ObsoleteIdentifiers>();
						AssetImportPipeline.targetDatabase.AddAsset(AssetDataPath.Create(subPath, prefabBase.name ?? ""), prefabBase).Save();
						orCreateSystemManaged.AddPrefab(prefabBase);
						orCreateSystemManaged2.ActivatePrefabTool(prefabBase);
					}
					else
					{
						if (rootPrefab.prefab.asset == null)
						{
							AssetImportPipeline.targetDatabase.AddAsset(AssetDataPath.Create(subPath, rootPrefab.prefab.name ?? ""), rootPrefab.prefab);
						}
						rootPrefab.prefab.asset.Save(force: true);
						orCreateSystemManaged.AddPrefab(rootPrefab.prefab);
					}
				}
			}
			else
			{
				log.Error("The path must contains ProjectFiles to act as the root folder of the art assets");
			}
		}
		catch (Exception exception)
		{
			log.Error(exception);
		}
		return prefabFactory;
	}

	[Preserve]
	public AssetImportPanel()
	{
	}
}
