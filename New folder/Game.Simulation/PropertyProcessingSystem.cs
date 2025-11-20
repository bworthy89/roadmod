#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Zones;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PropertyProcessingSystem : GameSystemBase
{
	[BurstCompile]
	public struct PutPropertyOnMarketJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<Entity> m_CommercialCompanyPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_IndustrialCompanyPrefabs;

		[ReadOnly]
		public ComponentLookup<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanieDatas;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertyOnMarkets;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (m_Abandoneds.HasComponent(nativeArray[i]))
				{
					m_CommandBuffer.RemoveComponent<PropertyToBeOnMarket>(unfilteredChunkIndex, nativeArray[i]);
					continue;
				}
				Entity prefab = nativeArray2[i].m_Prefab;
				BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
				BuildingData buildingData = m_BuildingDatas[prefab];
				Entity roadEdge = nativeArray3[i].m_RoadEdge;
				float landValueBase = 0f;
				if (m_LandValues.HasComponent(roadEdge))
				{
					landValueBase = m_LandValues[roadEdge].m_LandValue;
				}
				Game.Zones.AreaType areaType = Game.Zones.AreaType.None;
				int buildingLevel = PropertyUtils.GetBuildingLevel(prefab, m_SpawnableBuildingDatas);
				if (m_SpawnableBuildingDatas.HasComponent(prefab))
				{
					SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingDatas[prefab];
					areaType = m_ZoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType;
				}
				int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
				int rentPricePerRenter = PropertyUtils.GetRentPricePerRenter(buildingPropertyData, buildingLevel, lotSize, landValueBase, areaType, ref m_EconomyParameterData);
				if (chunk.Has<Signature>())
				{
					if (buildingPropertyData.m_AllowedSold != Resource.NoResource)
					{
						for (int j = 0; j < m_CommercialCompanyPrefabs.Length; j++)
						{
							Resource resource = m_IndustrialProcessDatas[m_CommercialCompanyPrefabs[j]].m_Output.m_Resource;
							if (buildingPropertyData.m_AllowedSold == resource)
							{
								Entity entity = m_CommercialCompanyPrefabs[j];
								ArchetypeData archetypeData = m_Archetypes[entity];
								Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, archetypeData.m_Archetype);
								m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new PrefabRef
								{
									m_Prefab = entity
								});
								break;
							}
						}
					}
					else if (buildingPropertyData.m_AllowedManufactured != Resource.NoResource)
					{
						for (int k = 0; k < m_IndustrialCompanyPrefabs.Length; k++)
						{
							IndustrialProcessData industrialProcessData = m_IndustrialProcessDatas[m_IndustrialCompanyPrefabs[k]];
							Resource resource2 = industrialProcessData.m_Output.m_Resource;
							Resource resource3 = industrialProcessData.m_Input1.m_Resource | industrialProcessData.m_Input2.m_Resource;
							if (buildingPropertyData.m_AllowedManufactured == resource2 && (buildingPropertyData.m_AllowedInput == EconomyUtils.GetAllResources() || buildingPropertyData.m_AllowedInput == resource3))
							{
								Entity entity2 = m_IndustrialCompanyPrefabs[k];
								ArchetypeData archetypeData2 = m_Archetypes[entity2];
								Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, archetypeData2.m_Archetype);
								m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new PrefabRef
								{
									m_Prefab = entity2
								});
								break;
							}
						}
					}
				}
				if (!m_PropertyOnMarkets.HasComponent(nativeArray[i]))
				{
					m_CommandBuffer.RemoveComponent<PropertyToBeOnMarket>(unfilteredChunkIndex, nativeArray[i]);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new PropertyOnMarket
					{
						m_AskingRent = rentPricePerRenter
					});
				}
				else
				{
					m_CommandBuffer.RemoveComponent<PropertyOnMarket>(unfilteredChunkIndex, nativeArray[i]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	public struct PropertyRentJob : IJob
	{
		[ReadOnly]
		public EntityArchetype m_RentEventArchetype;

		[ReadOnly]
		public EntityArchetype m_MovedEventArchetype;

		public ComponentLookup<WorkProvider> m_WorkProviders;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<ParkData> m_ParkDatas;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_Companies;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> m_Commercials;

		[ReadOnly]
		public ComponentLookup<IndustrialCompany> m_Industrials;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreaBufs;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_Lots;

		[ReadOnly]
		public ComponentLookup<Geometry> m_Geometries;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attacheds;

		[ReadOnly]
		public ComponentLookup<ExtractorCompanyData> m_ExtractorCompanyDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_Resources;

		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		public BufferLookup<Renter> m_Renters;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<RentAction> m_RentActionQueue;

		public NativeList<Entity> m_ReservedProperties;

		public NativeQueue<TriggerAction> m_TriggerQueue;

		public NativeQueue<StatisticsEvent> m_StatisticsQueue;

		public bool m_DebugDisableHomeless;

		public void Execute()
		{
			RentAction item;
			while (m_RentActionQueue.TryDequeue(out item))
			{
				Entity value = item.m_Property;
				if (!m_Renters.HasBuffer(value) || !m_PrefabRefs.HasComponent(item.m_Renter))
				{
					continue;
				}
				if (!m_ReservedProperties.Contains(value))
				{
					DynamicBuffer<Renter> dynamicBuffer = m_Renters[value];
					Entity prefab = m_PrefabRefs[value].m_Prefab;
					int num = 0;
					bool flag = false;
					bool flag2 = false;
					bool flag3 = false;
					Game.Zones.AreaType areaType = BuildingUtils.GetAreaType(prefab, ref m_SpawnableBuildingDatas, ref m_ZoneDatas);
					if (m_BuildingPropertyDatas.HasComponent(prefab))
					{
						BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
						bool flag4 = PropertyUtils.IsMixedBuilding(prefab, ref m_BuildingPropertyDatas);
						if (areaType == Game.Zones.AreaType.Residential)
						{
							num = buildingPropertyData.CountProperties(Game.Zones.AreaType.Residential);
							if (flag4)
							{
								flag = true;
							}
						}
						else
						{
							flag = true;
						}
						for (int i = 0; i < dynamicBuffer.Length; i++)
						{
							Entity renter = dynamicBuffer[i].m_Renter;
							if (m_Households.HasComponent(renter))
							{
								num--;
							}
							else if (m_Companies.HasComponent(renter))
							{
								flag2 = true;
							}
						}
					}
					else if (m_BuildingDatas.HasComponent(prefab) && BuildingUtils.IsHomelessShelterBuilding(value, ref m_Parks, ref m_Abandoneds))
					{
						num = BuildingUtils.GetShelterHomelessCapacity(prefab, ref m_BuildingDatas, ref m_BuildingPropertyDatas) - m_Renters[value].Length;
						flag3 = true;
					}
					bool flag5 = m_Companies.HasComponent(item.m_Renter);
					if ((!flag5 && num > 0) || (flag5 && flag && !flag2))
					{
						Entity propertyFromRenter = BuildingUtils.GetPropertyFromRenter(item.m_Renter, ref m_HomelessHouseholds, ref m_PropertyRenters);
						if (propertyFromRenter != Entity.Null && propertyFromRenter != value)
						{
							if (m_WorkProviders.HasComponent(item.m_Renter) && m_Employees.HasBuffer(item.m_Renter) && m_WorkProviders[item.m_Renter].m_MaxWorkers < m_Employees[item.m_Renter].Length)
							{
								continue;
							}
							if (m_Renters.HasBuffer(propertyFromRenter))
							{
								DynamicBuffer<Renter> dynamicBuffer2 = m_Renters[propertyFromRenter];
								for (int j = 0; j < dynamicBuffer2.Length; j++)
								{
									if (dynamicBuffer2[j].m_Renter.Equals(item.m_Renter))
									{
										dynamicBuffer2.RemoveAt(j);
										break;
									}
								}
								Entity e = m_CommandBuffer.CreateEntity(m_RentEventArchetype);
								m_CommandBuffer.SetComponent(e, new RentersUpdated(propertyFromRenter));
							}
							if (m_PrefabRefs.HasComponent(propertyFromRenter) && !m_PropertiesOnMarket.HasComponent(propertyFromRenter))
							{
								m_CommandBuffer.AddComponent(propertyFromRenter, default(PropertyToBeOnMarket));
							}
						}
						if (!flag3)
						{
							if (value == Entity.Null)
							{
								UnityEngine.Debug.LogWarning("trying to rent null property");
							}
							int rent = 0;
							if (m_PropertiesOnMarket.HasComponent(value))
							{
								rent = m_PropertiesOnMarket[value].m_AskingRent;
							}
							m_CommandBuffer.AddComponent(item.m_Renter, new PropertyRenter
							{
								m_Property = value,
								m_Rent = rent
							});
						}
						dynamicBuffer.Add(new Renter
						{
							m_Renter = item.m_Renter
						});
						if (flag5 && m_PrefabRefs.TryGetComponent(item.m_Renter, out var componentData) && m_Companies[item.m_Renter].m_Brand != Entity.Null)
						{
							m_TriggerQueue.Enqueue(new TriggerAction
							{
								m_PrimaryTarget = item.m_Renter,
								m_SecondaryTarget = item.m_Property,
								m_TriggerPrefab = componentData.m_Prefab,
								m_TriggerType = TriggerType.BrandRented
							});
						}
						if (m_WorkProviders.HasComponent(item.m_Renter))
						{
							Entity renter2 = item.m_Renter;
							WorkProvider value2 = m_WorkProviders[renter2];
							int companyMaxFittingWorkers = CompanyUtils.GetCompanyMaxFittingWorkers(item.m_Renter, item.m_Property, ref m_PrefabRefs, ref m_ServiceCompanyDatas, ref m_BuildingDatas, ref m_BuildingPropertyDatas, ref m_SpawnableBuildingDatas, ref m_IndustrialProcessDatas, ref m_ExtractorCompanyDatas, ref m_Attacheds, ref m_SubAreaBufs, ref m_InstalledUpgrades, ref m_Lots, ref m_Geometries);
							value2.m_MaxWorkers = math.max(math.min(value2.m_MaxWorkers, companyMaxFittingWorkers), 2 * companyMaxFittingWorkers / 3);
							m_WorkProviders[renter2] = value2;
						}
						if (m_HouseholdCitizens.HasBuffer(item.m_Renter))
						{
							DynamicBuffer<HouseholdCitizen> dynamicBuffer3 = m_HouseholdCitizens[item.m_Renter];
							if (m_BuildingPropertyDatas.HasComponent(prefab) && m_HomelessHouseholds.HasComponent(item.m_Renter) && !flag3)
							{
								m_CommandBuffer.RemoveComponent<HomelessHousehold>(item.m_Renter);
							}
							else if (!m_DebugDisableHomeless && flag3)
							{
								m_CommandBuffer.AddComponent(item.m_Renter, new HomelessHousehold
								{
									m_TempHome = value
								});
								Household value3 = m_Households[item.m_Renter];
								value3.m_Resources = 0;
								m_Households[item.m_Renter] = value3;
							}
							if (m_BuildingPropertyDatas.HasComponent(prefab) && m_PropertyRenters.HasComponent(item.m_Renter))
							{
								foreach (HouseholdCitizen item2 in dynamicBuffer3)
								{
									m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.CitizenMovedHouse, Entity.Null, item2.m_Citizen, m_PropertyRenters[item.m_Renter].m_Property));
								}
							}
						}
						if (m_BuildingPropertyDatas.HasComponent(prefab) && dynamicBuffer.Length >= m_BuildingPropertyDatas[prefab].CountProperties())
						{
							m_ReservedProperties.Add(in value);
							m_CommandBuffer.RemoveComponent<PropertyOnMarket>(value);
						}
						else if (flag3 && num <= 1)
						{
							m_ReservedProperties.Add(in value);
						}
						Entity e2 = m_CommandBuffer.CreateEntity(m_RentEventArchetype);
						m_CommandBuffer.SetComponent(e2, new RentersUpdated(value));
						if (m_MovedEventArchetype.Valid)
						{
							e2 = m_CommandBuffer.CreateEntity(m_MovedEventArchetype);
							m_CommandBuffer.SetComponent(e2, new PathTargetMoved(item.m_Renter, default(float3), default(float3)));
						}
					}
					else if (m_BuildingPropertyDatas.HasComponent(prefab) && dynamicBuffer.Length >= m_BuildingPropertyDatas[prefab].CountProperties())
					{
						m_CommandBuffer.RemoveComponent<PropertyOnMarket>(value);
					}
				}
				else
				{
					m_CommandBuffer.SetComponentEnabled<PropertySeeker>(item.m_Renter, value: true);
				}
			}
			m_ReservedProperties.Clear();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<ArchetypeData> __Game_Prefabs_ArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialCompany> __Game_Companies_IndustrialCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorCompanyData> __Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_ArchetypeData_RO_ComponentLookup = state.GetComponentLookup<ArchetypeData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
			__Game_Companies_IndustrialCompany_RO_ComponentLookup = state.GetComponentLookup<IndustrialCompany>(isReadOnly: true);
			__Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Companies_WorkProvider_RW_ComponentLookup = state.GetComponentLookup<WorkProvider>();
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup = state.GetComponentLookup<ExtractorCompanyData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		}
	}

	private EntityQuery m_CommercialCompanyPrefabQuery;

	private EntityQuery m_IndustrialCompanyPrefabQuery;

	private EntityQuery m_PropertyGroupQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private TriggerSystem m_TriggerSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ResourceSystem m_ResourceSystem;

	private EntityArchetype m_RentEventArchetype;

	private EntityArchetype m_MovedEventArchetype;

	private NativeQueue<RentAction> m_RentActionQueue;

	private NativeList<Entity> m_ReservedProperties;

	private JobHandle m_Writers;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RentActionQueue = new NativeQueue<RentAction>(Allocator.Persistent);
		m_ReservedProperties = new NativeList<Entity>(Allocator.Persistent);
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_RentEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<RentersUpdated>());
		m_MovedEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<PathTargetMoved>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_PropertyGroupQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadWrite<PropertyToBeOnMarket>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<CommercialProperty>(),
				ComponentType.ReadOnly<ResidentialProperty>(),
				ComponentType.ReadOnly<IndustrialProperty>(),
				ComponentType.ReadOnly<ExtractorProperty>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_CommercialCompanyPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<CommercialCompanyData>(), ComponentType.ReadOnly<IndustrialProcessData>());
		m_IndustrialCompanyPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<IndustrialCompanyData>(), ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.Exclude<StorageCompanyData>());
		RequireForUpdate(m_EconomyParameterQuery);
	}

	public NativeQueue<RentAction> GetRentActionQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_Writers;
		return m_RentActionQueue;
	}

	public void AddWriter(JobHandle writer)
	{
		m_Writers = JobHandle.CombineDependencies(m_Writers, writer);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_RentActionQueue.Dispose();
		m_ReservedProperties.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_PropertyGroupQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			PutPropertyOnMarketJob jobData = new PutPropertyOnMarketJob
			{
				m_CommercialCompanyPrefabs = m_CommercialCompanyPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_IndustrialCompanyPrefabs = m_IndustrialCompanyPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_Archetypes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyOnMarkets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompanieDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PropertyGroupQuery, JobHandle.CombineDependencies(outJobHandle, outJobHandle2, base.Dependency));
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		}
		JobHandle deps;
		PropertyRentJob jobData2 = new PropertyRentJob
		{
			m_RentEventArchetype = m_RentEventArchetype,
			m_MovedEventArchetype = m_MovedEventArchetype,
			m_PropertiesOnMarket = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Companies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Industrials = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Commercials = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerQueue = m_TriggerSystem.CreateActionBuffer(),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Parks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attacheds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtractorCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreaBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Geometries = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lots = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_Resources = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StatisticsQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_RentActionQueue = m_RentActionQueue,
			m_ReservedProperties = m_ReservedProperties,
			m_DebugDisableHomeless = false
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, m_Writers, deps));
		m_CityStatisticsSystem.AddWriter(base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public PropertyProcessingSystem()
	{
	}
}
