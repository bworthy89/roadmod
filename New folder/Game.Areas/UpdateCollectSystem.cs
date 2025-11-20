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

namespace Game.Areas;

[CompilerGenerated]
public class UpdateCollectSystem : GameSystemBase
{
	private struct UpdateBufferData
	{
		public NativeList<Bounds2> m_Bounds;

		public EntityQuery m_Query;

		public JobHandle m_WriteDependencies;

		public JobHandle m_ReadDependencies;

		public bool m_IsUpdated;

		public void Create(EntityQuery query)
		{
			m_Bounds = new NativeList<Bounds2>(Allocator.Persistent);
			m_Query = query;
		}

		public void Dispose()
		{
			m_Bounds.Dispose();
		}

		public void Clear()
		{
			m_WriteDependencies.Complete();
			m_ReadDependencies.Complete();
			m_Bounds.Clear();
			m_IsUpdated = false;
		}
	}

	[BurstCompile]
	private struct CollectUpdatedAreaBoundsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public NativeParallelHashMap<Entity, int> m_TriangleCount;

		public NativeQueue<Bounds2>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
				BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<Node> nodes = bufferAccessor[i];
					DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor2[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Bounds3 bounds = MathUtils.Bounds(AreaUtils.GetTriangle3(nodes, dynamicBuffer[j]));
						m_ResultQueue.Enqueue(bounds.xz);
					}
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity = nativeArray[k];
					if (!m_TriangleCount.TryGetValue(entity, out var item))
					{
						continue;
					}
					for (int l = 0; l < item; l++)
					{
						if (m_SearchTree.TryGet(new AreaSearchItem(entity, l), out var bounds2))
						{
							m_ResultQueue.Enqueue(bounds2.m_Bounds.xz);
						}
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Node> bufferAccessor3 = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor4 = chunk.GetBufferAccessor(ref m_TriangleType);
			for (int m = 0; m < nativeArray2.Length; m++)
			{
				Entity entity2 = nativeArray2[m];
				DynamicBuffer<Node> nodes2 = bufferAccessor3[m];
				DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor4[m];
				if (m_TriangleCount.TryGetValue(entity2, out var item2))
				{
					int num = math.min(item2, dynamicBuffer2.Length);
					for (int n = 0; n < num; n++)
					{
						Bounds2 xz = MathUtils.Bounds(AreaUtils.GetTriangle3(nodes2, dynamicBuffer2[n])).xz;
						if (m_SearchTree.TryGet(new AreaSearchItem(entity2, n), out var bounds3))
						{
							Bounds2 xz2 = bounds3.m_Bounds.xz;
							Bounds2 bounds4 = xz2 | xz;
							if (math.length(MathUtils.Size(bounds4)) < math.length(MathUtils.Size(xz2)) + math.length(MathUtils.Size(xz)))
							{
								m_ResultQueue.Enqueue(bounds4);
								continue;
							}
							m_ResultQueue.Enqueue(xz2);
							m_ResultQueue.Enqueue(xz);
						}
						else
						{
							m_ResultQueue.Enqueue(xz);
						}
					}
					for (int num2 = num; num2 < item2; num2++)
					{
						if (m_SearchTree.TryGet(new AreaSearchItem(entity2, num2), out var bounds5))
						{
							m_ResultQueue.Enqueue(bounds5.m_Bounds.xz);
						}
					}
					for (int num3 = num; num3 < dynamicBuffer2.Length; num3++)
					{
						Bounds3 bounds6 = MathUtils.Bounds(AreaUtils.GetTriangle3(nodes2, dynamicBuffer2[num3]));
						m_ResultQueue.Enqueue(bounds6.xz);
					}
				}
				else
				{
					for (int num4 = 0; num4 < dynamicBuffer2.Length; num4++)
					{
						Bounds3 bounds7 = MathUtils.Bounds(AreaUtils.GetTriangle3(nodes2, dynamicBuffer2[num4]));
						m_ResultQueue.Enqueue(bounds7.xz);
					}
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
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
		}
	}

	private SearchSystem m_SearchSystem;

	private UpdateBufferData m_LotData;

	private UpdateBufferData m_DistrictData;

	private UpdateBufferData m_MapTileData;

	private UpdateBufferData m_SpaceData;

	private TypeHandle __TypeHandle;

	public bool lotsUpdated => m_LotData.m_IsUpdated;

	public bool districtsUpdated => m_DistrictData.m_IsUpdated;

	public bool mapTilesUpdated => m_MapTileData.m_IsUpdated;

	public bool spacesUpdated => m_SpaceData.m_IsUpdated;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_LotData.Create(GetQuery<Lot>());
		m_DistrictData.Create(GetQuery<District>());
		m_MapTileData.Create(GetQuery<MapTile>());
		m_SpaceData.Create(GetQuery<Space>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LotData.Dispose();
		m_DistrictData.Dispose();
		m_MapTileData.Dispose();
		m_SpaceData.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_LotData.Clear();
		m_DistrictData.Clear();
		m_MapTileData.Clear();
		m_SpaceData.Clear();
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = base.Dependency;
		if (m_LotData.m_Query.IsEmptyIgnoreFilter)
		{
			m_LotData.Clear();
		}
		else
		{
			JobHandle job = UpdateBounds(ref m_LotData, dependency);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, job);
		}
		if (m_DistrictData.m_Query.IsEmptyIgnoreFilter)
		{
			m_DistrictData.Clear();
		}
		else
		{
			JobHandle job2 = UpdateBounds(ref m_DistrictData, dependency);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, job2);
		}
		if (m_MapTileData.m_Query.IsEmptyIgnoreFilter)
		{
			m_MapTileData.Clear();
		}
		else
		{
			JobHandle job3 = UpdateBounds(ref m_MapTileData, dependency);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, job3);
		}
		if (m_SpaceData.m_Query.IsEmptyIgnoreFilter)
		{
			m_SpaceData.Clear();
			return;
		}
		JobHandle job4 = UpdateBounds(ref m_SpaceData, dependency);
		base.Dependency = JobHandle.CombineDependencies(base.Dependency, job4);
	}

	private JobHandle UpdateBounds(ref UpdateBufferData data, JobHandle inputDeps)
	{
		data.m_IsUpdated = true;
		NativeQueue<Bounds2> queue = new NativeQueue<Bounds2>(Allocator.TempJob);
		JobHandle dependencies;
		NativeParallelHashMap<Entity, int> triangleCount;
		NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> searchTree = m_SearchSystem.GetSearchTree(readOnly: true, out dependencies, out triangleCount);
		CollectUpdatedAreaBoundsJob jobData = new CollectUpdatedAreaBoundsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SearchTree = searchTree,
			m_TriangleCount = triangleCount,
			m_ResultQueue = queue.AsParallelWriter()
		};
		DequeueBoundsJob jobData2 = new DequeueBoundsJob
		{
			m_Queue = queue,
			m_ResultList = data.m_Bounds
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, data.m_Query, JobHandle.CombineDependencies(inputDeps, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, data.m_ReadDependencies));
		queue.Dispose(jobHandle2);
		m_SearchSystem.AddSearchTreeReader(jobHandle);
		data.m_WriteDependencies = jobHandle2;
		data.m_ReadDependencies = default(JobHandle);
		return jobHandle2;
	}

	public NativeList<Bounds2> GetUpdatedLotBounds(out JobHandle dependencies)
	{
		dependencies = m_LotData.m_WriteDependencies;
		return m_LotData.m_Bounds;
	}

	public NativeList<Bounds2> GetUpdatedDistrictBounds(out JobHandle dependencies)
	{
		dependencies = m_DistrictData.m_WriteDependencies;
		return m_DistrictData.m_Bounds;
	}

	public NativeList<Bounds2> GetUpdatedMapTileBounds(out JobHandle dependencies)
	{
		dependencies = m_MapTileData.m_WriteDependencies;
		return m_MapTileData.m_Bounds;
	}

	public NativeList<Bounds2> GetUpdatedSpaceBounds(out JobHandle dependencies)
	{
		dependencies = m_SpaceData.m_WriteDependencies;
		return m_SpaceData.m_Bounds;
	}

	public void AddLotBoundsReader(JobHandle handle)
	{
		m_LotData.m_ReadDependencies = JobHandle.CombineDependencies(m_LotData.m_ReadDependencies, handle);
	}

	public void AddDistrictBoundsReader(JobHandle handle)
	{
		m_DistrictData.m_ReadDependencies = JobHandle.CombineDependencies(m_DistrictData.m_ReadDependencies, handle);
	}

	public void AddMapTileBoundsReader(JobHandle handle)
	{
		m_MapTileData.m_ReadDependencies = JobHandle.CombineDependencies(m_MapTileData.m_ReadDependencies, handle);
	}

	public void AddSpaceBoundsReader(JobHandle handle)
	{
		m_SpaceData.m_ReadDependencies = JobHandle.CombineDependencies(m_SpaceData.m_ReadDependencies, handle);
	}

	private EntityQuery GetQuery<T>()
	{
		return GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Triangle>(),
				ComponentType.ReadOnly<T>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
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
