#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
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
public class GarbageFacilityAISystem : GameSystemBase
{
	private struct GarbageFacilityAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static GarbageFacilityAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new GarbageFacilityAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct GarbageFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.SubArea> m_SubAreaType;

		public ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Game.Economy.Resources> m_ResourcesType;

		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public BufferTypeHandle<GuestVehicle> m_GuestVehicleType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		[ReadOnly]
		public ComponentLookup<GarbageTransferRequest> m_GarbageTransferRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<ReturnLoad> m_ReturnLoadData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Geometry> m_AreaGeometryData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_PrefabGarbageFacilityData;

		[ReadOnly]
		public ComponentLookup<GarbageTruckData> m_PrefabGarbageTruckData;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> m_PrefabStorageAreaData;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> m_ResourceProductionDatas;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Storage> m_AreaStorageData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameters;

		[ReadOnly]
		public GarbageTruckSelectData m_GarbageTruckSelectData;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public EntityArchetype m_GarbageTransferRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_GarbageCollectionRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<GarbageFacilityAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.GarbageFacility> nativeArray4 = chunk.GetNativeArray(ref m_GarbageFacilityType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<GuestVehicle> bufferAccessor4 = chunk.GetBufferAccessor(ref m_GuestVehicleType);
			BufferAccessor<Game.Areas.SubArea> bufferAccessor5 = chunk.GetBufferAccessor(ref m_SubAreaType);
			BufferAccessor<ServiceDispatch> bufferAccessor6 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor7 = chunk.GetBufferAccessor(ref m_ResourcesType);
			NativeList<ResourceProductionData> resourceProductionBuffer = default(NativeList<ResourceProductionData>);
			bool outside = chunk.Has(ref m_OutsideConnectionType);
			m_DeliveryTruckSelectData.GetCapacityRange(Resource.Garbage, out var min, out var max);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray3[i];
				Game.Buildings.GarbageFacility garbageFacility = nativeArray4[i];
				DynamicBuffer<OwnedVehicle> ownedVehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor6[i];
				DynamicBuffer<GuestVehicle> guestVehicles = default(DynamicBuffer<GuestVehicle>);
				if (bufferAccessor4.Length != 0)
				{
					guestVehicles = bufferAccessor4[i];
				}
				GarbageFacilityData data = m_PrefabGarbageFacilityData[prefabRef.m_Prefab];
				if (m_ResourceProductionDatas.HasBuffer(prefabRef.m_Prefab))
				{
					AddResourceProductionData(m_ResourceProductionDatas[prefabRef.m_Prefab], ref resourceProductionBuffer);
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabGarbageFacilityData);
					CombineResourceProductionData(bufferAccessor2[i], ref resourceProductionBuffer);
				}
				int garbageAmount = 0;
				DynamicBuffer<Game.Economy.Resources> resources = default(DynamicBuffer<Game.Economy.Resources>);
				if (bufferAccessor7.Length != 0)
				{
					resources = bufferAccessor7[i];
					garbageAmount = EconomyUtils.GetResources(Resource.Garbage, resources);
				}
				int num = garbageAmount;
				Building building = default(Building);
				if (nativeArray2.Length != 0)
				{
					building = nativeArray2[i];
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				int areaCapacity = 0;
				int areaGarbage = 0;
				if (bufferAccessor5.Length != 0)
				{
					DynamicBuffer<Game.Areas.SubArea> subAreas = bufferAccessor5[i];
					ProcessAreas(unfilteredChunkIndex, subAreas, ref garbageAmount, data, max, efficiency, out areaCapacity, out areaGarbage);
				}
				Tick(unfilteredChunkIndex, entity, building, ref random, ref garbageFacility, ref garbageAmount, data, efficiency, immediateEfficiency, areaCapacity, areaGarbage, min, max, ownedVehicles, guestVehicles, dispatches, resources, resourceProductionBuffer, outside);
				nativeArray4[i] = garbageFacility;
				if (resources.IsCreated)
				{
					EconomyUtils.SetResources(Resource.Garbage, resources, garbageAmount);
					int num2 = Mathf.RoundToInt((float)num / (float)math.max(1, data.m_GarbageCapacity) * 100f);
					int num3 = Mathf.RoundToInt((float)garbageAmount / (float)math.max(1, data.m_GarbageCapacity) * 100f);
					int4 @int = new int4(0, 33, 50, 66);
					if (math.any(num2 > @int != num3 > @int))
					{
						QuantityUpdated(unfilteredChunkIndex, entity);
					}
				}
				if (resourceProductionBuffer.IsCreated)
				{
					resourceProductionBuffer.Clear();
				}
			}
			if (resourceProductionBuffer.IsCreated)
			{
				resourceProductionBuffer.Dispose();
			}
		}

		private void QuantityUpdated(int jobIndex, Entity buildingEntity, bool updateAll = false)
		{
			if (!m_SubObjects.TryGetBuffer(buildingEntity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				bool updateAll2 = false;
				if (updateAll || m_QuantityData.HasComponent(subObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
					updateAll2 = true;
				}
				QuantityUpdated(jobIndex, subObject, updateAll2);
			}
		}

		private void AddResourceProductionData(DynamicBuffer<ResourceProductionData> resourceProductionDatas, ref NativeList<ResourceProductionData> resourceProductionBuffer)
		{
			if (!resourceProductionBuffer.IsCreated)
			{
				resourceProductionBuffer = new NativeList<ResourceProductionData>(resourceProductionDatas.Length, Allocator.Temp);
			}
			ResourceProductionData.Combine(resourceProductionBuffer, resourceProductionDatas);
		}

		private void CombineResourceProductionData(DynamicBuffer<InstalledUpgrade> upgrades, ref NativeList<ResourceProductionData> resourceProductionBuffer)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
				{
					PrefabRef prefabRef = m_PrefabRefData[installedUpgrade.m_Upgrade];
					if (m_ResourceProductionDatas.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
					{
						AddResourceProductionData(bufferData, ref resourceProductionBuffer);
					}
				}
			}
		}

		private void ProcessAreas(int jobIndex, DynamicBuffer<Game.Areas.SubArea> subAreas, ref int garbageAmount, GarbageFacilityData prefabGarbageFacilityData, int maxGarbageLoad, float efficiency, out int areaCapacity, out int areaGarbage)
		{
			areaCapacity = 0;
			areaGarbage = 0;
			for (int i = 0; i < subAreas.Length; i++)
			{
				Entity area = subAreas[i].m_Area;
				if (!m_AreaStorageData.HasComponent(area))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[area];
				Storage value = m_AreaStorageData[area];
				Geometry geometry = m_AreaGeometryData[area];
				StorageAreaData prefabStorageData = m_PrefabStorageAreaData[prefabRef.m_Prefab];
				int num = AreaUtils.CalculateStorageCapacity(geometry, prefabStorageData);
				int num2 = (int)((long)value.m_Amount * (long)prefabGarbageFacilityData.m_GarbageCapacity / (num * 2));
				float num3 = 0.0009765625f;
				float f = (float)prefabGarbageFacilityData.m_ProcessingSpeed * num3;
				num2 += maxGarbageLoad + Mathf.CeilToInt(f);
				int x = math.min(garbageAmount - num2, num - value.m_Amount);
				x = math.max(x, -math.min(value.m_Amount, prefabGarbageFacilityData.m_GarbageCapacity - garbageAmount));
				int num4 = Mathf.CeilToInt((float)math.abs(x) * math.saturate(efficiency));
				x = math.select(num4, -num4, x < 0);
				if (x != 0)
				{
					int num5 = (int)((long)value.m_Amount * 100L / num);
					value.m_Amount += x;
					value.m_WorkAmount += num4;
					garbageAmount -= x;
					m_AreaStorageData[area] = value;
					if ((int)((long)value.m_Amount * 100L / num) != num5)
					{
						m_CommandBuffer.AddComponent(jobIndex, area, default(Updated));
					}
				}
				areaCapacity += num;
				areaGarbage += value.m_Amount;
			}
		}

		private void Tick(int jobIndex, Entity entity, Building building, ref Unity.Mathematics.Random random, ref Game.Buildings.GarbageFacility garbageFacility, ref int garbageAmount, GarbageFacilityData prefabGarbageFacilityData, float efficiency, float immediateEfficiency, int areaCapacity, int areaGarbage, int minGarbageLoad, int maxGarbageLoad, DynamicBuffer<OwnedVehicle> ownedVehicles, DynamicBuffer<GuestVehicle> guestVehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Game.Economy.Resources> resources, NativeList<ResourceProductionData> resourceProductionBuffer, bool outside)
		{
			if (outside)
			{
				int num = prefabGarbageFacilityData.m_GarbageCapacity / 2 - garbageAmount;
				if (num != 0)
				{
					garbageAmount += num;
				}
			}
			float num2 = math.min(garbageAmount, (float)prefabGarbageFacilityData.m_ProcessingSpeed / 1024f);
			float num3 = CalculateProcessingRate(num2, efficiency, garbageAmount, prefabGarbageFacilityData.m_GarbageCapacity);
			float num4 = ((num2 > 0f) ? (num3 / num2) : 0f);
			if (resourceProductionBuffer.IsCreated)
			{
				for (int i = 0; i < resourceProductionBuffer.Length; i++)
				{
					ResourceProductionData resourceProductionData = resourceProductionBuffer[i];
					float num5 = (float)resourceProductionData.m_ProductionRate / 1024f;
					if (num5 > 0f)
					{
						int resources2 = EconomyUtils.GetResources(resourceProductionData.m_Type, resources);
						float num6 = math.clamp(resourceProductionData.m_StorageCapacity - resources2, 0f, num5);
						num4 = math.min(num4, num6 / num5);
					}
				}
				for (int j = 0; j < resourceProductionBuffer.Length; j++)
				{
					ResourceProductionData resourceProductionData2 = resourceProductionBuffer[j];
					float num7 = (float)resourceProductionData2.m_ProductionRate / 1024f;
					EconomyUtils.AddResources(amount: MathUtils.RoundToIntRandom(ref random, num4 * num7), resource: resourceProductionData2.m_Type, resources: resources);
				}
			}
			num3 = num4 * num2;
			garbageFacility.m_ProcessingRate = Mathf.RoundToInt(num3 * 1024f);
			int num8 = MathUtils.RoundToIntRandom(ref random, num3);
			garbageAmount -= num8;
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabGarbageFacilityData.m_VehicleCapacity);
			int num9 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabGarbageFacilityData.m_VehicleCapacity);
			int availableVehicles = vehicleCapacity;
			int availableDeliveryTrucks = prefabGarbageFacilityData.m_TransportCapacity;
			int availableSpace = prefabGarbageFacilityData.m_GarbageCapacity - garbageAmount + areaCapacity - areaGarbage;
			int availableGarbage = garbageAmount + areaGarbage - num8 * 2;
			StackList<Entity> parkedVehicles = stackalloc Entity[ownedVehicles.Length];
			for (int k = 0; k < ownedVehicles.Length; k++)
			{
				Entity vehicle = ownedVehicles[k].m_Vehicle;
				Game.Vehicles.DeliveryTruck componentData3;
				if (m_GarbageTruckData.TryGetComponent(vehicle, out var componentData))
				{
					if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData2))
					{
						if (!m_EntityLookup.Exists(componentData2.m_Lane))
						{
							m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
						}
						else
						{
							parkedVehicles.AddNoResize(vehicle);
						}
						continue;
					}
					PrefabRef prefabRef = m_PrefabRefData[vehicle];
					GarbageTruckData garbageTruckData = m_PrefabGarbageTruckData[prefabRef.m_Prefab];
					availableVehicles--;
					availableSpace -= garbageTruckData.m_GarbageCapacity;
					bool flag = --num9 < 0;
					if ((componentData.m_State & GarbageTruckFlags.Disabled) != 0 != flag)
					{
						m_ActionQueue.Enqueue(GarbageFacilityAction.SetDisabled(vehicle, flag));
					}
				}
				else if (m_DeliveryTruckData.TryGetComponent(vehicle, out componentData3))
				{
					if ((componentData3.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
					{
						continue;
					}
					DynamicBuffer<LayoutElement> dynamicBuffer = default(DynamicBuffer<LayoutElement>);
					if (m_LayoutElements.HasBuffer(vehicle))
					{
						dynamicBuffer = m_LayoutElements[vehicle];
					}
					if (dynamicBuffer.IsCreated && dynamicBuffer.Length != 0)
					{
						for (int l = 0; l < dynamicBuffer.Length; l++)
						{
							Entity vehicle2 = dynamicBuffer[l].m_Vehicle;
							if (!m_DeliveryTruckData.HasComponent(vehicle2))
							{
								continue;
							}
							Game.Vehicles.DeliveryTruck deliveryTruck = m_DeliveryTruckData[vehicle2];
							if ((deliveryTruck.m_Resource & Resource.Garbage) != Resource.NoResource && (componentData3.m_State & DeliveryTruckFlags.Buying) != 0)
							{
								availableSpace -= deliveryTruck.m_Amount;
							}
							if (m_ReturnLoadData.HasComponent(vehicle2))
							{
								ReturnLoad returnLoad = m_ReturnLoadData[vehicle2];
								if ((returnLoad.m_Resource & Resource.Garbage) != Resource.NoResource)
								{
									availableSpace -= returnLoad.m_Amount;
								}
							}
						}
					}
					else
					{
						if ((componentData3.m_Resource & Resource.Garbage) != Resource.NoResource && (componentData3.m_State & DeliveryTruckFlags.Buying) != 0)
						{
							availableSpace -= componentData3.m_Amount;
						}
						if (m_ReturnLoadData.HasComponent(vehicle))
						{
							ReturnLoad returnLoad2 = m_ReturnLoadData[vehicle];
							if ((returnLoad2.m_Resource & Resource.Garbage) != Resource.NoResource)
							{
								availableSpace -= returnLoad2.m_Amount;
							}
						}
					}
					availableDeliveryTrucks--;
				}
				else if (!m_EntityLookup.Exists(vehicle))
				{
					ownedVehicles.RemoveAt(k--);
				}
			}
			if (guestVehicles.IsCreated)
			{
				for (int m = 0; m < guestVehicles.Length; m++)
				{
					Entity vehicle3 = guestVehicles[m].m_Vehicle;
					if (!m_TargetData.HasComponent(vehicle3) || m_TargetData[vehicle3].m_Target != entity)
					{
						guestVehicles.RemoveAt(m--);
					}
					else
					{
						if (!m_DeliveryTruckData.HasComponent(vehicle3))
						{
							continue;
						}
						Game.Vehicles.DeliveryTruck deliveryTruck2 = m_DeliveryTruckData[vehicle3];
						if ((deliveryTruck2.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
						{
							continue;
						}
						DynamicBuffer<LayoutElement> dynamicBuffer2 = default(DynamicBuffer<LayoutElement>);
						if (m_LayoutElements.HasBuffer(vehicle3))
						{
							dynamicBuffer2 = m_LayoutElements[vehicle3];
						}
						if (dynamicBuffer2.IsCreated && dynamicBuffer2.Length != 0)
						{
							for (int n = 0; n < dynamicBuffer2.Length; n++)
							{
								Entity vehicle4 = dynamicBuffer2[n].m_Vehicle;
								if (!m_DeliveryTruckData.HasComponent(vehicle4))
								{
									continue;
								}
								Game.Vehicles.DeliveryTruck deliveryTruck3 = m_DeliveryTruckData[vehicle4];
								if ((deliveryTruck2.m_State & DeliveryTruckFlags.Buying) != 0)
								{
									if ((deliveryTruck3.m_Resource & Resource.Garbage) != Resource.NoResource)
									{
										availableGarbage -= deliveryTruck3.m_Amount;
									}
								}
								else if ((deliveryTruck3.m_Resource & Resource.Garbage) != Resource.NoResource)
								{
									availableSpace -= deliveryTruck3.m_Amount;
								}
								if (m_ReturnLoadData.HasComponent(vehicle4))
								{
									ReturnLoad returnLoad3 = m_ReturnLoadData[vehicle4];
									if ((returnLoad3.m_Resource & Resource.Garbage) != Resource.NoResource)
									{
										availableGarbage -= returnLoad3.m_Amount;
									}
								}
							}
							continue;
						}
						if ((deliveryTruck2.m_State & DeliveryTruckFlags.Buying) != 0)
						{
							if ((deliveryTruck2.m_Resource & Resource.Garbage) != Resource.NoResource)
							{
								availableGarbage -= deliveryTruck2.m_Amount;
							}
						}
						else if ((deliveryTruck2.m_Resource & Resource.Garbage) != Resource.NoResource)
						{
							availableSpace -= deliveryTruck2.m_Amount;
						}
						if (m_ReturnLoadData.HasComponent(vehicle3))
						{
							ReturnLoad returnLoad4 = m_ReturnLoadData[vehicle3];
							if ((returnLoad4.m_Resource & Resource.Garbage) != Resource.NoResource)
							{
								availableGarbage -= returnLoad4.m_Amount;
							}
						}
					}
				}
			}
			if (BuildingUtils.CheckOption(building, BuildingOption.Empty))
			{
				availableSpace = 0;
			}
			for (int num10 = 0; num10 < dispatches.Length; num10++)
			{
				Entity request = dispatches[num10].m_Request;
				if (m_GarbageCollectionRequestData.HasComponent(request))
				{
					TrySpawnGarbageTruck(jobIndex, ref random, entity, request, prefabGarbageFacilityData, ref garbageFacility, ref availableVehicles, ref availableSpace, ref parkedVehicles);
					dispatches.RemoveAt(num10--);
				}
				else if (m_GarbageTransferRequestData.HasComponent(request))
				{
					TrySpawnDeliveryTruck(jobIndex, ref random, entity, request, ref availableDeliveryTrucks, ref availableSpace, ref availableGarbage, ref garbageAmount);
					dispatches.RemoveAt(num10--);
				}
				else if (!m_ServiceRequestData.HasComponent(request))
				{
					dispatches.RemoveAt(num10--);
				}
			}
			while (parkedVehicles.Length > math.max(0, prefabGarbageFacilityData.m_VehicleCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			for (int num11 = 0; num11 < parkedVehicles.Length; num11++)
			{
				Entity entity2 = parkedVehicles[num11];
				Game.Vehicles.GarbageTruck garbageTruck = m_GarbageTruckData[entity2];
				bool flag2 = availableVehicles <= 0 || availableSpace <= 0;
				if ((garbageTruck.m_State & GarbageTruckFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(GarbageFacilityAction.SetDisabled(entity2, flag2));
				}
			}
			if (availableGarbage > 0 && BuildingUtils.CheckOption(building, BuildingOption.Empty))
			{
				garbageFacility.m_DeliverGarbagePriority = 2f;
			}
			else if (availableGarbage >= minGarbageLoad)
			{
				garbageFacility.m_DeliverGarbagePriority = (float)availableGarbage / (float)(prefabGarbageFacilityData.m_GarbageCapacity + areaCapacity + maxGarbageLoad);
			}
			else
			{
				garbageFacility.m_DeliverGarbagePriority = 0f;
			}
			if (availableSpace >= minGarbageLoad)
			{
				garbageFacility.m_AcceptGarbagePriority = (float)availableSpace / (float)(prefabGarbageFacilityData.m_GarbageCapacity + areaCapacity + maxGarbageLoad);
			}
			else
			{
				garbageFacility.m_AcceptGarbagePriority = 0f;
			}
			if (!outside)
			{
				if (garbageFacility.m_AcceptGarbagePriority > 0f)
				{
					GarbageTransferRequestFlags garbageTransferRequestFlags = GarbageTransferRequestFlags.Deliver;
					if (availableDeliveryTrucks <= 0)
					{
						garbageTransferRequestFlags |= GarbageTransferRequestFlags.RequireTransport;
					}
					int amount = math.min(availableSpace, maxGarbageLoad);
					if (m_GarbageTransferRequestData.HasComponent(garbageFacility.m_GarbageDeliverRequest))
					{
						if (m_GarbageTransferRequestData[garbageFacility.m_GarbageDeliverRequest].m_Flags != garbageTransferRequestFlags)
						{
							Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(garbageFacility.m_GarbageDeliverRequest, Entity.Null, completed: true));
						}
						else
						{
							garbageTransferRequestFlags = (GarbageTransferRequestFlags)0;
						}
					}
					if (garbageTransferRequestFlags != 0)
					{
						Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_GarbageTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e2, new GarbageTransferRequest(entity, garbageTransferRequestFlags, garbageFacility.m_AcceptGarbagePriority, amount));
						m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(8u));
					}
				}
				if (garbageFacility.m_DeliverGarbagePriority > 0f)
				{
					GarbageTransferRequestFlags garbageTransferRequestFlags2 = GarbageTransferRequestFlags.Receive;
					if (availableDeliveryTrucks <= 0)
					{
						garbageTransferRequestFlags2 |= GarbageTransferRequestFlags.RequireTransport;
					}
					int amount2 = math.min(availableGarbage, maxGarbageLoad);
					if (m_GarbageTransferRequestData.HasComponent(garbageFacility.m_GarbageReceiveRequest))
					{
						if (m_GarbageTransferRequestData[garbageFacility.m_GarbageReceiveRequest].m_Flags != garbageTransferRequestFlags2)
						{
							Entity e3 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e3, new HandleRequest(garbageFacility.m_GarbageReceiveRequest, Entity.Null, completed: true));
						}
						else
						{
							garbageTransferRequestFlags2 = (GarbageTransferRequestFlags)0;
						}
					}
					if (garbageTransferRequestFlags2 != 0)
					{
						Entity e4 = m_CommandBuffer.CreateEntity(jobIndex, m_GarbageTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e4, new GarbageTransferRequest(entity, garbageTransferRequestFlags2, garbageFacility.m_DeliverGarbagePriority, amount2));
						m_CommandBuffer.SetComponent(jobIndex, e4, new RequestGroup(8u));
					}
				}
			}
			if (prefabGarbageFacilityData.m_LongTermStorage)
			{
				if (garbageAmount + areaGarbage >= prefabGarbageFacilityData.m_GarbageCapacity + areaCapacity)
				{
					if ((garbageFacility.m_Flags & GarbageFacilityFlags.IsFull) == 0)
					{
						m_IconCommandBuffer.Add(entity, m_GarbageParameters.m_FacilityFullNotificationPrefab);
						garbageFacility.m_Flags |= GarbageFacilityFlags.IsFull;
					}
				}
				else if ((garbageFacility.m_Flags & GarbageFacilityFlags.IsFull) != 0)
				{
					m_IconCommandBuffer.Remove(entity, m_GarbageParameters.m_FacilityFullNotificationPrefab);
					garbageFacility.m_Flags &= ~GarbageFacilityFlags.IsFull;
				}
			}
			if (availableVehicles > 0)
			{
				garbageFacility.m_Flags |= GarbageFacilityFlags.HasAvailableGarbageTrucks;
			}
			else
			{
				garbageFacility.m_Flags &= ~GarbageFacilityFlags.HasAvailableGarbageTrucks;
			}
			if (availableSpace > 0)
			{
				garbageFacility.m_Flags |= GarbageFacilityFlags.HasAvailableSpace;
			}
			else
			{
				garbageFacility.m_Flags &= ~GarbageFacilityFlags.HasAvailableSpace;
			}
			if (prefabGarbageFacilityData.m_IndustrialWasteOnly)
			{
				garbageFacility.m_Flags |= GarbageFacilityFlags.IndustrialWasteOnly;
			}
			else
			{
				garbageFacility.m_Flags &= ~GarbageFacilityFlags.IndustrialWasteOnly;
			}
			if (availableVehicles > 0 && availableSpace > 0)
			{
				RequestTargetIfNeeded(jobIndex, entity, ref garbageFacility, prefabGarbageFacilityData, availableVehicles);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.GarbageFacility garbageFacility, GarbageFacilityData prefabGarbageFacilityData, int availableVehicles)
		{
			if (!m_ServiceRequestData.HasComponent(garbageFacility.m_TargetRequest))
			{
				uint num = math.max(512u, 256u);
				if ((m_SimulationFrameIndex & (num - 1)) == 80)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_GarbageCollectionRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new GarbageCollectionRequest(entity, availableVehicles, prefabGarbageFacilityData.m_IndustrialWasteOnly ? GarbageCollectionRequestFlags.IndustrialWaste : ((GarbageCollectionRequestFlags)0)));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool TrySpawnGarbageTruck(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, GarbageFacilityData prefabGarbageFacilityData, ref Game.Buildings.GarbageFacility garbageFacility, ref int availableVehicles, ref int availableSpace, ref StackList<Entity> parkedVehicles)
		{
			if (availableVehicles <= 0 || availableSpace <= 0)
			{
				return false;
			}
			if (!m_GarbageCollectionRequestData.TryGetComponent(request, out var componentData))
			{
				return false;
			}
			Entity target = componentData.m_Target;
			if (!m_EntityLookup.Exists(target))
			{
				return false;
			}
			int2 garbageCapacity = new int2(1, availableSpace);
			Entity entity2 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData2) && componentData2.m_Origin != entity)
			{
				if (m_PrefabRefData.TryGetComponent(componentData2.m_Origin, out var componentData3) && m_PrefabGarbageTruckData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
				{
					garbageCapacity = componentData4.m_GarbageCapacity;
				}
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData2.m_Origin))
				{
					return false;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData2.m_Origin];
				entity2 = componentData2.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity2, in m_ParkedToMovingRemoveTypes);
				Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
				m_CommandBuffer.AddComponent(jobIndex, entity2, in m_ParkedToMovingCarAddTypes);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new CarCurrentLane(parkedCar, flags));
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity2 == Entity.Null)
			{
				entity2 = m_GarbageTruckSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, Entity.Null, ref garbageCapacity, parked: false);
				if (entity2 == Entity.Null)
				{
					return false;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
			}
			availableVehicles--;
			availableSpace -= garbageCapacity.y;
			GarbageTruckFlags garbageTruckFlags = (GarbageTruckFlags)0u;
			if (prefabGarbageFacilityData.m_IndustrialWasteOnly)
			{
				garbageTruckFlags |= GarbageTruckFlags.IndustrialWasteOnly;
			}
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Game.Vehicles.GarbageTruck(garbageTruckFlags, 1));
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(target));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity2).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity2, componentData2);
			}
			if (m_ServiceRequestData.HasComponent(garbageFacility.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(garbageFacility.m_TargetRequest, Entity.Null, completed: true));
			}
			return true;
		}

		private bool TrySpawnDeliveryTruck(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, ref int availableDeliveryTrucks, ref int availableSpace, ref int availableGarbage, ref int garbageAmount)
		{
			if (availableDeliveryTrucks <= 0)
			{
				return false;
			}
			GarbageTransferRequest garbageTransferRequest = m_GarbageTransferRequestData[request];
			PathInformation component = m_PathInformationData[request];
			if (!m_PrefabRefData.HasComponent(component.m_Destination))
			{
				return false;
			}
			DeliveryTruckFlags deliveryTruckFlags = (DeliveryTruckFlags)0u;
			Resource resource = Resource.Garbage;
			Resource returnResource = Resource.NoResource;
			int amount = garbageTransferRequest.m_Amount;
			int returnAmount = 0;
			if ((garbageTransferRequest.m_Flags & GarbageTransferRequestFlags.RequireTransport) != 0)
			{
				if ((garbageTransferRequest.m_Flags & GarbageTransferRequestFlags.Deliver) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
				}
				if ((garbageTransferRequest.m_Flags & GarbageTransferRequestFlags.Receive) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Buying;
				}
			}
			else
			{
				if ((garbageTransferRequest.m_Flags & GarbageTransferRequestFlags.Deliver) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Buying;
				}
				if ((garbageTransferRequest.m_Flags & GarbageTransferRequestFlags.Receive) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
				}
			}
			if ((deliveryTruckFlags & DeliveryTruckFlags.Loaded) != 0)
			{
				amount = math.min(amount, availableGarbage);
				amount = math.min(amount, garbageAmount);
				if (amount <= 0)
				{
					return false;
				}
			}
			else
			{
				returnResource = resource;
				returnAmount = amount;
				resource = Resource.NoResource;
				amount = 0;
				returnAmount = math.min(returnAmount, amount + availableSpace);
				if (returnAmount <= 0)
				{
					return false;
				}
				deliveryTruckFlags = (DeliveryTruckFlags)((uint)deliveryTruckFlags & 0xFFFFFFEFu);
				deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
			}
			Entity entity2 = m_DeliveryTruckSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, ref m_PrefabDeliveryTruckData, ref m_PrefabObjectData, resource, returnResource, ref amount, ref returnAmount, m_TransformData[entity], entity, deliveryTruckFlags);
			if (entity2 != Entity.Null)
			{
				availableDeliveryTrucks--;
				availableSpace += amount - returnAmount;
				availableGarbage -= amount;
				garbageAmount -= amount;
				m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(component.m_Destination));
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: true));
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> sourceElements = m_PathElements[request];
					if (sourceElements.Length != 0)
					{
						DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
						PathUtils.CopyPath(sourceElements, default(PathOwner), 0, targetElements);
						m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
						m_CommandBuffer.SetComponent(jobIndex, entity2, component);
					}
				}
				return true;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct GarbageFacilityActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckData;

		public NativeQueue<GarbageFacilityAction> m_ActionQueue;

		public void Execute()
		{
			GarbageFacilityAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_GarbageTruckData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= GarbageTruckFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~GarbageTruckFlags.Disabled;
					}
					m_GarbageTruckData[item.m_Entity] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle;

		public BufferTypeHandle<GuestVehicle> __Game_Vehicles_GuestVehicle_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> __Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageTransferRequest> __Game_Simulation_GarbageTransferRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.GarbageTruck> __Game_Vehicles_GarbageTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ReturnLoad> __Game_Vehicles_ReturnLoad_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageTruckData> __Game_Prefabs_GarbageTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> __Game_Prefabs_ResourceProductionData_RO_BufferLookup;

		public ComponentLookup<Storage> __Game_Areas_Storage_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.GarbageTruck> __Game_Vehicles_GarbageTruck_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.GarbageFacility>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
			__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>();
			__Game_Vehicles_GuestVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<GuestVehicle>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup = state.GetComponentLookup<GarbageCollectionRequest>(isReadOnly: true);
			__Game_Simulation_GarbageTransferRequest_RO_ComponentLookup = state.GetComponentLookup<GarbageTransferRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Vehicles_GarbageTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.GarbageTruck>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_ReturnLoad_RO_ComponentLookup = state.GetComponentLookup<ReturnLoad>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
			__Game_Prefabs_GarbageTruckData_RO_ComponentLookup = state.GetComponentLookup<GarbageTruckData>(isReadOnly: true);
			__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_ResourceProductionData_RO_BufferLookup = state.GetBufferLookup<ResourceProductionData>(isReadOnly: true);
			__Game_Areas_Storage_RW_ComponentLookup = state.GetComponentLookup<Storage>();
			__Game_Vehicles_GarbageTruck_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.GarbageTruck>();
		}
	}

	private const int kUpdatesPerDay = 1024;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private IconCommandSystem m_IconCommandSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private GarbageTruckSelectData m_GarbageTruckSelectData;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_GarbageTruckPrefabQuery;

	private EntityQuery m_GarbageSettingsQuery;

	private EntityArchetype m_GarbageTransferRequestArchetype;

	private EntityArchetype m_GarbageCollectionRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 80;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_GarbageTruckSelectData = new GarbageTruckSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_GarbageSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_GarbageTruckPrefabQuery = GetEntityQuery(GarbageTruckSelectData.GetEntityQueryDesc());
		m_GarbageTransferRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GarbageTransferRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_GarbageCollectionRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GarbageCollectionRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_ParkedToMovingRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>());
		m_ParkedToMovingCarAddTypes = new ComponentTypeSet(new ComponentType[14]
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
		RequireForUpdate(m_BuildingQuery);
		RequireForUpdate(m_GarbageSettingsQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_GarbageTruckSelectData.PreUpdate(this, m_CityConfigurationSystem, m_GarbageTruckPrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<GarbageFacilityAction> actionQueue = new NativeQueue<GarbageFacilityAction>(Allocator.TempJob);
		GarbageFacilityTickJob jobData = new GarbageFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_GarbageFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageFacility_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_GuestVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_GarbageCollectionRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageTransferRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageTransferRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_GarbageTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ReturnLoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ReturnLoad_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStorageAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceProductionDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ResourceProductionData_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaStorageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Storage_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_GarbageParameters = m_GarbageSettingsQuery.GetSingleton<GarbageParameterData>(),
			m_GarbageTruckSelectData = m_GarbageTruckSelectData,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_GarbageTransferRequestArchetype = m_GarbageTransferRequestArchetype,
			m_GarbageCollectionRequestArchetype = m_GarbageCollectionRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		GarbageFacilityActionJob jobData2 = new GarbageFacilityActionJob
		{
			m_GarbageTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_GarbageTruck_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_GarbageTruckSelectData.PostUpdate(jobHandle2);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle3;
	}

	private static float CalculateProcessingRate(float maxProcessingRate, float efficiency, int garbageAmount, int garbageCapacity)
	{
		float num = CalculateGarbageAmountFactor(garbageAmount, garbageCapacity);
		return efficiency * num * maxProcessingRate;
	}

	private static float CalculateGarbageAmountFactor(int garbageAmount, int garbageCapacity)
	{
		return math.saturate(0.1f + (float)garbageAmount * 1.8f / math.max(1f, garbageCapacity));
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
	public GarbageFacilityAISystem()
	{
	}
}
