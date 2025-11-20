using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ZoneSpawnSystem : GameSystemBase
{
	public struct SpawnLocation
	{
		public Entity m_Entity;

		public Entity m_Building;

		public int4 m_LotArea;

		public float m_Priority;

		public ZoneType m_ZoneType;

		public Game.Zones.AreaType m_AreaType;

		public LotFlags m_LotFlags;
	}

	[BurstCompile]
	public struct EvaluateSpawnAreas : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_BuildingChunks;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public ZonePreferenceData m_Preferences;

		[ReadOnly]
		public int m_SpawnResidential;

		[ReadOnly]
		public int m_SpawnCommercial;

		[ReadOnly]
		public int m_SpawnIndustrial;

		[ReadOnly]
		public int m_SpawnStorage;

		[ReadOnly]
		public int m_MinDemand;

		public int3 m_ResidentialDemands;

		[ReadOnly]
		public NativeArray<int> m_CommercialBuildingDemands;

		[ReadOnly]
		public NativeArray<int> m_IndustrialDemands;

		[ReadOnly]
		public NativeArray<int> m_StorageDemands;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<CurvePosition> m_CurvePositionType;

		[ReadOnly]
		public BufferTypeHandle<VacantLot> m_VacantLotType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> m_BuildingDataType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

		[ReadOnly]
		public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;

		[ReadOnly]
		public ComponentTypeHandle<WarehouseData> m_WarehouseType;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_Availabilities;

		[ReadOnly]
		public NativeList<IndustrialProcessData> m_Processes;

		[ReadOnly]
		public BufferLookup<ProcessEstimate> m_ProcessEstimates;

		[ReadOnly]
		public ComponentLookup<LandValue> m_LandValues;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public NativeArray<GroundPollution> m_GroundPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		public NativeQueue<SpawnLocation>.ParallelWriter m_Residential;

		public NativeQueue<SpawnLocation>.ParallelWriter m_Commercial;

		public NativeQueue<SpawnLocation>.ParallelWriter m_Industrial;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			SpawnLocation bestLocation = default(SpawnLocation);
			SpawnLocation bestLocation2 = default(SpawnLocation);
			SpawnLocation bestLocation3 = default(SpawnLocation);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<VacantLot> bufferAccessor = chunk.GetBufferAccessor(ref m_VacantLotType);
			if (bufferAccessor.Length != 0)
			{
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
				NativeArray<CurvePosition> nativeArray3 = chunk.GetNativeArray(ref m_CurvePositionType);
				NativeArray<Block> nativeArray4 = chunk.GetNativeArray(ref m_BlockType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					DynamicBuffer<VacantLot> dynamicBuffer = bufferAccessor[i];
					Owner owner = nativeArray2[i];
					CurvePosition curvePosition = nativeArray3[i];
					Block block = nativeArray4[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						VacantLot lot = dynamicBuffer[j];
						if (!m_ZonePropertiesDatas.HasComponent(m_ZonePrefabs[lot.m_Type]))
						{
							continue;
						}
						ZoneData zoneData = m_ZoneData[m_ZonePrefabs[lot.m_Type]];
						ZonePropertiesData zonePropertiesData = m_ZonePropertiesDatas[m_ZonePrefabs[lot.m_Type]];
						DynamicBuffer<ProcessEstimate> estimates = m_ProcessEstimates[m_ZonePrefabs[lot.m_Type]];
						switch (zoneData.m_AreaType)
						{
						case Game.Zones.AreaType.Residential:
							if (m_SpawnResidential != 0)
							{
								float curvePos2 = CalculateCurvePos(curvePosition, lot, block);
								TryAddLot(ref bestLocation, ref random, owner.m_Owner, curvePos2, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, zonePropertiesData, estimates, m_Processes);
							}
							break;
						case Game.Zones.AreaType.Commercial:
							if (m_SpawnCommercial != 0)
							{
								float curvePos3 = CalculateCurvePos(curvePosition, lot, block);
								TryAddLot(ref bestLocation2, ref random, owner.m_Owner, curvePos3, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, zonePropertiesData, estimates, m_Processes);
							}
							break;
						case Game.Zones.AreaType.Industrial:
							if (m_SpawnIndustrial != 0 || m_SpawnStorage != 0)
							{
								float curvePos = CalculateCurvePos(curvePosition, lot, block);
								TryAddLot(ref bestLocation3, ref random, owner.m_Owner, curvePos, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, zonePropertiesData, estimates, m_Processes, m_SpawnIndustrial != 0, m_SpawnStorage != 0);
							}
							break;
						}
					}
				}
			}
			if (bestLocation.m_Priority != 0f)
			{
				m_Residential.Enqueue(bestLocation);
			}
			if (bestLocation2.m_Priority != 0f)
			{
				m_Commercial.Enqueue(bestLocation2);
			}
			if (bestLocation3.m_Priority != 0f)
			{
				m_Industrial.Enqueue(bestLocation3);
			}
		}

		private float CalculateCurvePos(CurvePosition curvePosition, VacantLot lot, Block block)
		{
			float t = math.saturate((float)(lot.m_Area.x + lot.m_Area.y) * 0.5f / (float)block.m_Size.x);
			return math.lerp(curvePosition.m_CurvePosition.x, curvePosition.m_CurvePosition.y, t);
		}

		private void TryAddLot(ref SpawnLocation bestLocation, ref Random random, Entity road, float curvePos, Entity entity, int4 area, LotFlags flags, int height, ZoneData zoneData, ZonePropertiesData zonePropertiesData, DynamicBuffer<ProcessEstimate> estimates, NativeList<IndustrialProcessData> processes, bool normal = true, bool storage = false)
		{
			if (!m_Availabilities.HasBuffer(road))
			{
				return;
			}
			if ((zoneData.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == 0)
			{
				flags &= ~LotFlags.CornerLeft;
			}
			if ((zoneData.m_ZoneFlags & ZoneFlags.SupportRightCorner) == 0)
			{
				flags &= ~LotFlags.CornerRight;
			}
			SpawnLocation location = new SpawnLocation
			{
				m_Entity = entity,
				m_LotArea = area,
				m_ZoneType = zoneData.m_ZoneType,
				m_AreaType = zoneData.m_AreaType,
				m_LotFlags = flags
			};
			bool office = zoneData.m_AreaType == Game.Zones.AreaType.Industrial && estimates.Length == 0;
			DynamicBuffer<ResourceAvailability> availabilities = m_Availabilities[road];
			if (m_BlockData.HasComponent(location.m_Entity))
			{
				float3 position = ZoneUtils.GetPosition(m_BlockData[location.m_Entity], location.m_LotArea.xz, location.m_LotArea.yw);
				bool extractor = false;
				GroundPollution pollution = GroundPollutionSystem.GetPollution(position, m_GroundPollutionMap);
				NoisePollution pollution2 = NoisePollutionSystem.GetPollution(position, m_NoisePollutionMap);
				AirPollution pollution3 = AirPollutionSystem.GetPollution(position, m_AirPollutionMap);
				float landValue = m_LandValues[road].m_LandValue;
				float maxHeight = (float)height - position.y;
				if (SelectBuilding(ref location, ref random, availabilities, zoneData, zonePropertiesData, curvePos, new float3(pollution.m_Pollution, pollution2.m_Pollution, pollution3.m_Pollution), landValue, maxHeight, estimates, processes, normal, storage, extractor, office) && location.m_Priority > bestLocation.m_Priority)
				{
					bestLocation = location;
				}
			}
		}

		private bool SelectBuilding(ref SpawnLocation location, ref Random random, DynamicBuffer<ResourceAvailability> availabilities, ZoneData zoneData, ZonePropertiesData zonePropertiesData, float curvePos, float3 pollution, float landValue, float maxHeight, DynamicBuffer<ProcessEstimate> estimates, NativeList<IndustrialProcessData> processes, bool normal = true, bool storage = false, bool extractor = false, bool office = false)
		{
			int2 @int = location.m_LotArea.yw - location.m_LotArea.xz;
			BuildingData buildingData = default(BuildingData);
			bool2 @bool = new bool2((location.m_LotFlags & LotFlags.CornerLeft) != 0, (location.m_LotFlags & LotFlags.CornerRight) != 0);
			bool flag = (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0;
			for (int i = 0; i < m_BuildingChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_BuildingChunks[i];
				if (!archetypeChunk.GetSharedComponent(m_BuildingSpawnGroupType).m_ZoneType.Equals(location.m_ZoneType))
				{
					continue;
				}
				bool flag2 = archetypeChunk.Has(ref m_WarehouseType);
				if ((flag2 && !storage) || (!flag2 && !normal))
				{
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<BuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_BuildingDataType);
				NativeArray<SpawnableBuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_SpawnableBuildingType);
				NativeArray<BuildingPropertyData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_BuildingPropertyType);
				NativeArray<ObjectGeometryData> nativeArray5 = archetypeChunk.GetNativeArray(ref m_ObjectGeometryType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if (nativeArray3[j].m_Level != 1)
					{
						continue;
					}
					BuildingData buildingData2 = nativeArray2[j];
					int2 lotSize = buildingData2.m_LotSize;
					bool2 bool2 = new bool2((buildingData2.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0, (buildingData2.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0);
					float y = nativeArray5[j].m_Size.y;
					if (!math.all(lotSize <= @int) || !(y <= maxHeight))
					{
						continue;
					}
					BuildingPropertyData buildingPropertyData = nativeArray4[j];
					ZoneDensity zoneDensity = PropertyUtils.GetZoneDensity(zoneData, zonePropertiesData);
					int num = EvaluateDemandAndAvailability(buildingPropertyData, zoneData.m_AreaType, zoneDensity, flag2);
					if (!(num >= m_MinDemand || extractor))
					{
						continue;
					}
					int2 int2 = math.select(@int - lotSize, 0, lotSize == @int - 1);
					float num2 = (float)(lotSize.x * lotSize.y) * random.NextFloat(1f, 1.05f);
					num2 += (float)(int2.x * lotSize.y) * random.NextFloat(0.95f, 1f);
					num2 += (float)(@int.x * int2.y) * random.NextFloat(0.55f, 0.6f);
					num2 /= (float)(@int.x * @int.y);
					num2 *= (float)(num + 1);
					num2 *= math.csum(math.select(0.01f, 0.5f, @bool == bool2));
					if (!extractor)
					{
						float num3 = landValue;
						float num4;
						if (location.m_AreaType == Game.Zones.AreaType.Residential)
						{
							num4 = ((buildingPropertyData.m_ResidentialProperties == 1) ? 2f : ((float)buildingPropertyData.CountProperties()));
							lotSize.x = math.select(lotSize.x, @int.x, lotSize.x == @int.x - 1 && flag);
							num3 *= (float)(lotSize.x * @int.y);
						}
						else
						{
							num4 = buildingPropertyData.m_SpaceMultiplier;
						}
						float score = ZoneEvaluationUtils.GetScore(location.m_AreaType, office, availabilities, curvePos, ref m_Preferences, flag2, flag2 ? m_StorageDemands : m_IndustrialDemands, buildingPropertyData, pollution, num3 / num4, estimates, processes, m_ResourcePrefabs, ref m_ResourceDatas);
						score = math.select(score, math.max(0f, score) + 1f, m_MinDemand == 0);
						num2 *= score;
					}
					if (num2 > location.m_Priority)
					{
						location.m_Building = nativeArray[j];
						buildingData = buildingData2;
						location.m_Priority = num2;
					}
				}
			}
			if (location.m_Building != Entity.Null)
			{
				if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) == 0 && ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0 || random.NextBool()))
				{
					location.m_LotArea.x = location.m_LotArea.y - buildingData.m_LotSize.x;
					location.m_LotArea.w = location.m_LotArea.z + buildingData.m_LotSize.y;
				}
				else
				{
					location.m_LotArea.yw = location.m_LotArea.xz + buildingData.m_LotSize;
				}
				return true;
			}
			return false;
		}

		private int EvaluateDemandAndAvailability(BuildingPropertyData buildingPropertyData, Game.Zones.AreaType areaType, ZoneDensity zoneDensity, bool storage = false)
		{
			switch (areaType)
			{
			case Game.Zones.AreaType.Residential:
				return zoneDensity switch
				{
					ZoneDensity.Low => m_ResidentialDemands.x, 
					ZoneDensity.Medium => m_ResidentialDemands.y, 
					_ => m_ResidentialDemands.z, 
				};
			case Game.Zones.AreaType.Commercial:
			{
				int num2 = 0;
				ResourceIterator iterator2 = ResourceIterator.GetIterator();
				while (iterator2.Next())
				{
					if ((buildingPropertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
					{
						num2 += m_CommercialBuildingDemands[EconomyUtils.GetResourceIndex(iterator2.resource)];
					}
				}
				return num2;
			}
			case Game.Zones.AreaType.Industrial:
			{
				int num = 0;
				ResourceIterator iterator = ResourceIterator.GetIterator();
				while (iterator.Next())
				{
					if (storage)
					{
						if ((buildingPropertyData.m_AllowedStored & iterator.resource) != Resource.NoResource)
						{
							num += m_StorageDemands[EconomyUtils.GetResourceIndex(iterator.resource)];
						}
					}
					else if ((buildingPropertyData.m_AllowedManufactured & iterator.resource) != Resource.NoResource)
					{
						num += m_IndustrialDemands[EconomyUtils.GetResourceIndex(iterator.resource)];
					}
				}
				return num;
			}
			default:
				return 0;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	public struct SpawnBuildingJob : IJobParallelFor
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public int2 m_LotSize;

			public float2 m_StartPosition;

			public float2 m_Right;

			public float2 m_Forward;

			public int m_MaxHeight;

			public ComponentLookup<Block> m_BlockData;

			public ComponentLookup<ValidArea> m_ValidAreaData;

			public BufferLookup<Cell> m_Cells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds))
				{
					return;
				}
				ValidArea validArea = m_ValidAreaData[blockEntity];
				if (validArea.m_Area.y <= validArea.m_Area.x)
				{
					return;
				}
				Block block = m_BlockData[blockEntity];
				DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
				float2 @float = m_StartPosition;
				int2 @int = default(int2);
				@int.y = 0;
				while (@int.y < m_LotSize.y)
				{
					float2 position = @float;
					@int.x = 0;
					while (@int.x < m_LotSize.x)
					{
						int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
						if (math.all((cellIndex >= validArea.m_Area.xz) & (cellIndex < validArea.m_Area.yw)))
						{
							int index = cellIndex.y * block.m_Size.x + cellIndex.x;
							Cell cell = dynamicBuffer[index];
							if ((cell.m_State & CellFlags.Visible) != CellFlags.None)
							{
								m_MaxHeight = math.min(m_MaxHeight, cell.m_Height);
							}
						}
						position -= m_Right;
						@int.x++;
					}
					@float -= m_Forward;
					@int.y++;
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public EntityArchetype m_DefinitionArchetype;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[NativeDisableParallelForRestriction]
		public NativeQueue<SpawnLocation> m_Residential;

		[NativeDisableParallelForRestriction]
		public NativeQueue<SpawnLocation> m_Commercial;

		[NativeDisableParallelForRestriction]
		public NativeQueue<SpawnLocation> m_Industrial;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			SpawnLocation location;
			switch (index)
			{
			default:
				return;
			case 0:
				if (!SelectLocation(m_Residential, out location))
				{
					return;
				}
				break;
			case 1:
				if (!SelectLocation(m_Commercial, out location))
				{
					return;
				}
				break;
			case 2:
				if (!SelectLocation(m_Industrial, out location))
				{
					return;
				}
				break;
			}
			Random random = m_RandomSeed.GetRandom(index);
			Spawn(index, location, ref random);
		}

		private bool SelectLocation(NativeQueue<SpawnLocation> queue, out SpawnLocation location)
		{
			location = default(SpawnLocation);
			SpawnLocation item;
			while (queue.TryDequeue(out item))
			{
				if (item.m_Priority > location.m_Priority)
				{
					location = item;
				}
			}
			return location.m_Priority != 0f;
		}

		private void Spawn(int jobIndex, SpawnLocation location, ref Random random)
		{
			BuildingData prefabBuildingData = m_PrefabBuildingData[location.m_Building];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[location.m_Building];
			PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
			if (m_PrefabPlaceableObjectData.HasComponent(location.m_Building))
			{
				placeableObjectData = m_PrefabPlaceableObjectData[location.m_Building];
			}
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = location.m_Building
			};
			component.m_Flags |= CreationFlags.Permanent | CreationFlags.Construction;
			component.m_RandomSeed = random.NextInt();
			Transform transform = default(Transform);
			if (m_BlockData.HasComponent(location.m_Entity))
			{
				Block block = m_BlockData[location.m_Entity];
				transform.m_Position = ZoneUtils.GetPosition(block, location.m_LotArea.xz, location.m_LotArea.yw);
				transform.m_Rotation = ZoneUtils.GetRotation(block);
			}
			else if (m_TransformData.HasComponent(location.m_Entity))
			{
				component.m_Attached = location.m_Entity;
				component.m_Flags |= CreationFlags.Attach;
				Transform transform2 = m_TransformData[location.m_Entity];
				PrefabRef prefabRef = m_PrefabRefData[location.m_Entity];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
				transform.m_Position = transform2.m_Position;
				transform.m_Rotation = transform2.m_Rotation;
				float z = (float)(buildingData.m_LotSize.y - prefabBuildingData.m_LotSize.y) * 4f;
				transform.m_Position += math.rotate(transform.m_Rotation, new float3(0f, 0f, z));
			}
			float3 worldPosition = BuildingUtils.CalculateFrontPosition(transform, prefabBuildingData.m_LotSize.y);
			transform.m_Position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, worldPosition);
			if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
			{
				transform.m_Position.y += placeableObjectData.m_PlacementOffset.y;
			}
			float maxHeight = GetMaxHeight(transform, prefabBuildingData);
			transform.m_Position.y = math.min(transform.m_Position.y, maxHeight - objectGeometryData.m_Size.y - 0.1f);
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_ParentMesh = -1,
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation,
				m_LocalPosition = transform.m_Position,
				m_LocalRotation = transform.m_Rotation
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_DefinitionArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
			m_CommandBuffer.SetComponent(jobIndex, e, component2);
			OwnerDefinition ownerDefinition = new OwnerDefinition
			{
				m_Prefab = location.m_Building,
				m_Position = component2.m_Position,
				m_Rotation = component2.m_Rotation
			};
			if (m_PrefabSubAreas.HasBuffer(location.m_Building))
			{
				Spawn(jobIndex, ownerDefinition, m_PrefabSubAreas[location.m_Building], m_PrefabSubAreaNodes[location.m_Building], prefabBuildingData, ref random);
			}
			if (m_PrefabSubNets.HasBuffer(location.m_Building))
			{
				Spawn(jobIndex, ownerDefinition, m_PrefabSubNets[location.m_Building], ref random);
			}
		}

		private float GetMaxHeight(Transform transform, BuildingData prefabBuildingData)
		{
			float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
			float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
			float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
			float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
			float2 float3 = math.abs(float2) + math.abs(@float);
			Iterator iterator = new Iterator
			{
				m_Bounds = new Bounds2(transform.m_Position.xz - float3, transform.m_Position.xz + float3),
				m_LotSize = prefabBuildingData.m_LotSize,
				m_StartPosition = transform.m_Position.xz + float2 + @float,
				m_Right = xz,
				m_Forward = xz2,
				m_MaxHeight = int.MaxValue,
				m_BlockData = m_BlockData,
				m_ValidAreaData = m_ValidAreaData,
				m_Cells = m_Cells
			};
			m_ZoneSearchTree.Iterate(ref iterator);
			return iterator.m_MaxHeight;
		}

		private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, BuildingData prefabBuildingData, ref Random random)
		{
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			bool flag = false;
			for (int i = 0; i < subAreas.Length; i++)
			{
				Game.Prefabs.SubArea subArea = subAreas[i];
				AreaGeometryData areaGeometryData = m_PrefabAreaGeometryData[subArea.m_Prefab];
				if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
				{
					if (flag)
					{
						continue;
					}
					subArea.m_Prefab = m_BuildingConfigurationData.m_ConstructionSurface;
					flag = true;
				}
				int seed;
				if (m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out var bufferData))
				{
					if (!selectedSpawnables.IsCreated)
					{
						selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
					}
					if (!AreaUtils.SelectAreaPrefab(bufferData, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
					{
						continue;
					}
				}
				else
				{
					seed = random.NextInt();
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex);
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = subArea.m_Prefab,
					m_RandomSeed = seed
				};
				component.m_Flags |= CreationFlags.Permanent;
				m_CommandBuffer.AddComponent(jobIndex, e, component);
				m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
				m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, e);
				if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
				{
					Quad3 quad = BuildingUtils.CalculateCorners(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation), prefabBuildingData.m_LotSize);
					dynamicBuffer.ResizeUninitialized(5);
					dynamicBuffer[0] = new Game.Areas.Node(quad.a, float.MinValue);
					dynamicBuffer[1] = new Game.Areas.Node(quad.b, float.MinValue);
					dynamicBuffer[2] = new Game.Areas.Node(quad.c, float.MinValue);
					dynamicBuffer[3] = new Game.Areas.Node(quad.d, float.MinValue);
					dynamicBuffer[4] = new Game.Areas.Node(quad.a, float.MinValue);
					continue;
				}
				dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
				int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
				int num2 = 0;
				for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
				{
					float3 position = subAreaNodes[num].m_Position;
					float3 position2 = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, position);
					int parentMesh = subAreaNodes[num].m_ParentMesh;
					float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
					dynamicBuffer[num2] = new Game.Areas.Node(position2, elevation);
					num2++;
					if (++num == subArea.m_NodeRange.y)
					{
						num = subArea.m_NodeRange.x;
					}
				}
			}
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubNet> subNets, ref Random random)
		{
			NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
			for (int i = 0; i < subNets.Length; i++)
			{
				Game.Prefabs.SubNet subNet = subNets[i];
				if (subNet.m_NodeIndex.x >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.x)
					{
						nodePositions.Add(default(float4));
					}
					nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
				}
				if (subNet.m_NodeIndex.y >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.y)
					{
						nodePositions.Add(default(float4));
					}
					nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
				}
			}
			for (int j = 0; j < nodePositions.Length; j++)
			{
				nodePositions[j] /= math.max(1f, nodePositions[j].w);
			}
			for (int k = 0; k < subNets.Length; k++)
			{
				Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_PrefabNetGeometryData);
				CreateSubNet(jobIndex, subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, ownerDefinition, ref random);
			}
			nodePositions.Dispose();
		}

		private void CreateSubNet(int jobIndex, Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, OwnerDefinition ownerDefinition, ref Random random)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_RandomSeed = random.NextInt()
			};
			component.m_Flags |= CreationFlags.Permanent;
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
			m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
			NetCourse component2 = default(NetCourse);
			component2.m_Curve = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, curve);
			component2.m_StartPosition.m_Position = component2.m_Curve.a;
			component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), ownerDefinition.m_Rotation);
			component2.m_StartPosition.m_CourseDelta = 0f;
			component2.m_StartPosition.m_Elevation = curve.a.y;
			component2.m_StartPosition.m_ParentMesh = parentMesh.x;
			if (nodeIndex.x >= 0)
			{
				component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.x].xyz);
			}
			component2.m_EndPosition.m_Position = component2.m_Curve.d;
			component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), ownerDefinition.m_Rotation);
			component2.m_EndPosition.m_CourseDelta = 1f;
			component2.m_EndPosition.m_Elevation = curve.d.y;
			component2.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.y].xyz);
			}
			component2.m_Length = MathUtils.Length(component2.m_Curve);
			component2.m_FixedIndex = -1;
			component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
			component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
			if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
			{
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			m_CommandBuffer.AddComponent(jobIndex, e, component2);
			if (upgrades != default(CompositionFlags))
			{
				Upgraded component3 = new Upgraded
				{
					m_Flags = upgrades
				};
				m_CommandBuffer.AddComponent(jobIndex, e, component3);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurvePosition> __Game_Zones_CurvePosition_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<VacantLot> __Game_Zones_VacantLot_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WarehouseData> __Game_Prefabs_WarehouseData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ProcessEstimate> __Game_Zones_ProcessEstimate_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Zones_CurvePosition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurvePosition>(isReadOnly: true);
			__Game_Zones_VacantLot_RO_BufferTypeHandle = state.GetBufferTypeHandle<VacantLot>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
			__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WarehouseData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Zones_ProcessEstimate_RO_BufferLookup = state.GetBufferLookup<ProcessEstimate>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	private ZoneSystem m_ZoneSystem;

	private ResidentialDemandSystem m_ResidentialDemandSystem;

	private CommercialDemandSystem m_CommercialDemandSystem;

	private IndustrialDemandSystem m_IndustrialDemandSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private TerrainSystem m_TerrainSystem;

	private Game.Zones.SearchSystem m_SearchSystem;

	private ResourceSystem m_ResourceSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_LotQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_ProcessQuery;

	private EntityQuery m_BuildingConfigurationQuery;

	private EntityArchetype m_DefinitionArchetype;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1944910157_0;

	public bool debugFastSpawn { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 13;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneSystem = base.World.GetOrCreateSystemManaged<ZoneSystem>();
		m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
		m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
		m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_LotQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Block>(),
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<CurvePosition>(),
				ComponentType.ReadOnly<VacantLot>()
			},
			Any = new ComponentType[0],
			None = new ComponentType[2]
			{
				ComponentType.ReadWrite<Temp>(),
				ComponentType.ReadWrite<Deleted>()
			}
		});
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<SpawnableBuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
		m_DefinitionArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<CreationDefinition>(), ComponentType.ReadWrite<ObjectDefinition>(), ComponentType.ReadWrite<Updated>(), ComponentType.ReadWrite<Deleted>());
		m_ProcessQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
		m_BuildingConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_LotQuery);
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		RandomSeed.Next().GetRandom(0);
		bool flag = debugFastSpawn || (m_ResidentialDemandSystem.buildingDemand.x + m_ResidentialDemandSystem.buildingDemand.y + m_ResidentialDemandSystem.buildingDemand.z) / 3 > 0;
		bool flag2 = debugFastSpawn || m_CommercialDemandSystem.buildingDemand > 0;
		bool flag3 = debugFastSpawn || (m_IndustrialDemandSystem.industrialBuildingDemand + m_IndustrialDemandSystem.officeBuildingDemand) / 2 > 0;
		bool flag4 = debugFastSpawn || m_IndustrialDemandSystem.storageBuildingDemand > 0;
		NativeQueue<SpawnLocation> residential = new NativeQueue<SpawnLocation>(Allocator.TempJob);
		NativeQueue<SpawnLocation> commercial = new NativeQueue<SpawnLocation>(Allocator.TempJob);
		NativeQueue<SpawnLocation> industrial = new NativeQueue<SpawnLocation>(Allocator.TempJob);
		JobHandle outJobHandle;
		JobHandle deps;
		JobHandle deps2;
		JobHandle deps3;
		JobHandle outJobHandle2;
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		EvaluateSpawnAreas jobData = new EvaluateSpawnAreas
		{
			m_BuildingChunks = m_BuildingQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_ZonePrefabs = m_ZoneSystem.GetPrefabs(),
			m_Preferences = __query_1944910157_0.GetSingleton<ZonePreferenceData>(),
			m_SpawnResidential = (flag ? 1 : 0),
			m_SpawnCommercial = (flag2 ? 1 : 0),
			m_SpawnIndustrial = (flag3 ? 1 : 0),
			m_SpawnStorage = (flag4 ? 1 : 0),
			m_MinDemand = ((!debugFastSpawn) ? 1 : 0),
			m_ResidentialDemands = m_ResidentialDemandSystem.buildingDemand,
			m_CommercialBuildingDemands = m_CommercialDemandSystem.GetBuildingDemands(out deps),
			m_IndustrialDemands = m_IndustrialDemandSystem.GetBuildingDemands(out deps2),
			m_StorageDemands = m_IndustrialDemandSystem.GetStorageBuildingDemands(out deps3),
			m_RandomSeed = RandomSeed.Next(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurvePositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_CurvePosition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VacantLotType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingSpawnGroupType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_WarehouseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Availabilities = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
			m_LandValues = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LandValue_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Processes = m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_ProcessEstimates = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_GroundPollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_Residential = residential.AsParallelWriter(),
			m_Commercial = commercial.AsParallelWriter(),
			m_Industrial = industrial.AsParallelWriter()
		};
		JobHandle dependencies4;
		SpawnBuildingJob jobData2 = new SpawnBuildingJob
		{
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ValidAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_DefinitionArchetype = m_DefinitionArchetype,
			m_RandomSeed = RandomSeed.Next(),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_ZoneSearchTree = m_SearchSystem.GetSearchTree(readOnly: true, out dependencies4),
			m_BuildingConfigurationData = m_BuildingConfigurationQuery.GetSingleton<BuildingConfigurationData>(),
			m_Residential = residential,
			m_Commercial = commercial,
			m_Industrial = industrial,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_LotQuery, JobUtils.CombineDependencies(outJobHandle, deps, deps2, deps3, dependencies, dependencies2, dependencies3, base.Dependency, outJobHandle2));
		JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(jobData2, 3, 1, JobHandle.CombineDependencies(jobHandle, dependencies4));
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_AirPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		m_CommercialDemandSystem.AddReader(jobHandle);
		m_IndustrialDemandSystem.AddReader(jobHandle);
		residential.Dispose(jobHandle2);
		commercial.Dispose(jobHandle2);
		industrial.Dispose(jobHandle2);
		m_ZoneSystem.AddPrefabsReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_SearchSystem.AddSearchTreeReader(jobHandle2);
		base.Dependency = jobHandle2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ZonePreferenceData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1944910157_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public ZoneSpawnSystem()
	{
	}
}
