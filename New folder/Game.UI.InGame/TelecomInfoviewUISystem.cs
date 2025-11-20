using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class TelecomInfoviewUISystem : InfoviewUISystemBase
{
	private const string kGroup = "telecomInfo";

	private TerrainSystem m_TerrainSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_TelecomQuery;

	private EntityQuery m_TelecomModifiedQuery;

	private EntityQuery m_DensityQuery;

	private NativeArray<TelecomCoverage> m_Coverage;

	private NativeArray<TelecomStatus> m_Status;

	private ValueBinding<IndicatorValue> m_NetworkAvailability;

	protected override bool Active
	{
		get
		{
			if (!base.Active)
			{
				return m_NetworkAvailability.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_TelecomModifiedQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TelecomQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>()
			}
		});
		m_TelecomModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_DensityQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<HouseholdCitizen>(),
				ComponentType.ReadOnly<Employee>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		AddBinding(m_NetworkAvailability = new ValueBinding<IndicatorValue>("telecomInfo", "networkAvailability", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		m_Coverage = new NativeArray<TelecomCoverage>(0, Allocator.Persistent);
		m_Status = new NativeArray<TelecomStatus>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Coverage.Dispose();
		m_Status.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
	}

	[Preserve]
	public TelecomInfoviewUISystem()
	{
	}
}
