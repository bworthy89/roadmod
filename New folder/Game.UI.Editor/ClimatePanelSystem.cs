using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Reflection;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class ClimatePanelSystem : EditorPanelSystemBase, SeasonsField.IAdapter
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ClimateSystem m_ClimateSystem;

	private WindSimulationSystem m_WindSimulationSystem;

	private PlanetarySystem m_PlanetarySystem;

	private EntityQuery m_ClimateQuery;

	private EntityQuery m_ClimateSeasonQuery;

	private EntityQuery m_UpdatedQuery;

	private SeasonsField.SeasonCurves m_SeasonsCurves;

	private EntityQuery m_RenderQuery;

	private ToolSystem m_ToolSystem;

	private InfoviewPrefab m_WindInfoview;

	private double m_LastWindDirection;

	private int m_LastClimateHash;

	private int m_InfoviewCooldown;

	private EditorSection m_InspectorSection;

	private EditorGenerator m_Generator;

	private Coroutine m_DelayedInfomodeReset;

	private InfoviewPrefab m_PreviousInfoview;

	private EntityQuery m_AllInfoviewQuery;

	private TypeHandle __TypeHandle;

	private ClimatePrefab currentClimate
	{
		get
		{
			if (base.World.EntityManager.Exists(m_ClimateSystem.currentClimate))
			{
				return m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate);
			}
			return null;
		}
		set
		{
			m_ClimateSystem.currentClimate = m_PrefabSystem.GetEntity(value);
			RebuildInspector();
		}
	}

	private double windDirection
	{
		get
		{
			float2 constantWind = m_WindSimulationSystem.constantWind;
			float num = Mathf.Atan2(constantWind.y, constantWind.x) * 57.29578f;
			if (num < 0f)
			{
				num += 360f;
			}
			return num;
		}
		set
		{
			float num = math.radians((float)value);
			float2 direction = new float2((float)Math.Cos(num), (float)Math.Sin(num));
			m_WindSimulationSystem.SetWind(direction, 40f);
			if (m_DelayedInfomodeReset == null)
			{
				m_PreviousInfoview = m_ToolSystem.activeInfoview;
			}
			m_ToolSystem.infoview = m_WindInfoview;
			foreach (InfomodeInfo infomode in m_ToolSystem.GetInfomodes(m_WindInfoview))
			{
				m_ToolSystem.SetInfomodeActive(infomode.m_Mode, infomode.m_Mode.name == "WindInfomode", infomode.m_Priority);
			}
			if (m_DelayedInfomodeReset != null)
			{
				GameManager.instance.StopCoroutine(m_DelayedInfomodeReset);
			}
			m_DelayedInfomodeReset = GameManager.instance.StartCoroutine(DisableInfomode());
		}
	}

	IEnumerable<ClimateSystem.SeasonInfo> SeasonsField.IAdapter.seasons
	{
		get
		{
			return currentClimate.m_Seasons;
		}
		set
		{
			currentClimate.m_Seasons = value.ToArray();
		}
	}

	SeasonsField.SeasonCurves SeasonsField.IAdapter.curves
	{
		get
		{
			return m_SeasonsCurves;
		}
		set
		{
			m_SeasonsCurves = value;
		}
	}

	public Entity selectedSeason { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_AllInfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<InfoviewData>());
		GetWindInfoView();
		title = "Editor.CLIMATE_SETTINGS";
		m_Generator = new EditorGenerator();
		IWidget[] array = new IWidget[1];
		IWidget[] array2 = new IWidget[1];
		EditorSection obj = new EditorSection
		{
			displayName = "Editor.CLIMATE_SETTINGS",
			tooltip = "Editor.CLIMATE_SETTINGS_TOOLTIP",
			expanded = true
		};
		EditorSection editorSection = obj;
		m_InspectorSection = obj;
		array2[0] = editorSection;
		array[0] = Scrollable.WithChildren(array2);
		children = array;
	}

	protected override void OnValueChanged(IWidget widget)
	{
		base.OnValueChanged(widget);
		ClimatePrefab climatePrefab = currentClimate;
		if (!climatePrefab.builtin)
		{
			climatePrefab.RebuildCurves();
		}
		m_PlanetarySystem.latitude = climatePrefab.m_Latitude;
		m_PlanetarySystem.longitude = climatePrefab.m_Longitude;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (serializationContext.purpose == Purpose.LoadMap || serializationContext.purpose == Purpose.NewMap)
		{
			RebuildInspector();
		}
	}

	private void RebuildInspector()
	{
		List<IWidget> list = new List<IWidget>
		{
			new PopupValueField<PrefabBase>
			{
				displayName = "Editor.CLIMATE_LOAD_PREFAB",
				tooltip = "Editor.CLIMATE_LOAD_PREFAB_TOOLTIP",
				accessor = new DelegateAccessor<PrefabBase>(() => currentClimate, delegate(PrefabBase prefab)
				{
					currentClimate = (ClimatePrefab)prefab;
				}),
				popup = new PrefabPickerPopup(typeof(ClimatePrefab))
			}
		};
		ClimatePrefab climatePrefab = currentClimate;
		bool builtin = climatePrefab.builtin;
		if (builtin)
		{
			list.Add(new Label
			{
				displayName = "Editor.CREATE_CUSTOM_CLIMATE_PROMPT"
			});
			list.Add(new Button
			{
				displayName = "Editor.CREATE_CUSTOM_CLIMATE",
				action = Duplicate
			});
		}
		IWidget[] array = m_Generator.BuildMembers(new ObjectAccessor<PrefabBase>(climatePrefab), 0, "Climate Settings").ToArray();
		if (builtin)
		{
			IWidget[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				InspectorPanelSystem.DisableAllFields(array2[num]);
			}
		}
		list.AddRange(array);
		m_InspectorSection.children = list;
	}

	private void Duplicate()
	{
		PrefabBase prefabBase = currentClimate.Clone();
		m_PrefabSystem.AddPrefab(prefabBase);
		currentClimate = (ClimatePrefab)prefabBase;
	}

	private IEnumerator DisableInfomode()
	{
		yield return new WaitForSeconds(1f);
		m_ToolSystem.infoview = m_PreviousInfoview;
		m_DelayedInfomodeReset = null;
	}

	private void GetWindInfoView()
	{
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = m_AllInfoviewQuery.ToArchetypeChunkArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			NativeArray<PrefabData> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				InfoviewPrefab prefab = m_PrefabSystem.GetPrefab<InfoviewPrefab>(nativeArray2[j]);
				if (prefab.name == "AirPollution")
				{
					m_WindInfoview = prefab;
				}
			}
		}
		nativeArray.Dispose();
	}

	public void RebuildCurves()
	{
		ClimatePrefab climatePrefab = currentClimate;
		climatePrefab.RebuildCurves();
		m_SeasonsCurves = default(SeasonsField.SeasonCurves);
		m_SeasonsCurves.m_Temperature = climatePrefab.m_Temperature;
		m_SeasonsCurves.m_Precipitation = climatePrefab.m_Precipitation;
		m_SeasonsCurves.m_Cloudiness = climatePrefab.m_Cloudiness;
		m_SeasonsCurves.m_Aurora = climatePrefab.m_Aurora;
		m_SeasonsCurves.m_Fog = climatePrefab.m_Fog;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public ClimatePanelSystem()
	{
	}
}
