using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TransportStopSystem : GameSystemBase
{
	[BurstCompile]
	private struct TransportStopTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TaxiStand> m_TaxiStandType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedRoute> m_ConnectedRouteType;

		public ComponentTypeHandle<Game.Routes.TransportStop> m_TransportStopType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportStation> m_TransportStationData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_PrefabTransportStopData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Routes.TransportStop> nativeArray4 = chunk.GetNativeArray(ref m_TransportStopType);
			BufferAccessor<ConnectedRoute> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedRouteType);
			bool flag = chunk.Has(ref m_TaxiStandType);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				PrefabRef prefabRef = nativeArray2[i];
				Game.Routes.TransportStop value = nativeArray4[i];
				TransportStopData transportStopData = default(TransportStopData);
				if (m_PrefabTransportStopData.HasComponent(prefabRef.m_Prefab))
				{
					transportStopData = m_PrefabTransportStopData[prefabRef.m_Prefab];
				}
				float num = math.saturate(transportStopData.m_ComfortFactor);
				float num2 = math.max(0f, 1f + transportStopData.m_LoadingFactor);
				bool flag2 = true;
				if (nativeArray3.Length != 0)
				{
					Entity transportStation = GetTransportStation(nativeArray3[i].m_Owner);
					if (transportStation != Entity.Null)
					{
						Game.Buildings.TransportStation transportStation2 = m_TransportStationData[transportStation];
						num = math.saturate(num + (1f - num) * transportStation2.m_ComfortFactor);
						num2 = math.max(0f, num2 * transportStation2.m_LoadingFactor);
						flag2 = (transportStation2.m_Flags & TransportStationFlags.TransportStopsActive) != 0;
					}
				}
				if (num != value.m_ComfortFactor || num2 != value.m_LoadingFactor || flag2 != ((value.m_Flags & StopFlags.Active) != 0))
				{
					if (flag)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(PathfindUpdated));
					}
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<ConnectedRoute> dynamicBuffer = bufferAccessor[i];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, dynamicBuffer[j].m_Waypoint, default(PathfindUpdated));
						}
					}
				}
				value.m_ComfortFactor = num;
				value.m_LoadingFactor = num2;
				if (flag2)
				{
					value.m_Flags |= StopFlags.Active;
				}
				else
				{
					value.m_Flags &= ~StopFlags.Active;
				}
				nativeArray4[i] = value;
			}
		}

		private Entity GetTransportStation(Entity owner)
		{
			while (true)
			{
				if (m_TransportStationData.HasComponent(owner))
				{
					if (m_OwnerData.HasComponent(owner))
					{
						Entity owner2 = m_OwnerData[owner].m_Owner;
						if (m_TransportStationData.HasComponent(owner2))
						{
							return owner2;
						}
					}
					return owner;
				}
				if (!m_OwnerData.HasComponent(owner))
				{
					break;
				}
				owner = m_OwnerData[owner].m_Owner;
			}
			return Entity.Null;
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
		public ComponentTypeHandle<TaxiStand> __Game_Routes_TaxiStand_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Routes.TransportStop> __Game_Routes_TransportStop_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportStation> __Game_Buildings_TransportStation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_TaxiStand_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiStand>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedRoute>(isReadOnly: true);
			__Game_Routes_TransportStop_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.TransportStop>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_TransportStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.TransportStation>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 256u;

	private EntityQuery m_StopQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_StopQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Routes.TransportStop>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_StopQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TransportStopTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TaxiStandType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TaxiStand_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedRouteType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TransportStop_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TransportStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_StopQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public TransportStopSystem()
	{
	}
}
