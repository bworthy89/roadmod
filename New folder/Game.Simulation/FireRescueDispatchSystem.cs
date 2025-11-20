using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Vehicles;
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
public class FireRescueDispatchSystem : GameSystemBase
{
	private struct VehicleDispatch
	{
		public Entity m_Request;

		public Entity m_Source;

		public VehicleDispatch(Entity request, Entity source)
		{
			m_Request = request;
			m_Source = source;
		}
	}

	[BurstCompile]
	private struct FireRescueDispatchJob : IJobChunk
	{
		private struct DistrictIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float2 m_Position;

			public ComponentLookup<District> m_DistrictData;

			public BufferLookup<Game.Areas.Node> m_Nodes;

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
					DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
					DynamicBuffer<Triangle> dynamicBuffer = m_Triangles[areaItem.m_Area];
					if (dynamicBuffer.Length > areaItem.m_Triangle && MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, dynamicBuffer[areaItem.m_Triangle]), m_Position, out var _))
					{
						m_Result = areaItem.m_Area;
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<FireRescueRequest> m_FireRescueRequestType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<OnFire> m_OnFireData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<RescueTarget> m_RescueTargetData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<FireStation> m_FireStationData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<FireEngine> m_FireEngineData;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<VehicleDispatch>.ParallelWriter m_VehicleDispatches;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<FireRescueRequest> nativeArray2 = chunk.GetNativeArray(ref m_FireRescueRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					FireRescueRequest fireRescueRequest = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if ((serviceRequest.m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						if (!ValidateReversed(entity, fireRescueRequest.m_Target))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleTarget(unfilteredChunkIndex, entity, fireRescueRequest.m_Target);
						}
					}
					else
					{
						if (!ValidateTarget(entity, fireRescueRequest.m_Target))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleSource(unfilteredChunkIndex, entity, fireRescueRequest.m_Target);
						}
					}
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<FireRescueRequest> nativeArray5 = chunk.GetNativeArray(ref m_FireRescueRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					FireRescueRequest fireRescueRequest2 = nativeArray5[j];
					ServiceRequest serviceRequest2 = nativeArray6[j];
					if (ValidateHandler(entity2, dispatched.m_Handler))
					{
						serviceRequest2.m_Cooldown = 0;
					}
					else if (serviceRequest2.m_Cooldown == 0)
					{
						serviceRequest2.m_Cooldown = 1;
					}
					else
					{
						if (!ValidateTarget(entity2, fireRescueRequest2.m_Target))
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
				FireRescueRequest fireRescueRequest3 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if ((serviceRequest3.m_Flags & ServiceRequestFlags.Reversed) != 0)
				{
					if (!ValidateReversed(entity3, fireRescueRequest3.m_Target))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
						continue;
					}
					if (pathInformation.m_Destination != Entity.Null)
					{
						ResetReverseRequest(unfilteredChunkIndex, entity3, pathInformation, ref serviceRequest3);
					}
					else
					{
						ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
					}
				}
				else
				{
					if (!ValidateTarget(entity3, fireRescueRequest3.m_Target))
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
				}
				nativeArray6[k] = serviceRequest3;
			}
		}

		private bool ValidateReversed(Entity entity, Entity source)
		{
			if (m_FireStationData.TryGetComponent(source, out var componentData))
			{
				if ((componentData.m_Flags & (FireStationFlags.HasAvailableFireEngines | FireStationFlags.HasAvailableFireHelicopters)) == 0)
				{
					return false;
				}
				if (componentData.m_TargetRequest != entity)
				{
					if (m_FireRescueRequestData.HasComponent(componentData.m_TargetRequest))
					{
						return false;
					}
					componentData.m_TargetRequest = entity;
					m_FireStationData[source] = componentData;
				}
				return true;
			}
			if (m_FireEngineData.TryGetComponent(source, out var componentData2))
			{
				if ((componentData2.m_State & (FireEngineFlags.Empty | FireEngineFlags.EstimatedEmpty | FireEngineFlags.Disabled)) != 0 || componentData2.m_RequestCount > 1 || m_ParkedCarData.HasComponent(source))
				{
					return false;
				}
				if (componentData2.m_TargetRequest != entity)
				{
					if (m_FireRescueRequestData.HasComponent(componentData2.m_TargetRequest))
					{
						return false;
					}
					componentData2.m_TargetRequest = entity;
					m_FireEngineData[source] = componentData2;
				}
				return true;
			}
			return false;
		}

		private bool ValidateHandler(Entity entity, Entity handler)
		{
			if (m_ServiceDispatches.TryGetBuffer(handler, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (bufferData[i].m_Request == entity)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateTarget(Entity entity, Entity target)
		{
			if (m_OnFireData.TryGetComponent(target, out var componentData))
			{
				if (componentData.m_RescueRequest != entity)
				{
					if (m_FireRescueRequestData.HasComponent(componentData.m_RescueRequest))
					{
						return false;
					}
					componentData.m_RescueRequest = entity;
					m_OnFireData[target] = componentData;
				}
				return true;
			}
			if (m_RescueTargetData.TryGetComponent(target, out var componentData2))
			{
				if (componentData2.m_Request != entity)
				{
					if (m_FireRescueRequestData.HasComponent(componentData2.m_Request))
					{
						return false;
					}
					componentData2.m_Request = entity;
					m_RescueTargetData[target] = componentData2;
				}
				return true;
			}
			return false;
		}

		private void ResetReverseRequest(int jobIndex, Entity entity, PathInformation pathInformation, ref ServiceRequest serviceRequest)
		{
			VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Destination);
			m_VehicleDispatches.Enqueue(value);
			SimulationUtils.ResetReverseRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
		}

		private void ResetFailedRequest(int jobIndex, Entity entity, bool dispatched, ref ServiceRequest serviceRequest)
		{
			SimulationUtils.ResetFailedRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, entity);
			if (dispatched)
			{
				m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, entity);
			}
		}

		private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
		{
			Entity entity2 = pathInformation.m_Origin;
			if (m_ParkedCarData.HasComponent(entity2) && m_OwnerData.TryGetComponent(entity2, out var componentData))
			{
				entity2 = componentData.m_Owner;
			}
			VehicleDispatch value = new VehicleDispatch(entity, entity2);
			m_VehicleDispatches.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(entity2));
		}

		private void FindVehicleSource(int jobIndex, Entity requestEntity, Entity target)
		{
			Entity entity = Entity.Null;
			if (m_CurrentDistrictData.HasComponent(target))
			{
				entity = m_CurrentDistrictData[target].m_District;
			}
			else if (m_TransformData.HasComponent(target))
			{
				DistrictIterator iterator = new DistrictIterator
				{
					m_Position = m_TransformData[target].m_Position.xz,
					m_DistrictData = m_DistrictData,
					m_Nodes = m_Nodes,
					m_Triangles = m_Triangles
				};
				m_AreaTree.Iterate(ref iterator);
				entity = iterator.m_Result;
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 0f, 0f, 0f),
				m_Methods = (PathMethod.Road | PathMethod.Flying | PathMethod.Offroad),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.FireEngine,
				m_Methods = (PathMethod.Road | PathMethod.Flying | PathMethod.Offroad),
				m_RoadTypes = (RoadTypes.Car | RoadTypes.Helicopter),
				m_FlyingTypes = RoadTypes.Helicopter,
				m_Entity = entity
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Flying | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Car,
				m_FlyingTypes = RoadTypes.Helicopter,
				m_Entity = target,
				m_Value2 = 30f
			};
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		private void FindVehicleTarget(int jobIndex, Entity requestEntity, Entity vehicleSource)
		{
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 0f, 0f, 0f),
				m_Methods = PathMethod.Road,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_Entity = vehicleSource
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.FireRescueRequest
			};
			bool flag = false;
			bool flag2 = false;
			if (m_FireStationData.TryGetComponent(vehicleSource, out var componentData))
			{
				flag = (componentData.m_Flags & FireStationFlags.HasAvailableFireEngines) != 0;
				flag2 = (componentData.m_Flags & FireStationFlags.HasAvailableFireHelicopters) != 0;
			}
			else if (m_HelicopterData.HasComponent(vehicleSource))
			{
				flag2 = true;
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				parameters.m_Methods |= PathMethod.Offroad;
				origin.m_Methods |= PathMethod.Offroad;
				origin.m_RoadTypes |= RoadTypes.Car;
				destination.m_Methods |= PathMethod.Road | PathMethod.Offroad;
				destination.m_RoadTypes |= RoadTypes.Car;
			}
			if (flag2)
			{
				parameters.m_Methods |= PathMethod.Flying;
				origin.m_Methods |= PathMethod.Flying;
				origin.m_RoadTypes |= RoadTypes.Helicopter;
				origin.m_FlyingTypes |= RoadTypes.Helicopter;
				destination.m_Methods |= PathMethod.Flying;
				destination.m_FlyingTypes |= RoadTypes.Helicopter;
			}
			if (m_FireEngineData.TryGetComponent(vehicleSource, out var componentData2) && (componentData2.m_State & FireEngineFlags.Returning) == 0)
			{
				origin.m_Flags |= SetupTargetFlags.PathEnd;
			}
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DispatchVehiclesJob : IJob
	{
		public NativeQueue<VehicleDispatch> m_VehicleDispatches;

		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		public void Execute()
		{
			VehicleDispatch item;
			while (m_VehicleDispatches.TryDequeue(out item))
			{
				ServiceRequest componentData;
				if (m_ServiceDispatches.TryGetBuffer(item.m_Source, out var bufferData))
				{
					bufferData.Add(new ServiceDispatch(item.m_Request));
				}
				else if (m_ServiceRequestData.TryGetComponent(item.m_Source, out componentData))
				{
					componentData.m_Flags |= ServiceRequestFlags.SkipCooldown;
					m_ServiceRequestData[item.m_Source] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		public ComponentLookup<OnFire> __Game_Events_OnFire_RW_ComponentLookup;

		public ComponentLookup<RescueTarget> __Game_Buildings_RescueTarget_RW_ComponentLookup;

		public ComponentLookup<FireStation> __Game_Buildings_FireStation_RW_ComponentLookup;

		public ComponentLookup<FireEngine> __Game_Vehicles_FireEngine_RW_ComponentLookup;

		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_FireRescueRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FireRescueRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Events_OnFire_RW_ComponentLookup = state.GetComponentLookup<OnFire>();
			__Game_Buildings_RescueTarget_RW_ComponentLookup = state.GetComponentLookup<RescueTarget>();
			__Game_Buildings_FireStation_RW_ComponentLookup = state.GetComponentLookup<FireStation>();
			__Game_Vehicles_FireEngine_RW_ComponentLookup = state.GetComponentLookup<FireEngine>();
			__Game_Simulation_ServiceRequest_RW_ComponentLookup = state.GetComponentLookup<ServiceRequest>();
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

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
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<FireRescueRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = (m_SimulationSystem.frameIndex / 16) & 3;
		uint nextUpdateFrameIndex = (num + 4) & 3;
		NativeQueue<VehicleDispatch> vehicleDispatches = new NativeQueue<VehicleDispatch>(Allocator.TempJob);
		JobHandle dependencies;
		FireRescueDispatchJob jobData = new FireRescueDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_FireRescueRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RescueTargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_RescueTarget_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_FireStation_RW_ComponentLookup, ref base.CheckedStateRef),
			m_FireEngineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_FireEngine_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_VehicleDispatches = vehicleDispatches.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		DispatchVehiclesJob jobData2 = new DispatchVehiclesJob
		{
			m_VehicleDispatches = vehicleDispatches,
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_RequestQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		vehicleDispatches.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public FireRescueDispatchSystem()
	{
	}
}
