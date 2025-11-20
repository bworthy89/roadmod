using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Policies;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class CityPolicyTooltipSystem : TooltipSystemBase
{
	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private RaycastSystem m_RaycastSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_ActiveInfomodeQuery;

	private Entity m_AdvancedPollutionManagementPolicy;

	private StringTooltip m_PollutionManagement;

	private RaycastResult m_RaycastResult;

	private RaycastResult raycastResult
	{
		get
		{
			if (m_RaycastResult.m_Owner == Entity.Null && m_CameraUpdateSystem.TryGetViewer(out var viewer))
			{
				RaycastInput input = new RaycastInput
				{
					m_Line = ToolRaycastSystem.CalculateRaycastLine(viewer.camera),
					m_TypeMask = TypeMask.StaticObjects,
					m_CollisionMask = (CollisionMask.OnGround | CollisionMask.Overground)
				};
				m_RaycastSystem.AddInput(this, input);
				NativeArray<RaycastResult> result = m_RaycastSystem.GetResult(this);
				if (result.Length != 0)
				{
					m_RaycastResult = result[0];
				}
			}
			return m_RaycastResult;
		}
		set
		{
			m_RaycastResult = value;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_RaycastSystem = base.World.GetOrCreateSystemManaged<RaycastSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ActiveInfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<InfomodeData>(),
				ComponentType.ReadOnly<InfomodeActive>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<InfoviewHeatmapData>(),
				ComponentType.ReadOnly<InfoviewBuildingStatusData>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<InfomodeGroup>() }
		});
		m_PollutionManagement = new StringTooltip
		{
			path = "cityPolicyEffect",
			icon = "Media/Game/Policies/AdvancedPollutionManagement.svg"
		};
		foreach (Entity item in GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PolicyData>(),
				ComponentType.ReadOnly<CityModifierData>()
			}
		}).ToEntityArray(Allocator.Temp))
		{
			if (m_PrefabSystem.GetPrefab<PolicyPrefab>(item).name == "Advanced Pollution Management")
			{
				m_AdvancedPollutionManagementPolicy = item;
				break;
			}
		}
		if (m_AdvancedPollutionManagementPolicy == Entity.Null)
		{
			throw new Exception("Advanced pollution management policy not found");
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool != m_DefaultTool || m_ToolSystem.activeInfoview == null || m_ActiveInfomodeQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		raycastResult = default(RaycastResult);
		foreach (Entity item in m_ActiveInfomodeQuery.ToEntityArray(Allocator.Temp))
		{
			InfoviewBuildingStatusData component10;
			if (base.EntityManager.TryGetComponent<InfoviewHeatmapData>(item, out var component))
			{
				switch (component.m_Type)
				{
				case HeatmapData.GroundPollution:
				{
					if (IsAdvancedPollutionManagementEnabled() && raycastResult.m_Owner != Entity.Null && base.EntityManager.HasComponent<Building>(raycastResult.m_Owner) && base.EntityManager.TryGetComponent<PrefabRef>(raycastResult.m_Owner, out var component6) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(component6.m_Prefab, out var component7) && base.EntityManager.TryGetComponent<ZoneData>(component7.m_ZonePrefab, out var component8) && component8.m_AreaType == AreaType.Industrial && base.EntityManager.TryGetComponent<PollutionData>(component6.m_Prefab, out var component9) && component9.m_GroundPollution > 0f)
					{
						m_PollutionManagement.value = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[PolicyEffectGroundPollution]");
						AddMouseTooltip(m_PollutionManagement);
					}
					return;
				}
				case HeatmapData.AirPollution:
				{
					if (IsAdvancedPollutionManagementEnabled() && raycastResult.m_Owner != Entity.Null && base.EntityManager.HasComponent<Building>(raycastResult.m_Owner) && base.EntityManager.TryGetComponent<PrefabRef>(raycastResult.m_Owner, out var component2) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(component2.m_Prefab, out var component3) && base.EntityManager.TryGetComponent<ZoneData>(component3.m_ZonePrefab, out var component4) && component4.m_AreaType == AreaType.Industrial && base.EntityManager.TryGetComponent<PollutionData>(component2.m_Prefab, out var component5) && component5.m_AirPollution > 0f)
					{
						m_PollutionManagement.value = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[PolicyEffectAirPolution]");
						AddMouseTooltip(m_PollutionManagement);
					}
					return;
				}
				}
			}
			else if (base.EntityManager.TryGetComponent<InfoviewBuildingStatusData>(item, out component10) && component10.m_Type == BuildingStatusType.GarbageAccumulation)
			{
				if (IsAdvancedPollutionManagementEnabled() && raycastResult.m_Owner != Entity.Null && base.EntityManager.HasComponent<Building>(raycastResult.m_Owner) && base.EntityManager.TryGetComponent<PrefabRef>(raycastResult.m_Owner, out var component11) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(component11.m_Prefab, out var component12) && base.EntityManager.TryGetComponent<ZoneData>(component12.m_ZonePrefab, out var component13) && component13.m_AreaType == AreaType.Industrial && base.EntityManager.HasComponent<GarbageProducer>(raycastResult.m_Owner))
				{
					m_PollutionManagement.value = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[PolicyEffectGarbage]");
					AddMouseTooltip(m_PollutionManagement);
				}
				break;
			}
		}
	}

	private bool IsAdvancedPollutionManagementEnabled()
	{
		foreach (Policy item in base.EntityManager.GetBuffer<Policy>(m_CitySystem.City, isReadOnly: true))
		{
			if (!(item.m_Policy != m_AdvancedPollutionManagementPolicy))
			{
				return (item.m_Flags & PolicyFlags.Active) != 0;
			}
		}
		return false;
	}

	[Preserve]
	public CityPolicyTooltipSystem()
	{
	}
}
