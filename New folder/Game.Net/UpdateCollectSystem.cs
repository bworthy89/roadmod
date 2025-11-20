using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class UpdateCollectSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectUpdatedNetBoundsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> m_StartGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> m_EndGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<Bounds2>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<EdgeGeometry> nativeArray = chunk.GetNativeArray(ref m_EdgeGeometryType);
				if (nativeArray.Length != 0)
				{
					NativeArray<StartNodeGeometry> nativeArray2 = chunk.GetNativeArray(ref m_StartGeometryType);
					NativeArray<EndNodeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_EndGeometryType);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Bounds3 bounds = nativeArray[i].m_Bounds | nativeArray2[i].m_Geometry.m_Bounds | nativeArray3[i].m_Geometry.m_Bounds;
						m_ResultQueue.Enqueue(bounds.xz);
					}
				}
				else
				{
					NativeArray<NodeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_NodeGeometryType);
					for (int j = 0; j < nativeArray4.Length; j++)
					{
						Bounds3 bounds2 = nativeArray4[j].m_Bounds;
						m_ResultQueue.Enqueue(bounds2.xz);
					}
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
				for (int k = 0; k < nativeArray5.Length; k++)
				{
					Entity item = nativeArray5[k];
					if (m_SearchTree.TryGet(item, out var bounds3))
					{
						m_ResultQueue.Enqueue(bounds3.m_Bounds.xz);
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray6 = chunk.GetNativeArray(m_EntityType);
			NativeArray<EdgeGeometry> nativeArray7 = chunk.GetNativeArray(ref m_EdgeGeometryType);
			if (nativeArray7.Length != 0)
			{
				NativeArray<StartNodeGeometry> nativeArray8 = chunk.GetNativeArray(ref m_StartGeometryType);
				NativeArray<EndNodeGeometry> nativeArray9 = chunk.GetNativeArray(ref m_EndGeometryType);
				for (int l = 0; l < nativeArray6.Length; l++)
				{
					Entity item2 = nativeArray6[l];
					Bounds2 xz = (nativeArray7[l].m_Bounds | nativeArray8[l].m_Geometry.m_Bounds | nativeArray9[l].m_Geometry.m_Bounds).xz;
					if (m_SearchTree.TryGet(item2, out var bounds4))
					{
						Bounds2 xz2 = bounds4.m_Bounds.xz;
						Bounds2 bounds5 = xz2 | xz;
						if (math.length(MathUtils.Size(bounds5)) < math.length(MathUtils.Size(xz2)) + math.length(MathUtils.Size(xz)))
						{
							m_ResultQueue.Enqueue(bounds5);
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
				return;
			}
			NativeArray<NodeGeometry> nativeArray10 = chunk.GetNativeArray(ref m_NodeGeometryType);
			for (int m = 0; m < nativeArray6.Length; m++)
			{
				Entity item3 = nativeArray6[m];
				Bounds2 xz3 = nativeArray10[m].m_Bounds.xz;
				if (m_SearchTree.TryGet(item3, out var bounds6))
				{
					Bounds2 xz4 = bounds6.m_Bounds.xz;
					Bounds2 bounds7 = xz4 | xz3;
					if (math.length(MathUtils.Size(bounds7)) < math.length(MathUtils.Size(xz4)) + math.length(MathUtils.Size(xz3)))
					{
						m_ResultQueue.Enqueue(bounds7);
						continue;
					}
					m_ResultQueue.Enqueue(xz4);
					m_ResultQueue.Enqueue(xz3);
				}
				else
				{
					m_ResultQueue.Enqueue(xz3);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CollectUpdatedLaneBoundsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<Bounds2>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<Curve> nativeArray = chunk.GetNativeArray(ref m_CurveType);
				NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Bounds3 bounds = MathUtils.Bounds(nativeArray[i].m_Bezier);
					PrefabRef prefabRef = nativeArray2[i];
					if (m_PrefabLaneGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						bounds = MathUtils.Expand(bounds, componentData.m_Size.x * 0.5f);
					}
					m_ResultQueue.Enqueue(bounds.xz);
				}
				return;
			}
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					Entity item = nativeArray3[j];
					if (m_SearchTree.TryGet(item, out var bounds2))
					{
						m_ResultQueue.Enqueue(bounds2.m_Bounds.xz);
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Curve> nativeArray5 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity item2 = nativeArray4[k];
				Bounds2 bounds3 = MathUtils.Bounds(nativeArray5[k].m_Bezier).xz;
				PrefabRef prefabRef2 = nativeArray6[k];
				if (m_PrefabLaneGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
				{
					bounds3 = MathUtils.Expand(bounds3, componentData2.m_Size.x * 0.5f);
				}
				if (m_SearchTree.TryGet(item2, out var bounds4))
				{
					Bounds2 xz = bounds4.m_Bounds.xz;
					Bounds2 bounds5 = xz | bounds3;
					if (math.length(MathUtils.Size(bounds5)) < math.length(MathUtils.Size(xz)) + math.length(MathUtils.Size(bounds3)))
					{
						m_ResultQueue.Enqueue(bounds5);
						continue;
					}
					m_ResultQueue.Enqueue(xz);
					m_ResultQueue.Enqueue(bounds3);
				}
				else
				{
					m_ResultQueue.Enqueue(bounds3);
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
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeGeometry>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_NetGeometryQuery;

	private EntityQuery m_LaneGeometryQuery;

	private SearchSystem m_SearchSystem;

	private NativeList<Bounds2> m_UpdatedNetBounds;

	private JobHandle m_NetWriteDependencies;

	private JobHandle m_NetReadDependencies;

	private JobHandle m_LaneWriteDependencies;

	private JobHandle m_LaneReadDependencies;

	private NativeList<Bounds2> m_UpdatedLaneBounds;

	private TypeHandle __TypeHandle;

	public bool netsUpdated { get; private set; }

	public bool lanesUpdated { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_NetGeometryQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<EdgeGeometry>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<NodeGeometry>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_LaneGeometryQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<LaneGeometry>(),
				ComponentType.ReadOnly<UtilityLane>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedNetBounds = new NativeList<Bounds2>(Allocator.Persistent);
		m_UpdatedLaneBounds = new NativeList<Bounds2>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_NetWriteDependencies.Complete();
		m_NetReadDependencies.Complete();
		m_LaneWriteDependencies.Complete();
		m_LaneReadDependencies.Complete();
		m_UpdatedNetBounds.Dispose();
		m_UpdatedLaneBounds.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_NetGeometryQuery.IsEmptyIgnoreFilter;
		bool flag2 = !m_LaneGeometryQuery.IsEmptyIgnoreFilter;
		if (!flag && netsUpdated)
		{
			m_NetWriteDependencies.Complete();
			m_NetReadDependencies.Complete();
			m_UpdatedNetBounds.Clear();
			netsUpdated = false;
		}
		if (!flag2 && lanesUpdated)
		{
			m_LaneWriteDependencies.Complete();
			m_LaneReadDependencies.Complete();
			m_UpdatedLaneBounds.Clear();
			lanesUpdated = false;
		}
		if (flag || flag2)
		{
			JobHandle jobHandle = default(JobHandle);
			if (flag)
			{
				netsUpdated = true;
				NativeQueue<Bounds2> queue = new NativeQueue<Bounds2>(Allocator.TempJob);
				JobHandle dependencies;
				CollectUpdatedNetBoundsJob jobData = new CollectUpdatedNetBoundsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_StartGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EndGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SearchTree = m_SearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
					m_ResultQueue = queue.AsParallelWriter()
				};
				DequeueBoundsJob jobData2 = new DequeueBoundsJob
				{
					m_Queue = queue,
					m_ResultList = m_UpdatedNetBounds
				};
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_NetGeometryQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
				JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle2, m_NetReadDependencies));
				queue.Dispose(jobHandle3);
				m_SearchSystem.AddNetSearchTreeReader(jobHandle2);
				m_NetWriteDependencies = jobHandle3;
				m_NetReadDependencies = default(JobHandle);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			if (flag2)
			{
				lanesUpdated = true;
				NativeQueue<Bounds2> queue2 = new NativeQueue<Bounds2>(Allocator.TempJob);
				JobHandle dependencies2;
				CollectUpdatedLaneBoundsJob jobData3 = new CollectUpdatedLaneBoundsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SearchTree = m_SearchSystem.GetLaneSearchTree(readOnly: true, out dependencies2),
					m_ResultQueue = queue2.AsParallelWriter()
				};
				DequeueBoundsJob jobData4 = new DequeueBoundsJob
				{
					m_Queue = queue2,
					m_ResultList = m_UpdatedLaneBounds
				};
				JobHandle jobHandle4 = JobChunkExtensions.ScheduleParallel(jobData3, m_LaneGeometryQuery, JobHandle.CombineDependencies(base.Dependency, dependencies2));
				JobHandle jobHandle5 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(jobHandle4, m_LaneReadDependencies));
				queue2.Dispose(jobHandle5);
				m_SearchSystem.AddLaneSearchTreeReader(jobHandle4);
				m_LaneWriteDependencies = jobHandle5;
				m_LaneReadDependencies = default(JobHandle);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
			}
			base.Dependency = jobHandle;
		}
	}

	public NativeList<Bounds2> GetUpdatedNetBounds(out JobHandle dependencies)
	{
		dependencies = m_NetWriteDependencies;
		return m_UpdatedNetBounds;
	}

	public void AddNetBoundsReader(JobHandle handle)
	{
		m_NetReadDependencies = JobHandle.CombineDependencies(m_NetReadDependencies, handle);
	}

	public NativeList<Bounds2> GetUpdatedLaneBounds(out JobHandle dependencies)
	{
		dependencies = m_LaneWriteDependencies;
		return m_UpdatedLaneBounds;
	}

	public void AddLaneBoundsReader(JobHandle handle)
	{
		m_LaneReadDependencies = JobHandle.CombineDependencies(m_LaneReadDependencies, handle);
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
