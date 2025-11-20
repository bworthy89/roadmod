using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CommuterSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnCommuterHouseholdJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_PrefabEntities;

		[ReadOnly]
		public NativeList<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public NativeList<HouseholdData> m_HouseholdPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		[ReadOnly]
		public Workplaces m_FreeWorkplaces;

		[ReadOnly]
		public NativeArray<int> m_Employables;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		public uint m_Frame;

		public EntityCommandBuffer m_CommandBuffer;

		public RandomSeed m_RandomSeed;

		public void Execute()
		{
			Random random = m_RandomSeed.GetRandom((int)m_Frame);
			int num = m_FreeWorkplaces[2] + m_FreeWorkplaces[3] + m_FreeWorkplaces[4];
			int num2 = m_Employables[2] + m_Employables[3] + m_Employables[4];
			int num3 = (num - num2) / m_DemandParameterData.m_CommuterSlowSpawnFactor;
			for (int i = 0; i < num3; i++)
			{
				if (m_OutsideConnectionEntities.Length > 0 && BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, m_DemandParameterData.m_CommuterOCSpawnParameters, out var result))
				{
					int index = random.NextInt(m_HouseholdPrefabs.Length);
					Entity prefab = m_PrefabEntities[index];
					ArchetypeData archetypeData = m_Archetypes[index];
					Entity e = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
					PrefabRef component = new PrefabRef
					{
						m_Prefab = prefab
					};
					m_CommandBuffer.SetComponent(e, component);
					Household component2 = new Household
					{
						m_Flags = HouseholdFlags.Commuter
					};
					m_CommandBuffer.SetComponent(e, component2);
					CurrentBuilding component3 = new CurrentBuilding
					{
						m_CurrentBuilding = result
					};
					m_CommandBuffer.AddComponent(e, new CommuterHousehold
					{
						m_OriginalFrom = result
					});
					m_CommandBuffer.AddComponent(e, component3);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_CommuterQuery;

	private EntityQuery m_WorkerQuery;

	private EntityQuery m_DemandParameterQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>());
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CommuterQuery = GetEntityQuery(ComponentType.ReadOnly<CommuterHousehold>(), ComponentType.Exclude<Deleted>());
		m_WorkerQuery = GetEntityQuery(ComponentType.ReadOnly<Worker>(), ComponentType.Exclude<Deleted>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		RequireForUpdate(m_HouseholdPrefabQuery);
		RequireForUpdate(m_OutsideConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		DemandParameterData singleton = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
		int num = m_CommuterQuery.CalculateEntityCount();
		int num2 = m_WorkerQuery.CalculateEntityCount();
		if (num * singleton.m_CommuterWorkerRatioLimit < num2)
		{
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			JobHandle outJobHandle3;
			JobHandle outJobHandle4;
			SpawnCommuterHouseholdJob jobData = new SpawnCommuterHouseholdJob
			{
				m_PrefabEntities = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_Archetypes = m_HouseholdPrefabQuery.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_HouseholdPrefabs = m_HouseholdPrefabQuery.ToComponentDataListAsync<HouseholdData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle3),
				m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle4),
				m_Employables = m_CountHouseholdDataSystem.GetEmployables(),
				m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces(),
				m_DemandParameterData = singleton,
				m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Frame = m_SimulationSystem.frameIndex,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
				m_RandomSeed = RandomSeed.Next()
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(outJobHandle, outJobHandle2, outJobHandle3, base.Dependency, outJobHandle4));
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		}
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
	public CommuterSpawnSystem()
	{
	}
}
