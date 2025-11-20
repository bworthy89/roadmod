using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HouseholdSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnHouseholdJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_PrefabEntities;

		[ReadOnly]
		public NativeList<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<HouseholdData> m_HouseholdDatas;

		[ReadOnly]
		public ComponentLookup<DynamicHousehold> m_Dynamics;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		public Entity m_City;

		public EntityCommandBuffer m_CommandBuffer;

		public int m_Demand;

		public Unity.Mathematics.Random m_Random;

		[ReadOnly]
		public NativeArray<int> m_LowFactors;

		[ReadOnly]
		public NativeArray<int> m_MedFactors;

		[ReadOnly]
		public NativeArray<int> m_HiFactors;

		[ReadOnly]
		public NativeArray<int> m_StudyPositions;

		private bool IsValidStudyPrefab(Entity householdPrefab)
		{
			HouseholdData householdData = m_HouseholdDatas[householdPrefab];
			if ((m_StudyPositions[1] + m_StudyPositions[2] <= 0 || !m_Random.NextBool()) && m_StudyPositions[3] + m_StudyPositions[4] > 0)
			{
				return householdData.m_StudentCount > 0;
			}
			if (m_StudyPositions[1] + m_StudyPositions[2] > 0)
			{
				return householdData.m_ChildCount > 0;
			}
			return false;
		}

		public void Execute()
		{
			Population population = m_Populations[m_City];
			int max = Mathf.RoundToInt(300f / math.clamp(m_DemandParameterData.m_HouseholdSpawnSpeedFactor * math.log(1f + 0.001f * (float)population.m_Population), 0.5f, 20f));
			int num = m_Random.NextInt(max);
			int num2 = 0;
			while (num < m_Demand)
			{
				num2++;
				m_Demand -= num;
				num = m_Random.NextInt(max);
			}
			if (num2 == 0)
			{
				return;
			}
			int y = m_LowFactors[6] + m_MedFactors[6] + m_HiFactors[6];
			int y2 = m_LowFactors[12] + m_MedFactors[12] + m_HiFactors[12];
			y = math.max(0, y);
			y2 = math.max(0, y2);
			float num3 = (float)y2 / (float)(y2 + y);
			for (int i = 0; i < num2; i++)
			{
				int num4 = 0;
				bool flag = m_Random.NextFloat() < num3;
				for (int j = 0; j < m_PrefabEntities.Length; j++)
				{
					if (IsValidStudyPrefab(m_PrefabEntities[j]) == flag)
					{
						num4 += m_HouseholdDatas[m_PrefabEntities[j]].m_Weight;
					}
				}
				num4 = m_Random.NextInt(num4);
				int index = 0;
				for (int k = 0; k < m_PrefabEntities.Length; k++)
				{
					if (IsValidStudyPrefab(m_PrefabEntities[k]) == flag)
					{
						num4 -= m_HouseholdDatas[m_PrefabEntities[k]].m_Weight;
					}
					if (num4 < 0)
					{
						index = k;
						break;
					}
				}
				Entity prefab = m_PrefabEntities[index];
				ArchetypeData archetypeData = m_Archetypes[index];
				Entity e = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
				PrefabRef component = new PrefabRef
				{
					m_Prefab = prefab
				};
				m_CommandBuffer.SetComponent(e, component);
				if (m_OutsideConnectionEntities.Length > 0 && BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, m_Random, m_DemandParameterData.m_CitizenOCSpawnParameters, out var result))
				{
					CurrentBuilding component2 = new CurrentBuilding
					{
						m_CurrentBuilding = result
					};
					m_CommandBuffer.AddComponent(e, component2);
				}
				else
				{
					m_CommandBuffer.AddComponent(e, default(Deleted));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<HouseholdData> __Game_Prefabs_HouseholdData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DynamicHousehold> __Game_Prefabs_DynamicHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_HouseholdData_RO_ComponentLookup = state.GetComponentLookup<HouseholdData>(isReadOnly: true);
			__Game_Prefabs_DynamicHousehold_RO_ComponentLookup = state.GetComponentLookup<DynamicHousehold>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_DemandParameterQuery;

	private ResidentialDemandSystem m_ResidentialDemandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CountStudyPositionsSystem m_CountStudyPositionsSystem;

	private CitySystem m_CitySystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
		m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>(), ComponentType.Exclude<DynamicHousehold>());
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		RequireForUpdate(m_HouseholdPrefabQuery);
		RequireForUpdate(m_OutsideConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = base.Dependency;
		int householdDemand = m_ResidentialDemandSystem.householdDemand;
		if (householdDemand > 0)
		{
			JobHandle deps;
			NativeArray<int> lowDensityDemandFactors = m_ResidentialDemandSystem.GetLowDensityDemandFactors(out deps);
			JobHandle deps2;
			NativeArray<int> mediumDensityDemandFactors = m_ResidentialDemandSystem.GetMediumDensityDemandFactors(out deps2);
			JobHandle deps3;
			NativeArray<int> highDensityDemandFactors = m_ResidentialDemandSystem.GetHighDensityDemandFactors(out deps3);
			jobHandle = IJobExtensions.Schedule(new SpawnHouseholdJob
			{
				m_PrefabEntities = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle),
				m_Archetypes = m_HouseholdPrefabQuery.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2),
				m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3),
				m_HouseholdDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HouseholdData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Dynamics = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DynamicHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DemandParameterData = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
				m_LowFactors = lowDensityDemandFactors,
				m_MedFactors = mediumDensityDemandFactors,
				m_HiFactors = highDensityDemandFactors,
				m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out var deps4),
				m_City = m_CitySystem.City,
				m_Demand = householdDemand,
				m_Random = RandomSeed.Next().GetRandom((int)m_SimulationSystem.frameIndex),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			}, JobUtils.CombineDependencies(outJobHandle, outJobHandle2, jobHandle, outJobHandle3, deps, deps2, deps3, deps4));
			m_ResidentialDemandSystem.AddReader(jobHandle);
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		}
		base.Dependency = jobHandle;
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
	public HouseholdSpawnSystem()
	{
	}
}
