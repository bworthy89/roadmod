#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TransportDepotAISystem : GameSystemBase
{
	private enum DepotActionType : byte
	{
		SetDisabled,
		ClearOdometer
	}

	private struct DepotAction
	{
		public Entity m_Entity;

		public DepotActionType m_Type;

		public bool m_Disabled;

		public static DepotAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new DepotAction
			{
				m_Entity = vehicle,
				m_Type = DepotActionType.SetDisabled,
				m_Disabled = disabled
			};
		}

		public static DepotAction ClearOdometer(Entity vehicle)
		{
			return new DepotAction
			{
				m_Entity = vehicle,
				m_Type = DepotActionType.ClearOdometer
			};
		}
	}

	[BurstCompile]
	private struct TransportDepotTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<SpectatorSite> m_SpectatorSiteType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_EventType;

		[ReadOnly]
		public ComponentTypeHandle<VehicleLaunchData> m_VehicleLaunchType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceRequestType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> m_TaxiRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> m_RouteColorData;

		[ReadOnly]
		public ComponentLookup<Produced> m_ProducedData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> m_TaxiData;

		[ReadOnly]
		public ComponentLookup<Odometer> m_OdometerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_PrefabTransportLineData;

		[ReadOnly]
		public ComponentLookup<TaxiData> m_PrefabTaxiData;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PrefabPublicTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> m_PrefabCargoTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<VehicleModel> m_VehicleModels;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocationElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_TransportVehicleRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_TaxiRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingTaxiAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingBusAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingTrainAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingTrainControllerAddTypes;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventPrefabChunks;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<DepotAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray4 = chunk.GetNativeArray(ref m_TransportDepotType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceRequestType);
			bool isOutsideConnection = chunk.Has(ref m_OutsideConnectionType);
			bool isSpectatorSite = chunk.Has(ref m_SpectatorSiteType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Objects.Transform transform = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				Game.Buildings.TransportDepot transportDepot = nativeArray4[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				TransportDepotData data = default(TransportDepotData);
				if (m_PrefabTransportDepotData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabTransportDepotData[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabTransportDepotData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				Tick(unfilteredChunkIndex, ref random, entity, transform, prefabRef, ref transportDepot, data, vehicles, dispatches, efficiency, immediateEfficiency, isOutsideConnection, isSpectatorSite);
				nativeArray4[i] = transportDepot;
			}
		}

		private void Tick(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Game.Objects.Transform transform, PrefabRef prefabRef, ref Game.Buildings.TransportDepot transportDepot, TransportDepotData prefabTransportDepotData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, float efficiency, float immediateEfficiency, bool isOutsideConnection, bool isSpectatorSite)
		{
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabTransportDepotData.m_VehicleCapacity);
			int num = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabTransportDepotData.m_VehicleCapacity);
			int num2 = vehicleCapacity;
			int num3 = 0;
			Entity entity2 = Entity.Null;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				bool flag;
				Game.Vehicles.CargoTransport componentData2;
				if (m_PublicTransportData.TryGetComponent(vehicle, out var componentData))
				{
					if ((componentData.m_State & PublicTransportFlags.DummyTraffic) != 0)
					{
						continue;
					}
					flag = (componentData.m_State & PublicTransportFlags.Disabled) != 0;
				}
				else if (m_CargoTransportData.TryGetComponent(vehicle, out componentData2))
				{
					if ((componentData2.m_State & CargoTransportFlags.DummyTraffic) != 0)
					{
						continue;
					}
					flag = (componentData2.m_State & CargoTransportFlags.Disabled) != 0;
				}
				else
				{
					if (!m_TaxiData.TryGetComponent(vehicle, out var componentData3))
					{
						continue;
					}
					flag = (componentData3.m_State & TaxiFlags.Disabled) != 0;
				}
				ParkedCar componentData4;
				bool flag2 = m_ParkedCarData.TryGetComponent(vehicle, out componentData4);
				ParkedTrain componentData5;
				bool flag3 = m_ParkedTrainData.TryGetComponent(vehicle, out componentData5);
				if (flag2 || flag3)
				{
					if (m_OdometerData.TryGetComponent(vehicle, out var componentData6) && componentData6.m_Distance != 0f)
					{
						CargoTransportVehicleData componentData8;
						TaxiData componentData9;
						if (m_PrefabPublicTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out var componentData7))
						{
							if (componentData7.m_MaintenanceRange > 0.1f)
							{
								transportDepot.m_MaintenanceRequirement += math.saturate(componentData6.m_Distance / componentData7.m_MaintenanceRange);
							}
						}
						else if (m_PrefabCargoTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out componentData8))
						{
							if (componentData8.m_MaintenanceRange > 0.1f)
							{
								transportDepot.m_MaintenanceRequirement += math.saturate(componentData6.m_Distance / componentData8.m_MaintenanceRange);
							}
						}
						else if (m_PrefabTaxiData.TryGetComponent(prefabRef.m_Prefab, out componentData9) && componentData9.m_MaintenanceRange > 0.1f)
						{
							transportDepot.m_MaintenanceRequirement += math.saturate(componentData6.m_Distance / componentData9.m_MaintenanceRange);
						}
						m_ActionQueue.Enqueue(DepotAction.ClearOdometer(vehicle));
					}
					if ((flag2 && !m_EntityLookup.Exists(componentData4.m_Lane)) || (flag3 && !m_EntityLookup.Exists(componentData5.m_ParkingLocation)))
					{
						m_LayoutElements.TryGetBuffer(vehicle, out var bufferData);
						VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicle, bufferData);
					}
					else
					{
						parkedVehicles.AddNoResize(vehicle);
					}
				}
				else
				{
					num2--;
					num3++;
					bool flag4 = --num < 0;
					if (flag != flag4)
					{
						m_ActionQueue.Enqueue(DepotAction.SetDisabled(vehicle, flag4));
					}
					if (m_ProducedData.HasComponent(vehicle))
					{
						entity2 = vehicle;
					}
				}
			}
			if (prefabTransportDepotData.m_MaintenanceDuration > 0f)
			{
				float num4 = 256f / (262144f * prefabTransportDepotData.m_MaintenanceDuration) * efficiency;
				transportDepot.m_MaintenanceRequirement = math.max(0f, transportDepot.m_MaintenanceRequirement - num4);
				num2 -= Mathf.CeilToInt(transportDepot.m_MaintenanceRequirement - 0.001f);
			}
			if (prefabTransportDepotData.m_ProductionDuration > 0f)
			{
				float num5 = 256f / (262144f * prefabTransportDepotData.m_ProductionDuration) * efficiency;
				if (num5 > 0f)
				{
					if (entity2 != Entity.Null)
					{
						Produced component = m_ProducedData[entity2];
						if (component.m_Completed < 1f)
						{
							component.m_Completed = math.min(1f, component.m_Completed + num5);
							m_CommandBuffer.SetComponent(jobIndex, entity2, component);
						}
						if (component.m_Completed == 1f && !isSpectatorSite)
						{
							TryCreateLaunchEvent(jobIndex, entity, prefabTransportDepotData);
						}
					}
					else if (num2 > 0 && !isSpectatorSite)
					{
						SpawnVehicle(jobIndex, ref random, entity, Entity.Null, transform, prefabRef, ref transportDepot, ref parkedVehicles, prefabTransportDepotData, num5);
						num2--;
						num3++;
					}
				}
			}
			int num6 = 0;
			bool flag5 = false;
			while (num6 < dispatches.Length)
			{
				Entity request = dispatches[num6].m_Request;
				if (m_TransportVehicleRequestData.HasComponent(request) || m_TaxiRequestData.HasComponent(request))
				{
					if (num2 > 0)
					{
						if (!flag5)
						{
							flag5 = SpawnVehicle(jobIndex, ref random, entity, request, transform, prefabRef, ref transportDepot, ref parkedVehicles, prefabTransportDepotData, 0f);
							dispatches.RemoveAt(num6);
							if (flag5)
							{
								num3++;
							}
						}
						if (flag5)
						{
							num2--;
						}
					}
					else
					{
						dispatches.RemoveAt(num6);
					}
				}
				else if (!m_ServiceRequestData.HasComponent(request))
				{
					dispatches.RemoveAt(num6);
				}
				else
				{
					num6++;
				}
			}
			while (parkedVehicles.Length > math.max(0, prefabTransportDepotData.m_VehicleCapacity - num3))
			{
				int index = random.NextInt(parkedVehicles.Length);
				Entity entity3 = parkedVehicles[index];
				m_LayoutElements.TryGetBuffer(entity3, out var bufferData2);
				VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity3, bufferData2);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity4 = parkedVehicles[j];
				bool flag6;
				Game.Vehicles.CargoTransport componentData11;
				if (m_PublicTransportData.TryGetComponent(entity4, out var componentData10))
				{
					flag6 = (componentData10.m_State & PublicTransportFlags.Disabled) != 0;
				}
				else if (m_CargoTransportData.TryGetComponent(entity4, out componentData11))
				{
					flag6 = (componentData11.m_State & CargoTransportFlags.Disabled) != 0;
				}
				else
				{
					if (!m_TaxiData.TryGetComponent(entity4, out var componentData12))
					{
						continue;
					}
					flag6 = (componentData12.m_State & TaxiFlags.Disabled) != 0;
				}
				bool flag7 = num2 <= 0;
				if (flag6 != flag7)
				{
					m_ActionQueue.Enqueue(DepotAction.SetDisabled(entity4, flag7));
				}
			}
			transportDepot.m_AvailableVehicles = (byte)math.clamp(num2, 0, 255);
			if (num2 > 0)
			{
				transportDepot.m_Flags |= TransportDepotFlags.HasAvailableVehicles;
				RequestTargetIfNeeded(jobIndex, entity, ref transportDepot, prefabTransportDepotData, num2, vehicleCapacity, isOutsideConnection);
			}
			else
			{
				transportDepot.m_Flags &= ~TransportDepotFlags.HasAvailableVehicles;
			}
			if (prefabTransportDepotData.m_DispatchCenter && efficiency > 0.001f)
			{
				transportDepot.m_Flags |= TransportDepotFlags.HasDispatchCenter;
			}
			else
			{
				transportDepot.m_Flags &= ~TransportDepotFlags.HasDispatchCenter;
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.TransportDepot transportDepot, TransportDepotData prefabTransportDepotData, int availableVehicles, int vehicleCapacity, bool isOutsideConnection)
		{
			if (!m_ServiceRequestData.HasComponent(transportDepot.m_TargetRequest))
			{
				if (prefabTransportDepotData.m_TransportType == TransportType.Taxi)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_TaxiRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new TaxiRequest(entity, Entity.Null, Entity.Null, isOutsideConnection ? TaxiRequestType.Outside : TaxiRequestType.None, availableVehicles));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
				}
				else
				{
					Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_TransportVehicleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e2, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e2, new TransportVehicleRequest(entity, (float)availableVehicles / (float)vehicleCapacity));
					m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(8u));
				}
			}
		}

		private bool TryCreateLaunchEvent(int jobIndex, Entity entity, TransportDepotData prefabTransportDepotData)
		{
			for (int i = 0; i < m_EventPrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_EventPrefabChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<EventData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_EventType);
				NativeArray<VehicleLaunchData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_VehicleLaunchType);
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_LockedType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((!enabledMask.EnableBit.IsValid || !enabledMask[j]) && nativeArray3[j].m_TransportType == prefabTransportDepotData.m_TransportType)
					{
						Entity prefab = nativeArray[j];
						EventData eventData = nativeArray2[j];
						Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
						m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(prefab));
						m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(entity));
						return true;
					}
				}
			}
			return false;
		}

		private bool SpawnVehicle(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, Game.Objects.Transform transform, PrefabRef prefabRef, ref Game.Buildings.TransportDepot transportDepot, ref StackList<Entity> parkedVehicles, TransportDepotData prefabTransportDepotData, float productionState)
		{
			Entity entity2 = Entity.Null;
			Entity entity3 = Entity.Null;
			Entity entity4 = Entity.Null;
			DynamicBuffer<VehicleModel> bufferData = default(DynamicBuffer<VehicleModel>);
			PublicTransportPurpose publicTransportPurpose = (PublicTransportPurpose)0;
			Resource resource = Resource.NoResource;
			int2 passengerCapacity = 0;
			int2 cargoCapacity = 0;
			TaxiRequestType taxiRequestType = TaxiRequestType.None;
			PathInformation componentData = default(PathInformation);
			if (productionState == 0f)
			{
				TaxiRequest componentData3;
				if (m_TransportVehicleRequestData.TryGetComponent(request, out var componentData2))
				{
					entity2 = componentData2.m_Route;
				}
				else if (m_TaxiRequestData.TryGetComponent(request, out componentData3))
				{
					entity2 = componentData3.m_Seeker;
					taxiRequestType = componentData3.m_Type;
					passengerCapacity = new int2(1, int.MaxValue);
				}
				if (!m_PrefabRefData.TryGetComponent(entity2, out var componentData4))
				{
					return false;
				}
				if (m_PrefabTransportLineData.TryGetComponent(componentData4.m_Prefab, out var componentData5))
				{
					publicTransportPurpose = (componentData5.m_PassengerTransport ? PublicTransportPurpose.TransportLine : ((PublicTransportPurpose)0));
					resource = (Resource)(componentData5.m_CargoTransport ? 8 : 0);
					passengerCapacity = (componentData5.m_PassengerTransport ? new int2(1, int.MaxValue) : ((int2)0));
					cargoCapacity = (componentData5.m_CargoTransport ? new int2(1, int.MaxValue) : ((int2)0));
				}
				if (!m_PathInformationData.TryGetComponent(request, out componentData))
				{
					return false;
				}
				entity3 = componentData.m_Destination;
				entity4 = componentData.m_Origin;
				if (!m_EntityLookup.Exists(entity3))
				{
					return false;
				}
				m_VehicleModels.TryGetBuffer(entity2, out bufferData);
			}
			else
			{
				if (m_ActivityLocationElements.TryGetBuffer(prefabRef.m_Prefab, out var bufferData2))
				{
					ActivityMask activityMask = new ActivityMask(ActivityType.Producing);
					for (int i = 0; i < bufferData2.Length; i++)
					{
						ActivityLocationElement activityLocationElement = bufferData2[i];
						if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
						{
							transform = ObjectUtils.LocalToWorld(transform, activityLocationElement.m_Position, activityLocationElement.m_Rotation);
						}
					}
				}
				publicTransportPurpose = PublicTransportPurpose.Other;
			}
			Entity entity5 = Entity.Null;
			if (entity4 != Entity.Null && entity4 != entity)
			{
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, entity4))
				{
					return false;
				}
				entity5 = entity4;
				m_LayoutElements.TryGetBuffer(entity5, out var bufferData3);
				switch (prefabTransportDepotData.m_TransportType)
				{
				case TransportType.Taxi:
				{
					ParkedCar parkedCar = m_ParkedCarData[entity5];
					Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
					m_CommandBuffer.RemoveComponent(jobIndex, entity5, in m_ParkedToMovingRemoveTypes);
					m_CommandBuffer.AddComponent(jobIndex, entity5, in m_ParkedToMovingTaxiAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity5, new CarCurrentLane(parkedCar, flags));
					if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
					}
					break;
				}
				case TransportType.Bus:
				{
					ParkedCar parkedCar2 = m_ParkedCarData[entity5];
					Game.Vehicles.CarLaneFlags flags2 = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
					m_CommandBuffer.RemoveComponent(jobIndex, entity5, in m_ParkedToMovingRemoveTypes);
					m_CommandBuffer.AddComponent(jobIndex, entity5, in m_ParkedToMovingBusAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity5, new CarCurrentLane(parkedCar2, flags2));
					if (m_ParkingLaneData.HasComponent(parkedCar2.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar2.m_Lane))
					{
						m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar2.m_Lane);
					}
					break;
				}
				case TransportType.Train:
				case TransportType.Tram:
				case TransportType.Subway:
				{
					for (int j = 0; j < bufferData3.Length; j++)
					{
						Entity vehicle = bufferData3[j].m_Vehicle;
						ParkedTrain parkedTrain = m_ParkedTrainData[vehicle];
						m_CommandBuffer.RemoveComponent(jobIndex, vehicle, in m_ParkedToMovingRemoveTypes);
						if (vehicle == entity5)
						{
							m_CommandBuffer.AddComponent(jobIndex, vehicle, in m_ParkedToMovingTrainControllerAddTypes);
							if (m_SpawnLocationData.HasComponent(parkedTrain.m_ParkingLocation))
							{
								m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedTrain.m_ParkingLocation);
							}
						}
						else
						{
							m_CommandBuffer.AddComponent(jobIndex, vehicle, in m_ParkedToMovingTrainAddTypes);
						}
						m_CommandBuffer.SetComponent(jobIndex, vehicle, new TrainCurrentLane(parkedTrain));
					}
					break;
				}
				}
			}
			if (entity5 == Entity.Null)
			{
				entity5 = m_TransportVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, transform, entity4, bufferData, prefabTransportDepotData.m_TransportType, prefabTransportDepotData.m_EnergyTypes, prefabTransportDepotData.m_SizeClass, publicTransportPurpose, resource, ref passengerCapacity, ref cargoCapacity, parked: false);
				if (entity5 == Entity.Null)
				{
					return false;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity5, new Owner(entity));
				TransportType transportType = prefabTransportDepotData.m_TransportType;
				if (transportType == TransportType.Train || transportType == TransportType.Tram || transportType == TransportType.Subway)
				{
					RemoveCollidingParkedTrain(jobIndex, request, ref parkedVehicles);
				}
			}
			m_CommandBuffer.SetComponent(jobIndex, entity5, new Target(entity3));
			if (productionState > 0f)
			{
				m_CommandBuffer.RemoveComponent<Moving>(jobIndex, entity5);
				m_CommandBuffer.RemoveComponent<TransformFrame>(jobIndex, entity5);
				m_CommandBuffer.RemoveComponent<InterpolatedTransform>(jobIndex, entity5);
				m_CommandBuffer.RemoveComponent<Swaying>(jobIndex, entity5);
				m_CommandBuffer.AddComponent(jobIndex, entity5, new Produced(productionState));
				m_CommandBuffer.AddComponent(jobIndex, entity5, default(Stopped));
			}
			else
			{
				bool flag = taxiRequestType == TaxiRequestType.Customer || taxiRequestType == TaxiRequestType.Outside;
				if (flag)
				{
					if (prefabTransportDepotData.m_TransportType == TransportType.Taxi)
					{
						TaxiFlags taxiFlags = TaxiFlags.Dispatched;
						if (taxiRequestType == TaxiRequestType.Outside)
						{
							taxiFlags |= TaxiFlags.FromOutside;
						}
						m_CommandBuffer.SetComponent(jobIndex, entity5, new Game.Vehicles.Taxi(taxiFlags));
						m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity5).Add(new ServiceDispatch(request));
					}
				}
				else if (entity2 != Entity.Null)
				{
					m_CommandBuffer.AddComponent(jobIndex, entity5, new CurrentRoute(entity2));
					if (entity4 == entity5)
					{
						m_CommandBuffer.AppendToBuffer(jobIndex, entity2, new RouteVehicle(entity5));
					}
					if (m_RouteColorData.TryGetComponent(entity2, out var componentData6))
					{
						m_CommandBuffer.AddComponent(jobIndex, entity5, componentData6);
					}
					if (publicTransportPurpose != 0)
					{
						m_CommandBuffer.SetComponent(jobIndex, entity5, new Game.Vehicles.PublicTransport
						{
							m_State = PublicTransportFlags.EnRoute
						});
					}
					if (resource != Resource.NoResource)
					{
						m_CommandBuffer.SetComponent(jobIndex, entity5, new Game.Vehicles.CargoTransport
						{
							m_State = CargoTransportFlags.EnRoute
						});
					}
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity5, !flag));
			}
			if (m_PathElements.TryGetBuffer(request, out var bufferData4) && bufferData4.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity5);
				PathUtils.CopyPath(bufferData4, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity5, new PathOwner(PathFlags.Updated));
				if (prefabTransportDepotData.m_TransportType != TransportType.Taxi)
				{
					m_CommandBuffer.SetComponent(jobIndex, entity5, componentData);
				}
			}
			if (m_ServiceRequestData.HasComponent(transportDepot.m_TargetRequest))
			{
				Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e2, new HandleRequest(transportDepot.m_TargetRequest, Entity.Null, completed: true));
			}
			return true;
		}

		private void RemoveCollidingParkedTrain(int jobIndex, Entity pathEntity, ref StackList<Entity> parkedVehicles)
		{
			if (!m_PathElements.TryGetBuffer(pathEntity, out var bufferData) || bufferData.Length == 0)
			{
				return;
			}
			Entity target = bufferData[0].m_Target;
			for (int i = 0; i < parkedVehicles.Length; i++)
			{
				Entity entity = parkedVehicles[i];
				if (m_ParkedTrainData.TryGetComponent(entity, out var componentData) && componentData.m_ParkingLocation == target)
				{
					m_LayoutElements.TryGetBuffer(entity, out var bufferData2);
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity, bufferData2);
					parkedVehicles.RemoveAtSwapBack(i);
					break;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct TransportDepotActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		public ComponentLookup<Game.Vehicles.Taxi> m_TaxiData;

		public ComponentLookup<Odometer> m_OdometerData;

		public NativeQueue<DepotAction> m_ActionQueue;

		public void Execute()
		{
			DepotAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case DepotActionType.SetDisabled:
				{
					if (m_PublicTransportData.TryGetComponent(item.m_Entity, out var componentData5))
					{
						if (item.m_Disabled)
						{
							componentData5.m_State |= PublicTransportFlags.AbandonRoute | PublicTransportFlags.Disabled;
						}
						else
						{
							componentData5.m_State &= ~PublicTransportFlags.Disabled;
						}
						m_PublicTransportData[item.m_Entity] = componentData5;
					}
					if (m_CargoTransportData.TryGetComponent(item.m_Entity, out var componentData6))
					{
						if (item.m_Disabled)
						{
							componentData6.m_State |= CargoTransportFlags.AbandonRoute | CargoTransportFlags.Disabled;
						}
						else
						{
							componentData6.m_State &= ~CargoTransportFlags.Disabled;
						}
						m_CargoTransportData[item.m_Entity] = componentData6;
					}
					if (m_TaxiData.TryGetComponent(item.m_Entity, out var componentData7))
					{
						if (item.m_Disabled)
						{
							componentData7.m_State |= TaxiFlags.Disabled;
						}
						else
						{
							componentData7.m_State &= ~TaxiFlags.Disabled;
						}
						m_TaxiData[item.m_Entity] = componentData7;
					}
					break;
				}
				case DepotActionType.ClearOdometer:
				{
					if (m_OdometerData.TryGetComponent(item.m_Entity, out var componentData))
					{
						componentData.m_Distance = 0f;
						m_OdometerData[item.m_Entity] = componentData;
					}
					if (m_PublicTransportData.TryGetComponent(item.m_Entity, out var componentData2))
					{
						componentData2.m_State &= ~PublicTransportFlags.RequiresMaintenance;
						m_PublicTransportData[item.m_Entity] = componentData2;
					}
					if (m_CargoTransportData.TryGetComponent(item.m_Entity, out var componentData3))
					{
						componentData3.m_State &= ~CargoTransportFlags.RequiresMaintenance;
						m_CargoTransportData[item.m_Entity] = componentData3;
					}
					if (m_TaxiData.TryGetComponent(item.m_Entity, out var componentData4))
					{
						componentData4.m_State &= ~TaxiFlags.RequiresMaintenance;
						m_TaxiData[item.m_Entity] = componentData4;
					}
					break;
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
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpectatorSite> __Game_Events_SpectatorSite_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<VehicleLaunchData> __Game_Prefabs_VehicleLaunchData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> __Game_Simulation_TaxiRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> __Game_Routes_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Produced> __Game_Vehicles_Produced_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Odometer> __Game_Vehicles_Odometer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiData> __Game_Prefabs_TaxiData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<VehicleModel> __Game_Routes_VehicleModel_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RW_ComponentLookup;

		public ComponentLookup<Odometer> __Game_Vehicles_Odometer_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Events_SpectatorSite_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpectatorSite>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_VehicleLaunchData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<VehicleLaunchData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_TransportDepot_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportDepot>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup = state.GetComponentLookup<TransportVehicleRequest>(isReadOnly: true);
			__Game_Simulation_TaxiRequest_RO_ComponentLookup = state.GetComponentLookup<TaxiRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Color>(isReadOnly: true);
			__Game_Vehicles_Produced_RO_ComponentLookup = state.GetComponentLookup<Produced>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Vehicles_Odometer_RO_ComponentLookup = state.GetComponentLookup<Odometer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_TaxiData_RO_ComponentLookup = state.GetComponentLookup<TaxiData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Routes_VehicleModel_RO_BufferLookup = state.GetBufferLookup<VehicleModel>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>();
			__Game_Vehicles_CargoTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>();
			__Game_Vehicles_Taxi_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Taxi>();
			__Game_Vehicles_Odometer_RW_ComponentLookup = state.GetComponentLookup<Odometer>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EntityQuery m_EventPrefabQuery;

	private EntityArchetype m_TransportVehicleRequestArchetype;

	private EntityArchetype m_TaxiRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingTaxiAddTypes;

	private ComponentTypeSet m_ParkedToMovingBusAddTypes;

	private ComponentTypeSet m_ParkedToMovingTrainAddTypes;

	private ComponentTypeSet m_ParkedToMovingTrainControllerAddTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 32;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TransportDepot>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_EventPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<VehicleLaunchData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Locked>());
		m_TransportVehicleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TransportVehicleRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_TaxiRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TaxiRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_ParkedToMovingRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<ParkedTrain>(), ComponentType.ReadWrite<Stopped>());
		m_ParkedToMovingTaxiAddTypes = new ComponentTypeSet(new ComponentType[13]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingBusAddTypes = new ComponentTypeSet(new ComponentType[14]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingTrainAddTypes = new ComponentTypeSet(new ComponentType[7]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<TrainNavigation>(),
			ComponentType.ReadWrite<TrainCurrentLane>(),
			ComponentType.ReadWrite<TrainBogieFrame>(),
			ComponentType.ReadWrite<Updated>()
		});
		m_ParkedToMovingTrainControllerAddTypes = new ComponentTypeSet(new ComponentType[14]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<TrainNavigation>(),
			ComponentType.ReadWrite<TrainNavigationLane>(),
			ComponentType.ReadWrite<TrainCurrentLane>(),
			ComponentType.ReadWrite<TrainBogieFrame>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Updated>()
		});
		RequireForUpdate(m_BuildingQuery);
		Assert.IsTrue(condition: true);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> eventPrefabChunks = m_EventPrefabQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		NativeQueue<DepotAction> actionQueue = new NativeQueue<DepotAction>(Allocator.TempJob);
		TransportDepotTickJob jobData = new TransportDepotTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SpectatorSiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_SpectatorSite_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleLaunchType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_VehicleLaunchData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportDepot_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_TransportVehicleRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Color_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProducedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Produced_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OdometerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Odometer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TaxiData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCargoTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_VehicleModels = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_VehicleModel_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocationElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TransportVehicleRequestArchetype = m_TransportVehicleRequestArchetype,
			m_TaxiRequestArchetype = m_TaxiRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingTaxiAddTypes = m_ParkedToMovingTaxiAddTypes,
			m_ParkedToMovingBusAddTypes = m_ParkedToMovingBusAddTypes,
			m_ParkedToMovingTrainAddTypes = m_ParkedToMovingTrainAddTypes,
			m_ParkedToMovingTrainControllerAddTypes = m_ParkedToMovingTrainControllerAddTypes,
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_EventPrefabChunks = eventPrefabChunks,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		TransportDepotActionJob jobData2 = new TransportDepotActionJob
		{
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Taxi_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OdometerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle, outJobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		eventPrefabChunks.Dispose(jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_TransportVehicleSelectData.PostUpdate(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle3;
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
	public TransportDepotAISystem()
	{
	}
}
