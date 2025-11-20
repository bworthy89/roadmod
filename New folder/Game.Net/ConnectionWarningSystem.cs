using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class ConnectionWarningSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectOwnersJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> m_RoadConnectionUpdatedType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public NativeList<Entity> m_Owners;

		public void Execute()
		{
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(32, Allocator.Temp);
			for (int i = 0; i < m_Owners.Length; i++)
			{
				nativeParallelHashSet.Add(m_Owners[i]);
			}
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				NativeArray<RoadConnectionUpdated> nativeArray = archetypeChunk.GetNativeArray(ref m_RoadConnectionUpdatedType);
				if (nativeArray.Length != 0)
				{
					for (int k = 0; k < nativeArray.Length; k++)
					{
						RoadConnectionUpdated roadConnectionUpdated = nativeArray[k];
						if (!m_TempData.HasComponent(roadConnectionUpdated.m_Building) && nativeParallelHashSet.Add(roadConnectionUpdated.m_Building))
						{
							m_Owners.Add(in roadConnectionUpdated.m_Building);
						}
					}
					continue;
				}
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Owner> nativeArray3 = archetypeChunk.GetNativeArray(ref m_OwnerType);
				if (nativeArray3.Length != 0)
				{
					bool flag = archetypeChunk.Has(ref m_NodeType);
					bool flag2 = archetypeChunk.Has(ref m_BuildingType);
					for (int l = 0; l < nativeArray3.Length; l++)
					{
						Owner owner = nativeArray3[l];
						if (flag && m_EdgeData.HasComponent(owner.m_Owner))
						{
							if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
							{
								Entity value = nativeArray2[l];
								if (nativeParallelHashSet.Add(value))
								{
									m_Owners.Add(in value);
								}
								continue;
							}
							owner = componentData;
						}
						if (flag2)
						{
							Entity value2 = nativeArray2[l];
							if (nativeParallelHashSet.Add(value2))
							{
								m_Owners.Add(in value2);
							}
						}
						while (nativeParallelHashSet.Add(owner.m_Owner))
						{
							m_Owners.Add(in owner.m_Owner);
							if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData2))
							{
								break;
							}
							owner = componentData2;
						}
					}
					continue;
				}
				for (int m = 0; m < nativeArray2.Length; m++)
				{
					Entity value3 = nativeArray2[m];
					if (nativeParallelHashSet.Add(value3))
					{
						m_Owners.Add(in value3);
					}
				}
			}
			nativeParallelHashSet.Dispose();
		}
	}

	[BurstCompile]
	private struct CollectOwnersJob2 : IJob
	{
		public struct NodeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Node> m_NodeData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Owner> m_OwnerData;

			public NativeParallelHashSet<Entity> m_OwnerSet;

			public NativeList<Entity> m_Owners;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!Intersect(bounds) || !m_NodeData.HasComponent(entity))
				{
					return;
				}
				if (m_OwnerData.TryGetComponent(entity, out var componentData))
				{
					if (m_EdgeData.HasComponent(componentData.m_Owner))
					{
						if (!m_OwnerData.TryGetComponent(componentData.m_Owner, out var componentData2))
						{
							if (m_OwnerSet.Add(entity))
							{
								m_Owners.Add(in entity);
							}
							return;
						}
						componentData = componentData2;
					}
					while (m_OwnerSet.Add(componentData.m_Owner))
					{
						m_Owners.Add(in componentData.m_Owner);
						if (m_OwnerData.TryGetComponent(componentData.m_Owner, out var componentData3))
						{
							componentData = componentData3;
							continue;
						}
						break;
					}
				}
				else if (m_OwnerSet.Add(entity))
				{
					m_Owners.Add(in entity);
				}
			}
		}

		public struct BuildingIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Owner> m_OwnerData;

			public NativeParallelHashSet<Entity> m_OwnerSet;

			public NativeList<Entity> m_Owners;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!Intersect(bounds) || !m_BuildingData.HasComponent(entity))
				{
					return;
				}
				if (m_OwnerSet.Add(entity))
				{
					m_Owners.Add(in entity);
				}
				if (!m_OwnerData.TryGetComponent(entity, out var componentData))
				{
					return;
				}
				while (m_OwnerSet.Add(componentData.m_Owner))
				{
					m_Owners.Add(in componentData.m_Owner);
					if (m_OwnerData.TryGetComponent(componentData.m_Owner, out var componentData2))
					{
						componentData = componentData2;
						continue;
					}
					break;
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeList<Bounds2> m_Bounds;

		public NativeList<Entity> m_Owners;

		public void Execute()
		{
			NativeParallelHashSet<Entity> ownerSet = new NativeParallelHashSet<Entity>(32, Allocator.Temp);
			for (int i = 0; i < m_Owners.Length; i++)
			{
				ownerSet.Add(m_Owners[i]);
			}
			NodeIterator iterator = new NodeIterator
			{
				m_NodeData = m_NodeData,
				m_EdgeData = m_EdgeData,
				m_OwnerData = m_OwnerData,
				m_OwnerSet = ownerSet,
				m_Owners = m_Owners
			};
			BuildingIterator iterator2 = new BuildingIterator
			{
				m_BuildingData = m_BuildingData,
				m_OwnerData = m_OwnerData,
				m_OwnerSet = ownerSet,
				m_Owners = m_Owners
			};
			for (int j = 0; j < m_Bounds.Length; j++)
			{
				iterator.m_Bounds = (iterator2.m_Bounds = m_Bounds[j]);
				m_NetSearchTree.Iterate(ref iterator);
				m_ObjectSearchTree.Iterate(ref iterator2);
			}
			ownerSet.Dispose();
		}
	}

	private struct PathfindElement
	{
		public PathNode m_StartNode;

		public PathNode m_MiddleNode;

		public PathNode m_EndNode;

		public Entity m_Entity;

		public bool2 m_Connected;

		public bool2 m_Directions;

		public byte m_IconType;

		public byte m_IconLocation;

		public byte m_IconLocation2;

		public sbyte m_Priority;

		public bool m_CanIgnore;

		public bool m_Optional;

		public sbyte m_SubConnection;
	}

	private struct BufferElement
	{
		public PathNode m_Node;

		public bool2 m_Connected;
	}

	private struct Connection
	{
		public RoadTypes m_RoadTypes;

		public RoadTypes m_RoadTypes2;

		public TrackTypes m_TrackTypes;

		public TrackTypes m_TrackTypes2;
	}

	[BurstCompile]
	private struct CheckOwnersJob : IJobParallelForDefer
	{
		public struct MapTileIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public int m_Result;

			public float3 m_Position;

			public ComponentLookup<Native> m_NativeData;

			public ComponentLookup<MapTile> m_MapTileData;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (m_Result == 0)
				{
					return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz);
				}
				return false;
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (Intersect(bounds) && m_MapTileData.HasComponent(item.m_Area) && MathUtils.Intersect(AreaUtils.GetTriangle3(m_AreaNodes[item.m_Area], m_AreaTriangles[item.m_Area][item.m_Triangle]).xz, m_Position.xz, out var _))
				{
					m_Result = math.select(1, 2, m_NativeData.HasComponent(item.m_Area));
				}
			}
		}

		private struct IconItem : IEquatable<IconItem>
		{
			public Entity m_Target;

			public IconFlags m_Flags;

			public IconItem(Entity target, IconFlags flags)
			{
				m_Target = target;
				m_Flags = flags;
			}

			public bool Equals(IconItem other)
			{
				if (m_Target == other.m_Target)
				{
					return m_Flags == other.m_Flags;
				}
				return false;
			}

			public override int GetHashCode()
			{
				int hashCode = m_Target.GetHashCode();
				byte b = (byte)m_Flags;
				return hashCode ^ b.GetHashCode();
			}
		}

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public NativeArray<Entity> m_Owners;

		[ReadOnly]
		public WaterPipeParameterData m_WaterPipeParameterData;

		[ReadOnly]
		public ElectricityParameterData m_ElectricityParameterData;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		[ReadOnly]
		public TrafficConfigurationData m_TrafficConfigurationData;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public IconCommandBuffer m_IconCommandBuffer;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<LaneConnection> m_LaneConnectionData;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<ResourceConnection> m_ResourceConnectionData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> m_TakeoffLocationData;

		[ReadOnly]
		public ComponentLookup<AccessLane> m_AccessLaneData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[ReadOnly]
		public ComponentLookup<ElectricityProducer> m_ElectricityProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Transformer> m_TransformerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<MapTile> m_MapTileData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<ResourceConnectionData> m_PrefabResourceConnectionData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public BufferLookup<SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<IconElement> m_IconElements;

		public void Execute(int index)
		{
			Entity entity = m_Owners[index];
			if (m_ConnectedEdges.HasBuffer(entity))
			{
				UpdateNodeConnectionWarnings(entity, IsNativeMapTile(m_NodeData[entity].m_Position));
			}
			else
			{
				if (!m_BuildingData.HasComponent(entity))
				{
					return;
				}
				Building building = m_BuildingData[entity];
				PrefabRef prefabRef = m_PrefabRefData[entity];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
				bool isSubBuilding = false;
				bool isPlaceholder = false;
				bool flag = !m_DestroyedData.HasComponent(entity);
				if (m_OwnerData.TryGetComponent(entity, out var componentData))
				{
					isSubBuilding = true;
					if (m_AttachmentData.TryGetComponent(componentData.m_Owner, out var componentData2) && m_PrefabRefData.TryGetComponent(componentData2.m_Attached, out var componentData3))
					{
						isPlaceholder = true;
						flag &= (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.RequireRoad | Game.Prefabs.BuildingFlags.RequireAccess)) != 0 || (m_PrefabBuildingData.TryGetComponent(componentData3.m_Prefab, out var componentData4) && (componentData4.m_Flags & (Game.Prefabs.BuildingFlags.RequireRoad | Game.Prefabs.BuildingFlags.RequireAccess)) != 0 && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.HasResourceNode) == 0);
					}
					else
					{
						flag &= (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.RequireRoad | Game.Prefabs.BuildingFlags.RequireAccess)) != 0 || (m_PrefabRefData.TryGetComponent(componentData.m_Owner, out var componentData5) && m_PrefabBuildingData.TryGetComponent(componentData5.m_Prefab, out var componentData6) && (componentData6.m_Flags & (Game.Prefabs.BuildingFlags.RequireRoad | Game.Prefabs.BuildingFlags.RequireAccess)) != 0 && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.HasResourceNode) == 0);
					}
				}
				else
				{
					UpdateSubnetConnectionWarnings(entity);
					if (m_AttachmentData.TryGetComponent(entity, out var componentData7) && m_PrefabRefData.TryGetComponent(componentData7.m_Attached, out var componentData8))
					{
						isPlaceholder = true;
						m_BuildingData.TryGetComponent(componentData7.m_Attached, out var componentData9);
						m_PrefabBuildingData.TryGetComponent(componentData8.m_Prefab, out var componentData10);
						flag &= (m_PrefabRefData.HasComponent(componentData9.m_RoadEdge) && (componentData10.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0) || (componentData10.m_Flags & Game.Prefabs.BuildingFlags.RequireAccess) != 0;
					}
					else
					{
						flag &= (m_PrefabRefData.HasComponent(building.m_RoadEdge) && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0) || (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RequireAccess) != 0;
					}
				}
				if (flag)
				{
					Game.Objects.Transform transform = m_TransformData[entity];
					if (IsNativeMapTile(transform.m_Position))
					{
						ClearPathfindIslandWarnings(entity);
						return;
					}
					float2 @float = buildingData.m_LotSize;
					Quad3 lot = ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
					{
						min = 
						{
							xz = @float * -4f
						},
						max = 
						{
							xz = @float * 4f
						}
					}, position: transform.m_Position, rotation: transform.m_Rotation);
					UpdatePathfindIslandWarnings(entity, lot, isSubBuilding, isPlaceholder);
				}
				else
				{
					ClearPathfindIslandWarnings(entity);
				}
			}
		}

		private bool IsNativeMapTile(float3 position)
		{
			MapTileIterator iterator = new MapTileIterator
			{
				m_Position = position,
				m_NativeData = m_NativeData,
				m_MapTileData = m_MapTileData,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles
			};
			m_AreaSearchTree.Iterate(ref iterator);
			m_NativeData = iterator.m_NativeData;
			m_MapTileData = iterator.m_MapTileData;
			m_AreaNodes = iterator.m_AreaNodes;
			m_AreaTriangles = iterator.m_AreaTriangles;
			return iterator.m_Result == 2;
		}

		private void ClearPathfindIslandWarnings(Entity owner)
		{
			if (!m_IconElements.HasBuffer(owner))
			{
				return;
			}
			DynamicBuffer<IconElement> dynamicBuffer = m_IconElements[owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity icon = dynamicBuffer[i].m_Icon;
				Entity prefab = m_PrefabRefData[icon].m_Prefab;
				if ((prefab == m_TrafficConfigurationData.m_CarConnectionNotification || prefab == m_TrafficConfigurationData.m_ShipConnectionNotification || prefab == m_TrafficConfigurationData.m_PedestrianConnectionNotification || prefab == m_TrafficConfigurationData.m_TrainConnectionNotification || prefab == m_TrafficConfigurationData.m_RoadConnectionNotification || prefab == m_TrafficConfigurationData.m_BicycleConnectionNotification) && m_TargetData.TryGetComponent(icon, out var componentData))
				{
					IconFlags flags = m_IconData[icon].m_Flags & IconFlags.SecondaryLocation;
					m_IconCommandBuffer.Remove(owner, prefab, componentData.m_Target, flags);
				}
			}
		}

		private void UpdatePathfindIslandWarnings(Entity owner, Quad3 lot, bool isSubBuilding, bool isPlaceholder)
		{
			NativeList<PathfindElement> ownedElements = new NativeList<PathfindElement>(100, Allocator.Temp);
			NativeParallelMultiHashMap<PathNode, int> nodeMap = new NativeParallelMultiHashMap<PathNode, int>(100, Allocator.Temp);
			NativeParallelHashSet<PathNode> externalNodes = new NativeParallelHashSet<PathNode>(100, Allocator.Temp);
			AddPathfindElements(ownedElements, nodeMap, externalNodes, owner, owner, lot, isRoad: false, isSubBuilding, isPlaceholder);
			if (isSubBuilding && m_OwnerData.TryGetComponent(owner, out var componentData) && m_SubLanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				AddPathfindElements(ownedElements, nodeMap, bufferData, pedestrianIcon: true, isRoad: false, onlyExisting: true, optionalConnections: false, false, -1f);
			}
			CheckConnectedElements(ownedElements, nodeMap, externalNodes, out var canIgnore);
			UpdatePathfindWarnings(ownedElements, owner, canIgnore);
			ownedElements.Dispose();
			nodeMap.Dispose();
			externalNodes.Dispose();
		}

		private void UpdatePathfindWarnings(NativeList<PathfindElement> ownedElements, Entity owner, bool canIgnore)
		{
			NativeHashSet<IconItem> nativeHashSet = default(NativeHashSet<IconItem>);
			if (!canIgnore)
			{
				bool flag = false;
				for (int i = 0; i < ownedElements.Length; i++)
				{
					PathfindElement pathfindElement = ownedElements[i];
					if (!math.all(pathfindElement.m_Connected) && pathfindElement.m_Priority >= 0 && !pathfindElement.m_Optional && (pathfindElement.m_Priority != 0 || pathfindElement.m_SubConnection == 0) && pathfindElement.m_IconType == 4)
					{
						flag = true;
						break;
					}
				}
				for (int j = 0; j < ownedElements.Length; j++)
				{
					PathfindElement pathfindElement2 = ownedElements[j];
					if (math.all(pathfindElement2.m_Connected) || pathfindElement2.m_Priority < 0 || pathfindElement2.m_Optional || (pathfindElement2.m_Priority == 0 && pathfindElement2.m_SubConnection != 0))
					{
						continue;
					}
					Entity entity = pathfindElement2.m_IconType switch
					{
						1 => m_TrafficConfigurationData.m_CarConnectionNotification, 
						2 => m_TrafficConfigurationData.m_PedestrianConnectionNotification, 
						3 => m_TrafficConfigurationData.m_TrainConnectionNotification, 
						4 => m_TrafficConfigurationData.m_RoadConnectionNotification, 
						5 => m_TrafficConfigurationData.m_ShipConnectionNotification, 
						6 => m_TrafficConfigurationData.m_BicycleConnectionNotification, 
						_ => Entity.Null, 
					};
					if (!(entity != Entity.Null))
					{
						continue;
					}
					IconFlags iconFlags = (IconFlags)0;
					if (!nativeHashSet.IsCreated)
					{
						nativeHashSet = new NativeHashSet<IconItem>(10, Allocator.Temp);
					}
					if (pathfindElement2.m_IconType == 4)
					{
						if (m_EdgeLaneData.TryGetComponent(pathfindElement2.m_Entity, out var componentData))
						{
							float num = math.lerp(componentData.m_EdgeDelta.x, componentData.m_EdgeDelta.y, (float)(int)pathfindElement2.m_IconLocation * 0.003921569f);
							float num2 = math.lerp(componentData.m_EdgeDelta.x, componentData.m_EdgeDelta.y, (float)(int)pathfindElement2.m_IconLocation2 * 0.003921569f);
							pathfindElement2.m_IconLocation = (byte)math.clamp(Mathf.RoundToInt(num * 255f), 0, 255);
							pathfindElement2.m_IconLocation2 = (byte)math.clamp(Mathf.RoundToInt(num2 * 255f), 0, 255);
						}
						if (m_OwnerData.TryGetComponent(pathfindElement2.m_Entity, out var componentData2))
						{
							pathfindElement2.m_Entity = componentData2.m_Owner;
							if (pathfindElement2.m_IconLocation == byte.MaxValue && pathfindElement2.m_IconLocation2 == byte.MaxValue)
							{
								iconFlags = IconFlags.SecondaryLocation;
							}
						}
					}
					else if (flag)
					{
						continue;
					}
					if (!nativeHashSet.Add(new IconItem(pathfindElement2.m_Entity, iconFlags)))
					{
						continue;
					}
					if (m_CurveData.HasComponent(pathfindElement2.m_Entity))
					{
						float3 location = MathUtils.Position(m_CurveData[pathfindElement2.m_Entity].m_Bezier, (float)(int)pathfindElement2.m_IconLocation * 0.003921569f);
						m_IconCommandBuffer.Add(owner, entity, location, IconPriority.Warning, IconClusterLayer.Default, IconFlags.TargetLocation | iconFlags, pathfindElement2.m_Entity);
						if (pathfindElement2.m_IconLocation2 != pathfindElement2.m_IconLocation && nativeHashSet.Add(new IconItem(pathfindElement2.m_Entity, IconFlags.SecondaryLocation)))
						{
							location = MathUtils.Position(m_CurveData[pathfindElement2.m_Entity].m_Bezier, (float)(int)pathfindElement2.m_IconLocation2 * 0.003921569f);
							m_IconCommandBuffer.Add(owner, entity, location, IconPriority.Warning, IconClusterLayer.Default, IconFlags.TargetLocation | IconFlags.SecondaryLocation, pathfindElement2.m_Entity);
						}
					}
					else
					{
						m_IconCommandBuffer.Add(owner, entity, IconPriority.Warning, IconClusterLayer.Default, IconFlags.TargetLocation | iconFlags, pathfindElement2.m_Entity);
					}
				}
			}
			if (m_IconElements.HasBuffer(owner))
			{
				DynamicBuffer<IconElement> dynamicBuffer = m_IconElements[owner];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity icon = dynamicBuffer[k].m_Icon;
					Entity prefab = m_PrefabRefData[icon].m_Prefab;
					if ((prefab == m_TrafficConfigurationData.m_CarConnectionNotification || prefab == m_TrafficConfigurationData.m_ShipConnectionNotification || prefab == m_TrafficConfigurationData.m_PedestrianConnectionNotification || prefab == m_TrafficConfigurationData.m_TrainConnectionNotification || prefab == m_TrafficConfigurationData.m_RoadConnectionNotification || prefab == m_TrafficConfigurationData.m_BicycleConnectionNotification) && m_TargetData.TryGetComponent(icon, out var componentData3))
					{
						IconFlags flags = m_IconData[icon].m_Flags & IconFlags.SecondaryLocation;
						if (!nativeHashSet.IsCreated || !nativeHashSet.Contains(new IconItem(componentData3.m_Target, flags)))
						{
							m_IconCommandBuffer.Remove(owner, prefab, componentData3.m_Target, flags);
						}
					}
				}
			}
			if (nativeHashSet.IsCreated)
			{
				nativeHashSet.Dispose();
			}
		}

		private void CheckConnectedElements(NativeList<PathfindElement> ownedElements, NativeParallelMultiHashMap<PathNode, int> nodeMap, NativeParallelHashSet<PathNode> externalNodes, out bool canIgnore)
		{
			NativeList<BufferElement> nativeList = new NativeList<BufferElement>(100, Allocator.Temp);
			canIgnore = true;
			for (int i = 0; i < ownedElements.Length; i++)
			{
				PathfindElement pathfindElement = ownedElements[i];
				canIgnore &= pathfindElement.m_CanIgnore;
				if (math.all(pathfindElement.m_Connected))
				{
					continue;
				}
				if (math.all(pathfindElement.m_Directions))
				{
					if (externalNodes.Contains(pathfindElement.m_StartNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_StartNode,
							m_Connected = true
						};
						nativeList.Add(in value);
					}
					if (externalNodes.Contains(pathfindElement.m_EndNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_EndNode,
							m_Connected = true
						};
						nativeList.Add(in value);
					}
				}
				else if (pathfindElement.m_Directions.x)
				{
					if (!pathfindElement.m_Connected.x && externalNodes.Contains(pathfindElement.m_StartNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_StartNode,
							m_Connected = new bool2(x: true, y: false)
						};
						nativeList.Add(in value);
					}
					if (!pathfindElement.m_Connected.y && externalNodes.Contains(pathfindElement.m_EndNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_EndNode,
							m_Connected = new bool2(x: false, y: true)
						};
						nativeList.Add(in value);
					}
				}
				else if (pathfindElement.m_Directions.y)
				{
					if (!pathfindElement.m_Connected.x && externalNodes.Contains(pathfindElement.m_EndNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_EndNode,
							m_Connected = new bool2(x: true, y: false)
						};
						nativeList.Add(in value);
					}
					if (!pathfindElement.m_Connected.y && externalNodes.Contains(pathfindElement.m_StartNode.StripCurvePos()))
					{
						BufferElement value = new BufferElement
						{
							m_Node = pathfindElement.m_StartNode,
							m_Connected = new bool2(x: false, y: true)
						};
						nativeList.Add(in value);
					}
				}
				while (nativeList.Length > 0)
				{
					BufferElement bufferElement = nativeList[nativeList.Length - 1];
					nativeList.RemoveAt(nativeList.Length - 1);
					if (!nodeMap.TryGetFirstValue(bufferElement.m_Node.StripCurvePos(), out var item, out var it))
					{
						continue;
					}
					do
					{
						pathfindElement = ownedElements[item];
						if (pathfindElement.m_StartNode.EqualsIgnoreCurvePos(bufferElement.m_Node))
						{
							bool2 @bool = bufferElement.m_Connected & pathfindElement.m_Directions & !pathfindElement.m_Connected;
							if (math.any(@bool))
							{
								pathfindElement.m_Connected |= @bool;
								ownedElements[item] = pathfindElement;
								BufferElement value = new BufferElement
								{
									m_Node = pathfindElement.m_MiddleNode,
									m_Connected = @bool
								};
								nativeList.Add(in value);
								value = new BufferElement
								{
									m_Node = pathfindElement.m_EndNode,
									m_Connected = @bool
								};
								nativeList.Add(in value);
							}
						}
						else if (pathfindElement.m_MiddleNode.EqualsIgnoreCurvePos(bufferElement.m_Node))
						{
							bool2 bool2 = bufferElement.m_Connected & !pathfindElement.m_Connected;
							if (math.any(bool2))
							{
								pathfindElement.m_Connected |= bool2;
								ownedElements[item] = pathfindElement;
								if (math.any(bool2 & pathfindElement.m_Directions))
								{
									BufferElement value = new BufferElement
									{
										m_Node = pathfindElement.m_EndNode,
										m_Connected = (bool2 & pathfindElement.m_Directions)
									};
									nativeList.Add(in value);
								}
								if (math.any(bool2 & pathfindElement.m_Directions.yx))
								{
									BufferElement value = new BufferElement
									{
										m_Node = pathfindElement.m_StartNode,
										m_Connected = (bool2 & pathfindElement.m_Directions.yx)
									};
									nativeList.Add(in value);
								}
							}
						}
						else if (pathfindElement.m_EndNode.EqualsIgnoreCurvePos(bufferElement.m_Node))
						{
							bool2 bool3 = bufferElement.m_Connected & pathfindElement.m_Directions.yx & !pathfindElement.m_Connected;
							if (math.any(bool3))
							{
								pathfindElement.m_Connected |= bool3;
								ownedElements[item] = pathfindElement;
								BufferElement value = new BufferElement
								{
									m_Node = pathfindElement.m_MiddleNode,
									m_Connected = bool3
								};
								nativeList.Add(in value);
								value = new BufferElement
								{
									m_Node = pathfindElement.m_StartNode,
									m_Connected = bool3
								};
								nativeList.Add(in value);
							}
						}
					}
					while (nodeMap.TryGetNextValue(out item, ref it));
				}
			}
			for (int j = 0; j < ownedElements.Length; j++)
			{
				PathfindElement pathfindElement2 = ownedElements[j];
				if (math.all(pathfindElement2.m_Connected))
				{
					continue;
				}
				if (pathfindElement2.m_Priority > 0)
				{
					bool flag = pathfindElement2.m_Optional;
					bool flag2 = false;
					BufferElement value = new BufferElement
					{
						m_Node = pathfindElement2.m_StartNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_MiddleNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_EndNode
					};
					nativeList.Add(in value);
					while (nativeList.Length > 0)
					{
						BufferElement bufferElement2 = nativeList[nativeList.Length - 1];
						nativeList.RemoveAt(nativeList.Length - 1);
						if (!nodeMap.TryGetFirstValue(bufferElement2.m_Node.StripCurvePos(), out var item2, out var it2))
						{
							continue;
						}
						do
						{
							pathfindElement2 = ownedElements[item2];
							flag2 |= flag & !pathfindElement2.m_Optional & !math.all(pathfindElement2.m_Connected);
							if (pathfindElement2.m_Priority == 0)
							{
								pathfindElement2.m_Priority = -1;
								ownedElements[item2] = pathfindElement2;
								if (pathfindElement2.m_StartNode.EqualsIgnoreCurvePos(bufferElement2.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_MiddleNode.EqualsIgnoreCurvePos(bufferElement2.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_EndNode.EqualsIgnoreCurvePos(bufferElement2.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
								}
							}
						}
						while (nodeMap.TryGetNextValue(out item2, ref it2));
					}
					if (!flag2)
					{
						continue;
					}
					pathfindElement2 = ownedElements[j];
					pathfindElement2.m_Optional = false;
					ownedElements[j] = pathfindElement2;
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_StartNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_MiddleNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_EndNode
					};
					nativeList.Add(in value);
					while (nativeList.Length > 0)
					{
						BufferElement bufferElement3 = nativeList[nativeList.Length - 1];
						nativeList.RemoveAt(nativeList.Length - 1);
						if (!nodeMap.TryGetFirstValue(bufferElement3.m_Node.StripCurvePos(), out var item3, out var it3))
						{
							continue;
						}
						do
						{
							pathfindElement2 = ownedElements[item3];
							if (pathfindElement2.m_Optional)
							{
								pathfindElement2.m_Optional = false;
								ownedElements[item3] = pathfindElement2;
								if (pathfindElement2.m_StartNode.EqualsIgnoreCurvePos(bufferElement3.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_MiddleNode.EqualsIgnoreCurvePos(bufferElement3.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_EndNode.EqualsIgnoreCurvePos(bufferElement3.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
								}
							}
						}
						while (nodeMap.TryGetNextValue(out item3, ref it3));
					}
				}
				else
				{
					if (pathfindElement2.m_SubConnection <= 0)
					{
						continue;
					}
					BufferElement value = new BufferElement
					{
						m_Node = pathfindElement2.m_StartNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_MiddleNode
					};
					nativeList.Add(in value);
					value = new BufferElement
					{
						m_Node = pathfindElement2.m_EndNode
					};
					nativeList.Add(in value);
					while (nativeList.Length > 0)
					{
						BufferElement bufferElement4 = nativeList[nativeList.Length - 1];
						nativeList.RemoveAt(nativeList.Length - 1);
						if (!nodeMap.TryGetFirstValue(bufferElement4.m_Node.StripCurvePos(), out var item4, out var it4))
						{
							continue;
						}
						do
						{
							pathfindElement2 = ownedElements[item4];
							if (pathfindElement2.m_SubConnection == 0)
							{
								pathfindElement2.m_SubConnection = -1;
								ownedElements[item4] = pathfindElement2;
								if (pathfindElement2.m_StartNode.EqualsIgnoreCurvePos(bufferElement4.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_MiddleNode.EqualsIgnoreCurvePos(bufferElement4.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_EndNode
									};
									nativeList.Add(in value);
								}
								else if (pathfindElement2.m_EndNode.EqualsIgnoreCurvePos(bufferElement4.m_Node))
								{
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_MiddleNode
									};
									nativeList.Add(in value);
									value = new BufferElement
									{
										m_Node = pathfindElement2.m_StartNode
									};
									nativeList.Add(in value);
								}
							}
						}
						while (nodeMap.TryGetNextValue(out item4, ref it4));
					}
				}
			}
			nativeList.Dispose();
		}

		private bool IsDeadEnd(Entity edge, Entity node, Entity topOwner, Entity owner, out bool maybe)
		{
			maybe = false;
			if (m_ConnectedEdges.TryGetBuffer(node, out var bufferData))
			{
				if (bufferData.Length == 1)
				{
					return true;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity edge2 = bufferData[i].m_Edge;
					if (!(edge2 == edge) && m_OwnerData.TryGetComponent(edge2, out var componentData))
					{
						Entity owner2 = componentData.m_Owner;
						if (owner2 == owner)
						{
							return false;
						}
						while (m_OwnerData.TryGetComponent(owner2, out componentData))
						{
							owner2 = componentData.m_Owner;
						}
						if (owner2 == topOwner)
						{
							return false;
						}
					}
				}
				maybe = true;
				return true;
			}
			return false;
		}

		private void AddPathfindElements(NativeList<PathfindElement> ownedElements, NativeParallelMultiHashMap<PathNode, int> nodeMap, NativeParallelHashSet<PathNode> externalNodes, Entity topOwner, Entity owner, Quad3 lot, bool isRoad, bool isSubBuilding, bool isPlaceholder)
		{
			if (m_SubNets.HasBuffer(owner))
			{
				DynamicBuffer<SubNet> dynamicBuffer = m_SubNets[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subNet = dynamicBuffer[i].m_SubNet;
					PrefabRef prefabRef = m_PrefabRefData[subNet];
					bool isRoad2 = (m_PrefabNetData[prefabRef.m_Prefab].m_RequiredLayers & Layer.Road) != 0;
					AddPathfindElements(ownedElements, nodeMap, externalNodes, topOwner, subNet, lot, isRoad2, isSubBuilding, isPlaceholder);
					if (m_ConnectedEdges.TryGetBuffer(subNet, out var bufferData))
					{
						for (int j = 0; j < bufferData.Length; j++)
						{
							ConnectedEdge connectedEdge = bufferData[j];
							if (!AddExternalNodes(externalNodes, topOwner, owner, connectedEdge.m_Edge))
							{
								continue;
							}
							Edge edge = m_EdgeData[connectedEdge.m_Edge];
							DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_ConnectedNodes[connectedEdge.m_Edge];
							if (AddExternalNodes(externalNodes, topOwner, owner, edge.m_Start))
							{
								DynamicBuffer<ConnectedEdge> dynamicBuffer3 = m_ConnectedEdges[edge.m_Start];
								for (int k = 0; k < dynamicBuffer3.Length; k++)
								{
									ConnectedEdge connectedEdge2 = dynamicBuffer3[k];
									if (connectedEdge2.m_Edge != connectedEdge.m_Edge)
									{
										AddExternalNodes(externalNodes, topOwner, owner, connectedEdge2.m_Edge);
									}
								}
							}
							if (AddExternalNodes(externalNodes, topOwner, owner, edge.m_End))
							{
								DynamicBuffer<ConnectedEdge> dynamicBuffer4 = m_ConnectedEdges[edge.m_End];
								for (int l = 0; l < dynamicBuffer4.Length; l++)
								{
									ConnectedEdge connectedEdge3 = dynamicBuffer4[l];
									if (connectedEdge3.m_Edge != connectedEdge.m_Edge)
									{
										AddExternalNodes(externalNodes, topOwner, owner, connectedEdge3.m_Edge);
									}
								}
							}
							for (int m = 0; m < dynamicBuffer2.Length; m++)
							{
								ConnectedNode connectedNode = dynamicBuffer2[m];
								if (!AddExternalNodes(externalNodes, topOwner, owner, connectedNode.m_Node))
								{
									continue;
								}
								DynamicBuffer<ConnectedEdge> dynamicBuffer5 = m_ConnectedEdges[connectedNode.m_Node];
								for (int n = 0; n < dynamicBuffer5.Length; n++)
								{
									ConnectedEdge connectedEdge4 = dynamicBuffer5[n];
									if (connectedEdge4.m_Edge != connectedEdge.m_Edge)
									{
										AddExternalNodes(externalNodes, topOwner, owner, connectedEdge4.m_Edge);
									}
								}
							}
						}
					}
					else
					{
						if (!m_EdgeData.TryGetComponent(subNet, out var componentData) || !m_ConnectedNodes.TryGetBuffer(subNet, out var bufferData2))
						{
							continue;
						}
						AddExternalNodes(externalNodes, topOwner, owner, componentData.m_Start);
						AddExternalNodes(externalNodes, topOwner, owner, componentData.m_End);
						for (int num = 0; num < bufferData2.Length; num++)
						{
							ConnectedNode connectedNode2 = bufferData2[num];
							if (!AddExternalNodes(externalNodes, topOwner, owner, connectedNode2.m_Node))
							{
								continue;
							}
							DynamicBuffer<ConnectedEdge> dynamicBuffer6 = m_ConnectedEdges[connectedNode2.m_Node];
							for (int num2 = 0; num2 < dynamicBuffer6.Length; num2++)
							{
								ConnectedEdge connectedEdge5 = dynamicBuffer6[num2];
								if (connectedEdge5.m_Edge != subNet)
								{
									AddExternalNodes(externalNodes, topOwner, owner, connectedEdge5.m_Edge);
								}
							}
						}
					}
				}
			}
			if (m_SubAreas.HasBuffer(owner))
			{
				DynamicBuffer<Game.Areas.SubArea> dynamicBuffer7 = m_SubAreas[owner];
				for (int num3 = 0; num3 < dynamicBuffer7.Length; num3++)
				{
					Entity area = dynamicBuffer7[num3].m_Area;
					if (!isPlaceholder || isSubBuilding || !m_LotData.HasComponent(area))
					{
						AddPathfindElements(ownedElements, nodeMap, externalNodes, topOwner, area, lot, isRoad: false, isSubBuilding, isPlaceholder);
					}
				}
			}
			float t;
			if (m_SubLanes.HasBuffer(owner) && !m_ResourceConnectionData.HasComponent(owner))
			{
				PrefabRef prefabRef2 = m_PrefabRefData[owner];
				bool optionalConnections = isPlaceholder && !isSubBuilding && topOwner == owner;
				bool pedestrianIcon = true;
				bool2 maybeDeadEnd = false;
				float2 @float = -1f;
				if (m_PrefabNetData.HasComponent(prefabRef2.m_Prefab))
				{
					NetData netData = m_PrefabNetData[prefabRef2.m_Prefab];
					pedestrianIcon = (netData.m_RequiredLayers & (Layer.TrainTrack | Layer.Waterway | Layer.TramTrack | Layer.SubwayTrack)) == 0;
					float num4 = 0f;
					if (m_PrefabNetGeometryData.HasComponent(prefabRef2.m_Prefab))
					{
						num4 += m_PrefabNetGeometryData[prefabRef2.m_Prefab].m_DefaultWidth * 0.5f;
					}
					if (m_PrefabLocalConnectData.HasComponent(prefabRef2.m_Prefab))
					{
						num4 += m_PrefabLocalConnectData[prefabRef2.m_Prefab].m_SearchDistance;
					}
					if ((netData.m_RequiredLayers & Layer.Waterway) != Layer.None)
					{
						if (m_NodeData.HasComponent(owner))
						{
							Node node = m_NodeData[owner];
							if (!MathUtils.Intersect(lot.xz, node.m_Position.xz))
							{
								@float.x = 0.5f;
							}
						}
					}
					else if (m_EdgeData.HasComponent(owner))
					{
						Edge edge2 = m_EdgeData[owner];
						bool2 @bool = false;
						if (IsDeadEnd(owner, edge2.m_Start, topOwner, owner, out maybeDeadEnd.x))
						{
							Node node2 = m_NodeData[edge2.m_Start];
							@bool.x = !MathUtils.Intersect(lot.xz, node2.m_Position.xz);
							if (@bool.x || MathUtils.Distance(lot.ab.xz, node2.m_Position.xz, out t) <= num4)
							{
								@float.x = 0f;
							}
						}
						if (IsDeadEnd(owner, edge2.m_End, topOwner, owner, out maybeDeadEnd.y))
						{
							Node node3 = m_NodeData[edge2.m_End];
							@bool.y = !MathUtils.Intersect(lot.xz, node3.m_Position.xz);
							if (@bool.y || MathUtils.Distance(lot.ab.xz, node3.m_Position.xz, out t) <= num4)
							{
								@float.y = 1f;
							}
						}
						@float = math.select(-1f, @float, @bool | !@bool.yx);
					}
				}
				else if (m_PrefabAreaGeometryData.HasComponent(prefabRef2.m_Prefab))
				{
					pedestrianIcon = (m_PrefabAreaGeometryData[prefabRef2.m_Prefab].m_Flags & Game.Areas.GeometryFlags.OnWaterSurface) == 0;
				}
				AddPathfindElements(ownedElements, nodeMap, m_SubLanes[owner], pedestrianIcon, isRoad, onlyExisting: false, optionalConnections, maybeDeadEnd, @float);
			}
			if (!m_SubObjects.HasBuffer(owner))
			{
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer8 = m_SubObjects[owner];
			for (int num5 = 0; num5 < dynamicBuffer8.Length; num5++)
			{
				Entity subObject = dynamicBuffer8[num5].m_SubObject;
				if (m_BuildingData.HasComponent(subObject))
				{
					continue;
				}
				AddPathfindElements(ownedElements, nodeMap, externalNodes, topOwner, subObject, lot, isRoad: false, isSubBuilding, isPlaceholder);
				if (m_SpawnLocationData.HasComponent(subObject))
				{
					Game.Objects.SpawnLocation spawnLocation = m_SpawnLocationData[subObject];
					PrefabRef prefabRef3 = m_PrefabRefData[subObject];
					if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef3.m_Prefab, out var componentData2))
					{
						PathfindElement value = new PathfindElement
						{
							m_Entity = subObject,
							m_Directions = true
						};
						value.m_StartNode = new PathNode(subObject, 2);
						value.m_MiddleNode = new PathNode(subObject, 1);
						value.m_EndNode = new PathNode(subObject, 0);
						switch (componentData2.m_ConnectionType)
						{
						case RouteConnectionType.Road:
							value.m_IconType = (byte)math.select(1, 6, componentData2.m_RoadTypes == RoadTypes.Bicycle);
							break;
						case RouteConnectionType.Pedestrian:
							value.m_IconType = 2;
							break;
						case RouteConnectionType.Air:
							value.m_IconType = (byte)math.select(1, 0, m_EditorMode);
							break;
						case RouteConnectionType.Parking:
							value.m_IconType = (byte)math.select(1, 2, componentData2.m_RoadTypes == RoadTypes.Bicycle);
							break;
						case RouteConnectionType.Track:
							value.m_IconType = 3;
							break;
						default:
							continue;
						}
						if (m_LaneData.HasComponent(spawnLocation.m_ConnectedLane1))
						{
							AddExternalNode(externalNodes, topOwner, spawnLocation.m_ConnectedLane1);
							value.m_StartNode = new PathNode(m_LaneData[spawnLocation.m_ConnectedLane1].m_MiddleNode, spawnLocation.m_CurvePosition1);
						}
						else if (componentData2.m_ConnectionType == RouteConnectionType.Pedestrian || (componentData2.m_ConnectionType == RouteConnectionType.Parking && componentData2.m_RoadTypes == RoadTypes.Bicycle))
						{
							value.m_CanIgnore = true;
						}
						value.m_SubConnection = (sbyte)(isSubBuilding ? 1 : 0);
						int length = ownedElements.Length;
						ownedElements.Add(in value);
						nodeMap.Add(value.m_StartNode.StripCurvePos(), length);
						nodeMap.Add(value.m_MiddleNode.StripCurvePos(), length);
						nodeMap.Add(value.m_EndNode.StripCurvePos(), length);
					}
				}
				else
				{
					if (!m_TakeoffLocationData.HasComponent(subObject))
					{
						continue;
					}
					AccessLane accessLane = m_AccessLaneData[subObject];
					RouteLane routeLane = m_RouteLaneData[subObject];
					PrefabRef prefabRef4 = m_PrefabRefData[subObject];
					RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef4.m_Prefab];
					PathfindElement value2 = new PathfindElement
					{
						m_Entity = subObject,
						m_Directions = true
					};
					value2.m_StartNode = new PathNode(subObject, 2);
					value2.m_MiddleNode = new PathNode(subObject, 1);
					value2.m_EndNode = new PathNode(subObject, 0);
					switch (routeConnectionData.m_AccessConnectionType)
					{
					case RouteConnectionType.Road:
						value2.m_IconType = (byte)math.select(1, 6, routeConnectionData.m_AccessRoadType == RoadTypes.Bicycle);
						break;
					case RouteConnectionType.Pedestrian:
						value2.m_IconType = 2;
						break;
					case RouteConnectionType.Air:
						value2.m_IconType = (byte)math.select(1, 0, m_EditorMode);
						break;
					default:
						continue;
					}
					bool num6 = m_LaneData.HasComponent(accessLane.m_Lane);
					bool flag = m_LaneData.HasComponent(routeLane.m_EndLane);
					if (num6 && flag)
					{
						AddExternalNode(externalNodes, topOwner, accessLane.m_Lane);
						AddExternalNode(externalNodes, topOwner, routeLane.m_EndLane);
					}
					if (num6)
					{
						value2.m_StartNode = new PathNode(m_LaneData[accessLane.m_Lane].m_MiddleNode, accessLane.m_CurvePos);
					}
					if (flag)
					{
						value2.m_EndNode = new PathNode(m_LaneData[routeLane.m_EndLane].m_MiddleNode, routeLane.m_EndCurvePos);
					}
					if (!num6 || !flag)
					{
						Game.Objects.Transform transform = m_TransformData[subObject];
						if (!MathUtils.Intersect(lot.xz, transform.m_Position.xz) || MathUtils.Distance(lot.ab.xz, transform.m_Position.xz, out t) <= 10f)
						{
							value2.m_Priority = 1;
						}
					}
					int length2 = ownedElements.Length;
					ownedElements.Add(in value2);
					nodeMap.Add(value2.m_StartNode.StripCurvePos(), length2);
					nodeMap.Add(value2.m_MiddleNode.StripCurvePos(), length2);
					nodeMap.Add(value2.m_EndNode.StripCurvePos(), length2);
				}
			}
		}

		private void AddPathfindElements(NativeList<PathfindElement> ownedElements, NativeParallelMultiHashMap<PathNode, int> nodeMap, DynamicBuffer<SubLane> subLanes, bool pedestrianIcon, bool isRoad, bool onlyExisting, bool optionalConnections, bool2 maybeDeadEnd, float2 errorLocation)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				PathfindElement value = new PathfindElement
				{
					m_Entity = subLane,
					m_IconLocation = 128,
					m_IconLocation2 = 128
				};
				bool2 @bool = false;
				bool flag = false;
				if (m_CarLaneData.HasComponent(subLane))
				{
					if (m_SlaveLaneData.HasComponent(subLane))
					{
						continue;
					}
					CarLane carLane = m_CarLaneData[subLane];
					value.m_Directions = new bool2(x: true, (carLane.m_Flags & CarLaneFlags.Twoway) != 0);
					value.m_IconType = (byte)math.select(1, 4, isRoad);
					if (errorLocation.x == 0.5f && (carLane.m_Flags & CarLaneFlags.Unsafe) == 0)
					{
						Curve curve = m_CurveData[subLane];
						float2 forward = math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz);
						float num = math.dot(y: math.normalizesafe(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz), x: MathUtils.Left(forward));
						flag = m_LeftHandTraffic == num < 0f && math.abs(num) > 0.5f;
					}
					if (m_PrefabRefData.TryGetComponent(subLane, out var componentData) && m_PrefabCarLaneData.TryGetComponent(componentData.m_Prefab, out var componentData2))
					{
						value.m_IconType = (byte)math.select(value.m_IconType, 5, (componentData2.m_RoadTypes & (RoadTypes.Car | RoadTypes.Watercraft)) == RoadTypes.Watercraft);
						value.m_IconType = (byte)math.select(value.m_IconType, 6, (componentData2.m_RoadTypes & (RoadTypes.Car | RoadTypes.Bicycle)) == RoadTypes.Bicycle);
					}
				}
				else if (m_PedestrianLaneData.HasComponent(subLane))
				{
					value.m_Directions = true;
					value.m_IconType = (byte)math.select(2, 4, isRoad);
					value.m_IconType = (byte)math.select(0, value.m_IconType, pedestrianIcon);
				}
				else if (m_TrackLaneData.HasComponent(subLane))
				{
					TrackLane trackLane = m_TrackLaneData[subLane];
					value.m_Directions = new bool2(x: true, (trackLane.m_Flags & TrackLaneFlags.Twoway) != 0);
					value.m_IconType = 3;
					value.m_Optional = (trackLane.m_Flags & TrackLaneFlags.Twoway) == 0;
					@bool.x = (trackLane.m_Flags & TrackLaneFlags.StartingLane) != 0;
					@bool.y = (trackLane.m_Flags & TrackLaneFlags.EndingLane) != 0;
				}
				else
				{
					if (!m_ConnectionLaneData.HasComponent(subLane))
					{
						continue;
					}
					ConnectionLane connectionLane = m_ConnectionLaneData[subLane];
					value.m_Optional = optionalConnections;
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						value.m_Directions = true;
						value.m_IconType = (byte)math.select(1, 4, isRoad);
						value.m_IconType = (byte)math.select(value.m_IconType, 5, (connectionLane.m_RoadTypes & (RoadTypes.Car | RoadTypes.Watercraft)) == RoadTypes.Watercraft);
						value.m_IconType = (byte)math.select(value.m_IconType, 6, (connectionLane.m_RoadTypes & (RoadTypes.Car | RoadTypes.Bicycle)) == RoadTypes.Bicycle);
					}
					else if ((connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						value.m_Directions = true;
						value.m_IconType = (byte)math.select(2, 4, isRoad);
						value.m_IconType = (byte)math.select(0, value.m_IconType, pedestrianIcon);
					}
					else
					{
						if ((connectionLane.m_Flags & ConnectionLaneFlags.Track) == 0)
						{
							continue;
						}
						value.m_Directions = true;
						value.m_IconType = 3;
					}
				}
				if (math.any(errorLocation >= 0f) && value.m_IconType != 0)
				{
					if (m_EdgeLaneData.HasComponent(subLane))
					{
						EdgeLane edgeLane = m_EdgeLaneData[subLane];
						bool2 x = (edgeLane.m_EdgeDelta.x == errorLocation) & (@bool.x | !maybeDeadEnd);
						bool2 x2 = (edgeLane.m_EdgeDelta.y == errorLocation) & (@bool.y | !maybeDeadEnd);
						if (math.any(x))
						{
							value.m_Priority = 1;
							value.m_IconLocation = 0;
							value.m_IconLocation2 = 0;
						}
						if (math.any(x2))
						{
							value.m_Priority = 1;
							value.m_IconLocation = (byte)math.select(255, 0, math.any(x));
							value.m_IconLocation2 = byte.MaxValue;
						}
					}
					else if (errorLocation.x == 0.5f && flag)
					{
						value.m_Priority = 1;
						value.m_IconLocation = 128;
						value.m_IconLocation2 = 128;
					}
				}
				Lane lane = m_LaneData[subLane];
				if (m_LaneConnectionData.TryGetComponent(subLane, out var componentData3))
				{
					if (m_LaneData.TryGetComponent(componentData3.m_StartLane, out var componentData4))
					{
						lane.m_StartNode = new PathNode(componentData4.m_MiddleNode, componentData3.m_StartPosition);
					}
					if (m_LaneData.TryGetComponent(componentData3.m_EndLane, out var componentData5))
					{
						lane.m_EndNode = new PathNode(componentData5.m_MiddleNode, componentData3.m_EndPosition);
					}
				}
				if (!onlyExisting || (nodeMap.ContainsKey(lane.m_StartNode.StripCurvePos()) && nodeMap.ContainsKey(lane.m_EndNode.StripCurvePos())))
				{
					value.m_StartNode = lane.m_StartNode;
					value.m_MiddleNode = lane.m_MiddleNode;
					value.m_EndNode = lane.m_EndNode;
					int length = ownedElements.Length;
					ownedElements.Add(in value);
					nodeMap.Add(lane.m_StartNode.StripCurvePos(), length);
					nodeMap.Add(lane.m_MiddleNode.StripCurvePos(), length);
					nodeMap.Add(lane.m_EndNode.StripCurvePos(), length);
				}
			}
		}

		private void AddExternalNode(NativeParallelHashSet<PathNode> externalNodes, Entity topOwner, Entity laneEntity)
		{
			Entity entity = laneEntity;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData) && !m_BuildingData.HasComponent(entity))
			{
				entity = componentData.m_Owner;
			}
			if (!(entity == topOwner))
			{
				Lane lane = m_LaneData[laneEntity];
				externalNodes.Add(lane.m_StartNode.StripCurvePos());
				externalNodes.Add(lane.m_MiddleNode.StripCurvePos());
				externalNodes.Add(lane.m_EndNode.StripCurvePos());
			}
		}

		private bool AddExternalNodes(NativeParallelHashSet<PathNode> externalNodes, Entity topOwner, Entity owner, Entity netEntity)
		{
			if (m_OwnerData.TryGetComponent(netEntity, out var componentData))
			{
				Entity owner2 = componentData.m_Owner;
				if (owner2 == owner)
				{
					return false;
				}
				while (m_OwnerData.TryGetComponent(owner2, out componentData) && !m_BuildingData.HasComponent(owner2))
				{
					owner2 = componentData.m_Owner;
				}
				if (owner2 == topOwner)
				{
					return false;
				}
			}
			if (m_SubLanes.HasBuffer(netEntity))
			{
				DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[netEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					Lane lane = m_LaneData[subLane];
					externalNodes.Add(lane.m_StartNode.StripCurvePos());
					externalNodes.Add(lane.m_MiddleNode.StripCurvePos());
					externalNodes.Add(lane.m_EndNode.StripCurvePos());
				}
			}
			return true;
		}

		private void UpdateSubnetConnectionWarnings(Entity owner)
		{
			NativeHashSet<Entity> resourceConnections = default(NativeHashSet<Entity>);
			Layer connectedLayers = Layer.None;
			Layer disconnectedLayers = Layer.None;
			if (m_SubNets.TryGetBuffer(owner, out var bufferData))
			{
				CheckConnectedLayers(ref connectedLayers, ref disconnectedLayers, ref resourceConnections, owner, bufferData, subBuilding: false);
			}
			if (m_SubObjects.TryGetBuffer(owner, out var bufferData2))
			{
				CheckConnectedLayers(ref connectedLayers, ref disconnectedLayers, ref resourceConnections, bufferData2, subBuilding: false);
			}
			Layer layer = connectedLayers | disconnectedLayers;
			if ((disconnectedLayers & (Layer.PowerlineLow | Layer.PowerlineHigh | Layer.WaterPipe | Layer.SewagePipe | Layer.StormwaterPipe)) != Layer.None && m_BuildingData.TryGetComponent(owner, out var componentData) && m_PrefabRefData.TryGetComponent(componentData.m_RoadEdge, out var componentData2))
			{
				NetData netData = m_PrefabNetData[componentData2.m_Prefab];
				connectedLayers = (Layer)((uint)connectedLayers | ((uint)netData.m_LocalConnectLayers & 0xFFFFFFE5u));
			}
			if ((disconnectedLayers & Layer.PowerlineHigh) != Layer.None && (connectedLayers & Layer.PowerlineLow) != Layer.None && m_ElectricityProducerData.HasComponent(owner) && m_TransformerData.HasComponent(owner))
			{
				disconnectedLayers = (Layer)((uint)disconnectedLayers & 0xFFFFFFFBu);
			}
			disconnectedLayers &= ~connectedLayers;
			if (layer != Layer.None)
			{
				if (bufferData.IsCreated)
				{
					UpdateConnectionWarnings(layer, connectedLayers, disconnectedLayers, resourceConnections, owner, bufferData);
				}
				if (bufferData2.IsCreated)
				{
					UpdateConnectionWarnings(layer, connectedLayers, disconnectedLayers, resourceConnections, bufferData2);
				}
			}
			if (resourceConnections.IsCreated)
			{
				resourceConnections.Dispose();
			}
			if (!bufferData.IsCreated)
			{
				return;
			}
			NativeHashMap<PathNode, Connection> nodeConnections = default(NativeHashMap<PathNode, Connection>);
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subNet = bufferData[i].m_SubNet;
				if (!m_ConnectedEdges.TryGetBuffer(subNet, out var bufferData3) || !m_SubLanes.TryGetBuffer(subNet, out var bufferData4))
				{
					continue;
				}
				bool flag = false;
				for (int j = 0; j < bufferData3.Length; j++)
				{
					if (!m_OwnerData.HasComponent(bufferData3[j].m_Edge))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					bool flag2 = IsNativeMapTile(m_NodeData[subNet].m_Position) || m_OutsideConnectionData.HasComponent(subNet);
					if (!flag2)
					{
						FillNodeConnections(bufferData4, ref nodeConnections);
						FillNodeConnections(bufferData3, ref nodeConnections, subNet);
					}
					CheckNodeConnections(bufferData3, nodeConnections, subNet, flag2, standaloneOnly: true);
					if (nodeConnections.IsCreated)
					{
						nodeConnections.Clear();
					}
				}
			}
			if (nodeConnections.IsCreated)
			{
				nodeConnections.Dispose();
			}
		}

		private void CheckConnectedLayers(ref Layer connectedLayers, ref Layer disconnectedLayers, ref NativeHashSet<Entity> resourceConnections, DynamicBuffer<Game.Objects.SubObject> subObjects, bool subBuilding)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				bool subBuilding2 = subBuilding || m_BuildingData.HasComponent(subObject);
				if (m_SubNets.TryGetBuffer(subObject, out var bufferData))
				{
					CheckConnectedLayers(ref connectedLayers, ref disconnectedLayers, ref resourceConnections, subObject, bufferData, subBuilding2);
				}
				if (m_SubObjects.TryGetBuffer(subObject, out var bufferData2))
				{
					CheckConnectedLayers(ref connectedLayers, ref disconnectedLayers, ref resourceConnections, bufferData2, subBuilding2);
				}
			}
		}

		private void CheckConnectedLayers(ref Layer connectedLayers, ref Layer disconnectedLayers, ref NativeHashSet<Entity> resourceConnections, Entity owner, DynamicBuffer<SubNet> subNets, bool subBuilding)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (!m_ConnectedEdges.HasBuffer(subNet))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[subNet];
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				if (m_ResourceConnectionData.HasComponent(subNet))
				{
					if (subBuilding || m_ServiceUpgradeData.HasComponent(subNet))
					{
						disconnectedLayers |= Layer.ResourceLine;
					}
					else
					{
						AddResourceConnections(ref resourceConnections, subNet);
					}
					continue;
				}
				DynamicBuffer<ConnectedEdge> connectedEdges = m_ConnectedEdges[subNet];
				Layer connectedOnce = netData.m_RequiredLayers;
				Layer connectedTwice = Layer.None;
				FindEdgeConnections(subNet, connectedEdges, owner, netData.m_RequiredLayers, ref connectedOnce, ref connectedTwice);
				if (m_OutsideConnectionData.HasComponent(subNet))
				{
					connectedTwice |= connectedOnce;
				}
				connectedLayers |= netData.m_RequiredLayers & connectedTwice;
				disconnectedLayers |= netData.m_RequiredLayers & ~connectedTwice;
				if ((connectedOnce & ~netData.m_RequiredLayers & ~connectedTwice) != Layer.None)
				{
					FindSecondaryConnections(subNet, connectedEdges, ref connectedTwice);
				}
				if (connectedOnce != Layer.None)
				{
					UpdateConnectionWarnings(subNet, Entity.Null, prefabRef.m_Prefab, connectedOnce, netData.m_RequiredLayers | connectedTwice);
				}
			}
		}

		private void AddResourceConnections(ref NativeHashSet<Entity> resourceConnections, Entity subNet)
		{
			if (!resourceConnections.IsCreated)
			{
				resourceConnections = new NativeHashSet<Entity>(100, Allocator.Temp);
			}
			if (!resourceConnections.Add(subNet) || m_ConnectedEdges[subNet].Length == 0)
			{
				return;
			}
			NativeList<Entity> nativeList = new NativeList<Entity>(10, Allocator.Temp) { in subNet };
			while (nativeList.Length != 0)
			{
				subNet = nativeList[nativeList.Length - 1];
				nativeList.RemoveAt(nativeList.Length - 1);
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[subNet];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					ConnectedEdge connectedEdge = dynamicBuffer[i];
					Edge edge = m_EdgeData[connectedEdge.m_Edge];
					DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_ConnectedNodes[connectedEdge.m_Edge];
					if (resourceConnections.Add(edge.m_Start))
					{
						nativeList.Add(in edge.m_Start);
					}
					if (resourceConnections.Add(edge.m_End))
					{
						nativeList.Add(in edge.m_End);
					}
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Entity value = dynamicBuffer2[j].m_Node;
						if (resourceConnections.Add(value))
						{
							nativeList.Add(in value);
						}
					}
				}
			}
		}

		private void UpdateConnectionWarnings(Layer allLayers, Layer connectedLayers, Layer disconnectedLayers, NativeHashSet<Entity> resourceConnections, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_SubNets.TryGetBuffer(subObject, out var bufferData))
				{
					UpdateConnectionWarnings(allLayers, connectedLayers, disconnectedLayers, resourceConnections, subObject, bufferData);
				}
				if (m_SubObjects.TryGetBuffer(subObject, out var bufferData2))
				{
					UpdateConnectionWarnings(allLayers, connectedLayers, disconnectedLayers, resourceConnections, bufferData2);
				}
			}
		}

		private void UpdateConnectionWarnings(Layer allLayers, Layer connectedLayers, Layer disconnectedLayers, NativeHashSet<Entity> resourceConnections, Entity owner, DynamicBuffer<SubNet> subNets)
		{
			for (int i = 0; i < subNets.Length; i++)
			{
				Entity subNet = subNets[i].m_SubNet;
				if (!m_ConnectedEdges.HasBuffer(subNet))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[subNet];
				Layer layer = m_PrefabNetData[prefabRef.m_Prefab].m_RequiredLayers;
				if (m_ResourceConnectionData.HasComponent(subNet))
				{
					layer |= Layer.ResourceLine;
				}
				layer &= allLayers;
				if (layer != Layer.None)
				{
					Layer layer2 = connectedLayers | ~disconnectedLayers;
					if (resourceConnections.IsCreated && resourceConnections.Contains(subNet))
					{
						layer2 |= Layer.ResourceLine;
					}
					UpdateConnectionWarnings(owner, subNet, prefabRef.m_Prefab, layer, layer2);
				}
			}
		}

		private void UpdateNodeConnectionWarnings(Entity node, bool allConnected)
		{
			PrefabRef prefabRef = m_PrefabRefData[node];
			NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
			DynamicBuffer<ConnectedEdge> connectedEdges = m_ConnectedEdges[node];
			Layer connectedOnce = Layer.None;
			Layer connectedTwice = Layer.None;
			FindEdgeConnections(node, connectedEdges, netData.m_RequiredLayers, ref connectedOnce, ref connectedTwice);
			Layer layer = netData.m_RequiredLayers | connectedOnce;
			allConnected |= m_OutsideConnectionData.HasComponent(node);
			if ((layer & ~connectedTwice) != Layer.None)
			{
				FindSecondaryConnections(node, connectedEdges, ref connectedTwice);
				if (allConnected)
				{
					connectedTwice |= connectedOnce;
				}
			}
			if (layer != Layer.None)
			{
				UpdateConnectionWarnings(node, Entity.Null, prefabRef.m_Prefab, layer, connectedTwice);
			}
			if (m_SubLanes.TryGetBuffer(node, out var bufferData))
			{
				NativeHashMap<PathNode, Connection> nodeConnections = default(NativeHashMap<PathNode, Connection>);
				if (!allConnected)
				{
					FillNodeConnections(bufferData, ref nodeConnections);
					FillNodeConnections(connectedEdges, ref nodeConnections, node);
				}
				CheckNodeConnections(connectedEdges, nodeConnections, node, allConnected, standaloneOnly: false);
				if (nodeConnections.IsCreated)
				{
					nodeConnections.Dispose();
				}
			}
		}

		private void FillNodeConnections(DynamicBuffer<SubLane> subLanes, ref NativeHashMap<PathNode, Connection> nodeConnections)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				Connection connection = default(Connection);
				if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && !m_SlaveLaneData.HasComponent(subLane))
				{
					connection.m_RoadTypes = componentData.m_RoadTypes;
				}
				if (m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					connection.m_TrackTypes = componentData2.m_TrackTypes;
				}
				if (connection.m_RoadTypes != RoadTypes.None || connection.m_TrackTypes != TrackTypes.None)
				{
					Lane lane = m_LaneData[subLane];
					AddNodeConnection(lane.m_StartNode, connection, ref nodeConnections);
					AddNodeConnection(lane.m_EndNode, connection, ref nodeConnections);
				}
			}
		}

		private void FillNodeConnections(DynamicBuffer<ConnectedEdge> connectedEdges, ref NativeHashMap<PathNode, Connection> nodeConnections, Entity node)
		{
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				Entity edge = connectedEdges[i].m_Edge;
				if (!m_SubLanes.TryGetBuffer(edge, out var bufferData))
				{
					continue;
				}
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == node)
				{
					FillNodeConnections(edge, bufferData, ref nodeConnections, 0f);
					continue;
				}
				if (edge2.m_End == node)
				{
					FillNodeConnections(edge, bufferData, ref nodeConnections, 1f);
					continue;
				}
				FillNodeConnections(edge, bufferData, ref nodeConnections, connectedEdges);
				if (m_SubLanes.TryGetBuffer(edge2.m_Start, out bufferData))
				{
					FillNodeConnections(edge, bufferData, ref nodeConnections, connectedEdges);
				}
				if (m_SubLanes.TryGetBuffer(edge2.m_End, out bufferData))
				{
					FillNodeConnections(edge, bufferData, ref nodeConnections, connectedEdges);
				}
			}
		}

		private void FillNodeConnections(Entity edgeEntity, DynamicBuffer<SubLane> subLanes, ref NativeHashMap<PathNode, Connection> nodeConnections, float edgeDelta)
		{
			PathNode other = new PathNode(edgeEntity, 0);
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				if (!m_EdgeLaneData.TryGetComponent(subLane, out var componentData))
				{
					continue;
				}
				Lane lane = m_LaneData[subLane];
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				Connection connection = default(Connection);
				if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && !m_SlaveLaneData.HasComponent(subLane))
				{
					if (componentData.m_EdgeDelta.x == edgeDelta && !lane.m_StartNode.OwnerEquals(other))
					{
						connection.m_RoadTypes2 = componentData2.m_RoadTypes;
					}
					if (componentData.m_EdgeDelta.y == edgeDelta && !lane.m_EndNode.OwnerEquals(other))
					{
						connection.m_RoadTypes2 = componentData2.m_RoadTypes;
					}
				}
				if (m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					if (componentData.m_EdgeDelta.x == edgeDelta && !lane.m_StartNode.OwnerEquals(other))
					{
						connection.m_TrackTypes2 = componentData3.m_TrackTypes;
					}
					if (componentData.m_EdgeDelta.y == edgeDelta && !lane.m_EndNode.OwnerEquals(other))
					{
						connection.m_TrackTypes2 = componentData3.m_TrackTypes;
					}
				}
				if (connection.m_RoadTypes2 != RoadTypes.None || connection.m_TrackTypes2 != TrackTypes.None)
				{
					if (componentData.m_EdgeDelta.x == edgeDelta)
					{
						AddNodeConnection(lane.m_StartNode, connection, ref nodeConnections);
					}
					if (componentData.m_EdgeDelta.y == edgeDelta)
					{
						AddNodeConnection(lane.m_EndNode, connection, ref nodeConnections);
					}
				}
			}
		}

		private void FillNodeConnections(Entity edgeEntity, DynamicBuffer<SubLane> subLanes, ref NativeHashMap<PathNode, Connection> nodeConnections, DynamicBuffer<ConnectedEdge> connectedEdges)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				if (m_EdgeLaneData.HasComponent(subLane))
				{
					continue;
				}
				Lane lane = m_LaneData[subLane];
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				Connection connection = default(Connection);
				if (!m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || m_SlaveLaneData.HasComponent(subLane))
				{
					continue;
				}
				connection.m_RoadTypes = componentData.m_RoadTypes;
				for (int j = 0; j < connectedEdges.Length; j++)
				{
					Entity edge = connectedEdges[j].m_Edge;
					if (edge != edgeEntity)
					{
						PathNode pathNode = new PathNode(edge, 0);
						if (pathNode.OwnerEquals(lane.m_StartNode))
						{
							AddNodeConnection(lane.m_StartNode, connection, ref nodeConnections);
						}
						if (pathNode.OwnerEquals(lane.m_EndNode))
						{
							AddNodeConnection(lane.m_EndNode, connection, ref nodeConnections);
						}
					}
				}
			}
		}

		private void AddNodeConnection(PathNode pathNode, Connection connection, ref NativeHashMap<PathNode, Connection> nodeConnections)
		{
			if (!nodeConnections.IsCreated)
			{
				nodeConnections = new NativeHashMap<PathNode, Connection>(100, Allocator.Temp);
			}
			if (nodeConnections.TryGetValue(pathNode, out var item))
			{
				connection.m_RoadTypes |= item.m_RoadTypes | (item.m_RoadTypes2 & connection.m_RoadTypes2);
				connection.m_RoadTypes2 |= item.m_RoadTypes2;
				connection.m_TrackTypes |= item.m_TrackTypes | (item.m_TrackTypes2 & connection.m_TrackTypes2);
				connection.m_TrackTypes2 |= item.m_TrackTypes2;
				if (connection.m_RoadTypes != item.m_RoadTypes || connection.m_RoadTypes2 != item.m_RoadTypes2 || connection.m_TrackTypes != item.m_TrackTypes || connection.m_TrackTypes2 != item.m_TrackTypes2)
				{
					nodeConnections[pathNode] = connection;
				}
			}
			else
			{
				nodeConnections.Add(pathNode, connection);
			}
		}

		private void CheckNodeConnections(DynamicBuffer<ConnectedEdge> connectedEdges, NativeHashMap<PathNode, Connection> nodeConnections, Entity node, bool allConnected, bool standaloneOnly)
		{
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				Entity edge = connectedEdges[i].m_Edge;
				if ((!standaloneOnly || !m_OwnerData.HasComponent(edge)) && m_SubLanes.TryGetBuffer(edge, out var bufferData))
				{
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == node)
					{
						CheckNodeConnections(bufferData, nodeConnections, 0f, allConnected);
					}
					else if (edge2.m_End == node)
					{
						CheckNodeConnections(bufferData, nodeConnections, 1f, allConnected);
					}
				}
			}
		}

		private void CheckNodeConnections(DynamicBuffer<SubLane> subLanes, NativeHashMap<PathNode, Connection> nodeConnections, float edgeDelta, bool allConnected)
		{
			for (int i = 0; i < subLanes.Length; i++)
			{
				Entity subLane = subLanes[i].m_SubLane;
				if (!m_EdgeLaneData.TryGetComponent(subLane, out var componentData))
				{
					continue;
				}
				Lane lane = m_LaneData[subLane];
				Curve curve = m_CurveData[subLane];
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				if (m_CarLaneData.TryGetComponent(subLane, out var componentData2) && !m_SlaveLaneData.HasComponent(subLane) && m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					if (componentData.m_EdgeDelta.x == edgeDelta)
					{
						if (allConnected || (componentData2.m_Flags & CarLaneFlags.Twoway) != 0 || (nodeConnections.IsCreated && nodeConnections.TryGetValue(lane.m_StartNode, out var item) && CheckRoadTypes(item.m_RoadTypes, componentData3.m_RoadTypes)))
						{
							m_IconCommandBuffer.Remove(subLane, m_TrafficConfigurationData.m_DeadEndNotification, Entity.Null);
						}
						else
						{
							m_IconCommandBuffer.Add(subLane, m_TrafficConfigurationData.m_DeadEndNotification, curve.m_Bezier.a, IconPriority.Warning);
						}
					}
					if (componentData.m_EdgeDelta.y == edgeDelta)
					{
						if (allConnected || (componentData2.m_Flags & CarLaneFlags.Twoway) != 0 || (nodeConnections.IsCreated && nodeConnections.TryGetValue(lane.m_EndNode, out var item2) && CheckRoadTypes(item2.m_RoadTypes, componentData3.m_RoadTypes)))
						{
							m_IconCommandBuffer.Remove(subLane, m_TrafficConfigurationData.m_DeadEndNotification, Entity.Null, IconFlags.SecondaryLocation);
						}
						else
						{
							m_IconCommandBuffer.Add(subLane, m_TrafficConfigurationData.m_DeadEndNotification, curve.m_Bezier.d, IconPriority.Warning, IconClusterLayer.Default, IconFlags.SecondaryLocation);
						}
					}
				}
				if (!m_TrackLaneData.TryGetComponent(subLane, out var componentData4) || !m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData5))
				{
					continue;
				}
				if (componentData.m_EdgeDelta.x == edgeDelta)
				{
					if (allConnected || (componentData4.m_Flags & TrackLaneFlags.Twoway) != 0 || (nodeConnections.IsCreated && nodeConnections.TryGetValue(lane.m_StartNode, out var item3) && (item3.m_TrackTypes & componentData5.m_TrackTypes) == componentData5.m_TrackTypes))
					{
						m_IconCommandBuffer.Remove(subLane, m_TrafficConfigurationData.m_TrackConnectionNotification, Entity.Null);
					}
					else
					{
						m_IconCommandBuffer.Add(subLane, m_TrafficConfigurationData.m_TrackConnectionNotification, curve.m_Bezier.a, IconPriority.Warning);
					}
				}
				if (componentData.m_EdgeDelta.y == edgeDelta)
				{
					if (allConnected || (componentData4.m_Flags & TrackLaneFlags.Twoway) != 0 || (nodeConnections.IsCreated && nodeConnections.TryGetValue(lane.m_EndNode, out var item4) && (item4.m_TrackTypes & componentData5.m_TrackTypes) == componentData5.m_TrackTypes))
					{
						m_IconCommandBuffer.Remove(subLane, m_TrafficConfigurationData.m_TrackConnectionNotification, Entity.Null, IconFlags.SecondaryLocation);
					}
					else
					{
						m_IconCommandBuffer.Add(subLane, m_TrafficConfigurationData.m_TrackConnectionNotification, curve.m_Bezier.d, IconPriority.Warning, IconClusterLayer.Default, IconFlags.SecondaryLocation);
					}
				}
			}
		}

		private bool CheckRoadTypes(RoadTypes connected, RoadTypes required)
		{
			RoadTypes roadTypes = connected & required;
			if (roadTypes != RoadTypes.None)
			{
				return (roadTypes & ~RoadTypes.Bicycle) == (required & ~RoadTypes.Bicycle);
			}
			return false;
		}

		private void FindEdgeConnections(Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, Layer nodeLayers, ref Layer connectedOnce, ref Layer connectedTwice)
		{
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				Entity edge = connectedEdges[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				PrefabRef prefabRef = m_PrefabRefData[edge];
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				Layer layer = netData.m_RequiredLayers | (nodeLayers & netData.m_LocalConnectLayers);
				connectedTwice |= ((edge2.m_Start == node || edge2.m_End == node) ? (connectedOnce & layer) : layer);
				connectedOnce |= layer;
			}
		}

		private void FindEdgeConnections(Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, Entity owner, Layer nodeLayers, ref Layer connectedOnce, ref Layer connectedTwice)
		{
			Layer layer = Layer.None;
			Layer layer2 = Layer.None;
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				Entity edge = connectedEdges[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				PrefabRef prefabRef = m_PrefabRefData[edge];
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				Layer layer3 = netData.m_RequiredLayers | (nodeLayers & netData.m_LocalConnectLayers);
				if (m_OwnerData.TryGetComponent(edge, out var componentData) && componentData.m_Owner == owner)
				{
					connectedTwice |= ((edge2.m_Start == node || edge2.m_End == node) ? (layer & layer3) : layer3);
					layer |= layer3;
				}
				else
				{
					connectedTwice |= ((edge2.m_Start == node || edge2.m_End == node) ? (layer2 & layer3) : layer3);
					layer2 |= layer3;
				}
			}
			connectedTwice |= (layer | connectedOnce) & layer2;
			connectedOnce |= layer | layer2;
		}

		private void FindSecondaryConnections(Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, ref Layer connected)
		{
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				Entity edge = connectedEdges[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				PrefabRef prefabRef = m_PrefabRefData[edge];
				NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
				if (edge2.m_Start == node)
				{
					DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[edge];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						ConnectedNode connectedNode = dynamicBuffer[j];
						if (connectedNode.m_CurvePosition <= 0.5f)
						{
							PrefabRef prefabRef2 = m_PrefabRefData[connectedNode.m_Node];
							NetData netData2 = m_PrefabNetData[prefabRef2.m_Prefab];
							connected |= netData.m_RequiredLayers & netData2.m_RequiredLayers;
						}
					}
				}
				else
				{
					if (!(edge2.m_End == node))
					{
						continue;
					}
					DynamicBuffer<ConnectedNode> dynamicBuffer2 = m_ConnectedNodes[edge];
					for (int k = 0; k < dynamicBuffer2.Length; k++)
					{
						ConnectedNode connectedNode2 = dynamicBuffer2[k];
						if (connectedNode2.m_CurvePosition >= 0.5f)
						{
							PrefabRef prefabRef3 = m_PrefabRefData[connectedNode2.m_Node];
							NetData netData3 = m_PrefabNetData[prefabRef3.m_Prefab];
							connected |= netData.m_RequiredLayers & netData3.m_RequiredLayers;
						}
					}
				}
			}
		}

		private void UpdateConnectionWarnings(Entity owner, Entity subNet, Entity prefab, Layer required, Layer connected)
		{
			if ((required & (Layer.WaterPipe | Layer.SewagePipe)) != Layer.None)
			{
				Layer layer = required & ~connected;
				UpdateConnectionWarning(owner, subNet, m_WaterPipeParameterData.m_WaterPipeNotConnectedNotification, (layer & Layer.WaterPipe) != 0);
				UpdateConnectionWarning(owner, subNet, m_WaterPipeParameterData.m_SewagePipeNotConnectedNotification, (layer & Layer.SewagePipe) != 0);
			}
			if ((required & (Layer.PowerlineLow | Layer.PowerlineHigh)) != Layer.None)
			{
				Layer layer2 = required & ~connected;
				UpdateConnectionWarning(owner, subNet, m_ElectricityParameterData.m_LowVoltageNotConnectedPrefab, (layer2 & Layer.PowerlineLow) != 0);
				UpdateConnectionWarning(owner, subNet, m_ElectricityParameterData.m_HighVoltageNotConnectedPrefab, (layer2 & Layer.PowerlineHigh) != 0);
			}
			if ((required & Layer.ResourceLine) != Layer.None && m_PrefabResourceConnectionData.TryGetComponent(prefab, out var componentData) && componentData.m_ConnectionWarningNotification != Entity.Null)
			{
				Layer layer3 = required & ~connected;
				UpdateConnectionWarning(owner, subNet, componentData.m_ConnectionWarningNotification, (layer3 & Layer.ResourceLine) != 0);
			}
		}

		private void UpdateConnectionWarning(Entity owner, Entity subNet, Entity icon, bool active)
		{
			if (!(icon != Entity.Null))
			{
				return;
			}
			if (subNet != Entity.Null)
			{
				if (active)
				{
					m_IconCommandBuffer.Add(owner, icon, IconPriority.Warning, IconClusterLayer.Default, IconFlags.TargetLocation, subNet);
				}
				else
				{
					m_IconCommandBuffer.Remove(owner, icon, subNet);
				}
			}
			else if (active)
			{
				m_IconCommandBuffer.Add(owner, icon, IconPriority.Warning);
			}
			else
			{
				m_IconCommandBuffer.Remove(owner, icon);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> __Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneConnection> __Game_Net_LaneConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceConnection> __Game_Net_ResourceConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccessLane> __Game_Routes_AccessLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityProducer> __Game_Buildings_ElectricityProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Transformer> __Game_Buildings_Transformer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MapTile> __Game_Areas_MapTile_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceConnectionData> __Game_Prefabs_ResourceConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<IconElement> __Game_Notifications_IconElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadConnectionUpdated>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<TrackLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<ConnectionLane>(isReadOnly: true);
			__Game_Net_LaneConnection_RO_ComponentLookup = state.GetComponentLookup<LaneConnection>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<OutsideConnection>(isReadOnly: true);
			__Game_Net_ResourceConnection_RO_ComponentLookup = state.GetComponentLookup<ResourceConnection>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Routes_TakeoffLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TakeoffLocation>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentLookup = state.GetComponentLookup<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Buildings_ElectricityProducer_RO_ComponentLookup = state.GetComponentLookup<ElectricityProducer>(isReadOnly: true);
			__Game_Buildings_Transformer_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Transformer>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentLookup = state.GetComponentLookup<MapTile>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_ResourceConnectionData_RO_ComponentLookup = state.GetComponentLookup<ResourceConnectionData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<SubNet>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferLookup = state.GetBufferLookup<IconElement>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private IconCommandSystem m_IconCommandSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Areas.UpdateCollectSystem m_AreaUpdateCollectSystem;

	private SearchSystem m_NetSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_UpdateQuery;

	private EntityQuery m_NewGameQuery;

	private EntityQuery m_WaterConfigQuery;

	private EntityQuery m_ElectricityConfigQuery;

	private EntityQuery m_TrafficConfigQuery;

	private bool m_IsNewGame;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_AreaUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Areas.UpdateCollectSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_UpdateQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<RoadConnectionUpdated>(),
				ComponentType.ReadOnly<Game.Common.Event>()
			}
		});
		m_NewGameQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>(),
				ComponentType.ReadOnly<Game.Routes.TakeoffLocation>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_WaterConfigQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		m_ElectricityConfigQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>());
		m_TrafficConfigQuery = GetEntityQuery(ComponentType.ReadOnly<TrafficConfigurationData>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_IsNewGame = serializationContext.purpose == Purpose.NewGame;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = (m_IsNewGame ? m_NewGameQuery : m_UpdateQuery);
		m_IsNewGame = false;
		bool flag = !entityQuery.IsEmptyIgnoreFilter;
		bool mapTilesUpdated = m_AreaUpdateCollectSystem.mapTilesUpdated;
		if (flag || mapTilesUpdated)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(32, Allocator.TempJob);
			JobHandle job = base.Dependency;
			if (flag)
			{
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				JobHandle jobHandle = IJobExtensions.Schedule(new CollectOwnersJob
				{
					m_Chunks = chunks,
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_RoadConnectionUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
					m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
					m_Owners = nativeList
				}, JobHandle.CombineDependencies(job, outJobHandle));
				chunks.Dispose(jobHandle);
				job = jobHandle;
			}
			if (mapTilesUpdated)
			{
				JobHandle dependencies;
				JobHandle dependencies2;
				JobHandle dependencies3;
				JobHandle jobHandle2 = IJobExtensions.Schedule(new CollectOwnersJob2
				{
					m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
					m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
					m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
					m_Bounds = m_AreaUpdateCollectSystem.GetUpdatedMapTileBounds(out dependencies3),
					m_Owners = nativeList
				}, JobUtils.CombineDependencies(job, dependencies, dependencies2, dependencies3));
				m_NetSearchSystem.AddNetSearchTreeReader(jobHandle2);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle2);
				m_AreaUpdateCollectSystem.AddMapTileBoundsReader(jobHandle2);
				job = jobHandle2;
			}
			JobHandle dependencies4;
			JobHandle jobHandle3 = new CheckOwnersJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_Owners = nativeList.AsDeferredJobArray(),
				m_WaterPipeParameterData = GetConfigData<WaterPipeParameterData>(m_WaterConfigQuery),
				m_ElectricityParameterData = GetConfigData<ElectricityParameterData>(m_ElectricityConfigQuery),
				m_TrafficConfigurationData = GetConfigData<TrafficConfigurationData>(m_TrafficConfigQuery),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies4),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ResourceConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TakeoffLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TakeoffLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Transformer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MapTileData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabResourceConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_IconElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferLookup, ref base.CheckedStateRef)
			}.Schedule(nativeList, 1, JobHandle.CombineDependencies(job, dependencies4));
			nativeList.Dispose(jobHandle3);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle3);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle3);
			base.Dependency = jobHandle3;
		}
	}

	private T GetConfigData<T>(EntityQuery query) where T : unmanaged, IComponentData
	{
		if (query.IsEmptyIgnoreFilter)
		{
			return default(T);
		}
		return query.GetSingleton<T>();
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
	public ConnectionWarningSystem()
	{
	}
}
