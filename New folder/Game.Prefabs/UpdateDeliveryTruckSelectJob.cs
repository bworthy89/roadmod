using Game.Economy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs;

[BurstCompile]
public struct UpdateDeliveryTruckSelectJob : IJob
{
	private struct TruckData
	{
		public Entity m_Entity;

		public DeliveryTruckData m_DeliveryTruckData;

		public CarTrailerData m_TrailerData;

		public CarTractorData m_TractorData;

		public ObjectData m_ObjectData;
	}

	[ReadOnly]
	public EntityTypeHandle m_EntityType;

	[ReadOnly]
	public ComponentTypeHandle<DeliveryTruckData> m_DeliveryTruckDataType;

	[ReadOnly]
	public ComponentTypeHandle<CarTrailerData> m_CarTrailerDataType;

	[ReadOnly]
	public ComponentTypeHandle<CarTractorData> m_CarTractorDataType;

	[ReadOnly]
	public NativeList<ArchetypeChunk> m_PrefabChunks;

	[ReadOnly]
	public VehicleSelectRequirementData m_RequirementData;

	public NativeList<DeliveryTruckSelectItem> m_DeliveryTruckItems;

	public void Execute()
	{
		m_DeliveryTruckItems.Clear();
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray2 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTrailerData> nativeArray3 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				TruckData truckData = new TruckData
				{
					m_DeliveryTruckData = nativeArray2[j]
				};
				if (truckData.m_DeliveryTruckData.m_CargoCapacity == 0 || truckData.m_DeliveryTruckData.m_TransportedResources == Resource.NoResource || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				Resource transportedResources = truckData.m_DeliveryTruckData.m_TransportedResources;
				truckData.m_Entity = nativeArray[j];
				bool flag = false;
				if (nativeArray3.Length != 0)
				{
					truckData.m_TrailerData = nativeArray3[j];
					flag = true;
				}
				if (nativeArray4.Length != 0)
				{
					truckData.m_TractorData = nativeArray4[j];
					if (truckData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						CheckTrailers(transportedResources, flag, truckData);
						continue;
					}
				}
				if (flag)
				{
					CheckTractors(transportedResources, truckData);
					continue;
				}
				ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
				DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
				{
					m_Capacity = truckData.m_DeliveryTruckData.m_CargoCapacity,
					m_Cost = truckData.m_DeliveryTruckData.m_CostToDrive,
					m_Resources = transportedResources,
					m_Prefab1 = truckData.m_Entity
				};
				deliveryTruckItems.Add(in value);
			}
		}
		if (m_DeliveryTruckItems.Length >= 2)
		{
			m_DeliveryTruckItems.Sort();
			DeliveryTruckSelectItem deliveryTruckSelectItem = default(DeliveryTruckSelectItem);
			DeliveryTruckSelectItem deliveryTruckSelectItem2 = m_DeliveryTruckItems[0];
			int num = 0;
			for (int k = 1; k < m_DeliveryTruckItems.Length; k++)
			{
				DeliveryTruckSelectItem deliveryTruckSelectItem3 = m_DeliveryTruckItems[k];
				if (deliveryTruckSelectItem2.m_Resources != Resource.NoResource && deliveryTruckSelectItem2.m_Cost > deliveryTruckSelectItem3.m_Cost)
				{
					deliveryTruckSelectItem2.m_Resources &= ~deliveryTruckSelectItem3.m_Resources;
					for (int l = k + 1; l < m_DeliveryTruckItems.Length; l++)
					{
						if (deliveryTruckSelectItem2.m_Resources == Resource.NoResource)
						{
							break;
						}
						DeliveryTruckSelectItem deliveryTruckSelectItem4 = m_DeliveryTruckItems[l];
						if (deliveryTruckSelectItem2.m_Cost <= deliveryTruckSelectItem4.m_Cost)
						{
							break;
						}
						deliveryTruckSelectItem2.m_Resources &= ~deliveryTruckSelectItem4.m_Resources;
					}
				}
				if (deliveryTruckSelectItem2.m_Resources != Resource.NoResource)
				{
					m_DeliveryTruckItems[num++] = deliveryTruckSelectItem2;
					deliveryTruckSelectItem = deliveryTruckSelectItem2;
				}
				deliveryTruckSelectItem2 = deliveryTruckSelectItem3;
				if (deliveryTruckSelectItem2.m_Resources == Resource.NoResource || deliveryTruckSelectItem2.m_Cost * deliveryTruckSelectItem.m_Capacity <= deliveryTruckSelectItem.m_Cost * deliveryTruckSelectItem2.m_Capacity)
				{
					continue;
				}
				deliveryTruckSelectItem2.m_Resources &= ~deliveryTruckSelectItem.m_Resources;
				int num2 = num - 2;
				while (num2 >= 0 && deliveryTruckSelectItem2.m_Resources != Resource.NoResource)
				{
					DeliveryTruckSelectItem deliveryTruckSelectItem5 = m_DeliveryTruckItems[num2];
					if (deliveryTruckSelectItem2.m_Cost * deliveryTruckSelectItem5.m_Capacity <= deliveryTruckSelectItem5.m_Cost * deliveryTruckSelectItem2.m_Capacity)
					{
						break;
					}
					deliveryTruckSelectItem2.m_Resources &= ~deliveryTruckSelectItem5.m_Resources;
					num2--;
				}
			}
			if (deliveryTruckSelectItem2.m_Resources != Resource.NoResource)
			{
				m_DeliveryTruckItems[num++] = deliveryTruckSelectItem2;
			}
			if (num < m_DeliveryTruckItems.Length)
			{
				m_DeliveryTruckItems.RemoveRange(num, m_DeliveryTruckItems.Length - num);
			}
		}
		m_DeliveryTruckItems.TrimExcess();
	}

	private void CheckTrailers(Resource resourceMask, bool firstIsTrailer, TruckData firstData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTrailerData> nativeArray = chunk.GetNativeArray(ref m_CarTrailerDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData truckData = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				if (truckData.m_DeliveryTruckData.m_CargoCapacity != 0 || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				truckData.m_Entity = nativeArray2[j];
				truckData.m_TrailerData = nativeArray[j];
				if (firstData.m_TractorData.m_TrailerType != truckData.m_TrailerData.m_TrailerType || (firstData.m_TractorData.m_FixedTrailer != Entity.Null && firstData.m_TractorData.m_FixedTrailer != truckData.m_Entity) || (truckData.m_TrailerData.m_FixedTractor != Entity.Null && truckData.m_TrailerData.m_FixedTractor != firstData.m_Entity))
				{
					continue;
				}
				if (nativeArray4.Length != 0)
				{
					truckData.m_TractorData = nativeArray4[j];
					if (truckData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						CheckTrailers(resourceMask, firstIsTrailer, firstData, truckData);
						continue;
					}
				}
				if (firstIsTrailer)
				{
					CheckTractors(resourceMask, firstData, truckData);
					continue;
				}
				ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
				DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
				{
					m_Capacity = firstData.m_DeliveryTruckData.m_CargoCapacity + truckData.m_DeliveryTruckData.m_CargoCapacity,
					m_Cost = firstData.m_DeliveryTruckData.m_CostToDrive + truckData.m_DeliveryTruckData.m_CostToDrive,
					m_Resources = resourceMask,
					m_Prefab1 = firstData.m_Entity,
					m_Prefab2 = truckData.m_Entity
				};
				deliveryTruckItems.Add(in value);
			}
		}
	}

	private void CheckTrailers(Resource resourceMask, bool firstIsTrailer, TruckData firstData, TruckData secondData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTrailerData> nativeArray = chunk.GetNativeArray(ref m_CarTrailerDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData truckData = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				if (truckData.m_DeliveryTruckData.m_CargoCapacity != 0 || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				truckData.m_Entity = nativeArray2[j];
				truckData.m_TrailerData = nativeArray[j];
				if (secondData.m_TractorData.m_TrailerType != truckData.m_TrailerData.m_TrailerType || (secondData.m_TractorData.m_FixedTrailer != Entity.Null && secondData.m_TractorData.m_FixedTrailer != truckData.m_Entity) || (truckData.m_TrailerData.m_FixedTractor != Entity.Null && truckData.m_TrailerData.m_FixedTractor != secondData.m_Entity))
				{
					continue;
				}
				if (nativeArray4.Length != 0)
				{
					truckData.m_TractorData = nativeArray4[j];
					if (truckData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						if (!firstIsTrailer)
						{
							CheckTrailers(resourceMask, firstData, secondData, truckData);
						}
						continue;
					}
				}
				if (firstIsTrailer)
				{
					CheckTractors(resourceMask, firstData, secondData, truckData);
					continue;
				}
				ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
				DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
				{
					m_Capacity = firstData.m_DeliveryTruckData.m_CargoCapacity + secondData.m_DeliveryTruckData.m_CargoCapacity + truckData.m_DeliveryTruckData.m_CargoCapacity,
					m_Cost = firstData.m_DeliveryTruckData.m_CostToDrive + secondData.m_DeliveryTruckData.m_CostToDrive + truckData.m_DeliveryTruckData.m_CostToDrive,
					m_Resources = resourceMask,
					m_Prefab1 = firstData.m_Entity,
					m_Prefab2 = secondData.m_Entity,
					m_Prefab3 = truckData.m_Entity
				};
				deliveryTruckItems.Add(in value);
			}
		}
	}

	private void CheckTrailers(Resource resourceMask, TruckData firstData, TruckData secondData, TruckData thirdData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTrailerData> nativeArray = chunk.GetNativeArray(ref m_CarTrailerDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData truckData = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				if (truckData.m_DeliveryTruckData.m_CargoCapacity != 0 || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				truckData.m_Entity = nativeArray2[j];
				truckData.m_TrailerData = nativeArray[j];
				if (thirdData.m_TractorData.m_TrailerType != truckData.m_TrailerData.m_TrailerType || (thirdData.m_TractorData.m_FixedTrailer != Entity.Null && thirdData.m_TractorData.m_FixedTrailer != truckData.m_Entity) || (truckData.m_TrailerData.m_FixedTractor != Entity.Null && truckData.m_TrailerData.m_FixedTractor != thirdData.m_Entity))
				{
					continue;
				}
				if (nativeArray4.Length != 0)
				{
					truckData.m_TractorData = nativeArray4[j];
					if (truckData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						continue;
					}
				}
				m_DeliveryTruckItems.Add(new DeliveryTruckSelectItem
				{
					m_Capacity = firstData.m_DeliveryTruckData.m_CargoCapacity + secondData.m_DeliveryTruckData.m_CargoCapacity + thirdData.m_DeliveryTruckData.m_CargoCapacity + truckData.m_DeliveryTruckData.m_CargoCapacity,
					m_Cost = firstData.m_DeliveryTruckData.m_CostToDrive + secondData.m_DeliveryTruckData.m_CostToDrive + thirdData.m_DeliveryTruckData.m_CostToDrive + truckData.m_DeliveryTruckData.m_CostToDrive,
					m_Resources = resourceMask,
					m_Prefab1 = firstData.m_Entity,
					m_Prefab2 = secondData.m_Entity,
					m_Prefab3 = thirdData.m_Entity,
					m_Prefab4 = truckData.m_Entity
				});
			}
		}
	}

	private void CheckTractors(Resource resourceMask, TruckData secondData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTrailerData> nativeArray4 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData secondData2 = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				Resource resource = resourceMask;
				if (secondData2.m_DeliveryTruckData.m_CargoCapacity != 0)
				{
					resource &= secondData2.m_DeliveryTruckData.m_TransportedResources;
					if (resource == Resource.NoResource)
					{
						continue;
					}
				}
				if (!m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				secondData2.m_Entity = nativeArray2[j];
				secondData2.m_TractorData = nativeArray[j];
				if (secondData2.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(secondData2.m_TractorData.m_FixedTrailer != Entity.Null) || !(secondData2.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != secondData2.m_Entity)))
				{
					if (nativeArray4.Length != 0)
					{
						secondData2.m_TrailerData = nativeArray4[j];
						CheckTractors(resource, secondData2, secondData);
						continue;
					}
					ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
					DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
					{
						m_Capacity = secondData2.m_DeliveryTruckData.m_CargoCapacity + secondData.m_DeliveryTruckData.m_CargoCapacity,
						m_Cost = secondData2.m_DeliveryTruckData.m_CostToDrive + secondData.m_DeliveryTruckData.m_CostToDrive,
						m_Resources = resource,
						m_Prefab1 = secondData2.m_Entity,
						m_Prefab2 = secondData.m_Entity
					};
					deliveryTruckItems.Add(in value);
				}
			}
		}
	}

	private void CheckTractors(Resource resourceMask, TruckData secondData, TruckData thirdData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			NativeArray<CarTrailerData> nativeArray4 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData secondData2 = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				Resource resource = resourceMask;
				if (secondData2.m_DeliveryTruckData.m_CargoCapacity != 0)
				{
					resource &= secondData2.m_DeliveryTruckData.m_TransportedResources;
					if (resource == Resource.NoResource)
					{
						continue;
					}
				}
				if (!m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				secondData2.m_Entity = nativeArray2[j];
				secondData2.m_TractorData = nativeArray[j];
				if (secondData2.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(secondData2.m_TractorData.m_FixedTrailer != Entity.Null) || !(secondData2.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != secondData2.m_Entity)))
				{
					if (nativeArray4.Length != 0)
					{
						secondData2.m_TrailerData = nativeArray4[j];
						CheckTractors(resource, secondData2, secondData, thirdData);
						continue;
					}
					ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
					DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
					{
						m_Capacity = secondData2.m_DeliveryTruckData.m_CargoCapacity + secondData.m_DeliveryTruckData.m_CargoCapacity + thirdData.m_DeliveryTruckData.m_CargoCapacity,
						m_Cost = secondData2.m_DeliveryTruckData.m_CostToDrive + secondData.m_DeliveryTruckData.m_CostToDrive + thirdData.m_DeliveryTruckData.m_CostToDrive,
						m_Resources = resource,
						m_Prefab1 = secondData2.m_Entity,
						m_Prefab2 = secondData.m_Entity,
						m_Prefab3 = thirdData.m_Entity
					};
					deliveryTruckItems.Add(in value);
				}
			}
		}
	}

	private void CheckTractors(Resource resourceMask, TruckData secondData, TruckData thirdData, TruckData forthData)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0 || chunk.Has(ref m_CarTrailerDataType))
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<DeliveryTruckData> nativeArray3 = chunk.GetNativeArray(ref m_DeliveryTruckDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				TruckData truckData = new TruckData
				{
					m_DeliveryTruckData = nativeArray3[j]
				};
				Resource resource = resourceMask;
				if (truckData.m_DeliveryTruckData.m_CargoCapacity != 0)
				{
					resource &= truckData.m_DeliveryTruckData.m_TransportedResources;
					if (resource == Resource.NoResource)
					{
						continue;
					}
				}
				if (m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					truckData.m_Entity = nativeArray2[j];
					truckData.m_TractorData = nativeArray[j];
					if (truckData.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(truckData.m_TractorData.m_FixedTrailer != Entity.Null) || !(truckData.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != truckData.m_Entity)))
					{
						ref NativeList<DeliveryTruckSelectItem> deliveryTruckItems = ref m_DeliveryTruckItems;
						DeliveryTruckSelectItem value = new DeliveryTruckSelectItem
						{
							m_Capacity = truckData.m_DeliveryTruckData.m_CargoCapacity + secondData.m_DeliveryTruckData.m_CargoCapacity + thirdData.m_DeliveryTruckData.m_CargoCapacity + forthData.m_DeliveryTruckData.m_CargoCapacity,
							m_Cost = truckData.m_DeliveryTruckData.m_CostToDrive + secondData.m_DeliveryTruckData.m_CostToDrive + thirdData.m_DeliveryTruckData.m_CostToDrive + forthData.m_DeliveryTruckData.m_CostToDrive,
							m_Resources = resource,
							m_Prefab1 = truckData.m_Entity,
							m_Prefab2 = secondData.m_Entity,
							m_Prefab3 = thirdData.m_Entity,
							m_Prefab4 = forthData.m_Entity
						};
						deliveryTruckItems.Add(in value);
					}
				}
			}
		}
	}
}
