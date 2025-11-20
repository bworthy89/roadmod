using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Events;
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

public struct PolicePathfindSetup
{
	[BurstCompile]
	private struct SetupPolicePatrolsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.PoliceStation> m_PoliceStationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PoliceCar> m_PoliceCarType;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> m_HelicopterType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

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
			NativeArray<Game.Buildings.PoliceStation> nativeArray2 = chunk.GetNativeArray(ref m_PoliceStationType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					Game.Buildings.PoliceStation policeStation = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity2, out var targetSeeker);
						PolicePurpose value = (PolicePurpose)targetSeeker.m_SetupQueueTarget.m_Value;
						if ((policeStation.m_PurposeMask & value) == 0)
						{
							continue;
						}
						RoadTypes roadTypes = RoadTypes.None;
						if (AreaUtils.CheckServiceDistrict(entity2, entity, m_ServiceDistricts))
						{
							if ((policeStation.m_Flags & PoliceStationFlags.HasAvailablePatrolCars) != 0)
							{
								roadTypes |= RoadTypes.Car;
							}
							if ((policeStation.m_Flags & PoliceStationFlags.HasAvailablePoliceHelicopters) != 0)
							{
								roadTypes |= RoadTypes.Helicopter;
							}
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
			NativeArray<Game.Vehicles.PoliceCar> nativeArray3 = chunk.GetNativeArray(ref m_PoliceCarType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Passenger> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PassengerType);
			bool flag = chunk.Has(ref m_HelicopterType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.PoliceCar policeCar = nativeArray3[k];
				if ((policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0)
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					PolicePurpose value2 = (PolicePurpose)targetSeeker2.m_SetupQueueTarget.m_Value;
					if ((policeCar.m_PurposeMask & value2) == 0 || ((targetSeeker2.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) == 0 && flag))
					{
						continue;
					}
					float num = 0f;
					if ((value2 & PolicePurpose.Patrol) != 0)
					{
						if ((policeCar.m_State & (PoliceCarFlags.Empty | PoliceCarFlags.EstimatedShiftEnd)) != PoliceCarFlags.Empty)
						{
							continue;
						}
					}
					else if (bufferAccessor3.Length != 0)
					{
						num += (float)bufferAccessor3[k].Length * 10f;
					}
					if (nativeArray5.Length != 0)
					{
						if (!AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts))
						{
							continue;
						}
						if (m_OutsideConnections.HasComponent(nativeArray5[k].m_Owner))
						{
							num += 30f;
						}
					}
					if ((policeCar.m_State & PoliceCarFlags.Returning) != 0 || nativeArray4.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, num);
						continue;
					}
					PathOwner pathOwner = nativeArray4[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num2 = math.min(policeCar.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					bool flag2 = false;
					if (num2 >= 1 && ((value2 & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) == 0 || m_PoliceEmergencyRequestData.HasComponent(dynamicBuffer[0].m_Request)))
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * policeCar.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag2 = true;
						}
					}
					for (int m = 1; m < num2; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						bool flag3 = m_PoliceEmergencyRequestData.HasComponent(request);
						if ((value2 & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) == 0 || flag3)
						{
							if (m_PathInformationData.TryGetComponent(request, out var componentData))
							{
								num += componentData.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
							}
							if (flag3)
							{
								num += 30f * targetSeeker2.m_PathfindParameters.m_Weights.time;
							}
							if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
							{
								pathElement = bufferData[bufferData.Length - 1];
								flag2 = true;
							}
						}
					}
					if (flag2)
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
	private struct SetupCrimeProducersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CrimeProducer> nativeArray2 = chunk.GetNativeArray(ref m_CrimeProducerType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			BufferAccessor<Employee> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EmployeeType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					CrimeProducer crimeProducer = nativeArray2[j];
					if ((bufferAccessor2.Length <= 0 || bufferAccessor2[j].Length > 0) && (bufferAccessor.Length <= 0 || bufferAccessor[j].Length > 0))
					{
						targetSeeker.FindTargets(entity2, (0f - random.NextFloat(0.5f, 0.7f)) * crimeProducer.m_Crime);
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
	private struct SetupPrisonerTransportJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Prison> m_PrisonType;

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
			NativeArray<Game.Buildings.Prison> nativeArray2 = chunk.GetNativeArray(ref m_PrisonType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.Prison prison = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity, out var targetSeeker);
						if ((prison.m_Flags & (PrisonFlags.HasAvailablePrisonVans | PrisonFlags.HasPrisonerSpace)) == (PrisonFlags.HasAvailablePrisonVans | PrisonFlags.HasPrisonerSpace))
						{
							Entity entity2 = nativeArray[i];
							if (AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
							{
								float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
								targetSeeker.FindTargets(entity2, cost);
							}
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
				if ((publicTransport.m_State & (PublicTransportFlags.PrisonerTransport | PublicTransportFlags.Disabled | PublicTransportFlags.Full)) != PublicTransportFlags.PrisonerTransport)
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
	private struct PrisonerTransportRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<PrisonerTransportRequest> m_PrisonerTransportRequestType;

		[ReadOnly]
		public ComponentLookup<PrisonerTransportRequest> m_PrisonerTransportRequestData;

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
			NativeArray<PrisonerTransportRequest> nativeArray3 = chunk.GetNativeArray(ref m_PrisonerTransportRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_PrisonerTransportRequestData.TryGetComponent(owner, out var componentData))
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
						PrisonerTransportRequest prisonerTransportRequest = nativeArray3[j];
						Entity district = Entity.Null;
						if (m_CurrentDistrictData.HasComponent(prisonerTransportRequest.m_Target))
						{
							district = m_CurrentDistrictData[prisonerTransportRequest.m_Target].m_District;
						}
						if (AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
						{
							targetSeeker.FindTargets(nativeArray[j], prisonerTransportRequest.m_Target, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
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
	private struct PoliceRequestsJob : IJobChunk
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
		public ComponentTypeHandle<PolicePatrolRequest> m_PolicePatrolRequestType;

		[ReadOnly]
		public ComponentTypeHandle<PoliceEmergencyRequest> m_PoliceEmergencyRequestType;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetTree;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<PolicePatrolRequest> nativeArray3 = chunk.GetNativeArray(ref m_PolicePatrolRequestType);
			NativeArray<PoliceEmergencyRequest> nativeArray4 = chunk.GetNativeArray(ref m_PoliceEmergencyRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				Entity entity2;
				PolicePurpose policePurpose;
				if (m_PolicePatrolRequestData.TryGetComponent(owner, out var componentData))
				{
					entity2 = componentData.m_Target;
					policePurpose = PolicePurpose.Patrol | PolicePurpose.Emergency | PolicePurpose.Intelligence;
				}
				else
				{
					if (!m_PoliceEmergencyRequestData.TryGetComponent(owner, out var componentData2))
					{
						continue;
					}
					entity2 = componentData2.m_Site;
					policePurpose = componentData2.m_Purpose;
				}
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				Entity service = Entity.Null;
				if (m_PoliceCarData.TryGetComponent(entity2, out var componentData3))
				{
					policePurpose &= componentData3.m_PurposeMask;
					if (targetSeeker.m_Owner.TryGetComponent(entity2, out var componentData4))
					{
						service = componentData4.m_Owner;
					}
				}
				else
				{
					if (!m_PoliceStationData.TryGetComponent(entity2, out var componentData5))
					{
						continue;
					}
					policePurpose &= componentData5.m_PurposeMask;
					service = entity2;
				}
				if ((policePurpose & PolicePurpose.Patrol) != 0)
				{
					for (int j = 0; j < nativeArray3.Length; j++)
					{
						if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
						{
							continue;
						}
						PolicePatrolRequest policePatrolRequest = nativeArray3[j];
						Entity district = Entity.Null;
						if (m_CurrentDistrictData.HasComponent(policePatrolRequest.m_Target))
						{
							district = m_CurrentDistrictData[policePatrolRequest.m_Target].m_District;
						}
						if (!AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
						{
							continue;
						}
						float cost = random.NextFloat(30f);
						targetSeeker.FindTargets(nativeArray[j], policePatrolRequest.m_Target, cost, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
						if (targetSeeker.m_Transform.TryGetComponent(policePatrolRequest.m_Target, out var componentData6) && (targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) != 0 && (targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
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
					}
				}
				if ((policePurpose & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) == 0)
				{
					continue;
				}
				targetSeeker.m_SetupQueueTarget.m_Methods &= PathMethod.Road;
				targetSeeker.m_SetupQueueTarget.m_RoadTypes &= RoadTypes.Car;
				for (int k = 0; k < nativeArray4.Length; k++)
				{
					if ((nativeArray2[k].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					PoliceEmergencyRequest policeEmergencyRequest = nativeArray4[k];
					if ((policePurpose & policeEmergencyRequest.m_Purpose) == 0)
					{
						continue;
					}
					Entity district2 = Entity.Null;
					Transform componentData7;
					if (m_CurrentDistrictData.HasComponent(policeEmergencyRequest.m_Target))
					{
						district2 = m_CurrentDistrictData[policeEmergencyRequest.m_Target].m_District;
					}
					else if (targetSeeker.m_Transform.TryGetComponent(policeEmergencyRequest.m_Target, out componentData7))
					{
						DistrictIterator iterator = new DistrictIterator
						{
							m_Position = componentData7.m_Position.xz,
							m_DistrictData = m_DistrictData,
							m_Nodes = targetSeeker.m_AreaNode,
							m_Triangles = targetSeeker.m_AreaTriangle
						};
						m_AreaTree.Iterate(ref iterator);
						district2 = iterator.m_Result;
					}
					if (!AreaUtils.CheckServiceDistrict(district2, service, m_ServiceDistricts))
					{
						continue;
					}
					if (m_AccidentSiteData.TryGetComponent(policeEmergencyRequest.m_Site, out var componentData8))
					{
						if (!m_TargetElements.TryGetBuffer(componentData8.m_Event, out var bufferData))
						{
							continue;
						}
						bool allowAccessRestriction = true;
						CheckTarget(nativeArray[k], policeEmergencyRequest.m_Site, componentData8, ref targetSeeker, ref allowAccessRestriction);
						for (int l = 0; l < bufferData.Length; l++)
						{
							Entity entity3 = bufferData[l].m_Entity;
							if (entity3 != policeEmergencyRequest.m_Site)
							{
								CheckTarget(nativeArray[k], entity3, componentData8, ref targetSeeker, ref allowAccessRestriction);
							}
						}
					}
					else
					{
						targetSeeker.FindTargets(nativeArray[k], policeEmergencyRequest.m_Target, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
					}
				}
			}
		}

		private void CheckTarget(Entity target, Entity entity, AccidentSite accidentSite, ref PathfindTargetSeeker<PathfindSetupBuffer> targetSeeker, ref bool allowAccessRestriction)
		{
			if ((accidentSite.m_Flags & AccidentSiteFlags.TrafficAccident) == 0 || m_CreatureData.HasComponent(entity) || m_VehicleData.HasComponent(entity))
			{
				int num = targetSeeker.FindTargets(target, entity, 0f, EdgeFlags.DefaultMask, allowAccessRestriction, navigationEnd: false);
				allowAccessRestriction &= num == 0;
				Entity entity2 = entity;
				if (targetSeeker.m_CurrentTransport.HasComponent(entity2))
				{
					entity2 = targetSeeker.m_CurrentTransport[entity2].m_CurrentTransport;
				}
				else if (targetSeeker.m_CurrentBuilding.HasComponent(entity2))
				{
					entity2 = targetSeeker.m_CurrentBuilding[entity2].m_CurrentBuilding;
				}
				if (targetSeeker.m_Transform.HasComponent(entity2))
				{
					float3 position = targetSeeker.m_Transform[entity2].m_Position;
					float num2 = 30f;
					CommonPathfindSetup.TargetIterator iterator = new CommonPathfindSetup.TargetIterator
					{
						m_Entity = target,
						m_Bounds = new Bounds3(position - num2, position + num2),
						m_Position = position,
						m_MaxDistance = num2,
						m_TargetSeeker = targetSeeker,
						m_Flags = EdgeFlags.DefaultMask,
						m_CompositionData = m_CompositionData,
						m_NetCompositionData = m_NetCompositionData
					};
					m_NetTree.Iterate(ref iterator);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_PolicePatrolQuery;

	private EntityQuery m_CrimeProducerQuery;

	private EntityQuery m_PrisonerTransportQuery;

	private EntityQuery m_PrisonerTransportRequestQuery;

	private EntityQuery m_PoliceRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<PrisonerTransportRequest> m_PrisonerTransportRequestType;

	private ComponentTypeHandle<PolicePatrolRequest> m_PolicePatrolRequestType;

	private ComponentTypeHandle<PoliceEmergencyRequest> m_PoliceEmergencyRequestType;

	private ComponentTypeHandle<Game.Buildings.PoliceStation> m_PoliceStationType;

	private ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

	private ComponentTypeHandle<Game.Buildings.Prison> m_PrisonType;

	private ComponentTypeHandle<Game.Vehicles.PoliceCar> m_PoliceCarType;

	private ComponentTypeHandle<Helicopter> m_HelicopterType;

	private ComponentTypeHandle<Game.Vehicles.PublicTransport> m_PublicTransportType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private BufferTypeHandle<Passenger> m_PassengerType;

	private BufferTypeHandle<Renter> m_RenterType;

	private BufferTypeHandle<Employee> m_EmployeeType;

	private ComponentLookup<PathInformation> m_PathInformationData;

	private ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

	private ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

	private ComponentLookup<PrisonerTransportRequest> m_PrisonerTransportRequestData;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<Composition> m_CompositionData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<District> m_DistrictData;

	private ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;

	private ComponentLookup<Creature> m_CreatureData;

	private ComponentLookup<Vehicle> m_VehicleData;

	private ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

	private ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

	private ComponentLookup<AccidentSite> m_AccidentSiteData;

	private ComponentLookup<NetCompositionData> m_NetCompositionData;

	private ComponentLookup<Game.City.City> m_CityData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private BufferLookup<TargetElement> m_TargetElements;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private CitySystem m_CitySystem;

	public PolicePathfindSetup(PathfindSetupSystem system)
	{
		m_PolicePatrolQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.PoliceStation>(),
				ComponentType.ReadOnly<Game.Vehicles.PoliceCar>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_CrimeProducerQuery = system.GetSetupQuery(ComponentType.ReadOnly<CrimeProducer>(), ComponentType.Exclude<PropertyOnMarket>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_PrisonerTransportQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.Prison>(),
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
		m_PrisonerTransportRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<PrisonerTransportRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_PoliceRequestQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<PolicePatrolRequest>(),
				ComponentType.ReadOnly<PoliceEmergencyRequest>()
			},
			None = new ComponentType[2]
			{
				ComponentType.Exclude<Dispatched>(),
				ComponentType.Exclude<PathInformation>()
			}
		});
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_PrisonerTransportRequestType = system.GetComponentTypeHandle<PrisonerTransportRequest>(isReadOnly: true);
		m_PolicePatrolRequestType = system.GetComponentTypeHandle<PolicePatrolRequest>(isReadOnly: true);
		m_PoliceEmergencyRequestType = system.GetComponentTypeHandle<PoliceEmergencyRequest>(isReadOnly: true);
		m_PoliceStationType = system.GetComponentTypeHandle<Game.Buildings.PoliceStation>(isReadOnly: true);
		m_CrimeProducerType = system.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
		m_PrisonType = system.GetComponentTypeHandle<Game.Buildings.Prison>(isReadOnly: true);
		m_PoliceCarType = system.GetComponentTypeHandle<Game.Vehicles.PoliceCar>(isReadOnly: true);
		m_HelicopterType = system.GetComponentTypeHandle<Helicopter>(isReadOnly: true);
		m_PublicTransportType = system.GetComponentTypeHandle<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_PassengerType = system.GetBufferTypeHandle<Passenger>(isReadOnly: true);
		m_RenterType = system.GetBufferTypeHandle<Renter>(isReadOnly: true);
		m_EmployeeType = system.GetBufferTypeHandle<Employee>(isReadOnly: true);
		m_PathInformationData = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_PolicePatrolRequestData = system.GetComponentLookup<PolicePatrolRequest>(isReadOnly: true);
		m_PoliceEmergencyRequestData = system.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
		m_PrisonerTransportRequestData = system.GetComponentLookup<PrisonerTransportRequest>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_CompositionData = system.GetComponentLookup<Composition>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_DistrictData = system.GetComponentLookup<District>(isReadOnly: true);
		m_PoliceStationData = system.GetComponentLookup<Game.Buildings.PoliceStation>(isReadOnly: true);
		m_CreatureData = system.GetComponentLookup<Creature>(isReadOnly: true);
		m_VehicleData = system.GetComponentLookup<Vehicle>(isReadOnly: true);
		m_PoliceCarData = system.GetComponentLookup<Game.Vehicles.PoliceCar>(isReadOnly: true);
		m_PublicTransportData = system.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_AccidentSiteData = system.GetComponentLookup<AccidentSite>(isReadOnly: true);
		m_NetCompositionData = system.GetComponentLookup<NetCompositionData>(isReadOnly: true);
		m_CityData = system.GetComponentLookup<Game.City.City>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_TargetElements = system.GetBufferLookup<TargetElement>(isReadOnly: true);
		m_AreaSearchSystem = system.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NetSearchSystem = system.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
	}

	public JobHandle SetupPolicePatrols(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PoliceStationType.Update(system);
		m_PoliceCarType.Update(system);
		m_HelicopterType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PassengerType.Update(system);
		m_PathInformationData.Update(system);
		m_PoliceEmergencyRequestData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		m_OutsideConnections.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupPolicePatrolsJob
		{
			m_EntityType = m_EntityType,
			m_PoliceStationType = m_PoliceStationType,
			m_PoliceCarType = m_PoliceCarType,
			m_HelicopterType = m_HelicopterType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PassengerType = m_PassengerType,
			m_PathInformationData = m_PathInformationData,
			m_PoliceEmergencyRequestData = m_PoliceEmergencyRequestData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_OutsideConnections = m_OutsideConnections,
			m_CityData = m_CityData,
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_PolicePatrolQuery, inputDeps);
	}

	public JobHandle SetupCrimeProducer(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_RenterType.Update(system);
		m_EmployeeType.Update(system);
		m_CrimeProducerType.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupCrimeProducersJob
		{
			m_EntityType = m_EntityType,
			m_CrimeProducerType = m_CrimeProducerType,
			m_RenterType = m_RenterType,
			m_EmployeeType = m_EmployeeType,
			m_SetupData = setupData
		}, m_CrimeProducerQuery, inputDeps);
	}

	public JobHandle SetupPrisonerTransport(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PrisonType.Update(system);
		m_PublicTransportType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupPrisonerTransportJob
		{
			m_EntityType = m_EntityType,
			m_PrisonType = m_PrisonType,
			m_PublicTransportType = m_PublicTransportType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_PrisonerTransportQuery, inputDeps);
	}

	public JobHandle SetupPrisonerTransportRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_PrisonerTransportRequestType.Update(system);
		m_PrisonerTransportRequestData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_PublicTransportData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new PrisonerTransportRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_PrisonerTransportRequestType = m_PrisonerTransportRequestType,
			m_PrisonerTransportRequestData = m_PrisonerTransportRequestData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_PublicTransportData = m_PublicTransportData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_PrisonerTransportRequestQuery, inputDeps);
	}

	public JobHandle SetupPoliceRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_PolicePatrolRequestType.Update(system);
		m_PoliceEmergencyRequestType.Update(system);
		m_PolicePatrolRequestData.Update(system);
		m_PoliceEmergencyRequestData.Update(system);
		m_CompositionData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_DistrictData.Update(system);
		m_CreatureData.Update(system);
		m_VehicleData.Update(system);
		m_PoliceCarData.Update(system);
		m_PoliceStationData.Update(system);
		m_AccidentSiteData.Update(system);
		m_NetCompositionData.Update(system);
		m_ServiceDistricts.Update(system);
		m_TargetElements.Update(system);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PoliceRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_PolicePatrolRequestType = m_PolicePatrolRequestType,
			m_PoliceEmergencyRequestType = m_PoliceEmergencyRequestType,
			m_PolicePatrolRequestData = m_PolicePatrolRequestData,
			m_PoliceEmergencyRequestData = m_PoliceEmergencyRequestData,
			m_CompositionData = m_CompositionData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_DistrictData = m_DistrictData,
			m_CreatureData = m_CreatureData,
			m_VehicleData = m_VehicleData,
			m_PoliceCarData = m_PoliceCarData,
			m_PoliceStationData = m_PoliceStationData,
			m_AccidentSiteData = m_AccidentSiteData,
			m_NetCompositionData = m_NetCompositionData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_TargetElements = m_TargetElements,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_NetTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_SetupData = setupData
		}, m_PoliceRequestQuery, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}
}
