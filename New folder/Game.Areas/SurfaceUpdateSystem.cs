using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
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
public class SurfaceUpdateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateAreasJob : IJobChunk
	{
		private struct SurfaceIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float3 m_Position;

			public int m_JobIndex;

			public ComponentLookup<Surface> m_SurfaceData;

			public BufferLookup<Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz) && m_SurfaceData.HasComponent(item.m_Area))
				{
					DynamicBuffer<Node> nodes = m_AreaNodes[item.m_Area];
					Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
					if (MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, triangle), m_Position.xz))
					{
						m_CommandBuffer.AddComponent(m_JobIndex, item.m_Area, default(Updated));
					}
				}
			}
		}

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<Surface> m_SurfaceData;

		[ReadOnly]
		public BufferLookup<Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Net.Node> nativeArray = chunk.GetNativeArray(ref m_NodeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				UpdateSurfaces(unfilteredChunkIndex, nativeArray[i].m_Position);
			}
		}

		private void UpdateSurfaces(int jobIndex, float3 position)
		{
			SurfaceIterator iterator = new SurfaceIterator
			{
				m_Position = position,
				m_JobIndex = jobIndex,
				m_SurfaceData = m_SurfaceData,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_CommandBuffer = m_CommandBuffer
			};
			m_AreaSearchTree.Iterate(ref iterator);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Surface> __Game_Areas_Surface_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Node>(isReadOnly: true);
			__Game_Areas_Surface_RO_ComponentLookup = state.GetComponentLookup<Surface>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private SearchSystem m_AreaSearchSystem;

	private EntityQuery m_UpdatedNetQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_UpdatedNetQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireForUpdate(m_UpdatedNetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateAreasJob
		{
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SurfaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Surface_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_UpdatedNetQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public SurfaceUpdateSystem()
	{
	}
}
