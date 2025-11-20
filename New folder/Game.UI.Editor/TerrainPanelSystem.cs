using System;
using System.Collections.Generic;
using System.IO;
using Colossal;
using Colossal.AssetPipeline.Native;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Game.Common;
using Game.Prefabs;
using Game.Reflection;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class TerrainPanelSystem : EditorPanelSystemBase
{
	private struct UIPriorityComparer : IComparer<PrefabData>
	{
		private PrefabSystem m_PrefabSystem;

		public UIPriorityComparer(PrefabSystem prefabSystem)
		{
			m_PrefabSystem = prefabSystem;
		}

		public int Compare(PrefabData a, PrefabData b)
		{
			TerraformingPrefab prefab = m_PrefabSystem.GetPrefab<TerraformingPrefab>(a);
			PrefabBase prefab2 = m_PrefabSystem.GetPrefab<TerraformingPrefab>(b);
			if (prefab.TryGet<UIObject>(out var component) && prefab2.TryGet<UIObject>(out var component2))
			{
				return component.m_Priority - component2.m_Priority;
			}
			return -1;
		}
	}

	private static readonly string kHeightmapFolder = "Heightmaps";

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_PrefabQuery;

	private IconButtonGroup m_ToolButtonGroup;

	private IconButtonGroup m_MaterialButtonGroup = new IconButtonGroup();

	private List<PrefabBase> m_ToolPrefabs = new List<PrefabBase>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<TerraformingData>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		title = "Editor.TOOL[TerrainTool]";
		children = new IWidget[1] { Scrollable.WithChildren(new IWidget[2]
		{
			new EditorSection
			{
				displayName = "Editor.TERRAIN_TOOLS",
				uiTag = "UITagPrefab:TerrainTools",
				tooltip = "Editor.TERRAIN_TOOLS_TOOLTIP",
				expanded = true,
				children = new IWidget[1] { m_ToolButtonGroup = new IconButtonGroup() }
			},
			new EditorSection
			{
				displayName = "Editor.HEIGHTMAPS",
				tooltip = "Editor.HEIGHTMAPS_TOOLTIP",
				uiTag = "UITagPrefab:Heightmaps",
				expanded = true,
				children = new IWidget[3]
				{
					new FloatInputField
					{
						displayName = "Editor.HEIGHT_SCALE",
						tooltip = "Editor.HEIGHT_SCALE_TOOLTIP",
						uiTag = "UITagPrefab:HeightScale",
						accessor = new DelegateAccessor<double>(() => m_TerrainSystem.heightScaleOffset.x, delegate(double val)
						{
							float2 heightScaleOffset = m_TerrainSystem.heightScaleOffset;
							heightScaleOffset.x = (float)val;
							RefreshTerrainProperties(heightScaleOffset);
						}),
						min = 200.0,
						max = 10000.0
					},
					Column.WithChildren(new Button[2]
					{
						new Button
						{
							displayName = "Editor.IMPORT_HEIGHTMAP",
							tooltip = "Editor.IMPORT_HEIGHTMAP_TOOLTIP",
							uiTag = "UITagPrefab:ImportHeightmap",
							action = ShowImportHeightmapPanel
						},
						new Button
						{
							displayName = "Editor.EXPORT_HEIGHTMAP",
							uiTag = "UITagPrefab:ExportHeightmap",
							tooltip = "Editor.EXPORT_HEIGHTMAP_TOOLTIP",
							action = ShowExportHeightmapPanel
						}
					}),
					Column.WithChildren(new Button[3]
					{
						new Button
						{
							displayName = "Editor.IMPORT_WORLDMAP",
							tooltip = "Editor.IMPORT_WORLDMAP_TOOLTIP",
							uiTag = "UITagPrefab:ImportWorldmap",
							action = ShowImportWorldmapPanel
						},
						new Button
						{
							displayName = "Editor.EXPORT_WORLDMAP",
							tooltip = "Editor.EXPORT_WORLDMAP_TOOLTIP",
							action = ShowExportWorldmapPanel,
							disabled = () => m_TerrainSystem.worldHeightmap == null
						},
						new Button
						{
							displayName = "Editor.REMOVE_WORLDMAP",
							tooltip = "Editor.REMOVE_WORLDMAP_TOOLTIP",
							action = RemoveWorldmap,
							disabled = () => m_TerrainSystem.worldHeightmap == null
						}
					})
				}
			}
		}) };
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		List<IconButton> list = new List<IconButton>();
		List<IconButton> list2 = new List<IconButton>();
		NativeArray<PrefabData> array = m_PrefabQuery.ToComponentDataArray<PrefabData>(Allocator.Temp);
		array.Sort(new UIPriorityComparer(m_PrefabSystem));
		m_ToolPrefabs.Clear();
		foreach (PrefabData item in array)
		{
			TerraformingPrefab prefab = m_PrefabSystem.GetPrefab<TerraformingPrefab>(item);
			if (prefab.m_Target == TerraformingTarget.Height)
			{
				m_ToolPrefabs.Add(prefab);
				list.Add(new IconButton
				{
					icon = (ImageSystem.GetIcon(prefab) ?? "Media/Editor/Terrain.svg"),
					tooltip = LocalizedString.Id("Assets.NAME[" + prefab.name + "]"),
					action = delegate
					{
						m_ToolSystem.ActivatePrefabTool((m_ToolSystem.activePrefab != prefab) ? prefab : null);
					},
					selected = () => m_ToolSystem.activePrefab == prefab
				});
			}
			if (prefab.m_Target == TerraformingTarget.Material)
			{
				m_ToolPrefabs.Add(prefab);
				list2.Add(new IconButton
				{
					icon = (ImageSystem.GetIcon(prefab) ?? "Media/Editor/Material.svg"),
					tooltip = LocalizedString.Id("Assets.NAME[" + prefab.name + "]"),
					action = delegate
					{
						m_ToolSystem.ActivatePrefabTool((m_ToolSystem.activePrefab != prefab) ? prefab : null);
					},
					selected = () => m_ToolSystem.activePrefab == prefab
				});
			}
		}
		array.Dispose();
		m_ToolButtonGroup.children = list.ToArray();
		m_MaterialButtonGroup.children = list2.ToArray();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		base.activeSubPanel = null;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		if (m_ToolPrefabs.Contains(m_ToolSystem.activePrefab))
		{
			m_ToolSystem.ActivatePrefabTool(null);
		}
		base.OnStopRunning();
	}

	protected override bool OnCancel()
	{
		if (m_ToolPrefabs.Contains(m_ToolSystem.activePrefab))
		{
			m_ToolSystem.ActivatePrefabTool(null);
			return false;
		}
		return base.OnCancel();
	}

	private void ShowImportHeightmapPanel()
	{
		base.activeSubPanel = new LoadAssetPanel("Editor.IMPORT_HEIGHTMAP", GetHeightmaps(), OnLoadHeightmap, base.CloseSubPanel);
	}

	private void ShowExportHeightmapPanel()
	{
		base.activeSubPanel = new SaveAssetPanel("Editor.EXPORT_HEIGHTMAP", GetHeightmaps(), null, OnSaveHeightmap, base.CloseSubPanel);
	}

	private void ShowImportWorldmapPanel()
	{
		base.activeSubPanel = new LoadAssetPanel("Editor.IMPORT_WORLDMAP", GetHeightmaps(), OnLoadWorldHeightmap, base.CloseSubPanel);
	}

	private void ShowExportWorldmapPanel()
	{
		base.activeSubPanel = new SaveAssetPanel("Editor.EXPORT_WORLDMAP", GetHeightmaps(), null, OnSaveWorldHeightmap, base.CloseSubPanel);
	}

	private void RemoveWorldmap()
	{
		m_TerrainSystem.ReplaceWorldHeightmap(null);
	}

	private void RefreshTerrainProperties(float2 heightScaleOffset)
	{
		m_TerrainSystem.SetTerrainProperties(heightScaleOffset);
	}

	private static IEnumerable<AssetItem> GetHeightmaps()
	{
		foreach (ImageAsset asset in AssetDatabase.global.GetAssets(SearchFilter<ImageAsset>.ByCondition((ImageAsset a) => a.GetMeta().subPath?.StartsWith(kHeightmapFolder) ?? false)))
		{
			yield return new AssetItem
			{
				guid = asset.id,
				fileName = asset.name,
				displayName = asset.name,
				image = asset.ToUri(),
				tooltip = asset.name
			};
		}
	}

	private void OnLoadHeightmap(Colossal.Hash128 guid)
	{
		CloseSubPanel();
		if (!AssetDatabase.global.TryGetAsset(guid, out ImageAsset asset))
		{
			return;
		}
		using (asset)
		{
			Texture2D texture2D = asset.Load(srgb: true);
			if (!TerrainSystem.IsValidHeightmapFormat(texture2D))
			{
				DisplayHeightmapError();
			}
			else
			{
				m_TerrainSystem.ReplaceHeightmap(texture2D);
			}
		}
	}

	private void OnLoadWorldHeightmap(Colossal.Hash128 guid)
	{
		CloseSubPanel();
		if (!AssetDatabase.global.TryGetAsset(guid, out ImageAsset asset))
		{
			return;
		}
		using (asset)
		{
			Texture2D texture2D = asset.Load(srgb: true);
			if (!TerrainSystem.IsValidHeightmapFormat(texture2D))
			{
				DisplayHeightmapError();
			}
			else
			{
				m_TerrainSystem.ReplaceWorldHeightmap(texture2D);
			}
		}
	}

	private void DisplayHeightmapError()
	{
		AppBindings appBindings = GameManager.instance.userInterface.appBindings;
		LocalizedString? localizedString = LocalizedString.Id("Editor.INCORRECT_HEIGHTMAP_TITLE");
		Dictionary<string, ILocElement> dictionary = new Dictionary<string, ILocElement>();
		int kDefaultHeightmapWidth = TerrainSystem.kDefaultHeightmapWidth;
		dictionary.Add("WIDTH", LocalizedString.Value(kDefaultHeightmapWidth.ToString()));
		kDefaultHeightmapWidth = TerrainSystem.kDefaultHeightmapHeight;
		dictionary.Add("HEIGHT", LocalizedString.Value(kDefaultHeightmapWidth.ToString()));
		appBindings.ShowMessageDialog(new MessageDialog(localizedString, new LocalizedString("Editor.INCORRECT_HEIGHTMAP_MESSAGE", null, dictionary), LocalizedString.Id("Common.ERROR_ACTION[Continue]")), null);
	}

	private void OnSaveHeightmap(string fileName, Colossal.Hash128? overwriteGuid)
	{
		OnSaveHeightmap(fileName, overwriteGuid, worldMap: false);
	}

	private void OnSaveWorldHeightmap(string fileName, Colossal.Hash128? overwriteGuid)
	{
		OnSaveHeightmap(fileName, overwriteGuid, worldMap: true);
	}

	private unsafe void OnSaveHeightmap(string fileName, Colossal.Hash128? overwriteGuid, bool worldMap)
	{
		CloseSubPanel();
		bool flag = false;
		if (overwriteGuid.HasValue && AssetDatabase.user.TryGetAsset(overwriteGuid.Value, out ImageAsset assetData))
		{
			AssetDatabase.user.DeleteAsset(assetData);
			flag = true;
		}
		Texture texture = (worldMap ? m_TerrainSystem.worldHeightmap : m_TerrainSystem.heightmap);
		NativeArray<ushort> output = new NativeArray<ushort>(texture.width * texture.height, Allocator.Persistent);
		AsyncGPUReadback.RequestIntoNativeArray(ref output, texture).WaitForCompletion();
		try
		{
			byte[] array = TextureUtilities.SaveImage((IntPtr)output.GetUnsafeReadOnlyPtr(), output.Length * 2, texture.width, texture.height, 1, 16, NativeTextures.ImageFileFormat.PNG);
			AssetDataPath name = AssetDataPath.Create(kHeightmapFolder, fileName);
			using Stream stream = AssetDatabase.user.AddAsset<ImageAsset>(name, flag ? overwriteGuid.Value : default(Colossal.Hash128)).GetWriteStream();
			stream.Write(array, 0, array.Length);
		}
		finally
		{
			output.Dispose();
		}
	}

	[Preserve]
	public TerrainPanelSystem()
	{
	}
}
