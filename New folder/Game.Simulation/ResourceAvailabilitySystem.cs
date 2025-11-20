using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
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
public class ResourceAvailabilitySystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct FindWorkplaceLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 4 * nativeArray2[i].m_MaxWorkers, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindAttractionLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessProviderType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<AttractivenessProvider> nativeArray2 = chunk.GetNativeArray(ref m_AttractivenessProviderType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], nativeArray2[i].m_Attractiveness, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindServiceLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceAvailable> nativeArray2 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 5f + (float)nativeArray2[i].m_ServiceAvailable / 100f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindConsumerLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public bool m_Educated;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity provider = nativeArray[i];
				DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
				int num = 0;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (!m_HouseholdCitizens.HasBuffer(dynamicBuffer[j].m_Renter))
					{
						continue;
					}
					DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = m_HouseholdCitizens[dynamicBuffer[j].m_Renter];
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						Entity citizen = dynamicBuffer2[k].m_Citizen;
						if (!m_Citizens.HasComponent(citizen))
						{
							continue;
						}
						Citizen citizen2 = m_Citizens[citizen];
						if (m_Citizens[citizen].GetAge() == CitizenAge.Adult)
						{
							int educationLevel = citizen2.GetEducationLevel();
							if ((educationLevel > 1 && m_Educated) || (educationLevel <= 1 && !m_Educated))
							{
								num++;
							}
						}
					}
				}
				if (num != 0)
				{
					AddProvider(provider, 2f * (float)num, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindConvenienceFoodStoreLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity provider = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_IndustrialProcessDatas.HasComponent(prefab) && (m_IndustrialProcessDatas[prefab].m_Output.m_Resource & Resource.ConvenienceFood) != Resource.NoResource)
				{
					AddProvider(provider, 10f, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindOutsideConnectionLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindSellerLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessData;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageDatas;

		[ReadOnly]
		public BufferLookup<TradeCost> m_TradeCosts;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public Resource m_Resource;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_ProcessData.HasComponent(prefab))
				{
					continue;
				}
				if (m_StorageCompanies.HasComponent(entity))
				{
					if ((m_Resource & m_StorageDatas[prefab].m_StoredResources) != Resource.NoResource)
					{
						DynamicBuffer<TradeCost> costs = m_TradeCosts[entity];
						TradeCost tradeCost = EconomyUtils.GetTradeCost(m_Resource, costs);
						AddProvider(entity, 100f, m_Providers, ref m_TargetSeeker, 100f * tradeCost.m_BuyCost);
					}
				}
				else if ((m_Resource & m_ProcessData[prefab].m_Output.m_Resource) != Resource.NoResource)
				{
					AddProvider(entity, 100f, m_Providers, ref m_TargetSeeker);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindTaxiLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = chunk.GetNativeArray(ref m_TransportDepotType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.TransportDepot transportDepot = nativeArray2[i];
					if ((transportDepot.m_Flags & (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter)) == (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter))
					{
						PrefabRef prefabRef = nativeArray3[i];
						if (m_PrefabTransportDepotData[prefabRef.m_Prefab].m_TransportType == TransportType.Taxi)
						{
							AddProvider(nativeArray[i], (int)transportDepot.m_AvailableVehicles, m_Providers, ref m_TargetSeeker);
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.Taxi> nativeArray4 = chunk.GetNativeArray(ref m_TaxiType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PathOwner> nativeArray6 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				Owner owner = nativeArray5[j];
				if (!m_TransportDepotData.HasComponent(owner.m_Owner) || (m_TransportDepotData[owner.m_Owner].m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
				{
					continue;
				}
				Game.Vehicles.Taxi taxi = nativeArray4[j];
				DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[j];
				Entity entity = nativeArray[j];
				PathOwner pathOwner = nativeArray6[j];
				if ((taxi.m_State & TaxiFlags.Dispatched) != 0)
				{
					AddProvider(entity, 0.1f, m_Providers, ref m_TargetSeeker);
					continue;
				}
				int num = dynamicBuffer.Length - taxi.m_ExtraPathElementCount;
				if (num <= 0 || num > dynamicBuffer.Length)
				{
					AddProvider(entity, 1f, m_Providers, ref m_TargetSeeker);
					continue;
				}
				float cost = math.max(0f, (float)(num - pathOwner.m_ElementIndex) * taxi.m_PathElementTime);
				PathElement pathElement = dynamicBuffer[num - 1];
				m_TargetSeeker.m_Buffer.Enqueue(new PathTarget(entity, pathElement.m_Target, pathElement.m_TargetDelta.y, 0f));
				m_Providers.Enqueue(new AvailabilityProvider(entity, 1f, cost));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindBusStopLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				AddProvider(nativeArray[i], 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FindTramSubwayLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<SubwayStop> m_SubWayStopData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public UnsafeQueue<AvailabilityProvider>.ParallelWriter m_Providers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (m_SubWayStopData.HasComponent(entity) && m_OwnerData.TryGetComponent(entity, out var componentData))
				{
					entity = componentData.m_Owner;
				}
				AddProvider(entity, 10f, m_Providers, ref m_TargetSeeker);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ClearAvailabilityJob : IJobChunk
	{
		[ReadOnly]
		public AvailableResource m_ResourceType;

		public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ResourceAvailability> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceAvailabilityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[(int)m_ResourceType] = default(ResourceAvailability);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ApplyAvailabilityJob : IJobParallelFor
	{
		[ReadOnly]
		public AvailableResource m_ResourceType;

		[ReadOnly]
		public NativeArray<AvailabilityElement> m_AvailabilityElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<ResourceAvailability> m_ResourceAvailability;

		public void Execute(int index)
		{
			AvailabilityElement availabilityElement = m_AvailabilityElements[index];
			if (m_ResourceAvailability.HasBuffer(availabilityElement.m_Edge))
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer = m_ResourceAvailability[availabilityElement.m_Edge];
				dynamicBuffer[(int)m_ResourceType] = new ResourceAvailability
				{
					m_Availability = availabilityElement.m_Availability
				};
			}
		}
	}

	[BurstCompile]
	private struct FindTaxiDistrictsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public NativeParallelHashSet<Entity>.ParallelWriter m_Districts;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = chunk.GetNativeArray(ref m_TransportDepotType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					if ((nativeArray2[i].m_Flags & (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter)) != (TransportDepotFlags.HasAvailableVehicles | TransportDepotFlags.HasDispatchCenter))
					{
						continue;
					}
					PrefabRef prefabRef = nativeArray3[i];
					if (m_PrefabTransportDepotData[prefabRef.m_Prefab].m_TransportType != TransportType.Taxi)
					{
						continue;
					}
					Entity entity = nativeArray[i];
					if (m_ServiceDistricts.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
					{
						for (int j = 0; j < bufferData.Length; j++)
						{
							m_Districts.Add(bufferData[j].m_District);
						}
					}
					else
					{
						m_Districts.Add(Entity.Null);
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.Taxi> nativeArray4 = chunk.GetNativeArray(ref m_TaxiType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Owner owner = nativeArray5[k];
				if (!m_TransportDepotData.HasComponent(owner.m_Owner) || (m_TransportDepotData[owner.m_Owner].m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
				{
					continue;
				}
				if (m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData2) && bufferData2.Length != 0)
				{
					for (int l = 0; l < bufferData2.Length; l++)
					{
						m_Districts.Add(bufferData2[l].m_District);
					}
				}
				else
				{
					m_Districts.Add(Entity.Null);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ApplyTaxiAvailabilityJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<AvailabilityElement> m_AvailabilityElements;

		[ReadOnly]
		public NativeParallelHashSet<Entity> m_Districts;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<ResourceAvailability> m_ResourceAvailability;

		public void Execute(int index)
		{
			AvailabilityElement availabilityElement = m_AvailabilityElements[index];
			if (!m_Districts.Contains(Entity.Null))
			{
				Owner componentData2;
				if (m_BorderDistrictData.TryGetComponent(availabilityElement.m_Edge, out var componentData))
				{
					if (!m_Districts.Contains(componentData.m_Left) && !m_Districts.Contains(componentData.m_Right))
					{
						availabilityElement.m_Availability = 0f;
					}
				}
				else if (m_OwnerData.TryGetComponent(availabilityElement.m_Edge, out componentData2))
				{
					while (true)
					{
						if (m_CurrentDistrictData.TryGetComponent(componentData2.m_Owner, out var componentData3))
						{
							if (!m_Districts.Contains(componentData3.m_District))
							{
								availabilityElement.m_Availability = 0f;
							}
							break;
						}
						if (m_OwnerData.HasComponent(componentData2.m_Owner))
						{
							componentData2 = m_OwnerData[componentData2.m_Owner];
							continue;
						}
						availabilityElement.m_Availability = 0f;
						break;
					}
				}
			}
			if (m_ResourceAvailability.HasBuffer(availabilityElement.m_Edge))
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer = m_ResourceAvailability[availabilityElement.m_Edge];
				dynamicBuffer[30] = new ResourceAvailability
				{
					m_Availability = availabilityElement.m_Availability
				};
			}
			if (!m_SubLanes.HasBuffer(availabilityElement.m_Edge))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = m_SubLanes[availabilityElement.m_Edge];
			int num = Mathf.RoundToInt(math.min(65535f, math.csum(availabilityElement.m_Availability) * 32767.5f));
			for (int i = 0; i < dynamicBuffer2.Length; i++)
			{
				Entity subLane = dynamicBuffer2[i].m_SubLane;
				if (m_ParkingLaneData.HasComponent(subLane))
				{
					Game.Net.ParkingLane value = m_ParkingLaneData[subLane];
					int num2 = math.select(value.m_TaxiAvailability * 3 + num + 3 >> 2, 0, num == 0);
					if (num2 != value.m_TaxiAvailability)
					{
						value.m_TaxiAvailability = (ushort)num2;
						value.m_Flags |= ParkingLaneFlags.TaxiAvailabilityChanged;
					}
					value.m_Flags |= ParkingLaneFlags.TaxiAvailabilityUpdated;
					m_ParkingLaneData[subLane] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct RefreshTaxiAvailabilityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> m_PathfindTransportData;

		public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

		public NativeQueue<TimeActionData>.ParallelWriter m_TimeActions;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Lane> nativeArray2 = chunk.GetNativeArray(ref m_LaneType);
			NativeArray<Game.Net.ParkingLane> nativeArray3 = chunk.GetNativeArray(ref m_ParkingLaneType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Game.Net.ParkingLane parkingLane = nativeArray3[i];
				if ((parkingLane.m_Flags & ParkingLaneFlags.TaxiAvailabilityUpdated) == 0 && parkingLane.m_TaxiAvailability != 0)
				{
					parkingLane.m_TaxiAvailability = 0;
					parkingLane.m_Flags |= ParkingLaneFlags.TaxiAvailabilityChanged;
				}
				if ((parkingLane.m_Flags & ParkingLaneFlags.TaxiAvailabilityChanged) != 0)
				{
					Lane lane = nativeArray2[i];
					TimeActionData value = new TimeActionData
					{
						m_Owner = nativeArray[i],
						m_StartNode = lane.m_StartNode,
						m_EndNode = lane.m_EndNode,
						m_Flags = (TimeActionFlags.SetSecondary | TimeActionFlags.EnableForward)
					};
					if ((parkingLane.m_Flags & ParkingLaneFlags.AdditionalStart) != 0)
					{
						value.m_SecondaryStartNode = parkingLane.m_AdditionalStartNode;
						value.m_SecondaryEndNode = lane.m_EndNode;
					}
					else
					{
						value.m_SecondaryStartNode = lane.m_StartNode;
						value.m_SecondaryEndNode = lane.m_EndNode;
					}
					if (parkingLane.m_TaxiAvailability != 0)
					{
						PrefabRef prefabRef = nativeArray4[i];
						NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
						PathfindTransportData pathfindTransportData = m_PathfindTransportData[netLaneData.m_PathfindPrefab];
						value.m_Flags |= TimeActionFlags.EnableBackward;
						value.m_Time = pathfindTransportData.m_OrderingCost.m_Value.x + pathfindTransportData.m_StartingCost.m_Value.x + PathUtils.GetTaxiAvailabilityDelay(parkingLane);
					}
					m_TimeActions.Enqueue(value);
				}
				parkingLane.m_Flags &= ~(ParkingLaneFlags.TaxiAvailabilityUpdated | ParkingLaneFlags.TaxiAvailabilityChanged);
				nativeArray3[i] = parkingLane;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RW_ComponentLookup;

		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathfindTransportData> __Game_Prefabs_PathfindTransportData_RO_ComponentLookup;

		public ComponentTypeHandle<Game.Net.ParkingLane> __Game_Net_ParkingLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

		public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<SubwayStop> __Game_Routes_SubwayStop_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_ResourceAvailability_RW_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_TransportDepot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TransportDepot>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_TransportDepot_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.TransportDepot>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
			__Game_Areas_ServiceDistrict_RO_BufferLookup = state.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RW_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>();
			__Game_Net_ResourceAvailability_RW_BufferLookup = state.GetBufferLookup<ResourceAvailability>();
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_PathfindTransportData_RO_ComponentLookup = state.GetComponentLookup<PathfindTransportData>(isReadOnly: true);
			__Game_Net_ParkingLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ParkingLane>();
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Companies_TradeCost_RO_BufferLookup = state.GetBufferLookup<TradeCost>(isReadOnly: true);
			__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>();
			__Game_Pathfind_PathOwner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Routes_SubwayStop_RO_ComponentLookup = state.GetComponentLookup<SubwayStop>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private EntityQuery m_EdgeGroup;

	private EntityQuery m_WorkplaceGroup;

	private EntityQuery m_ServiceGroup;

	private EntityQuery m_RenterGroup;

	private EntityQuery m_ConvenienceFoodStoreGroup;

	private EntityQuery m_OutsideConnectionGroup;

	private EntityQuery m_AttractionGroup;

	private EntityQuery m_ResourceSellerGroup;

	private EntityQuery m_TaxiQuery;

	private EntityQuery m_BusStopQuery;

	private EntityQuery m_TramSubwayQuery;

	private EntityQuery m_ParkingLaneQuery;

	private SimulationSystem m_SimulationSystem;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private AirwaySystem m_AirwaySystem;

	private ResourceSystem m_ResourceSystem;

	private PathfindTargetSeekerData m_TargetSeekerData;

	private Entity m_AvailabilityContainer;

	private AvailableResource m_LastQueriedResource;

	private AvailableResource m_LastWrittenResource;

	private TypeHandle __TypeHandle;

	public AvailableResource appliedResource { get; private set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_EdgeGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadWrite<ResourceAvailability>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_WorkplaceGroup = GetEntityQuery(ComponentType.ReadOnly<WorkProvider>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Game.Objects.OutsideConnection>());
		m_ServiceGroup = GetEntityQuery(ComponentType.ReadOnly<ServiceAvailable>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_RenterGroup = GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.ReadOnly<Renter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConvenienceFoodStoreGroup = GetEntityQuery(ComponentType.ReadOnly<ServiceAvailable>(), ComponentType.ReadOnly<ResourceSeller>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_OutsideConnectionGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_AttractionGroup = GetEntityQuery(ComponentType.ReadOnly<AttractivenessProvider>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ResourceSellerGroup = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<ResourceSeller>(),
				ComponentType.ReadOnly<Game.Companies.StorageCompany>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<ServiceAvailable>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TaxiQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),
				ComponentType.ReadOnly<Game.Vehicles.Taxi>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_BusStopQuery = GetEntityQuery(ComponentType.ReadOnly<BusStop>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_TramSubwayQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Routes.TransportStop>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<TramStop>(),
				ComponentType.ReadOnly<SubwayStop>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ParkingLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.ParkingLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_TargetSeekerData = new PathfindTargetSeekerData(this);
		m_AvailabilityContainer = base.EntityManager.CreateEntity(ComponentType.ReadWrite<AvailabilityElement>());
	}

	private AvailabilityParameters GetAvailabilityParameters(AvailableResource resource, ResourcePrefabs prefabs, ComponentLookup<ResourceData> datas)
	{
		switch (resource)
		{
		case AvailableResource.GrainSupply:
		case AvailableResource.VegetableSupply:
		case AvailableResource.WoodSupply:
		case AvailableResource.TextilesSupply:
		case AvailableResource.ConvenienceFoodSupply:
		case AvailableResource.PaperSupply:
		case AvailableResource.VehiclesSupply:
		case AvailableResource.OilSupply:
		case AvailableResource.PetrochemicalsSupply:
		case AvailableResource.OreSupply:
		case AvailableResource.MetalsSupply:
		case AvailableResource.ElectronicsSupply:
		case AvailableResource.PlasticsSupply:
		case AvailableResource.CoalSupply:
		case AvailableResource.StoneSupply:
		case AvailableResource.LivestockSupply:
		case AvailableResource.CottonSupply:
		case AvailableResource.SteelSupply:
		case AvailableResource.MineralSupply:
		case AvailableResource.ChemicalSupply:
		case AvailableResource.MachinerySupply:
		case AvailableResource.BeveragesSupply:
		case AvailableResource.TimberSupply:
		{
			Resource resource2 = EconomyUtils.GetResource(resource);
			float costFactor = 0.1f;
			if (resource2 != Resource.NoResource)
			{
				costFactor = 0.1f * EconomyUtils.GetTransportCost(1f, 0, EconomyUtils.GetWeight(resource2, prefabs, ref datas), StorageTransferFlags.Car);
			}
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = costFactor,
				m_ResultFactor = 0.01f
			};
		}
		case AvailableResource.Workplaces:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 0.08f
			};
		case AvailableResource.UneducatedCitizens:
		case AvailableResource.EducatedCitizens:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.5f,
				m_ResultFactor = 1f
			};
		case AvailableResource.Services:
		case AvailableResource.ConvenienceFoodStore:
		case AvailableResource.Attractiveness:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		case AvailableResource.OutsideConnection:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 3f
			};
		case AvailableResource.Taxi:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.1f,
				m_CostFactor = 0.05f,
				m_ResultFactor = 1f
			};
		case AvailableResource.Bus:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		case AvailableResource.TramSubway:
			return new AvailabilityParameters
			{
				m_DensityWeight = 0.05f,
				m_CostFactor = 0.1f,
				m_ResultFactor = 1f
			};
		default:
			return new AvailabilityParameters
			{
				m_DensityWeight = 1f,
				m_CostFactor = 1f,
				m_ResultFactor = 1f
			};
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((int)m_LastWrittenResource);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_LastQueriedResource = AvailableResource.Count;
		m_LastWrittenResource = (AvailableResource)math.clamp(value, 0, 33);
		appliedResource = AvailableResource.Count;
	}

	public void SetDefaults(Context context)
	{
		m_LastQueriedResource = AvailableResource.Count;
		m_LastWrittenResource = AvailableResource.FishSupply;
		appliedResource = AvailableResource.Count;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LastQueriedResource != AvailableResource.Count)
		{
			m_LastWrittenResource = m_LastQueriedResource;
			appliedResource = m_LastWrittenResource;
		}
		else
		{
			m_LastQueriedResource = m_LastWrittenResource;
			m_LastWrittenResource = AvailableResource.Count;
		}
		if (++m_LastQueriedResource == AvailableResource.Count)
		{
			m_LastQueriedResource = AvailableResource.Workplaces;
		}
		AvailabilityAction action = new AvailabilityAction(Allocator.Persistent, GetAvailabilityParameters(m_LastQueriedResource, m_ResourceSystem.GetPrefabs(), GetComponentLookup<ResourceData>(isReadOnly: true)));
		JobHandle jobHandle = FindLocations(m_LastQueriedResource, action.data.m_Sources, action.data.m_Providers, base.Dependency);
		if (m_LastWrittenResource != AvailableResource.Count)
		{
			JobHandle job = ApplyAvailability(m_LastWrittenResource, base.Dependency, jobHandle);
			base.Dependency = JobHandle.CombineDependencies(job, jobHandle);
		}
		else
		{
			base.Dependency = jobHandle;
		}
		m_PathfindQueueSystem.Enqueue(action, m_AvailabilityContainer, jobHandle, m_SimulationSystem.frameIndex + 64, this);
	}

	private JobHandle ApplyAvailability(AvailableResource resource, JobHandle inputDeps, JobHandle pathDeps)
	{
		NativeArray<AvailabilityElement> availabilityElements = base.EntityManager.GetBuffer<AvailabilityElement>(m_AvailabilityContainer, isReadOnly: true).AsNativeArray();
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new ClearAvailabilityJob
		{
			m_ResourceType = resource,
			m_ResourceAvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferTypeHandle, ref base.CheckedStateRef)
		}, m_EdgeGroup, inputDeps);
		if (resource == AvailableResource.Taxi)
		{
			TimeAction action = new TimeAction(Allocator.Persistent);
			NativeParallelHashSet<Entity> districts = new NativeParallelHashSet<Entity>(m_TaxiQuery.CalculateEntityCount(), Allocator.TempJob);
			FindTaxiDistrictsJob jobData = new FindTaxiDistrictsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TaxiType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_ServiceDistrict_RO_BufferLookup, ref base.CheckedStateRef),
				m_Districts = districts.AsParallelWriter()
			};
			ApplyTaxiAvailabilityJob jobData2 = new ApplyTaxiAvailabilityJob
			{
				m_AvailabilityElements = availabilityElements,
				m_Districts = districts,
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceAvailability = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferLookup, ref base.CheckedStateRef)
			};
			RefreshTaxiAvailabilityJob jobData3 = new RefreshTaxiAvailabilityJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathfindTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PathfindTransportData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TimeActions = action.m_TimeData.AsParallelWriter()
			};
			JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.ScheduleParallel(jobData, m_TaxiQuery, inputDeps), pathDeps), jobData: jobData2, arrayLength: availabilityElements.Length, innerloopBatchCount: 4);
			JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(jobData3, m_ParkingLaneQuery, jobHandle2);
			districts.Dispose(jobHandle2);
			m_PathfindQueueSystem.Enqueue(action, jobHandle3);
			return jobHandle3;
		}
		return IJobParallelForExtensions.Schedule(new ApplyAvailabilityJob
		{
			m_ResourceType = resource,
			m_AvailabilityElements = availabilityElements,
			m_ResourceAvailability = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferLookup, ref base.CheckedStateRef)
		}, availabilityElements.Length, 16, jobHandle);
	}

	private JobHandle FindLocations(AvailableResource resource, UnsafeQueue<PathTarget> pathTargets, UnsafeQueue<AvailabilityProvider> providers, JobHandle inputDeps)
	{
		m_TargetSeekerData.Update(this, m_AirwaySystem.GetAirwayData());
		PathfindParameters pathfindParameters = new PathfindParameters
		{
			m_MaxSpeed = 111.111115f,
			m_WalkSpeed = 5.555556f,
			m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
			m_Methods = PathMethod.Road,
			m_PathfindFlags = (PathfindFlags.Stable | PathfindFlags.IgnoreFlow | PathfindFlags.Simplified),
			m_IgnoredRules = (RuleFlags.HasBlockage | RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
		};
		SetupQueueTarget setupQueueTarget = new SetupQueueTarget
		{
			m_Methods = PathMethod.Road,
			m_RoadTypes = RoadTypes.Car
		};
		switch (resource)
		{
		case AvailableResource.Workplaces:
			return JobChunkExtensions.ScheduleParallel(new FindWorkplaceLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_WorkplaceGroup, inputDeps);
		case AvailableResource.Services:
			return JobChunkExtensions.ScheduleParallel(new FindServiceLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_ServiceGroup, inputDeps);
		case AvailableResource.UneducatedCitizens:
			return JobChunkExtensions.ScheduleParallel(new FindConsumerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Educated = false
			}, m_RenterGroup, inputDeps);
		case AvailableResource.EducatedCitizens:
			return JobChunkExtensions.ScheduleParallel(new FindConsumerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Educated = true
			}, m_RenterGroup, inputDeps);
		case AvailableResource.ConvenienceFoodStore:
			return JobChunkExtensions.ScheduleParallel(new FindConvenienceFoodStoreLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_ConvenienceFoodStoreGroup, inputDeps);
		case AvailableResource.OutsideConnection:
			return JobChunkExtensions.ScheduleParallel(new FindOutsideConnectionLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_OutsideConnectionGroup, inputDeps);
		case AvailableResource.GrainSupply:
		case AvailableResource.VegetableSupply:
		case AvailableResource.WoodSupply:
		case AvailableResource.TextilesSupply:
		case AvailableResource.ConvenienceFoodSupply:
		case AvailableResource.PaperSupply:
		case AvailableResource.VehiclesSupply:
		case AvailableResource.OilSupply:
		case AvailableResource.PetrochemicalsSupply:
		case AvailableResource.OreSupply:
		case AvailableResource.MetalsSupply:
		case AvailableResource.ElectronicsSupply:
		case AvailableResource.PlasticsSupply:
		case AvailableResource.CoalSupply:
		case AvailableResource.StoneSupply:
		case AvailableResource.LivestockSupply:
		case AvailableResource.CottonSupply:
		case AvailableResource.SteelSupply:
		case AvailableResource.MineralSupply:
		case AvailableResource.ChemicalSupply:
		case AvailableResource.MachinerySupply:
		case AvailableResource.BeveragesSupply:
		case AvailableResource.TimberSupply:
		case AvailableResource.FishSupply:
		{
			Resource resource2;
			switch (resource)
			{
			case AvailableResource.GrainSupply:
				resource2 = Resource.Grain;
				break;
			case AvailableResource.TextilesSupply:
				resource2 = Resource.Textiles;
				break;
			case AvailableResource.VegetableSupply:
				resource2 = Resource.Vegetables;
				break;
			case AvailableResource.WoodSupply:
				resource2 = Resource.Wood;
				break;
			case AvailableResource.ConvenienceFoodSupply:
				resource2 = Resource.ConvenienceFood;
				break;
			case AvailableResource.PaperSupply:
				resource2 = Resource.Paper;
				break;
			case AvailableResource.VehiclesSupply:
				resource2 = Resource.Vehicles;
				break;
			case AvailableResource.MetalsSupply:
				resource2 = Resource.Metals;
				break;
			case AvailableResource.OilSupply:
				resource2 = Resource.Oil;
				break;
			case AvailableResource.OreSupply:
				resource2 = Resource.Ore;
				break;
			case AvailableResource.PetrochemicalsSupply:
				resource2 = Resource.Petrochemicals;
				break;
			case AvailableResource.ElectronicsSupply:
				resource2 = Resource.Electronics;
				break;
			case AvailableResource.PlasticsSupply:
				resource2 = Resource.Plastics;
				break;
			case AvailableResource.CoalSupply:
				resource2 = Resource.Coal;
				break;
			case AvailableResource.StoneSupply:
				resource2 = Resource.Stone;
				break;
			case AvailableResource.LivestockSupply:
				resource2 = Resource.Livestock;
				break;
			case AvailableResource.CottonSupply:
				resource2 = Resource.Cotton;
				break;
			case AvailableResource.SteelSupply:
				resource2 = Resource.Steel;
				break;
			case AvailableResource.MineralSupply:
				resource2 = Resource.Minerals;
				break;
			case AvailableResource.ChemicalSupply:
				resource2 = Resource.Chemicals;
				break;
			case AvailableResource.TimberSupply:
				resource2 = Resource.Timber;
				break;
			case AvailableResource.MachinerySupply:
				resource2 = Resource.Machinery;
				break;
			case AvailableResource.BeveragesSupply:
				resource2 = Resource.Beverages;
				break;
			case AvailableResource.FishSupply:
				resource2 = Resource.Fish;
				break;
			default:
				return inputDeps;
			}
			return JobChunkExtensions.ScheduleParallel(new FindSellerLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter(),
				m_Resource = resource2,
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ProcessData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TradeCosts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup, ref base.CheckedStateRef)
			}, m_ResourceSellerGroup, inputDeps);
		}
		case AvailableResource.Attractiveness:
			return JobChunkExtensions.ScheduleParallel(new FindAttractionLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_AttractivenessProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_AttractionGroup, inputDeps);
		case AvailableResource.Taxi:
			return JobChunkExtensions.ScheduleParallel(new FindTaxiLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TaxiType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TransportDepot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_TaxiQuery, inputDeps);
		case AvailableResource.Bus:
			return JobChunkExtensions.ScheduleParallel(new FindBusStopLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_BusStopQuery, inputDeps);
		case AvailableResource.TramSubway:
			return JobChunkExtensions.ScheduleParallel(new FindTramSubwayLocationsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_SubWayStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_SubwayStop_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, pathTargets.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true),
				m_Providers = providers.AsParallelWriter()
			}, m_TramSubwayQuery, inputDeps);
		default:
			return inputDeps;
		}
	}

	private static void AddProvider(Entity provider, float capacity, UnsafeQueue<AvailabilityProvider>.ParallelWriter providers, ref PathfindTargetSeeker<PathfindTargetBuffer> targetSeeker, float cost)
	{
		if (targetSeeker.FindTargets(provider, cost) != 0)
		{
			providers.Enqueue(new AvailabilityProvider(provider, capacity, cost));
		}
	}

	private static void AddProvider(Entity provider, float capacity, UnsafeQueue<AvailabilityProvider>.ParallelWriter providers, ref PathfindTargetSeeker<PathfindTargetBuffer> targetSeeker)
	{
		if (targetSeeker.FindTargets(provider, 0f) != 0)
		{
			providers.Enqueue(new AvailabilityProvider(provider, capacity, 0f));
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
	public ResourceAvailabilitySystem()
	{
	}
}
