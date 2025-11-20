#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
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
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
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
public class ResourceBuyerSystem : GameSystemBase
{
	[Flags]
	private enum SaleFlags : byte
	{
		None = 0,
		CommercialSeller = 1,
		ImportFromOC = 2,
		Virtual = 4
	}

	private struct SalesEvent
	{
		public SaleFlags m_Flags;

		public Entity m_Buyer;

		public Entity m_Seller;

		public Resource m_Resource;

		public int m_Amount;

		public float m_Distance;
	}

	[BurstCompile]
	private struct BuyJob : IJob
	{
		public NativeQueue<SalesEvent> m_SalesQueue;

		public BufferLookup<Game.Economy.Resources> m_Resources;

		public ComponentLookup<ServiceAvailable> m_Services;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Household> m_Households;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<BuyingCompany> m_BuyingCompanies;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanies;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

		public ComponentLookup<CompanyStatisticData> m_CompanyStatistics;

		public BufferLookup<TradeCost> m_TradeCosts;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public PersonalCarSelectData m_PersonalCarSelectData;

		[ReadOnly]
		public ComponentLookup<Population> m_PopulationData;

		public NativeArray<int> m_CitizenConsumptionAccumulator;

		public Entity m_PopulationEntity;

		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer m_CommandBuffer;

		[ReadOnly]
		public uint m_FrameIndex;

