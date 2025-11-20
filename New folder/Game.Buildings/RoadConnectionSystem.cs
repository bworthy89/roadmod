using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Audio;
using Game.Common;
using Game.Effects;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class RoadConnectionSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckRoadConnectionJob : IJobChunk
	{
		private struct CheckRoadConnectionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public EdgeGeometry m_EdgeGeometry;

			public EdgeNodeGeometry m_StartGeometry;

			public EdgeNodeGeometry m_EndGeometry;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public NativeQueue<Entity>.ParallelWriter m_ReplaceRoadConnectionQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity buildingEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || !m_BuildingData.HasComponent(buildingEntity))
				{
					return;
				}
				Transform transform = m_TransformData[buildingEntity];
				PrefabRef prefabRef = m_PrefabRefData[buildingEntity];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
				if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.NoRoadConnection) != 0)
				{
					return;
				}
				float3 position = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
				bool flag = (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.CanBeOnRoad | Game.Prefabs.BuildingFlags.CanBeOnRoadArea)) != 0;
				bool flag2 = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.CanBeRoadSide) != 0;
				if (flag && !flag2)
				{
					position = transform.m_Position;
				}
				if (!MathUtils.Intersect(m_Bounds.xz, position.xz))
				{
					return;
				}
				float maxDistance = 8.4f;
				bool isOnRoad = false;
				CheckDistance(m_EdgeGeometry, m_StartGeometry, m_EndGeometry, position, flag, ref maxDistance, ref isOnRoad);
				if (!(maxDistance < 8.4f))
				{
					return;
				}
				Building building = m_BuildingData[buildingEntity];
				if (building.m_RoadEdge != Entity.Null)
				{
					EdgeGeometry edgeGeometry = m_EdgeGeometryData[building.m_RoadEdge];
					EdgeNodeGeometry geometry = m_StartNodeGeometryData[building.m_RoadEdge].m_Geometry;
					EdgeNodeGeometry geometry2 = m_EndNodeGeometryData[building.m_RoadEdge].m_Geometry;
					float maxDistance2 = 8.4f;
					bool isOnRoad2 = false;
					CheckDistance(edgeGeometry, geometry, geometry2, position, flag, ref maxDistance2, ref isOnRoad2);
					if (maxDistance < maxDistance2)
					{
						m_ReplaceRoadConnectionQueue.Enqueue(buildingEntity);
					}
				}
				else
				{
					m_ReplaceRoadConnectionQueue.Enqueue(buildingEntity);
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> m_EdgeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> m_StartNodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> m_EndNodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedBuilding> m_ConnectedBuildingType;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		public NativeQueue<Entity>.ParallelWriter m_ReplaceRoadConnectionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Objects.SpawnLocation> nativeArray = chunk.GetNativeArray(ref m_SpawnLocationType);
			BufferAccessor<ConnectedBuilding> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedBuildingType);
			if (bufferAccessor.Length != 0)
			{
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					DynamicBuffer<ConnectedBuilding> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						m_ReplaceRoadConnectionQueue.Enqueue(dynamicBuffer[j].m_Building);
					}
				}
				if (!chunk.Has(ref m_DeletedType))
				{
					NativeArray<EdgeGeometry> nativeArray2 = chunk.GetNativeArray(ref m_EdgeGeometryType);
					NativeArray<StartNodeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_StartNodeGeometryType);
					NativeArray<EndNodeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_EndNodeGeometryType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						EdgeGeometry edgeGeometry = nativeArray2[k];
						EdgeNodeGeometry geometry = nativeArray3[k].m_Geometry;
						EdgeNodeGeometry geometry2 = nativeArray4[k].m_Geometry;
						CheckRoadConnectionIterator iterator = new CheckRoadConnectionIterator
						{
							m_Bounds = MathUtils.Expand(edgeGeometry.m_Bounds | geometry.m_Bounds | geometry2.m_Bounds, 8.4f),
							m_EdgeGeometry = edgeGeometry,
							m_StartGeometry = geometry,
							m_EndGeometry = geometry2,
							m_BuildingData = m_BuildingData,
							m_TransformData = m_TransformData,
							m_PrefabRefData = m_PrefabRefData,
							m_PrefabBuildingData = m_PrefabBuildingData,
							m_EdgeGeometryData = m_EdgeGeometryData,
							m_StartNodeGeometryData = m_StartNodeGeometryData,
							m_EndNodeGeometryData = m_EndNodeGeometryData,
							m_ReplaceRoadConnectionQueue = m_ReplaceRoadConnectionQueue
						};
						m_ObjectSearchTree.Iterate(ref iterator);
					}
				}
			}
			else if (nativeArray.Length != 0)
			{
				NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
				for (int l = 0; l < nativeArray5.Length; l++)
				{
					Entity entity = nativeArray5[l];
					Owner componentData;
					while (m_OwnerData.TryGetComponent(entity, out componentData))
					{
						entity = componentData.m_Owner;
						if (m_BuildingData.HasComponent(entity))
						{
							m_ReplaceRoadConnectionQueue.Enqueue(entity);
						}
					}
				}
			}
			else
			{
				NativeArray<Entity> nativeArray6 = chunk.GetNativeArray(m_EntityType);
				for (int m = 0; m < nativeArray6.Length; m++)
				{
					m_ReplaceRoadConnectionQueue.Enqueue(nativeArray6[m]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillReplacementListJob : IJob
	{
		public NativeQueue<Entity> m_ReplaceRoadConnectionQueue;

		public NativeList<ReplaceRoad> m_ReplaceRoadConnection;

		public void Execute()
		{
			int count = m_ReplaceRoadConnectionQueue.Count;
			m_ReplaceRoadConnection.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_ReplaceRoadConnection[i] = new ReplaceRoad(m_ReplaceRoadConnectionQueue.Dequeue());
			}
			m_ReplaceRoadConnection.Sort();
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_ReplaceRoadConnection.Length)
			{
				ReplaceRoad value = m_ReplaceRoadConnection[num++];
				if (value.m_Building != entity)
				{
					m_ReplaceRoadConnection[num2++] = value;
					entity = value.m_Building;
				}
			}
			if (num2 < m_ReplaceRoadConnection.Length)
			{
				m_ReplaceRoadConnection.RemoveRange(num2, m_ReplaceRoadConnection.Length - num2);
			}
		}
	}

	private struct ReplaceRoad : IComparable<ReplaceRoad>
	{
		public Entity m_Building;

		public Entity m_NewRoad;

		public float3 m_FrontPos;

		public float m_CurvePos;

		public bool m_Deleted;

		public bool m_Required;

		public ReplaceRoad(Entity building)
		{
			m_Building = building;
			m_NewRoad = Entity.Null;
			m_FrontPos = default(float3);
			m_CurvePos = 0f;
			m_Deleted = false;
			m_Required = false;
		}

		public int CompareTo(ReplaceRoad other)
		{
			return m_Building.Index - other.m_Building.Index;
		}
	}

	[BurstCompile]
	private struct FindRoadConnectionJob : IJobParallelForDefer
	{
		public struct FindRoadConnectionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float m_MinDistance;

			public float m_BestCurvePos;

			public Entity m_BestRoad;

			public float3 m_FrontPosition;

			public bool m_CanBeOnRoad;

			public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

			public ComponentLookup<Deleted> m_DeletedData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) && !m_DeletedData.HasComponent(edgeEntity))
				{
					CheckEdge(edgeEntity);
				}
			}

			public void CheckEdge(Entity edgeEntity)
			{
				if (!m_ConnectedBuildings.HasBuffer(edgeEntity))
				{
					return;
				}
				NetCompositionData componentData = default(NetCompositionData);
				if (m_CompositionData.TryGetComponent(edgeEntity, out var componentData2) && m_PrefabNetCompositionData.TryGetComponent(componentData2.m_Edge, out componentData) && (componentData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) != 0)
				{
					return;
				}
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[edgeEntity];
				EdgeNodeGeometry geometry = m_StartNodeGeometryData[edgeEntity].m_Geometry;
				EdgeNodeGeometry geometry2 = m_EndNodeGeometryData[edgeEntity].m_Geometry;
				float maxDistance = m_MinDistance;
				bool isOnRoad = false;
				CheckDistance(edgeGeometry, geometry, geometry2, m_FrontPosition, m_CanBeOnRoad, ref maxDistance, ref isOnRoad);
				if (!(maxDistance < m_MinDistance))
				{
					return;
				}
				Curve curve = m_CurveData[edgeEntity];
				MathUtils.Distance(curve.m_Bezier.xz, m_FrontPosition.xz, out var t);
				if (!m_CanBeOnRoad || !isOnRoad)
				{
					float3 @float = MathUtils.Position(curve.m_Bezier, t);
					if ((((math.dot(MathUtils.Right(MathUtils.Tangent(curve.m_Bezier, t).xz), m_FrontPosition.xz - @float.xz) >= 0f) ? componentData.m_Flags.m_Right : componentData.m_Flags.m_Left) & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) != 0)
					{
						return;
					}
				}
				m_Bounds = new Bounds3(m_FrontPosition - maxDistance, m_FrontPosition + maxDistance);
				m_MinDistance = maxDistance;
				m_BestCurvePos = t;
				m_BestRoad = edgeEntity;
			}
		}

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<BackSide> m_BackSideData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_UpdatedNetChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public NativeArray<ReplaceRoad> m_ReplaceRoadConnection;

		public void Execute(int index)
		{
			ReplaceRoad value = m_ReplaceRoadConnection[index];
			if (m_DeletedData.HasComponent(value.m_Building))
			{
				value.m_Deleted = true;
				m_ReplaceRoadConnection[index] = value;
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[value.m_Building];
			BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
			BackSide value2 = default(BackSide);
			if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.NoRoadConnection) == 0)
			{
				Transform transform = m_TransformData[value.m_Building];
				float3 @float = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
				value.m_Required = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0;
				bool flag = (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.CanBeOnRoad | Game.Prefabs.BuildingFlags.CanBeOnRoadArea)) != 0;
				bool flag2 = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.CanBeRoadSide) != 0;
				if (flag && !flag2)
				{
					@float = transform.m_Position;
				}
				FindRoadConnectionIterator iterator = new FindRoadConnectionIterator
				{
					m_Bounds = default(Bounds3),
					m_MinDistance = float.MaxValue,
					m_BestCurvePos = 0f,
					m_BestRoad = Entity.Null,
					m_FrontPosition = transform.m_Position,
					m_CanBeOnRoad = flag,
					m_ConnectedBuildings = m_ConnectedBuildings,
					m_CurveData = m_CurveData,
					m_CompositionData = m_CompositionData,
					m_EdgeGeometryData = m_EdgeGeometryData,
					m_StartNodeGeometryData = m_StartNodeGeometryData,
					m_EndNodeGeometryData = m_EndNodeGeometryData,
					m_PrefabNetCompositionData = m_PrefabNetCompositionData,
					m_DeletedData = m_DeletedData
				};
				if (m_SubNets.HasBuffer(value.m_Building))
				{
					DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_SubNets[value.m_Building];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity subNet = dynamicBuffer[i].m_SubNet;
						if (!m_DeletedData.HasComponent(subNet))
						{
							iterator.CheckEdge(subNet);
						}
					}
				}
				if (iterator.m_BestRoad == Entity.Null && m_TempData.HasComponent(value.m_Building))
				{
					Temp temp = m_TempData[value.m_Building];
					if (m_SubNets.HasBuffer(temp.m_Original))
					{
						DynamicBuffer<Game.Net.SubNet> dynamicBuffer2 = m_SubNets[temp.m_Original];
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							Entity subNet2 = dynamicBuffer2[j].m_SubNet;
							if (!m_DeletedData.HasComponent(subNet2))
							{
								iterator.CheckEdge(subNet2);
							}
						}
					}
				}
				bool flag3 = false;
				if (iterator.m_BestRoad == Entity.Null)
				{
					float num = 8.4f;
					iterator.m_Bounds = new Bounds3(@float - num, @float + num);
					iterator.m_MinDistance = num;
					iterator.m_FrontPosition = @float;
					m_NetSearchTree.Iterate(ref iterator);
					for (int k = 0; k < m_UpdatedNetChunks.Length; k++)
					{
						NativeArray<Entity> nativeArray = m_UpdatedNetChunks[k].GetNativeArray(m_EntityType);
						for (int l = 0; l < nativeArray.Length; l++)
						{
							iterator.CheckEdge(nativeArray[l]);
						}
					}
					flag3 = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) != 0 && m_TempData.HasComponent(value.m_Building);
				}
				value.m_NewRoad = iterator.m_BestRoad;
				value.m_FrontPos = iterator.m_FrontPosition;
				value.m_CurvePos = iterator.m_BestCurvePos;
				if (flag3)
				{
					@float = BuildingUtils.CalculateFrontPosition(transform, -buildingData.m_LotSize.y);
					float num2 = 8.4f;
					iterator.m_BestRoad = Entity.Null;
					iterator.m_BestCurvePos = 0f;
					iterator.m_Bounds = new Bounds3(@float - num2, @float + num2);
					iterator.m_MinDistance = num2;
					iterator.m_FrontPosition = @float;
					m_NetSearchTree.Iterate(ref iterator);
					for (int m = 0; m < m_UpdatedNetChunks.Length; m++)
					{
						NativeArray<Entity> nativeArray2 = m_UpdatedNetChunks[m].GetNativeArray(m_EntityType);
						for (int n = 0; n < nativeArray2.Length; n++)
						{
							iterator.CheckEdge(nativeArray2[n]);
						}
					}
					value2.m_RoadEdge = iterator.m_BestRoad;
					value2.m_CurvePosition = iterator.m_BestCurvePos;
				}
			}
			if (m_BackSideData.HasComponent(value.m_Building))
			{
				m_BackSideData[value.m_Building] = value2;
			}
			m_ReplaceRoadConnection[index] = value;
		}
	}

	[BurstCompile]
	private struct ReplaceRoadConnectionJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Created> m_CreatedData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public ComponentLookup<Building> m_BuildingData;

		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public EntityArchetype m_RoadConnectionEventArchetype;

		[ReadOnly]
		public NativeList<ReplaceRoad> m_ReplaceRoadConnection;

		[ReadOnly]
		public TrafficConfigurationData m_TrafficConfigurationData;

		public EntityCommandBuffer m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public SourceUpdateData m_SourceUpdateData;

		public void Execute()
		{
			for (int i = 0; i < m_ReplaceRoadConnection.Length; i++)
			{
				ReplaceRoad replaceRoad = m_ReplaceRoadConnection[i];
				Building value = m_BuildingData[replaceRoad.m_Building];
				bool flag = m_CreatedData.HasComponent(replaceRoad.m_Building);
				if (replaceRoad.m_NewRoad != value.m_RoadEdge || flag)
				{
					if (m_TempData.HasComponent(replaceRoad.m_Building))
					{
						if (value.m_RoadEdge == Entity.Null && replaceRoad.m_NewRoad != Entity.Null && m_TempData[replaceRoad.m_Building].m_Original == Entity.Null)
						{
							m_SourceUpdateData.AddSnap();
						}
						value.m_RoadEdge = replaceRoad.m_NewRoad;
						value.m_CurvePosition = replaceRoad.m_CurvePos;
						m_BuildingData[replaceRoad.m_Building] = value;
						continue;
					}
					if (value.m_RoadEdge != Entity.Null != (replaceRoad.m_NewRoad != Entity.Null))
					{
						if (replaceRoad.m_NewRoad != Entity.Null)
						{
							m_IconCommandBuffer.Remove(replaceRoad.m_Building, m_TrafficConfigurationData.m_RoadConnectionNotification);
						}
						else if (!replaceRoad.m_Deleted && replaceRoad.m_Required)
						{
							m_IconCommandBuffer.Add(replaceRoad.m_Building, m_TrafficConfigurationData.m_RoadConnectionNotification, replaceRoad.m_FrontPos, IconPriority.Warning);
						}
					}
					RoadConnectionUpdated component = new RoadConnectionUpdated
					{
						m_Building = replaceRoad.m_Building,
						m_Old = (flag ? Entity.Null : value.m_RoadEdge),
						m_New = replaceRoad.m_NewRoad
					};
					Entity e = m_CommandBuffer.CreateEntity(m_RoadConnectionEventArchetype);
					m_CommandBuffer.SetComponent(e, component);
					if (value.m_RoadEdge != Entity.Null)
					{
						CollectionUtils.RemoveValue(m_ConnectedBuildings[value.m_RoadEdge], new ConnectedBuilding(replaceRoad.m_Building));
					}
					value.m_RoadEdge = replaceRoad.m_NewRoad;
					value.m_CurvePosition = replaceRoad.m_CurvePos;
					m_BuildingData[replaceRoad.m_Building] = value;
					if (replaceRoad.m_NewRoad != Entity.Null)
					{
						m_ConnectedBuildings[replaceRoad.m_NewRoad].Add(new ConnectedBuilding(replaceRoad.m_Building));
					}
				}
				else if (replaceRoad.m_CurvePos != value.m_CurvePosition)
				{
					value.m_CurvePosition = replaceRoad.m_CurvePos;
					m_BuildingData[replaceRoad.m_Building] = value;
				}
			}
		}
	}

	private struct ConnectionLaneKey : IEquatable<ConnectionLaneKey>
	{
		private PathNode m_Node1;

		private PathNode m_Node2;

		public ConnectionLaneKey(PathNode node1, PathNode node2)
		{
			if (node1.GetOrder(node2))
			{
				m_Node1 = node2;
				m_Node2 = node1;
			}
			else
			{
				m_Node1 = node1;
				m_Node2 = node2;
			}
		}

		public bool Equals(ConnectionLaneKey other)
		{
			if (m_Node1.Equals(other.m_Node1))
			{
				return m_Node2.Equals(other.m_Node2);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (17 * 31 + m_Node1.GetHashCode()) * 31 + m_Node2.GetHashCode();
		}
	}

	private struct SpawnLocationData
	{
		public Entity m_Entity;

		public Entity m_Original;

		public float3 m_Position;

		public Game.Prefabs.SpawnLocationData m_PrefabData;

		public int m_Group;
	}

	[BurstCompile]
	private struct UpdateSecondaryLanesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerData;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerData;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.UtilityObject> m_UtilityObjectData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> m_UtilityLaneData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> m_PrefabUtilityObjectData;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> m_PrefabNetLaneArchetypeData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public NativeList<Entity> m_ConnectionPrefabs;

		[ReadOnly]
		public NativeList<ReplaceRoad> m_ReplaceRoadConnection;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			ReplaceRoad replaceRoad = m_ReplaceRoadConnection[index];
			FindRoadUtilityLanes(replaceRoad.m_Building, replaceRoad.m_NewRoad, replaceRoad.m_FrontPos, out var electricityCurve, out var electricityLanePrefab, out var electricityObjectPrefab, out var electricityNode, out var sewageCurve, out var sewageLanePrefab, out var sewageObjectPrefab, out var sewageNode, out var waterCurve, out var waterLanePrefab, out var waterObjectPrefab, out var waterNode);
			Temp temp = default(Temp);
			Temp subTemp = default(Temp);
			bool isTemp = false;
			if (m_TempData.HasComponent(replaceRoad.m_Building))
			{
				temp = m_TempData[replaceRoad.m_Building];
				subTemp.m_Flags = temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
				if ((temp.m_Flags & (TempFlags.Replace | TempFlags.Upgrade)) != 0)
				{
					subTemp.m_Flags |= TempFlags.Modify;
				}
				isTemp = true;
			}
			FindOriginalLanes(temp.m_Original, out var originalConnections, out var originalElectricityLane, out var originalSewageLane, out var originalWaterLane);
			FindOriginalObjects(temp.m_Original, out var originalElectricityObject, out var originalSewageObject, out var originalWaterObject);
			float3 electricityObjectPosition = CalculateObjectPosition(electricityCurve, electricityObjectPrefab, start: true);
			float3 sewageObjectPosition = CalculateObjectPosition(sewageCurve, sewageObjectPrefab, start: true);
			float3 waterObjectPosition = CalculateObjectPosition(waterCurve, waterObjectPrefab, start: true);
			UpdateLanes(index, replaceRoad.m_Building, replaceRoad.m_FrontPos, replaceRoad.m_Deleted, isTemp, subTemp, originalConnections, electricityCurve, electricityLanePrefab, electricityNode, originalElectricityLane, sewageCurve, sewageLanePrefab, sewageNode, originalSewageLane, waterCurve, waterLanePrefab, waterNode, originalWaterLane);
			UpdateObjects(index, replaceRoad.m_Building, replaceRoad.m_FrontPos, isTemp, subTemp, electricityObjectPrefab, originalElectricityObject, electricityObjectPosition, sewageObjectPrefab, originalSewageObject, sewageObjectPosition, waterObjectPrefab, originalWaterObject, waterObjectPosition);
			if (originalConnections.IsCreated)
			{
				originalConnections.Dispose();
			}
		}

		private void UpdateObjects(int jobIndex, Entity building, float3 connectPos, bool isTemp, Temp subTemp, Entity electricityObjectPrefab, Entity originalElectricityObject, float3 electricityObjectPosition, Entity sewageObjectPrefab, Entity originalSewageObject, float3 sewageObjectPosition, Entity waterObjectPrefab, Entity originalWaterObject, float3 waterObjectPosition)
		{
			if (!m_SubObjects.HasBuffer(building))
			{
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[building];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subObject = dynamicBuffer[i].m_SubObject;
				if (!m_UtilityObjectData.HasComponent(subObject) || !m_SecondaryData.HasComponent(subObject))
				{
					continue;
				}
				Transform transform = m_TransformData[subObject];
				PrefabRef prefabRef = m_PrefabRefData[subObject];
				UtilityObjectData utilityObjectData = m_PrefabUtilityObjectData[prefabRef.m_Prefab];
				if ((utilityObjectData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None)
				{
					if (prefabRef.m_Prefab != electricityObjectPrefab)
					{
						DeleteObject(jobIndex, subObject);
						continue;
					}
					electricityObjectPrefab = Entity.Null;
					if (m_DeletedData.HasComponent(subObject))
					{
						m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subObject);
					}
					if (!transform.m_Position.Equals(electricityObjectPosition))
					{
						UpdateObject(jobIndex, subObject, electricityObjectPosition, isTemp, subTemp, originalElectricityObject);
					}
				}
				else if ((utilityObjectData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None)
				{
					if (prefabRef.m_Prefab != sewageObjectPrefab)
					{
						DeleteObject(jobIndex, subObject);
						continue;
					}
					sewageObjectPrefab = Entity.Null;
					if (m_DeletedData.HasComponent(subObject))
					{
						m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subObject);
					}
					if (!transform.m_Position.Equals(sewageObjectPosition))
					{
						UpdateObject(jobIndex, subObject, sewageObjectPosition, isTemp, subTemp, originalSewageObject);
					}
				}
				else
				{
					if ((utilityObjectData.m_UtilityTypes & UtilityTypes.WaterPipe) == 0)
					{
						continue;
					}
					if (prefabRef.m_Prefab != waterObjectPrefab)
					{
						DeleteObject(jobIndex, subObject);
						continue;
					}
					waterObjectPrefab = Entity.Null;
					if (m_DeletedData.HasComponent(subObject))
					{
						m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subObject);
					}
					if (!transform.m_Position.Equals(waterObjectPosition))
					{
						UpdateObject(jobIndex, subObject, waterObjectPosition, isTemp, subTemp, originalWaterObject);
					}
				}
			}
			if (electricityObjectPrefab != Entity.Null)
			{
				CreateObject(jobIndex, building, electricityObjectPrefab, electricityObjectPosition, connectPos, isTemp, subTemp, originalElectricityObject);
			}
			if (sewageObjectPrefab != Entity.Null)
			{
				CreateObject(jobIndex, building, sewageObjectPrefab, sewageObjectPosition, connectPos, isTemp, subTemp, originalSewageObject);
			}
			if (waterObjectPrefab != Entity.Null)
			{
				CreateObject(jobIndex, building, waterObjectPrefab, waterObjectPosition, connectPos, isTemp, subTemp, originalWaterObject);
			}
		}

		private void UpdateLanes(int jobIndex, Entity building, float3 connectPos, bool isDeleted, bool isTemp, Temp subTemp, NativeParallelHashMap<ConnectionLaneKey, Entity> originalConnections, Bezier4x3 electricityCurve, Entity electricityLanePrefab, PathNode electricityNode, Entity originalElectricityLane, Bezier4x3 sewageCurve, Entity sewageLanePrefab, PathNode sewageNode, Entity originalSewageLane, Bezier4x3 waterCurve, Entity waterLanePrefab, PathNode waterNode, Entity originalWaterLane)
		{
			NativeParallelHashMap<ConnectionLaneKey, Entity> oldConnections = default(NativeParallelHashMap<ConnectionLaneKey, Entity>);
			if (!m_SubLanes.HasBuffer(building))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[building];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!m_SecondaryLaneData.HasComponent(subLane))
				{
					continue;
				}
				if (m_UtilityLaneData.HasComponent(subLane))
				{
					Curve curve = m_CurveData[subLane];
					Lane lane = m_LaneData[subLane];
					PrefabRef prefabRef = m_PrefabRefData[subLane];
					UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef.m_Prefab];
					if ((utilityLaneData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None)
					{
						if (prefabRef.m_Prefab != electricityLanePrefab)
						{
							DeleteLane(jobIndex, subLane);
							continue;
						}
						electricityLanePrefab = Entity.Null;
						if (m_DeletedData.HasComponent(subLane))
						{
							m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subLane);
						}
						if (!curve.m_Bezier.Equals(electricityCurve) || !lane.m_EndNode.Equals(electricityNode))
						{
							UpdateLane(jobIndex, subLane, electricityCurve, electricityNode, isTemp, subTemp, originalElectricityLane);
						}
					}
					else if ((utilityLaneData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None)
					{
						if (prefabRef.m_Prefab != sewageLanePrefab)
						{
							DeleteLane(jobIndex, subLane);
							continue;
						}
						sewageLanePrefab = Entity.Null;
						if (m_DeletedData.HasComponent(subLane))
						{
							m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subLane);
						}
						if (!curve.m_Bezier.Equals(sewageCurve) || !lane.m_EndNode.Equals(sewageNode))
						{
							UpdateLane(jobIndex, subLane, sewageCurve, sewageNode, isTemp, subTemp, originalSewageLane);
						}
					}
					else
					{
						if ((utilityLaneData.m_UtilityTypes & UtilityTypes.WaterPipe) == 0)
						{
							continue;
						}
						if (prefabRef.m_Prefab != waterLanePrefab)
						{
							DeleteLane(jobIndex, subLane);
							continue;
						}
						waterLanePrefab = Entity.Null;
						if (m_DeletedData.HasComponent(subLane))
						{
							m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, subLane);
						}
						if (!curve.m_Bezier.Equals(waterCurve) || !lane.m_EndNode.Equals(waterNode))
						{
							UpdateLane(jobIndex, subLane, waterCurve, waterNode, isTemp, subTemp, originalWaterLane);
						}
					}
				}
				else if (m_ConnectionLaneData.HasComponent(subLane))
				{
					Lane lane2 = m_LaneData[subLane];
					if (!oldConnections.IsCreated)
					{
						oldConnections = new NativeParallelHashMap<ConnectionLaneKey, Entity>(dynamicBuffer.Length, Allocator.Temp);
					}
					oldConnections.TryAdd(new ConnectionLaneKey(lane2.m_StartNode, lane2.m_EndNode), subLane);
				}
				else
				{
					DeleteLane(jobIndex, subLane);
				}
			}
			if (electricityLanePrefab != Entity.Null)
			{
				CreateLane(jobIndex, building, 65530, electricityLanePrefab, electricityCurve, electricityNode, connectPos, isTemp, subTemp, originalElectricityLane);
			}
			if (sewageLanePrefab != Entity.Null)
			{
				CreateLane(jobIndex, building, 65532, sewageLanePrefab, sewageCurve, sewageNode, connectPos, isTemp, subTemp, originalSewageLane);
			}
			if (waterLanePrefab != Entity.Null)
			{
				CreateLane(jobIndex, building, 65534, waterLanePrefab, waterCurve, waterNode, connectPos, isTemp, subTemp, originalWaterLane);
			}
			if (!isDeleted)
			{
				DynamicBuffer<SpawnLocationElement> dynamicBuffer2 = m_SpawnLocations[building];
				bool flag = dynamicBuffer2.Length >= 1;
				if (flag)
				{
					Entity entity = building;
					Owner componentData;
					while (m_OwnerData.TryGetComponent(entity, out componentData))
					{
						entity = componentData.m_Owner;
						if (m_SpawnLocations.HasBuffer(entity))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					NativeParallelHashMap<ConnectionLaneKey, Entity> newConnections = new NativeParallelHashMap<ConnectionLaneKey, Entity>(dynamicBuffer2.Length * 4, Allocator.Temp);
					NativeArray<SpawnLocationData> nativeArray = new NativeArray<SpawnLocationData>(dynamicBuffer2.Length, Allocator.Temp);
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						if (dynamicBuffer2[j].m_Type != SpawnLocationType.SpawnLocation)
						{
							continue;
						}
						Entity spawnLocation = dynamicBuffer2[j].m_SpawnLocation;
						if (m_TransformData.HasComponent(spawnLocation))
						{
							PrefabRef prefabRef2 = m_PrefabRefData[spawnLocation];
							if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2) && componentData2.m_ActivityMask.m_Mask == 0 && componentData2.m_ConnectionType != RouteConnectionType.Air && componentData2.m_ConnectionType != RouteConnectionType.Track && componentData2.m_ConnectionType != RouteConnectionType.Offroad)
							{
								m_TempData.TryGetComponent(spawnLocation, out var componentData3);
								m_SpawnLocationData.TryGetComponent(spawnLocation, out var componentData4);
								nativeArray[j] = new SpawnLocationData
								{
									m_Entity = spawnLocation,
									m_Original = componentData3.m_Original,
									m_Position = m_TransformData[spawnLocation].m_Position,
									m_PrefabData = componentData2,
									m_Group = componentData4.m_GroupIndex
								};
							}
						}
					}
					PrefabRef prefabRef3 = m_PrefabRefData[building];
					m_PrefabBuildingData.TryGetComponent(prefabRef3.m_Prefab, out var componentData5);
					for (int k = 0; k < nativeArray.Length; k++)
					{
						SpawnLocationData spawnLocationData = nativeArray[k];
						if (spawnLocationData.m_PrefabData.m_ConnectionType == RouteConnectionType.None)
						{
							continue;
						}
						float3 @float = float.MaxValue;
						float3 float2 = float.MaxValue;
						float3 float3 = float.MaxValue;
						float3 float4 = float.MaxValue;
						int3 @int = -1;
						int3 int2 = -1;
						int3 int3 = -1;
						int3 int4 = -1;
						for (int l = 0; l < nativeArray.Length; l++)
						{
							if (l == k)
							{
								continue;
							}
							SpawnLocationData spawnLocationData2 = nativeArray[l];
							if (spawnLocationData2.m_Group != spawnLocationData.m_Group)
							{
								continue;
							}
							bool flag2 = false;
							switch (spawnLocationData.m_PrefabData.m_ConnectionType)
							{
							case RouteConnectionType.Pedestrian:
								if (spawnLocationData.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
								{
									if (spawnLocationData2.m_PrefabData.m_ConnectionType == RouteConnectionType.Pedestrian)
									{
										flag2 = spawnLocationData2.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle;
										break;
									}
									if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Parking || spawnLocationData2.m_PrefabData.m_RoadTypes != RoadTypes.Bicycle)
									{
										continue;
									}
									flag2 = true;
								}
								else if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Pedestrian)
								{
									continue;
								}
								break;
							case RouteConnectionType.Cargo:
								if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Cargo)
								{
									continue;
								}
								break;
							case RouteConnectionType.Road:
							case RouteConnectionType.Air:
								if ((spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Road && spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Air) || (spawnLocationData.m_PrefabData.m_RoadTypes & spawnLocationData2.m_PrefabData.m_RoadTypes) == 0)
								{
									continue;
								}
								break;
							case RouteConnectionType.Track:
								if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Track || (spawnLocationData.m_PrefabData.m_TrackTypes & spawnLocationData2.m_PrefabData.m_TrackTypes) == 0)
								{
									continue;
								}
								break;
							case RouteConnectionType.Parking:
								if (spawnLocationData.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
								{
									if (spawnLocationData2.m_PrefabData.m_ConnectionType == RouteConnectionType.Pedestrian)
									{
										flag2 = spawnLocationData2.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle;
										break;
									}
									if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Parking || spawnLocationData2.m_PrefabData.m_RoadTypes != RoadTypes.Bicycle)
									{
										continue;
									}
									flag2 = true;
								}
								else if (spawnLocationData2.m_PrefabData.m_ConnectionType != RouteConnectionType.Parking || (spawnLocationData.m_PrefabData.m_RoadTypes & spawnLocationData2.m_PrefabData.m_RoadTypes) == 0)
								{
									continue;
								}
								break;
							}
							float3 x = spawnLocationData2.m_Position - spawnLocationData.m_Position;
							float distance = math.length(x);
							float3 float5 = math.abs(x);
							bool3 @bool = float5.xxy >= float5.yzz;
							if (math.all(@bool.xy))
							{
								CheckDistance(x.x, distance, l, ref @float.x, ref float2.x, ref @int.x, ref int2.x);
								if (flag2)
								{
									CheckDistance(x.x, distance, l, ref float3.x, ref float4.x, ref int3.x, ref int4.x);
								}
							}
							else if (@bool.z)
							{
								CheckDistance(x.y, distance, l, ref @float.y, ref float2.y, ref @int.y, ref int2.y);
								if (flag2)
								{
									CheckDistance(x.y, distance, l, ref float3.y, ref float4.y, ref int3.y, ref int4.y);
								}
							}
							else
							{
								CheckDistance(x.z, distance, l, ref @float.z, ref float2.z, ref @int.z, ref int2.z);
								if (flag2)
								{
									CheckDistance(x.z, distance, l, ref float3.z, ref float4.z, ref int3.z, ref int4.z);
								}
							}
						}
						float num = float.MaxValue;
						int num2 = -1;
						if (spawnLocationData.m_PrefabData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData.m_PrefabData.m_RoadTypes != RoadTypes.Bicycle)
						{
							for (int m = 0; m < nativeArray.Length; m++)
							{
								SpawnLocationData spawnLocationData3 = nativeArray[m];
								if (spawnLocationData3.m_PrefabData.m_ConnectionType == RouteConnectionType.Pedestrian)
								{
									float num3 = math.length(spawnLocationData3.m_Position - spawnLocationData.m_Position);
									if (num3 < num)
									{
										num = num3;
										num2 = m;
									}
								}
							}
						}
						if (@int.x != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[@int.x], originalConnections, oldConnections, newConnections);
						}
						if (@int.y != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[@int.y], originalConnections, oldConnections, newConnections);
						}
						if (@int.z != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[@int.z], originalConnections, oldConnections, newConnections);
						}
						if (int2.x != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int2.x], originalConnections, oldConnections, newConnections);
						}
						if (int2.y != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int2.y], originalConnections, oldConnections, newConnections);
						}
						if (int2.z != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int2.z], originalConnections, oldConnections, newConnections);
						}
						if (int3.x != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int3.x], originalConnections, oldConnections, newConnections);
						}
						if (int3.y != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int3.y], originalConnections, oldConnections, newConnections);
						}
						if (int3.z != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int3.z], originalConnections, oldConnections, newConnections);
						}
						if (int4.x != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int4.x], originalConnections, oldConnections, newConnections);
						}
						if (int4.y != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int4.y], originalConnections, oldConnections, newConnections);
						}
						if (int4.z != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[int4.z], originalConnections, oldConnections, newConnections);
						}
						if (num2 != -1)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, nativeArray[num2], originalConnections, oldConnections, newConnections);
						}
						if (spawnLocationData.m_PrefabData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
						{
							CheckConnection(jobIndex, building, componentData5, isTemp, subTemp, spawnLocationData, spawnLocationData, originalConnections, oldConnections, newConnections);
						}
					}
					nativeArray.Dispose();
					newConnections.Dispose();
				}
			}
			if (oldConnections.IsCreated)
			{
				NativeParallelHashMap<ConnectionLaneKey, Entity>.Enumerator enumerator = oldConnections.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DeleteLane(jobIndex, enumerator.Current.Value);
				}
				enumerator.Dispose();
				oldConnections.Dispose();
			}
		}

		private void CheckConnection(int jobIndex, Entity building, BuildingData prefabBuildingData, bool isTemp, Temp temp, SpawnLocationData spawnLocationData1, SpawnLocationData spawnLocationData2, NativeParallelHashMap<ConnectionLaneKey, Entity> originalConnections, NativeParallelHashMap<ConnectionLaneKey, Entity> oldConnections, NativeParallelHashMap<ConnectionLaneKey, Entity> newConnections)
		{
			PathNode pathNode = new PathNode(spawnLocationData1.m_Entity, 0);
			PathNode pathNode2 = new PathNode(spawnLocationData2.m_Entity, 0);
			ConnectionLaneKey key = new ConnectionLaneKey(pathNode, pathNode2);
			if (newConnections.ContainsKey(key))
			{
				return;
			}
			Lane component = default(Lane);
			component.m_StartNode = pathNode;
			component.m_MiddleNode = new PathNode(spawnLocationData1.m_Entity, 3);
			component.m_EndNode = pathNode2;
			Curve component2 = default(Curve);
			component2.m_Bezier = NetUtils.StraightCurve(spawnLocationData1.m_Position, spawnLocationData2.m_Position);
			component2.m_Length = math.distance(spawnLocationData1.m_Position, spawnLocationData2.m_Position);
			if (isTemp && originalConnections.IsCreated)
			{
				PathNode node = new PathNode(spawnLocationData1.m_Original, 0);
				PathNode node2 = new PathNode(spawnLocationData2.m_Original, 0);
				ConnectionLaneKey key2 = new ConnectionLaneKey(node, node2);
				if (originalConnections.TryGetValue(key2, out var item))
				{
					originalConnections.Remove(key2);
					temp.m_Original = item;
				}
			}
			if (oldConnections.IsCreated && oldConnections.TryGetValue(key, out var item2))
			{
				oldConnections.Remove(key);
				if (m_DeletedData.HasComponent(item2))
				{
					m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, item2);
				}
				Curve curve = m_CurveData[item2];
				if (!component2.m_Bezier.Equals(curve.m_Bezier) && !MathUtils.Invert(component2.m_Bezier).Equals(curve.m_Bezier))
				{
					Lane lane = m_LaneData[item2];
					if (lane.m_StartNode.Equals(pathNode2) && !lane.m_StartNode.Equals(pathNode))
					{
						CommonUtils.Swap(ref component.m_StartNode, ref component.m_EndNode);
						component2.m_Bezier = MathUtils.Invert(component2.m_Bezier);
					}
					m_CommandBuffer.SetComponent(jobIndex, item2, component);
					m_CommandBuffer.SetComponent(jobIndex, item2, component2);
					m_CommandBuffer.AddComponent(jobIndex, item2, default(Updated));
				}
				if (isTemp)
				{
					m_CommandBuffer.SetComponent(jobIndex, item2, temp);
				}
				newConnections.Add(key, item2);
				return;
			}
			Entity entity = m_ConnectionPrefabs[0];
			NetLaneArchetypeData netLaneArchetypeData = m_PrefabNetLaneArchetypeData[entity];
			Owner component3 = new Owner(building);
			PrefabRef component4 = new PrefabRef(entity);
			Game.Net.SecondaryLane component5 = default(Game.Net.SecondaryLane);
			Game.Net.ConnectionLane component6 = new Game.Net.ConnectionLane
			{
				m_Flags = ConnectionLaneFlags.Inside
			};
			Game.Prefabs.BuildingFlags buildingFlags = (Game.Prefabs.BuildingFlags)0u;
			switch (spawnLocationData1.m_PrefabData.m_ConnectionType)
			{
			case RouteConnectionType.Pedestrian:
				if (spawnLocationData2.m_PrefabData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData2.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle && spawnLocationData1.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
				{
					component6.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Pedestrian;
					component6.m_RoadTypes = RoadTypes.Bicycle;
					buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian;
				}
				else
				{
					component6.m_Flags |= ConnectionLaneFlags.Pedestrian;
					buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian;
				}
				break;
			case RouteConnectionType.Cargo:
				component6.m_Flags |= ConnectionLaneFlags.AllowCargo;
				break;
			case RouteConnectionType.Road:
			case RouteConnectionType.Air:
				component6.m_Flags |= ConnectionLaneFlags.Road;
				component6.m_RoadTypes = spawnLocationData1.m_PrefabData.m_RoadTypes | spawnLocationData2.m_PrefabData.m_RoadTypes;
				if ((component6.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
				{
					buildingFlags = Game.Prefabs.BuildingFlags.RestrictedCar;
				}
				break;
			case RouteConnectionType.Track:
				component6.m_Flags |= ConnectionLaneFlags.Track;
				component6.m_TrackTypes = spawnLocationData1.m_PrefabData.m_TrackTypes | spawnLocationData2.m_PrefabData.m_TrackTypes;
				break;
			case RouteConnectionType.Parking:
				if (spawnLocationData2.m_Entity == spawnLocationData1.m_Entity)
				{
					component6.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.Parking;
					component6.m_RoadTypes = spawnLocationData1.m_PrefabData.m_RoadTypes;
					buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar;
				}
				else if (spawnLocationData2.m_PrefabData.m_ConnectionType == RouteConnectionType.Pedestrian)
				{
					if (spawnLocationData1.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle && spawnLocationData2.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
					{
						component6.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Pedestrian;
						component6.m_RoadTypes = RoadTypes.Bicycle;
						buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian;
					}
					else if (spawnLocationData1.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
					{
						component6.m_Flags |= ConnectionLaneFlags.Pedestrian;
						buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian;
					}
					else
					{
						component6.m_Flags |= ConnectionLaneFlags.Parking;
						component6.m_RoadTypes = spawnLocationData1.m_PrefabData.m_RoadTypes | spawnLocationData2.m_PrefabData.m_RoadTypes;
						buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar;
					}
				}
				else if (spawnLocationData2.m_PrefabData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData1.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle && spawnLocationData2.m_PrefabData.m_RoadTypes == RoadTypes.Bicycle)
				{
					component6.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Pedestrian;
					component6.m_RoadTypes = RoadTypes.Bicycle;
					buildingFlags = Game.Prefabs.BuildingFlags.RestrictedPedestrian;
				}
				else
				{
					component6.m_Flags |= ConnectionLaneFlags.Road;
					component6.m_RoadTypes = spawnLocationData1.m_PrefabData.m_RoadTypes | spawnLocationData2.m_PrefabData.m_RoadTypes;
					if ((component6.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
					{
						buildingFlags = Game.Prefabs.BuildingFlags.RestrictedCar;
					}
				}
				break;
			}
			if ((buildingFlags & prefabBuildingData.m_Flags) != 0)
			{
				Entity entity2 = spawnLocationData1.m_Entity;
				Entity entity3 = spawnLocationData2.m_Entity;
				Owner componentData;
				while (m_OwnerData.TryGetComponent(entity2, out componentData) && !m_BuildingData.HasComponent(entity2))
				{
					entity2 = componentData.m_Owner;
				}
				Owner componentData2;
				while (m_OwnerData.TryGetComponent(entity3, out componentData2) && !m_BuildingData.HasComponent(entity3))
				{
					entity3 = componentData2.m_Owner;
				}
				if (entity2 == entity3 && entity2 != building && m_PrefabRefData.TryGetComponent(entity2, out var componentData3) && m_PrefabBuildingData.TryGetComponent(componentData3.m_Prefab, out var componentData4) && (componentData4.m_Flags & buildingFlags) == 0)
				{
					component6.m_Flags |= ConnectionLaneFlags.NoRestriction;
				}
			}
			Entity entity4 = m_CommandBuffer.CreateEntity(jobIndex, netLaneArchetypeData.m_LaneArchetype);
			m_CommandBuffer.SetComponent(jobIndex, entity4, component4);
			m_CommandBuffer.SetComponent(jobIndex, entity4, component);
			m_CommandBuffer.SetComponent(jobIndex, entity4, component2);
			m_CommandBuffer.SetComponent(jobIndex, entity4, component6);
			m_CommandBuffer.AddComponent(jobIndex, entity4, component3);
			m_CommandBuffer.AddComponent(jobIndex, entity4, component5);
			if ((component6.m_Flags & ConnectionLaneFlags.Parking) != 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity4, default(GarageLane));
			}
			if (isTemp)
			{
				m_CommandBuffer.AddComponent(jobIndex, entity4, temp);
			}
			newConnections.Add(key, entity4);
		}

		private void CheckDistance(float offset, float distance, int index, ref float bestDistance1, ref float bestDistance2, ref int bestIndex1, ref int bestIndex2)
		{
			if (offset >= 0f)
			{
				if (distance < bestDistance1)
				{
					bestDistance1 = distance;
					bestIndex1 = index;
				}
			}
			else if (distance < bestDistance2)
			{
				bestDistance2 = distance;
				bestIndex2 = index;
			}
		}

		private float3 CalculateObjectPosition(Bezier4x3 curve, Entity prefab, bool start = false)
		{
			float3 result = (start ? curve.a : curve.d);
			if (prefab != Entity.Null)
			{
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefab];
				result.y -= MathUtils.Center(objectGeometryData.m_Bounds).y;
			}
			return result;
		}

		private void FindOriginalLanes(Entity originalBuilding, out NativeParallelHashMap<ConnectionLaneKey, Entity> originalConnections, out Entity originalElectricityLane, out Entity originalSewageLane, out Entity originalWaterLane)
		{
			originalConnections = default(NativeParallelHashMap<ConnectionLaneKey, Entity>);
			originalElectricityLane = default(Entity);
			originalSewageLane = default(Entity);
			originalWaterLane = default(Entity);
			if (!m_SubLanes.HasBuffer(originalBuilding))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[originalBuilding];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!m_SecondaryLaneData.HasComponent(subLane))
				{
					continue;
				}
				if (m_UtilityLaneData.HasComponent(subLane))
				{
					PrefabRef prefabRef = m_PrefabRefData[subLane];
					UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef.m_Prefab];
					if ((utilityLaneData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None)
					{
						originalElectricityLane = subLane;
					}
					else if ((utilityLaneData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None)
					{
						originalSewageLane = subLane;
					}
					else if ((utilityLaneData.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None)
					{
						originalWaterLane = subLane;
					}
				}
				else if (m_ConnectionLaneData.HasComponent(subLane))
				{
					Lane lane = m_LaneData[subLane];
					if (!originalConnections.IsCreated)
					{
						originalConnections = new NativeParallelHashMap<ConnectionLaneKey, Entity>(dynamicBuffer.Length, Allocator.Temp);
					}
					originalConnections.TryAdd(new ConnectionLaneKey(lane.m_StartNode, lane.m_EndNode), subLane);
				}
			}
		}

		private void FindOriginalObjects(Entity originalBuilding, out Entity originalElectricityObject, out Entity originalSewageObject, out Entity originalWaterObject)
		{
			originalElectricityObject = default(Entity);
			originalSewageObject = default(Entity);
			originalWaterObject = default(Entity);
			if (!m_SubObjects.HasBuffer(originalBuilding))
			{
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[originalBuilding];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subObject = dynamicBuffer[i].m_SubObject;
				if (m_UtilityObjectData.HasComponent(subObject) && m_SecondaryData.HasComponent(subObject))
				{
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					UtilityObjectData utilityObjectData = m_PrefabUtilityObjectData[prefabRef.m_Prefab];
					if ((utilityObjectData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None)
					{
						originalElectricityObject = subObject;
					}
					else if ((utilityObjectData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None)
					{
						originalSewageObject = subObject;
					}
					else if ((utilityObjectData.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None)
					{
						originalWaterObject = subObject;
					}
				}
			}
		}

		private void FindRoadUtilityLanes(Entity building, Entity road, float3 connectionPosition, out Bezier4x3 electricityCurve, out Entity electricityLanePrefab, out Entity electricityObjectPrefab, out PathNode electricityNode, out Bezier4x3 sewageCurve, out Entity sewageLanePrefab, out Entity sewageObjectPrefab, out PathNode sewageNode, out Bezier4x3 waterCurve, out Entity waterLanePrefab, out Entity waterObjectPrefab, out PathNode waterNode)
		{
			electricityCurve = default(Bezier4x3);
			electricityLanePrefab = default(Entity);
			electricityObjectPrefab = default(Entity);
			electricityNode = default(PathNode);
			sewageCurve = default(Bezier4x3);
			sewageLanePrefab = default(Entity);
			sewageObjectPrefab = default(Entity);
			sewageNode = default(PathNode);
			waterCurve = default(Bezier4x3);
			waterLanePrefab = default(Entity);
			waterObjectPrefab = default(Entity);
			waterNode = default(PathNode);
			if (!m_SubLanes.TryGetBuffer(road, out var bufferData))
			{
				return;
			}
			EdgeGeometry edgeGeometry = m_EdgeGeometryData[road];
			Transform transform = m_TransformData[building];
			PrefabRef prefabRef = m_PrefabRefData[building];
			BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
			int3 @int = default(int3);
			if (m_ElectricityConsumerData.HasComponent(building) && (buildingData.m_Flags & Game.Prefabs.BuildingFlags.HasLowVoltageNode) == 0)
			{
				@int.x = 4;
			}
			if (m_WaterConsumerData.HasComponent(building))
			{
				if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.HasSewageNode) == 0)
				{
					@int.y = 4;
				}
				if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.HasWaterNode) == 0)
				{
					@int.z = 4;
				}
			}
			float3 @float = math.rotate(transform.m_Rotation, new float3(0.15f, 0f, 0f));
			float3 float2 = connectionPosition - @float * math.csum(@int.xz);
			float3 float3 = transform.m_Position - @float * math.csum(@int.xz);
			float3 float4 = float2;
			float3 startPos = float3;
			float2 += @float * math.csum(@int.xy);
			float3 float5 = float3 + @float * math.csum(@int.xy);
			float3 float6 = float2;
			float3 startPos2 = float5;
			float2 += @float * math.csum(@int.yz);
			float3 float7 = float5 + @float * math.csum(@int.yz);
			float3 float8 = float2;
			float3 startPos3 = float7;
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			Entity entity3 = Entity.Null;
			float delta = 0f;
			float delta2 = 0f;
			float delta3 = 0f;
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			float num3 = float.MaxValue;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subLane = bufferData[i].m_SubLane;
				if (!m_UtilityLaneData.HasComponent(subLane) || !m_EdgeLaneData.HasComponent(subLane))
				{
					continue;
				}
				Curve curve = m_CurveData[subLane];
				PrefabRef prefabRef2 = m_PrefabRefData[subLane];
				UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef2.m_Prefab];
				if ((utilityLaneData.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None && @int.x != 0)
				{
					float t;
					float num4 = MathUtils.Distance(curve.m_Bezier, float6, out t);
					if (num4 < num)
					{
						entity = subLane;
						delta = t;
						num = num4;
					}
				}
				if ((utilityLaneData.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None && @int.y != 0)
				{
					float t2;
					float num5 = MathUtils.Distance(curve.m_Bezier, float4, out t2);
					if (num5 < num2)
					{
						entity2 = subLane;
						delta2 = t2;
						num2 = num5;
					}
				}
				if ((utilityLaneData.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None && @int.z != 0)
				{
					float t3;
					float num6 = MathUtils.Distance(curve.m_Bezier, float8, out t3);
					if (num6 < num3)
					{
						entity3 = subLane;
						delta3 = t3;
						num3 = num6;
					}
				}
			}
			float yOffset = CalculateYOffset(edgeGeometry, entity);
			float yOffset2 = CalculateYOffset(edgeGeometry, entity2);
			float yOffset3 = CalculateYOffset(edgeGeometry, entity3);
			bool2 @bool = ShouldCheckNodeLanes(entity, delta);
			bool2 bool2 = ShouldCheckNodeLanes(entity2, delta2);
			bool2 bool3 = ShouldCheckNodeLanes(entity3, delta3);
			bool2 x = @bool | bool2 | bool3;
			if (math.any(x))
			{
				Game.Net.Edge edge = m_EdgeData[road];
				if (x.x && m_SubLanes.TryGetBuffer(edge.m_Start, out bufferData))
				{
					for (int j = 0; j < bufferData.Length; j++)
					{
						Entity subLane2 = bufferData[j].m_SubLane;
						if (!m_UtilityLaneData.HasComponent(subLane2))
						{
							continue;
						}
						Curve curve2 = m_CurveData[subLane2];
						PrefabRef prefabRef3 = m_PrefabRefData[subLane2];
						UtilityLaneData utilityLaneData2 = m_PrefabUtilityLaneData[prefabRef3.m_Prefab];
						if ((utilityLaneData2.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None && @bool.x)
						{
							float t4;
							float num7 = MathUtils.Distance(curve2.m_Bezier, float6, out t4);
							if (num7 < num)
							{
								entity = subLane2;
								delta = t4;
								num = num7;
							}
						}
						if ((utilityLaneData2.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None && bool2.x)
						{
							float t5;
							float num8 = MathUtils.Distance(curve2.m_Bezier, float4, out t5);
							if (num8 < num2)
							{
								entity2 = subLane2;
								delta2 = t5;
								num2 = num8;
							}
						}
						if ((utilityLaneData2.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None && bool3.x)
						{
							float t6;
							float num9 = MathUtils.Distance(curve2.m_Bezier, float8, out t6);
							if (num9 < num3)
							{
								entity3 = subLane2;
								delta3 = t6;
								num3 = num9;
							}
						}
					}
				}
				if (x.y && m_SubLanes.TryGetBuffer(edge.m_End, out bufferData))
				{
					for (int k = 0; k < bufferData.Length; k++)
					{
						Entity subLane3 = bufferData[k].m_SubLane;
						if (!m_UtilityLaneData.HasComponent(subLane3))
						{
							continue;
						}
						Curve curve3 = m_CurveData[subLane3];
						PrefabRef prefabRef4 = m_PrefabRefData[subLane3];
						UtilityLaneData utilityLaneData3 = m_PrefabUtilityLaneData[prefabRef4.m_Prefab];
						if ((utilityLaneData3.m_UtilityTypes & UtilityTypes.LowVoltageLine) != UtilityTypes.None && @bool.y)
						{
							float t7;
							float num10 = MathUtils.Distance(curve3.m_Bezier, float6, out t7);
							if (num10 < num)
							{
								entity = subLane3;
								delta = t7;
								num = num10;
							}
						}
						if ((utilityLaneData3.m_UtilityTypes & UtilityTypes.SewagePipe) != UtilityTypes.None && bool2.y)
						{
							float t8;
							float num11 = MathUtils.Distance(curve3.m_Bezier, float4, out t8);
							if (num11 < num2)
							{
								entity2 = subLane3;
								delta2 = t8;
								num2 = num11;
							}
						}
						if ((utilityLaneData3.m_UtilityTypes & UtilityTypes.WaterPipe) != UtilityTypes.None && bool3.y)
						{
							float t9;
							float num12 = MathUtils.Distance(curve3.m_Bezier, float8, out t9);
							if (num12 < num3)
							{
								entity3 = subLane3;
								delta3 = t9;
								num3 = num12;
							}
						}
					}
				}
			}
			if (entity != Entity.Null)
			{
				electricityCurve = CalculateConnectCurve(startPos2, float6, entity, yOffset, delta);
				electricityLanePrefab = GetLanePrefab(entity, m_BuildingConfigurationData.m_ElectricityConnectionLane, @int.x);
				electricityObjectPrefab = GetNodeObjectPrefab(electricityLanePrefab);
				electricityNode = GetPathNode(entity, delta);
			}
			if (entity2 != Entity.Null)
			{
				sewageCurve = CalculateConnectCurve(startPos, float4, entity2, yOffset2, delta2);
				sewageLanePrefab = GetLanePrefab(entity2, m_BuildingConfigurationData.m_SewageConnectionLane, @int.y);
				sewageObjectPrefab = GetNodeObjectPrefab(sewageLanePrefab);
				sewageNode = GetPathNode(entity2, delta2);
			}
			if (entity3 != Entity.Null)
			{
				waterCurve = CalculateConnectCurve(startPos3, float8, entity3, yOffset3, delta3);
				waterLanePrefab = GetLanePrefab(entity3, m_BuildingConfigurationData.m_WaterConnectionLane, @int.z);
				waterObjectPrefab = GetNodeObjectPrefab(waterLanePrefab);
				waterNode = GetPathNode(entity3, delta3);
			}
		}

		private bool2 ShouldCheckNodeLanes(Entity bestLane, float delta)
		{
			if (bestLane != Entity.Null)
			{
				bool2 x = delta == new float2(0f, 1f);
				if (math.any(x))
				{
					EdgeLane edgeLane = m_EdgeLaneData[bestLane];
					return math.select(edgeLane.m_EdgeDelta.x, edgeLane.m_EdgeDelta.y, x.y) == new float2(0f, 1f);
				}
			}
			return false;
		}

		private float CalculateYOffset(EdgeGeometry edgeGeometry, Entity roadLane)
		{
			if (roadLane != Entity.Null)
			{
				Curve curve = m_CurveData[roadLane];
				EdgeLane edgeLane = m_EdgeLaneData[roadLane];
				if (edgeLane.m_EdgeDelta.x > 0.5f)
				{
					float t = math.saturate(edgeLane.m_EdgeDelta.x * 2f - 1f);
					return curve.m_Bezier.a.y - math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, t).y, MathUtils.Position(edgeGeometry.m_End.m_Right, t).y, 0.5f);
				}
				float t2 = math.saturate(edgeLane.m_EdgeDelta.x * 2f);
				return curve.m_Bezier.a.y - math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, t2).y, MathUtils.Position(edgeGeometry.m_Start.m_Right, t2).y, 0.5f);
			}
			return 0f;
		}

		private Bezier4x3 CalculateConnectCurve(float3 startPos, float3 connectPos, Entity roadLane, float yOffset, float delta)
		{
			Curve curve = m_CurveData[roadLane];
			startPos.y += yOffset;
			connectPos.y += yOffset;
			float3 a = MathUtils.Position(curve.m_Bezier, delta);
			return NetUtils.FitCurve(new Line3.Segment(startPos, connectPos), new Line3.Segment(a, connectPos));
		}

		private Entity GetNodeObjectPrefab(Entity lanePrefab)
		{
			return m_PrefabUtilityLaneData[lanePrefab].m_NodeObjectPrefab;
		}

		private Entity GetLanePrefab(Entity roadLane, Entity consumerConnectionPrefab, int needConnection)
		{
			if (needConnection < 6)
			{
				return consumerConnectionPrefab;
			}
			Entity prefab = m_PrefabRefData[roadLane].m_Prefab;
			if (needConnection > 6)
			{
				UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefab];
				if (!(utilityLaneData.m_LocalConnectionPrefab != Entity.Null))
				{
					return prefab;
				}
				return utilityLaneData.m_LocalConnectionPrefab;
			}
			return prefab;
		}

		private PathNode GetPathNode(Entity roadLane, float delta)
		{
			return new PathNode(m_LaneData[roadLane].m_MiddleNode, delta);
		}

		private void DeleteLane(int jobIndex, Entity lane)
		{
			m_CommandBuffer.RemoveComponent(jobIndex, lane, in m_AppliedTypes);
			m_CommandBuffer.AddComponent<Deleted>(jobIndex, lane);
		}

		private void UpdateLane(int jobIndex, Entity lane, Bezier4x3 curve, PathNode endNode, bool isTemp, Temp temp, Entity original)
		{
			Curve component = default(Curve);
			component.m_Bezier = curve;
			component.m_Length = MathUtils.Length(curve);
			Lane component2 = m_LaneData[lane];
			component2.m_EndNode = endNode;
			m_CommandBuffer.SetComponent(jobIndex, lane, component);
			m_CommandBuffer.SetComponent(jobIndex, lane, component2);
			m_CommandBuffer.AddComponent(jobIndex, lane, default(Updated));
			if (isTemp)
			{
				temp.m_Original = original;
				m_CommandBuffer.SetComponent(jobIndex, lane, temp);
			}
		}

		private void CreateLane(int jobIndex, Entity owner, int laneIndex, Entity prefab, Bezier4x3 curve, PathNode endNode, float3 connectPos, bool isTemp, Temp temp, Entity original)
		{
			NetLaneArchetypeData netLaneArchetypeData = m_PrefabNetLaneArchetypeData[prefab];
			Owner component = new Owner(owner);
			PrefabRef component2 = new PrefabRef(prefab);
			Game.Net.SecondaryLane component3 = default(Game.Net.SecondaryLane);
			Lane component4 = default(Lane);
			component4.m_StartNode = new PathNode(owner, (ushort)laneIndex);
			component4.m_MiddleNode = new PathNode(owner, (ushort)(laneIndex + 1));
			component4.m_EndNode = endNode;
			Curve component5 = default(Curve);
			component5.m_Bezier = curve;
			component5.m_Length = MathUtils.Length(curve);
			Game.Net.Elevation component6 = default(Game.Net.Elevation);
			component6.m_Elevation.x = curve.a.y - connectPos.y;
			component6.m_Elevation.y = curve.d.y - connectPos.y;
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, netLaneArchetypeData.m_LaneArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component2);
			m_CommandBuffer.SetComponent(jobIndex, e, component4);
			m_CommandBuffer.SetComponent(jobIndex, e, component5);
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, component3);
			m_CommandBuffer.AddComponent(jobIndex, e, component6);
			if (isTemp)
			{
				temp.m_Original = original;
				m_CommandBuffer.AddComponent(jobIndex, e, temp);
			}
		}

		private void DeleteObject(int jobIndex, Entity lane)
		{
			m_CommandBuffer.RemoveComponent(jobIndex, lane, in m_AppliedTypes);
			m_CommandBuffer.AddComponent<Deleted>(jobIndex, lane);
		}

		private void UpdateObject(int jobIndex, Entity obj, float3 position, bool isTemp, Temp temp, Entity original)
		{
			Transform component = default(Transform);
			component.m_Position = position;
			component.m_Rotation = quaternion.identity;
			m_CommandBuffer.SetComponent(jobIndex, obj, component);
			m_CommandBuffer.AddComponent(jobIndex, obj, default(Updated));
			if (isTemp)
			{
				temp.m_Original = original;
				m_CommandBuffer.SetComponent(jobIndex, obj, temp);
			}
		}

		private void CreateObject(int jobIndex, Entity owner, Entity prefab, float3 position, float3 connectPos, bool isTemp, Temp temp, Entity original)
		{
			ObjectData objectData = m_PrefabObjectData[prefab];
			Owner component = new Owner(owner);
			PrefabRef component2 = new PrefabRef(prefab);
			Secondary component3 = default(Secondary);
			Transform component4 = default(Transform);
			component4.m_Position = position;
			component4.m_Rotation = quaternion.identity;
			Game.Objects.Elevation component5 = default(Game.Objects.Elevation);
			component5.m_Elevation = position.y - connectPos.y;
			component5.m_Flags = (ElevationFlags)0;
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component2);
			m_CommandBuffer.SetComponent(jobIndex, e, component4);
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, component3);
			m_CommandBuffer.AddComponent(jobIndex, e, component5);
			if (isTemp)
			{
				temp.m_Original = original;
				m_CommandBuffer.AddComponent(jobIndex, e, temp);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public ComponentLookup<BackSide> __Game_Buildings_BackSide_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

		public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.UtilityObject> __Game_Objects_UtilityObject_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Secondary> __Game_Objects_Secondary_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityObjectData> __Game_Prefabs_UtilityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> __Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedBuilding>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Buildings_BackSide_RW_ComponentLookup = state.GetComponentLookup<BackSide>();
			__Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
			__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
			__Game_Buildings_ConnectedBuilding_RW_BufferLookup = state.GetBufferLookup<ConnectedBuilding>();
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_UtilityObject_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.UtilityObject>(isReadOnly: true);
			__Game_Objects_Secondary_RO_ComponentLookup = state.GetComponentLookup<Secondary>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.SecondaryLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_UtilityObjectData_RO_ComponentLookup = state.GetComponentLookup<UtilityObjectData>(isReadOnly: true);
			__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup = state.GetComponentLookup<NetLaneArchetypeData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.SpawnLocationData>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
		}
	}

	private ModificationBarrier4B m_ModificationBarrier;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private IconCommandSystem m_IconCommandSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_ModificationQuery;

	private EntityQuery m_UpdatedNetQuery;

	private EntityQuery m_TrafficConfigQuery;

	private EntityQuery m_BuildingConfigQuery;

	private EntityQuery m_ConnectionQuery;

	private EntityArchetype m_RoadConnectionEventArchetype;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_ModificationQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.SpawnLocation>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Net.Edge>(),
				ComponentType.ReadOnly<ConnectedBuilding>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpdatedNetQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Net.Edge>(), ComponentType.ReadOnly<ConnectedBuilding>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_TrafficConfigQuery = GetEntityQuery(ComponentType.ReadOnly<TrafficConfigurationData>());
		m_BuildingConfigQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_ConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<ConnectionLaneData>(), ComponentType.ReadOnly<PrefabData>());
		m_RoadConnectionEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<RoadConnectionUpdated>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_ModificationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Entity> replaceRoadConnectionQueue = new NativeQueue<Entity>(Allocator.TempJob);
		NativeList<ReplaceRoad> nativeList = new NativeList<ReplaceRoad>(Allocator.TempJob);
		JobHandle dependencies;
		CheckRoadConnectionJob jobData = new CheckRoadConnectionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StartNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EndNodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedBuildingType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ReplaceRoadConnectionQueue = replaceRoadConnectionQueue.AsParallelWriter()
		};
		FillReplacementListJob jobData2 = new FillReplacementListJob
		{
			m_ReplaceRoadConnectionQueue = replaceRoadConnectionQueue,
			m_ReplaceRoadConnection = nativeList
		};
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> updatedNetChunks = m_UpdatedNetQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle dependencies2;
		FindRoadConnectionJob jobData3 = new FindRoadConnectionJob
		{
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BackSideData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_BackSide_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedNetChunks = updatedNetChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_ReplaceRoadConnection = nativeList.AsDeferredJobArray()
		};
		JobHandle deps;
		ReplaceRoadConnectionJob jobData4 = new ReplaceRoadConnectionJob
		{
			m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RW_BufferLookup, ref base.CheckedStateRef),
			m_RoadConnectionEventArchetype = m_RoadConnectionEventArchetype,
			m_ReplaceRoadConnection = nativeList,
			m_TrafficConfigurationData = m_TrafficConfigQuery.GetSingleton<TrafficConfigurationData>(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_SourceUpdateData = m_AudioManager.GetSourceUpdateData(out deps)
		};
		JobHandle outJobHandle2;
		NativeList<Entity> connectionPrefabs = m_ConnectionQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle2);
		UpdateSecondaryLanesJob jobData5 = new UpdateSecondaryLanesJob
		{
			m_WaterConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UtilityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UtilityObject_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneArchetypeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectionPrefabs = connectionPrefabs,
			m_ReplaceRoadConnection = nativeList,
			m_AppliedTypes = m_AppliedTypes,
			m_BuildingConfigurationData = m_BuildingConfigQuery.GetSingleton<BuildingConfigurationData>(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_ModificationQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		JobHandle jobHandle3 = jobData3.Schedule(nativeList, 1, JobUtils.CombineDependencies(jobHandle2, dependencies2, outJobHandle, outJobHandle2));
		JobHandle jobHandle4 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(jobHandle3, deps));
		JobHandle jobHandle5 = jobData5.Schedule(nativeList, 1, jobHandle3);
		replaceRoadConnectionQueue.Dispose(jobHandle2);
		updatedNetChunks.Dispose(jobHandle3);
		connectionPrefabs.Dispose(jobHandle5);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle3);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle4);
		base.Dependency = JobHandle.CombineDependencies(jobHandle4, jobHandle5);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
		nativeList.Dispose(base.Dependency);
	}

	private static void CheckDistance(EdgeGeometry edgeGeometry, EdgeNodeGeometry startGeometry, EdgeNodeGeometry endGeometry, float3 position, bool canBeOnRoad, ref float maxDistance, ref bool isOnRoad)
	{
		if (MathUtils.DistanceSquared(edgeGeometry.m_Bounds.xz, position.xz) < maxDistance * maxDistance)
		{
			CheckDistance(edgeGeometry.m_Start.m_Left, position, ref maxDistance);
			CheckDistance(edgeGeometry.m_Start.m_Right, position, ref maxDistance);
			CheckDistance(edgeGeometry.m_End.m_Left, position, ref maxDistance);
			CheckDistance(edgeGeometry.m_End.m_Right, position, ref maxDistance);
			if (canBeOnRoad)
			{
				CheckDistance(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, position, ref maxDistance, ref isOnRoad);
				CheckDistance(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, position, ref maxDistance, ref isOnRoad);
			}
		}
		if (MathUtils.DistanceSquared(startGeometry.m_Bounds.xz, position.xz) < maxDistance * maxDistance)
		{
			CheckDistance(startGeometry.m_Left.m_Left, position, ref maxDistance);
			CheckDistance(startGeometry.m_Right.m_Right, position, ref maxDistance);
			if (startGeometry.m_MiddleRadius > 0f)
			{
				CheckDistance(startGeometry.m_Left.m_Right, position, ref maxDistance);
				CheckDistance(startGeometry.m_Right.m_Left, position, ref maxDistance);
			}
			if (canBeOnRoad)
			{
				if (startGeometry.m_MiddleRadius > 0f)
				{
					CheckDistance(startGeometry.m_Left.m_Left, startGeometry.m_Left.m_Right, position, ref maxDistance, ref isOnRoad);
					CheckDistance(startGeometry.m_Right.m_Left, startGeometry.m_Middle, position, ref maxDistance, ref isOnRoad);
					CheckDistance(startGeometry.m_Middle, startGeometry.m_Right.m_Right, position, ref maxDistance, ref isOnRoad);
				}
				else
				{
					CheckDistance(startGeometry.m_Left.m_Left, startGeometry.m_Middle, position, ref maxDistance, ref isOnRoad);
					CheckDistance(startGeometry.m_Middle, startGeometry.m_Right.m_Right, position, ref maxDistance, ref isOnRoad);
				}
			}
		}
		if (!(MathUtils.DistanceSquared(endGeometry.m_Bounds.xz, position.xz) < maxDistance * maxDistance))
		{
			return;
		}
		CheckDistance(endGeometry.m_Left.m_Left, position, ref maxDistance);
		CheckDistance(endGeometry.m_Right.m_Right, position, ref maxDistance);
		if (endGeometry.m_MiddleRadius > 0f)
		{
			CheckDistance(endGeometry.m_Left.m_Right, position, ref maxDistance);
			CheckDistance(endGeometry.m_Right.m_Left, position, ref maxDistance);
		}
		if (canBeOnRoad)
		{
			if (endGeometry.m_MiddleRadius > 0f)
			{
				CheckDistance(endGeometry.m_Left.m_Left, endGeometry.m_Left.m_Right, position, ref maxDistance, ref isOnRoad);
				CheckDistance(endGeometry.m_Right.m_Left, endGeometry.m_Middle, position, ref maxDistance, ref isOnRoad);
				CheckDistance(endGeometry.m_Middle, endGeometry.m_Right.m_Right, position, ref maxDistance, ref isOnRoad);
			}
			else
			{
				CheckDistance(endGeometry.m_Left.m_Left, endGeometry.m_Middle, position, ref maxDistance, ref isOnRoad);
				CheckDistance(endGeometry.m_Middle, endGeometry.m_Right.m_Right, position, ref maxDistance, ref isOnRoad);
			}
		}
	}

	private static void CheckDistance(Bezier4x3 curve1, Bezier4x3 curve2, float3 position, ref float maxDistance, ref bool isOnRoad)
	{
		if (MathUtils.DistanceSquared(MathUtils.Bounds(curve1.xz) | MathUtils.Bounds(curve2.xz), position.xz) < maxDistance * maxDistance)
		{
			MathUtils.Distance(MathUtils.Lerp(curve1.xz, curve2.xz, 0.5f), position.xz, out var t);
			float num = MathUtils.Distance(new Line2.Segment(MathUtils.Position(curve1.xz, t), MathUtils.Position(curve2.xz, t)), position.xz, out t);
			isOnRoad |= num < maxDistance && t > 0.01f && t < 0.99f;
			maxDistance = math.min(num, maxDistance);
		}
	}

	private static void CheckDistance(Bezier4x3 curve, float3 position, ref float maxDistance)
	{
		if (MathUtils.DistanceSquared(MathUtils.Bounds(curve.xz), position.xz) < maxDistance * maxDistance)
		{
			float t;
			float x = MathUtils.Distance(curve.xz, position.xz, out t);
			maxDistance = math.min(x, maxDistance);
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
	public RoadConnectionSystem()
	{
	}
}
