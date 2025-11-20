using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Input;
using Game.Objects;
using Game.Prefabs;
using Game.Reflection;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class WaterPanelSystem : EditorPanelSystemBase
{
	[Serializable]
	private class WaterMaterialConfig
	{
		[InspectorName("Editor.WATER_SEA_FLOW_DIRECTION")]
		[Range(0f, 360f)]
		public float m_seaWindDirection;

		[InspectorName("Editor.WATER_SEA_FLOW_STRENGTH")]
		[Range(0f, 1f)]
		public float m_seaWindStrength;

		[InspectorName("Editor.WATER_FOAM_AMOUNT")]
		[Range(0f, 2f)]
		public float m_foamAmount;

		[InspectorName("Editor.WATER_FOAM_FADE_START")]
		[Range(0f, 1000f)]
		public float m_foamFadeStart;

		[InspectorName("Editor.WATER_FOAM_FADE_DISTANCE")]
		[Range(100f, 5000f)]
		public float m_foamFadeDistance;

		[InspectorName("Editor.WATER_RIPPLES_WIND_SPEED")]
		[Range(0f, 15f)]
		public float m_ripplesWindSpeed = 5f;

		[InspectorName("Editor.WATER_WAVES_MULTIPLIER")]
		[Range(0f, 2f)]
		public float m_wavesMultiplier = 1f;

		[InspectorName("Editor.WATER_LAKE_WAVES_MULTIPLIER")]
		[Range(0f, 1f)]
		public float m_lakeWavesMultiplier = 1f;

		[InspectorName("Editor.WATER_ABSORPTION_DISTANCE")]
		[Range(0f, 100f)]
		public float m_absorbtionDistance;

		[InspectorName("Editor.WATER_REFRACTION_COLOR")]
		public UnityEngine.Color m_waterColor;

		[InspectorName("Editor.WATER_SCATTERING_COLOR")]
		public UnityEngine.Color m_waterScatteringColor;

		[InspectorName("Editor.WATER_CAUSTICS_PLANE_DISTANCE")]
		[Range(0f, 10f)]
		public float m_causticsPlaneDistance;

		[InspectorName("Editor.WATER_CAUSTICS_INTENSITY")]
		[Range(0f, 3f)]
		public float m_causticsIntensity = 1f;

		[InspectorName("Editor.WATER_WAVE_MIN_DEPTH")]
		[Range(5f, 200f)]
		public float m_minWaterAmountForWaves = 1f;

		public void LoadConfig(WaterRenderSystem.WaterMaterialParams paramsIn)
		{
			m_seaWindDirection = paramsIn.SeaWindDirection;
			m_seaWindStrength = paramsIn.SeaWindStrength;
			m_foamAmount = paramsIn.m_foamAmount;
			m_foamFadeStart = paramsIn.m_foamFadeStart;
			m_foamFadeDistance = paramsIn.m_foamFadeDistance;
			m_wavesMultiplier = paramsIn.m_wavesMultiplier;
			m_lakeWavesMultiplier = paramsIn.m_lakeWavesMultiplier;
			m_waterColor = paramsIn.m_waterColor;
			m_waterScatteringColor = paramsIn.m_waterScatteringColor;
			m_causticsPlaneDistance = paramsIn.m_causticsPlaneDistance;
			m_causticsIntensity = paramsIn.m_causticsIntensity;
			m_minWaterAmountForWaves = paramsIn.m_minWaterAmountForWaves;
			m_ripplesWindSpeed = paramsIn.m_ripplesWindSpeed;
			m_absorbtionDistance = paramsIn.m_absorbtionDistance;
		}

		internal bool OnUpdate(WaterRenderSystem.WaterMaterialParams waterMaterialParams)
		{
			bool result = false;
			if (waterMaterialParams.m_seaWindDirection != m_seaWindDirection)
			{
				waterMaterialParams.m_seaWindDirection = m_seaWindDirection;
				result = true;
			}
			if (waterMaterialParams.m_seaWindStrength != m_seaWindStrength)
			{
				waterMaterialParams.m_seaWindStrength = m_seaWindStrength;
				result = true;
			}
			if (waterMaterialParams.m_foamAmount != m_foamAmount)
			{
				waterMaterialParams.m_foamAmount = m_foamAmount;
				result = true;
			}
			if (waterMaterialParams.m_foamFadeStart != m_foamFadeStart)
			{
				waterMaterialParams.m_foamFadeStart = m_foamFadeStart;
				result = true;
			}
			if (waterMaterialParams.m_foamFadeDistance != m_foamFadeDistance)
			{
				waterMaterialParams.m_foamFadeDistance = m_foamFadeDistance;
				result = true;
			}
			if (waterMaterialParams.m_wavesMultiplier != m_wavesMultiplier)
			{
				waterMaterialParams.m_wavesMultiplier = m_wavesMultiplier;
				result = true;
			}
			if (waterMaterialParams.m_lakeWavesMultiplier != m_lakeWavesMultiplier)
			{
				waterMaterialParams.m_lakeWavesMultiplier = m_lakeWavesMultiplier;
				result = true;
			}
			if (waterMaterialParams.m_waterColor != m_waterColor)
			{
				waterMaterialParams.m_waterColor = m_waterColor;
				result = true;
			}
			if (waterMaterialParams.m_waterScatteringColor != m_waterScatteringColor)
			{
				waterMaterialParams.m_waterScatteringColor = m_waterScatteringColor;
				result = true;
			}
			if (!waterMaterialParams.m_causticsPlaneDistance.Equals(m_causticsPlaneDistance))
			{
				waterMaterialParams.m_causticsPlaneDistance = m_causticsPlaneDistance;
				result = true;
			}
			if (!waterMaterialParams.m_causticsIntensity.Equals(m_causticsIntensity))
			{
				waterMaterialParams.m_causticsIntensity = m_causticsIntensity;
				result = true;
			}
			if (waterMaterialParams.m_minWaterAmountForWaves != m_minWaterAmountForWaves)
			{
				waterMaterialParams.m_minWaterAmountForWaves = m_minWaterAmountForWaves;
				result = true;
			}
			if (waterMaterialParams.m_ripplesWindSpeed != m_ripplesWindSpeed)
			{
				waterMaterialParams.m_ripplesWindSpeed = m_ripplesWindSpeed;
				result = true;
			}
			if (waterMaterialParams.m_absorbtionDistance != m_absorbtionDistance)
			{
				waterMaterialParams.m_absorbtionDistance = m_absorbtionDistance;
				result = true;
			}
			return result;
		}
	}

	private class WaterConfigLegacy
	{
		[Serializable]
		public class ConstantRateWaterSource : WaterSource
		{
			[InspectorName("Editor.WATER_RATE")]
			public float m_Rate;
		}

		[Serializable]
		public class ConstantLevelWaterSource : WaterSource
		{
			[InspectorName("Editor.HEIGHT")]
			public float m_Height;
		}

		[Serializable]
		public class BorderWaterSource : ConstantLevelWaterSource
		{
			[NonSerialized]
			[InspectorName("Editor.FLOOD_HEIGHT")]
			public float m_FloodHeight;
		}

		public abstract class WaterSource
		{
			[NonSerialized]
			public bool m_Initialized;

			[CustomField(typeof(WaterSourcePositionFactory))]
			[InspectorName("Editor.POSITION")]
			public float2 m_Position;

			[InspectorName("Editor.RADIUS")]
			public float m_Radius;

			[InspectorName("Editor.POLLUTION")]
			public float m_Pollution;
		}

		public class WaterSourcePositionFactory : IFieldBuilderFactory
		{
			public FieldBuilder TryCreate(Type memberType, object[] attributes)
			{
				return delegate(IValueAccessor accessor)
				{
					CastAccessor<float2> castAccessor = new CastAccessor<float2>(accessor);
					return new Column
					{
						children = new IWidget[2]
						{
							new Float2InputField
							{
								displayName = "Editor.POSITION",
								tooltip = "Editor.POSITION_TOOLTIP",
								accessor = castAccessor
							},
							new Button
							{
								displayName = "Editor.LOCATE",
								tooltip = "Editor.LOCATE_TOOLTIP",
								action = delegate
								{
									Locate(castAccessor);
								}
							}
						}
					};
				};
			}

			private void Locate(CastAccessor<float2> accessor)
			{
				float2 typedValue = accessor.GetTypedValue();
				World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>().activeCameraController.pivot = new Vector3(typedValue.x, 0f, typedValue.y);
			}
		}

		[InspectorName("Editor.CONSTANT_RATE_WATER_SOURCES")]
		public List<ConstantRateWaterSource> m_ConstantRateWaterSources = new List<ConstantRateWaterSource>();

		[InspectorName("Editor.CONSTANT_LEVEL_WATER_SOURCES")]
		public List<ConstantLevelWaterSource> m_ConstantLevelWaterSources = new List<ConstantLevelWaterSource>();

		[InspectorName("Editor.BORDER_RIVER_WATER_SOURCES")]
		public List<BorderWaterSource> m_BorderRiverWaterSources = new List<BorderWaterSource>();

		[InspectorName("Editor.BORDER_SEA_WATER_SOURCES")]
		public List<BorderWaterSource> m_BorderSeaWaterSources = new List<BorderWaterSource>();
	}

	private class WaterConfig
	{
		[Serializable]
		public class WaterSource
		{
			[NonSerialized]
			public bool m_Initialized;

			[ListElementLabel(false)]
			public string m_Name;

			[CustomField(typeof(WaterSourcePositionFactory))]
			[InspectorName("Editor.POSITION")]
			public float2 m_Position;

			[InspectorName("Editor.HEIGHT")]
			[Range(1f, 250f)]
			public float m_Height;

			[InspectorName("Editor.RADIUS")]
			[Range(1f, 2500f)]
			public float m_Radius;

			[InspectorName("Editor.POLLUTION")]
			public float m_Pollution;

			public int SourceID { get; set; }

			public int SourceNameID { get; set; }
		}

		public class WaterSourcePositionFactory : IFieldBuilderFactory
		{
			public FieldBuilder TryCreate(Type memberType, object[] attributes)
			{
				return delegate(IValueAccessor accessor)
				{
					CastAccessor<float2> castAccessor = new CastAccessor<float2>(accessor);
					return new Column
					{
						children = new IWidget[2]
						{
							new Float2InputField
							{
								displayName = "Editor.POSITION",
								tooltip = "Editor.POSITION_TOOLTIP",
								accessor = castAccessor
							},
							new Button
							{
								displayName = "Editor.LOCATE",
								tooltip = "Editor.LOCATE_TOOLTIP",
								action = delegate
								{
									Locate(castAccessor);
								}
							}
						}
					};
				};
			}

			private void Locate(CastAccessor<float2> accessor)
			{
				float2 typedValue = accessor.GetTypedValue();
				CameraUpdateSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
				existingSystemManaged.activeCameraController.pivot = new Vector3(typedValue.x, 0f, typedValue.y);
				Quaternion quaternion = Quaternion.LookRotation(existingSystemManaged.activeCameraController.pivot - existingSystemManaged.activeCameraController.position, math.up());
				existingSystemManaged.activeCameraController.rotation = quaternion.eulerAngles;
			}
		}

		[InspectorName("Editor.CONSTANT_LEVEL_WATER_SOURCES")]
		public List<WaterSource> m_WaterSources = new List<WaterSource>();

		[InspectorName("Editor.WATER_SEA_LEVEL")]
		[Range(0f, 2000f)]
		public float m_SeaLevel;

		[InspectorName("Editor.WATER_MATERIAL_CONFIG")]
		public WaterMaterialConfig m_waterMaterialConfig;

		public WaterConfig()
		{
			m_waterMaterialConfig = new WaterMaterialConfig();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private ToolSystem m_ToolSystem;

	private WaterToolSystem m_WaterToolSystem;

	private WaterSystem m_WaterSystem;

	private WaterRenderSystem m_WaterRenderSystem;

	private TerrainSystem m_TerrainSystem;

	private PrefabSystem m_PrefabSystem;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private NameSystem m_NameSystem;

	private EditorWaterConfigurationPrefab m_waterConfigPrefab;

	private EntityQuery m_AllInfoviewQuery;

	private EntityQuery m_WaterSourceQuery;

	private EntityQuery m_UpdatedSourceQuery;

	private EntityArchetype m_WaterSourceArchetype;

	private WaterConfig m_Config = new WaterConfig();

	private WaterConfigLegacy m_ConfigLegacy = new WaterConfigLegacy();

	private static readonly int[] kWaterSpeedValues = new int[7] { 0, 1, 8, 16, 32, 64, 128 };

	private float m_LastSeaLevel;

	private bool m_useLegacySources;

	private List<int> m_sourcesToRemove;

	private bool m_waterSourcesFectched;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_WaterToolSystem = base.World.GetOrCreateSystemManaged<WaterToolSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_WaterToolSystem.onWaterSourceClick += OnWaterSourceClick;
		m_WaterToolSystem.onWaterSourceDeleted += OnWaterSourceDeleted;
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_WaterSourceQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.Exclude<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_AllInfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<InfoviewData>());
		m_useLegacySources = m_WaterSystem.UseLegacyWaterSources;
		EntityQuery entityQuery = GetEntityQuery(ComponentType.ReadOnly<EditorWaterConfigurationData>());
		m_waterConfigPrefab = m_PrefabSystem.GetSingletonPrefab<EditorWaterConfigurationPrefab>(entityQuery);
		m_UpdatedSourceQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Simulation.WaterSourceData>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_WaterSourceArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>(), ComponentType.ReadWrite<Game.Objects.Transform>());
		UpdateConfigUI();
		m_sourcesToRemove = new List<int>();
		m_waterSourcesFectched = false;
	}

	private void UpdateConfigUI()
	{
		EditorGenerator editorGenerator = new EditorGenerator();
		title = "Editor.WATER";
		if (!m_WaterSystem.UseLegacyWaterSources)
		{
			IEnumerable<IWidget> source = editorGenerator.BuildMembers(new ObjectAccessor<WaterConfig>(m_Config), 0, "WaterSettings").Append(new Button
			{
				displayName = "Editor.RESET_WATER",
				action = delegate
				{
					ResetWater();
				}
			}).Append(new Button
			{
				displayName = "Editor.APPLY_WATER_SEA_LEVEL",
				action = delegate
				{
					ApplySeaLevel();
				}
			})
				.Append(new Button
				{
					displayName = "Editor.SHOW_SOURCES_NAME_TOGGLE",
					action = delegate
					{
						m_WaterToolSystem.m_showSourceNames = !m_WaterToolSystem.m_showSourceNames;
					}
				});
			source = source.Prepend(new ToggleField
			{
				displayName = "Editor.WATER_ACTIVE_BACKDROP_SIM",
				accessor = new DelegateAccessor<bool>(() => m_WaterSystem.WaterBackdropSimActive, delegate(bool val)
				{
					m_WaterSystem.WaterBackdropSimActive = val;
				}),
				hidden = () => !TerrainSystem.HasBackdrop
			});
			EditorSection editorSection = new EditorSection
			{
				displayName = "Editor.WATER_SETTINGS",
				tooltip = "Editor.WATER_SETTINGS_TOOLTIP",
				expanded = true,
				children = source.ToArray()
			};
			children = new IWidget[1] { Scrollable.WithChildren(new IWidget[2]
			{
				editorSection,
				new EditorSection
				{
					displayName = "Editor.WATER_SIMULATION_SPEED",
					tooltip = "Editor.WATER_SIMULATION_SPEED_TOOLTIP",
					children = BuildWaterSpeedToggles()
				}
			}) };
		}
		else
		{
			children = new IWidget[1] { Scrollable.WithChildren(new IWidget[3]
			{
				new Button
				{
					displayName = "Editor.CONVERT_TO_NEW_WATER_SOURCES",
					action = delegate
					{
						m_WaterSystem.UseLegacyWaterSources = false;
						FetchWaterSources();
						UpdateConfigUI();
					}
				},
				new EditorSection
				{
					displayName = "Editor.WATER_SETTINGS",
					tooltip = "Editor.WATER_SETTINGS_TOOLTIP",
					expanded = true,
					children = editorGenerator.BuildMembers(new ObjectAccessor<WaterConfigLegacy>(m_ConfigLegacy), 0, "WaterSettings").ToArray()
				},
				new EditorSection
				{
					displayName = "Editor.WATER_SIMULATION_SPEED",
					tooltip = "Editor.WATER_SIMULATION_SPEED_TOOLTIP",
					children = BuildWaterSpeedToggles()
				}
			}) };
		}
	}

	private bool TryGetWaterConfig(out EditorWaterConfigurationPrefab config)
	{
		return m_PrefabSystem.TryGetSingletonPrefab<EditorWaterConfigurationPrefab>(GetEntityQuery(ComponentType.ReadOnly<EditorWaterConfigurationPrefab>()), out config);
	}

	private void OnWaterSourceDeleted(int sourceId)
	{
		FetchWaterSources();
	}

	private void OnWaterSourceClick(int sourceEntityIndex)
	{
	}

	private void ApplySeaLevel()
	{
		m_WaterSystem.ResetToSealevel();
	}

	private void ResetWater()
	{
		m_WaterSystem.Reset();
	}

	private IWidget[] BuildWaterSpeedToggles()
	{
		IWidget[] array = new IWidget[kWaterSpeedValues.Length];
		for (int i = 0; i < kWaterSpeedValues.Length; i++)
		{
			int speed = kWaterSpeedValues[i];
			array[i] = new ToggleField
			{
				displayName = $"{speed}x",
				accessor = new DelegateAccessor<bool>(() => m_WaterSystem.WaterSimSpeed == speed, delegate(bool val)
				{
					if (val)
					{
						m_WaterSystem.WaterSimSpeed = speed;
					}
				})
			};
		}
		return array;
	}

	private void ShowInfoView()
	{
		m_ToolSystem.infoview = m_waterConfigPrefab.m_WaterInfoview;
		foreach (InfomodeInfo infomode in m_ToolSystem.GetInfomodes(m_waterConfigPrefab.m_WaterInfoview))
		{
			m_ToolSystem.SetInfomodeActive(infomode.m_Mode, infomode.m_Mode.name == m_waterConfigPrefab.m_WaterFlowInfo.m_Mode.name, infomode.m_Priority);
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse)
		{
			m_ToolSystem.activeTool = m_WaterToolSystem;
		}
		m_Config.m_SeaLevel = m_WaterSystem.SeaLevel;
		m_Config.m_waterMaterialConfig.LoadConfig(m_WaterRenderSystem.m_WaterMaterialParams);
		m_LastSeaLevel = m_Config.m_SeaLevel;
		if (!m_waterSourcesFectched)
		{
			FetchWaterSources();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_UpdatedSourceQuery.IsEmptyIgnoreFilter && !m_waterSourcesFectched)
		{
			FetchWaterSources();
		}
		if (!m_WaterSystem.UseLegacyWaterSources)
		{
			m_WaterSystem.SeaLevel = m_Config.m_SeaLevel;
			if (m_WaterSystem.SeaFlowDirection != m_Config.m_waterMaterialConfig.m_seaWindDirection)
			{
				ShowInfoView();
			}
			if (m_WaterSystem.SeaFlowStrength != m_Config.m_waterMaterialConfig.m_seaWindStrength)
			{
				ShowInfoView();
			}
			if (m_Config.m_waterMaterialConfig.OnUpdate(m_WaterRenderSystem.m_WaterMaterialParams))
			{
				m_WaterRenderSystem.m_WaterMaterialParams.ApplyMaterialParams();
			}
		}
		if (m_useLegacySources != m_WaterSystem.UseLegacyWaterSources)
		{
			m_useLegacySources = m_WaterSystem.UseLegacyWaterSources;
			UpdateConfigUI();
		}
		if (m_LastSeaLevel != m_Config.m_SeaLevel)
		{
			m_LastSeaLevel = m_Config.m_SeaLevel;
			m_WaterSystem.UpdateSealevel();
		}
		World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
		m_waterSourcesFectched = false;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		if (m_ToolSystem.activeTool == m_WaterToolSystem)
		{
			m_ToolSystem.ActivatePrefabTool(null);
		}
		m_WaterSystem.WaterSimSpeed = 1;
		m_waterSourcesFectched = false;
		base.OnStopRunning();
	}

	protected override bool OnCancel()
	{
		if (m_ToolSystem.activeTool == m_WaterToolSystem)
		{
			m_ToolSystem.ActivatePrefabTool(null);
			return false;
		}
		return base.OnCancel();
	}

	protected override void OnValueChanged(IWidget widget)
	{
		ApplyWaterSources();
	}

	private void FetchWaterSourcesInternal()
	{
		m_Config.m_WaterSources.Clear();
		m_Config.m_SeaLevel = m_WaterSystem.SeaLevel;
		m_Config.m_waterMaterialConfig.LoadConfig(m_WaterRenderSystem.m_WaterMaterialParams);
		NativeArray<Entity> nativeArray = m_WaterSourceQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<Game.Simulation.WaterSourceData> nativeArray2 = m_WaterSourceQuery.ToComponentDataArray<Game.Simulation.WaterSourceData>(Allocator.TempJob);
		NativeArray<Game.Objects.Transform> nativeArray3 = m_WaterSourceQuery.ToComponentDataArray<Game.Objects.Transform>(Allocator.TempJob);
		m_TerrainSystem.GetHeightData();
		try
		{
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Game.Simulation.WaterSourceData componentData = nativeArray2[i];
				_ = nativeArray3[i];
				if (!m_NameSystem.TryGetCustomName(nativeArray[i], out var customName))
				{
					customName = componentData.m_id.ToString("D2");
					m_NameSystem.SetCustomName(nativeArray[i], customName);
				}
				int num = m_OverlayRenderSystem.RegisterString(customName);
				bool num2 = num != componentData.SourceNameId;
				componentData.SourceNameId = num;
				List<WaterConfig.WaterSource> list = m_Config.m_WaterSources;
				WaterConfig.WaterSource waterSource = new WaterConfig.WaterSource();
				waterSource.m_Initialized = true;
				waterSource.SourceID = componentData.m_id;
				waterSource.SourceNameID = componentData.SourceNameId;
				waterSource.m_Position = nativeArray3[i].m_Position.xz;
				waterSource.m_Radius = componentData.m_Radius;
				waterSource.m_Height = componentData.m_Height;
				waterSource.m_Pollution = componentData.m_Polluted;
				waterSource.m_Name = customName;
				list.Add(waterSource);
				if (num2)
				{
					base.EntityManager.SetComponentData(nativeArray[i], componentData);
				}
			}
			m_Config.m_WaterSources.Sort((WaterConfig.WaterSource a, WaterConfig.WaterSource b) => a.SourceID.CompareTo(b.SourceID));
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeArray3.Dispose();
		}
	}

	private void FetchWaterSourcesLegacy()
	{
		m_ConfigLegacy.m_ConstantRateWaterSources.Clear();
		m_ConfigLegacy.m_ConstantLevelWaterSources.Clear();
		m_ConfigLegacy.m_BorderRiverWaterSources.Clear();
		m_ConfigLegacy.m_BorderSeaWaterSources.Clear();
		NativeArray<Game.Simulation.WaterSourceData> nativeArray = m_WaterSourceQuery.ToComponentDataArray<Game.Simulation.WaterSourceData>(Allocator.TempJob);
		NativeArray<Game.Objects.Transform> nativeArray2 = m_WaterSourceQuery.ToComponentDataArray<Game.Objects.Transform>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				switch (nativeArray[i].m_ConstantDepth)
				{
				case 0:
					m_ConfigLegacy.m_ConstantRateWaterSources.Add(new WaterConfigLegacy.ConstantRateWaterSource
					{
						m_Initialized = true,
						m_Rate = nativeArray[i].m_Height,
						m_Position = nativeArray2[i].m_Position.xz,
						m_Radius = nativeArray[i].m_Radius,
						m_Pollution = nativeArray[i].m_Polluted
					});
					break;
				case 1:
					m_ConfigLegacy.m_ConstantLevelWaterSources.Add(new WaterConfigLegacy.ConstantLevelWaterSource
					{
						m_Initialized = true,
						m_Height = nativeArray[i].m_Height,
						m_Position = nativeArray2[i].m_Position.xz,
						m_Radius = nativeArray[i].m_Radius,
						m_Pollution = nativeArray[i].m_Polluted
					});
					break;
				case 2:
					m_ConfigLegacy.m_BorderRiverWaterSources.Add(new WaterConfigLegacy.BorderWaterSource
					{
						m_Initialized = true,
						m_FloodHeight = nativeArray[i].m_Multiplier,
						m_Height = nativeArray[i].m_Height,
						m_Position = nativeArray2[i].m_Position.xz,
						m_Radius = nativeArray[i].m_Radius,
						m_Pollution = nativeArray[i].m_Polluted
					});
					break;
				case 3:
					m_ConfigLegacy.m_BorderSeaWaterSources.Add(new WaterConfigLegacy.BorderWaterSource
					{
						m_Initialized = true,
						m_FloodHeight = nativeArray[i].m_Multiplier,
						m_Height = nativeArray[i].m_Height,
						m_Position = nativeArray2[i].m_Position.xz,
						m_Radius = nativeArray[i].m_Radius,
						m_Pollution = nativeArray[i].m_Polluted
					});
					break;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
		}
	}

	public void FetchWaterSources()
	{
		m_waterSourcesFectched = true;
		if (m_WaterSystem.UseLegacyWaterSources)
		{
			FetchWaterSourcesLegacy();
		}
		else
		{
			FetchWaterSourcesInternal();
		}
	}

	private void ApplyWaterSources()
	{
		EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
		NativeArray<Entity> sources = m_WaterSourceQuery.ToEntityArray(Allocator.Temp);
		int sourceCount = 0;
		TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData();
		if (m_useLegacySources)
		{
			foreach (WaterConfigLegacy.ConstantRateWaterSource item in m_ConfigLegacy.m_ConstantRateWaterSources)
			{
				AddSourceLegacy(buffer, GetSource(sources, ref sourceCount), item, 0, ref item.m_Rate, ref terrainHeightData);
			}
			foreach (WaterConfigLegacy.ConstantLevelWaterSource item2 in m_ConfigLegacy.m_ConstantLevelWaterSources)
			{
				AddSourceLegacy(buffer, GetSource(sources, ref sourceCount), item2, 1, ref item2.m_Height, ref terrainHeightData);
			}
			foreach (WaterConfigLegacy.BorderWaterSource item3 in m_ConfigLegacy.m_BorderRiverWaterSources)
			{
				AddBorderSourceLegacy(buffer, GetSource(sources, ref sourceCount), item3, 2, ref terrainHeightData);
			}
			foreach (WaterConfigLegacy.BorderWaterSource item4 in m_ConfigLegacy.m_BorderSeaWaterSources)
			{
				AddBorderSourceLegacy(buffer, GetSource(sources, ref sourceCount), item4, 3, ref terrainHeightData);
			}
		}
		else
		{
			for (int i = 0; i < m_Config.m_WaterSources.Count; i++)
			{
				WaterConfig.WaterSource waterSource = m_Config.m_WaterSources[i];
				waterSource.SourceID = i;
				AddSource(buffer, GetSource(sources, ref sourceCount), waterSource, ref terrainHeightData);
			}
		}
		while (sourceCount < sources.Length)
		{
			buffer.AddComponent(sources[sourceCount++], default(Deleted));
		}
		sources.Dispose();
	}

	private Entity GetSource(NativeArray<Entity> sources, ref int sourceCount)
	{
		if (sourceCount < sources.Length)
		{
			return sources[sourceCount++];
		}
		return base.EntityManager.CreateEntity(m_WaterSourceArchetype);
	}

	private void AddSource(EntityCommandBuffer buffer, Entity entity, WaterConfig.WaterSource source, ref TerrainHeightData terrainHeightData)
	{
		if (!source.m_Initialized)
		{
			CameraUpdateSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
			float3 @float = existingSystemManaged.activeCameraController.pivot;
			source.m_Initialized = true;
			source.m_Position = @float.xz;
			Bounds3 bounds = MathUtils.Expand(TerrainUtils.GetBounds(ref terrainHeightData), 0f - source.m_Radius);
			if (!m_WaterSystem.WaterBackdropSimActive && !MathUtils.Intersect(bounds.xz, source.m_Position))
			{
				source.m_Position = MathUtils.Clamp(source.m_Position, bounds.xz);
				@float.xz = source.m_Position;
				existingSystemManaged.activeCameraController.pivot = @float;
			}
			new float3(source.m_Position.x, 0f, source.m_Position.y);
			source.m_Radius = 30f;
			source.m_Height = 10f;
			source.m_Position.y += source.m_Height;
			source.SourceID = m_WaterSystem.GetNextSourceId();
			source.m_Pollution = 0f;
			source.m_Name = source.SourceID.ToString("D2");
		}
		m_NameSystem.SetCustomName(entity, source.m_Name);
		source.SourceNameID = m_OverlayRenderSystem.RegisterString(source.m_Name);
		float3 float2 = new float3(source.m_Position.x, 0f, source.m_Position.y);
		float2.y = TerrainUtils.SampleHeight(ref terrainHeightData, float2);
		Game.Simulation.WaterSourceData component = new Game.Simulation.WaterSourceData
		{
			SourceNameId = source.SourceNameID,
			m_Radius = source.m_Radius,
			m_Polluted = source.m_Pollution,
			m_Height = source.m_Height,
			m_id = source.SourceID,
			m_modifier = 1f
		};
		buffer.SetComponent(entity, component);
		buffer.SetComponent(entity, new Game.Objects.Transform
		{
			m_Position = float2,
			m_Rotation = quaternion.identity
		});
	}

	private void AddSourceLegacy(EntityCommandBuffer buffer, Entity entity, WaterConfigLegacy.WaterSource source, int constantDepth, ref float amount, ref TerrainHeightData terrainHeightData)
	{
		if (!source.m_Initialized)
		{
			CameraUpdateSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
			float3 @float = existingSystemManaged.activeCameraController.pivot;
			source.m_Initialized = true;
			source.m_Position = @float.xz;
			Bounds3 bounds = MathUtils.Expand(TerrainUtils.GetBounds(ref terrainHeightData), 0f - source.m_Radius);
			if (!MathUtils.Intersect(bounds.xz, source.m_Position))
			{
				source.m_Position = MathUtils.Clamp(source.m_Position, bounds.xz);
				@float.xz = source.m_Position;
				existingSystemManaged.activeCameraController.pivot = @float;
			}
			if (constantDepth == 0)
			{
				source.m_Radius = 30f;
				amount = 20f;
			}
			else
			{
				source.m_Radius = 40f;
				amount = TerrainUtils.SampleHeight(ref terrainHeightData, new float3(source.m_Position.x, 0f, source.m_Position.y));
				amount += 25f - m_TerrainSystem.positionOffset.y;
			}
		}
		float3 float2 = new float3(source.m_Position.x, 0f, source.m_Position.y);
		Game.Simulation.WaterSourceData waterSourceData = new Game.Simulation.WaterSourceData
		{
			m_Height = amount,
			m_ConstantDepth = constantDepth,
			m_Radius = source.m_Radius,
			m_Polluted = source.m_Pollution
		};
		waterSourceData.m_Multiplier = WaterSystem.CalculateSourceMultiplier(waterSourceData, float2);
		buffer.SetComponent(entity, waterSourceData);
		buffer.SetComponent(entity, new Game.Objects.Transform
		{
			m_Position = float2,
			m_Rotation = quaternion.identity
		});
	}

	private void AddBorderSourceLegacy(EntityCommandBuffer buffer, Entity entity, WaterConfigLegacy.BorderWaterSource source, int constantDepth, ref TerrainHeightData terrainHeightData)
	{
		if (!source.m_Initialized)
		{
			CameraUpdateSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CameraUpdateSystem>();
			float3 @float = existingSystemManaged.activeCameraController.pivot;
			source.m_Initialized = true;
			source.m_Position = @float.xz;
			Bounds3 bounds = TerrainUtils.GetBounds(ref terrainHeightData);
			Bounds3 bounds2 = MathUtils.Expand(bounds, 0f - source.m_Radius);
			if (!MathUtils.Intersect(MathUtils.Expand(bounds, source.m_Radius).xz, source.m_Position))
			{
				source.m_Position = MathUtils.Clamp(source.m_Position, bounds.xz);
				@float.xz = source.m_Position;
				existingSystemManaged.activeCameraController.pivot = @float;
			}
			else if (MathUtils.Intersect(bounds2.xz, source.m_Position))
			{
				float2 float2 = source.m_Position - bounds2.min.xz;
				float2 float3 = bounds2.max.xz - source.m_Position;
				float2 trueValue = math.select(bounds.min.xz, bounds.max.xz, float3 < float2);
				float2 = math.min(float2, float3);
				source.m_Position = math.select(source.m_Position, trueValue, float2.xy < float2.yx);
				@float.xz = source.m_Position;
				existingSystemManaged.activeCameraController.pivot = @float;
			}
			source.m_Height = TerrainUtils.SampleHeight(ref terrainHeightData, new float3(source.m_Position.x, 0f, source.m_Position.y));
			source.m_Height -= m_TerrainSystem.positionOffset.y;
			if (constantDepth == 2)
			{
				source.m_Radius = 50f;
				source.m_Height += 30f;
			}
			else
			{
				source.m_Radius = 5000f;
				source.m_Height += 100f;
			}
		}
		buffer.SetComponent(entity, new Game.Simulation.WaterSourceData
		{
			m_Height = source.m_Height,
			m_ConstantDepth = constantDepth,
			m_Radius = source.m_Radius,
			m_Multiplier = source.m_FloodHeight,
			m_Polluted = source.m_Pollution
		});
		buffer.SetComponent(entity, new Game.Objects.Transform
		{
			m_Position = new float3(source.m_Position.x, 0f, source.m_Position.y),
			m_Rotation = quaternion.identity
		});
	}

	[Preserve]
	public WaterPanelSystem()
	{
	}
}
