using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
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
public class CommercialSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnCompanyJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_CompanyPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_PropertyLessCompanies;

		[ReadOnly]
		public NativeArray<int> m_ResourceDemands;

		[ReadOnly]
		public Random m_Random;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_Processes;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		public int m_EmptySignatureBuildingCount;

		public NativeArray<uint> m_LastSpawnedCommercialFrame;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				int num = m_ResourceDemands[resourceIndex];
				if (m_EmptySignatureBuildingCount <= 0 && (num <= 0 || m_FrameIndex - m_LastSpawnedCommercialFrame[resourceIndex] <= m_DemandParameterData.m_FrameIntervalForSpawning.y))
				{
					continue;
				}
				bool flag = false;
				for (int i = 0; i < m_PropertyLessCompanies.Length; i++)
				{
					Entity entity = m_PropertyLessCompanies[i];
					if (m_Prefabs.HasComponent(entity))
					{
						Entity prefab = m_Prefabs[entity].m_Prefab;
						if (m_Processes.HasComponent(prefab) && m_Processes[prefab].m_Output.m_Resource == iterator.resource)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					SpawnCompany(iterator.resource);
					m_LastSpawnedCommercialFrame[resourceIndex] = m_FrameIndex;
				}
			}
		}

		private void SpawnCompany(Resource resource)
		{
			if (m_CompanyPrefabs.Length <= 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < m_CompanyPrefabs.Length; i++)
			{
				if ((resource & m_Processes[m_CompanyPrefabs[i]].m_Output.m_Resource) != Resource.NoResource)
				{
					num++;
				}
			}
			if (num == 0)
			{
				return;
			}
			int num2 = m_Random.NextInt(num);
			for (int j = 0; j < m_CompanyPrefabs.Length; j++)
			{
				if ((resource & m_Processes[m_CompanyPrefabs[j]].m_Output.m_Resource) != Resource.NoResource)
				{
					if (num2 == 0)
					{
						Entity entity = m_CompanyPrefabs[j];
						ArchetypeData archetypeData = m_Archetypes[entity];
						Entity e = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
						PrefabRef component = new PrefabRef
						{
							m_Prefab = entity
						};
						m_CommandBuffer.SetComponent(e, component);
					}
					num2--;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_ArchetypeData_RO_ComponentLookup = state.GetComponentLookup<ArchetypeData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	private EntityQuery m_CommercialCompanyPrefabGroup;

	private EntityQuery m_PropertyLessCompanyGroup;

	private EntityQuery m_DemandParameterQuery;

	private CommercialDemandSystem m_CommercialDemandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private NativeArray<uint> m_LastSpawnedCommercialFrame;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CommercialCompanyPrefabGroup = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<CommercialCompanyData>(), ComponentType.ReadOnly<IndustrialProcessData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_PropertyLessCompanyGroup = GetEntityQuery(ComponentType.ReadOnly<CommercialCompany>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<PropertyRenter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_LastSpawnedCommercialFrame = new NativeArray<uint>(EconomyUtils.ResourceCount, Allocator.Persistent);
		RequireForUpdate(m_CommercialCompanyPrefabGroup);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_LastSpawnedCommercialFrame.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_SimulationSystem.frameIndex / 16 % 8 == 1 && m_CommercialDemandSystem.companyDemand > 0)
		{
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			JobHandle deps;
			SpawnCompanyJob jobData = new SpawnCompanyJob
			{
				m_CompanyPrefabs = m_CommercialCompanyPrefabGroup.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_PropertyLessCompanies = m_PropertyLessCompanyGroup.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_Archetypes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Processes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDemands = m_CommercialDemandSystem.GetResourceDemands(out deps),
				m_Random = RandomSeed.Next().GetRandom((int)m_SimulationSystem.frameIndex),
				m_FrameIndex = m_SimulationSystem.frameIndex,
				m_LastSpawnedCommercialFrame = m_LastSpawnedCommercialFrame,
				m_DemandParameterData = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, deps));
			m_CommercialDemandSystem.AddReader(base.Dependency);
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
	public CommercialSpawnSystem()
	{
	}
}
