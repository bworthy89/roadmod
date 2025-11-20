using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Effects;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class SubObjectSystem : GameSystemBase
{
	private struct SubObjectOwnerData
	{
		public Entity m_Owner;

		public Entity m_Original;

		public bool m_Temp;

		public bool m_Created;

		public bool m_Deleted;

		public SubObjectOwnerData(Entity owner, Entity original, bool temp, bool created, bool deleted)
		{
			m_Owner = owner;
			m_Original = original;
			m_Temp = temp;
			m_Created = created;
			m_Deleted = deleted;
		}
	}

	public struct SubObjectData : IComparable<SubObjectData>
	{
		public Entity m_SubObject;

		public float m_Radius;

		public SubObjectData(Entity subObject, float radius)
		{
			m_SubObject = subObject;
			m_Radius = radius;
		}

		public int CompareTo(SubObjectData other)
		{
			return math.select(0, math.select(1, -1, m_Radius > other.m_Radius), m_Radius != other.m_Radius);
		}
	}

	private struct DeepSubObjectOwnerData
	{
		public Transform m_Transform;

		public Temp m_Temp;

		public Entity m_Entity;

		public Entity m_Prefab;

		public float m_Elevation;

		public PseudoRandomSeed m_RandomSeed;

		public bool m_Deleted;

		public bool m_New;

		public bool m_HasRandomSeed;

		public bool m_IsSubRandom;

		public bool m_UnderConstruction;

		public bool m_Destroyed;

		public bool m_Overridden;

		public int m_Depth;
	}

	private struct PlaceholderKey : IEquatable<PlaceholderKey>
	{
		public Entity m_GroupPrefab;

		public int m_GroupIndex;

		public PlaceholderKey(Entity groupPrefab, int groupIndex)
		{
			m_GroupPrefab = groupPrefab;
			m_GroupIndex = groupIndex;
		}

		public bool Equals(PlaceholderKey other)
		{
			if (m_GroupPrefab.Equals(other.m_GroupPrefab))
			{
				return m_GroupIndex == other.m_GroupIndex;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (17 * 31 + m_GroupPrefab.GetHashCode()) * 31 + m_GroupIndex.GetHashCode();
		}
	}

	private struct UpdateSubObjectsData
	{
		public NativeParallelMultiHashMap<Entity, Entity> m_OldEntities;

		public NativeParallelMultiHashMap<Entity, Entity> m_OriginalEntities;

		public NativeParallelHashMap<Entity, int2> m_PlaceholderRequirements;

		public NativeParallelHashMap<PlaceholderKey, Unity.Mathematics.Random> m_SelectedSpawnabled;

		public NativeList<AreaUtils.ObjectItem> m_ObjectBuffer;

		public NativeList<DeepSubObjectOwnerData> m_DeepOwners;

		public NativeList<ClearAreaData> m_ClearAreas;

		public ObjectRequirementFlags m_PlaceholderRequirementFlags;

		public Resource m_StoredResources;

		public bool m_RequirementsSearched;

		public void EnsureOldEntities(Allocator allocator)
		{
			if (!m_OldEntities.IsCreated)
			{
				m_OldEntities = new NativeParallelMultiHashMap<Entity, Entity>(32, allocator);
			}
		}

		public void EnsureOriginalEntities(Allocator allocator)
		{
			if (!m_OriginalEntities.IsCreated)
			{
				m_OriginalEntities = new NativeParallelMultiHashMap<Entity, Entity>(32, allocator);
			}
		}

		public void EnsurePlaceholderRequirements(Allocator allocator)
		{
			if (!m_PlaceholderRequirements.IsCreated)
			{
				m_PlaceholderRequirements = new NativeParallelHashMap<Entity, int2>(10, allocator);
			}
		}

		public void EnsureSelectedSpawnables(Allocator allocator)
		{
			if (!m_SelectedSpawnabled.IsCreated)
			{
				m_SelectedSpawnabled = new NativeParallelHashMap<PlaceholderKey, Unity.Mathematics.Random>(10, allocator);
			}
		}

		public void EnsureObjectBuffer(Allocator allocator)
		{
			if (!m_ObjectBuffer.IsCreated)
			{
				m_ObjectBuffer = new NativeList<AreaUtils.ObjectItem>(32, allocator);
			}
		}

		public void EnsureDeepOwners(Allocator allocator)
		{
			if (!m_DeepOwners.IsCreated)
			{
				m_DeepOwners = new NativeList<DeepSubObjectOwnerData>(16, allocator);
			}
		}

		public void Clear(bool deepOwners)
		{
			if (m_OldEntities.IsCreated)
			{
				m_OldEntities.Clear();
			}
			if (m_OriginalEntities.IsCreated)
			{
				m_OriginalEntities.Clear();
			}
			if (deepOwners && m_PlaceholderRequirements.IsCreated)
			{
				m_PlaceholderRequirements.Clear();
			}
			if (deepOwners && m_SelectedSpawnabled.IsCreated)
			{
				m_SelectedSpawnabled.Clear();
			}
			if (m_ObjectBuffer.IsCreated)
			{
				m_ObjectBuffer.Clear();
			}
			if (deepOwners && m_DeepOwners.IsCreated)
			{
				m_DeepOwners.Clear();
			}
			if (m_ClearAreas.IsCreated)
			{
				m_ClearAreas.Clear();
			}
			if (deepOwners)
			{
				m_PlaceholderRequirementFlags = (ObjectRequirementFlags)0;
				m_StoredResources = Resource.NoResource;
				m_RequirementsSearched = false;
			}
		}

		public void Dispose()
		{
			if (m_OldEntities.IsCreated)
			{
				m_OldEntities.Dispose();
			}
			if (m_OriginalEntities.IsCreated)
			{
				m_OriginalEntities.Dispose();
			}
			if (m_PlaceholderRequirements.IsCreated)
			{
				m_PlaceholderRequirements.Dispose();
			}
			if (m_SelectedSpawnabled.IsCreated)
			{
				m_SelectedSpawnabled.Dispose();
			}
			if (m_ObjectBuffer.IsCreated)
			{
				m_ObjectBuffer.Dispose();
			}
			if (m_DeepOwners.IsCreated)
			{
				m_DeepOwners.Dispose();
			}
			if (m_ClearAreas.IsCreated)
			{
				m_ClearAreas.Dispose();
			}
		}
	}

	[BurstCompile]
	private struct CheckSubObjectOwnersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<SubObjectsUpdated> m_SubObjectsUpdatedType;

		[ReadOnly]
		public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> m_VehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Creature> m_CreatureType;

		[ReadOnly]
		public ComponentLookup<Created> m_CreatedData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<Object> m_ObjectData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SubObjectOwnerData>.ParallelWriter m_OwnerQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<RentersUpdated> nativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity property = nativeArray[i].m_Property;
					if (m_SubObjects.HasBuffer(property))
					{
						m_OwnerQueue.Enqueue(new SubObjectOwnerData(property, Entity.Null, temp: false, m_CreatedData.HasComponent(property), m_DeletedData.HasComponent(property)));
					}
				}
				return;
			}
			NativeArray<SubObjectsUpdated> nativeArray2 = chunk.GetNativeArray(ref m_SubObjectsUpdatedType);
			if (nativeArray2.Length != 0)
			{
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity owner = nativeArray2[j].m_Owner;
					if (m_SubObjects.HasBuffer(owner))
					{
						m_OwnerQueue.Enqueue(new SubObjectOwnerData(owner, Entity.Null, temp: false, m_CreatedData.HasComponent(owner), m_DeletedData.HasComponent(owner)));
					}
				}
				return;
			}
			bool flag = chunk.Has(ref m_DeletedType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			if (chunk.Has(ref m_ObjectType) && chunk.Has(ref m_OwnerType) && !chunk.Has(ref m_ServiceUpgradeType) && !chunk.Has(ref m_BuildingType) && !chunk.Has(ref m_VehicleType) && !chunk.Has(ref m_CreatureType) && (nativeArray3.Length == 0 || flag))
			{
				return;
			}
			NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			if (flag)
			{
				for (int k = 0; k < nativeArray4.Length; k++)
				{
					Entity entity = nativeArray4[k];
					DynamicBuffer<SubObject> dynamicBuffer = bufferAccessor[k];
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						Entity subObject = dynamicBuffer[l].m_SubObject;
						if (m_DeletedData.HasComponent(subObject) || m_SecondaryData.HasComponent(subObject))
						{
							continue;
						}
						if (m_OwnerData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == entity && (m_ServiceUpgradeData.HasComponent(subObject) || m_BuildingData.HasComponent(subObject)))
						{
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, subObject, in m_AppliedTypes);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subObject, default(Deleted));
							if (m_SubObjects.HasBuffer(subObject))
							{
								m_OwnerQueue.Enqueue(new SubObjectOwnerData(subObject, Entity.Null, temp: false, created: false, deleted: true));
							}
						}
						if (m_AttachedData.HasComponent(subObject) && m_AttachedData[subObject].m_Parent == entity)
						{
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, subObject, default(Attached));
						}
					}
				}
			}
			bool created = chunk.Has(ref m_CreatedType);
			for (int m = 0; m < nativeArray4.Length; m++)
			{
				Entity entity2 = nativeArray4[m];
				if (nativeArray3.Length != 0)
				{
					Temp temp = nativeArray3[m];
					m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity2, temp.m_Original, temp: true, created, flag));
					if (flag || !m_SubObjects.HasBuffer(temp.m_Original))
					{
						continue;
					}
					DynamicBuffer<SubObject> dynamicBuffer2 = m_SubObjects[temp.m_Original];
					for (int n = 0; n < dynamicBuffer2.Length; n++)
					{
						Entity subObject2 = dynamicBuffer2[n].m_SubObject;
						if (!m_OwnerData.HasComponent(subObject2) || !m_AttachedData.HasComponent(subObject2) || m_SecondaryData.HasComponent(subObject2))
						{
							continue;
						}
						Owner owner2 = m_OwnerData[subObject2];
						if (owner2.m_Owner != temp.m_Original && m_AttachedData[subObject2].m_Parent == temp.m_Original && !m_HiddenData.HasComponent(owner2.m_Owner))
						{
							while (m_OwnerData.HasComponent(owner2.m_Owner) && m_ObjectData.HasComponent(owner2.m_Owner) && !m_ServiceUpgradeData.HasComponent(owner2.m_Owner) && !m_BuildingData.HasComponent(owner2.m_Owner))
							{
								owner2 = m_OwnerData[owner2.m_Owner];
							}
							m_OwnerQueue.Enqueue(new SubObjectOwnerData(owner2.m_Owner, Entity.Null, temp: true, m_CreatedData.HasComponent(owner2.m_Owner), m_DeletedData.HasComponent(owner2.m_Owner)));
						}
					}
					continue;
				}
				m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity2, Entity.Null, temp: false, created, flag));
				DynamicBuffer<SubObject> dynamicBuffer3 = bufferAccessor[m];
				for (int num = 0; num < dynamicBuffer3.Length; num++)
				{
					Entity subObject3 = dynamicBuffer3[num].m_SubObject;
					if (!m_OwnerData.HasComponent(subObject3) || !m_AttachedData.HasComponent(subObject3) || m_SecondaryData.HasComponent(subObject3))
					{
						continue;
					}
					Owner owner3 = m_OwnerData[subObject3];
					if (owner3.m_Owner != entity2 && m_AttachedData[subObject3].m_Parent == entity2 && !m_HiddenData.HasComponent(owner3.m_Owner))
					{
						while (m_OwnerData.HasComponent(owner3.m_Owner) && m_ObjectData.HasComponent(owner3.m_Owner) && !m_ServiceUpgradeData.HasComponent(owner3.m_Owner) && !m_BuildingData.HasComponent(owner3.m_Owner))
						{
							owner3 = m_OwnerData[owner3.m_Owner];
						}
						m_OwnerQueue.Enqueue(new SubObjectOwnerData(owner3.m_Owner, Entity.Null, temp: false, m_CreatedData.HasComponent(owner3.m_Owner), m_DeletedData.HasComponent(owner3.m_Owner)));
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
	private struct CollectSubObjectOwnersJob : IJob
	{
		public NativeQueue<SubObjectOwnerData> m_OwnerQueue;

		public NativeList<SubObjectOwnerData> m_OwnerList;

		public NativeParallelHashMap<Entity, SubObjectOwnerData> m_OwnerMap;

		public void Execute()
		{
			SubObjectOwnerData item;
			while (m_OwnerQueue.TryDequeue(out item))
			{
				if (m_OwnerMap.TryGetValue(item.m_Owner, out var item2))
				{
					if (item.m_Original != Entity.Null)
					{
						item.m_Created |= item2.m_Created;
						item.m_Deleted |= item2.m_Deleted;
						m_OwnerMap[item.m_Owner] = item;
					}
					else
					{
						item2.m_Created |= item.m_Created;
						item2.m_Deleted |= item.m_Deleted;
						m_OwnerMap[item.m_Owner] = item2;
					}
				}
				else
				{
					m_OwnerMap.Add(item.m_Owner, item);
				}
			}
			m_OwnerList.SetCapacity(m_OwnerMap.Count());
			NativeParallelHashMap<Entity, SubObjectOwnerData>.Enumerator enumerator = m_OwnerMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				m_OwnerList.Add(in enumerator.Current.Value);
			}
			enumerator.Dispose();
		}
	}

	[BurstCompile]
	private struct FillIgnoreSetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		public NativeParallelHashSet<Entity> m_IgnoreSet;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity item = nativeArray[i];
				Temp temp = nativeArray2[i];
				m_IgnoreSet.Add(item);
				if (temp.m_Original != Entity.Null)
				{
					m_IgnoreSet.Add(temp.m_Original);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateSubObjectsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<PillarData> m_PrefabPillarData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ThemeData> m_PrefabThemeData;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> m_PrefabMovingObjectData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> m_PrefabWorkVehicleData;

		[ReadOnly]
		public ComponentLookup<EffectData> m_PrefabEffectData;

		[ReadOnly]
		public ComponentLookup<StreetLightData> m_PrefabStreetLightData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PrefabPlaceableNetData;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> m_PrefabCargoTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> m_PrefabPlaceholderObjectData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyData;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> m_ActivityPropData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Aligned> m_AlignedData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<StreetLight> m_StreetLightData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Overridden> m_OverriddenData;

		[ReadOnly]
		public ComponentLookup<Surface> m_SurfaceData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerData;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> m_GarbageFacilityData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<ResidentialProperty> m_ResidentialPropertyData;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeepData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NetNodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_NetEdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_NetCurveData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdData;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHousehold;

		[ReadOnly]
		public ComponentLookup<Human> m_HumanData;

		[ReadOnly]
		public ComponentLookup<Area> m_AreaData;

		[ReadOnly]
		public ComponentLookup<Geometry> m_AreaGeometryData;

		[ReadOnly]
		public ComponentLookup<Clear> m_AreaClearData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<Watercraft> m_WatercraftData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public BufferLookup<Renter> m_BuildingRenters;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> m_PrefabSubLanes;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PlaceholderObjects;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_ObjectRequirements;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_PrefabActivityLocations;

		[ReadOnly]
		public BufferLookup<CompanyBrandElement> m_CompanyBrands;

		[ReadOnly]
		public BufferLookup<AffiliatedBrandElement> m_AffiliatedBrands;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_PrefabProceduralBones;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_PrefabServiceUpkeepDatas;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_PrefabSubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_PrefabCharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_PrefabAnimationClips;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Entity m_DefaultTheme;

		[ReadOnly]
		public Entity m_TransformEditor;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public CitizenHappinessParameterData m_HappinessParameterData;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		[ReadOnly]
		public NativeArray<SubObjectOwnerData> m_OwnerList;

		[ReadOnly]
		public NativeParallelHashSet<Entity> m_IgnoreSet;

		[ReadOnly]
		public NativeParallelHashMap<Entity, SubObjectOwnerData> m_OwnerMap;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public NativeQueue<Entity>.ParallelWriter m_LoopErrorPrefabs;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			SubObjectOwnerData subObjectOwnerData = m_OwnerList[index];
			PrefabRef prefabRef = m_PrefabRefData[subObjectOwnerData.m_Owner];
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool relative = false;
			bool interpolated = false;
			bool native = m_NativeData.HasComponent(subObjectOwnerData.m_Owner);
			if (m_TransformData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag = true;
				if (!m_EditorMode && (m_PrefabMovingObjectData.HasComponent(prefabRef.m_Prefab) || (m_PrefabPlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & PlacementFlags.Swaying) != PlacementFlags.None)))
				{
					relative = true;
					interpolated = m_InterpolatedTransformData.HasComponent(subObjectOwnerData.m_Owner);
				}
			}
			else if (m_NetNodeData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag2 = true;
			}
			else if (m_NetEdgeData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag3 = true;
			}
			else if (m_AreaData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag4 = true;
			}
			if (!subObjectOwnerData.m_Deleted && flag && m_ServiceUpgradeData.HasComponent(subObjectOwnerData.m_Owner) && m_OwnerData.TryGetComponent(subObjectOwnerData.m_Owner, out var componentData2) && m_OwnerMap.ContainsKey(componentData2.m_Owner))
			{
				return;
			}
			bool flag5 = false;
			Temp ownerTemp = default(Temp);
			if (m_TempData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag5 = true;
				ownerTemp = m_TempData[subObjectOwnerData.m_Owner];
			}
			bool flag6 = false;
			float ownerElevation = 0f;
			DynamicBuffer<InstalledUpgrade> bufferData = default(DynamicBuffer<InstalledUpgrade>);
			if (flag)
			{
				m_InstalledUpgrades.TryGetBuffer(subObjectOwnerData.m_Owner, out bufferData);
			}
			if (!subObjectOwnerData.m_Deleted)
			{
				if (!flag2 && !flag3)
				{
					if (subObjectOwnerData.m_Temp)
					{
						if ((ownerTemp.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) != 0 || !flag5 || (m_EditorMode && ownerTemp.m_Original != Entity.Null && (!bufferData.IsCreated || bufferData.Length == 0)))
						{
							flag6 = true;
						}
					}
					else if (!subObjectOwnerData.m_Created && m_EditorMode && !m_UpdatedData.HasComponent(prefabRef.m_Prefab) && (!bufferData.IsCreated || bufferData.Length == 0))
					{
						return;
					}
				}
				if (m_ElevationData.HasComponent(subObjectOwnerData.m_Owner))
				{
					ownerElevation = m_ElevationData[subObjectOwnerData.m_Owner].m_Elevation;
				}
				else if (m_NetElevationData.HasComponent(subObjectOwnerData.m_Owner))
				{
					ownerElevation = math.cmin(m_NetElevationData[subObjectOwnerData.m_Owner].m_Elevation);
				}
			}
			if (subObjectOwnerData.m_Temp && !flag5)
			{
				subObjectOwnerData.m_Original = subObjectOwnerData.m_Owner;
			}
			if (flag6 && flag4 && (ownerTemp.m_Flags & TempFlags.Select) != 0 && m_OwnerData.TryGetComponent(subObjectOwnerData.m_Owner, out var componentData3) && m_TempData.TryGetComponent(componentData3.m_Owner, out var componentData4) && (componentData4.m_Flags & TempFlags.Select) != 0)
			{
				ownerTemp.m_Flags &= ~TempFlags.Select;
			}
			UpdateSubObjectsData updateData = default(UpdateSubObjectsData);
			DynamicBuffer<SubObject> subObjects = m_SubObjects[subObjectOwnerData.m_Owner];
			FillOldSubObjectsBuffer(subObjectOwnerData.m_Owner, subObjects, ref updateData, subObjectOwnerData.m_Temp, flag6);
			if (!subObjectOwnerData.m_Deleted)
			{
				if (m_SubObjects.HasBuffer(subObjectOwnerData.m_Original))
				{
					DynamicBuffer<SubObject> subObjects2 = m_SubObjects[subObjectOwnerData.m_Original];
					FillOriginalSubObjectsBuffer(subObjectOwnerData.m_Original, subObjects2, ref updateData, flag6);
				}
				if (bufferData.IsCreated)
				{
					FillClearAreas(subObjectOwnerData.m_Owner, bufferData, ref updateData);
				}
				PseudoRandomSeed componentData5;
				Unity.Mathematics.Random random = ((!m_PseudoRandomSeedData.TryGetComponent(subObjectOwnerData.m_Owner, out componentData5)) ? m_RandomSeed.GetRandom(index) : componentData5.GetRandom(PseudoRandomSeed.kSubObject));
				Unity.Mathematics.Random subRandom = random;
				EnsurePlaceholderRequirements(subObjectOwnerData.m_Owner, prefabRef.m_Prefab, ref updateData, ref random, flag);
				if (flag6)
				{
					Transform ownerTransform = default(Transform);
					if (flag)
					{
						ownerTransform = m_TransformData[subObjectOwnerData.m_Owner];
					}
					DuplicateSubObjects(index, ref random, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Original, ownerTransform, ref updateData, prefabRef.m_Prefab, subObjectOwnerData.m_Temp, ownerTemp, ownerElevation, flag, native, relative, interpolated, 0);
				}
				else if (flag)
				{
					Transform transform = m_TransformData[subObjectOwnerData.m_Owner];
					bool isUnderConstruction = false;
					bool isDestroyed = m_DestroyedData.HasComponent(subObjectOwnerData.m_Owner);
					bool isOverridden = m_OverriddenData.HasComponent(subObjectOwnerData.m_Owner);
					if (m_UnderConstructionData.TryGetComponent(subObjectOwnerData.m_Owner, out var componentData6))
					{
						isUnderConstruction = componentData6.m_NewPrefab == Entity.Null;
					}
					CreateSubObjects(index, ref random, ref subRandom, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Owner, transform, transform, transform, ref updateData, prefabRef.m_Prefab, isTransform: true, isEdge: false, isNode: false, subObjectOwnerData.m_Temp, ownerTemp, ownerElevation, native, relative, interpolated, isUnderConstruction, isDestroyed, isOverridden, 0);
				}
				else if (flag2)
				{
					Game.Net.Node node = m_NetNodeData[subObjectOwnerData.m_Owner];
					Transform transform2 = new Transform(node.m_Position, node.m_Rotation);
					CreateSubObjects(index, ref random, ref subRandom, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Owner, transform2, transform2, transform2, ref updateData, prefabRef.m_Prefab, isTransform: false, isEdge: false, isNode: true, subObjectOwnerData.m_Temp, ownerTemp, ownerElevation, native, relative: false, interpolated: false, isUnderConstruction: false, isDestroyed: false, isOverridden: false, 0);
				}
				else if (flag3)
				{
					Curve curve = m_NetCurveData[subObjectOwnerData.m_Owner];
					CreateSubObjects(transform1: new Transform(curve.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve.m_Bezier))), transform2: new Transform(MathUtils.Position(curve.m_Bezier, 0.5f), NetUtils.GetNodeRotation(MathUtils.Tangent(curve.m_Bezier, 0.5f))), transform3: new Transform(curve.m_Bezier.d, NetUtils.GetNodeRotation(-MathUtils.EndTangent(curve.m_Bezier))), jobIndex: index, random: ref random, subRandom: ref subRandom, topOwner: subObjectOwnerData.m_Owner, owner: subObjectOwnerData.m_Owner, updateData: ref updateData, prefab: prefabRef.m_Prefab, isTransform: false, isEdge: true, isNode: false, isTemp: subObjectOwnerData.m_Temp, ownerTemp: ownerTemp, ownerElevation: ownerElevation, native: native, relative: false, interpolated: false, isUnderConstruction: false, isDestroyed: false, isOverridden: false, depth: 0);
				}
				else if (flag4)
				{
					Area area = m_AreaData[subObjectOwnerData.m_Owner];
					Geometry geometry = m_AreaGeometryData[subObjectOwnerData.m_Owner];
					DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[subObjectOwnerData.m_Owner];
					DynamicBuffer<Triangle> triangles = m_AreaTriangles[subObjectOwnerData.m_Owner];
					RelocateSubObjects(index, ref random, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Owner, subObjectOwnerData.m_Original, area, geometry, nodes, triangles, ref updateData, prefabRef.m_Prefab, subObjectOwnerData.m_Temp, ownerTemp, ownerElevation);
				}
				if (bufferData.IsCreated)
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						if (m_OwnerMap.TryGetValue(bufferData[i].m_Upgrade, out var item) && !item.m_Deleted)
						{
							updateData.EnsureDeepOwners(Allocator.Temp);
							DeepSubObjectOwnerData value = new DeepSubObjectOwnerData
							{
								m_Transform = m_TransformData[item.m_Owner],
								m_Entity = item.m_Owner,
								m_Prefab = m_PrefabRefData[item.m_Owner],
								m_RandomSeed = m_PseudoRandomSeedData[item.m_Owner],
								m_HasRandomSeed = true,
								m_IsSubRandom = true,
								m_Depth = 1
							};
							if (m_ElevationData.TryGetComponent(item.m_Owner, out var componentData7))
							{
								value.m_Elevation = componentData7.m_Elevation;
							}
							if (m_UnderConstructionData.TryGetComponent(item.m_Owner, out var componentData8))
							{
								value.m_UnderConstruction = componentData8.m_NewPrefab == Entity.Null;
							}
							if (m_TempData.TryGetComponent(item.m_Owner, out var componentData9))
							{
								value.m_Temp = componentData9;
							}
							value.m_Destroyed = m_DestroyedData.HasComponent(item.m_Owner);
							value.m_Overridden = m_OverriddenData.HasComponent(item.m_Owner);
							updateData.m_DeepOwners.Add(in value);
						}
					}
				}
			}
			RemoveUnusedOldSubObjects(index, subObjectOwnerData.m_Owner, subObjects, ref updateData, subObjectOwnerData.m_Temp, flag6);
			if (updateData.m_DeepOwners.IsCreated)
			{
				int num = 0;
				while (num < updateData.m_DeepOwners.Length)
				{
					DeepSubObjectOwnerData deepSubObjectOwnerData = updateData.m_DeepOwners[num++];
					updateData.Clear(deepOwners: false);
					if (!deepSubObjectOwnerData.m_New)
					{
						subObjects = m_SubObjects[deepSubObjectOwnerData.m_Entity];
						FillOldSubObjectsBuffer(deepSubObjectOwnerData.m_Entity, subObjects, ref updateData, subObjectOwnerData.m_Temp, flag6);
					}
					if (!deepSubObjectOwnerData.m_Deleted)
					{
						Unity.Mathematics.Random random2 = ((!deepSubObjectOwnerData.m_HasRandomSeed) ? m_RandomSeed.GetRandom(index + num * 137209) : deepSubObjectOwnerData.m_RandomSeed.GetRandom(PseudoRandomSeed.kSubObject));
						Unity.Mathematics.Random subRandom2 = random2;
						if (deepSubObjectOwnerData.m_IsSubRandom)
						{
							random2 = ((!m_PseudoRandomSeedData.TryGetComponent(subObjectOwnerData.m_Owner, out var componentData10)) ? m_RandomSeed.GetRandom(index) : componentData10.GetRandom(PseudoRandomSeed.kSubObject));
						}
						bool num2 = HasSubRequirements(deepSubObjectOwnerData.m_Prefab);
						if (num2)
						{
							updateData.m_PlaceholderRequirements.Clear();
							updateData.m_PlaceholderRequirementFlags = (ObjectRequirementFlags)0;
							updateData.m_StoredResources = Resource.NoResource;
							updateData.m_RequirementsSearched = false;
							EnsurePlaceholderRequirements(Entity.Null, deepSubObjectOwnerData.m_Prefab, ref updateData, ref random2, isObject: true);
						}
						else
						{
							EnsurePlaceholderRequirements(subObjectOwnerData.m_Owner, prefabRef.m_Prefab, ref updateData, ref random2, flag);
						}
						if (m_SubObjects.HasBuffer(deepSubObjectOwnerData.m_Temp.m_Original))
						{
							FillOriginalSubObjectsBuffer(subObjects: m_SubObjects[deepSubObjectOwnerData.m_Temp.m_Original], owner: deepSubObjectOwnerData.m_Temp.m_Original, updateData: ref updateData, useIgnoreSet: flag6);
						}
						if (flag6)
						{
							DuplicateSubObjects(index, ref random2, subObjectOwnerData.m_Owner, deepSubObjectOwnerData.m_Entity, deepSubObjectOwnerData.m_Temp.m_Original, deepSubObjectOwnerData.m_Transform, ref updateData, deepSubObjectOwnerData.m_Prefab, subObjectOwnerData.m_Temp, deepSubObjectOwnerData.m_Temp, deepSubObjectOwnerData.m_Elevation, hasTransform: true, native, relative, interpolated, deepSubObjectOwnerData.m_Depth);
						}
						else
						{
							CreateSubObjects(index, ref random2, ref subRandom2, subObjectOwnerData.m_Owner, deepSubObjectOwnerData.m_Entity, deepSubObjectOwnerData.m_Transform, deepSubObjectOwnerData.m_Transform, deepSubObjectOwnerData.m_Transform, ref updateData, deepSubObjectOwnerData.m_Prefab, isTransform: true, isEdge: false, isNode: false, subObjectOwnerData.m_Temp, deepSubObjectOwnerData.m_Temp, deepSubObjectOwnerData.m_Elevation, native, relative, interpolated, deepSubObjectOwnerData.m_UnderConstruction, deepSubObjectOwnerData.m_Destroyed, deepSubObjectOwnerData.m_Overridden, deepSubObjectOwnerData.m_Depth);
						}
						if (num2)
						{
							updateData.m_PlaceholderRequirements.Clear();
							updateData.m_PlaceholderRequirementFlags = (ObjectRequirementFlags)0;
							updateData.m_StoredResources = Resource.NoResource;
							updateData.m_RequirementsSearched = false;
						}
					}
					if (!deepSubObjectOwnerData.m_New)
					{
						RemoveUnusedOldSubObjects(index, deepSubObjectOwnerData.m_Entity, subObjects, ref updateData, subObjectOwnerData.m_Temp, flag6);
					}
				}
			}
			updateData.Dispose();
		}

		private bool HasSubRequirements(Entity ownerPrefab)
		{
			if (m_ObjectRequirements.TryGetBuffer(ownerPrefab, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if ((bufferData[i].m_Type & ObjectRequirementType.SelectOnly) != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void FillOldSubObjectsBuffer(Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSubObjectsData updateData, bool isTemp, bool useIgnoreSet)
		{
			if (subObjects.Length == 0)
			{
				return;
			}
			updateData.EnsureOldEntities(Allocator.Temp);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_OwnerData.HasComponent(subObject) && !m_ServiceUpgradeData.HasComponent(subObject) && !m_BuildingData.HasComponent(subObject) && !m_SecondaryData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == owner && isTemp == m_TempData.HasComponent(subObject) && (!useIgnoreSet || !m_IgnoreSet.Contains(subObject)))
				{
					if (m_EditorMode && m_EditorContainerData.HasComponent(subObject))
					{
						Game.Tools.EditorContainer editorContainer = m_EditorContainerData[subObject];
						updateData.m_OldEntities.Add(editorContainer.m_Prefab, subObject);
					}
					else
					{
						PrefabRef prefabRef = m_PrefabRefData[subObject];
						updateData.m_OldEntities.Add(prefabRef.m_Prefab, subObject);
					}
				}
			}
		}

		private void FillOriginalSubObjectsBuffer(Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSubObjectsData updateData, bool useIgnoreSet)
		{
			if (subObjects.Length == 0)
			{
				return;
			}
			updateData.EnsureOriginalEntities(Allocator.Temp);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_OwnerData.HasComponent(subObject) && !m_ServiceUpgradeData.HasComponent(subObject) && !m_BuildingData.HasComponent(subObject) && !m_SecondaryData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == owner && !m_TempData.HasComponent(subObject) && (!useIgnoreSet || !m_IgnoreSet.Contains(subObject)))
				{
					if (m_EditorMode && m_EditorContainerData.HasComponent(subObject))
					{
						Game.Tools.EditorContainer editorContainer = m_EditorContainerData[subObject];
						updateData.m_OriginalEntities.Add(editorContainer.m_Prefab, subObject);
					}
					else
					{
						PrefabRef prefabRef = m_PrefabRefData[subObject];
						updateData.m_OriginalEntities.Add(prefabRef.m_Prefab, subObject);
					}
				}
			}
		}

		private void FillClearAreas(Entity owner, DynamicBuffer<InstalledUpgrade> installedUpgrades, ref UpdateSubObjectsData updateData)
		{
			ClearAreaHelpers.FillClearAreas(installedUpgrades, Entity.Null, m_TransformData, m_AreaClearData, m_PrefabRefData, m_PrefabObjectGeometryData, m_SubAreas, m_AreaNodes, m_AreaTriangles, ref updateData.m_ClearAreas);
			ClearAreaHelpers.InitClearAreas(updateData.m_ClearAreas, m_TransformData[owner]);
		}

		private void RemoveUnusedOldSubObjects(int jobIndex, Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSubObjectsData updateData, bool isTemp, bool useIgnoreSet)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (!m_OwnerData.HasComponent(subObject) || m_ServiceUpgradeData.HasComponent(subObject) || m_BuildingData.HasComponent(subObject) || m_SecondaryData.HasComponent(subObject) || !(m_OwnerData[subObject].m_Owner == owner) || (useIgnoreSet && m_IgnoreSet.Contains(subObject)))
				{
					continue;
				}
				if (isTemp == m_TempData.HasComponent(subObject))
				{
					Entity key = ((!m_EditorMode || !m_EditorContainerData.HasComponent(subObject)) ? m_PrefabRefData[subObject].m_Prefab : m_EditorContainerData[subObject].m_Prefab);
					if (updateData.m_OldEntities.TryGetFirstValue(key, out var item, out var it))
					{
						m_CommandBuffer.RemoveComponent(jobIndex, item, in m_AppliedTypes);
						m_CommandBuffer.AddComponent(jobIndex, item, default(Deleted));
						updateData.m_OldEntities.Remove(it);
						if (m_SubObjects.HasBuffer(item))
						{
							updateData.EnsureDeepOwners(Allocator.Temp);
							ref NativeList<DeepSubObjectOwnerData> reference = ref updateData.m_DeepOwners;
							DeepSubObjectOwnerData value = new DeepSubObjectOwnerData
							{
								m_Entity = item,
								m_Deleted = true
							};
							reference.Add(in value);
						}
					}
				}
				else if (isTemp && updateData.m_OriginalEntities.IsCreated)
				{
					Entity key2 = ((!m_EditorMode || !m_EditorContainerData.HasComponent(subObject)) ? m_PrefabRefData[subObject].m_Prefab : m_EditorContainerData[subObject].m_Prefab);
					if (updateData.m_OriginalEntities.TryGetFirstValue(key2, out var item2, out var it2))
					{
						m_CommandBuffer.AddComponent(jobIndex, item2, default(Hidden));
						m_CommandBuffer.AddComponent(jobIndex, item2, default(BatchesUpdated));
						updateData.m_OriginalEntities.Remove(it2);
					}
				}
			}
		}

		private void DuplicateSubObjects(int jobIndex, ref Unity.Mathematics.Random random, Entity topOwner, Entity owner, Entity original, Transform ownerTransform, ref UpdateSubObjectsData updateData, Entity prefab, bool isTemp, Temp ownerTemp, float ownerElevation, bool hasTransform, bool native, bool relative, bool interpolated, int depth)
		{
			Transform componentData = default(Transform);
			if (hasTransform && m_TransformData.TryGetComponent(original, out componentData))
			{
				componentData = ObjectUtils.InverseTransform(componentData);
			}
			if (!m_SubObjects.TryGetBuffer(original, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (m_ServiceUpgradeData.HasComponent(subObject) || m_BuildingData.HasComponent(subObject) || m_SecondaryData.HasComponent(subObject) || m_IgnoreSet.Contains(subObject))
				{
					continue;
				}
				Entity prefab2 = m_PrefabRefData[subObject].m_Prefab;
				Transform transform = m_TransformData[subObject];
				Transform transform2 = transform;
				int num = 0;
				int groupIndex = 0;
				int probability = 100;
				int prefabSubIndex = -1;
				bool flag = false;
				Relative componentData3;
				if (m_LocalTransformCacheData.HasComponent(subObject))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[subObject];
					transform2.m_Position = localTransformCache.m_Position;
					transform2.m_Rotation = localTransformCache.m_Rotation;
					num = localTransformCache.m_ParentMesh;
					groupIndex = localTransformCache.m_GroupIndex;
					probability = localTransformCache.m_Probability;
					prefabSubIndex = localTransformCache.m_PrefabSubIndex;
					transform = ObjectUtils.LocalToWorld(ownerTransform, transform2);
					if (m_ElevationData.TryGetComponent(subObject, out var componentData2))
					{
						flag = (componentData2.m_Flags & ElevationFlags.OnAttachedParent) != 0;
					}
				}
				else if (m_RelativeData.TryGetComponent(subObject, out componentData3))
				{
					transform2 = componentData3.ToTransform();
					if (m_ElevationData.TryGetComponent(subObject, out var componentData4))
					{
						num = ObjectUtils.GetSubParentMesh(componentData4.m_Flags);
						flag = (componentData4.m_Flags & ElevationFlags.OnAttachedParent) != 0;
					}
					else
					{
						num = -1;
					}
				}
				else if (hasTransform)
				{
					transform2 = ObjectUtils.WorldToLocal(componentData, transform);
					if (m_ElevationData.TryGetComponent(subObject, out var componentData5))
					{
						num = ObjectUtils.GetSubParentMesh(componentData5.m_Flags);
						flag = (componentData5.m_Flags & ElevationFlags.OnAttachedParent) != 0;
					}
					else
					{
						num = -1;
					}
				}
				SubObjectFlags subObjectFlags = (SubObjectFlags)0;
				if (flag)
				{
					num = -1;
					subObjectFlags |= SubObjectFlags.OnAttachedParent;
				}
				else if (num == -1)
				{
					subObjectFlags |= SubObjectFlags.OnGround;
				}
				if (m_EditorMode && m_EditorContainerData.HasComponent(subObject))
				{
					Game.Tools.EditorContainer editorContainer = m_EditorContainerData[subObject];
					CreateContainerObject(jobIndex, owner, isTemp, ownerTemp, ownerElevation, Entity.Null, transform, transform2, ref updateData, editorContainer.m_Prefab, editorContainer.m_Scale, editorContainer.m_Intensity, num, editorContainer.m_GroupIndex, prefabSubIndex);
				}
				else
				{
					CreateSubObject(jobIndex, ref random, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transform, transform2, subObjectFlags, ref updateData, prefab2, m_EditorMode, native, relative, interpolated, underConstruction: false, isDestroyed: false, isOverridden: false, updated: false, -1, num, groupIndex, probability, prefabSubIndex, depth);
				}
			}
		}

		private void RelocateSubObjects(int jobIndex, ref Unity.Mathematics.Random random, Entity topOwner, Entity owner, Entity original, Area area, Geometry geometry, DynamicBuffer<Game.Areas.Node> nodes, DynamicBuffer<Triangle> triangles, ref UpdateSubObjectsData updateData, Entity prefab, bool isTemp, Temp ownerTemp, float ownerElevation)
		{
			if (m_SubObjects.TryGetBuffer(original, out var bufferData))
			{
				ownerTemp.m_Flags &= ~TempFlags.Modify;
				NativeArray<SubObjectData> array = new NativeArray<SubObjectData>(bufferData.Length, Allocator.Temp);
				AreaGeometryData areaData = m_PrefabAreaGeometryData[prefab];
				int num = 0;
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subObject = bufferData[i].m_SubObject;
					if (!m_SecondaryData.HasComponent(subObject))
					{
						Entity prefab2 = m_PrefabRefData[subObject].m_Prefab;
						ObjectGeometryData objectGeometryData = default(ObjectGeometryData);
						if (m_PrefabObjectGeometryData.HasComponent(prefab2))
						{
							objectGeometryData = m_PrefabObjectGeometryData[prefab2];
						}
						float num2 = (((objectGeometryData.m_Flags & GeometryFlags.Circular) == 0) ? (math.length(MathUtils.Size(objectGeometryData.m_Bounds.xz)) * 0.5f) : (objectGeometryData.m_Size.x * 0.5f));
						if (m_BuildingData.HasComponent(subObject))
						{
							Transform transform = m_TransformData[subObject];
							float minNodeDistance = AreaUtils.GetMinNodeDistance(areaData);
							updateData.EnsureObjectBuffer(Allocator.Temp);
							updateData.m_ObjectBuffer.Add(new AreaUtils.ObjectItem(num2 + minNodeDistance, transform.m_Position.xz, Entity.Null));
						}
						else
						{
							array[num++] = new SubObjectData
							{
								m_SubObject = subObject,
								m_Radius = num2
							};
						}
					}
				}
				array.Sort();
				for (int j = 0; j < num; j++)
				{
					Entity entity = array[j].m_SubObject;
					Entity prefab3 = m_PrefabRefData[entity].m_Prefab;
					Transform transform2 = m_TransformData[entity];
					ObjectGeometryData objectGeometryData2 = default(ObjectGeometryData);
					if (m_PrefabObjectGeometryData.HasComponent(prefab3))
					{
						objectGeometryData2 = m_PrefabObjectGeometryData[prefab3];
					}
					float num3;
					float3 @float;
					if ((objectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
					{
						num3 = objectGeometryData2.m_Size.x * 0.5f;
						@float = default(float3);
					}
					else
					{
						num3 = math.length(MathUtils.Size(objectGeometryData2.m_Bounds.xz)) * 0.5f;
						@float = math.rotate(transform2.m_Rotation, MathUtils.Center(objectGeometryData2.m_Bounds));
						@float.y = 0f;
					}
					float num4 = 0f;
					float3 position = transform2.m_Position + @float;
					if (AreaUtils.IntersectArea(position, num3, nodes, triangles) && !AreaUtils.IntersectEdges(position, num3, num4, nodes) && !AreaUtils.IntersectObjects(position, num3, num4, updateData.m_ObjectBuffer))
					{
						Entity entity2 = FindOldSubObject(prefab3, entity, ref updateData);
						SubObjectFlags subObjectFlags = (SubObjectFlags)0;
						if (!m_ElevationData.HasComponent(entity2))
						{
							subObjectFlags |= SubObjectFlags.OnGround;
						}
						if (entity2 == Entity.Null)
						{
							entity2.Index = -1;
						}
						updateData.EnsureObjectBuffer(Allocator.Temp);
						updateData.m_ObjectBuffer.Add(new AreaUtils.ObjectItem(num3 + num4, position.xz, Entity.Null));
						CreateSubObject(jobIndex, ref random, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, entity2, transform2, transform2, default(Transform), subObjectFlags, ref updateData, prefab3, cacheTransform: false, native: false, relative: false, interpolated: false, underConstruction: false, isDestroyed: false, isOverridden: false, updated: false, -1, -1, 0, 100, -1, 0);
					}
				}
				array.Dispose();
			}
			else
			{
				if (!m_SubObjects.HasBuffer(owner))
				{
					return;
				}
				DynamicBuffer<SubObject> dynamicBuffer = m_SubObjects[owner];
				NativeArray<SubObjectData> array2 = new NativeArray<SubObjectData>(dynamicBuffer.Length, Allocator.Temp);
				AreaGeometryData areaData2 = m_PrefabAreaGeometryData[prefab];
				int num5 = 0;
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity subObject2 = dynamicBuffer[k].m_SubObject;
					if (!m_SecondaryData.HasComponent(subObject2))
					{
						Entity prefab4 = m_PrefabRefData[subObject2].m_Prefab;
						ObjectGeometryData objectGeometryData3 = default(ObjectGeometryData);
						if (m_PrefabObjectGeometryData.HasComponent(prefab4))
						{
							objectGeometryData3 = m_PrefabObjectGeometryData[prefab4];
						}
						float num6 = (((objectGeometryData3.m_Flags & GeometryFlags.Circular) == 0) ? (math.length(MathUtils.Size(objectGeometryData3.m_Bounds.xz)) * 0.5f) : (objectGeometryData3.m_Size.x * 0.5f));
						if (m_BuildingData.HasComponent(subObject2))
						{
							Transform transform3 = m_TransformData[subObject2];
							float minNodeDistance2 = AreaUtils.GetMinNodeDistance(areaData2);
							updateData.EnsureObjectBuffer(Allocator.Temp);
							updateData.m_ObjectBuffer.Add(new AreaUtils.ObjectItem(num6 + minNodeDistance2, transform3.m_Position.xz, Entity.Null));
						}
						else
						{
							array2[num5++] = new SubObjectData
							{
								m_SubObject = subObject2,
								m_Radius = num6
							};
						}
					}
				}
				array2.Sort();
				for (int l = 0; l < num5; l++)
				{
					Entity entity3 = array2[l].m_SubObject;
					Entity prefab5 = m_PrefabRefData[entity3].m_Prefab;
					Transform transform4 = m_TransformData[entity3];
					ObjectGeometryData objectGeometryData4 = default(ObjectGeometryData);
					if (m_PrefabObjectGeometryData.HasComponent(prefab5))
					{
						objectGeometryData4 = m_PrefabObjectGeometryData[prefab5];
					}
					float num7;
					float3 float2;
					if ((objectGeometryData4.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
					{
						num7 = objectGeometryData4.m_Size.x * 0.5f;
						float2 = default(float3);
					}
					else
					{
						num7 = math.length(MathUtils.Size(objectGeometryData4.m_Bounds.xz)) * 0.5f;
						float2 = math.rotate(transform4.m_Rotation, MathUtils.Center(objectGeometryData4.m_Bounds));
						float2.y = 0f;
					}
					float num8 = 0f;
					float3 position2 = transform4.m_Position + float2;
					if (!AreaUtils.IntersectArea(position2, num7, nodes, triangles) || AreaUtils.IntersectEdges(position2, num7, num8, nodes) || AreaUtils.IntersectObjects(position2, num7, num8, updateData.m_ObjectBuffer))
					{
						position2 = AreaUtils.GetRandomPosition(ref random, geometry, nodes, triangles);
						if (!AreaUtils.TryFitInside(ref position2, num7, num8, area, nodes, updateData.m_ObjectBuffer))
						{
							continue;
						}
						transform4.m_Rotation = AreaUtils.GetRandomRotation(ref random, position2, nodes);
						if ((objectGeometryData4.m_Flags & GeometryFlags.Circular) == 0)
						{
							float2 = math.rotate(transform4.m_Rotation, MathUtils.Center(objectGeometryData4.m_Bounds));
							float2.y = 0f;
						}
						transform4.m_Position = position2 - float2;
					}
					SubObjectFlags subObjectFlags2 = (SubObjectFlags)0;
					if (!m_ElevationData.HasComponent(entity3))
					{
						subObjectFlags2 |= SubObjectFlags.OnGround;
					}
					updateData.EnsureObjectBuffer(Allocator.Temp);
					updateData.m_ObjectBuffer.Add(new AreaUtils.ObjectItem(num7 + num8, position2.xz, Entity.Null));
					CreateSubObject(jobIndex, ref random, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, Entity.Null, transform4, transform4, default(Transform), subObjectFlags2, ref updateData, prefab5, cacheTransform: false, native: false, relative: false, interpolated: false, underConstruction: false, isDestroyed: false, isOverridden: false, updated: false, -1, -1, 0, 100, -1, 0);
				}
				array2.Dispose();
			}
		}

		private void CreateSubObjects(int jobIndex, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random subRandom, Entity topOwner, Entity owner, Transform transform1, Transform transform2, Transform transform3, ref UpdateSubObjectsData updateData, Entity prefab, bool isTransform, bool isEdge, bool isNode, bool isTemp, Temp ownerTemp, float ownerElevation, bool native, bool relative, bool interpolated, bool isUnderConstruction, bool isDestroyed, bool isOverridden, int depth)
		{
			if (m_EditorMode && isTransform)
			{
				if (m_PrefabSubObjects.TryGetBuffer(prefab, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Game.Prefabs.SubObject subObject = bufferData[i];
						if ((subObject.m_Flags & SubObjectFlags.CoursePlacement) == 0 && isEdge == ((subObject.m_Flags & SubObjectFlags.EdgePlacement) != 0))
						{
							Transform transform4 = new Transform(subObject.m_Position, subObject.m_Rotation);
							Transform transformData = ObjectUtils.LocalToWorld(transform2, transform4);
							int alignIndex = math.select(-1, i, isEdge);
							CreateSubObject(jobIndex, ref random, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, Entity.Null, transform2, transformData, transform4, subObject.m_Flags, ref updateData, subObject.m_Prefab, cacheTransform: true, native, relative, interpolated, underConstruction: false, isDestroyed: false, isOverridden, updated: false, alignIndex, subObject.m_ParentIndex, subObject.m_GroupIndex, subObject.m_Probability, i, depth);
						}
					}
				}
				if (m_PrefabEffects.HasBuffer(prefab))
				{
					DynamicBuffer<Effect> dynamicBuffer = m_PrefabEffects[prefab];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Effect effect = dynamicBuffer[j];
						if (!effect.m_Procedural)
						{
							Transform transform5 = new Transform(effect.m_Position, effect.m_Rotation);
							Transform transformData2 = ObjectUtils.LocalToWorld(transform2, transform5);
							CreateContainerObject(jobIndex, owner, isTemp, ownerTemp, ownerElevation, Entity.Null, transformData2, transform5, ref updateData, effect.m_Effect, effect.m_Scale, effect.m_Intensity, effect.m_ParentMesh, effect.m_AnimationIndex, j);
						}
					}
				}
				if (m_PrefabActivityLocations.HasBuffer(prefab))
				{
					DynamicBuffer<ActivityLocationElement> dynamicBuffer2 = m_PrefabActivityLocations[prefab];
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						ActivityLocationElement activityLocationElement = dynamicBuffer2[k];
						Transform transform6 = new Transform(activityLocationElement.m_Position, activityLocationElement.m_Rotation);
						Transform transformData3 = ObjectUtils.LocalToWorld(transform2, transform6);
						CreateContainerObject(jobIndex, owner, isTemp, ownerTemp, ownerElevation, Entity.Null, transformData3, transform6, ref updateData, activityLocationElement.m_Prefab, 1f, 1f, 0, -1, k);
					}
				}
				return;
			}
			Unity.Mathematics.Random random2 = random;
			Unity.Mathematics.Random subRandom2 = subRandom;
			if (m_PrefabSubObjects.TryGetBuffer(prefab, out var bufferData2))
			{
				for (int l = 0; l < bufferData2.Length; l++)
				{
					Game.Prefabs.SubObject subObject2 = bufferData2[l];
					if ((subObject2.m_Flags & SubObjectFlags.CoursePlacement) != 0)
					{
						continue;
					}
					if ((subObject2.m_Flags & SubObjectFlags.EdgePlacement) != 0)
					{
						if (!isEdge)
						{
							if ((subObject2.m_Flags & SubObjectFlags.AllowCombine) == 0 || !IsContinuous(topOwner, Entity.Null))
							{
								continue;
							}
							subObject2.m_Position.z = 0f;
						}
					}
					else if (isEdge)
					{
						continue;
					}
					bool flag = true;
					bool flag2 = true;
					if ((subObject2.m_Flags & SubObjectFlags.RequireElevated) != 0)
					{
						if (!m_PrefabNetGeometryData.TryGetComponent(prefab, out var componentData))
						{
							continue;
						}
						float2 @float = componentData.m_ElevationLimit * new float2(1f, 2f);
						if (m_PrefabPlaceableNetData.TryGetComponent(prefab, out var componentData2))
						{
							@float += math.max(0f, componentData2.m_ElevationRange.min);
						}
						if (isEdge && (subObject2.m_Flags & (SubObjectFlags.EdgePlacement | SubObjectFlags.MiddlePlacement)) == SubObjectFlags.EdgePlacement)
						{
							Edge edge = m_NetEdgeData[topOwner];
							m_NetElevationData.TryGetComponent(topOwner, out var componentData3);
							m_NetElevationData.TryGetComponent(edge.m_Start, out var componentData4);
							m_NetElevationData.TryGetComponent(edge.m_End, out var componentData5);
							bool test = math.all(componentData3.m_Elevation >= @float.y) || (componentData.m_Flags & Game.Net.GeometryFlags.RequireElevated) != 0;
							flag = math.all(componentData4.m_Elevation >= math.select(@float.y, @float.x, test));
							flag2 = math.all(componentData5.m_Elevation >= math.select(@float.y, @float.x, test));
							if (!flag && !flag2)
							{
								continue;
							}
						}
						else
						{
							m_NetElevationData.TryGetComponent(topOwner, out var componentData6);
							if (!math.all(componentData6.m_Elevation >= @float.y) && (!isEdge || (componentData.m_Flags & Game.Net.GeometryFlags.RequireElevated) == 0))
							{
								continue;
							}
						}
					}
					if ((subObject2.m_Flags & SubObjectFlags.RequireOutsideConnection) != 0 && !m_OutsideConnectionData.HasComponent(topOwner))
					{
						continue;
					}
					if ((subObject2.m_Flags & SubObjectFlags.RequireDeadEnd) != 0)
					{
						if (!IsDeadEnd(topOwner, out var isEnd))
						{
							continue;
						}
						if (isEnd)
						{
							subObject2.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), subObject2.m_Rotation);
						}
					}
					if ((subObject2.m_Flags & SubObjectFlags.RequireOrphan) != 0 && !IsOrphan(topOwner))
					{
						continue;
					}
					if ((subObject2.m_Flags & (SubObjectFlags.WaterwayCrossing | SubObjectFlags.NotWaterwayCrossing)) != 0)
					{
						if (isEdge && (subObject2.m_Flags & SubObjectFlags.MiddlePlacement) == 0)
						{
							Edge edge2 = m_NetEdgeData[topOwner];
							flag &= IsWaterwayCrossing(topOwner, edge2.m_Start) == ((subObject2.m_Flags & SubObjectFlags.WaterwayCrossing) != 0);
							flag2 &= IsWaterwayCrossing(topOwner, edge2.m_End) == ((subObject2.m_Flags & SubObjectFlags.WaterwayCrossing) != 0);
							if (!flag && !flag2)
							{
								continue;
							}
						}
						else if (isEdge)
						{
							if (IsWaterwayCrossing(topOwner) != ((subObject2.m_Flags & SubObjectFlags.WaterwayCrossing) != 0))
							{
								continue;
							}
						}
						else if (isNode && IsWaterwayCrossing(Entity.Null, topOwner) != ((subObject2.m_Flags & SubObjectFlags.WaterwayCrossing) != 0))
						{
							continue;
						}
					}
					Transform transform7 = new Transform(subObject2.m_Position, subObject2.m_Rotation);
					int parentMesh = 0;
					if (isTransform)
					{
						parentMesh = subObject2.m_ParentIndex;
					}
					else if ((subObject2.m_Flags & SubObjectFlags.FixedPlacement) != 0)
					{
						int2 fixedRange = GetFixedRange(owner);
						if ((subObject2.m_Flags & SubObjectFlags.StartPlacement) != 0)
						{
							if (fixedRange.x != subObject2.m_ParentIndex || (!isEdge && fixedRange.x == fixedRange.y))
							{
								continue;
							}
							flag2 = false;
						}
						else if ((subObject2.m_Flags & SubObjectFlags.EndPlacement) != 0)
						{
							if (fixedRange.y != subObject2.m_ParentIndex || (!isEdge && fixedRange.x == fixedRange.y))
							{
								continue;
							}
							flag = false;
						}
						else if (fixedRange.x != subObject2.m_ParentIndex || (!isEdge && fixedRange.x != fixedRange.y))
						{
							continue;
						}
					}
					if (isNode && IsAbruptEnd(topOwner))
					{
						continue;
					}
					if (isEdge && (subObject2.m_Flags & SubObjectFlags.MiddlePlacement) == 0)
					{
						Edge edge3 = m_NetEdgeData[topOwner];
						if (flag)
						{
							Transform transformData4 = ObjectUtils.LocalToWorld(transform1, transform7);
							if ((subObject2.m_Flags & SubObjectFlags.AllowCombine) == 0 || !IsContinuous(edge3.m_Start, topOwner))
							{
								CreateSubObject(jobIndex, ref random, ref subRandom, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform1, transformData4, transform7, subObject2.m_Flags, ref updateData, subObject2.m_Prefab, native, relative, interpolated, isUnderConstruction, isDestroyed, isOverridden, l, parentMesh, subObject2.m_GroupIndex, subObject2.m_Probability, l, depth);
							}
						}
						if (flag2)
						{
							Transform transformData5 = ObjectUtils.LocalToWorld(transform3, transform7);
							if ((subObject2.m_Flags & SubObjectFlags.AllowCombine) == 0 || !IsContinuous(edge3.m_End, topOwner))
							{
								CreateSubObject(jobIndex, ref random, ref subRandom, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform3, transformData5, transform7, subObject2.m_Flags, ref updateData, subObject2.m_Prefab, native, relative, interpolated, isUnderConstruction, isDestroyed, isOverridden, l, parentMesh, subObject2.m_GroupIndex, subObject2.m_Probability, l, depth);
							}
						}
					}
					else if (isEdge && (subObject2.m_Flags & SubObjectFlags.EvenSpacing) != 0)
					{
						Curve curve = m_NetCurveData[topOwner];
						float num = MathUtils.Length(curve.m_Bezier.xz);
						int num2 = (int)(num / math.max(1f, subObject2.m_Position.z) - 0.5f);
						transform7.m_Position.z = 0f;
						for (int m = 0; m < num2; m++)
						{
							Bounds1 t = new Bounds1(0f, 1f);
							MathUtils.ClampLength(curve.m_Bezier.xz, ref t, (float)(m + 1) * num / (float)(num2 + 1));
							Transform transform8 = new Transform(MathUtils.Position(curve.m_Bezier, t.max), NetUtils.GetNodeRotation(MathUtils.Tangent(curve.m_Bezier, t.max)));
							Transform transformData6 = ObjectUtils.LocalToWorld(transform8, transform7);
							int alignIndex2 = m * bufferData2.Length + l;
							CreateSubObject(jobIndex, ref random, ref subRandom, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform8, transformData6, transform7, subObject2.m_Flags, ref updateData, subObject2.m_Prefab, native, relative, interpolated, isUnderConstruction, isDestroyed, isOverridden, alignIndex2, parentMesh, subObject2.m_GroupIndex, subObject2.m_Probability, l, depth);
						}
					}
					else
					{
						Transform transformData7 = ObjectUtils.LocalToWorld(transform2, transform7);
						int alignIndex3 = math.select(-1, l, isEdge || isNode);
						CreateSubObject(jobIndex, ref random, ref subRandom, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform2, transformData7, transform7, subObject2.m_Flags, ref updateData, subObject2.m_Prefab, native, relative, interpolated, isUnderConstruction, isDestroyed, isOverridden, alignIndex3, parentMesh, subObject2.m_GroupIndex, subObject2.m_Probability, l, depth);
					}
				}
			}
			if (isUnderConstruction && m_PrefabBuildingData.HasComponent(prefab))
			{
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefab];
				Transform transform9 = new Transform
				{
					m_Rotation = quaternion.identity
				};
				if (m_PrefabSubMeshes.TryGetBuffer(prefab, out var bufferData3) && bufferData3.Length != 0)
				{
					random2.NextInt();
					SubMesh subMesh = bufferData3[subRandom2.NextInt(bufferData3.Length)];
					transform9.m_Position = subMesh.m_Position;
					transform9.m_Rotation = subMesh.m_Rotation;
				}
				transform9.m_Position.y += math.max(objectGeometryData.m_Bounds.max.y, 15f);
				transform9.m_Position.y += math.csum(math.frac(transform2.m_Position.xz / 60f)) * 5f;
				Transform transformData8 = ObjectUtils.LocalToWorld(transform2, transform9);
				CreateSubObject(jobIndex, ref random2, ref subRandom2, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform2, transformData8, transform9, (SubObjectFlags)0, ref updateData, m_BuildingConfigurationData.m_ConstructionObject, native, relative, interpolated, underConstruction: false, isDestroyed, isOverridden, -1, 0, 0, 100, -1, depth);
			}
			if (!isDestroyed || !m_PrefabObjectGeometryData.TryGetComponent(prefab, out var componentData7) || (componentData7.m_Flags & (GeometryFlags.Physical | GeometryFlags.HasLot)) != (GeometryFlags.Physical | GeometryFlags.HasLot) || !m_PrefabSubMeshes.TryGetBuffer(prefab, out var bufferData4))
			{
				return;
			}
			int num3 = 0;
			for (int n = 0; n < bufferData4.Length; n++)
			{
				SubMesh subMesh2 = bufferData4[n];
				if (!m_PrefabMeshData.TryGetComponent(subMesh2.m_SubMesh, out var componentData8))
				{
					continue;
				}
				float2 float2 = MathUtils.Center(componentData8.m_Bounds.xz);
				float2 float3 = MathUtils.Size(componentData8.m_Bounds.xz);
				int2 @int = math.max(1, (int2)math.sqrt(float3));
				float2 float4 = float3 / @int;
				float3 float5 = math.rotate(subMesh2.m_Rotation, new float3(float4.x, 0f, 0f));
				float3 float6 = math.rotate(subMesh2.m_Rotation, new float3(0f, 0f, float4.y));
				float3 float7 = subMesh2.m_Position + math.rotate(subMesh2.m_Rotation, new float3(float2.x, 0f, float2.y));
				float7 -= float5 * ((float)@int.x * 0.5f - 0.5f) + float6 * ((float)@int.y * 0.5f - 0.5f);
				for (int num4 = 0; num4 < @int.y; num4++)
				{
					for (int num5 = 0; num5 < @int.x; num5++)
					{
						float2 float8 = new float2(num5, num4) + random.NextFloat2(-0.5f, 0.5f);
						Transform transform10 = new Transform
						{
							m_Position = float7 + float5 * float8.x + float6 * float8.y,
							m_Rotation = quaternion.RotateY(subRandom2.NextFloat(-MathF.PI, MathF.PI))
						};
						random2.NextFloat();
						Transform transformData9 = ObjectUtils.LocalToWorld(transform2, transform10);
						CreateSubObject(jobIndex, ref random2, ref subRandom2, topOwner, owner, prefab, isTemp, ownerTemp, ownerElevation, transform2, transformData9, transform10, SubObjectFlags.OnGround, ref updateData, m_BuildingConfigurationData.m_CollapsedObject, native, relative, interpolated, underConstruction: false, destroyed: false, isOverridden, -1, 0, num3++, 100, -1, depth);
					}
				}
			}
		}

		private bool GetPropID(Entity owner, Entity prefab, TransformFrame transformFrame, ActivityCondition conditions, DynamicBuffer<MeshGroup> meshGroups, out AnimatedPropID propID)
		{
			if (transformFrame.m_Activity == 10 || transformFrame.m_Activity == 11 || transformFrame.m_Activity == 1)
			{
				propID = AnimatedPropID.None;
				return false;
			}
			m_PseudoRandomSeedData.TryGetComponent(owner, out var componentData);
			ObjectUtils.GetStateDuration(prefab, transformFrame.m_State, transformFrame.m_Activity, componentData, AnimatedPropID.Any, conditions, meshGroups, ref m_PrefabSubMeshGroups, ref m_PrefabCharacterElements, ref m_PrefabSubMeshes, ref m_PrefabAnimationClips, out var _, out var animationClip, out var _);
			propID = animationClip.m_PropID;
			return propID != AnimatedPropID.None;
		}

		private int2 GetFixedRange(Entity owner)
		{
			if (m_Edges.HasBuffer(owner))
			{
				PrefabRef prefabRef = m_PrefabRefData[owner];
				int2 result = new int2(int.MaxValue, int.MinValue);
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					if (prefabRef.m_Prefab == prefabRef2.m_Prefab && m_FixedData.HasComponent(value.m_Edge))
					{
						Fixed obj = m_FixedData[value.m_Edge];
						if (value.m_End)
						{
							result.y = math.max(result.y, obj.m_Index);
						}
						else
						{
							result.x = math.min(result.x, obj.m_Index);
						}
					}
				}
				return result;
			}
			if (m_FixedData.HasComponent(owner))
			{
				Fixed obj2 = m_FixedData[owner];
				return new int2(obj2.m_Index, obj2.m_Index);
			}
			return new int2(int.MaxValue, int.MinValue);
		}

		private bool IsDeadEnd(Entity owner, out bool isEnd)
		{
			PrefabRef prefabRef = m_PrefabRefData[owner];
			isEnd = false;
			if (m_Edges.HasBuffer(owner) && m_PrefabNetGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
				int num = 0;
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					if ((m_PrefabNetGeometryData[prefabRef2.m_Prefab].m_MergeLayers & netGeometryData.m_MergeLayers) != Layer.None)
					{
						isEnd = value.m_End;
						num++;
					}
				}
				return num <= 1;
			}
			return false;
		}

		private bool IsAbruptEnd(Entity owner)
		{
			if (m_Edges.HasBuffer(owner))
			{
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					if (m_UpgradedData.TryGetComponent(value.m_Edge, out var componentData) && ((value.m_End ? componentData.m_Flags.m_Right : componentData.m_Flags.m_Left) & CompositionFlags.Side.AbruptEnd) != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsOrphan(Entity owner)
		{
			PrefabRef prefabRef = m_PrefabRefData[owner];
			if (m_Edges.HasBuffer(owner) && m_PrefabNetGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					if ((m_PrefabNetGeometryData[prefabRef2.m_Prefab].m_MergeLayers & netGeometryData.m_MergeLayers) != Layer.None)
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool IsWaterwayCrossing(Entity edge, Entity node)
		{
			if (m_Edges.HasBuffer(node) && m_PrefabRefData.TryGetComponent(node, out var componentData) && m_PrefabNetGeometryData.HasComponent(componentData.m_Prefab))
			{
				int num = 0;
				int num2 = 0;
				EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef = m_PrefabRefData[value.m_Edge];
					if ((m_PrefabNetGeometryData[prefabRef.m_Prefab].m_MergeLayers & Layer.Waterway) != Layer.None)
					{
						num++;
					}
					else
					{
						num2++;
					}
				}
				if (num >= 1)
				{
					return num2 >= 2;
				}
				return false;
			}
			return false;
		}

		private bool IsWaterwayCrossing(Entity edge)
		{
			if (m_NetEdgeData.TryGetComponent(edge, out var componentData))
			{
				if (!IsWaterwayCrossing(edge, componentData.m_Start))
				{
					return IsWaterwayCrossing(edge, componentData.m_End);
				}
				return true;
			}
			return false;
		}

		private bool IsContinuous(Entity node, Entity edge)
		{
			PrefabRef prefabRef = m_PrefabRefData[node];
			if (m_Edges.HasBuffer(node) && m_PrefabNetGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
				int num = 0;
				Curve curve = default(Curve);
				EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					NetGeometryData netGeometryData2 = m_PrefabNetGeometryData[prefabRef2.m_Prefab];
					if ((netGeometryData2.m_MergeLayers & netGeometryData.m_MergeLayers) != Layer.None)
					{
						if (prefabRef2.m_Prefab != prefabRef.m_Prefab)
						{
							return false;
						}
						if (++num == 1)
						{
							curve = m_NetCurveData[value.m_Edge];
							if (value.m_End)
							{
								curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
							}
							continue;
						}
						Curve curve2 = m_NetCurveData[value.m_Edge];
						if (value.m_End)
						{
							curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
						}
						float3 value2 = MathUtils.StartTangent(curve.m_Bezier);
						float3 value3 = MathUtils.StartTangent(curve2.m_Bezier);
						if (MathUtils.TryNormalize(ref value2) && MathUtils.TryNormalize(ref value3))
						{
							if (math.dot(value2, value3) > -0.99f)
							{
								return false;
							}
							float3 @float = (value2 - value3) * 0.5f;
							float3 x = curve2.m_Bezier.a - curve.m_Bezier.a;
							x -= @float * math.dot(x, @float);
							if (math.lengthsq(x) > 0.01f)
							{
								return false;
							}
						}
					}
					else if ((netGeometryData2.m_IntersectLayers & netGeometryData.m_IntersectLayers) != Layer.None)
					{
						return false;
					}
				}
				return num == 2;
			}
			return false;
		}

		private bool CheckRequirements(Entity prefab, int groupIndex, bool isExplicit, ref UpdateSubObjectsData updateData)
		{
			if (m_ObjectRequirements.TryGetBuffer(prefab, out var bufferData))
			{
				int num = -1;
				bool flag = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ObjectRequirementElement objectRequirementElement = bufferData[i];
					if ((objectRequirementElement.m_Type & ObjectRequirementType.SelectOnly) != 0)
					{
						continue;
					}
					if (objectRequirementElement.m_Group != num)
					{
						if (!flag)
						{
							break;
						}
						num = objectRequirementElement.m_Group;
						flag = false;
					}
					if (objectRequirementElement.m_Requirement != Entity.Null)
					{
						if (updateData.m_PlaceholderRequirements.TryGetValue(objectRequirementElement.m_Requirement, out var item))
						{
							if (item.y == 0)
							{
								flag = true;
								continue;
							}
							int num2 = groupIndex % item.y;
							num2 = math.select(num2, -item.y, num2 == 0 && groupIndex < 0);
							flag |= num2 == item.x;
						}
						else if (isExplicit && (objectRequirementElement.m_Type & ObjectRequirementType.IgnoreExplicit) != 0)
						{
							flag = true;
						}
					}
					else
					{
						flag |= (updateData.m_PlaceholderRequirementFlags & (objectRequirementElement.m_RequireFlags | objectRequirementElement.m_ForbidFlags)) == objectRequirementElement.m_RequireFlags;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		private void CreateSubObject(int jobIndex, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random subRandom, Entity topOwner, Entity owner, Entity ownerPrefab, bool isTemp, Temp ownerTemp, float ownerElevation, Transform ownerTransform, Transform transformData, Transform localTransformData, SubObjectFlags flags, ref UpdateSubObjectsData updateData, Entity prefab, bool native, bool relative, bool interpolated, bool underConstruction, bool destroyed, bool overridden, int alignIndex, int parentMesh, int groupIndex, int probability, int prefabSubIndex, int depth)
		{
			if (!m_PrefabPlaceholderObjectData.TryGetComponent(prefab, out var componentData) || !m_PlaceholderObjects.TryGetBuffer(prefab, out var bufferData))
			{
				Entity groupPrefab = prefab;
				if (m_PrefabSpawnableObjectData.TryGetComponent(prefab, out var componentData2) && componentData2.m_RandomizationGroup != Entity.Null)
				{
					groupPrefab = componentData2.m_RandomizationGroup;
				}
				if (CheckRequirements(prefab, groupIndex, isExplicit: true, ref updateData))
				{
					Unity.Mathematics.Random random2 = random;
					random.NextInt();
					random.NextInt();
					subRandom.NextInt();
					subRandom.NextInt();
					if (updateData.m_SelectedSpawnabled.IsCreated && updateData.m_SelectedSpawnabled.TryGetValue(new PlaceholderKey(groupPrefab, groupIndex), out var item))
					{
						random2 = item;
					}
					else
					{
						updateData.EnsureSelectedSpawnables(Allocator.Temp);
						updateData.m_SelectedSpawnabled.TryAdd(new PlaceholderKey(groupPrefab, groupIndex), random2);
					}
					if (random2.NextInt(100) < probability)
					{
						CreateSubObject(jobIndex, ref random2, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, ref updateData, prefab, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated: false, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
					}
				}
				return;
			}
			float num = 0f;
			bool updated = false;
			bool updated2 = false;
			float num2 = -1f;
			float num3 = -1f;
			float num4 = -1f;
			Entity prefab2 = Entity.Null;
			Entity prefab3 = Entity.Null;
			Entity prefab4 = Entity.Null;
			Entity groupPrefab2 = Entity.Null;
			Entity groupPrefab3 = Entity.Null;
			Entity groupPrefab4 = Entity.Null;
			Unity.Mathematics.Random random3 = default(Unity.Mathematics.Random);
			Unity.Mathematics.Random random4 = default(Unity.Mathematics.Random);
			Unity.Mathematics.Random random5 = default(Unity.Mathematics.Random);
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			if (componentData.m_RandomizeGroupIndex)
			{
				random.NextInt();
				int num8 = subRandom.NextInt() & 0x7FFFFFFF;
				groupIndex = math.select(num8, -1 - num8, groupIndex < 0);
			}
			AnimatedPropID propID = AnimatedPropID.Any;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity entity = bufferData[i].m_Object;
				if (!CheckRequirements(entity, groupIndex, isExplicit: false, ref updateData))
				{
					continue;
				}
				float num9 = 0f;
				float num10 = 0f;
				int num11 = 0;
				bool flag = false;
				if (m_PrefabPillarData.TryGetComponent(entity, out var componentData3))
				{
					switch (componentData3.m_Type)
					{
					case PillarType.Horizontal:
					{
						num11 = 1;
						flag = true;
						if (m_PrefabNetGeometryData.TryGetComponent(ownerPrefab, out var componentData8) && m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData9))
						{
							float num16 = componentData8.m_ElevatedWidth - 1f;
							float max = componentData3.m_OffsetRange.max;
							num10 += 1f / (1f + math.abs(componentData9.m_Size.x - num16));
							num10 += 0.01f / (1f + math.max(0f, max));
						}
						break;
					}
					case PillarType.Vertical:
					{
						flag = true;
						if (m_PrefabNetGeometryData.TryGetComponent(ownerPrefab, out var componentData10) && m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData11))
						{
							if (m_PrefabPlaceableObjectData.TryGetComponent(entity, out var componentData12))
							{
								componentData11.m_Size.y -= componentData12.m_PlacementOffset.y;
							}
							float num17 = ownerElevation + transformData.m_Position.y - ownerTransform.m_Position.y;
							float num18 = componentData11.m_Size.y - num17;
							num10 += 1f / (1f + math.select(2f * num18, 0f - num18, num18 < 0f));
							num9 = math.max(0f, componentData10.m_ElevatedWidth * 0.5f - componentData11.m_Size.x * 0.5f);
						}
						break;
					}
					case PillarType.Standalone:
					{
						if (m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData6))
						{
							if (m_PrefabPlaceableObjectData.TryGetComponent(entity, out var componentData7))
							{
								componentData6.m_Size.y -= componentData7.m_PlacementOffset.y;
							}
							float num14 = ownerElevation + transformData.m_Position.y - ownerTransform.m_Position.y;
							float num15 = componentData6.m_Size.y - num14;
							num10 += 1f / (1f + math.select(2f * num15, 0f - num15, num15 < 0f));
						}
						break;
					}
					case PillarType.Base:
					{
						num11 = 2;
						if (m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData4))
						{
							if (m_PrefabPlaceableObjectData.TryGetComponent(entity, out var componentData5))
							{
								componentData4.m_Size.y -= componentData5.m_PlacementOffset.y;
							}
							float num12 = ownerElevation + transformData.m_Position.y - ownerTransform.m_Position.y;
							float num13 = componentData4.m_Size.y - num12;
							num10 += 1f / (1f + math.select(2f * num13, 0f - num13, num13 < 0f));
						}
						break;
					}
					}
				}
				if (m_PrefabQuantityObjectData.TryGetComponent(entity, out var componentData13))
				{
					if ((componentData13.m_Resources & updateData.m_StoredResources) != Resource.NoResource)
					{
						num10 += 1f;
						componentData13.m_Resources = Resource.NoResource;
					}
					if (componentData13.m_Resources != Resource.NoResource && m_DeliveryTruckData.TryGetComponent(topOwner, out var componentData14) && (componentData13.m_Resources & componentData14.m_Resource) != Resource.NoResource)
					{
						num10 += 1f;
						componentData13.m_Resources = Resource.NoResource;
					}
					if ((componentData13.m_Resources & Resource.LocalMail) != Resource.NoResource && m_MailProducerData.HasComponent(topOwner))
					{
						num10 += 0.9f;
						componentData13.m_Resources = Resource.NoResource;
					}
					if ((componentData13.m_Resources & Resource.Garbage) != Resource.NoResource && m_GarbageProducerData.HasComponent(topOwner))
					{
						num10 += 0.9f;
						componentData13.m_Resources = Resource.NoResource;
					}
					if (componentData13.m_Resources != Resource.NoResource && m_Resources.HasBuffer(topOwner))
					{
						PrefabRef prefabRef = m_PrefabRefData[topOwner];
						Resource resource = componentData13.m_Resources;
						if (m_PrefabCargoTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out var componentData15))
						{
							resource &= componentData15.m_Resources;
						}
						if (resource != Resource.NoResource)
						{
							DynamicBuffer<Resources> dynamicBuffer = m_Resources[topOwner];
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								if ((dynamicBuffer[j].m_Resource & resource) != Resource.NoResource)
								{
									num10 += 1f;
									componentData13.m_Resources = Resource.NoResource;
									break;
								}
							}
						}
					}
					if (componentData13.m_MapFeature != MapFeature.None)
					{
						PrefabRef prefabRef2 = m_PrefabRefData[topOwner];
						if (m_PrefabWorkVehicleData.TryGetComponent(prefabRef2.m_Prefab, out var componentData16) && componentData13.m_MapFeature == componentData16.m_MapFeature)
						{
							num10 += 1f;
							componentData13.m_MapFeature = MapFeature.None;
						}
					}
					if (componentData13.m_Resources != Resource.NoResource)
					{
						PrefabRef prefabRef3 = m_PrefabRefData[topOwner];
						if (m_PrefabWorkVehicleData.TryGetComponent(prefabRef3.m_Prefab, out var componentData17) && (componentData13.m_Resources & componentData17.m_Resources) != Resource.NoResource)
						{
							num10 += 1f;
							componentData13.m_Resources = Resource.NoResource;
						}
					}
					if (componentData13.m_Resources != Resource.NoResource || componentData13.m_MapFeature != MapFeature.None)
					{
						continue;
					}
				}
				if (m_ActivityPropData.TryGetComponent(entity, out var componentData18))
				{
					if (propID == AnimatedPropID.Any)
					{
						propID = AnimatedPropID.None;
						if (relative && !m_RelativeData.HasComponent(owner) && !m_UpdatedData.HasComponent(owner) && m_TransformFrames.TryGetBuffer(owner, out var bufferData2))
						{
							ActivityCondition conditions = (ActivityCondition)0u;
							if (m_HumanData.TryGetComponent(owner, out var componentData19))
							{
								conditions = CreatureUtils.GetConditions(componentData19);
							}
							m_MeshGroups.TryGetBuffer(owner, out var bufferData3);
							TransformState transformState = TransformState.Default;
							byte b = 0;
							for (int k = 0; k < bufferData2.Length; k++)
							{
								TransformFrame transformFrame = bufferData2[k];
								if (transformFrame.m_State != transformState || transformFrame.m_Activity != b)
								{
									transformState = transformFrame.m_State;
									b = transformFrame.m_Activity;
									if (GetPropID(owner, ownerPrefab, transformFrame, conditions, bufferData3, out propID))
									{
										break;
									}
								}
							}
						}
					}
					if (componentData18.m_AnimatedPropID != propID)
					{
						continue;
					}
				}
				SpawnableObjectData spawnableObjectData = m_PrefabSpawnableObjectData[entity];
				Entity entity2 = ((spawnableObjectData.m_RandomizationGroup != Entity.Null) ? spawnableObjectData.m_RandomizationGroup : entity);
				Unity.Mathematics.Random random6 = random;
				random.NextInt();
				random.NextInt();
				subRandom.NextInt();
				subRandom.NextInt();
				if (updateData.m_SelectedSpawnabled.IsCreated && updateData.m_SelectedSpawnabled.TryGetValue(new PlaceholderKey(entity2, groupIndex), out var item2))
				{
					num10 += 0.5f;
					random6 = item2;
				}
				switch (num11)
				{
				case 0:
					if (num10 > num2)
					{
						num = num9;
						updated = flag;
						num2 = num10;
						prefab2 = entity;
						groupPrefab2 = entity2;
						random3 = random6;
						num5 = spawnableObjectData.m_Probability;
					}
					else if (num10 == num2)
					{
						int probability4 = spawnableObjectData.m_Probability;
						num5 += probability4;
						subRandom.NextInt();
						if (random.NextInt(num5) < probability4)
						{
							num = num9;
							updated = flag;
							prefab2 = entity;
							groupPrefab2 = entity2;
							random3 = random6;
						}
					}
					break;
				case 1:
					if (num10 > num3)
					{
						updated2 = flag;
						num3 = num10;
						prefab3 = entity;
						groupPrefab3 = entity2;
						random4 = random6;
						num6 = spawnableObjectData.m_Probability;
					}
					else if (num10 == num3)
					{
						int probability3 = spawnableObjectData.m_Probability;
						num6 += probability3;
						subRandom.NextInt();
						if (random.NextInt(num6) < probability3)
						{
							updated2 = flag;
							prefab3 = entity;
							groupPrefab3 = entity2;
							random4 = random6;
						}
					}
					break;
				case 2:
					if (num10 > num4)
					{
						num4 = num10;
						prefab4 = entity;
						groupPrefab4 = entity2;
						random5 = random6;
						num7 = spawnableObjectData.m_Probability;
					}
					else if (num10 == num4)
					{
						int probability2 = spawnableObjectData.m_Probability;
						num7 += probability2;
						subRandom.NextInt();
						if (random.NextInt(num7) < probability2)
						{
							prefab4 = entity;
							groupPrefab4 = entity2;
							random5 = random6;
						}
					}
					break;
				}
			}
			if (num5 > 0)
			{
				updateData.EnsureSelectedSpawnables(Allocator.Temp);
				updateData.m_SelectedSpawnabled.TryAdd(new PlaceholderKey(groupPrefab2, groupIndex), random3);
				if (random3.NextInt(100) < probability)
				{
					if (num != 0f)
					{
						Transform transform = localTransformData;
						Transform transform2 = localTransformData;
						transform.m_Position.x -= num;
						transform2.m_Position.x += num;
						Transform transformData2 = ObjectUtils.LocalToWorld(ownerTransform, transform);
						Transform transformData3 = ObjectUtils.LocalToWorld(ownerTransform, transform2);
						Unity.Mathematics.Random random7 = random3;
						CreateSubObject(jobIndex, ref random3, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData2, transform, flags, ref updateData, prefab2, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
						CreateSubObject(jobIndex, ref random7, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData3, transform2, flags, ref updateData, prefab2, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
					}
					else
					{
						CreateSubObject(jobIndex, ref random3, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, ref updateData, prefab2, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
					}
				}
			}
			if (num6 > 0)
			{
				updateData.EnsureSelectedSpawnables(Allocator.Temp);
				updateData.m_SelectedSpawnabled.TryAdd(new PlaceholderKey(groupPrefab3, groupIndex), random4);
				if (random4.NextInt(100) < probability)
				{
					CreateSubObject(jobIndex, ref random4, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, ref updateData, prefab3, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated2, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
				}
			}
			if (num7 > 0)
			{
				updateData.EnsureSelectedSpawnables(Allocator.Temp);
				updateData.m_SelectedSpawnabled.TryAdd(new PlaceholderKey(groupPrefab4, groupIndex), random5);
				if (random5.NextInt(100) < probability)
				{
					CreateSubObject(jobIndex, ref random5, topOwner, owner, ownerPrefab, isTemp, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, ref updateData, prefab4, cacheTransform: false, native, relative, interpolated, underConstruction, destroyed, overridden, updated: false, alignIndex, parentMesh, groupIndex, probability, prefabSubIndex, depth);
				}
			}
		}

		private void CreateSubObject(int jobIndex, ref Unity.Mathematics.Random random, Entity topOwner, Entity owner, Entity ownerPrefab, bool isTemp, Temp ownerTemp, float ownerElevation, Entity oldSubObject, Transform ownerTransform, Transform transformData, Transform localTransformData, SubObjectFlags flags, ref UpdateSubObjectsData updateData, Entity prefab, bool cacheTransform, bool native, bool relative, bool interpolated, bool underConstruction, bool isDestroyed, bool isOverridden, bool updated, int alignIndex, int parentMesh, int groupIndex, int probability, int prefabSubIndex, int depth)
		{
			ObjectGeometryData componentData;
			bool flag = m_PrefabObjectGeometryData.TryGetComponent(prefab, out componentData);
			bool flag2 = m_PrefabData.IsComponentEnabled(prefab);
			if (alignIndex >= 0 && m_PrefabPillarData.TryGetComponent(prefab, out var componentData2))
			{
				switch (componentData2.m_Type)
				{
				case PillarType.Vertical:
					flags |= SubObjectFlags.AnchorTop;
					flags |= SubObjectFlags.OnGround;
					break;
				case PillarType.Standalone:
					flags |= SubObjectFlags.AnchorTop;
					flags |= SubObjectFlags.OnGround;
					break;
				case PillarType.Base:
					flags |= SubObjectFlags.OnGround;
					break;
				}
			}
			m_PrefabPlaceableObjectData.TryGetComponent(prefab, out var componentData3);
			if ((flags & SubObjectFlags.AnchorTop) != 0)
			{
				componentData.m_Bounds.max.y -= componentData3.m_PlacementOffset.y;
				transformData.m_Position.y -= componentData.m_Bounds.max.y;
				localTransformData.m_Position.y -= componentData.m_Bounds.max.y;
			}
			else if ((flags & SubObjectFlags.AnchorCenter) != 0)
			{
				float num = (componentData.m_Bounds.max.y - componentData.m_Bounds.min.y) * 0.5f;
				transformData.m_Position.y -= num;
				localTransformData.m_Position.y -= num;
			}
			PseudoRandomSeed pseudoRandomSeed = default(PseudoRandomSeed);
			if (flag)
			{
				pseudoRandomSeed = new PseudoRandomSeed(ref random);
			}
			if (underConstruction && flag && (componentData.m_Flags & GeometryFlags.Marker) == 0 && !m_PrefabBuildingExtensionData.HasComponent(prefab) && !m_PrefabSubLanes.HasBuffer(prefab) && !m_PrefabSpawnLocationData.HasComponent(prefab))
			{
				return;
			}
			Elevation elevation = new Elevation(ownerElevation, (math.abs(parentMesh) >= 1000) ? ElevationFlags.Stacked : ((ElevationFlags)0));
			if ((flags & SubObjectFlags.OnAttachedParent) != 0)
			{
				elevation.m_Flags |= ElevationFlags.OnAttachedParent;
				if (m_AttachedData.TryGetComponent(owner, out var componentData4) && m_EdgeGeometryData.TryGetComponent(componentData4.m_Parent, out var componentData5))
				{
					transformData.m_Position.y = ObjectUtils.GetAttachedParentHeight(componentData5, transformData);
				}
			}
			else if ((flags & SubObjectFlags.OnGround) == 0)
			{
				elevation.m_Elevation += localTransformData.m_Position.y;
				if (ownerElevation >= 0f && elevation.m_Elevation >= -0.5f && elevation.m_Elevation < 0f)
				{
					elevation.m_Elevation = 0f;
				}
				if (parentMesh < 0)
				{
					elevation.m_Flags |= ElevationFlags.OnGround;
				}
			}
			else
			{
				if ((flags & (SubObjectFlags.AnchorTop | SubObjectFlags.AnchorCenter)) == 0)
				{
					transformData.m_Position.y = ownerTransform.m_Position.y - ownerElevation;
					localTransformData.m_Position.y = 0f - ownerElevation;
				}
				elevation.m_Elevation = 0f;
				elevation.m_Flags |= ElevationFlags.OnGround;
			}
			if ((elevation.m_Flags & ElevationFlags.OnGround) != 0)
			{
				bool flag3 = true;
				if (flag)
				{
					flag3 = (componentData.m_Flags & GeometryFlags.DeleteOverridden) == 0 && (componentData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.Marker | GeometryFlags.Brushable)) != 0;
				}
				if (flag3)
				{
					bool angledSample;
					Transform transform = ObjectUtils.AdjustPosition(transformData, ref elevation, prefab, out angledSample, ref m_TerrainHeightData, ref m_WaterSurfaceData, ref m_PrefabPlaceableObjectData, ref m_PrefabObjectGeometryData);
					if (math.abs(transform.m_Position.y - transformData.m_Position.y) >= 0.01f || (angledSample && MathUtils.RotationAngle(transform.m_Rotation, transformData.m_Rotation) >= math.radians(0.1f)))
					{
						transformData = transform;
					}
				}
			}
			if ((isDestroyed && (elevation.m_Flags & (ElevationFlags.Stacked | ElevationFlags.OnGround)) != ElevationFlags.OnGround && !m_PrefabBuildingExtensionData.HasComponent(prefab) && !m_TransportStopData.HasComponent(prefab)) || ClearAreaHelpers.ShouldClear(updateData.m_ClearAreas, transformData.m_Position, (flags & SubObjectFlags.OnGround) != 0))
			{
				return;
			}
			if (oldSubObject == Entity.Null)
			{
				oldSubObject = FindOldSubObject(prefab, transformData, alignIndex, ref updateData);
			}
			else if (oldSubObject.Index < 0)
			{
				oldSubObject = Entity.Null;
			}
			if ((componentData3.m_Flags & PlacementFlags.Swaying) != PlacementFlags.None)
			{
				relative = false;
			}
			int3 boneIndex = new int3(0, -1, -1);
			if (!m_EditorMode)
			{
				int num2 = parentMesh % 1000;
				if (num2 > 0)
				{
					boneIndex.yz = RenderingUtils.FindBoneIndex(ownerPrefab, ref localTransformData.m_Position, ref localTransformData.m_Rotation, num2, ref m_PrefabSubMeshes, ref m_PrefabProceduralBones);
					boneIndex.x = math.select(0, num2, boneIndex.y >= 0);
				}
			}
			if (oldSubObject != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldSubObject);
				Temp temp = default(Temp);
				if (isTemp)
				{
					if (m_TempData.HasComponent(oldSubObject))
					{
						temp = m_TempData[oldSubObject];
						temp.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Dragging | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
						if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
						{
							temp.m_Flags |= TempFlags.Modify;
						}
						temp.m_Original = FindOriginalSubObject(prefab, temp.m_Original, transformData, alignIndex, ref updateData);
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, temp);
						if (temp.m_Original != Entity.Null && flag2 && m_PrefabTreeData.HasComponent(prefab) && m_TreeData.TryGetComponent(temp.m_Original, out var componentData6))
						{
							m_CommandBuffer.SetComponent(jobIndex, oldSubObject, componentData6);
						}
					}
					if (m_PrefabObjectGeometryData.HasComponent(prefab))
					{
						interpolated = true;
					}
					if ((componentData3.m_Flags & PlacementFlags.Attached) != PlacementFlags.None)
					{
						m_AttachedData.TryGetComponent(oldSubObject, out var componentData7);
						componentData7.m_OldParent = componentData7.m_Parent;
						componentData7.m_Parent = Entity.Null;
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, componentData7);
					}
					m_CommandBuffer.SetComponent(jobIndex, oldSubObject, transformData);
					updated = true;
				}
				else if (!transformData.Equals(m_TransformData[oldSubObject]))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldSubObject, transformData);
					updated = true;
				}
				if (cacheTransform)
				{
					LocalTransformCache component = default(LocalTransformCache);
					component.m_Position = localTransformData.m_Position;
					component.m_Rotation = localTransformData.m_Rotation;
					component.m_ParentMesh = parentMesh;
					component.m_GroupIndex = groupIndex;
					component.m_Probability = probability;
					component.m_PrefabSubIndex = prefabSubIndex;
					if (m_LocalTransformCacheData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, component);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, component);
					}
				}
				else if (m_LocalTransformCacheData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.RemoveComponent<LocalTransformCache>(jobIndex, oldSubObject);
				}
				PseudoRandomSeed componentData8 = default(PseudoRandomSeed);
				if (flag)
				{
					if (m_PseudoRandomSeedData.TryGetComponent(temp.m_Original, out componentData8))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, componentData8);
					}
					else if (!m_PseudoRandomSeedData.TryGetComponent(oldSubObject, out componentData8))
					{
						componentData8 = pseudoRandomSeed;
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, componentData8);
					}
				}
				if ((flags & SubObjectFlags.OnGround) == 0)
				{
					if (m_ElevationData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, elevation);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, elevation);
					}
				}
				else if (m_ElevationData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.RemoveComponent<Elevation>(jobIndex, oldSubObject);
				}
				if (alignIndex >= 0)
				{
					if (m_AlignedData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, new Aligned((ushort)alignIndex));
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, new Aligned((ushort)alignIndex));
					}
				}
				else if (m_AlignedData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.RemoveComponent<Aligned>(jobIndex, oldSubObject);
				}
				if (m_RelativeData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldSubObject, new Relative(localTransformData, boneIndex));
				}
				Destroyed componentData9;
				if (interpolated || boneIndex.y >= 0)
				{
					if (m_InterpolatedTransformData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, new InterpolatedTransform(transformData));
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, new InterpolatedTransform(transformData));
						updated = true;
					}
				}
				else if (m_InterpolatedTransformData.HasComponent(oldSubObject) && (!m_PrefabBuildingExtensionData.HasComponent(prefab) || !m_DestroyedData.TryGetComponent(oldSubObject, out componentData9) || componentData9.m_Cleared >= 0f))
				{
					m_CommandBuffer.RemoveComponent<InterpolatedTransform>(jobIndex, oldSubObject);
					updated = true;
				}
				UnderConstruction componentData10 = default(UnderConstruction);
				if (temp.m_Original != Entity.Null)
				{
					underConstruction = m_UnderConstructionData.TryGetComponent(temp.m_Original, out componentData10);
				}
				if (underConstruction)
				{
					if (!m_UnderConstructionData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, componentData10);
						updated = true;
					}
				}
				else if (m_UnderConstructionData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.RemoveComponent<UnderConstruction>(jobIndex, oldSubObject);
					updated = true;
				}
				Destroyed componentData11 = default(Destroyed);
				if (temp.m_Original != Entity.Null && (ownerTemp.m_Flags & TempFlags.Upgrade) == 0)
				{
					isDestroyed = m_DestroyedData.TryGetComponent(temp.m_Original, out componentData11);
				}
				if (isDestroyed)
				{
					if (!m_DestroyedData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, componentData11);
						updated = true;
					}
				}
				else if (m_DestroyedData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.RemoveComponent<Destroyed>(jobIndex, oldSubObject);
					updated = true;
				}
				if (m_OverriddenData.HasComponent(oldSubObject))
				{
					isOverridden = true;
				}
				else if (isOverridden)
				{
					m_CommandBuffer.AddComponent(jobIndex, oldSubObject, default(Overridden));
					updated = true;
				}
				if (updated && !m_UpdatedData.HasComponent(oldSubObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, oldSubObject, default(Updated));
				}
				if (m_PrefabStreetLightData.HasComponent(prefab))
				{
					StreetLight streetLight = default(StreetLight);
					bool flag4 = false;
					if (m_StreetLightData.TryGetComponent(oldSubObject, out var componentData12))
					{
						streetLight = componentData12;
						flag4 = true;
					}
					Watercraft componentData14;
					if (m_BuildingData.TryGetComponent(topOwner, out var componentData13))
					{
						StreetLightSystem.UpdateStreetLightState(ref streetLight, componentData13);
					}
					else if (m_WatercraftData.TryGetComponent(topOwner, out componentData14))
					{
						StreetLightSystem.UpdateStreetLightState(ref streetLight, componentData14);
					}
					if (flag4)
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, streetLight);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, streetLight);
					}
				}
				if (flag2 && m_PrefabStackData.TryGetComponent(prefab, out var componentData15))
				{
					if (m_StackData.TryGetComponent(temp.m_Original, out var componentData16))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, componentData16);
					}
					else if (updated || !m_StackData.HasComponent(oldSubObject))
					{
						if (componentData15.m_Direction == StackDirection.Up)
						{
							componentData16.m_Range.min = componentData15.m_FirstBounds.min - elevation.m_Elevation;
							componentData16.m_Range.max = componentData15.m_LastBounds.max;
						}
						else
						{
							componentData16.m_Range.min = componentData15.m_FirstBounds.min;
							componentData16.m_Range.max = componentData15.m_FirstBounds.max + MathUtils.Size(componentData15.m_MiddleBounds) * 2f + MathUtils.Size(componentData15.m_LastBounds);
						}
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, componentData16);
					}
				}
				if (m_SubObjects.HasBuffer(oldSubObject))
				{
					if (depth < 7)
					{
						updateData.EnsureDeepOwners(Allocator.Temp);
						ref NativeList<DeepSubObjectOwnerData> reference = ref updateData.m_DeepOwners;
						DeepSubObjectOwnerData value = new DeepSubObjectOwnerData
						{
							m_Transform = transformData,
							m_Temp = temp,
							m_Entity = oldSubObject,
							m_Prefab = prefab,
							m_Elevation = elevation.m_Elevation,
							m_RandomSeed = componentData8,
							m_HasRandomSeed = flag,
							m_UnderConstruction = underConstruction,
							m_Destroyed = isDestroyed,
							m_Overridden = isOverridden,
							m_Depth = depth + 1
						};
						reference.Add(in value);
					}
					else
					{
						m_LoopErrorPrefabs.Enqueue(prefab);
					}
				}
				return;
			}
			ObjectData objectData = m_PrefabObjectData[prefab];
			if (!objectData.m_Archetype.Valid)
			{
				return;
			}
			Entity entity = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Owner(owner));
			m_CommandBuffer.SetComponent(jobIndex, entity, new PrefabRef(prefab));
			m_CommandBuffer.SetComponent(jobIndex, entity, transformData);
			if ((componentData3.m_Flags & PlacementFlags.Attached) != PlacementFlags.None)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Attached));
			}
			Temp temp2 = default(Temp);
			if (isTemp)
			{
				temp2.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Dragging | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
				if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
				{
					temp2.m_Flags |= TempFlags.Modify;
				}
				temp2.m_Original = FindOriginalSubObject(prefab, Entity.Null, transformData, alignIndex, ref updateData);
				m_CommandBuffer.AddComponent(jobIndex, entity, temp2);
				if (m_PrefabObjectGeometryData.HasComponent(prefab))
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Animation));
					m_CommandBuffer.AddComponent(jobIndex, entity, default(InterpolatedTransform));
				}
				if (temp2.m_Original != Entity.Null)
				{
					if (flag2 && m_PrefabTreeData.HasComponent(prefab) && m_TreeData.TryGetComponent(temp2.m_Original, out var componentData17))
					{
						m_CommandBuffer.SetComponent(jobIndex, entity, componentData17);
					}
					if ((temp2.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) != 0 && m_OverriddenData.HasComponent(temp2.m_Original))
					{
						isOverridden = true;
					}
					if (owner.Index >= 0 && !m_TempData.HasComponent(owner))
					{
						m_CommandBuffer.AddComponent(jobIndex, temp2.m_Original, default(Hidden));
						m_CommandBuffer.AddComponent(jobIndex, temp2.m_Original, default(BatchesUpdated));
					}
				}
			}
			if (cacheTransform)
			{
				LocalTransformCache component2 = default(LocalTransformCache);
				component2.m_Position = localTransformData.m_Position;
				component2.m_Rotation = localTransformData.m_Rotation;
				component2.m_ParentMesh = parentMesh;
				component2.m_GroupIndex = groupIndex;
				component2.m_Probability = probability;
				component2.m_PrefabSubIndex = prefabSubIndex;
				m_CommandBuffer.AddComponent(jobIndex, entity, component2);
			}
			PseudoRandomSeed componentData18 = default(PseudoRandomSeed);
			if (flag)
			{
				if (!m_PseudoRandomSeedData.TryGetComponent(temp2.m_Original, out componentData18))
				{
					componentData18 = pseudoRandomSeed;
				}
				m_CommandBuffer.SetComponent(jobIndex, entity, componentData18);
			}
			if ((flags & SubObjectFlags.OnGround) == 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, elevation);
			}
			if (alignIndex >= 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, new Aligned((ushort)alignIndex));
			}
			if (relative || boneIndex.y >= 0)
			{
				m_CommandBuffer.RemoveComponent<Static>(jobIndex, entity);
				m_CommandBuffer.AddComponent(jobIndex, entity, new Relative(localTransformData, boneIndex));
			}
			if (interpolated || boneIndex.y >= 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, new InterpolatedTransform(transformData));
			}
			UnderConstruction componentData19 = default(UnderConstruction);
			if (temp2.m_Original != Entity.Null)
			{
				underConstruction = m_UnderConstructionData.TryGetComponent(temp2.m_Original, out componentData19);
			}
			if (underConstruction)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, componentData19);
			}
			Destroyed componentData20 = default(Destroyed);
			if (temp2.m_Original != Entity.Null && (ownerTemp.m_Flags & TempFlags.Upgrade) == 0)
			{
				isDestroyed = m_DestroyedData.TryGetComponent(temp2.m_Original, out componentData20);
			}
			if (isDestroyed)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, componentData20);
			}
			if (isOverridden)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Overridden));
			}
			if (native)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Native));
			}
			if (m_EditorMode && m_EditorContainerData.HasComponent(temp2.m_Original))
			{
				Game.Tools.EditorContainer component3 = m_EditorContainerData[temp2.m_Original];
				m_CommandBuffer.AddComponent(jobIndex, entity, component3);
				if (m_PrefabEffectData.HasComponent(component3.m_Prefab))
				{
					m_CommandBuffer.AddBuffer<EnabledEffect>(jobIndex, entity);
				}
			}
			if (m_PrefabStreetLightData.HasComponent(prefab))
			{
				StreetLight streetLight2 = default(StreetLight);
				if (m_StreetLightData.TryGetComponent(temp2.m_Original, out var componentData21))
				{
					streetLight2 = componentData21;
				}
				Watercraft componentData23;
				if (m_BuildingData.TryGetComponent(topOwner, out var componentData22))
				{
					StreetLightSystem.UpdateStreetLightState(ref streetLight2, componentData22);
				}
				else if (m_WatercraftData.TryGetComponent(topOwner, out componentData23))
				{
					StreetLightSystem.UpdateStreetLightState(ref streetLight2, componentData23);
				}
				m_CommandBuffer.SetComponent(jobIndex, entity, streetLight2);
			}
			if (flag2 && m_PrefabStackData.TryGetComponent(prefab, out var componentData24))
			{
				if (m_StackData.TryGetComponent(temp2.m_Original, out var componentData25))
				{
					m_CommandBuffer.SetComponent(jobIndex, entity, componentData25);
				}
				else
				{
					if (componentData24.m_Direction == StackDirection.Up)
					{
						componentData25.m_Range.min = componentData24.m_FirstBounds.min - elevation.m_Elevation;
						componentData25.m_Range.max = componentData24.m_LastBounds.max;
					}
					else
					{
						componentData25.m_Range.min = componentData24.m_FirstBounds.min;
						componentData25.m_Range.max = componentData24.m_FirstBounds.max + MathUtils.Size(componentData24.m_MiddleBounds) * 2f + MathUtils.Size(componentData24.m_LastBounds);
					}
					m_CommandBuffer.SetComponent(jobIndex, entity, componentData25);
				}
			}
			if (m_PrefabSpawnLocationData.HasComponent(prefab))
			{
				SpawnLocation component4 = new SpawnLocation
				{
					m_GroupIndex = groupIndex
				};
				m_CommandBuffer.SetComponent(jobIndex, entity, component4);
			}
			if (m_PrefabSubObjects.HasBuffer(prefab))
			{
				if (depth < 7)
				{
					updateData.EnsureDeepOwners(Allocator.Temp);
					ref NativeList<DeepSubObjectOwnerData> reference2 = ref updateData.m_DeepOwners;
					DeepSubObjectOwnerData value = new DeepSubObjectOwnerData
					{
						m_Transform = transformData,
						m_Temp = temp2,
						m_Entity = entity,
						m_Prefab = prefab,
						m_Elevation = elevation.m_Elevation,
						m_RandomSeed = componentData18,
						m_New = true,
						m_HasRandomSeed = flag,
						m_UnderConstruction = underConstruction,
						m_Destroyed = isDestroyed,
						m_Overridden = isOverridden,
						m_Depth = depth + 1
					};
					reference2.Add(in value);
				}
				else
				{
					m_LoopErrorPrefabs.Enqueue(prefab);
				}
			}
		}

		private void CreateContainerObject(int jobIndex, Entity owner, bool isTemp, Temp ownerTemp, float ownerElevation, Entity oldSubObject, Transform transformData, Transform localTransformData, ref UpdateSubObjectsData updateData, Entity prefab, float3 scale, float intensity, int parentMesh, int groupIndex, int prefabSubIndex)
		{
			Elevation component = new Elevation(ownerElevation, (ElevationFlags)0);
			component.m_Elevation += localTransformData.m_Position.y;
			if (oldSubObject == Entity.Null)
			{
				oldSubObject = FindOldSubObject(prefab, transformData, -1, ref updateData);
			}
			if (oldSubObject != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldSubObject);
				if (isTemp)
				{
					if (m_TempData.HasComponent(oldSubObject))
					{
						Temp component2 = m_TempData[oldSubObject];
						component2.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Dragging | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
						if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
						{
							component2.m_Flags |= TempFlags.Modify;
						}
						component2.m_Original = FindOriginalSubObject(prefab, component2.m_Original, transformData, -1, ref updateData);
						m_CommandBuffer.SetComponent(jobIndex, oldSubObject, component2);
					}
					m_CommandBuffer.SetComponent(jobIndex, oldSubObject, transformData);
					if (!m_UpdatedData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, default(Updated));
					}
				}
				else if (!transformData.Equals(m_TransformData[oldSubObject]))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldSubObject, transformData);
					if (!m_UpdatedData.HasComponent(oldSubObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSubObject, default(Updated));
					}
				}
				LocalTransformCache component3 = default(LocalTransformCache);
				component3.m_Position = localTransformData.m_Position;
				component3.m_Rotation = localTransformData.m_Rotation;
				component3.m_ParentMesh = parentMesh;
				component3.m_GroupIndex = groupIndex;
				component3.m_Probability = 100;
				component3.m_PrefabSubIndex = prefabSubIndex;
				m_CommandBuffer.SetComponent(jobIndex, oldSubObject, component3);
				m_CommandBuffer.SetComponent(jobIndex, oldSubObject, component);
				Game.Tools.EditorContainer component4 = new Game.Tools.EditorContainer
				{
					m_Prefab = prefab,
					m_Scale = scale,
					m_Intensity = intensity,
					m_GroupIndex = groupIndex
				};
				m_CommandBuffer.SetComponent(jobIndex, oldSubObject, component4);
				return;
			}
			ObjectData objectData = m_PrefabObjectData[m_TransformEditor];
			if (!objectData.m_Archetype.Valid)
			{
				return;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
			m_CommandBuffer.AddComponent(jobIndex, e, new Owner(owner));
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(m_TransformEditor));
			m_CommandBuffer.SetComponent(jobIndex, e, transformData);
			Temp component5 = default(Temp);
			if (isTemp)
			{
				component5.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Dragging | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
				if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
				{
					component5.m_Flags |= TempFlags.Modify;
				}
				component5.m_Original = FindOriginalSubObject(prefab, Entity.Null, transformData, -1, ref updateData);
				m_CommandBuffer.AddComponent(jobIndex, e, component5);
			}
			LocalTransformCache component6 = default(LocalTransformCache);
			component6.m_Position = localTransformData.m_Position;
			component6.m_Rotation = localTransformData.m_Rotation;
			component6.m_ParentMesh = parentMesh;
			component6.m_GroupIndex = groupIndex;
			component6.m_Probability = 100;
			component6.m_PrefabSubIndex = prefabSubIndex;
			m_CommandBuffer.AddComponent(jobIndex, e, component6);
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			Game.Tools.EditorContainer component7 = new Game.Tools.EditorContainer
			{
				m_Prefab = prefab,
				m_Scale = scale,
				m_Intensity = intensity,
				m_GroupIndex = groupIndex
			};
			m_CommandBuffer.SetComponent(jobIndex, e, component7);
			if (m_PrefabEffectData.HasComponent(component7.m_Prefab))
			{
				m_CommandBuffer.AddBuffer<EnabledEffect>(jobIndex, e);
			}
		}

		private void EnsurePlaceholderRequirements(Entity owner, Entity ownerPrefab, ref UpdateSubObjectsData updateData, ref Unity.Mathematics.Random random, bool isObject)
		{
			if (updateData.m_RequirementsSearched)
			{
				return;
			}
			updateData.EnsurePlaceholderRequirements(Allocator.Temp);
			bool flag = false;
			bool flag2 = false;
			if (isObject && m_OwnerData.TryGetComponent(owner, out var componentData) && m_AreaData.HasComponent(componentData.m_Owner))
			{
				owner = componentData.m_Owner;
				isObject = false;
			}
			if (!isObject && m_OwnerData.TryGetComponent(owner, out var componentData2))
			{
				owner = componentData2.m_Owner;
				if (m_AttachmentData.TryGetComponent(componentData2.m_Owner, out var componentData3))
				{
					owner = componentData3.m_Attached;
				}
			}
			if (m_CityServiceUpkeepData.HasComponent(owner))
			{
				if (m_PrefabServiceUpkeepDatas.TryGetBuffer(ownerPrefab, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						updateData.m_StoredResources |= bufferData[i].m_Upkeep.m_Resource;
					}
				}
				if (m_StorageCompanyData.TryGetComponent(ownerPrefab, out var componentData4))
				{
					updateData.m_StoredResources |= componentData4.m_StoredResources;
				}
				if (m_GarbageFacilityData.HasComponent(owner))
				{
					updateData.m_StoredResources |= Resource.Garbage;
				}
			}
			if (m_BuildingRenters.TryGetBuffer(owner, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Entity renter = bufferData2[j].m_Renter;
					if (m_Deleteds.HasComponent(renter))
					{
						continue;
					}
					if (m_CompanyData.TryGetComponent(renter, out var componentData5))
					{
						updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Renter;
						Entity prefab = m_PrefabRefData[renter].m_Prefab;
						if (componentData5.m_Brand != Entity.Null)
						{
							updateData.m_PlaceholderRequirements.TryAdd(componentData5.m_Brand, new int2(0, 1));
							AddAffiliatedBrands(prefab, ref updateData, ref random);
							flag2 = true;
						}
						if (m_StorageCompanyData.TryGetComponent(prefab, out var componentData6))
						{
							updateData.m_StoredResources |= componentData6.m_StoredResources;
						}
						updateData.m_PlaceholderRequirements.TryAdd(prefab, 0);
					}
					else
					{
						if (!m_HouseholdCitizens.TryGetBuffer(renter, out var bufferData3))
						{
							continue;
						}
						for (int k = 0; k < bufferData3.Length; k++)
						{
							if (m_CitizenData.TryGetComponent(bufferData3[k].m_Citizen, out var componentData7))
							{
								switch (componentData7.GetAge())
								{
								case CitizenAge.Child:
									updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Children;
									break;
								case CitizenAge.Teen:
									updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Teens;
									break;
								}
							}
						}
						if (m_HouseholdData.TryGetComponent(renter, out var componentData8))
						{
							int2 consumptionBonuses = CitizenHappinessSystem.GetConsumptionBonuses(componentData8.m_ConsumptionPerDay, bufferData3.Length, in m_HappinessParameterData);
							if (consumptionBonuses.x + consumptionBonuses.y > 0)
							{
								updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.GoodWealth;
							}
						}
						if (m_HouseholdAnimals.TryGetBuffer(renter, out var bufferData4) && bufferData4.Length != 0)
						{
							updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Dogs;
						}
						if (m_HomelessHousehold.HasComponent(renter) && m_HomelessHousehold[renter].m_TempHome == owner)
						{
							updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Homeless;
						}
						else
						{
							updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Renter;
						}
					}
				}
			}
			if (!m_ResidentialPropertyData.HasComponent(owner))
			{
				updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Children | ObjectRequirementFlags.Teens | ObjectRequirementFlags.GoodWealth | ObjectRequirementFlags.Dogs;
			}
			if (m_SurfaceData.TryGetComponent(owner, out var componentData9) && componentData9.m_AccumulatedSnow >= 15)
			{
				updateData.m_PlaceholderRequirementFlags |= ObjectRequirementFlags.Snow;
			}
			if (m_ObjectRequirements.TryGetBuffer(ownerPrefab, out var bufferData5))
			{
				int num = 0;
				while (num < bufferData5.Length)
				{
					ObjectRequirementElement objectRequirementElement = bufferData5[num];
					if ((objectRequirementElement.m_Type & ObjectRequirementType.SelectOnly) != 0)
					{
						int num2 = num;
						while (++num2 < bufferData5.Length && bufferData5[num2].m_Group == objectRequirementElement.m_Group)
						{
						}
						Entity requirement = bufferData5[random.NextInt(num, num2)].m_Requirement;
						updateData.m_PlaceholderRequirements.TryAdd(requirement, 0);
						if (m_CompanyBrands.TryGetBuffer(requirement, out var bufferData6))
						{
							if (bufferData6.Length != 0)
							{
								Entity brand = bufferData6[random.NextInt(bufferData6.Length)].m_Brand;
								updateData.m_PlaceholderRequirements.TryAdd(brand, new int2(0, 1));
								AddAffiliatedBrands(requirement, ref updateData, ref random);
								flag2 = true;
							}
						}
						else if (m_PrefabThemeData.HasComponent(requirement))
						{
							flag = true;
						}
						num = num2;
					}
					else
					{
						num++;
					}
				}
			}
			if (!flag && m_DefaultTheme != Entity.Null)
			{
				updateData.m_PlaceholderRequirements.TryAdd(m_DefaultTheme, 0);
			}
			if (!flag2 && m_BuildingConfigurationData.m_DefaultRenterBrand != Entity.Null)
			{
				updateData.m_PlaceholderRequirements.TryAdd(m_BuildingConfigurationData.m_DefaultRenterBrand, 0);
			}
			updateData.m_RequirementsSearched = true;
		}

		private void AddAffiliatedBrands(Entity entity, ref UpdateSubObjectsData updateData, ref Unity.Mathematics.Random random)
		{
			if (m_AffiliatedBrands.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
			{
				int num = bufferData.Length + 3 >> 1;
				int num2 = 0;
				int num3 = 0;
				Unity.Mathematics.Random random2 = random;
				for (int i = random.NextInt(num >> 1); i < bufferData.Length; i += 1 + random.NextInt(num))
				{
					num3++;
				}
				for (int j = random2.NextInt(num >> 1); j < bufferData.Length; j += 1 + random2.NextInt(num))
				{
					updateData.m_PlaceholderRequirements.TryAdd(bufferData[j].m_Brand, new int2(--num2, num3));
				}
			}
		}

		private Entity FindOldSubObject(Entity prefab, Entity original, ref UpdateSubObjectsData updateData)
		{
			if (updateData.m_OldEntities.IsCreated && updateData.m_OldEntities.TryGetFirstValue(prefab, out var item, out var it))
			{
				do
				{
					if (m_TempData.HasComponent(item) && m_TempData[item].m_Original == original)
					{
						updateData.m_OldEntities.Remove(it);
						return item;
					}
				}
				while (updateData.m_OldEntities.TryGetNextValue(out item, ref it));
			}
			return Entity.Null;
		}

		private Entity FindOldSubObject(Entity prefab, Transform transform, int alignIndex, ref UpdateSubObjectsData updateData)
		{
			Entity entity = Entity.Null;
			if (updateData.m_OldEntities.IsCreated && updateData.m_OldEntities.TryGetFirstValue(prefab, out var item, out var it))
			{
				float num = 0f;
				if (alignIndex >= 0)
				{
					if (m_AlignedData.TryGetComponent(item, out var componentData) && componentData.m_SubObjectIndex == alignIndex)
					{
						updateData.m_OldEntities.Remove(it);
						return item;
					}
				}
				else
				{
					entity = item;
					num = math.distance(m_TransformData[item].m_Position, transform.m_Position);
				}
				NativeParallelMultiHashMapIterator<Entity> it2 = it;
				while (updateData.m_OldEntities.TryGetNextValue(out item, ref it))
				{
					if (alignIndex >= 0)
					{
						if (m_AlignedData.TryGetComponent(item, out var componentData2) && componentData2.m_SubObjectIndex == alignIndex)
						{
							updateData.m_OldEntities.Remove(it);
							return item;
						}
						continue;
					}
					float num2 = math.distance(m_TransformData[item].m_Position, transform.m_Position);
					if (num2 < num)
					{
						entity = item;
						num = num2;
						it2 = it;
					}
				}
				if (entity != Entity.Null)
				{
					updateData.m_OldEntities.Remove(it2);
				}
			}
			return entity;
		}

		private Entity FindOriginalSubObject(Entity prefab, Entity original, Transform transform, int alignIndex, ref UpdateSubObjectsData updateData)
		{
			Entity entity = Entity.Null;
			if (updateData.m_OriginalEntities.IsCreated && updateData.m_OriginalEntities.TryGetFirstValue(prefab, out var item, out var it))
			{
				float num = 0f;
				if (item == original)
				{
					updateData.m_OriginalEntities.Remove(it);
					return original;
				}
				if (alignIndex >= 0)
				{
					if (m_AlignedData.TryGetComponent(item, out var componentData) && componentData.m_SubObjectIndex == alignIndex)
					{
						updateData.m_OriginalEntities.Remove(it);
						return item;
					}
				}
				else
				{
					entity = item;
					num = math.distance(m_TransformData[item].m_Position, transform.m_Position);
				}
				NativeParallelMultiHashMapIterator<Entity> it2 = it;
				while (updateData.m_OriginalEntities.TryGetNextValue(out item, ref it))
				{
					if (item == original)
					{
						updateData.m_OriginalEntities.Remove(it);
						return original;
					}
					if (alignIndex >= 0)
					{
						if (m_AlignedData.TryGetComponent(item, out var componentData2) && componentData2.m_SubObjectIndex == alignIndex)
						{
							updateData.m_OriginalEntities.Remove(it);
							return item;
						}
						continue;
					}
					float num2 = math.distance(m_TransformData[item].m_Position, transform.m_Position);
					if (num2 < num)
					{
						entity = item;
						num = num2;
						it2 = it;
					}
				}
				if (entity != Entity.Null)
				{
					updateData.m_OriginalEntities.Remove(it2);
				}
			}
			return entity;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SubObjectsUpdated> __Game_Objects_SubObjectsUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RentersUpdated> __Game_Buildings_RentersUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Creature> __Game_Creatures_Creature_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Secondary> __Game_Objects_Secondary_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Object> __Game_Objects_Object_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PillarData> __Game_Prefabs_PillarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ThemeData> __Game_Prefabs_ThemeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> __Game_Prefabs_MovingObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkVehicleData> __Game_Prefabs_WorkVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StreetLightData> __Game_Prefabs_StreetLightData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> __Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ActivityPropData> __Game_Prefabs_ActivityPropData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aligned> __Game_Objects_Aligned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StreetLight> __Game_Objects_StreetLight_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Surface> __Game_Objects_Surface_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResidentialProperty> __Game_Buildings_ResidentialProperty_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Human> __Game_Creatures_Human_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Clear> __Game_Areas_Clear_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> __Game_Prefabs_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CompanyBrandElement> __Game_Prefabs_CompanyBrandElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AffiliatedBrandElement> __Game_Prefabs_AffiliatedBrandElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Object>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_SubObjectsUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SubObjectsUpdated>(isReadOnly: true);
			__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Vehicle>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Creature>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Secondary_RO_ComponentLookup = state.GetComponentLookup<Secondary>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentLookup = state.GetComponentLookup<Object>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_PillarData_RO_ComponentLookup = state.GetComponentLookup<PillarData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_ThemeData_RO_ComponentLookup = state.GetComponentLookup<ThemeData>(isReadOnly: true);
			__Game_Prefabs_MovingObjectData_RO_ComponentLookup = state.GetComponentLookup<MovingObjectData>(isReadOnly: true);
			__Game_Prefabs_QuantityObjectData_RO_ComponentLookup = state.GetComponentLookup<QuantityObjectData>(isReadOnly: true);
			__Game_Prefabs_WorkVehicleData_RO_ComponentLookup = state.GetComponentLookup<WorkVehicleData>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_StreetLightData_RO_ComponentLookup = state.GetComponentLookup<StreetLightData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderObjectData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_ActivityPropData_RO_ComponentLookup = state.GetComponentLookup<ActivityPropData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Objects_Aligned_RO_ComponentLookup = state.GetComponentLookup<Aligned>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_StreetLight_RO_ComponentLookup = state.GetComponentLookup<StreetLight>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Objects_Surface_RO_ComponentLookup = state.GetComponentLookup<Surface>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Buildings_ResidentialProperty_RO_ComponentLookup = state.GetComponentLookup<ResidentialProperty>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.OutsideConnection>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentLookup = state.GetComponentLookup<Human>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Areas_Clear_RO_ComponentLookup = state.GetComponentLookup<Clear>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentLookup = state.GetComponentLookup<Watercraft>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubLane>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Prefabs_CompanyBrandElement_RO_BufferLookup = state.GetBufferLookup<CompanyBrandElement>(isReadOnly: true);
			__Game_Prefabs_AffiliatedBrandElement_RO_BufferLookup = state.GetBufferLookup<AffiliatedBrandElement>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RO_BufferLookup = state.GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
		}
	}

	private const int kMaxSubObjectDepth = 7;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ToolSystem m_ToolSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ModificationBarrier2B m_ModificationBarrier;

	private EntityQuery m_UpdateQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_ContainerQuery;

	private EntityQuery m_BuildingSettingsQuery;

	private EntityQuery m_HappinessParameterQuery;

	private ComponentTypeSet m_AppliedTypes;

	private NativeQueue<Entity> m_LoopErrorPrefabs;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2B>();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<SubObject>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Event>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<RentersUpdated>(),
				ComponentType.ReadOnly<SubObjectsUpdated>()
			}
		});
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Object>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_ContainerQuery = GetEntityQuery(ComponentType.ReadOnly<EditorContainerData>(), ComponentType.ReadOnly<ObjectData>());
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_UpdateQuery);
		m_LoopErrorPrefabs = new NativeQueue<Entity>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LoopErrorPrefabs.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ShowLoopErrors();
		NativeQueue<SubObjectOwnerData> ownerQueue = new NativeQueue<SubObjectOwnerData>(Allocator.TempJob);
		NativeList<SubObjectOwnerData> nativeList = new NativeList<SubObjectOwnerData>(Allocator.TempJob);
		NativeParallelHashSet<Entity> ignoreSet = new NativeParallelHashSet<Entity>(100, Allocator.TempJob);
		NativeParallelHashMap<Entity, SubObjectOwnerData> ownerMap = new NativeParallelHashMap<Entity, SubObjectOwnerData>(100, Allocator.TempJob);
		CheckSubObjectOwnersJob jobData = new CheckSubObjectOwnersJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjectsUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SubObjectsUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RentersUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Object_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_OwnerQueue = ownerQueue.AsParallelWriter()
		};
		CollectSubObjectOwnersJob jobData2 = new CollectSubObjectOwnersJob
		{
			m_OwnerQueue = ownerQueue,
			m_OwnerList = nativeList,
			m_OwnerMap = ownerMap
		};
		FillIgnoreSetJob jobData3 = new FillIgnoreSetJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IgnoreSet = ignoreSet
		};
		Entity transformEditor = Entity.Null;
		if (m_ToolSystem.actionMode.IsEditor() && !m_ContainerQuery.IsEmptyIgnoreFilter)
		{
			transformEditor = m_ContainerQuery.GetSingletonEntity();
		}
		JobHandle deps;
		UpdateSubObjectsJob jobData4 = new UpdateSubObjectsJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPillarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PillarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabThemeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ThemeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMovingObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MovingObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWorkVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StreetLightData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCargoTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ActivityPropData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ActivityPropData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AlignedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Aligned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_StreetLight_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SurfaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Surface_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResidentialPropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ResidentialProperty_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityServiceUpkeepData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetNodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHousehold = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaClearData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Clear_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingRenters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_PlaceholderObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ObjectRequirements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_CompanyBrands = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CompanyBrandElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AffiliatedBrands = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AffiliatedBrandElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabServiceUpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabCharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabAnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_RandomSeed = RandomSeed.Next(),
			m_DefaultTheme = m_CityConfigurationSystem.defaultTheme,
			m_TransformEditor = transformEditor,
			m_BuildingConfigurationData = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
			m_HappinessParameterData = m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_AppliedTypes = m_AppliedTypes,
			m_OwnerList = nativeList.AsDeferredJobArray(),
			m_IgnoreSet = ignoreSet,
			m_OwnerMap = ownerMap,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_LoopErrorPrefabs = m_LoopErrorPrefabs.AsParallelWriter(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_UpdateQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, dependsOn);
		JobHandle jobHandle2 = IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.Schedule(jobData3, m_TempQuery, base.Dependency), deps), jobData: jobData4, list: nativeList, innerloopBatchCount: 1);
		ownerQueue.Dispose(jobHandle);
		nativeList.Dispose(jobHandle2);
		ignoreSet.Dispose(jobHandle2);
		ownerMap.Dispose(jobHandle2);
		m_TerrainSystem.AddCPUHeightReader(jobHandle2);
		m_WaterSystem.AddSurfaceReader(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
	}

	private void ShowLoopErrors()
	{
		if (m_LoopErrorPrefabs.IsEmpty())
		{
			return;
		}
		NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(m_LoopErrorPrefabs.Count, Allocator.Temp);
		Entity item;
		while (m_LoopErrorPrefabs.TryDequeue(out item))
		{
			nativeParallelHashSet.Add(item);
		}
		PrefabSystem existingSystemManaged = base.World.GetExistingSystemManaged<PrefabSystem>();
		NativeArray<Entity> nativeArray = nativeParallelHashSet.ToNativeArray(Allocator.Temp);
		foreach (Entity item2 in nativeArray)
		{
			PrefabBase prefab = existingSystemManaged.GetPrefab<PrefabBase>(item2);
			COSystemBase.baseLog.ErrorFormat("Sub objects are nested too deep in '{0}'. Are you using a parent object as a sub object?", prefab.name);
		}
		nativeParallelHashSet.Dispose();
		nativeArray.Dispose();
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
	public SubObjectSystem()
	{
	}
}
