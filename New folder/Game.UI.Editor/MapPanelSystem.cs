using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Game.Achievements;
using Game.Areas;
using Game.Assets;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Reflection;
using Game.SceneFlow;
using Game.Serialization;
using Game.Simulation;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class MapPanelSystem : EditorPanelSystemBase
{
	private struct PreviewInfo : IEquatable<PreviewInfo>
	{
		private ImageAsset m_ImageAsset;

		private TextureAsset m_TextureAsset;

		public Texture texture
		{
			get
			{
				object obj = m_ImageAsset?.Load(srgb: true);
				if (obj == null)
				{
					TextureAsset textureAsset = m_TextureAsset;
					if ((object)textureAsset == null)
					{
						return null;
					}
					obj = textureAsset.Load();
				}
				return (Texture)obj;
			}
		}

		public IconButton button { get; set; }

		public string name
		{
			get
			{
				object obj = m_ImageAsset?.name;
				if (obj == null)
				{
					TextureAsset textureAsset = m_TextureAsset;
					if ((object)textureAsset == null)
					{
						return null;
					}
					obj = textureAsset.name;
				}
				return (string)obj;
			}
		}

		public void Set(ImageAsset imageAsset, TextureAsset fallback = null)
		{
			m_ImageAsset = imageAsset;
			m_TextureAsset = null;
			button.icon = imageAsset?.ToUri() ?? fallback?.ToUri();
		}

		public void Set(TextureAsset textureAsset, TextureAsset fallback = null)
		{
			m_TextureAsset = textureAsset;
			m_ImageAsset = null;
			button.icon = textureAsset?.ToUri() ?? fallback?.ToUri();
		}

		public PreviewInfo(IconButton button)
		{
			this.button = button;
			m_ImageAsset = null;
			m_TextureAsset = null;
		}

		public bool CopyToTextureAsset(ILocalAssetDatabase db, AssetDataPath path, out TextureAsset asset)
		{
			if (m_ImageAsset != null || m_TextureAsset != null)
			{
				asset = db.AddAsset<TextureAsset>(path);
				if (m_ImageAsset != null)
				{
					Texture2D texture2D = m_ImageAsset.Load(srgb: false);
					Texture2D texture2D2 = new Texture2D(texture2D.width, texture2D.height, texture2D.graphicsFormat, TextureCreationFlags.DontInitializePixels);
					Graphics.CopyTexture(texture2D, texture2D2);
					asset.SetData(texture2D2);
					return true;
				}
				using Stream stream = m_TextureAsset.GetReadStream();
				using Stream destination = asset.GetWriteStream();
				stream.CopyTo(destination);
				return true;
			}
			asset = null;
			return false;
		}

		public bool Equals(PreviewInfo other)
		{
			if (m_ImageAsset == other.m_ImageAsset)
			{
				return m_TextureAsset == other.m_TextureAsset;
			}
			return false;
		}
	}

	private Colossal.Hash128 m_CurrentSourceDataGuid;

	private TerrainSystem m_TerrainSystem;

	private SaveGameSystem m_SaveGameSystem;

	private MapMetadataSystem m_MapMetadataSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private IMapTilePurchaseSystem m_MapTilePurchaseSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private PrefabSystem m_PrefabSystem;

	private EditorAssetUploadPanel m_AssetUploadPanel;

	private EntityQuery m_TimeQuery;

	private EntityQuery m_ThemeQuery;

	public bool m_MapNameAsCityName;

	public int m_StartingYear;

	public int m_StartingMonth;

	public float m_StartingTime;

	public bool m_CurrentYearAsStartingYear;

	private IconButtonGroup m_ThemeButtonGroup;

	private LocalizationField m_MapNameLocalization;

	private LocalizationField m_MapDescriptionLocalization;

	private PreviewInfo m_Preview;

	private PreviewInfo m_Thumbnail;

	private Button m_MapTileSelectionButton;

	private static readonly string kSelectStartingTilesPrompt = "Editor.SELECT_STARTING_TILES";

	private static readonly string kStopSelectingStartingTilesPrompt = "Editor.STOP_SELECTING_STARTING_TILES";

	private MapRequirementSystem m_MapRequirementSystem;

	private PagedList m_RequiredListWidget;

	private EditorGenerator m_Generator;

	private PdxSdkPlatform m_Platform;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Generator = new EditorGenerator();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SaveGameSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_MapMetadataSystem = base.World.GetOrCreateSystemManaged<MapMetadataSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_MapRequirementSystem = base.World.GetOrCreateSystemManaged<MapRequirementSystem>();
		m_AssetUploadPanel = base.World.GetOrCreateSystemManaged<EditorAssetUploadPanel>();
		m_TimeQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_ThemeQuery = GetEntityQuery(ComponentType.ReadOnly<ThemeData>());
		m_Preview = new PreviewInfo(new IconButton
		{
			action = ShowPreviewPicker
		});
		m_Thumbnail = new PreviewInfo(new IconButton
		{
			action = ShowThumbnailPicker
		});
		title = "Editor.MAP";
		IWidget[] array = new IWidget[2];
		IWidget[] array2 = new IWidget[2];
		EditorSection editorSection = new EditorSection
		{
			displayName = "Editor.MAP_SETTINGS",
			expanded = true
		};
		EditorSection editorSection2 = editorSection;
		IWidget[] obj = new IWidget[14]
		{
			new Group
			{
				displayName = "Editor.MAP_NAME",
				tooltip = "Editor.MAP_NAME_TOOLTIP",
				children = new IWidget[1] { m_MapNameLocalization = new LocalizationField("Editor.MAP_NAME") }
			},
			new Group
			{
				displayName = "Editor.MAP_DESCRIPTION",
				tooltip = "Editor.MAP_DESCRIPTION_TOOLTIP",
				children = new IWidget[1] { m_MapDescriptionLocalization = new LocalizationField("Editor.MAP_DESCRIPTION") }
			},
			new ToggleField
			{
				displayName = "Editor.MAP_NAME_AS_DEFAULT",
				tooltip = "Editor.MAP_NAME_AS_DEFAULT_TOOLTIP",
				accessor = new DelegateAccessor<bool>(() => m_MapNameAsCityName, delegate(bool value)
				{
					m_MapNameAsCityName = value;
				})
			},
			new Divider(),
			new IntInputField
			{
				displayName = "Editor.STARTING_YEAR",
				tooltip = "Editor.STARTING_YEAR_TOOLTIP",
				disabled = () => m_CurrentYearAsStartingYear,
				min = 0,
				max = 3000,
				accessor = new DelegateAccessor<int>(() => (!m_CurrentYearAsStartingYear) ? m_StartingYear : DateTime.Now.Year, SetStartingYear)
			},
			new ToggleField
			{
				displayName = "Editor.CURRENT_YEAR_AS_DEFAULT",
				tooltip = "Editor.CURRENT_YEAR_AS_DEFAULT_TOOLTIP",
				accessor = new DelegateAccessor<bool>(() => m_CurrentYearAsStartingYear, delegate(bool value)
				{
					m_CurrentYearAsStartingYear = value;
				})
			},
			new IntInputField
			{
				displayName = "Editor.STARTING_MONTH",
				tooltip = "Editor.STARTING_MONTH_TOOLTIP",
				min = 1,
				max = 12,
				accessor = new DelegateAccessor<int>(() => m_StartingMonth, SetStartingMonth)
			},
			new TimeSliderField
			{
				displayName = "Editor.STARTING_TIME",
				tooltip = "Editor.STARTING_TIME_TOOLTIP",
				min = 0f,
				max = 0.99930555f,
				accessor = new DelegateAccessor<float>(() => m_StartingTime, SetStartingTime)
			},
			new Group
			{
				displayName = "Editor.CAMERA_STARTING_POSITION",
				tooltip = "Editor.CAMERA_STARTING_POSITION_TOOLTIP",
				children = new IWidget[4]
				{
					new Float3InputField
					{
						displayName = "Editor.CAMERA_PIVOT",
						tooltip = "Editor.CAMERA_PIVOT_TOOLTIP",
						accessor = new DelegateAccessor<float3>(() => m_CityConfigurationSystem.m_CameraPivot, delegate(float3 value)
						{
							m_CityConfigurationSystem.m_CameraPivot = value;
						})
					},
					new Float2InputField
					{
						displayName = "Editor.CAMERA_ANGLE",
						tooltip = "Editor.CAMERA_ANGLE_TOOLTIP",
						accessor = new DelegateAccessor<float2>(() => m_CityConfigurationSystem.m_CameraAngle, delegate(float2 value)
						{
							m_CityConfigurationSystem.m_CameraAngle = value;
						})
					},
					new FloatInputField
					{
						displayName = "Editor.CAMERA_ZOOM",
						tooltip = "Editor.CAMERA_ZOOM_TOOLTIP",
						accessor = new DelegateAccessor<double>(() => m_CityConfigurationSystem.m_CameraZoom, delegate(double value)
						{
							m_CityConfigurationSystem.m_CameraZoom = (float)value;
						})
					},
					new Button
					{
						displayName = "Editor.CAPTURE_CAMERA_POSITION",
						tooltip = "Editor.CAPTURE_CAMERA_POSITION_TOOLTIP",
						action = CaptureCameraProperties
					}
				}
			},
			null,
			null,
			null,
			null,
			null
		};
		Button obj2 = new Button
		{
			displayName = kSelectStartingTilesPrompt,
			action = ToggleMapTileSelection,
			showBackHint = true
		};
		Button button = obj2;
		m_MapTileSelectionButton = obj2;
		obj[9] = button;
		obj[10] = new EditorSection
		{
			displayName = "Editor.THEME",
			tooltip = "Editor.THEME_TOOLTIP",
			expanded = true,
			children = new IWidget[1] { m_ThemeButtonGroup = new IconButtonGroup() }
		};
		obj[11] = new EditorSection
		{
			displayName = "Editor.CONTENT_PREREQUISITES",
			tooltip = "Editor.CONTENT_PREREQUISITES_TOOLTIP",
			expanded = true,
			children = new IWidget[1] { m_RequiredListWidget = EditorGenerator.NamedWidget(m_Generator.TryBuildList(new ObjectAccessor<PrefabEntityListWrapper<ContentPrefab>>(new PrefabEntityListWrapper<ContentPrefab>(m_CityConfigurationSystem.requiredContent, m_PrefabSystem), readOnly: false), 0, null, Array.Empty<object>()), "Editor.REQUIREMENTS", "Editor.REQUIREMENTS_TOOLTIP") }
		};
		obj[12] = new Group
		{
			displayName = "Editor.PREVIEW",
			tooltip = "Editor.PREVIEW_TOOLTIP",
			children = new IWidget[1] { m_Preview.button }
		};
		obj[13] = new Group
		{
			displayName = "Editor.THUMBNAIL",
			tooltip = "Editor.THUMBNAIL_TOOLTIP",
			children = new IWidget[1] { m_Thumbnail.button }
		};
		editorSection2.children = obj;
		array2[0] = editorSection;
		array2[1] = new EditorSection
		{
			displayName = "Editor.CHECKLIST",
			tooltip = "Editor.CHECKLIST_TOOLTIP",
			children = new IWidget[2]
			{
				new Group
				{
					displayName = "Editor.CHECKLIST_REQUIRED",
					tooltip = "Editor.CHECKLIST_REQUIRED_TOOLTIP",
					children = new IWidget[4]
					{
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_STARTING_TILES",
							tooltip = "Editor.CHECKLIST_STARTING_TILES_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.hasStartingArea)
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_WATER",
							tooltip = "Editor.CHECKLIST_WATER_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.StartingAreaHasResource(MapFeature.SurfaceWater) || m_MapRequirementSystem.StartingAreaHasResource(MapFeature.GroundWater))
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_ROAD_CONNECTION",
							tooltip = "Editor.CHECKLIST_ROAD_CONNECTION_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.roadConnection)
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_NAME",
							tooltip = "Editor.CHECKLIST_NAME_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapNameLocalization.IsValid())
						}
					}
				},
				new Group
				{
					displayName = "Editor.CHECKLIST_OPTIONAL",
					tooltip = "Editor.CHECKLIST_OPTIONAL_TOOLTIP",
					children = new IWidget[7]
					{
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_TRAIN_CONNECTION",
							tooltip = "Editor.CHECKLIST_TRAIN_CONNECTION_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.trainConnection)
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_AIR_CONNECTION",
							tooltip = "Editor.CHECKLIST_AIR_CONNECTION_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.airConnection)
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_ELECTRICITY_CONNECTION",
							tooltip = "Editor.CHECKLIST_ELECTRICITY_CONNECTION_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.electricityConnection)
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_OIL",
							tooltip = "Editor.CHECKLIST_OIL_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.MapHasResource(MapFeature.Oil))
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_ORE",
							tooltip = "Editor.CHECKLIST_ORE_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.MapHasResource(MapFeature.Ore))
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_FOREST",
							tooltip = "Editor.CHECKLIST_FOREST_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.MapHasResource(MapFeature.Forest))
						},
						new ToggleField
						{
							displayName = "Editor.CHECKLIST_FERTILE",
							tooltip = "Editor.CHECKLIST_FERTILE_TOOLTIP",
							disabled = () => true,
							accessor = new DelegateAccessor<bool>(() => m_MapRequirementSystem.MapHasResource(MapFeature.FertileLand))
						}
					}
				}
			}
		};
		array[0] = Scrollable.WithChildren(array2);
		array[1] = ButtonRow.WithChildren(new Button[3]
		{
			new Button
			{
				displayName = "Editor.LOAD_MAP",
				tooltip = "Editor.LOAD_MAP_TOOLTIP",
				action = ShowLoadMapPanel
			},
			new Button
			{
				displayName = "Editor.SAVE_MAP",
				tooltip = "Editor.SAVE_MAP_TOOLTIP",
				action = ShowSaveMapPanel
			},
			new Button
			{
				displayName = "GameListScreen.GAME_OPTION[shareMap]",
				action = ShowShareMapPanel,
				hidden = () => m_Platform == null || !m_Platform.cachedLoggedIn
			}
		});
		children = array;
		m_Platform = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
		PlatformManager.instance.onPlatformRegistered += delegate(IPlatformServiceIntegration psi)
		{
			if (psi is PdxSdkPlatform platform)
			{
				m_Platform = platform;
			}
		};
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (serializationContext.purpose == Purpose.NewMap || serializationContext.purpose == Purpose.LoadMap)
		{
			if (m_TimeQuery.IsEmptyIgnoreFilter)
			{
				Entity entity = base.EntityManager.CreateEntity();
				base.EntityManager.AddComponentData(entity, new TimeData
				{
					m_StartingYear = DateTime.Now.Year,
					m_StartingMonth = 6,
					m_StartingHour = 6,
					m_StartingMinutes = 0
				});
			}
			m_RequiredListWidget.SetPropertiesChanged();
			FetchThemes();
			FetchTime();
			m_CurrentSourceDataGuid = serializationContext.instigatorGuid;
			MapMetadata asset = AssetDatabase.global.GetAsset<MapMetadata>(m_CurrentSourceDataGuid);
			m_MapMetadataSystem.mapName = ((!string.IsNullOrEmpty(asset?.target?.displayName)) ? asset.target.displayName : Guid.NewGuid().ToString());
			InitLocalization(asset);
			InitPreview(asset);
		}
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		base.activeSubPanel = null;
		m_MapRequirementSystem.Enabled = true;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		m_MapTilePurchaseSystem.selecting = false;
		m_MapRequirementSystem.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		m_MapRequirementSystem.Update();
		if (m_MapTilePurchaseSystem.selecting)
		{
			UpdateMapTileButton(kStopSelectingStartingTilesPrompt);
			UpdateStartingTiles();
		}
		else
		{
			UpdateMapTileButton(kSelectStartingTilesPrompt);
		}
	}

	protected override bool OnClose()
	{
		if (m_MapTilePurchaseSystem.selecting)
		{
			ToggleMapTileSelection();
			return false;
		}
		return true;
	}

	private void FetchThemes()
	{
		List<IconButton> list = new List<IconButton>();
		NativeArray<Entity> nativeArray = m_ThemeQuery.ToEntityArray(Allocator.Temp);
		foreach (Entity theme in nativeArray)
		{
			ThemePrefab prefab = m_PrefabSystem.GetPrefab<ThemePrefab>(theme);
			list.Add(new IconButton
			{
				icon = (ImageSystem.GetIcon(prefab) ?? "Media/Editor/Object.svg"),
				tooltip = LocalizedString.Id("Assets.THEME[" + prefab.name + "]"),
				action = delegate
				{
					m_CityConfigurationSystem.defaultTheme = theme;
				},
				selected = () => m_CityConfigurationSystem.defaultTheme == theme
			});
		}
		nativeArray.Dispose();
		m_ThemeButtonGroup.children = list.ToArray();
	}

	private void FetchTime()
	{
		TimeData singleton = m_TimeQuery.GetSingleton<TimeData>();
		m_StartingYear = singleton.m_StartingYear;
		m_CurrentYearAsStartingYear = true;
		m_StartingMonth = singleton.m_StartingMonth + 1;
		m_StartingTime = singleton.TimeOffset;
	}

	private void SetStartingYear(int value)
	{
		m_StartingYear = value;
		ApplyTime();
	}

	private void SetStartingMonth(int value)
	{
		m_StartingMonth = value;
		ApplyTime();
	}

	private void SetStartingTime(float value)
	{
		m_StartingTime = value;
		ApplyTime();
	}

	private void ApplyTime()
	{
		TimeData component = new TimeData
		{
			m_StartingYear = m_StartingYear,
			m_StartingMonth = (byte)(m_StartingMonth - 1),
			TimeOffset = m_StartingTime
		};
		EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
		Entity singletonEntity = m_TimeQuery.GetSingletonEntity();
		entityCommandBuffer.SetComponent(singletonEntity, component);
	}

	public void ShowLoadMapPanel()
	{
		base.activeSubPanel = new LoadAssetPanel("Editor.LOAD_MAP", GetMaps(), OnLoadMap, base.CloseSubPanel);
	}

	public void ShowSaveMapPanel()
	{
		base.activeSubPanel = new SaveAssetPanel("Editor.SAVE_MAP", GetMaps(), m_CurrentSourceDataGuid, delegate(string name, Colossal.Hash128? overwriteGuid)
		{
			OnSaveMap(name, overwriteGuid);
		}, base.CloseSubPanel);
	}

	private void ShowShareMapPanel()
	{
		base.activeSubPanel = new SaveAssetPanel("Editor.SAVE_MAP_SHARE", GetMaps(), m_CurrentSourceDataGuid, delegate(string name, Colossal.Hash128? overwriteGuid)
		{
			OnSaveMap(name, overwriteGuid, ShareMap);
		}, base.CloseSubPanel, "Editor.SAVE_SHARE");
	}

	private IEnumerable<AssetItem> GetMaps()
	{
		foreach (MapMetadata asset in AssetDatabase.global.GetAssets(default(SearchFilter<MapMetadata>)))
		{
			using (asset)
			{
				if (!(asset.database is AssetDatabase<Colossal.IO.AssetDatabase.Game>) && TryGetAssetItem(asset, out var item))
				{
					yield return item;
				}
			}
		}
	}

	private bool TryGetAssetItem(MapMetadata asset, out AssetItem item)
	{
		try
		{
			MapInfo target = asset.target;
			SourceMeta meta = asset.GetMeta();
			item = new AssetItem
			{
				guid = asset.id,
				fileName = meta.fileName,
				displayName = meta.displayName,
				image = target.thumbnail.ToUri(MenuHelpers.defaultPreview),
				badge = ((meta.remoteStorageSourceName != "Local") ? meta.remoteStorageSourceName : null)
			};
			return true;
		}
		catch (Exception exception)
		{
			base.log.Error(exception);
			item = null;
		}
		return false;
	}

	private void ShowPreviewPicker()
	{
		base.activeSubPanel = new LoadAssetPanel("Editor.PREVIEW", EditorPrefabUtils.GetUserImages(), OnSelectPreview, base.CloseSubPanel);
	}

	private void ShowThumbnailPicker()
	{
		base.activeSubPanel = new LoadAssetPanel("Editor.THUMBNAIL", EditorPrefabUtils.GetUserImages(), OnSelectThumbnail, base.CloseSubPanel);
	}

	private void OnSelectPreview(Colossal.Hash128 guid)
	{
		m_Preview.Set(AssetDatabase.global.GetAsset<ImageAsset>(guid));
		CloseSubPanel();
	}

	private void OnSelectThumbnail(Colossal.Hash128 guid)
	{
		m_Thumbnail.Set(AssetDatabase.global.GetAsset<ImageAsset>(guid));
		CloseSubPanel();
	}

	private void OnLoadMap(Colossal.Hash128 guid)
	{
		GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new ConfirmationDialog(null, "Common.DIALOG_MESSAGE[ProgressLoss]", "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]"), delegate(int ret)
		{
			if (ret == 0)
			{
				LoadMap(guid);
			}
		});
	}

	public async Task LoadMap(Colossal.Hash128 guid)
	{
		CloseSubPanel();
		if (AssetDatabase.global.TryGetAsset(guid, out MapMetadata asset))
		{
			await GameManager.instance.Load(GameMode.Editor, Purpose.LoadMap, asset).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void InitPreview(MapMetadata asset = null)
	{
		m_Preview.Set(asset?.target?.preview, MenuHelpers.defaultPreview);
		m_Thumbnail.Set(asset?.target?.thumbnail, MenuHelpers.defaultThumbnail);
	}

	private void InitLocalization(MapMetadata asset = null)
	{
		if (asset != null)
		{
			m_MapNameLocalization.Initialize(asset.target.localeAssets, $"Maps.MAP_TITLE[{asset.target.displayName}]");
			m_MapDescriptionLocalization.Initialize(asset.target.localeAssets, $"Maps.MAP_DESCRIPTION[{asset.target.displayName}]");
		}
		else
		{
			m_MapNameLocalization.Initialize();
			m_MapDescriptionLocalization.Initialize();
		}
	}

	private void OnSaveMap(string fileName, Colossal.Hash128? overwriteGuid, Action<MapMetadata> callback = null)
	{
		m_MapMetadataSystem.Update();
		if (overwriteGuid.HasValue)
		{
			GameManager.instance.userInterface.appBindings.ShowConfirmationDialog(new ConfirmationDialog(null, "Common.DIALOG_MESSAGE[OverwriteMap]", "Common.DIALOG_ACTION[Yes]", "Common.DIALOG_ACTION[No]"), delegate(int ret)
			{
				if (ret == 0)
				{
					CloseSubPanel();
					MapMetadata asset = AssetDatabase.global.GetAsset<MapMetadata>(overwriteGuid.Value);
					SourceMeta meta = asset.GetMeta();
					SaveMap(meta.displayName, overwriteGuid.Value, asset.target, asset.database, AssetDataPath.Create(meta.subPath, meta.fileName), asset.database != AssetDatabase.game, callback);
				}
			});
		}
		else
		{
			CloseSubPanel();
			SaveMap(fileName, Colossal.Hash128.Empty, null, AssetDatabase.user, SaveHelpers.GetAssetDataPath<MapMetadata>(AssetDatabase.user, fileName), embedLocalization: true, callback);
		}
	}

	public async Task SaveMap(string fileName, Colossal.Hash128 overwriteGuid, MapInfo existing, ILocalAssetDatabase finalDb, AssetDataPath packagePath, bool embedLocalization, Action<MapMetadata> callback = null)
	{
		m_MapMetadataSystem.mapName = fileName;
		using ILocalAssetDatabase db = AssetDatabase.GetTransient(0L);
		MapInfo info = GetMapInfo(existing);
		AssetDataPath name = fileName;
		MapMetadata meta = db.AddAsset<MapMetadata>(name, overwriteGuid);
		meta.target = info;
		MapData mapData = (info.mapData = db.AddAsset<MapData>(name));
		info.climate = SaveClimate(db);
		m_CurrentSourceDataGuid = meta.id;
		m_SaveGameSystem.context = new Context(Purpose.SaveMap, Version.current, m_CurrentSourceDataGuid);
		m_SaveGameSystem.stream = mapData.GetWriteStream();
		await m_SaveGameSystem.RunOnce();
		string[] array = m_SaveGameSystem.referencedContent.Select((Entity x) => m_PrefabSystem.GetPrefabName(x)).ToArray();
		info.contentPrerequisites = ((array.Length != 0) ? array : null);
		if (m_Preview.CopyToTextureAsset(db, m_Preview.name, out var asset))
		{
			asset.Save();
			info.preview = asset;
		}
		TextureAsset asset2;
		if (m_Preview.Equals(m_Thumbnail))
		{
			info.thumbnail = asset;
		}
		else if (m_Thumbnail.CopyToTextureAsset(db, m_Thumbnail.name, out asset2))
		{
			asset2.Save();
			info.thumbnail = asset2;
		}
		if (embedLocalization)
		{
			info.localeAssets = SaveLocalization(db, fileName);
		}
		else
		{
			info.localeAssets = null;
		}
		meta.Save();
		using (AssetDatabase.global.DisableNotificationsScoped())
		{
			if (finalDb.Exists<PackageAsset>(packagePath, out var asset3))
			{
				Identifier id = asset3.id;
				finalDb.DeleteAsset(asset3);
				asset3 = finalDb.AddAsset<PackageAsset, ILocalAssetDatabase>(packagePath, db, id);
				asset3.Save();
			}
			else
			{
				asset3 = finalDb.AddAsset(packagePath, db);
				asset3.Save();
			}
		}
		if (finalDb.dataSource.hasCache)
		{
			string text = await finalDb.ResaveCache();
			if (!string.IsNullOrWhiteSpace(text))
			{
				UnityEngine.Debug.Log(text);
			}
		}
		GameManager.instance.RunOnMainThread(delegate
		{
			PlatformManager.instance.UnlockAchievement(Game.Achievements.Achievements.Cartography);
			InitPreview(meta);
			if (callback != null)
			{
				callback(meta);
			}
		});
	}

	private PrefabAsset SaveClimate(ILocalAssetDatabase database)
	{
		ClimateSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(orCreateSystemManaged.currentClimate);
		if (prefab.builtin)
		{
			return null;
		}
		PrefabBase prefabBase = prefab.Clone();
		prefabBase.name = prefab.name;
		prefabBase.asset = database.AddAsset<PrefabAsset, ScriptableObject>(prefabBase.name, prefabBase);
		prefabBase.asset.Save();
		return prefabBase.asset;
	}

	private LocaleAsset[] SaveLocalization(ILocalAssetDatabase db, string fileName)
	{
		Dictionary<string, LocaleData> dictionary = new Dictionary<string, LocaleData>();
		m_MapNameLocalization.BuildLocaleData($"Maps.MAP_TITLE[{m_MapMetadataSystem.mapName}]", dictionary, m_MapMetadataSystem.mapName);
		m_MapDescriptionLocalization.BuildLocaleData($"Maps.MAP_DESCRIPTION[{m_MapMetadataSystem.mapName}]", dictionary);
		List<LocaleAsset> list = new List<LocaleAsset>(dictionary.Keys.Count);
		foreach (string key in dictionary.Keys)
		{
			LocaleAsset localeAsset = db.AddAsset<LocaleAsset>(fileName + "_" + key);
			LocalizationManager localizationManager = GameManager.instance.localizationManager;
			localeAsset.SetData(dictionary[key], localizationManager.LocaleIdToSystemLanguage(key), GameManager.instance.localizationManager.GetLocalizedName(key));
			localeAsset.Save();
			list.Add(localeAsset);
		}
		return list.ToArray();
	}

	private void ShareMap(MapMetadata map)
	{
		m_AssetUploadPanel.Show(map);
		base.activeSubPanel = m_AssetUploadPanel;
	}

	private MapInfo GetMapInfo(MapInfo merge = null)
	{
		MapInfo obj = merge ?? new MapInfo();
		obj.displayName = m_MapMetadataSystem.mapName;
		obj.theme = m_MapMetadataSystem.theme;
		obj.temperatureRange = m_MapMetadataSystem.temperatureRange;
		obj.cloudiness = m_MapMetadataSystem.cloudiness;
		obj.precipitation = m_MapMetadataSystem.precipitation;
		obj.latitude = m_MapMetadataSystem.latitude;
		obj.longitude = m_MapMetadataSystem.longitude;
		obj.area = m_MapMetadataSystem.area;
		obj.surfaceWaterAvailability = m_MapMetadataSystem.surfaceWaterAvailability;
		obj.groundWaterAvailability = m_MapMetadataSystem.groundWaterAvailability;
		obj.resources = m_MapMetadataSystem.resources;
		obj.connections = m_MapMetadataSystem.connections;
		obj.nameAsCityName = m_MapNameAsCityName;
		obj.startingYear = (m_CurrentYearAsStartingYear ? (-1) : m_StartingYear);
		obj.buildableLand = m_MapMetadataSystem.buildableLand;
		return obj;
	}

	private void CaptureCameraProperties()
	{
		if (CameraController.TryGet(out var cameraController))
		{
			m_CityConfigurationSystem.m_CameraPivot = cameraController.pivot;
			m_CityConfigurationSystem.m_CameraAngle = cameraController.angle;
			m_CityConfigurationSystem.m_CameraZoom = cameraController.zoom;
		}
	}

	private void ToggleMapTileSelection()
	{
		m_MapTilePurchaseSystem.selecting = !m_MapTilePurchaseSystem.selecting;
		m_MapTileSelectionButton.showBackHint = m_MapTilePurchaseSystem.selecting;
		m_MapTileSelectionButton.SetPropertiesChanged();
	}

	private void UpdateStartingTiles()
	{
	}

	private void UpdateMapTileButton(string text)
	{
		if (m_MapTileSelectionButton.displayName.value != text)
		{
			m_MapTileSelectionButton.displayName = text;
			m_MapTileSelectionButton.SetPropertiesChanged();
		}
	}

	[Preserve]
	public MapPanelSystem()
	{
	}
}
