using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct FirePathfindSetup
{
	[BurstCompile]
	private struct SetupFireEnginesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.FireStation> m_FireStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.FireEngine> m_FireEngineType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public Entity m_City;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has<Game.Objects.OutsideConnection>() && !CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.FireStation> nativeArray2 = chunk.GetNativeArray(ref m_FireStationType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					Game.Buildings.FireStation fireStation = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity2, out var owner, out var targetSeeker);
						m_FireRescueRequestData.TryGetComponent(owner, out var componentData);
						RoadTypes roadTypes = RoadTypes.None;
						if (componentData.m_Target == entity)
						{
							if ((fireStation.m_Flags & FireStationFlags.HasFreeFireEngines) != 0)
							{
								roadTypes |= RoadTypes.Car;
							}
							if ((fireStation.m_Flags & FireStationFlags.HasFreeFireHelicopters) != 0)
							{
								roadTypes |= RoadTypes.Helicopter;
							}
						}
						else if (AreaUtils.CheckServiceDistrict(entity2, entity, m_ServiceDistricts))
						{
							if ((fireStation.m_Flags & FireStationFlags.HasAvailableFireEngines) != 0)
							{
								roadTypes |= RoadTypes.Car;
							}
							if ((fireStation.m_Flags & FireStationFlags.HasAvailableFireHelicopters) != 0)
							{
								roadTypes |= RoadTypes.Helicopter;
							}
						}
						if (componentData.m_Type == FireRescueRequestType.Disaster && (fireStation.m_Flags & FireStationFlags.DisasterResponseAvailable) == 0)
						{
							roadTypes = RoadTypes.None;
						}
						roadTypes &= targetSeeker.m_SetupQueueTarget.m_RoadTypes | targetSeeker.m_SetupQueueTarget.m_FlyingTypes;
						if (roadTypes != RoadTypes.None)
						{
							float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
							RoadTypes roadTypes2 = targetSeeker.m_SetupQueueTarget.m_RoadTypes;
							RoadTypes flyingTypes = targetSeeker.m_SetupQueueTarget.m_FlyingTypes;
							targetSeeker.m_SetupQueueTarget.m_RoadTypes &= roadTypes;
							targetSeeker.m_SetupQueueTarget.m_FlyingTypes &= roadTypes;
							targetSeeker.FindTargets(entity, cost);
							targetSeeker.m_SetupQueueTarget.m_RoadTypes = roadTypes2;
							targetSeeker.m_SetupQueueTarget.m_FlyingTypes = flyingTypes;
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.FireEngine> nativeArray3 = chunk.GetNativeArray(ref m_FireEngineType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.FireEngine fireEngine = nativeArray3[k];
				if ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.EstimatedEmpty)) != 0)
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var owner2, out var targetSeeker2);
					m_FireRescueRequestData.TryGetComponent(owner2, out var componentData2);
					if (componentData2.m_Type == FireRescueRequestType.Disaster && (fireEngine.m_State & FireEngineFlags.DisasterResponse) == 0)
					{
						continue;
					}
					float num = 0f;
					if (CollectionUtils.TryGet(nativeArray5, k, out var value))
					{
						if (!AreaUtils.CheckServiceDistrict(entity4, value.m_Owner, m_ServiceDistricts))
						{
							continue;
						}
						if (m_OutsideConnections.HasComponent(value.m_Owner))
						{
							num += 30f;
						}
					}
					if (componentData2.m_Target != value.m_Owner && (fireEngine.m_State & FireEngineFlags.Disabled) != 0)
					{
						continue;
					}
					if ((fireEngine.m_State & FireEngineFlags.Returning) != 0 || nativeArray4.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, num);
						continue;
					}
					PathOwner pathOwner = nativeArray4[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num2 = math.min(fireEngine.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					bool flag = false;
					if (num2 >= 1)
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * fireEngine.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag = true;
						}
					}
					for (int m = 1; m < num2; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						if (m_PathInformationData.TryGetComponent(request, out var componentData3))
						{
							num += componentData3.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
						}
						num += 10f * targetSeeker2.m_PathfindParameters.m_Weights.time;
						if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
						{
							pathElement = bufferData[bufferData.Length - 1];
							flag = true;
						}
					}
					if (flag)
					{
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, num));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, entity3, num, EdgeFlags.DefaultMask, allowAccessRestriction: true, num2 >= 1);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupEmergencySheltersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.EmergencyShelter> nativeArray2 = chunk.GetNativeArray(ref m_EmergencyShelterType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var targetSeeker);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if ((nativeArray2[j].m_Flags & EmergencyShelterFlags.HasShelterSpace) != 0 && AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
					{
						targetSeeker.FindTargets(entity2, 0f);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupEvacuationTransportJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PublicTransport> m_PublicTransportType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.EmergencyShelter> nativeArray2 = chunk.GetNativeArray(ref m_EmergencyShelterType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					if ((nativeArray2[i].m_Flags & (EmergencyShelterFlags.HasAvailableVehicles | EmergencyShelterFlags.HasShelterSpace)) != (EmergencyShelterFlags.HasAvailableVehicles | EmergencyShelterFlags.HasShelterSpace))
					{
						continue;
					}
					Entity entity = nativeArray[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity2, out var targetSeeker);
						if (AreaUtils.CheckServiceDistrict(entity2, entity, m_ServiceDistricts))
						{
							float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
							targetSeeker.FindTargets(entity, cost);
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.PublicTransport> nativeArray3 = chunk.GetNativeArray(ref m_PublicTransportType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Game.Vehicles.PublicTransport publicTransport = nativeArray3[k];
				if ((publicTransport.m_State & (PublicTransportFlags.Evacuating | PublicTransportFlags.Disabled | PublicTransportFlags.Full)) != PublicTransportFlags.Evacuating)
				{
					continue;
				}
				Entity entity3 = nativeArray[k];
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					if ((nativeArray5.Length != 0 && !AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts)) || (publicTransport.m_State & PublicTransportFlags.DummyTraffic) != 0)
					{
						continue;
					}
					if ((publicTransport.m_State & PublicTransportFlags.Returning) != 0 || nativeArray4.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, 0f);
						continue;
					}
					PathOwner pathOwner = nativeArray4[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num = math.min(publicTransport.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					float num2 = 0f;
					bool flag = false;
					if (num >= 1)
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num2 += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * publicTransport.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag = true;
						}
					}
					for (int m = 1; m < num; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						if (m_PathInformationData.TryGetComponent(request, out var componentData))
						{
							num2 += componentData.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
						}
						if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
						{
							pathElement = bufferData[bufferData.Length - 1];
							flag = true;
						}
					}
					if (flag)
					{
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, num2));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, entity3, num2, EdgeFlags.DefaultMask, allowAccessRestriction: true, num >= 1);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct EvacuationRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<EvacuationRequest> m_EvacuationRequestType;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> m_EvacuationRequestData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<EvacuationRequest> nativeArray3 = chunk.GetNativeArray(ref m_EvacuationRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_EvacuationRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				Entity service = Entity.Null;
				if (m_PublicTransportData.TryGetComponent(componentData.m_Target, out var _))
				{
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Target, out var componentData3))
					{
						service = componentData3.m_Owner;
					}
				}
				else
				{
					if (!targetSeeker.m_PrefabRef.HasComponent(componentData.m_Target))
					{
						continue;
					}
					service = componentData.m_Target;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) == 0)
					{
						EvacuationRequest evacuationRequest = nativeArray3[j];
						Entity district = Entity.Null;
						if (m_CurrentDistrictData.HasComponent(evacuationRequest.m_Target))
						{
							district = m_CurrentDistrictData[evacuationRequest.m_Target].m_District;
						}
						if (AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
						{
							targetSeeker.FindTargets(nativeArray[j], evacuationRequest.m_Target, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FireRescueRequestsJob : IJobChunk
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
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<FireRescueRequest> m_FireRescueRequestType;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.FireEngine> m_FireEngineData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.FireStation> m_FireStationData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetTree;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<FireRescueRequest> nativeArray3 = chunk.GetNativeArray(ref m_FireRescueRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_FireRescueRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				bool flag = false;
				Entity service = Entity.Null;
				if (m_FireEngineData.TryGetComponent(componentData.m_Target, out var componentData2))
				{
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Target, out var componentData3))
					{
						service = componentData3.m_Owner;
					}
					flag = (componentData2.m_State & FireEngineFlags.DisasterResponse) != 0;
				}
				else
				{
					if (!m_FireStationData.TryGetComponent(componentData.m_Target, out var componentData4))
					{
						continue;
					}
					service = componentData.m_Target;
					flag = (componentData4.m_Flags & FireStationFlags.DisasterResponseAvailable) != 0;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					FireRescueRequest fireRescueRequest = nativeArray3[j];
					if (fireRescueRequest.m_Type == FireRescueRequestType.Disaster && !flag)
					{
						continue;
					}
					Entity district = Entity.Null;
					Transform componentData5;
					if (m_CurrentDistrictData.HasComponent(fireRescueRequest.m_Target))
					{
						district = m_CurrentDistrictData[fireRescueRequest.m_Target].m_District;
					}
					else if (targetSeeker.m_Transform.TryGetComponent(fireRescueRequest.m_Target, out componentData5))
					{
						DistrictIterator iterator = new DistrictIterator
						{
							m_Position = componentData5.m_Position.xz,
							m_DistrictData = m_DistrictData,
							m_Nodes = targetSeeker.m_AreaNode,
							m_Triangles = targetSeeker.m_AreaTriangle
						};
						m_AreaTree.Iterate(ref iterator);
						district = iterator.m_Result;
					}
					if (!AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
					{
						continue;
					}
					targetSeeker.FindTargets(nativeArray[j], fireRescueRequest.m_Target, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
					if (!targetSeeker.m_Transform.TryGetComponent(fireRescueRequest.m_Target, out var componentData6))
					{
						continue;
					}
					if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) != 0 && (targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						Entity lane = Entity.Null;
						float curvePos = 0f;
						float distance = float.MaxValue;
						targetSeeker.m_AirwayData.helicopterMap.FindClosestLane(componentData6.m_Position, targetSeeker.m_Curve, ref lane, ref curvePos, ref distance);
						if (lane != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(nativeArray[j], lane, curvePos, 0f));
						}
					}
					float num = 30f;
					CommonPathfindSetup.TargetIterator iterator2 = new CommonPathfindSetup.TargetIterator
					{
						m_Entity = nativeArray[j],
						m_Bounds = new Bounds3(componentData6.m_Position - num, componentData6.m_Position + num),
						m_Position = componentData6.m_Position,
						m_MaxDistance = num,
						m_TargetSeeker = targetSeeker,
						m_Flags = EdgeFlags.DefaultMask,
						m_CompositionData = m_CompositionData,
						m_NetCompositionData = m_NetCompositionData
					};
					m_NetTree.Iterate(ref iterator2);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_FireEngineQuery;

	private EntityQuery m_EmergencyShelterQuery;

	private EntityQuery m_EvacuationTransportQuery;

	private EntityQuery m_EvacuationRequestQuery;

	private EntityQuery m_FireRescueRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<FireRescueRequest> m_FireRescueRequestType;

	private ComponentTypeHandle<EvacuationRequest> m_EvacuationRequestType;

	private ComponentTypeHandle<Game.Buildings.FireStation> m_FireStationType;

	private ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

	private ComponentTypeHandle<Game.Vehicles.FireEngine> m_FireEngineType;

	private ComponentTypeHandle<Game.Vehicles.PublicTransport> m_PublicTransportType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private ComponentLookup<PathInformation> m_PathInformationData;

	private ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

	private ComponentLookup<EvacuationRequest> m_EvacuationRequestData;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<Composition> m_CompositionData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<District> m_DistrictData;

	private ComponentLookup<Game.Buildings.FireStation> m_FireStationData;

	private ComponentLookup<Game.Vehicles.FireEngine> m_FireEngineData;

	private ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

	private ComponentLookup<NetCompositionData> m_NetCompositionData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private ComponentLookup<Game.City.City> m_CityData;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private CitySystem m_CitySystem;

	public FirePathfindSetup(PathfindSetupSystem system)
	{
		m_FireEngineQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.FireStation>(),
				ComponentType.ReadOnly<Game.Vehicles.FireEngine>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EmergencyShelterQuery = system.GetSetupQuery(ComponentType.ReadOnly<Game.Buildings.EmergencyShelter>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_EvacuationTransportQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.EmergencyShelter>(),
				ComponentType.ReadOnly<Game.Vehicles.PublicTransport>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EvacuationRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<EvacuationRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_FireRescueRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<FireRescueRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_FireRescueRequestType = system.GetComponentTypeHandle<FireRescueRequest>(isReadOnly: true);
		m_EvacuationRequestType = system.GetComponentTypeHandle<EvacuationRequest>(isReadOnly: true);
		m_FireStationType = system.GetComponentTypeHandle<Game.Buildings.FireStation>(isReadOnly: true);
		m_EmergencyShelterType = system.GetComponentTypeHandle<Game.Buildings.EmergencyShelter>(isReadOnly: true);
		m_FireEngineType = system.GetComponentTypeHandle<Game.Vehicles.FireEngine>(isReadOnly: true);
		m_PublicTransportType = system.GetComponentTypeHandle<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_PathInformationData = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_FireRescueRequestData = system.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
		m_EvacuationRequestData = system.GetComponentLookup<EvacuationRequest>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_CompositionData = system.GetComponentLookup<Composition>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_DistrictData = system.GetComponentLookup<District>(isReadOnly: true);
		m_FireStationData = system.GetComponentLookup<Game.Buildings.FireStation>(isReadOnly: true);
		m_FireEngineData = system.GetComponentLookup<Game.Vehicles.FireEngine>(isReadOnly: true);
		m_PublicTransportData = system.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_NetCompositionData = system.GetComponentLookup<NetCompositionData>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_CityData = system.GetComponentLookup<Game.City.City>(isReadOnly: true);
		m_AreaSearchSystem = system.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NetSearchSystem = system.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
	}

	public JobHandle SetupFireEngines(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_FireStationType.Update(system);
		m_FireEngineType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_FireRescueRequestData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		m_OutsideConnections.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupFireEnginesJob
		{
			m_EntityType = m_EntityType,
			m_FireStationType = m_FireStationType,
			m_FireEngineType = m_FireEngineType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_FireRescueRequestData = m_FireRescueRequestData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_OutsideConnections = m_OutsideConnections,
			m_CityData = m_CityData,
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_FireEngineQuery, inputDeps);
	}

	public JobHandle SetupEmergencyShelters(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_EmergencyShelterType.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupEmergencySheltersJob
		{
			m_EntityType = m_EntityType,
			m_EmergencyShelterType = m_EmergencyShelterType,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_EmergencyShelterQuery, inputDeps);
	}

	public JobHandle SetupEvacuationTransport(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_EmergencyShelterType.Update(system);
		m_PublicTransportType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupEvacuationTransportJob
		{
			m_EntityType = m_EntityType,
			m_EmergencyShelterType = m_EmergencyShelterType,
			m_PublicTransportType = m_PublicTransportType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_EvacuationTransportQuery, inputDeps);
	}

	public JobHandle SetupEvacuationRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_EvacuationRequestType.Update(system);
		m_EvacuationRequestData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_PublicTransportData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new EvacuationRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_EvacuationRequestType = m_EvacuationRequestType,
			m_EvacuationRequestData = m_EvacuationRequestData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_PublicTransportData = m_PublicTransportData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_EvacuationRequestQuery, inputDeps);
	}

	public JobHandle SetupFireRescueRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_FireRescueRequestType.Update(system);
		m_FireRescueRequestData.Update(system);
		m_CompositionData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_DistrictData.Update(system);
		m_FireEngineData.Update(system);
		m_FireStationData.Update(system);
		m_NetCompositionData.Update(system);
		m_ServiceDistricts.Update(system);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new FireRescueRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_FireRescueRequestType = m_FireRescueRequestType,
			m_FireRescueRequestData = m_FireRescueRequestData,
			m_CompositionData = m_CompositionData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_DistrictData = m_DistrictData,
			m_FireEngineData = m_FireEngineData,
			m_FireStationData = m_FireStationData,
			m_NetCompositionData = m_NetCompositionData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_NetTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_SetupData = setupData
		}, m_FireRescueRequestQuery, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}
}
