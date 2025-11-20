using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Prefabs;
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
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class QuantityUpdateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateQuantityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Quantity> m_QuantityType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.WorkVehicle> m_WorkVehicleData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerData;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> m_GarbageFacilityData;

		[ReadOnly]
		public ComponentLookup<IndustrialProperty> m_IndustrialPropertyData;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeepData;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> m_PrefabWorkVehicleData;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> m_PrefabCargoTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_PrefabStorageCompanyData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_PrefabGarbageFacilityData;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_EconomyResources;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_PrefabServiceUpkeepDatas;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		[ReadOnly]
		public GarbageParameterData m_GarbageConfigurationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Quantity> nativeArray2 = chunk.GetNativeArray(ref m_QuantityType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity entity2 = ((nativeArray3.Length == 0 || !(nativeArray3[i].m_Original != Entity.Null)) ? GetOwner(entity) : GetOwner(nativeArray3[i].m_Original));
				PrefabRef prefabRef = nativeArray4[i];
				QuantityObjectData quantityObjectData = m_PrefabQuantityObjectData[prefabRef.m_Prefab];
				Quantity value = default(Quantity);
				if (quantityObjectData.m_Resources != Resource.NoResource && m_IndustrialPropertyData.HasComponent(entity2) && m_PrefabRefData.TryGetComponent(entity2, out var componentData) && m_Renters.TryGetBuffer(entity2, out var bufferData) && m_PrefabSpawnableBuildingData.TryGetComponent(componentData.m_Prefab, out var componentData2) && m_PrefabBuildingData.TryGetComponent(componentData.m_Prefab, out var componentData3))
				{
					int num = 0;
					int num2 = 0;
					bool flag = false;
					for (int j = 0; j < bufferData.Length; j++)
					{
						Entity renter = bufferData[j].m_Renter;
						if (!m_PrefabRefData.TryGetComponent(renter, out var componentData4) || !m_EconomyResources.TryGetBuffer(renter, out var bufferData2) || !m_PrefabStorageCompanyData.TryGetComponent(componentData4.m_Prefab, out var componentData5) || !m_StorageLimitData.TryGetComponent(componentData4.m_Prefab, out var componentData6))
						{
							continue;
						}
						Resource resource = quantityObjectData.m_Resources & componentData5.m_StoredResources;
						if (resource != Resource.NoResource)
						{
							num2 += componentData6.GetAdjustedLimitForWarehouse(componentData2, componentData3);
							for (int k = 0; k < bufferData2.Length; k++)
							{
								Game.Economy.Resources resources = bufferData2[k];
								num += math.select(0, resources.m_Amount, (resources.m_Resource & resource) != 0);
							}
							flag = true;
						}
					}
					if (flag)
					{
						float num3 = (float)num / (float)math.max(1, num2);
						value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num3 * 100f), 0, 255);
						quantityObjectData.m_Resources = Resource.NoResource;
					}
				}
				if (quantityObjectData.m_Resources != Resource.NoResource && m_CityServiceUpkeepData.HasComponent(entity2) && m_PrefabRefData.TryGetComponent(entity2, out var componentData7) && m_EconomyResources.TryGetBuffer(entity2, out var bufferData3))
				{
					Resource resource2 = Resource.NoResource;
					if (m_PrefabServiceUpkeepDatas.TryGetBuffer(componentData7.m_Prefab, out var bufferData4))
					{
						for (int l = 0; l < bufferData4.Length; l++)
						{
							resource2 |= bufferData4[l].m_Upkeep.m_Resource;
						}
					}
					if (m_PrefabStorageCompanyData.TryGetComponent(componentData7.m_Prefab, out var componentData8))
					{
						resource2 |= componentData8.m_StoredResources;
					}
					if ((quantityObjectData.m_Resources & Resource.Garbage) != Resource.NoResource && m_GarbageFacilityData.HasComponent(entity2))
					{
						resource2 |= Resource.Garbage;
					}
					resource2 &= quantityObjectData.m_Resources;
					if (resource2 != Resource.NoResource)
					{
						int num4 = 0;
						int num5 = 0;
						if (m_StorageLimitData.TryGetComponent(componentData7.m_Prefab, out var componentData9))
						{
							if (m_InstalledUpgrades.TryGetBuffer(entity2, out var bufferData5))
							{
								UpgradeUtils.CombineStats(ref componentData9, bufferData5, ref m_PrefabRefData, ref m_StorageLimitData);
							}
							num5 += componentData9.m_Limit;
						}
						if ((quantityObjectData.m_Resources & Resource.Garbage) != Resource.NoResource && m_PrefabGarbageFacilityData.TryGetComponent(componentData7.m_Prefab, out var componentData10))
						{
							num5 += componentData10.m_GarbageCapacity;
						}
						for (int m = 0; m < bufferData3.Length; m++)
						{
							Game.Economy.Resources resources2 = bufferData3[m];
							num4 += math.select(0, resources2.m_Amount, (resources2.m_Resource & resource2) != 0);
						}
						float num6 = (float)num4 / (float)math.max(1, num5);
						value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num6 * 100f), 0, 255);
						quantityObjectData.m_Resources = Resource.NoResource;
					}
				}
				if (quantityObjectData.m_Resources != Resource.NoResource && m_DeliveryTruckData.TryGetComponent(entity2, out var componentData11) && (componentData11.m_State & DeliveryTruckFlags.Loaded) != 0 && (componentData11.m_Resource & quantityObjectData.m_Resources) != Resource.NoResource)
				{
					PrefabRef prefabRef2 = m_PrefabRefData[entity2];
					DeliveryTruckData deliveryTruckData = m_PrefabDeliveryTruckData[prefabRef2.m_Prefab];
					float num7 = (float)componentData11.m_Amount / (float)math.max(1, deliveryTruckData.m_CargoCapacity);
					value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num7 * 100f), 0, 255);
					quantityObjectData.m_Resources = Resource.NoResource;
				}
				if ((quantityObjectData.m_Resources & Resource.LocalMail) != Resource.NoResource && m_MailProducerData.TryGetComponent(entity2, out var componentData12))
				{
					value.m_Fullness = (byte)math.select(100, 0, !componentData12.mailDelivered || componentData12.receivingMail >= m_PostConfigurationData.m_MailAccumulationTolerance);
					quantityObjectData.m_Resources = Resource.NoResource;
				}
				if ((quantityObjectData.m_Resources & Resource.Garbage) != Resource.NoResource && m_GarbageProducerData.TryGetComponent(entity2, out var componentData13))
				{
					int falseValue = math.select(0, 100, componentData13.m_Garbage >= m_GarbageConfigurationData.m_RequestGarbageLimit);
					falseValue = math.select(falseValue, 255, componentData13.m_Garbage >= m_GarbageConfigurationData.m_WarningGarbageLimit);
					value.m_Fullness = (byte)falseValue;
					quantityObjectData.m_Resources = Resource.NoResource;
				}
				if (quantityObjectData.m_Resources != Resource.NoResource && m_EconomyResources.HasBuffer(entity2))
				{
					PrefabRef prefabRef3 = m_PrefabRefData[entity2];
					Resource resource3 = quantityObjectData.m_Resources;
					int num8 = 0;
					if (m_PrefabCargoTransportVehicleData.TryGetComponent(prefabRef3.m_Prefab, out var componentData14))
					{
						resource3 &= componentData14.m_Resources;
						num8 = componentData14.m_CargoCapacity;
					}
					if (resource3 != Resource.NoResource)
					{
						DynamicBuffer<Game.Economy.Resources> dynamicBuffer = m_EconomyResources[entity2];
						int num9 = 0;
						for (int n = 0; n < dynamicBuffer.Length; n++)
						{
							Game.Economy.Resources resources3 = dynamicBuffer[n];
							num9 += math.select(0, resources3.m_Amount, (resources3.m_Resource & resource3) != 0);
						}
						float num10 = (float)num9 / math.max(1f, num8);
						value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num10 * 100f), 0, 255);
						quantityObjectData.m_Resources = Resource.NoResource;
					}
				}
				if (quantityObjectData.m_MapFeature != MapFeature.None && m_WorkVehicleData.TryGetComponent(entity2, out var componentData15) && componentData15.m_DoneAmount != 0f)
				{
					PrefabRef prefabRef4 = m_PrefabRefData[entity2];
					WorkVehicleData workVehicleData = m_PrefabWorkVehicleData[prefabRef4.m_Prefab];
					if (workVehicleData.m_MapFeature == quantityObjectData.m_MapFeature)
					{
						float num11 = componentData15.m_DoneAmount / math.max(1f, workVehicleData.m_MaxWorkAmount);
						value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num11 * 100f), 0, 255);
						quantityObjectData.m_MapFeature = MapFeature.None;
					}
				}
				if (quantityObjectData.m_Resources != Resource.NoResource && m_WorkVehicleData.TryGetComponent(entity2, out var componentData16) && componentData16.m_DoneAmount != 0f)
				{
					PrefabRef prefabRef5 = m_PrefabRefData[entity2];
					WorkVehicleData workVehicleData2 = m_PrefabWorkVehicleData[prefabRef5.m_Prefab];
					if ((workVehicleData2.m_Resources & quantityObjectData.m_Resources) != Resource.NoResource)
					{
						float num12 = componentData16.m_DoneAmount / math.max(1f, workVehicleData2.m_MaxWorkAmount);
						value.m_Fullness = (byte)math.clamp(Mathf.RoundToInt(num12 * 100f), 0, 255);
						quantityObjectData.m_Resources = Resource.NoResource;
					}
				}
				nativeArray2[i] = value;
			}
		}

		public Entity GetOwner(Entity entity)
		{
			Entity entity2 = entity;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity2, out componentData) && !m_VehicleData.HasComponent(entity2) && !m_CreatureData.HasComponent(entity2))
			{
				entity2 = componentData.m_Owner;
			}
			return entity2;
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Quantity> __Game_Objects_Quantity_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.WorkVehicle> __Game_Vehicles_WorkVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProperty> __Game_Buildings_IndustrialProperty_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> __Game_Prefabs_WorkVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Quantity_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Quantity>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_WorkVehicle_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.WorkVehicle>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Buildings_IndustrialProperty_RO_ComponentLookup = state.GetComponentLookup<IndustrialProperty>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_QuantityObjectData_RO_ComponentLookup = state.GetComponentLookup<QuantityObjectData>(isReadOnly: true);
			__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_WorkVehicleData_RO_ComponentLookup = state.GetComponentLookup<WorkVehicleData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
		}
	}

	private EntityQuery m_QuantityQuery;

	private EntityQuery m_PostConfigurationQuery;

	private EntityQuery m_GarbageConfigurationQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_QuantityQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadWrite<Quantity>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>()
			}
		});
		m_PostConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PostConfigurationData>());
		m_GarbageConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		RequireForUpdate(m_QuantityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new UpdateQuantityJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_QuantityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Quantity_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WorkVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_IndustrialProperty_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityServiceUpkeepData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageLimitData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCargoTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStorageCompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyResources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabServiceUpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_PostConfigurationData = (m_PostConfigurationQuery.IsEmptyIgnoreFilter ? default(PostConfigurationData) : m_PostConfigurationQuery.GetSingleton<PostConfigurationData>()),
			m_GarbageConfigurationData = (m_GarbageConfigurationQuery.IsEmptyIgnoreFilter ? default(GarbageParameterData) : m_GarbageConfigurationQuery.GetSingleton<GarbageParameterData>())
		}, m_QuantityQuery, base.Dependency);
		base.Dependency = dependency;
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
	public QuantityUpdateSystem()
	{
	}
}
