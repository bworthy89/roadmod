using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class ReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRouteReferencesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_RouteChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> m_RouteWaypointType;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public ComponentLookup<Owner> m_OwnerData;

		public BufferLookup<SubRoute> m_SubRoutes;

		public void Execute()
		{
			for (int i = 0; i < m_RouteChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_RouteChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				BufferAccessor<RouteWaypoint> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RouteWaypointType);
				BufferAccessor<RouteSegment> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_RouteSegmentType);
				if (archetypeChunk.Has(ref m_DeletedType))
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Entity entity = nativeArray[j];
						if (m_OwnerData.TryGetComponent(entity, out var componentData) && m_SubRoutes.TryGetBuffer(componentData.m_Owner, out var bufferData))
						{
							CollectionUtils.RemoveValue(bufferData, new SubRoute(entity));
						}
					}
					continue;
				}
				bool flag = archetypeChunk.Has(ref m_TempType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity2 = nativeArray[k];
					DynamicBuffer<RouteWaypoint> dynamicBuffer = bufferAccessor[k];
					DynamicBuffer<RouteSegment> dynamicBuffer2 = bufferAccessor2[k];
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						Entity waypoint = dynamicBuffer[l].m_Waypoint;
						Owner value = m_OwnerData[waypoint];
						value.m_Owner = entity2;
						m_OwnerData[waypoint] = value;
					}
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						Entity segment = dynamicBuffer2[m].m_Segment;
						if (segment != Entity.Null)
						{
							Owner value2 = m_OwnerData[segment];
							value2.m_Owner = entity2;
							m_OwnerData[segment] = value2;
						}
					}
					if (m_OwnerData.TryGetComponent(entity2, out var componentData2) && m_TempData.HasComponent(componentData2.m_Owner) == flag && m_SubRoutes.TryGetBuffer(componentData2.m_Owner, out var bufferData2))
					{
						CollectionUtils.TryAddUniqueValue(bufferData2, new SubRoute(entity2));
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> __Game_Routes_RouteSegment_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		public ComponentLookup<Owner> __Game_Common_Owner_RW_ComponentLookup;

		public BufferLookup<SubRoute> __Game_Routes_SubRoute_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_RouteWaypoint_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteSegment>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Owner_RW_ComponentLookup = state.GetComponentLookup<Owner>();
			__Game_Routes_SubRoute_RW_BufferLookup = state.GetBufferLookup<SubRoute>();
		}
	}

	private EntityQuery m_RouteQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RouteQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadOnly<RouteWaypoint>(),
				ComponentType.ReadOnly<RouteSegment>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireForUpdate(m_RouteQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> routeChunks = m_RouteQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new UpdateRouteReferencesJob
		{
			m_RouteChunks = routeChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RouteWaypointType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RouteSegmentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SubRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_SubRoute_RW_BufferLookup, ref base.CheckedStateRef)
		}, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		routeChunks.Dispose(jobHandle);
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
	public ReferencesSystem()
	{
	}
}
