using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
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
public class NetComponentsSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckNodeComponentsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<TrafficLights> m_TrafficLightsType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		public ComponentTypeHandle<Roundabout> m_RoundaboutType;

		public ComponentTypeHandle<Gate> m_GateType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> m_PrefabCompositionPieces;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<TrafficLights> nativeArray3 = chunk.GetNativeArray(ref m_TrafficLightsType);
			NativeArray<Composition> nativeArray4 = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<Roundabout> nativeArray5 = chunk.GetNativeArray(ref m_RoundaboutType);
			NativeArray<Gate> nativeArray6 = chunk.GetNativeArray(ref m_GateType);
			BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[i];
				CompositionFlags compositionFlags = default(CompositionFlags);
				CompositionFlags compositionFlags2 = default(CompositionFlags);
				if (nativeArray3.Length != 0)
				{
					if ((nativeArray3[i].m_Flags & TrafficLightFlags.LevelCrossing) != 0)
					{
						compositionFlags.m_General |= CompositionFlags.General.LevelCrossing;
					}
					else
					{
						compositionFlags.m_General |= CompositionFlags.General.TrafficLights;
					}
				}
				if (CollectionUtils.TryGet(nativeArray5, i, out var value))
				{
					compositionFlags.m_General |= CompositionFlags.General.Roundabout;
				}
				value.m_Radius = 0f;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity edge = dynamicBuffer[j].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					bool flag = edge2.m_Start == entity;
					bool flag2 = edge2.m_End == entity;
					if ((flag || flag2) && m_CompositionData.TryGetComponent(edge, out var componentData))
					{
						NetCompositionData compositionData = m_PrefabCompositionData[flag ? componentData.m_StartNode : componentData.m_EndNode];
						compositionFlags2 |= compositionData.m_Flags;
						if ((compositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
						{
							EdgeNodeGeometry edgeNodeGeometry = ((!flag) ? m_EndNodeGeometryData[edge].m_Geometry : m_StartNodeGeometryData[edge].m_Geometry);
							DynamicBuffer<NetCompositionPiece> pieces = m_PrefabCompositionPieces[componentData.m_Edge];
							float2 @float = NetCompositionHelpers.CalculateRoundaboutSize(compositionData, pieces);
							float num = math.select(@float.x, @float.y, flag2);
							num += edgeNodeGeometry.m_MiddleRadius;
							value.m_Radius = math.max(value.m_Radius, num);
						}
					}
				}
				CompositionFlags compositionFlags3 = compositionFlags ^ compositionFlags2;
				if ((compositionFlags3.m_General & (CompositionFlags.General.LevelCrossing | CompositionFlags.General.TrafficLights)) != 0)
				{
					if ((compositionFlags2.m_General & CompositionFlags.General.LevelCrossing) != 0)
					{
						if ((compositionFlags.m_General & CompositionFlags.General.TrafficLights) != 0)
						{
							TrafficLights component = nativeArray3[i];
							component.m_Flags |= TrafficLightFlags.LevelCrossing;
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component);
						}
						else
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new TrafficLights
							{
								m_Flags = TrafficLightFlags.LevelCrossing
							});
						}
					}
					else if ((compositionFlags2.m_General & CompositionFlags.General.TrafficLights) != 0)
					{
						if ((compositionFlags.m_General & CompositionFlags.General.LevelCrossing) != 0)
						{
							TrafficLights component2 = nativeArray3[i];
							component2.m_Flags &= ~TrafficLightFlags.LevelCrossing;
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, component2);
						}
						else
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(TrafficLights));
						}
					}
					else
					{
						m_CommandBuffer.RemoveComponent<TrafficLights>(unfilteredChunkIndex, entity);
					}
				}
				if ((compositionFlags3.m_General & CompositionFlags.General.Roundabout) != 0)
				{
					if ((compositionFlags2.m_General & CompositionFlags.General.Roundabout) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, value);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Roundabout>(unfilteredChunkIndex, entity);
					}
				}
				CollectionUtils.TrySet(nativeArray5, i, value);
			}
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity e = nativeArray[k];
				Composition composition = nativeArray4[k];
				CompositionFlags compositionFlags4 = default(CompositionFlags);
				CompositionFlags compositionFlags5 = default(CompositionFlags);
				if (CollectionUtils.TryGet(nativeArray6, k, out var value2))
				{
					compositionFlags4.m_Left |= CompositionFlags.Side.Gate;
					compositionFlags4.m_Right |= CompositionFlags.Side.Gate;
				}
				value2.m_Domain = Entity.Null;
				NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
				compositionFlags5 = netCompositionData.m_Flags;
				if (((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Gate) != 0 && CollectionUtils.TryGet(nativeArray2, k, out var value3))
				{
					Owner componentData2;
					while (m_OwnerData.TryGetComponent(value3.m_Owner, out componentData2))
					{
						value3 = componentData2;
					}
					value2.m_Domain = value3.m_Owner;
				}
				if ((((compositionFlags4.m_Left | compositionFlags4.m_Right) ^ (compositionFlags5.m_Left | compositionFlags5.m_Right)) & CompositionFlags.Side.Gate) != 0)
				{
					if (((compositionFlags5.m_Left | compositionFlags5.m_Right) & CompositionFlags.Side.Gate) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, value2);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Gate>(unfilteredChunkIndex, e);
					}
				}
				CollectionUtils.TrySet(nativeArray6, k, value2);
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrafficLights> __Game_Net_TrafficLights_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		public ComponentTypeHandle<Roundabout> __Game_Net_Roundabout_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Gate> __Game_Net_Gate_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> __Game_Prefabs_NetCompositionPiece_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_TrafficLights_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrafficLights>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Roundabout_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Roundabout>();
			__Game_Net_Gate_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Gate>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionPiece_RO_BufferLookup = state.GetBufferLookup<NetCompositionPiece>(isReadOnly: true);
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_UpdatedNetQuery;

	private EntityQuery m_AllNetQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_UpdatedNetQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Edge>()
			}
		});
		m_AllNetQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Edge>()
			}
		});
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
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
		EntityQuery query = (GetLoaded() ? m_AllNetQuery : m_UpdatedNetQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CheckNodeComponentsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrafficLightsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrafficLights_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RoundaboutType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Roundabout_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GateType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Gate_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionPieces = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionPiece_RO_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, query, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
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
	public NetComponentsSystem()
	{
	}
}
