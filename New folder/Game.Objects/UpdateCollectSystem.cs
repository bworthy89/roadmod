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

namespace Game.Objects;

[CompilerGenerated]
public class UpdateCollectSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectUpdatedObjectBoundsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<Bounds2>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					PrefabRef prefabRef = nativeArray[i];
					Transform transform = nativeArray2[i];
					if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						ObjectGeometryData geometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
						Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
						m_ResultQueue.Enqueue(bounds.xz);
					}
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
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Transform> nativeArray6 = chunk.GetNativeArray(ref m_TransformType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity item2 = nativeArray4[k];
				PrefabRef prefabRef2 = nativeArray5[k];
				Transform transform2 = nativeArray6[k];
				Bounds2 bounds3 = default(Bounds2);
				Bounds2 bounds4 = default(Bounds2);
				bool2 x = default(bool2);
				if (m_SearchTree.TryGet(item2, out var bounds5))
				{
					bounds3 = bounds5.m_Bounds.xz;
					x.x = true;
				}
				if (m_PrefabObjectGeometryData.HasComponent(prefabRef2.m_Prefab))
				{
					ObjectGeometryData geometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
					bounds4 = ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, geometryData2).xz;
					x.y = true;
				}
				if (math.all(x))
				{
					Bounds2 bounds6 = bounds3 | bounds4;
					if (math.length(MathUtils.Size(bounds6)) < math.length(MathUtils.Size(bounds3)) + math.length(MathUtils.Size(bounds4)))
					{
						m_ResultQueue.Enqueue(bounds6);
						continue;
					}
					m_ResultQueue.Enqueue(bounds3);
					m_ResultQueue.Enqueue(bounds4);
				}
				else if (x.x)
				{
					m_ResultQueue.Enqueue(bounds3);
				}
				else if (x.y)
				{
					m_ResultQueue.Enqueue(bounds4);
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
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_ObjectQuery;

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
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Static>(),
				ComponentType.ReadOnly<Transform>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedBounds = new NativeList<Bounds2>(Allocator.Persistent);
		RequireForUpdate(m_ObjectQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_UpdatedBounds.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_WriteDependencies.Complete();
		m_ReadDependencies.Complete();
		m_UpdatedBounds.Clear();
		isUpdated = false;
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		isUpdated = true;
		NativeQueue<Bounds2> queue = new NativeQueue<Bounds2>(Allocator.TempJob);
		JobHandle dependencies;
		CollectUpdatedObjectBoundsJob jobData = new CollectUpdatedObjectBoundsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SearchTree = m_SearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_ResultQueue = queue.AsParallelWriter()
		};
		DequeueBoundsJob jobData2 = new DequeueBoundsJob
		{
			m_Queue = queue,
			m_ResultList = m_UpdatedBounds
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_ObjectQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, m_ReadDependencies));
		queue.Dispose(jobHandle2);
		m_SearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_WriteDependencies = jobHandle2;
		m_ReadDependencies = default(JobHandle);
		base.Dependency = jobHandle;
	}

	public NativeList<Bounds2> GetUpdatedBounds(out JobHandle dependencies)
	{
		dependencies = m_WriteDependencies;
		return m_UpdatedBounds;
	}

	public void AddBoundsReader(JobHandle handle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, handle);
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
