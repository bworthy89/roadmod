using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Net;
using Game.Pathfind;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GoodsDeliveryDispatchSystem : GameSystemBase
{
	private struct DispatchAction
	{
		public Entity m_RequestEntity;

		public Entity m_DispatchSource;

		public DispatchAction(Entity requestEntity, Entity dispatchSource)
		{
			m_RequestEntity = requestEntity;
			m_DispatchSource = dispatchSource;
		}
	}

	[BurstCompile]
	private struct GoodsDeliveryDispatchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryRequest> m_GoodsDeliveryRequestType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> m_GoodsDeliveryRequests;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[ReadOnly]
		public BufferLookup<TripNeeded> m_TripNeededs;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> m_ResourceNeedingBufs;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<DispatchAction>.ParallelWriter m_DispatchActions;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<GoodsDeliveryRequest> nativeArray2 = chunk.GetNativeArray(ref m_GoodsDeliveryRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					GoodsDeliveryRequest goodsDeliveryRequest = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if (!ValidateResourceNeeder(goodsDeliveryRequest))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
						continue;
					}
					if (SimulationUtils.TickServiceRequest(ref serviceRequest))
					{
						FindGoodsDeliverSource(unfilteredChunkIndex, entity, goodsDeliveryRequest);
					}
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<GoodsDeliveryRequest> nativeArray5 = chunk.GetNativeArray(ref m_GoodsDeliveryRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					GoodsDeliveryRequest goodsDeliveryRequest2 = nativeArray5[j];
					ServiceRequest serviceRequest2 = nativeArray6[j];
					if (ValidateDispatcher(entity2, dispatched.m_Handler))
					{
						serviceRequest2.m_Cooldown = 0;
					}
					else if (serviceRequest2.m_Cooldown == 0)
					{
						serviceRequest2.m_Cooldown = 1;
					}
					else
					{
						if (!ValidateResourceNeeder(goodsDeliveryRequest2))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity2);
							continue;
						}
						ResetFailedRequest(unfilteredChunkIndex, entity2, dispatched: true, ref serviceRequest2);
					}
					nativeArray6[j] = serviceRequest2;
				}
				return;
			}
			NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref m_PathInformationType);
			if (nativeArray8.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray9 = chunk.GetNativeArray(m_EntityType);
			for (int k = 0; k < nativeArray5.Length; k++)
			{
				Entity entity3 = nativeArray9[k];
				GoodsDeliveryRequest goodsDeliveryRequest3 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if (!ValidateResourceNeeder(goodsDeliveryRequest3))
				{
					m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
					continue;
				}
				if (pathInformation.m_Origin != Entity.Null)
				{
					DispatchVehicle(unfilteredChunkIndex, entity3, pathInformation);
				}
				else
				{
					ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
				}
				nativeArray6[k] = serviceRequest3;
			}
		}

		private bool ValidateDispatcher(Entity requestEntity, Entity dispatcherEntity)
		{
			if (m_ServiceDispatches.HasBuffer(dispatcherEntity))
			{
				DynamicBuffer<ServiceDispatch> dynamicBuffer = m_ServiceDispatches[dispatcherEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (dynamicBuffer[i].m_Request == requestEntity)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateResourceNeeder(GoodsDeliveryRequest goodsDeliveryRequest)
		{
			if (!m_ResourceNeedingBufs.TryGetBuffer(goodsDeliveryRequest.m_ResourceNeeder, out var bufferData))
			{
				return false;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				ResourceNeeding resourceNeeding = bufferData[i];
				if (resourceNeeding.m_Resource == goodsDeliveryRequest.m_Resource && resourceNeeding.m_Amount == goodsDeliveryRequest.m_Amount && resourceNeeding.m_Flags == ResourceNeedingFlags.Requested)
				{
					return true;
				}
			}
			return false;
		}

		private void ResetFailedRequest(int jobIndex, Entity requestEntity, bool dispatched, ref ServiceRequest serviceRequest)
		{
			SimulationUtils.ResetFailedRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, requestEntity);
			m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, requestEntity);
			if (dispatched)
			{
				m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, requestEntity);
			}
		}

		private void DispatchVehicle(int jobIndex, Entity requestEntity, PathInformation pathInformation)
		{
			m_DispatchActions.Enqueue(new DispatchAction(requestEntity, pathInformation.m_Origin));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, new Dispatched(pathInformation.m_Origin));
		}

		private void FindGoodsDeliverSource(int jobIndex, Entity requestEntity, GoodsDeliveryRequest requestData)
		{
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = PathMethod.Road,
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.GoodsDelivery,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Value = requestData.m_Amount,
				m_Resource = requestData.m_Resource,
				m_RandomCost = 30f
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = requestData.m_ResourceNeeder,
				m_RandomCost = 30f
			};
			origin.m_Flags |= SetupTargetFlags.Industrial | SetupTargetFlags.Import | SetupTargetFlags.RequireTransport;
			SetupQueueItem value = new SetupQueueItem(requestEntity, parameters, origin, destination);
			m_PathfindQueue.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DispatchActionJob : IJob
	{
		public BufferLookup<ServiceDispatch> m_ServiceDispatchBufs;

		public NativeQueue<DispatchAction> m_DispatchActionQueue;

		public void Execute()
		{
			DispatchAction item;
			while (m_DispatchActionQueue.TryDequeue(out item))
			{
				if (m_ServiceDispatchBufs.TryGetBuffer(item.m_DispatchSource, out var bufferData))
				{
					bufferData.Add(new ServiceDispatch(item.m_RequestEntity));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> __Game_Buildings_ResourceNeeding_RO_BufferLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GoodsDeliveryRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup = state.GetComponentLookup<GoodsDeliveryRequest>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RO_BufferLookup = state.GetBufferLookup<TripNeeded>(isReadOnly: true);
			__Game_Buildings_ResourceNeeding_RO_BufferLookup = state.GetBufferLookup<ResourceNeeding>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_RequestQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<GoodsDeliveryRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = (m_SimulationSystem.frameIndex >> 4) & 0x1F;
		uint nextUpdateFrameIndex = (num + 4) & 0x1F;
		NativeQueue<DispatchAction> dispatchActionQueue = new NativeQueue<DispatchAction>(Allocator.TempJob);
		GoodsDeliveryDispatchJob jobData = new GoodsDeliveryDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_GoodsDeliveryRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GoodsDeliveryRequests = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_TripNeededs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_TripNeeded_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceNeedingBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ResourceNeeding_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_DispatchActions = dispatchActionQueue.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		DispatchActionJob jobData2 = new DispatchActionJob
		{
			m_ServiceDispatchBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_DispatchActionQueue = dispatchActionQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_RequestQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		dispatchActionQueue.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
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
	public GoodsDeliveryDispatchSystem()
	{
	}
}
