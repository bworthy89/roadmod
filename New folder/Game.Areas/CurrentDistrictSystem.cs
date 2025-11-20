using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
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
public class CurrentDistrictSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindUpdatedDistrictItemsJob : IJobParallelForDefer
	{
		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

			public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_CurrentDistrictData.HasComponent(entity))
				{
					m_UpdateBuffer.Enqueue(entity);
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<BorderDistrict> m_BorderDistrictData;

			public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_BorderDistrictData.HasComponent(entity))
				{
					m_UpdateBuffer.Enqueue(entity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetTree;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

		public void Execute(int index)
		{
			ObjectIterator iterator = new ObjectIterator
			{
				m_Bounds = m_Bounds[index],
				m_CurrentDistrictData = m_CurrentDistrictData,
				m_UpdateBuffer = m_UpdateBuffer
			};
			m_ObjectTree.Iterate(ref iterator);
			NetIterator iterator2 = new NetIterator
			{
				m_Bounds = m_Bounds[index],
				m_BorderDistrictData = m_BorderDistrictData,
				m_UpdateBuffer = m_UpdateBuffer
			};
			m_NetTree.Iterate(ref iterator2);
		}
	}

	[BurstCompile]
	private struct CollectUpdatedDistrictItemsJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_UpdateBuffer;

		public NativeList<Entity> m_UpdateList;

		public void Execute()
		{
			int count = m_UpdateBuffer.Count;
			m_UpdateList.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_UpdateList[i] = m_UpdateBuffer.Dequeue();
			}
			m_UpdateList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_UpdateList.Length)
			{
				Entity entity2 = m_UpdateList[num++];
				if (entity2 != entity)
				{
					m_UpdateList[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_UpdateList.Length)
			{
				m_UpdateList.RemoveRange(num2, m_UpdateList.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct FindDistrictParallelJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<Entity> m_UpdateList;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		public void Execute(int index)
		{
			Entity entity = m_UpdateList[index];
			DistrictIterator iterator = new DistrictIterator
			{
				m_DistrictData = m_DistrictData,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles
			};
			if (m_CurrentDistrictData.HasComponent(entity))
			{
				CurrentDistrict currentDistrict = m_CurrentDistrictData[entity];
				iterator.m_Position = m_TransformData[entity].m_Position.xz;
				iterator.m_Result = Entity.Null;
				m_AreaTree.Iterate(ref iterator);
				if (currentDistrict.m_District != iterator.m_Result)
				{
					m_CurrentDistrictData[entity] = new CurrentDistrict(iterator.m_Result);
					CheckChangedDistrict(index, entity);
				}
			}
			if (!m_BorderDistrictData.HasComponent(entity))
			{
				return;
			}
			BorderDistrict borderDistrict = m_BorderDistrictData[entity];
			EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
			iterator.m_Position = edgeGeometry.m_Start.m_Left.d.xz;
			iterator.m_Result = Entity.Null;
			m_AreaTree.Iterate(ref iterator);
			Entity entity2 = iterator.m_Result;
			iterator.m_Position = edgeGeometry.m_Start.m_Right.d.xz;
			iterator.m_Result = Entity.Null;
			m_AreaTree.Iterate(ref iterator);
			Entity entity3 = iterator.m_Result;
			if (borderDistrict.m_Left != entity2 || borderDistrict.m_Right != entity3)
			{
				m_BorderDistrictData[entity] = new BorderDistrict(entity2, entity3);
				if (CheckChangedDistrict(index, entity))
				{
					m_CommandBuffer.AddComponent<ChangedDistrict>(index, entity);
				}
			}
		}

		private bool CheckChangedDistrict(int jobIndex, Entity entity)
		{
			if (m_UpdatedData.HasComponent(entity))
			{
				return false;
			}
			bool result = false;
			if (m_SubLanes.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subLane = bufferData[i].m_SubLane;
					if (m_CarLaneData.HasComponent(subLane) || m_PedestrianLaneData.HasComponent(subLane) || m_ParkingLaneData.HasComponent(subLane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, subLane);
						result = true;
					}
				}
			}
			return result;
		}
	}

	[BurstCompile]
	private struct FindDistrictChunkJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		public ComponentTypeHandle<BorderDistrict> m_BorderDistrictType;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CurrentDistrict> nativeArray = chunk.GetNativeArray(ref m_CurrentDistrictType);
			NativeArray<BorderDistrict> nativeArray2 = chunk.GetNativeArray(ref m_BorderDistrictType);
			DistrictIterator iterator = new DistrictIterator
			{
				m_DistrictData = m_DistrictData,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles
			};
			if (nativeArray.Length != 0)
			{
				NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
				for (int i = 0; i < nativeArray3.Length; i++)
				{
					iterator.m_Position = nativeArray3[i].m_Position.xz;
					iterator.m_Result = Entity.Null;
					m_AreaTree.Iterate(ref iterator);
					nativeArray[i] = new CurrentDistrict(iterator.m_Result);
				}
			}
			if (nativeArray2.Length != 0)
			{
				NativeArray<EdgeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_EdgeGeometryType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					EdgeGeometry edgeGeometry = nativeArray4[j];
					iterator.m_Position = edgeGeometry.m_Start.m_Left.d.xz;
					iterator.m_Result = Entity.Null;
					m_AreaTree.Iterate(ref iterator);
					Entity left = iterator.m_Result;
					iterator.m_Position = edgeGeometry.m_Start.m_Right.d.xz;
					iterator.m_Result = Entity.Null;
					m_AreaTree.Iterate(ref iterator);
					Entity right = iterator.m_Result;
					nativeArray2[j] = new BorderDistrict(left, right);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct DistrictIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public float2 m_Position;

		public ComponentLookup<District> m_DistrictData;

		public BufferLookup<Node> m_Nodes;

		public BufferLookup<Triangle> m_Triangles;

		public Entity m_Result;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
		{
			if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position) && m_DistrictData.HasComponent(areaItem.m_Area))
			{
				DynamicBuffer<Node> nodes = m_Nodes[areaItem.m_Area];
				DynamicBuffer<Triangle> dynamicBuffer = m_Triangles[areaItem.m_Area];
				if (dynamicBuffer.Length > areaItem.m_Triangle && MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, dynamicBuffer[areaItem.m_Triangle]), m_Position, out var _))
				{
					m_Result = areaItem.m_Area;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RW_ComponentLookup;

		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RW_ComponentTypeHandle;

		public ComponentTypeHandle<BorderDistrict> __Game_Areas_BorderDistrict_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<ParkingLane>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RW_ComponentLookup = state.GetComponentLookup<CurrentDistrict>();
			__Game_Areas_BorderDistrict_RW_ComponentLookup = state.GetComponentLookup<BorderDistrict>();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>();
			__Game_Areas_BorderDistrict_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BorderDistrict>();
		}
	}

	private UpdateCollectSystem m_UpdateCollectSystem;

	private SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_CurrentDistrictQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateCollectSystem = base.World.GetOrCreateSystemManaged<UpdateCollectSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_CurrentDistrictQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<CurrentDistrict>(),
				ComponentType.ReadOnly<BorderDistrict>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CurrentDistrictQuery.IsEmptyIgnoreFilter || m_UpdateCollectSystem.districtsUpdated)
		{
			if (m_UpdateCollectSystem.districtsUpdated)
			{
				NativeQueue<Entity> updateBuffer = new NativeQueue<Entity>(Allocator.TempJob);
				NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
				JobHandle dependencies;
				NativeList<Bounds2> updatedDistrictBounds = m_UpdateCollectSystem.GetUpdatedDistrictBounds(out dependencies);
				JobHandle dependencies2;
				JobHandle dependencies3;
				FindUpdatedDistrictItemsJob jobData = new FindUpdatedDistrictItemsJob
				{
					m_Bounds = updatedDistrictBounds.AsDeferredJobArray(),
					m_ObjectTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
					m_NetTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies3),
					m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
					m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
					m_UpdateBuffer = updateBuffer.AsParallelWriter()
				};
				CollectUpdatedDistrictItemsJob jobData2 = new CollectUpdatedDistrictItemsJob
				{
					m_UpdateBuffer = updateBuffer,
					m_UpdateList = nativeList
				};
				JobHandle dependencies4;
				FindDistrictParallelJob jobData3 = new FindDistrictParallelJob
				{
					m_UpdateList = nativeList.AsDeferredJobArray(),
					m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies4),
					m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
					m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
					m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
					m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
					m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
					m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
					m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
					m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RW_ComponentLookup, ref base.CheckedStateRef),
					m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RW_ComponentLookup, ref base.CheckedStateRef)
				};
				JobHandle job = JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3);
				JobHandle jobHandle = jobData.Schedule(updatedDistrictBounds, 1, JobHandle.CombineDependencies(base.Dependency, job));
				JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
				JobHandle jobHandle3 = jobData3.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle2, dependencies4));
				updateBuffer.Dispose(jobHandle2);
				nativeList.Dispose(jobHandle3);
				m_UpdateCollectSystem.AddDistrictBoundsReader(jobHandle);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
				m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
				m_AreaSearchSystem.AddSearchTreeReader(jobHandle3);
				m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
				base.Dependency = jobHandle3;
			}
			if (!m_CurrentDistrictQuery.IsEmptyIgnoreFilter)
			{
				JobHandle dependencies5;
				JobHandle jobHandle4 = JobChunkExtensions.ScheduleParallel(new FindDistrictChunkJob
				{
					m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RW_ComponentTypeHandle, ref base.CheckedStateRef),
					m_BorderDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_BorderDistrict_RW_ComponentTypeHandle, ref base.CheckedStateRef),
					m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies5),
					m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
					m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
					m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef)
				}, m_CurrentDistrictQuery, JobHandle.CombineDependencies(base.Dependency, dependencies5));
				m_AreaSearchSystem.AddSearchTreeReader(jobHandle4);
				base.Dependency = jobHandle4;
			}
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
	public CurrentDistrictSystem()
	{
	}
}