		public void Execute()
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			_ = m_PopulationData[m_PopulationEntity];
			SalesEvent item;
			while (m_SalesQueue.TryDequeue(out item))
			{
				if (!m_Resources.HasBuffer(item.m_Buyer) || item.m_Amount == 0)
				{
					continue;
				}
				bool flag = (item.m_Flags & SaleFlags.CommercialSeller) != 0;
				float num = (flag ? EconomyUtils.GetMarketPrice(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas) : EconomyUtils.GetIndustrialPrice(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas)) * (float)item.m_Amount;
				if (m_TradeCosts.HasBuffer(item.m_Seller))
				{
					DynamicBuffer<TradeCost> costs = m_TradeCosts[item.m_Seller];
					TradeCost tradeCost = EconomyUtils.GetTradeCost(item.m_Resource, costs);
					num += (float)item.m_Amount * tradeCost.m_BuyCost;
					float weight = EconomyUtils.GetWeight(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
					Assert.IsTrue(item.m_Amount != -1);
					float num2 = (float)EconomyUtils.GetTransportCost(item.m_Distance, item.m_Resource, item.m_Amount, weight) / (1f + (float)item.m_Amount);
					TradeCost newcost = default(TradeCost);
					if (m_TradeCosts.HasBuffer(item.m_Buyer))
					{
						newcost = EconomyUtils.GetTradeCost(item.m_Resource, m_TradeCosts[item.m_Buyer]);
					}
					if (!m_OutsideConnections.HasComponent(item.m_Seller) && !flag)
					{
						tradeCost.m_SellCost = math.lerp(tradeCost.m_SellCost, num2 + newcost.m_SellCost, 0.5f);
						EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs, keepLastTime: true);
					}
					if (m_TradeCosts.HasBuffer(item.m_Buyer) && !m_OutsideConnections.HasComponent(item.m_Buyer))
					{
						if (num2 + tradeCost.m_BuyCost < newcost.m_BuyCost)
						{
							newcost.m_BuyCost = num2 + tradeCost.m_BuyCost;
						}
						else
						{
							newcost.m_BuyCost = math.lerp(newcost.m_BuyCost, num2 + tradeCost.m_BuyCost, 0.5f);
						}
						EconomyUtils.SetTradeCost(item.m_Resource, newcost, m_TradeCosts[item.m_Buyer], keepLastTime: true);
					}
				}
				if (m_Resources.HasBuffer(item.m_Seller) && EconomyUtils.GetResources(item.m_Resource, m_Resources[item.m_Seller]) <= 0)
				{
					continue;
				}
				if (flag && m_Services.HasComponent(item.m_Seller) && m_PropertyRenters.HasComponent(item.m_Seller))
				{
					Entity prefab = m_Prefabs[item.m_Seller].m_Prefab;
					ServiceAvailable value = m_Services[item.m_Seller];
					ServiceCompanyData serviceCompanyData = m_ServiceCompanies[prefab];
					num *= EconomyUtils.GetServicePriceMultiplier(value.m_ServiceAvailable, serviceCompanyData.m_MaxService);
					value.m_ServiceAvailable = math.max(0, Mathf.RoundToInt(value.m_ServiceAvailable - item.m_Amount));
					if (value.m_MeanPriority > 0f)
					{
						value.m_MeanPriority = math.min(1f, math.lerp(value.m_MeanPriority, (float)value.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService, 0.1f));
					}
					else
					{
						value.m_MeanPriority = math.min(1f, (float)value.m_ServiceAvailable / (float)serviceCompanyData.m_MaxService);
					}
					m_Services[item.m_Seller] = value;
				}
				if (m_Resources.HasBuffer(item.m_Seller) && !m_Storages.HasComponent(item.m_Seller))
				{
					DynamicBuffer<Game.Economy.Resources> resources = m_Resources[item.m_Seller];
					int resources2 = EconomyUtils.GetResources(item.m_Resource, resources);
					EconomyUtils.AddResources(item.m_Resource, -math.min(resources2, Mathf.RoundToInt(item.m_Amount)), resources);
				}
				EconomyUtils.AddResources(Resource.Money, -Mathf.RoundToInt(num), m_Resources[item.m_Buyer]);
				if (m_Households.HasComponent(item.m_Buyer))
				{
					Household value2 = m_Households[item.m_Buyer];
					value2.m_Resources = (int)math.clamp((long)((float)value2.m_Resources + num), -2147483648L, 2147483647L);
					value2.m_ShoppedValuePerDay += (uint)num;
					m_Households[item.m_Buyer] = value2;
					int resourceIndex = EconomyUtils.GetResourceIndex(item.m_Resource);
					m_CitizenConsumptionAccumulator[resourceIndex] += item.m_Amount;
				}
				else if (m_BuyingCompanies.HasComponent(item.m_Buyer))
				{
					BuyingCompany value3 = m_BuyingCompanies[item.m_Buyer];
					value3.m_LastTradePartner = item.m_Seller;
					m_BuyingCompanies[item.m_Buyer] = value3;
					if ((item.m_Flags & SaleFlags.Virtual) != SaleFlags.None)
					{
						EconomyUtils.AddResources(item.m_Resource, item.m_Amount, m_Resources[item.m_Buyer]);
					}
				}
				if (!m_Storages.HasComponent(item.m_Seller) && m_PropertyRenters.HasComponent(item.m_Seller))
				{
					DynamicBuffer<Game.Economy.Resources> resources3 = m_Resources[item.m_Seller];
					EconomyUtils.AddResources(Resource.Money, Mathf.RoundToInt(num), resources3);
				}
				if (m_CompanyStatistics.HasComponent(item.m_Seller))
				{
					CompanyStatisticData value4 = m_CompanyStatistics[item.m_Seller];
					value4.m_CurrentNumberOfCustomers++;
					m_CompanyStatistics[item.m_Seller] = value4;
				}
				if (m_CompanyStatistics.HasComponent(item.m_Buyer))
				{
					CompanyStatisticData value5 = m_CompanyStatistics[item.m_Buyer];
					value5.m_CurrentCostOfBuyingResources += math.abs((int)num);
					m_CompanyStatistics[item.m_Buyer] = value5;
				}
				if (item.m_Resource != Resource.Vehicles || item.m_Amount != HouseholdBehaviorSystem.kCarAmount || !m_PropertyRenters.HasComponent(item.m_Seller))
				{
					continue;
				}
				Entity property = m_PropertyRenters[item.m_Seller].m_Property;
				if (!m_TransformDatas.HasComponent(property) || !m_HouseholdCitizens.HasBuffer(item.m_Buyer))
				{
					continue;
				}
				Entity entity = item.m_Buyer;
				Game.Objects.Transform transform = m_TransformDatas[property];
				int length = m_HouseholdCitizens[entity].Length;
				int num3 = (m_HouseholdAnimals.HasBuffer(entity) ? m_HouseholdAnimals[entity].Length : 0);
				int passengerAmount;
				int num4;
				if (m_OwnedVehicles.HasBuffer(entity) && m_OwnedVehicles[entity].Length >= 1)
				{
					passengerAmount = random.NextInt(1, 1 + length);
					num4 = random.NextInt(1, 2 + num3);
				}
				else
				{
					passengerAmount = length;
					num4 = 1 + num3;
				}
				if (random.NextInt(20) == 0)
				{
					num4 += 5;
				}
				Entity entity2 = m_PersonalCarSelectData.CreateVehicle(m_CommandBuffer, ref random, passengerAmount, num4, avoidTrailers: true, noSlowVehicles: false, bicycle: false, transform, property, Entity.Null, (PersonalCarFlags)0u, stopped: true);
				if (entity2 != Entity.Null)
				{
					m_CommandBuffer.AddComponent(entity2, new Owner(entity));
					if (!m_OwnedVehicles.HasBuffer(entity))
					{
						m_CommandBuffer.AddBuffer<OwnedVehicle>(entity);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct HandleBuyersJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ResourceBuyer> m_BuyerType;

		[ReadOnly]
		public ComponentTypeHandle<ResourceBought> m_BoughtType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<TripNeeded> m_TripType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> m_CreatureDataType;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> m_ResidentDataType;

		[ReadOnly]
		public ComponentTypeHandle<AttendingMeeting> m_AttendingMeetingType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformation;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<CarKeeper> m_CarKeepers;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> m_BicycleOwners;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CoordinatedMeeting> m_CoordinatedMeetings;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> m_HaveCoordinatedMeetingDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<HumanData> m_PrefabHumanData;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public float m_TimeOfDay;

		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_PathfindTypes;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_HumanChunks;

		[ReadOnly]
		public PersonalCarSelectData m_PersonalCarSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public EconomyParameterData m_EconomyParameterData;

		public Entity m_City;

		public NativeQueue<SalesEvent>.ParallelWriter m_SalesQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ResourceBuyer> nativeArray2 = chunk.GetNativeArray(ref m_BuyerType);
			NativeArray<ResourceBought> nativeArray3 = chunk.GetNativeArray(ref m_BoughtType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripType);
			NativeArray<Citizen> nativeArray4 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<AttendingMeeting> nativeArray5 = chunk.GetNativeArray(ref m_AttendingMeetingType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			ProcessResourceBought(unfilteredChunkIndex, nativeArray3, nativeArray);
			ProcessResourceBuyer(chunk, unfilteredChunkIndex, nativeArray2, nativeArray, bufferAccessor, nativeArray4, random, nativeArray5);
		}

		private void ProcessResourceBought(int unfilteredChunkIndex, NativeArray<ResourceBought> resourceBuyingWithTargets, NativeArray<Entity> entities)
		{
			for (int i = 0; i < resourceBuyingWithTargets.Length; i++)
			{
				Entity e = entities[i];
				ResourceBought resourceBought = resourceBuyingWithTargets[i];
				if (m_PrefabRefData.HasComponent(resourceBought.m_Payer) && m_PrefabRefData.HasComponent(resourceBought.m_Seller))
				{
					SalesEvent value = new SalesEvent
					{
						m_Amount = resourceBought.m_Amount,
						m_Buyer = resourceBought.m_Payer,
						m_Seller = resourceBought.m_Seller,
						m_Resource = resourceBought.m_Resource,
						m_Flags = SaleFlags.None,
						m_Distance = resourceBought.m_Distance
					};
					m_SalesQueue.Enqueue(value);
				}
				m_CommandBuffer.RemoveComponent<ResourceBought>(unfilteredChunkIndex, e);
			}
		}

		private void ProcessResourceBuyer(ArchetypeChunk chunk, int unfilteredChunkIndex, NativeArray<ResourceBuyer> resourceBuyingRequests, NativeArray<Entity> entities, BufferAccessor<TripNeeded> tripBuffers, NativeArray<Citizen> citizens, Unity.Mathematics.Random random, NativeArray<AttendingMeeting> meetings)
		{
			for (int i = 0; i < resourceBuyingRequests.Length; i++)
			{
				ResourceBuyer resourceBuyer = resourceBuyingRequests[i];
				Entity entity = entities[i];
				DynamicBuffer<TripNeeded> dynamicBuffer = tripBuffers[i];
				bool flag = false;
				Entity entity2 = m_ResourcePrefabs[resourceBuyer.m_ResourceNeeded];
				if (m_ResourceDatas.HasComponent(entity2))
				{
					flag = EconomyUtils.GetWeight(resourceBuyer.m_ResourceNeeded, m_ResourcePrefabs, ref m_ResourceDatas) == 0f;
				}
				if (m_PathInformation.HasComponent(entity))
				{
					PathInformation pathInformation = m_PathInformation[entity];
					if ((pathInformation.m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity destination = pathInformation.m_Destination;
					if (m_Properties.HasComponent(destination) || m_OutsideConnections.HasComponent(destination))
					{
						DynamicBuffer<Game.Economy.Resources> resources = m_Resources[destination];
						int num = EconomyUtils.GetResources(resourceBuyer.m_ResourceNeeded, resources);
						if (m_StorageCompanies.HasComponent(destination))
						{
							int allBuyingResourcesTrucks = VehicleUtils.GetAllBuyingResourcesTrucks(destination, resourceBuyer.m_ResourceNeeded, ref m_DeliveryTrucks, ref m_GuestVehicles, ref m_LayoutElements);
							num -= allBuyingResourcesTrucks;
						}
						num = math.max(num, 0);
						if (num <= resourceBuyer.m_AmountNeeded / 2)
						{
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
							continue;
						}
						resourceBuyer.m_AmountNeeded = math.min(resourceBuyer.m_AmountNeeded, num);
						bool num2 = m_ServiceAvailables.HasComponent(destination);
						bool flag2 = m_StorageCompanies.HasComponent(destination);
						SaleFlags saleFlags = (num2 ? SaleFlags.CommercialSeller : SaleFlags.None);
						if (flag)
						{
							saleFlags |= SaleFlags.Virtual;
						}
						if (m_OutsideConnections.HasComponent(destination))
						{
							saleFlags |= SaleFlags.ImportFromOC;
						}
						if (m_Households.HasComponent(resourceBuyer.m_Payer) && m_Resources.HasBuffer(resourceBuyer.m_Payer))
						{
							int num3 = math.max(0, EconomyUtils.GetResources(Resource.Money, m_Resources[resourceBuyer.m_Payer]) - HouseholdBehaviorSystem.kMinimumShoppingMoney);
							float marketPrice = EconomyUtils.GetMarketPrice(resourceBuyer.m_ResourceNeeded, m_ResourcePrefabs, ref m_ResourceDatas);
							float num4 = 1.4f;
							int y = (((float)num3 > 0f) ? ((int)((float)num3 / (marketPrice * num4))) : 0);
							resourceBuyer.m_AmountNeeded = math.min(resourceBuyer.m_AmountNeeded, y);
						}
						bool flag3 = resourceBuyer.m_AmountNeeded > 0;
						if (flag3)
						{
							SalesEvent value = new SalesEvent
							{
								m_Amount = resourceBuyer.m_AmountNeeded,
								m_Buyer = resourceBuyer.m_Payer,
								m_Seller = destination,
								m_Resource = resourceBuyer.m_ResourceNeeded,
								m_Flags = saleFlags,
								m_Distance = pathInformation.m_Distance
							};
							m_SalesQueue.Enqueue(value);
						}
						m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
						m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
						int population = m_Populations[m_City].m_Population;
						bool flag4 = citizens.Length > 0 && random.NextInt(100) < 100 - Mathf.RoundToInt(100f / math.max(1f, math.sqrt(m_EconomyParameterData.m_TrafficReduction * (float)population * 0.1f)));
						if (!flag && !flag4 && flag3)
						{
							m_CommandBuffer.AddBuffer<CurrentTrading>(unfilteredChunkIndex, entity).Add(new CurrentTrading
							{
								m_TradingResource = resourceBuyer.m_ResourceNeeded,
								m_TradingResourceAmount = resourceBuyer.m_AmountNeeded,
								m_OutsideConnectionType = (m_OutsideConnections.HasComponent(destination) ? BuildingUtils.GetOutsideConnectionType(destination, ref m_PrefabRefData, ref m_OutsideConnectionDatas) : OutsideConnectionTransferType.None),
								m_TradingStartFrameIndex = m_FrameIndex
							});
							dynamicBuffer.Add(new TripNeeded
							{
								m_TargetAgent = destination,
								m_Purpose = ((!flag2) ? Purpose.Shopping : Purpose.CompanyShopping),
								m_Data = resourceBuyer.m_AmountNeeded,
								m_Resource = resourceBuyer.m_ResourceNeeded
							});
							if (!m_Targets.HasComponent(entities[i]))
							{
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Target
								{
									m_Target = destination
								});
							}
						}
						continue;
					}
					m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
					m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, in m_PathfindTypes);
					if (meetings.IsCreated)
					{
						AttendingMeeting attendingMeeting = meetings[i];
						Entity prefab = m_PrefabRefData[attendingMeeting.m_Meeting].m_Prefab;
						CoordinatedMeeting value2 = m_CoordinatedMeetings[attendingMeeting.m_Meeting];
						if (m_HaveCoordinatedMeetingDatas[prefab][value2.m_Phase].m_TravelPurpose.m_Purpose == Purpose.Shopping)
						{
							value2.m_Status = MeetingStatus.Done;
							m_CoordinatedMeetings[attendingMeeting.m_Meeting] = value2;
						}
					}
				}
				else if ((!m_HouseholdMembers.HasComponent(entity) || (!m_TouristHouseholds.HasComponent(m_HouseholdMembers[entity].m_Household) && !m_CommuterHouseholds.HasComponent(m_HouseholdMembers[entity].m_Household))) && m_CurrentBuildings.HasComponent(entity) && m_OutsideConnections.HasComponent(m_CurrentBuildings[entity].m_CurrentBuilding) && !meetings.IsCreated)
				{
					SaleFlags flags = SaleFlags.ImportFromOC;
					SalesEvent value3 = new SalesEvent
					{
						m_Amount = resourceBuyer.m_AmountNeeded,
						m_Buyer = resourceBuyer.m_Payer,
						m_Seller = m_CurrentBuildings[entity].m_CurrentBuilding,
						m_Resource = resourceBuyer.m_ResourceNeeded,
						m_Flags = flags,
						m_Distance = 0f
					};
					m_SalesQueue.Enqueue(value3);
					m_CommandBuffer.RemoveComponent<ResourceBuyer>(unfilteredChunkIndex, entity);
				}
				else
				{
					Citizen citizen = default(Citizen);
					if (citizens.Length > 0)
					{
						citizen = citizens[i];
						Entity household = m_HouseholdMembers[entity].m_Household;
						Household householdData = m_Households[household];
						DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_HouseholdCitizens[household];
						FindShopForCitizen(chunk, unfilteredChunkIndex, entity, resourceBuyer.m_ResourceNeeded, resourceBuyer.m_AmountNeeded, resourceBuyer.m_Flags, citizen, householdData, dynamicBuffer2.Length, flag);
					}
					else
					{
						FindShopForCompany(chunk, unfilteredChunkIndex, entity, resourceBuyer.m_ResourceNeeded, resourceBuyer.m_AmountNeeded, resourceBuyer.m_Flags, flag);
					}
				}
			}
		}

		private void FindShopForCitizen(ArchetypeChunk chunk, int index, Entity buyer, Resource resource, int amount, SetupTargetFlags flags, Citizen citizenData, Household householdData, int householdCitizenCount, bool virtualGood)
		{
			m_CommandBuffer.AddComponent(index, buyer, in m_PathfindTypes);
			m_CommandBuffer.SetComponent(index, buyer, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			CreatureData creatureData;
			PseudoRandomSeed randomSeed;
			Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, m_HumanChunks, m_EntityType, ref m_CreatureDataType, ref m_ResidentDataType, out creatureData, out randomSeed);
			HumanData humanData = default(HumanData);
			if (entity != Entity.Null)
			{
				humanData = m_PrefabHumanData[entity];
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = humanData.m_WalkSpeed,
				m_Weights = CitizenUtils.GetPathfindWeights(citizenData, householdData, householdCitizenCount),
				m_Methods = (PathMethod.Pedestrian | PathMethod.Taxi | RouteUtils.GetPublicTransportMethods(m_TimeOfDay)),
				m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
				m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Pedestrian,
				m_RandomCost = 30f
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.ResourceSeller,
				m_Methods = PathMethod.Pedestrian,
				m_Resource = resource,
				m_Value = amount,
				m_Flags = flags,
				m_RandomCost = 30f,
				m_ActivityMask = creatureData.m_SupportedActivities
			};
			if (virtualGood)
			{
				parameters.m_PathfindFlags |= PathfindFlags.SkipPathfind;
			}
			Entity entity2 = Entity.Null;
			if (m_HouseholdMembers.TryGetComponent(buyer, out var componentData) && m_Properties.TryGetComponent(componentData.m_Household, out var componentData2))
			{
				entity2 = componentData2.m_Property;
				parameters.m_Authorization1 = componentData2.m_Property;
			}
			if (m_Workers.HasComponent(buyer))
			{
				Worker worker = m_Workers[buyer];
				if (m_Properties.HasComponent(worker.m_Workplace))
				{
					parameters.m_Authorization2 = m_Properties[worker.m_Workplace].m_Property;
				}
				else
				{
					parameters.m_Authorization2 = worker.m_Workplace;
				}
			}
			if (m_CarKeepers.IsComponentEnabled(buyer))
			{
				Entity car = m_CarKeepers[buyer].m_Car;
				if (m_ParkedCarData.HasComponent(car))
				{
					PrefabRef prefabRef = m_PrefabRefData[car];
					ParkedCar parkedCar = m_ParkedCarData[car];
					CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
					parameters.m_MaxSpeed.x = carData.m_MaxSpeed;
					parameters.m_ParkingTarget = parkedCar.m_Lane;
					parameters.m_ParkingDelta = parkedCar.m_CurvePosition;
					parameters.m_ParkingSize = VehicleUtils.GetParkingSize(car, ref m_PrefabRefData, ref m_ObjectGeometryData);
					parameters.m_Methods |= VehicleUtils.GetPathMethods(carData) | PathMethod.Parking;
					parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
					if (m_PersonalCarData.TryGetComponent(car, out var componentData3) && (componentData3.m_State & PersonalCarFlags.HomeTarget) == 0)
					{
						parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
					}
				}
			}
			else if (m_BicycleOwners.IsComponentEnabled(buyer))
			{
				Entity bicycle = m_BicycleOwners[buyer].m_Bicycle;
				if (!m_PrefabRefData.TryGetComponent(bicycle, out var componentData4) && m_CurrentBuildings.TryGetComponent(buyer, out var componentData5) && componentData5.m_CurrentBuilding == entity2)
				{
					Unity.Mathematics.Random random = citizenData.GetPseudoRandom(CitizenPseudoRandom.BicycleModel);
					componentData4.m_Prefab = m_PersonalCarSelectData.SelectVehiclePrefab(ref random, 1, 0, avoidTrailers: true, noSlowVehicles: false, bicycle: true, out var _);
				}
				if (m_PrefabCarData.TryGetComponent(componentData4.m_Prefab, out var componentData6) && m_ObjectGeometryData.TryGetComponent(componentData4.m_Prefab, out var componentData7))
				{
					parameters.m_MaxSpeed.x = componentData6.m_MaxSpeed;
					parameters.m_ParkingSize = VehicleUtils.GetParkingSize(componentData7, out var _);
					parameters.m_Methods |= PathMethod.Bicycle | PathMethod.BicycleParking;
					parameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRulesBicycleDefaults();
					if (m_ParkedCarData.TryGetComponent(bicycle, out var componentData8))
					{
						parameters.m_ParkingTarget = componentData8.m_Lane;
						parameters.m_ParkingDelta = componentData8.m_CurvePosition;
						if (m_PersonalCarData.TryGetComponent(bicycle, out var componentData9) && (componentData9.m_State & PersonalCarFlags.HomeTarget) == 0)
						{
							parameters.m_PathfindFlags |= PathfindFlags.ParkingReset;
						}
					}
					else
					{
						origin.m_Methods |= PathMethod.Bicycle;
						origin.m_RoadTypes |= RoadTypes.Bicycle;
					}
				}
			}
			SetupQueueItem value = new SetupQueueItem(buyer, parameters, origin, destination);
			m_PathfindQueue.Enqueue(value);
		}

		private void FindShopForCompany(ArchetypeChunk chunk, int index, Entity buyer, Resource resource, int amount, SetupTargetFlags flags, bool virtualGood)
		{
			m_CommandBuffer.AddComponent(index, buyer, in m_PathfindTypes);
			m_CommandBuffer.SetComponent(index, buyer, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			float transportCost = EconomyUtils.GetTransportCost(100f, amount, m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, transportCost, 1f),
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.ResourceSeller,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car,
				m_Resource = resource,
				m_Value = amount,
				m_Flags = flags
			};
			if (virtualGood)
			{
				parameters.m_PathfindFlags |= PathfindFlags.SkipPathfind;
			}
			SetupQueueItem value = new SetupQueueItem(buyer, parameters, origin, destination);
			m_PathfindQueue.Enqueue(value);
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
		public ComponentTypeHandle<ResourceBuyer> __Game_Companies_ResourceBuyer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResourceBought> __Game_Citizens_ResourceBought_RO_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AttendingMeeting> __Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;

		public ComponentLookup<CoordinatedMeeting> __Game_Citizens_CoordinatedMeeting_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HaveCoordinatedMeetingData> __Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		public ComponentLookup<Population> __Game_City_Population_RW_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		public ComponentLookup<Household> __Game_Citizens_Household_RW_ComponentLookup;

		public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RW_ComponentLookup;

		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;

		public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBuyer>(isReadOnly: true);
			__Game_Citizens_ResourceBought_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceBought>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Citizens_Citizen_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(isReadOnly: true);
			__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(isReadOnly: true);
			__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttendingMeeting>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RO_ComponentLookup = state.GetComponentLookup<CarKeeper>(isReadOnly: true);
			__Game_Citizens_BicycleOwner_RO_ComponentLookup = state.GetComponentLookup<BicycleOwner>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_CommuterHousehold_RO_ComponentLookup = state.GetComponentLookup<CommuterHousehold>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(isReadOnly: true);
			__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup = state.GetComponentLookup<CoordinatedMeeting>();
			__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup = state.GetBufferLookup<HaveCoordinatedMeetingData>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_City_Population_RW_ComponentLookup = state.GetComponentLookup<Population>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Companies_ServiceAvailable_RW_ComponentLookup = state.GetComponentLookup<ServiceAvailable>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Citizens_Household_RW_ComponentLookup = state.GetComponentLookup<Household>();
			__Game_Companies_BuyingCompany_RW_ComponentLookup = state.GetComponentLookup<BuyingCompany>();
			__Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
			__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
		}
	}

	private const int UPDATE_INTERVAL = 16;

	private EntityQuery m_BuyerQuery;

	private EntityQuery m_CarPrefabQuery;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_ResidentPrefabQuery;

	private EntityQuery m_PopulationQuery;

	private ComponentTypeSet m_PathfindTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private ResourceSystem m_ResourceSystem;

	private SimulationSystem m_SimulationSystem;

	private TaxSystem m_TaxSystem;

	private TimeSystem m_TimeSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private PersonalCarSelectData m_PersonalCarSelectData;

	private CitySystem m_CitySystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private NativeQueue<SalesEvent> m_SalesQueue;

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
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_PersonalCarSelectData = new PersonalCarSelectData(this);
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_SalesQueue = new NativeQueue<SalesEvent>(Allocator.Persistent);
		m_BuyerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<ResourceBuyer>(),
				ComponentType.ReadWrite<TripNeeded>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<TravelPurpose>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<ResourceBought>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_CarPrefabQuery = GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_PopulationQuery = GetEntityQuery(ComponentType.ReadOnly<Population>());
		m_ResidentPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>());
		m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>(), ComponentType.ReadWrite<PathElement>());
		RequireForUpdate(m_BuyerQuery);
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_PopulationQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SalesQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_BuyerQuery.CalculateEntityCount() > 0)
		{
			m_PersonalCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_CarPrefabQuery, Allocator.TempJob, out var jobHandle);
			JobHandle outJobHandle;
			HandleBuyersJob jobData = new HandleBuyersJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BuyerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ResourceBuyer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BoughtType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_ResourceBought_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TripType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AttendingMeetingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_AttendingMeeting_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformation = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Properties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarKeepers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BicycleOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommuterHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CoordinatedMeetings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CoordinatedMeeting_RW_ComponentLookup, ref base.CheckedStateRef),
				m_HaveCoordinatedMeetingDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_HaveCoordinatedMeetingData_RO_BufferLookup, ref base.CheckedStateRef),
				m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RW_ComponentLookup, ref base.CheckedStateRef),
				m_TimeOfDay = m_TimeSystem.normalizedTime,
				m_FrameIndex = m_SimulationSystem.frameIndex,
				m_RandomSeed = RandomSeed.Next(),
				m_PathfindTypes = m_PathfindTypes,
				m_HumanChunks = m_ResidentPrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_PersonalCarSelectData = m_PersonalCarSelectData,
				m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 80, 16).AsParallelWriter(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_City = m_CitySystem.City,
				m_SalesQueue = m_SalesQueue.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_BuyerQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle, jobHandle));
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
			m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
			JobHandle deps;
			BuyJob jobData2 = new BuyJob
			{
				m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
				m_SalesQueue = m_SalesQueue,
				m_Services = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentLookup, ref base.CheckedStateRef),
				m_TransformDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Storages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentLookup, ref base.CheckedStateRef),
				m_BuyingCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_BuyingCompany_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TradeCosts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref base.CheckedStateRef),
				m_CompanyStatistics = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_RandomSeed = RandomSeed.Next(),
				m_FrameIndex = m_SimulationSystem.frameIndex,
				m_PersonalCarSelectData = m_PersonalCarSelectData,
				m_PopulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PopulationEntity = m_PopulationQuery.GetSingletonEntity(),
				m_CitizenConsumptionAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, out deps),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, deps));
			m_PersonalCarSelectData.PostUpdate(base.Dependency);
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
			m_TaxSystem.AddReader(base.Dependency);
			m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.Citizens, base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public ResourceBuyerSystem()
	{
	}
}
