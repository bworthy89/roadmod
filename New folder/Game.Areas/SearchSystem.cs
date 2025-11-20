using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
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
public class SearchSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	public struct UpdateSearchTreeJob : IJobChunk
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
		public ComponentTypeHandle<Batch> m_BatchType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public bool m_Loaded;

		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		public NativeParallelHashMap<Entity, int> m_TriangleCount;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					if (m_TriangleCount.TryGetValue(entity, out var item))
					{
						for (int j = 0; j < item; j++)
						{
							m_SearchTree.Remove(new AreaSearchItem(entity, j));
						}
						m_TriangleCount.Remove(entity);
					}
				}
				return;
			}
			if (m_Loaded || chunk.Has(ref m_CreatedType))
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
				BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
				BoundsMask boundsMask = BoundsMask.Debug | BoundsMask.NotOverridden | BoundsMask.NotWalkThrough;
				if (chunk.Has(ref m_BatchType))
				{
					boundsMask |= BoundsMask.NormalLayers;
				}
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					Entity entity2 = nativeArray2[k];
					PrefabRef prefabRef = nativeArray3[k];
					DynamicBuffer<Node> nodes = bufferAccessor[k];
					DynamicBuffer<Triangle> dynamicBuffer = bufferAccessor2[k];
					AreaGeometryData areaData = m_PrefabAreaGeometryData[prefabRef.m_Prefab];
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						Triangle triangle = dynamicBuffer[l];
						Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
						Bounds3 bounds = AreaUtils.GetBounds(triangle, triangle2, areaData);
						m_SearchTree.Add(new AreaSearchItem(entity2, l), new QuadTreeBoundsXZ(bounds, boundsMask, triangle.m_MinLod));
					}
					m_TriangleCount.TryAdd(entity2, dynamicBuffer.Length);
				}
				return;
			}
			NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor3 = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor4 = chunk.GetBufferAccessor(ref m_TriangleType);
			BoundsMask boundsMask2 = BoundsMask.Debug | BoundsMask.NotOverridden | BoundsMask.NotWalkThrough;
			if (chunk.Has(ref m_BatchType))
			{
				boundsMask2 |= BoundsMask.NormalLayers;
			}
			for (int m = 0; m < nativeArray4.Length; m++)
			{
				Entity entity3 = nativeArray4[m];
				PrefabRef prefabRef2 = nativeArray5[m];
				DynamicBuffer<Node> nodes2 = bufferAccessor3[m];
				DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor4[m];
				AreaGeometryData areaData2 = m_PrefabAreaGeometryData[prefabRef2.m_Prefab];
				if (m_TriangleCount.TryGetValue(entity3, out var item2))
				{
					for (int n = dynamicBuffer2.Length; n < item2; n++)
					{
						m_SearchTree.Remove(new AreaSearchItem(entity3, n));
					}
					m_TriangleCount.Remove(entity3);
				}
				else
				{
					item2 = 0;
				}
				int num = math.min(item2, dynamicBuffer2.Length);
				for (int num2 = 0; num2 < num; num2++)
				{
					Triangle triangle3 = dynamicBuffer2[num2];
					Triangle3 triangle4 = AreaUtils.GetTriangle3(nodes2, triangle3);
					Bounds3 bounds2 = AreaUtils.GetBounds(triangle3, triangle4, areaData2);
					m_SearchTree.Update(new AreaSearchItem(entity3, num2), new QuadTreeBoundsXZ(bounds2, boundsMask2, triangle3.m_MinLod));
				}
				for (int num3 = item2; num3 < dynamicBuffer2.Length; num3++)
				{
					Triangle triangle5 = dynamicBuffer2[num3];
					Triangle3 triangle6 = AreaUtils.GetTriangle3(nodes2, triangle5);
					Bounds3 bounds3 = AreaUtils.GetBounds(triangle5, triangle6, areaData2);
					m_SearchTree.Add(new AreaSearchItem(entity3, num3), new QuadTreeBoundsXZ(bounds3, boundsMask2, triangle5.m_MinLod));
				}
				m_TriangleCount.TryAdd(entity3, dynamicBuffer2.Length);
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
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Batch> __Game_Areas_Batch_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Areas_Batch_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Batch>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedAreasQuery;

	private EntityQuery m_AllAreasQuery;

	private NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_SearchTree;

	private NativeParallelHashMap<Entity, int> m_TriangleCount;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedAreasQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Triangle>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllAreasQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Triangle>(), ComponentType.Exclude<Temp>());
		m_SearchTree = new NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
		m_TriangleCount = new NativeParallelHashMap<Entity, int>(100, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SearchTree.Dispose();
		m_TriangleCount.Dispose();
		base.OnDestroy();
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		EntityQuery query = (loaded ? m_AllAreasQuery : m_UpdatedAreasQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			NativeParallelHashMap<Entity, int> triangleCount;
			UpdateSearchTreeJob jobData = new UpdateSearchTreeJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BatchType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Batch_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Loaded = loaded,
				m_SearchTree = GetSearchTree(readOnly: false, out dependencies, out triangleCount),
				m_TriangleCount = triangleCount
			};
			base.Dependency = JobChunkExtensions.Schedule(jobData, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
			AddSearchTreeWriter(base.Dependency);
		}
	}

	public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> GetSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		return m_SearchTree;
	}

	public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> GetSearchTree(bool readOnly, out JobHandle dependencies, out NativeParallelHashMap<Entity, int> triangleCount)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		triangleCount = m_TriangleCount;
		return m_SearchTree;
	}

	public void AddSearchTreeReader(JobHandle jobHandle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
	}

	public void AddSearchTreeWriter(JobHandle jobHandle)
	{
		m_WriteDependencies = JobHandle.CombineDependencies(m_WriteDependencies, jobHandle);
	}

	public void PreDeserialize(Context context)
	{
		JobHandle dependencies;
		NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> searchTree = GetSearchTree(readOnly: false, out dependencies);
		dependencies.Complete();
		searchTree.Clear();
		m_Loaded = true;
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
	public SearchSystem()
	{
	}
}
