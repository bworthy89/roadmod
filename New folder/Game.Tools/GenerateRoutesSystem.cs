using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateRoutesSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateRoutesJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_SubElementChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Waypoint> m_WaypointType;

		[ReadOnly]
		public ComponentTypeHandle<Segment> m_SegmentType;

		[ReadOnly]
		public ComponentTypeHandle<ColorDefinition> m_ColorDefinitionType;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> m_WaypointDefinitionType;

		[ReadOnly]
		public ComponentLookup<Color> m_ColorData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_RouteData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<ColorDefinition> nativeArray2 = chunk.GetNativeArray(ref m_ColorDefinitionType);
			BufferAccessor<WaypointDefinition> bufferAccessor = chunk.GetBufferAccessor(ref m_WaypointDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				DynamicBuffer<WaypointDefinition> dynamicBuffer = bufferAccessor[i];
				RouteFlags routeFlags = (RouteFlags)0;
				TempFlags tempFlags = (TempFlags)0u;
				RouteData routeData;
				if (creationDefinition.m_Original != Entity.Null)
				{
					routeFlags |= RouteFlags.Complete;
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, creationDefinition.m_Original, default(Hidden));
					creationDefinition.m_Prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
					routeData = m_RouteData[creationDefinition.m_Prefab];
					routeData.m_Color = m_ColorData[creationDefinition.m_Original].m_Color;
					tempFlags = (((creationDefinition.m_Flags & CreationFlags.Delete) != 0) ? (tempFlags | TempFlags.Delete) : (((creationDefinition.m_Flags & CreationFlags.Select) == 0) ? (tempFlags | TempFlags.Modify) : (tempFlags | TempFlags.Select)));
				}
				else
				{
					tempFlags |= TempFlags.Create;
					routeData = m_RouteData[creationDefinition.m_Prefab];
					if (nativeArray2.Length != 0)
					{
						routeData.m_Color = nativeArray2[i].m_Color;
					}
				}
				Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, routeData.m_RouteArchetype);
				DynamicBuffer<RouteWaypoint> dynamicBuffer2 = m_CommandBuffer.SetBuffer<RouteWaypoint>(unfilteredChunkIndex, e);
				DynamicBuffer<RouteSegment> dynamicBuffer3 = m_CommandBuffer.SetBuffer<RouteSegment>(unfilteredChunkIndex, e);
				if ((routeFlags & RouteFlags.Complete) == 0 && dynamicBuffer.Length >= 3 && dynamicBuffer[0].m_Position.Equals(dynamicBuffer[dynamicBuffer.Length - 1].m_Position))
				{
					CollectionUtils.ResizeInitialized(dynamicBuffer2, dynamicBuffer.Length - 1);
					CollectionUtils.ResizeInitialized(dynamicBuffer3, dynamicBuffer.Length - 1);
					FindSubElements(dynamicBuffer2, dynamicBuffer3);
					routeFlags |= RouteFlags.Complete;
				}
				else
				{
					CollectionUtils.ResizeInitialized(dynamicBuffer2, dynamicBuffer.Length);
					CollectionUtils.ResizeInitialized(dynamicBuffer3, dynamicBuffer.Length);
					FindSubElements(dynamicBuffer2, dynamicBuffer3);
				}
				if (creationDefinition.m_Owner != Entity.Null)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, new Owner(creationDefinition.m_Owner));
				}
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Route
				{
					m_Flags = routeFlags
				});
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, new Temp(creationDefinition.m_Original, tempFlags));
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new PrefabRef(creationDefinition.m_Prefab));
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Color(routeData.m_Color));
				if (m_TransportLineData.HasComponent(creationDefinition.m_Prefab))
				{
					TransportLineData transportLineData = m_TransportLineData[creationDefinition.m_Prefab];
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new TransportLine(transportLineData));
				}
			}
		}

		private void FindSubElements(DynamicBuffer<RouteWaypoint> routeWaypoints, DynamicBuffer<RouteSegment> routeSegments)
		{
			for (int i = 0; i < m_SubElementChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_SubElementChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Waypoint> nativeArray2 = archetypeChunk.GetNativeArray(ref m_WaypointType);
				NativeArray<Segment> nativeArray3 = archetypeChunk.GetNativeArray(ref m_SegmentType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					routeWaypoints[nativeArray2[j].m_Index] = new RouteWaypoint(nativeArray[j]);
				}
				for (int k = 0; k < nativeArray3.Length; k++)
				{
					routeSegments[nativeArray3[k].m_Index] = new RouteSegment(nativeArray[k]);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Waypoint> __Game_Routes_Waypoint_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Segment> __Game_Routes_Segment_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> __Game_Routes_WaypointDefinition_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ColorDefinition> __Game_Tools_ColorDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Color> __Game_Routes_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Waypoint>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Segment>(isReadOnly: true);
			__Game_Routes_WaypointDefinition_RO_BufferTypeHandle = state.GetBufferTypeHandle<WaypointDefinition>(isReadOnly: true);
			__Game_Tools_ColorDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ColorDefinition>(isReadOnly: true);
			__Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Color>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_SubElementQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_DefinitionQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<WaypointDefinition>(), ComponentType.ReadOnly<Updated>());
		m_SubElementQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Updated>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<Segment>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> subElementChunks = m_SubElementQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CreateRoutesJob
		{
			m_SubElementChunks = subElementChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SegmentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointDefinitionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_WaypointDefinition_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ColorDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_ColorDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Color_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_DefinitionQuery, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		subElementChunks.Dispose(jobHandle);
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
	public GenerateRoutesSystem()
	{
	}
}
