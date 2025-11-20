using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class SearchSystem : GameSystemBase, IPreDeserialize
{
	[BurstCompile]
	public struct UpdateSearchTreeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Position> m_PositionType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> m_PathUpdatedType;

		[ReadOnly]
		public BufferTypeHandle<CurveElement> m_CurveElementType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Segment> m_SegmentData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<CurveElement> m_CurveElements;

		[ReadOnly]
		public bool m_Loaded;

		public NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		public NativeParallelHashMap<Entity, int> m_ElementCount;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_PathUpdatedType))
			{
				NativeArray<PathUpdated> nativeArray = chunk.GetNativeArray(ref m_PathUpdatedType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity owner = nativeArray[i].m_Owner;
					if (m_SegmentData.HasComponent(owner) && !m_TempData.HasComponent(owner) && !m_UpdatedData.HasComponent(owner) && !m_DeletedData.HasComponent(owner))
					{
						PrefabRef prefabRef = m_PrefabRefData[owner];
						DynamicBuffer<CurveElement> curveElements = m_CurveElements[owner];
						UpdateSegment(owner, prefabRef, curveElements);
					}
				}
			}
			else if (m_Loaded || chunk.Has(ref m_CreatedType))
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Position> nativeArray4 = chunk.GetNativeArray(ref m_PositionType);
				BufferAccessor<CurveElement> bufferAccessor = chunk.GetBufferAccessor(ref m_CurveElementType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity = nativeArray2[j];
					PrefabRef prefabRef2 = nativeArray3[j];
					Position waypointPosition = nativeArray4[j];
					RouteData routeData = m_PrefabRouteData[prefabRef2.m_Prefab];
					Bounds3 bounds = RouteUtils.CalculateBounds(waypointPosition, routeData);
					m_SearchTree.Add(new RouteSearchItem(entity, 0), new QuadTreeBoundsXZ(bounds));
				}
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					Entity entity2 = nativeArray2[k];
					PrefabRef prefabRef3 = nativeArray3[k];
					DynamicBuffer<CurveElement> dynamicBuffer = bufferAccessor[k];
					RouteData routeData2 = m_PrefabRouteData[prefabRef3.m_Prefab];
					int item = 0;
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						Bounds3 bounds2 = RouteUtils.CalculateBounds(dynamicBuffer[l], routeData2);
						m_SearchTree.Add(new RouteSearchItem(entity2, item++), new QuadTreeBoundsXZ(bounds2));
					}
					m_ElementCount.TryAdd(entity2, item);
				}
			}
			else if (chunk.Has(ref m_DeletedType))
			{
				NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Position> nativeArray6 = chunk.GetNativeArray(ref m_PositionType);
				BufferAccessor<CurveElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_CurveElementType);
				for (int m = 0; m < nativeArray6.Length; m++)
				{
					Entity entity3 = nativeArray5[m];
					m_SearchTree.Remove(new RouteSearchItem(entity3, 0));
				}
				for (int n = 0; n < bufferAccessor2.Length; n++)
				{
					Entity entity4 = nativeArray5[n];
					if (m_ElementCount.TryGetValue(entity4, out var item2))
					{
						for (int num = 0; num < item2; num++)
						{
							m_SearchTree.Remove(new RouteSearchItem(entity4, num));
						}
						m_ElementCount.Remove(entity4);
					}
				}
			}
			else
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray8 = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Position> nativeArray9 = chunk.GetNativeArray(ref m_PositionType);
				BufferAccessor<CurveElement> bufferAccessor3 = chunk.GetBufferAccessor(ref m_CurveElementType);
				for (int num2 = 0; num2 < nativeArray9.Length; num2++)
				{
					Entity entity5 = nativeArray7[num2];
					PrefabRef prefabRef4 = nativeArray8[num2];
					Position waypointPosition2 = nativeArray9[num2];
					RouteData routeData3 = m_PrefabRouteData[prefabRef4.m_Prefab];
					Bounds3 bounds3 = RouteUtils.CalculateBounds(waypointPosition2, routeData3);
					m_SearchTree.Update(new RouteSearchItem(entity5, 0), new QuadTreeBoundsXZ(bounds3));
				}
				for (int num3 = 0; num3 < bufferAccessor3.Length; num3++)
				{
					Entity entity6 = nativeArray7[num3];
					PrefabRef prefabRef5 = nativeArray8[num3];
					DynamicBuffer<CurveElement> curveElements2 = bufferAccessor3[num3];
					UpdateSegment(entity6, prefabRef5, curveElements2);
				}
			}
		}

		private void UpdateSegment(Entity entity, PrefabRef prefabRef, DynamicBuffer<CurveElement> curveElements)
		{
			RouteData routeData = m_PrefabRouteData[prefabRef.m_Prefab];
			if (m_ElementCount.TryGetValue(entity, out var item))
			{
				m_ElementCount.Remove(entity);
			}
			else
			{
				item = 0;
			}
			int num = 0;
			for (int i = 0; i < curveElements.Length; i++)
			{
				Bounds3 bounds = RouteUtils.CalculateBounds(curveElements[i], routeData);
				if (num < item)
				{
					m_SearchTree.Update(new RouteSearchItem(entity, num++), new QuadTreeBoundsXZ(bounds));
				}
				else
				{
					m_SearchTree.Add(new RouteSearchItem(entity, num++), new QuadTreeBoundsXZ(bounds));
				}
			}
			m_ElementCount.TryAdd(entity, num);
			while (num < item)
			{
				m_SearchTree.Remove(new RouteSearchItem(entity, num++));
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
		public ComponentTypeHandle<Position> __Game_Routes_Position_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> __Game_Pathfind_PathUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<CurveElement> __Game_Routes_CurveElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CurveElement> __Game_Routes_CurveElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_Position_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Position>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathUpdated>(isReadOnly: true);
			__Game_Routes_CurveElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<CurveElement>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Segment>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Routes_CurveElement_RO_BufferLookup = state.GetBufferLookup<CurveElement>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedRoutesQuery;

	private EntityQuery m_AllRoutesQuery;

	private NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> m_SearchTree;

	private NativeParallelHashMap<Entity, int> m_ElementCount;

	private JobHandle m_ReadDependencies;

	private JobHandle m_WriteDependencies;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedRoutesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<Position>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Segment>(),
				ComponentType.ReadOnly<PathElement>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Event>(),
				ComponentType.ReadOnly<PathUpdated>()
			}
		});
		m_AllRoutesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<Position>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Segment>(),
				ComponentType.ReadOnly<PathElement>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_SearchTree = new NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ>(1f, Allocator.Persistent);
		m_ElementCount = new NativeParallelHashMap<Entity, int>(100, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SearchTree.Dispose();
		m_ElementCount.Dispose();
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
		EntityQuery query = (loaded ? m_AllRoutesQuery : m_UpdatedRoutesQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			UpdateSearchTreeJob jobData = new UpdateSearchTreeJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Position_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_CurveElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Loaded = loaded,
				m_SearchTree = GetSearchTree(readOnly: false, out dependencies),
				m_ElementCount = m_ElementCount
			};
			base.Dependency = JobChunkExtensions.Schedule(jobData, query, JobHandle.CombineDependencies(base.Dependency, dependencies));
			AddSearchTreeWriter(base.Dependency);
		}
	}

	public NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> GetSearchTree(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_WriteDependencies : JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies));
		return m_SearchTree;
	}

	public void AddSearchTreeReader(JobHandle jobHandle)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, jobHandle);
	}

	public void AddSearchTreeWriter(JobHandle jobHandle)
	{
		m_WriteDependencies = jobHandle;
	}

	public void PreDeserialize(Context context)
	{
		JobHandle dependencies;
		NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> searchTree = GetSearchTree(readOnly: false, out dependencies);
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
