using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class PollutionTooltipSystem : TooltipSystemBase
{
	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private RaycastSystem m_RaycastSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_ActiveInfomodeQuery;

	private IntTooltip m_Garbage;

	private IntTooltip m_AirPollution;

	private IntTooltip m_GroundPollution;

	private IntTooltip m_NoisePollution;

	private IntTooltip m_WaterPollution;

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
					m_TypeMask = (TypeMask.Terrain | TypeMask.StaticObjects),
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
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
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
		m_Garbage = new IntTooltip
		{
			path = "garbage",
			icon = "Media/Game/Icons/Garbage.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[Garbage]"),
			unit = "integer"
		};
		m_AirPollution = new IntTooltip
		{
			path = "airPollution",
			icon = "Media/Game/Icons/AirPollution.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[AirPollution]"),
			unit = "integer"
		};
		m_GroundPollution = new IntTooltip
		{
			path = "groundPollution",
			icon = "Media/Game/Icons/GroundPollution.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[GroundPollution]"),
			unit = "integer"
		};
		m_NoisePollution = new IntTooltip
		{
			path = "noisePollution",
			icon = "Media/Game/Icons/NoisePollution.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[NoisePollution]"),
			unit = "integer"
		};
		m_WaterPollution = new IntTooltip
		{
			path = "waterPollution",
			icon = "Media/Game/Icons/WaterPollution.svg",
			label = LocalizedString.Id("DefaultTool.INFOMODE_TOOLTIP[WaterPollution]"),
			unit = "integer"
		};
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
			InfoviewBuildingStatusData component2;
			if (base.EntityManager.TryGetComponent<InfoviewHeatmapData>(item, out var component))
			{
				switch (component.m_Type)
				{
				case HeatmapData.GroundPollution:
				{
					if (raycastResult.m_Owner == Entity.Null)
					{
						return;
					}
					JobHandle dependencies4;
					NativeArray<GroundPollution> map4 = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies4);
					dependencies4.Complete();
					GroundPollution pollution3 = GroundPollutionSystem.GetPollution(raycastResult.m_Hit.m_HitPosition, map4);
					m_GroundPollution.value = pollution3.m_Pollution;
					AddMouseTooltip(m_GroundPollution);
					break;
				}
				case HeatmapData.AirPollution:
				{
					if (raycastResult.m_Owner == Entity.Null)
					{
						return;
					}
					JobHandle dependencies3;
					NativeArray<AirPollution> map3 = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies3);
					dependencies3.Complete();
					AirPollution pollution2 = AirPollutionSystem.GetPollution(raycastResult.m_Hit.m_HitPosition, map3);
					m_AirPollution.value = pollution2.m_Pollution;
					AddMouseTooltip(m_AirPollution);
					break;
				}
				case HeatmapData.WaterPollution:
				{
					if (raycastResult.m_Owner == Entity.Null)
					{
						return;
					}
					JobHandle deps;
					WaterSurfaceData<SurfaceWater> data = m_WaterSystem.GetSurfaceData(out deps);
					deps.Complete();
					if (WaterUtils.SampleDepth(ref data, raycastResult.m_Hit.m_HitPosition) > 0f)
					{
						float num = WaterUtils.SamplePolluted(ref data, raycastResult.m_Hit.m_HitPosition);
						m_WaterPollution.value = (int)(num * 10000f);
						AddMouseTooltip(m_WaterPollution);
					}
					break;
				}
				case HeatmapData.GroundWaterPollution:
				{
					if (raycastResult.m_Owner == Entity.Null)
					{
						return;
					}
					JobHandle deps2;
					WaterSurfaceData<SurfaceWater> data2 = m_WaterSystem.GetSurfaceData(out deps2);
					deps2.Complete();
					if (WaterUtils.SampleDepth(ref data2, raycastResult.m_Hit.m_HitPosition) == 0f)
					{
						JobHandle dependencies2;
						NativeArray<GroundWater> map2 = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies2);
						dependencies2.Complete();
						GroundWater groundWater = GroundWaterSystem.GetGroundWater(raycastResult.m_Hit.m_HitPosition, map2);
						m_WaterPollution.value = groundWater.m_Polluted;
						AddMouseTooltip(m_WaterPollution);
					}
					break;
				}
				case HeatmapData.Noise:
				{
					if (raycastResult.m_Owner == Entity.Null)
					{
						return;
					}
					JobHandle dependencies;
					NativeArray<NoisePollution> map = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies);
					dependencies.Complete();
					NoisePollution pollution = NoisePollutionSystem.GetPollution(raycastResult.m_Hit.m_HitPosition, map);
					m_NoisePollution.value = pollution.m_Pollution;
					AddMouseTooltip(m_NoisePollution);
					break;
				}
				}
			}
			else if (base.EntityManager.TryGetComponent<InfoviewBuildingStatusData>(item, out component2) && component2.m_Type == BuildingStatusType.GarbageAccumulation)
			{
				if (raycastResult.m_Owner == Entity.Null)
				{
					break;
				}
				if (base.EntityManager.HasComponent<Building>(raycastResult.m_Owner) && base.EntityManager.TryGetComponent<GarbageProducer>(raycastResult.m_Owner, out var component3))
				{
					m_Garbage.value = component3.m_Garbage;
					AddMouseTooltip(m_Garbage);
				}
			}
		}
	}

	[Preserve]
	public PollutionTooltipSystem()
	{
	}
}
