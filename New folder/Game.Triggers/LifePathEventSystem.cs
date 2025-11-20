#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class LifePathEventSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateLifePathEventJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<LifePathEventData> m_LifePathEventDatas;

		[ReadOnly]
		public BufferLookup<LifePathEntry> m_LifePathEntries;

		public EntityCommandBuffer m_CommandBuffer;

		[ReadOnly]
		public EntityArchetype m_EventArchetype;

		public NativeQueue<LifePathEventCreationData> m_Queue;

		public NativeQueue<ChirpCreationData> m_ChirpQueue;

		[ReadOnly]
		public TimeData m_TimeData;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public bool m_DebugLifePathChirps;

		public void Execute()
		{
			LifePathEventCreationData item;
			while (m_Queue.TryDequeue(out item))
			{
				LifePathEventData lifePathEventData = m_LifePathEventDatas[item.m_EventPrefab];
				bool flag = m_LifePathEntries.HasBuffer(item.m_Sender);
				if (m_DebugLifePathChirps || flag)
				{
					if (lifePathEventData.m_IsChirp)
					{
						m_ChirpQueue.Enqueue(new ChirpCreationData
						{
							m_TriggerPrefab = item.m_EventPrefab,
							m_Sender = item.m_Sender,
							m_Target = item.m_Target
						});
					}
					else if (flag)
					{
						Entity entity = m_CommandBuffer.CreateEntity(m_EventArchetype);
						m_CommandBuffer.SetComponent(entity, new LifePathEvent
						{
							m_EventPrefab = item.m_EventPrefab,
							m_Target = item.m_Target,
							m_Date = (uint)TimeSystem.GetDay(m_SimulationFrame, m_TimeData)
						});
						m_CommandBuffer.AppendToBuffer(item.m_Sender, new LifePathEntry(entity));
					}
					m_CommandBuffer.AddComponent<Updated>(item.m_Sender);
				}
			}
		}
	}

	[BurstCompile]
	private struct CleanupLifePathEntriesJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<LifePathEntry> m_EntryType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<LifePathEntry> bufferAccessor = chunk.GetBufferAccessor(ref m_EntryType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				for (int j = 0; j < bufferAccessor[i].Length; j++)
				{
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, bufferAccessor[i][j].m_Entity);
				}
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
		public BufferTypeHandle<LifePathEntry> __Game_Triggers_LifePathEntry_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<LifePathEventData> __Game_Prefabs_LifePathEventData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LifePathEntry> __Game_Triggers_LifePathEntry_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Triggers_LifePathEntry_RO_BufferTypeHandle = state.GetBufferTypeHandle<LifePathEntry>(isReadOnly: true);
			__Game_Prefabs_LifePathEventData_RO_ComponentLookup = state.GetComponentLookup<LifePathEventData>(isReadOnly: true);
			__Game_Triggers_LifePathEntry_RO_BufferLookup = state.GetBufferLookup<LifePathEntry>(isReadOnly: true);
		}
	}

	public static readonly int kMaxFollowed = 50;

	private SimulationSystem m_SimulationSystem;

	private ModificationEndBarrier m_ModificationBarrier;

	private CreateChirpSystem m_CreateChirpSystem;

	private EntityQuery m_FollowedQuery;

	private EntityQuery m_DeletedFollowedQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityArchetype m_EventArchetype;

	private NativeQueue<LifePathEventCreationData> m_Queue;

	private JobHandle m_WriteDependencies;

	private TypeHandle __TypeHandle;

	public bool m_DebugLifePathChirps { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_CreateChirpSystem = base.World.GetOrCreateSystemManaged<CreateChirpSystem>();
		m_FollowedQuery = GetEntityQuery(ComponentType.ReadOnly<Followed>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DeletedFollowedQuery = GetEntityQuery(ComponentType.ReadOnly<Followed>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_EventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<LifePathEvent>());
		m_Queue = new NativeQueue<LifePathEventCreationData>(Allocator.Persistent);
		RequireForUpdate(m_TimeDataQuery);
		base.Enabled = false;
	}

	protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_WriteDependencies.Complete();
		m_Queue.Dispose();
		base.OnDestroy();
	}

	public NativeQueue<LifePathEventCreationData> GetQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_WriteDependencies;
		return m_Queue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, handle);
	}

	public bool FollowCitizen(Entity citizen)
	{
		if (m_FollowedQuery.CalculateEntityCount() < kMaxFollowed)
		{
			Citizen component;
			bool startedFollowingAsChild = base.EntityManager.TryGetComponent<Citizen>(citizen, out component) && component.GetAge() == CitizenAge.Child;
			base.EntityManager.AddComponentData(citizen, new Followed
			{
				m_Priority = m_SimulationSystem.frameIndex,
				m_StartedFollowingAsChild = startedFollowingAsChild
			});
			base.EntityManager.AddBuffer<LifePathEntry>(citizen);
			base.EntityManager.AddComponent<Updated>(citizen);
			return true;
		}
		return false;
	}

	public bool UnfollowCitizen(Entity citizen)
	{
		if (base.EntityManager.HasComponent<Followed>(citizen))
		{
			base.EntityManager.RemoveComponent<Followed>(citizen);
			if (base.EntityManager.TryGetBuffer(citizen, isReadOnly: true, out DynamicBuffer<LifePathEntry> buffer))
			{
				foreach (LifePathEntry item in buffer)
				{
					base.EntityManager.AddComponent<Deleted>(item.m_Entity);
				}
			}
			base.EntityManager.RemoveComponent<LifePathEntry>(citizen);
			base.EntityManager.AddComponent<Updated>(citizen);
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CleanupLifePathEntriesJob jobData = new CleanupLifePathEntriesJob
		{
			m_EntryType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Triggers_LifePathEntry_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_DeletedFollowedQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		JobHandle deps;
		CreateLifePathEventJob jobData2 = new CreateLifePathEventJob
		{
			m_LifePathEventDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LifePathEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LifePathEntries = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Triggers_LifePathEntry_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_EventArchetype = m_EventArchetype,
			m_Queue = m_Queue,
			m_ChirpQueue = m_CreateChirpSystem.GetQueue(out deps),
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_DebugLifePathChirps = m_DebugLifePathChirps
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, m_WriteDependencies, deps));
		m_CreateChirpSystem.AddQueueWriter(base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		m_WriteDependencies = base.Dependency;
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
	public LifePathEventSystem()
	{
	}
}
