using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Zones;

[CompilerGenerated]
public class UpdateCollectSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectUpdatedBlockBoundsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		public NativeQueue<Bounds2>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<Block> nativeArray = chunk.GetNativeArray(ref m_BlockType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Block block = nativeArray[i];
					m_ResultQueue.Enqueue(ZoneUtils.CalculateBounds(block));
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity item = nativeArray2[j];
					if (m_SearchTree.TryGet(item, out var bounds))
					{
						m_ResultQueue.Enqueue(bounds);
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Block> nativeArray4 = chunk.GetNativeArray(ref m_BlockType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity item2 = nativeArray3[k];
				Bounds2 bounds2 = ZoneUtils.CalculateBounds(nativeArray4[k]);
				if (m_SearchTree.TryGet(item2, out var bounds3))
				{
					Bounds2 bounds4 = bounds3 | bounds2;
					if (math.length(MathUtils.Size(bounds4)) < math.length(MathUtils.Size(bounds3)) + math.length(MathUtils.Size(bounds2)))
					{
						m_ResultQueue.Enqueue(bounds4);
						continue;
					}
					m_ResultQueue.Enqueue(bounds3);
					m_ResultQueue.Enqueue(bounds2);
				}
				else
				{
					m_ResultQueue.Enqueue(bounds2);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DequeueBoundsJob : IJob
	{
		public NativeQueue<Bounds2> m_Queue;

		public NativeList<Bounds2> m_ResultList;

		public void Execute()
		{
			int count = m_Queue.Count;
			m_ResultList.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_ResultList[i] = m_Queue.Dequeue();
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
		}
	}

	private EntityQuery m_BlockQuery;

	private SearchSystem m_SearchSystem;

	private NativeList<Bounds2> m_UpdatedBounds;

	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private TypeHandle __TypeHandle;

	public bool isUpdated { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_BlockQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Block>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedBounds = new NativeList<Bounds2>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_WriteDependencies.Complete();
		m_ReadDependencies.Complete();
		m_UpdatedBounds.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_BlockQuery.IsEmptyIgnoreFilter)
		{
			m_WriteDependencies.Complete();
			m_ReadDependencies.Complete();
			m_UpdatedBounds.Clear();
			isUpdated = false;
			return;
		}
		isUpdated = true;
		NativeQueue<Bounds2> queue = new NativeQueue<Bounds2>(Allocator.TempJob);
		JobHandle dependencies;
		CollectUpdatedBlockBoundsJob jobData = new CollectUpdatedBlockBoundsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SearchTree = m_SearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_ResultQueue = queue.AsParallelWriter()
		};
		DequeueBoundsJob jobData2 = new DequeueBoundsJob
		{
			m_Queue = queue,
			m_ResultList = m_UpdatedBounds
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_BlockQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, m_WriteDependencies, m_ReadDependencies));
		queue.Dispose(jobHandle2);
		m_SearchSystem.AddSearchTreeReader(jobHandle);
		m_WriteDependencies = jobHandle2;
		m_ReadDependencies = default(JobHandle);
		base.Dependency = jobHandle;
	}

	public NativeList<Bounds2> GetUpdatedBounds(bool readOnly, out JobHandle dependencies)
	{
		if (readOnly)
		{
			dependencies = m_WriteDependencies;
		}
		else
		{
			dependencies = JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies);
			isUpdated = true;
		}
		return m_UpdatedBounds;
	}

	public void AddBoundsReader(JobHandle handle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, handle);
	}

	public void AddBoundsWriter(JobHandle handle)
	{
		m_WriteDependencies = handle;
		m_ReadDependencies = default(JobHandle);
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
	public UpdateCollectSystem()
	{
	}
}
