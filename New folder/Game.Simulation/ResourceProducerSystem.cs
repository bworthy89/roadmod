using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResourceProducerSystem : GameSystemBase
{
	[BurstCompile]
	private struct PlayerMoneyAddJob : IJob
	{
		public NativeQueue<int> m_PlayerMoneyAddQueue;

		public ComponentLookup<PlayerMoney> m_PlayerMoneys;

		public Entity m_City;

		public void Execute()
		{
			PlayerMoney value = m_PlayerMoneys[m_City];
			int item;
			while (m_PlayerMoneyAddQueue.TryDequeue(out item))
			{
				value.Add(item);
			}
			m_PlayerMoneys[m_City] = value;
		}
	}

	[BurstCompile]
	private struct ResourceProducerJob : IJobChunk
	{
		public NativeQueue<int>.ParallelWriter m_PlayerMoneyAddQueue;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeeps;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> m_ResourceProductionData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[NativeDisableContainerSafetyRestriction]
		private NativeList<ResourceProductionData> m_ResourceProductionBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!m_ResourceProductionBuffer.IsCreated)
			{
				m_ResourceProductionBuffer = new NativeList<ResourceProductionData>(Allocator.Temp);
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourcesType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				DynamicBuffer<Resources> resources = bufferAccessor2[i];
				if (m_ResourceProductionData.HasBuffer(prefabRef.m_Prefab))
				{
					ResourceProductionData.Combine(m_ResourceProductionBuffer, m_ResourceProductionData[prefabRef.m_Prefab]);
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<InstalledUpgrade> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						InstalledUpgrade installedUpgrade = dynamicBuffer[j];
						if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
						{
							PrefabRef prefabRef2 = m_PrefabRefData[installedUpgrade.m_Upgrade];
							if (m_ResourceProductionData.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData))
							{
								ResourceProductionData.Combine(m_ResourceProductionBuffer, bufferData);
							}
						}
					}
				}
				for (int k = 0; k < m_ResourceProductionBuffer.Length; k++)
				{
					ResourceProductionData resourceProductionData = m_ResourceProductionBuffer[k];
					int resources2 = EconomyUtils.GetResources(resourceProductionData.m_Type, resources);
					if (resources2 >= math.min(20000, resourceProductionData.m_StorageCapacity))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new ResourceExporter
						{
							m_Resource = resourceProductionData.m_Type,
							m_Amount = resources2
						});
					}
				}
				if (m_CityServiceUpkeeps.HasComponent(entity))
				{
					int resources3 = EconomyUtils.GetResources(Resource.Money, resources);
					if (resources3 > 10000)
					{
						m_PlayerMoneyAddQueue.Enqueue(resources3);
						EconomyUtils.SetResources(Resource.Money, resources, 0);
					}
				}
				m_ResourceProductionBuffer.Clear();
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> __Game_Prefabs_ResourceProductionData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ResourceProductionData_RO_BufferLookup = state.GetBufferLookup<ResourceProductionData>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(isReadOnly: true);
			__Game_City_PlayerMoney_RW_ComponentLookup = state.GetComponentLookup<PlayerMoney>();
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private EntityQuery m_ResourceProducerQuery;

	private NativeQueue<int> m_PlayerMoneyAddQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_PlayerMoneyAddQueue = new NativeQueue<int>(Allocator.Persistent);
		m_ResourceProducerQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ResourceProducer>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_ResourceProducerQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_PlayerMoneyAddQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ResourceProducerQuery.ResetFilter();
		m_ResourceProducerQuery.SetSharedComponentFilter(new UpdateFrame(SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16)));
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new ResourceProducerJob
		{
			m_PlayerMoneyAddQueue = m_PlayerMoneyAddQueue.AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceProductionData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ResourceProductionData_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityServiceUpkeeps = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_ResourceProducerQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(new PlayerMoneyAddJob
		{
			m_PlayerMoneyAddQueue = m_PlayerMoneyAddQueue,
			m_PlayerMoneys = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City
		}, dependsOn);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public ResourceProducerSystem()
	{
	}
}
