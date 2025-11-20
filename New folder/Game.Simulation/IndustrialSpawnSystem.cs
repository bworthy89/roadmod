using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Buildings;
using Game.City;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class IndustrialSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckSpawnJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

		[ReadOnly]
		public ComponentTypeHandle<ArchetypeData> m_ArchetypeType;

		[ReadOnly]
		public NativeArray<int> m_ResourceDemands;

		[ReadOnly]
		public NativeArray<int> m_WarehouseDemands;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_StorageChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_IndustrialChunks;

		[ReadOnly]
		public NativeList<PrefabRef> m_ExistingExtractorPrefabs;

		[ReadOnly]
		public NativeList<PrefabRef> m_ExistingIndustrialPrefabs;

		public int m_EmptySignatureBuildingCount;

		public Entity m_City;

		public EntityCommandBuffer m_CommandBuffer;

		public uint m_SimulationFrame;

		private void SpawnCompany(NativeList<ArchetypeChunk> chunks, Resource resource, ref Unity.Mathematics.Random random)
		{
			int num = 0;
			for (int i = 0; i < chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = chunks[i];
				NativeArray<IndustrialProcessData> nativeArray = archetypeChunk.GetNativeArray(ref m_ProcessType);
				for (int j = 0; j < archetypeChunk.Count; j++)
				{
					if ((resource & nativeArray[j].m_Output.m_Resource) != Resource.NoResource)
					{
						num++;
					}
				}
			}
			if (num <= 0)
			{
				return;
			}
			int num2 = random.NextInt(num);
			for (int k = 0; k < chunks.Length; k++)
			{
				ArchetypeChunk archetypeChunk2 = chunks[k];
				NativeArray<Entity> nativeArray2 = archetypeChunk2.GetNativeArray(m_EntityType);
				NativeArray<IndustrialProcessData> nativeArray3 = archetypeChunk2.GetNativeArray(ref m_ProcessType);
				NativeArray<ArchetypeData> nativeArray4 = archetypeChunk2.GetNativeArray(ref m_ArchetypeType);
				for (int l = 0; l < archetypeChunk2.Count; l++)
				{
					if ((resource & nativeArray3[l].m_Output.m_Resource) != Resource.NoResource)
					{
						if (num2 == 0)
						{
							Spawn(nativeArray2[l], nativeArray4[l]);
							return;
						}
						num2--;
					}
				}
			}
		}

		private void Spawn(Entity prefab, ArchetypeData archetypeData)
		{
			Entity e = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
			PrefabRef component = new PrefabRef
			{
				m_Prefab = prefab
			};
			m_CommandBuffer.SetComponent(e, component);
		}

		public void Execute()
		{
			Unity.Mathematics.Random random = new Unity.Mathematics.Random(m_SimulationFrame);
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				int num = m_ResourceDemands[resourceIndex];
				ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
				if (resourceData.m_IsProduceable)
				{
					if (!resourceData.m_IsMaterial)
					{
						Population population = m_Populations[m_City];
						if (m_EmptySignatureBuildingCount > 0 || random.NextInt(Mathf.RoundToInt(5000f / math.min(5f, math.max(1f, math.log10(1 + population.m_Population))))) < num)
						{
							bool flag = false;
							for (int i = 0; i < m_ExistingIndustrialPrefabs.Length; i++)
							{
								if (m_ProcessDatas[m_ExistingIndustrialPrefabs[i].m_Prefab].m_Output.m_Resource == iterator.resource)
								{
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								SpawnCompany(m_IndustrialChunks, iterator.resource, ref random);
							}
						}
					}
					else
					{
						bool flag2 = false;
						for (int j = 0; j < m_ExistingExtractorPrefabs.Length; j++)
						{
							if (m_ProcessDatas[m_ExistingExtractorPrefabs[j].m_Prefab].m_Output.m_Resource == iterator.resource)
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							SpawnCompany(m_IndustrialChunks, iterator.resource, ref random);
							break;
						}
					}
				}
				if (resourceData.m_IsTradable && m_WarehouseDemands[resourceIndex] > 0)
				{
					SpawnCompany(m_StorageChunks, iterator.resource, ref random);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_ArchetypeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ArchetypeData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
		}
	}

	private EntityQuery m_IndustrialCompanyPrefabQuery;

	private EntityQuery m_StorageCompanyPrefabQuery;

	private EntityQuery m_ExtractorQuery;

	private EntityQuery m_ExtractorCompanyQuery;

	private EntityQuery m_ExistingIndustrialQuery;

	private EntityQuery m_ExistingExtractorQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

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
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_IndustrialCompanyPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<IndustrialCompanyData>(), ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.Exclude<StorageCompanyData>());
		m_StorageCompanyPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<StorageCompanyData>());
		m_ExtractorQuery = GetEntityQuery(ComponentType.ReadOnly<ExtractorProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>());
		m_ExtractorCompanyQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ExtractorCompany>(), ComponentType.Exclude<Deleted>());
		m_ExistingIndustrialQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialCompany>(), ComponentType.Exclude<Game.Companies.ExtractorCompany>(), ComponentType.Exclude<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<PropertyRenter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ExistingExtractorQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.ExtractorCompany>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<PropertyRenter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_IndustrialCompanyPrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_SimulationSystem.frameIndex / 16 % 8 == 2 && m_IndustrialDemandSystem.industrialCompanyDemand + m_IndustrialDemandSystem.storageCompanyDemand + m_IndustrialDemandSystem.officeCompanyDemand > 0)
		{
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			JobHandle outJobHandle3;
			JobHandle outJobHandle4;
			JobHandle deps;
			JobHandle deps2;
			CheckSpawnJob jobData = new CheckSpawnJob
			{
				m_ArchetypeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ProcessType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IndustrialChunks = m_IndustrialCompanyPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_StorageChunks = m_StorageCompanyPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_ExistingExtractorPrefabs = m_ExistingExtractorQuery.ToComponentDataListAsync<PrefabRef>(base.World.UpdateAllocator.ToAllocator, out outJobHandle3),
				m_ExistingIndustrialPrefabs = m_ExistingIndustrialQuery.ToComponentDataListAsync<PrefabRef>(base.World.UpdateAllocator.ToAllocator, out outJobHandle4),
				m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDemands = m_IndustrialDemandSystem.GetResourceDemands(out deps),
				m_WarehouseDemands = m_IndustrialDemandSystem.GetStorageCompanyDemands(out deps2),
				m_City = m_CitySystem.City,
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_SimulationFrame = m_SimulationSystem.frameIndex,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, outJobHandle3, outJobHandle4, deps, deps2));
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
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
	public IndustrialSpawnSystem()
	{
	}
}
