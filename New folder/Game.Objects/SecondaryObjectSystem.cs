using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Policies;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
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
public class SecondaryObjectSystem : GameSystemBase
{
	private struct UpdateData
	{
		public Entity m_Owner;

		public Entity m_Prefab;

		public Transform m_Transform;
	}

	private struct SubObjectOwnerData : IComparable<SubObjectOwnerData>
	{
		public Entity m_Owner;

		public Entity m_Original;

		public bool m_Temp;

		public bool m_Deleted;

		public SubObjectOwnerData(Entity owner, Entity original, bool temp, bool deleted)
		{
			m_Owner = owner;
			m_Original = original;
			m_Temp = temp;
			m_Deleted = deleted;
		}

		public int CompareTo(SubObjectOwnerData other)
		{
			return m_Owner.Index - other.m_Owner.Index;
		}
	}

	private struct TrafficSignNeeds
	{
		public uint m_SignTypeMask;

		public uint m_RemoveSignTypeMask;

		public uint m_SignTypeMask2;

		public uint m_RemoveSignTypeMask2;

		public ushort m_SpeedLimit;

		public ushort m_SpeedLimit2;

		public float m_VehicleLanesLeft;

		public float m_VehicleLanesRight;

		public ushort m_VehicleMask;

		public ushort m_CrossingLeftMask;

		public ushort m_CrossingRightMask;

		public LaneDirectionType m_Left;

		public LaneDirectionType m_Forward;

		public LaneDirectionType m_Right;
	}

	private struct TrafficSignData
	{
		public Transform m_ParentTransform;

		public Transform m_ObjectTransform;

		public Transform m_LocalTransform;

		public float2 m_ForwardDirection;

		public Entity m_Prefab;

		public int m_Probability;

		public SubObjectFlags m_Flags;

		public TrafficSignNeeds m_TrafficSignNeeds;

		public bool m_IsLowered;
	}

	private struct StreetLightData
	{
		public Transform m_ParentTransform;

		public Transform m_ObjectTransform;

		public Transform m_LocalTransform;

		public Entity m_Prefab;

		public int m_Probability;

		public SubObjectFlags m_Flags;

		public StreetLightLayer m_Layer;

		public float m_Spacing;

		public float m_Priority;

		public bool m_IsLowered;
	}

	private struct UtilityObjectData
	{
		public float3 m_UtilityPosition;
	}

	private struct UtilityNodeData
	{
		public Transform m_Transform;

		public Entity m_Prefab;

		public PathNode m_PathNode;

		public int m_Count;

		public float m_LanePriority;

		public float m_NodePriority;

		public float m_Elevation;

		public UtilityTypes m_UtilityTypes;

		public bool m_Unsure;

		public bool m_Vertical;

		public bool m_IsNew;
	}

	private struct TargetLaneData
	{
		public CarLaneFlags m_CarLaneFlags;

		public CarLaneFlags m_AndCarLaneFlags;

		public float2 m_SpeedLimit;
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

	private struct UpdateSecondaryObjectsData
	{
		public NativeParallelMultiHashMap<Entity, Entity> m_OldEntities;

		public NativeParallelMultiHashMap<Entity, Entity> m_OriginalEntities;

		public NativeParallelHashSet<Entity> m_PlaceholderRequirements;

		public NativeParallelHashMap<PlaceholderKey, Unity.Mathematics.Random> m_SelectedSpawnabled;

		public NativeParallelHashMap<PathNode, TargetLaneData> m_SourceLanes;

		public NativeParallelHashMap<PathNode, TargetLaneData> m_TargetLanes;

		public NativeList<TrafficSignData> m_TrafficSigns;

		public NativeList<StreetLightData> m_StreetLights;

		public NativeList<UtilityObjectData> m_UtilityObjects;

		public NativeList<UtilityNodeData> m_UtilityNodes;

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
				m_PlaceholderRequirements = new NativeParallelHashSet<Entity>(10, allocator);
			}
		}

		public void EnsureSelectedSpawnables(Allocator allocator)
		{
			if (!m_SelectedSpawnabled.IsCreated)
			{
				m_SelectedSpawnabled = new NativeParallelHashMap<PlaceholderKey, Unity.Mathematics.Random>(10, allocator);
			}
		}

		public void EnsureSourceLanes(Allocator allocator)
		{
			if (!m_SourceLanes.IsCreated)
			{
				m_SourceLanes = new NativeParallelHashMap<PathNode, TargetLaneData>(16, allocator);
			}
		}

		public void EnsureTargetLanes(Allocator allocator)
		{
			if (!m_TargetLanes.IsCreated)
			{
				m_TargetLanes = new NativeParallelHashMap<PathNode, TargetLaneData>(16, allocator);
			}
		}

		public void EnsureTrafficSigns(Allocator allocator)
		{
			if (!m_TrafficSigns.IsCreated)
			{
				m_TrafficSigns = new NativeList<TrafficSignData>(16, allocator);
			}
		}

		public void EnsureStreetLights(Allocator allocator)
		{
			if (!m_StreetLights.IsCreated)
			{
				m_StreetLights = new NativeList<StreetLightData>(16, allocator);
			}
		}

		public void EnsureUtilityObjects(Allocator allocator)
		{
			if (!m_UtilityObjects.IsCreated)
			{
				m_UtilityObjects = new NativeList<UtilityObjectData>(16, allocator);
			}
		}

		public void EnsureUtilityNodes(Allocator allocator)
		{
			if (!m_UtilityNodes.IsCreated)
			{
				m_UtilityNodes = new NativeList<UtilityNodeData>(16, allocator);
			}
		}

		public void Clear()
		{
			if (m_OldEntities.IsCreated)
			{
				m_OldEntities.Clear();
			}
			if (m_OriginalEntities.IsCreated)
			{
				m_OriginalEntities.Clear();
			}
			if (m_PlaceholderRequirements.IsCreated)
			{
				m_PlaceholderRequirements.Clear();
			}
			if (m_SelectedSpawnabled.IsCreated)
			{
				m_SelectedSpawnabled.Clear();
			}
			if (m_SourceLanes.IsCreated)
			{
				m_SourceLanes.Clear();
			}
			if (m_TargetLanes.IsCreated)
			{
				m_TargetLanes.Clear();
			}
			if (m_TrafficSigns.IsCreated)
			{
				m_TrafficSigns.Clear();
			}
			if (m_StreetLights.IsCreated)
			{
				m_StreetLights.Clear();
			}
			if (m_UtilityObjects.IsCreated)
			{
				m_UtilityObjects.Clear();
			}
			if (m_UtilityNodes.IsCreated)
			{
				m_UtilityNodes.Clear();
			}
			m_RequirementsSearched = false;
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
			if (m_SourceLanes.IsCreated)
			{
				m_SourceLanes.Dispose();
			}
			if (m_TargetLanes.IsCreated)
			{
				m_TargetLanes.Dispose();
			}
			if (m_TrafficSigns.IsCreated)
			{
				m_TrafficSigns.Dispose();
			}
			if (m_StreetLights.IsCreated)
			{
				m_StreetLights.Dispose();
			}
			if (m_UtilityObjects.IsCreated)
			{
				m_UtilityObjects.Dispose();
			}
			if (m_UtilityNodes.IsCreated)
			{
				m_UtilityNodes.Dispose();
			}
		}
	}

	[BurstCompile]
	private struct FillUpdateMapJob : IJob
	{
		public NativeQueue<UpdateData> m_UpdateQueue;

		public NativeParallelMultiHashMap<Entity, UpdateData> m_UpdateMap;

		public void Execute()
		{
			UpdateData item;
			while (m_UpdateQueue.TryDequeue(out item))
			{
				m_UpdateMap.Add(item.m_Owner, item);
			}
		}
	}

	[BurstCompile]
	private struct SecondaryLaneAnchorJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> m_UtilityLaneType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> m_PrefabSubLanes;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, UpdateData> m_UpdateMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Owner> nativeArray = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Net.UtilityLane> nativeArray2 = chunk.GetNativeArray(ref m_UtilityLaneType);
			NativeArray<EdgeLane> nativeArray3 = chunk.GetNativeArray(ref m_EdgeLaneType);
			NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Game.Net.UtilityLane utilityLane = nativeArray2[i];
				if ((utilityLane.m_Flags & (UtilityLaneFlags.SecondaryStartAnchor | UtilityLaneFlags.SecondaryEndAnchor)) == 0)
				{
					continue;
				}
				Owner owner = nativeArray[i];
				Curve value = nativeArray4[i];
				PrefabRef prefabRef = nativeArray5[i];
				if ((utilityLane.m_Flags & UtilityLaneFlags.SecondaryStartAnchor) != 0)
				{
					Entity owner2 = owner.m_Owner;
					float3 x = -MathUtils.StartTangent(value.m_Bezier);
					x.y = 0f;
					x = math.normalizesafe(x);
					float requireDirection = float.MinValue;
					if (nativeArray3.Length != 0)
					{
						EdgeLane edgeLane = nativeArray3[i];
						if (edgeLane.m_EdgeDelta.x == 0f)
						{
							owner2 = m_EdgeData[owner.m_Owner].m_Start;
						}
						else if (edgeLane.m_EdgeDelta.x == 1f)
						{
							owner2 = m_EdgeData[owner.m_Owner].m_End;
						}
						if (edgeLane.m_EdgeDelta.x == edgeLane.m_EdgeDelta.y)
						{
							requireDirection = math.dot(x, value.m_Bezier.d - value.m_Bezier.a);
						}
					}
					value.m_Bezier.a = FindAnchorPosition(owner2, prefabRef.m_Prefab, value.m_Bezier.a, x, requireDirection);
				}
				if ((utilityLane.m_Flags & UtilityLaneFlags.SecondaryEndAnchor) != 0)
				{
					Entity owner3 = owner.m_Owner;
					float3 x2 = MathUtils.EndTangent(value.m_Bezier);
					x2.y = 0f;
					x2 = math.normalizesafe(x2);
					float requireDirection2 = float.MinValue;
					if (nativeArray3.Length != 0)
					{
						EdgeLane edgeLane2 = nativeArray3[i];
						if (edgeLane2.m_EdgeDelta.y == 0f)
						{
							owner3 = m_EdgeData[owner.m_Owner].m_Start;
						}
						else if (edgeLane2.m_EdgeDelta.y == 1f)
						{
							owner3 = m_EdgeData[owner.m_Owner].m_End;
						}
						if (edgeLane2.m_EdgeDelta.x == edgeLane2.m_EdgeDelta.y)
						{
							requireDirection2 = math.dot(x2, value.m_Bezier.a - value.m_Bezier.d);
						}
					}
					value.m_Bezier.d = FindAnchorPosition(owner3, prefabRef.m_Prefab, value.m_Bezier.d, x2, requireDirection2);
				}
				UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef.m_Prefab];
				if (utilityLaneData.m_Hanging != 0f)
				{
					value.m_Bezier.b = math.lerp(value.m_Bezier.a, value.m_Bezier.d, 1f / 3f);
					value.m_Bezier.c = math.lerp(value.m_Bezier.a, value.m_Bezier.d, 2f / 3f);
					float num = math.distance(value.m_Bezier.a.xz, value.m_Bezier.d.xz) * utilityLaneData.m_Hanging * 1.3333334f;
					value.m_Bezier.b.y -= num;
					value.m_Bezier.c.y -= num;
				}
				value.m_Length = MathUtils.Length(value.m_Bezier);
				nativeArray4[i] = value;
			}
		}

		private float3 FindAnchorPosition(Entity owner, Entity prefab, float3 position, float3 direction, float requireDirection)
		{
			float3 bestPosition = position;
			float bestDistance = float.MaxValue;
			DynamicBuffer<SubObject> bufferData;
			if (m_UpdatedData.HasComponent(owner))
			{
				if (m_UpdateMap.TryGetFirstValue(owner, out var item, out var it))
				{
					do
					{
						FindAnchorPosition(item.m_Prefab, prefab, item.m_Transform, position, direction, requireDirection, ref bestPosition, ref bestDistance);
					}
					while (m_UpdateMap.TryGetNextValue(out item, ref it));
				}
			}
			else if (m_SubObjects.TryGetBuffer(owner, out bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					SubObject subObject = bufferData[i];
					PrefabRef prefabRef = m_PrefabRefData[subObject.m_SubObject];
					Transform ownerTransform = m_TransformData[subObject.m_SubObject];
					FindAnchorPosition(prefabRef.m_Prefab, prefab, ownerTransform, position, direction, requireDirection, ref bestPosition, ref bestDistance);
				}
			}
			return bestPosition;
		}

		private void FindAnchorPosition(Entity ownerPrefab, Entity lanePrefab, Transform ownerTransform, float3 lanePosition, float3 laneDirection, float requireDirection, ref float3 bestPosition, ref float bestDistance)
		{
			if (!m_PrefabSubLanes.TryGetBuffer(ownerPrefab, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Prefabs.SubLane subLane = bufferData[i];
				if (subLane.m_Prefab != lanePrefab || subLane.m_NodeIndex.x != subLane.m_NodeIndex.y)
				{
					continue;
				}
				float3 @float = ObjectUtils.LocalToWorld(ownerTransform, subLane.m_Curve.a);
				float3 float2 = @float - lanePosition;
				if (requireDirection != float.MinValue)
				{
					if (math.dot(laneDirection, float2) < requireDirection)
					{
						continue;
					}
				}
				else
				{
					float2 -= laneDirection * (math.dot(laneDirection, float2) * 0.75f);
				}
				float num = math.lengthsq(float2);
				if (num < bestDistance)
				{
					bestPosition = @float;
					bestDistance = num;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckBorderDistrictsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> m_BorderDistrictType;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public NativeHashSet<Entity> m_Districts;

		public NativeQueue<SubObjectOwnerData>.ParallelWriter m_OwnerQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Net.Edge> nativeArray2 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<BorderDistrict> nativeArray3 = chunk.GetNativeArray(ref m_BorderDistrictType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				BorderDistrict borderDistrict = nativeArray3[i];
				if (m_Districts.Contains(borderDistrict.m_Left) || m_Districts.Contains(borderDistrict.m_Right))
				{
					m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity, Entity.Null, temp: false, deleted: false));
					if (CollectionUtils.TryGet(nativeArray2, i, out var value))
					{
						AddConnectedEdges(entity, value.m_Start);
						AddConnectedEdges(entity, value.m_End);
					}
				}
			}
		}

		private void AddConnectedEdges(Entity edge, Entity node)
		{
			if (!m_ConnectedEdges.TryGetBuffer(node, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				ConnectedEdge connectedEdge = bufferData[i];
				if (connectedEdge.m_Edge != edge && m_SubObjects.HasBuffer(connectedEdge.m_Edge) && (!m_BorderDistrictData.TryGetComponent(connectedEdge.m_Edge, out var componentData) || (!m_Districts.Contains(componentData.m_Left) && !m_Districts.Contains(componentData.m_Right))))
				{
					m_OwnerQueue.Enqueue(new SubObjectOwnerData(connectedEdge.m_Edge, Entity.Null, temp: false, deleted: false));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
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
		public ComponentTypeHandle<ChangedDistrict> m_ChangedDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> m_EdgeType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<ChangedDistrict> m_ChangedDistrictData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SubObjectOwnerData>.ParallelWriter m_OwnerQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					DynamicBuffer<SubObject> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subObject = dynamicBuffer[j].m_SubObject;
						if (!m_DeletedData.HasComponent(subObject) && m_SecondaryData.HasComponent(subObject) && m_OwnerData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == entity)
						{
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, subObject, in m_AppliedTypes);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subObject, default(Deleted));
							if (m_SubObjects.HasBuffer(subObject))
							{
								m_OwnerQueue.Enqueue(new SubObjectOwnerData(subObject, Entity.Null, temp: false, deleted: true));
							}
						}
					}
				}
				return;
			}
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Game.Net.Edge> nativeArray3 = chunk.GetNativeArray(ref m_EdgeType);
			bool flag = chunk.Has(ref m_ChangedDistrictType);
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity entity2 = nativeArray[k];
				if (nativeArray2.Length != 0)
				{
					Temp temp = nativeArray2[k];
					if ((temp.m_Flags & TempFlags.Replace) != 0)
					{
						m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity2, Entity.Null, temp: true, deleted: false));
					}
					else
					{
						m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity2, temp.m_Original, temp: true, deleted: false));
					}
				}
				else
				{
					m_OwnerQueue.Enqueue(new SubObjectOwnerData(entity2, Entity.Null, temp: false, deleted: false));
				}
				if (flag)
				{
					if (CollectionUtils.TryGet(nativeArray3, k, out var value))
					{
						AddConnectedEdges(entity2, value.m_Start);
						AddConnectedEdges(entity2, value.m_End);
					}
					m_CommandBuffer.RemoveComponent<ChangedDistrict>(unfilteredChunkIndex, entity2);
				}
			}
		}

		private void AddConnectedEdges(Entity edge, Entity node)
		{
			if (!m_ConnectedEdges.TryGetBuffer(node, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				ConnectedEdge connectedEdge = bufferData[i];
				if (connectedEdge.m_Edge != edge && m_SubObjects.HasBuffer(connectedEdge.m_Edge) && !m_DeletedData.HasComponent(connectedEdge.m_Edge) && !m_ChangedDistrictData.HasComponent(connectedEdge.m_Edge))
				{
					m_OwnerQueue.Enqueue(new SubObjectOwnerData(connectedEdge.m_Edge, Entity.Null, temp: false, deleted: false));
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

		public void Execute()
		{
			int count = m_OwnerQueue.Count;
			if (count != 0)
			{
				m_OwnerList.ResizeUninitialized(count);
				for (int i = 0; i < count; i++)
				{
					m_OwnerList[i] = m_OwnerQueue.Dequeue();
				}
				MergeOwners();
			}
		}

		private void MergeOwners()
		{
			m_OwnerList.Sort();
			SubObjectOwnerData value = default(SubObjectOwnerData);
			int num = 0;
			int num2 = 0;
			while (num < m_OwnerList.Length)
			{
				SubObjectOwnerData subObjectOwnerData = m_OwnerList[num++];
				if (subObjectOwnerData.m_Owner != value.m_Owner)
				{
					if (value.m_Owner != Entity.Null && !value.m_Deleted)
					{
						m_OwnerList[num2++] = value;
					}
					value = subObjectOwnerData;
				}
				else if (subObjectOwnerData.m_Original != Entity.Null)
				{
					subObjectOwnerData.m_Deleted |= value.m_Deleted;
					value = subObjectOwnerData;
				}
				else
				{
					value.m_Deleted |= subObjectOwnerData.m_Deleted;
				}
			}
			if (value.m_Owner != Entity.Null && !value.m_Deleted)
			{
				m_OwnerList[num2++] = value;
			}
			if (num2 < m_OwnerList.Length)
			{
				m_OwnerList.RemoveRange(num2, m_OwnerList.Length - num2);
			}
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
		public ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<TrafficLightData> m_PrefabTrafficLightData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.TrafficSignData> m_PrefabTrafficSignData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.StreetLightData> m_PrefabStreetLightData;

		[ReadOnly]
		public ComponentLookup<LaneDirectionData> m_PrefabLaneDirectionData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<ThemeData> m_PrefabThemeData;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> m_PrefabPlaceholderObjectData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.UtilityObjectData> m_PrefabUtilityObjectData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<TrafficLight> m_TrafficLightData;

		[ReadOnly]
		public ComponentLookup<StreetLight> m_StreetLightData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NetNodeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_NetEdgeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_NetCompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<LaneSignal> m_LaneSignalData;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> m_UtilityLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<TrafficLights> m_TrafficLightsData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<NetCompositionObject> m_NetCompositionObjects;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PlaceholderObjects;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_ObjectRequirements;

		[ReadOnly]
		public BufferLookup<DefaultNetLane> m_DefaultNetLanes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Entity m_DefaultTheme;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		[ReadOnly]
		public ComponentTypeSet m_SecondaryOwnerTypes;

		[ReadOnly]
		public ComponentTypeSet m_TempAnimationTypes;

		[ReadOnly]
		public NativeArray<SubObjectOwnerData> m_OwnerList;

		public NativeQueue<UpdateData>.ParallelWriter m_UpdateQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			SubObjectOwnerData subObjectOwnerData = m_OwnerList[index];
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool isNative = m_NativeData.HasComponent(subObjectOwnerData.m_Owner);
			if (m_NetNodeData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag = true;
			}
			else if (m_NetEdgeData.HasComponent(subObjectOwnerData.m_Owner))
			{
				flag2 = true;
			}
			else if (m_AreaNodes.HasBuffer(subObjectOwnerData.m_Owner))
			{
				flag3 = true;
			}
			UpdateSecondaryObjectsData updateData = default(UpdateSecondaryObjectsData);
			DynamicBuffer<SubObject> subObjects = m_SubObjects[subObjectOwnerData.m_Owner];
			FillOldSubObjectsBuffer(subObjectOwnerData.m_Owner, subObjects, ref updateData);
			if (subObjectOwnerData.m_Original != Entity.Null && m_SubObjects.HasBuffer(subObjectOwnerData.m_Original))
			{
				DynamicBuffer<SubObject> subObjects2 = m_SubObjects[subObjectOwnerData.m_Original];
				FillOriginalSubObjectsBuffer(subObjectOwnerData.m_Original, subObjects2, ref updateData);
			}
			Temp ownerTemp = default(Temp);
			if (subObjectOwnerData.m_Temp)
			{
				ownerTemp = m_TempData[subObjectOwnerData.m_Owner];
			}
			Unity.Mathematics.Random random = ((!m_PseudoRandomSeedData.HasComponent(subObjectOwnerData.m_Owner)) ? m_RandomSeed.GetRandom(index) : m_PseudoRandomSeedData[subObjectOwnerData.m_Owner].GetRandom(PseudoRandomSeed.kSecondaryObject));
			bool hasStreetLights = false;
			bool alwaysLit = false;
			if (flag)
			{
				CreateSecondaryNodeObjects(index, ref random, subObjectOwnerData.m_Owner, ref updateData, subObjectOwnerData.m_Temp, isNative, ownerTemp, out hasStreetLights, out alwaysLit);
			}
			else if (flag2)
			{
				CreateSecondaryEdgeObjects(index, ref random, subObjectOwnerData.m_Owner, ref updateData, subObjectOwnerData.m_Temp, isNative, ownerTemp, out hasStreetLights, out alwaysLit);
			}
			else if (flag3)
			{
				CreateSecondaryAreaObjects(index, ref random, subObjectOwnerData.m_Owner, ref updateData, subObjectOwnerData.m_Temp, isNative, ownerTemp);
			}
			if (m_RoadData.TryGetComponent(subObjectOwnerData.m_Owner, out var componentData))
			{
				if (hasStreetLights)
				{
					componentData.m_Flags |= Game.Net.RoadFlags.IsLit;
					if (alwaysLit)
					{
						componentData.m_Flags |= Game.Net.RoadFlags.AlwaysLit;
					}
					else
					{
						componentData.m_Flags &= ~Game.Net.RoadFlags.AlwaysLit;
					}
				}
				else
				{
					componentData.m_Flags &= ~(Game.Net.RoadFlags.IsLit | Game.Net.RoadFlags.AlwaysLit);
				}
				m_RoadData[subObjectOwnerData.m_Owner] = componentData;
			}
			RemoveUnusedOldSubObjects(index, subObjectOwnerData.m_Owner, subObjects, ref updateData);
			updateData.Dispose();
		}

		private void FillOldSubObjectsBuffer(Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSecondaryObjectsData updateData)
		{
			if (subObjects.Length == 0)
			{
				return;
			}
			updateData.EnsureOldEntities(Allocator.Temp);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_OwnerData.HasComponent(subObject) && m_SecondaryData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == owner)
				{
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					updateData.m_OldEntities.Add(prefabRef.m_Prefab, subObject);
				}
			}
		}

		private void FillOriginalSubObjectsBuffer(Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSecondaryObjectsData updateData)
		{
			if (subObjects.Length == 0)
			{
				return;
			}
			updateData.EnsureOriginalEntities(Allocator.Temp);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_OwnerData.HasComponent(subObject) && m_SecondaryData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == owner)
				{
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					updateData.m_OriginalEntities.Add(prefabRef.m_Prefab, subObject);
				}
			}
		}

		private void RemoveUnusedOldSubObjects(int jobIndex, Entity owner, DynamicBuffer<SubObject> subObjects, ref UpdateSecondaryObjectsData updateData)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_OwnerData.HasComponent(subObject) && m_SecondaryData.HasComponent(subObject) && m_OwnerData[subObject].m_Owner == owner)
				{
					PrefabRef prefabRef = m_PrefabRefData[subObject];
					if (updateData.m_OldEntities.TryGetFirstValue(prefabRef.m_Prefab, out var item, out var it))
					{
						m_CommandBuffer.RemoveComponent(jobIndex, item, in m_AppliedTypes);
						m_CommandBuffer.AddComponent(jobIndex, item, default(Deleted));
						updateData.m_OldEntities.Remove(it);
					}
				}
			}
		}

		private float Remap(float t, float4 syncOffsets, float4 syncTargets)
		{
			if (t < syncOffsets.x)
			{
				return syncTargets.x * math.saturate(t / syncOffsets.x);
			}
			if (t < syncOffsets.y)
			{
				return math.lerp(syncTargets.x, syncTargets.y, math.saturate((t - syncOffsets.x) / (syncOffsets.y - syncOffsets.x)));
			}
			if (t < syncOffsets.z)
			{
				return math.lerp(syncTargets.y, syncTargets.z, math.saturate((t - syncOffsets.y) / (syncOffsets.z - syncOffsets.y)));
			}
			if (t < syncOffsets.w)
			{
				return math.lerp(syncTargets.z, syncTargets.w, math.saturate((t - syncOffsets.z) / (syncOffsets.w - syncOffsets.z)));
			}
			return math.lerp(syncTargets.w, 1f, math.saturate((t - syncOffsets.w) / (1f - syncOffsets.w)));
		}

		private Bezier4x3 LerpRemap(Bezier4x3 left, Bezier4x3 right, float t, float4 syncOffsets, float4 syncTargets)
		{
			float t2 = Remap(t, syncOffsets, syncTargets);
			float num = math.distance(left.a.xz, right.a.xz);
			float num2 = math.distance(left.d.xz, right.d.xz);
			float t3 = 0.5f;
			syncTargets = math.lerp(syncOffsets * num, syncTargets * num2, t3) / math.max(1E-06f, math.lerp(num, num2, t3));
			t3 = Remap(t, syncOffsets, syncTargets);
			Bezier4x3 result = default(Bezier4x3);
			result.a = math.lerp(left.a, right.a, t);
			result.b = math.lerp(left.b, right.b, t);
			result.c = math.lerp(left.c, right.c, t3);
			result.d = math.lerp(left.d, right.d, t2);
			return result;
		}

		private Bezier4x3 LerpRemap2(Bezier4x3 left, Bezier4x3 right, float t, float4 syncOffsets, float4 syncTargets)
		{
			float t2 = Remap(t, syncOffsets, syncTargets);
			Bezier4x3 result = default(Bezier4x3);
			result.a = math.lerp(left.a, right.a, t2);
			result.b = math.lerp(left.b, right.b, t2);
			result.c = math.lerp(left.c, right.c, t2);
			result.d = math.lerp(left.d, right.d, t2);
			return result;
		}

		private void CreateSecondaryNodeObjects(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, ref UpdateSecondaryObjectsData updateData, bool isTemp, bool isNative, Temp ownerTemp, out bool hasStreetLights, out bool alwaysLit)
		{
			alwaysLit = false;
			float num = 0f;
			float3 x = default(float3);
			int num2 = 0;
			float ownerElevation = 0f;
			if (m_NetElevationData.HasComponent(owner))
			{
				ownerElevation = math.cmin(m_NetElevationData[owner].m_Elevation);
			}
			uint num3 = 0u;
			uint num4 = 0u;
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				int num5 = 0;
				if (updateData.m_TrafficSigns.IsCreated)
				{
					num5 = updateData.m_TrafficSigns.Length;
				}
				if (!value.m_Middle)
				{
					Composition composition = m_NetCompositionData[value.m_Edge];
					EdgeGeometry edgeGeometry = m_EdgeGeometryData[value.m_Edge];
					NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition.m_Edge];
					EdgeNodeGeometry geometry;
					NetCompositionData netCompositionData2;
					DynamicBuffer<NetCompositionObject> dynamicBuffer;
					if (value.m_End)
					{
						geometry = m_EndNodeGeometryData[value.m_Edge].m_Geometry;
						netCompositionData2 = m_PrefabNetCompositionData[composition.m_EndNode];
						dynamicBuffer = m_NetCompositionObjects[composition.m_EndNode];
					}
					else
					{
						geometry = m_StartNodeGeometryData[value.m_Edge].m_Geometry;
						netCompositionData2 = m_PrefabNetCompositionData[composition.m_StartNode];
						dynamicBuffer = m_NetCompositionObjects[composition.m_StartNode];
					}
					alwaysLit |= (netCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 && ((netCompositionData2.m_Flags.m_Left | netCompositionData2.m_Flags.m_Right) & (CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition)) == 0;
					bool isLowered = ((netCompositionData2.m_Flags.m_Left | netCompositionData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0;
					float3 @float = ((!value.m_End) ? (-(MathUtils.StartTangent(edgeGeometry.m_Start.m_Left) + MathUtils.StartTangent(edgeGeometry.m_Start.m_Right))) : (MathUtils.EndTangent(edgeGeometry.m_End.m_Left) + MathUtils.StartTangent(edgeGeometry.m_End.m_Right)));
					float2 x2;
					if (geometry.m_MiddleRadius > 0f)
					{
						num += math.max(math.distance(geometry.m_Middle.a, geometry.m_Middle.d) * 2f, geometry.m_Left.m_Length.x + geometry.m_Left.m_Length.y + geometry.m_Right.m_Length.x + geometry.m_Right.m_Length.y);
						x2 = MathUtils.StartTangent(geometry.m_Left.m_Left).xz + MathUtils.StartTangent(geometry.m_Left.m_Right).xz;
					}
					else
					{
						num += math.max(math.distance(geometry.m_Middle.a, geometry.m_Middle.d) * 2f, geometry.m_Left.m_Length.x + geometry.m_Right.m_Length.y);
						x2 = ((!(math.any(geometry.m_Left.m_Length > 0.05f) | math.any(geometry.m_Right.m_Length > 0.05f))) ? @float.xz : (MathUtils.StartTangent(geometry.m_Left.m_Left).xz + MathUtils.StartTangent(geometry.m_Right.m_Right).xz));
					}
					PrefabRef prefabRef = m_PrefabRefData[value.m_Edge];
					NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
					TrafficSignNeeds trafficSignNeeds = new TrafficSignNeeds
					{
						m_Left = LaneDirectionType.None,
						m_Forward = LaneDirectionType.None,
						m_Right = LaneDirectionType.None
					};
					if ((netGeometryData.m_MergeLayers & (Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad)) != Layer.None)
					{
						x2 = math.normalizesafe(x2);
						float num6 = math.atan2(x2.x, x2.y);
						uint num7 = (uint)(1 << (Mathf.FloorToInt(num6 * (16f / MathF.PI)) & 0x1F));
						if (((uint)netCompositionData.m_State & (uint)(value.m_End ? 8 : 64)) != 0)
						{
							num3 |= num7;
						}
						if (((uint)netCompositionData.m_State & (uint)(value.m_End ? 64 : 8)) != 0)
						{
							num4 |= num7;
						}
						if ((netCompositionData.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) == (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes))
						{
							trafficSignNeeds.m_RemoveSignTypeMask2 = Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.DoNotEnter) | Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Oneway);
						}
						if ((netCompositionData2.m_Flags.m_General & CompositionFlags.General.Intersection) == 0)
						{
							trafficSignNeeds.m_RemoveSignTypeMask = Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Street) | Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnLeft) | Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnRight) | Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnLeft) | Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnRight);
						}
						if ((netGeometryData.m_IntersectLayers & Layer.Waterway) == 0 || (netCompositionData.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) == 0 || (netCompositionData2.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0)
						{
							trafficSignNeeds.m_RemoveSignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.MoveableBridge);
						}
					}
					x = geometry.m_Middle.d;
					num2++;
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						NetCompositionObject netCompositionObject = dynamicBuffer[i];
						float num8 = netCompositionObject.m_Position.x / math.max(1f, netCompositionData2.m_Width) + 0.5f;
						float num9 = netCompositionData2.m_MiddleOffset / math.max(1f, netCompositionData2.m_Width) + 0.5f;
						float2 float2 = new float2(math.cmin(netCompositionObject.m_CurveOffsetRange), math.cmax(netCompositionObject.m_CurveOffsetRange));
						if (float2.y > 0.5f)
						{
							float2 = ((!(float2.x > 0.5f)) ? new float2((float2.x + 1f - float2.y) * 0.5f, 0.5f) : (1f - float2.yx));
						}
						float num10 = math.lerp(float2.x, float2.y, random.NextFloat(1f));
						float num11;
						float3 position;
						float3 float4;
						float3 x3;
						if ((netCompositionObject.m_Flags & (SubObjectFlags.OnMedian | SubObjectFlags.PreserveShape)) != 0)
						{
							Bezier4x3 middle = geometry.m_Middle;
							float3 float3;
							float3 end;
							if (geometry.m_MiddleRadius > 0f)
							{
								float3 = geometry.m_Left.m_Right.a - geometry.m_Left.m_Left.a;
								end = geometry.m_Right.m_Right.d - geometry.m_Right.m_Left.d;
							}
							else
							{
								float3 = geometry.m_Right.m_Right.a - geometry.m_Left.m_Left.a;
								end = geometry.m_Right.m_Right.d - geometry.m_Left.m_Left.d;
							}
							if ((netCompositionObject.m_Flags & SubObjectFlags.PreserveShape) != 0)
							{
								end = float3;
							}
							num11 = MathUtils.Length(middle.xz);
							if (num11 < 0.01f)
							{
								position = middle.a + float3 * (num8 - num9);
								float4 = ((!(geometry.m_MiddleRadius > 0f)) ? (MathUtils.StartTangent(geometry.m_Left.m_Left) + MathUtils.StartTangent(geometry.m_Right.m_Right)) : (MathUtils.StartTangent(geometry.m_Left.m_Left) + MathUtils.StartTangent(geometry.m_Left.m_Right)));
								x3 = float4;
							}
							else
							{
								float4 = MathUtils.StartTangent(middle);
								float num12 = num10 * 2f * num11 + netCompositionObject.m_Position.y;
								if (num12 > 0.001f)
								{
									Bounds1 t = new Bounds1(0f, 1f);
									MathUtils.ClampLength(middle.xz, ref t, num12);
									position = MathUtils.Position(middle, t.max) + math.lerp(float3, end, t.max) * (num8 - num9);
									x3 = MathUtils.Tangent(middle, t.max);
								}
								else
								{
									position = middle.a + float3 * (num8 - num9);
									x3 = float4;
								}
							}
							if (geometry.m_MiddleRadius == 0f && math.all(geometry.m_Left.m_Length <= 0.05f) && math.all(geometry.m_Right.m_Length < 0.05f))
							{
								float4 = @float;
								x3 = @float;
							}
						}
						else if (geometry.m_MiddleRadius > 0f)
						{
							MathUtils.Divide(geometry.m_Middle, out var output, out var output2, 0.99f);
							Bezier4x3 curve;
							Bezier4x3 curve2;
							if (num8 >= num9)
							{
								curve = LerpRemap(output, geometry.m_Left.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), netCompositionData2.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight);
								curve2 = LerpRemap2(output2, geometry.m_Right.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), netCompositionData2.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight);
							}
							else
							{
								curve = LerpRemap(geometry.m_Left.m_Left, output, num8 / math.max(1E-05f, num9), netCompositionData2.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft);
								curve2 = LerpRemap2(geometry.m_Right.m_Left, output2, num8 / math.max(1E-05f, num9), netCompositionData2.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft);
							}
							float num13 = MathUtils.Length(curve.xz);
							float num14 = MathUtils.Length(curve2.xz);
							num11 = num13 + num14;
							float4 = MathUtils.StartTangent(curve);
							float num15 = num10 * 2f * num11 + netCompositionObject.m_Position.y;
							if (num15 > num13)
							{
								Bounds1 t2 = new Bounds1(0f, 1f);
								MathUtils.ClampLength(curve2.xz, ref t2, num15 - num13);
								position = MathUtils.Position(curve2, t2.max);
								x3 = MathUtils.Tangent(curve2, t2.max);
							}
							else if (num15 > 0.001f)
							{
								Bounds1 t3 = new Bounds1(0f, 1f);
								MathUtils.ClampLength(curve.xz, ref t3, num15);
								position = MathUtils.Position(curve, t3.max);
								x3 = MathUtils.Tangent(curve, t3.max);
							}
							else
							{
								position = curve.a;
								x3 = float4;
							}
						}
						else
						{
							Bezier4x3 curve3 = ((!(num8 >= num9)) ? LerpRemap(geometry.m_Left.m_Left, geometry.m_Left.m_Right, num8 / math.max(1E-05f, num9), netCompositionData2.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft) : LerpRemap(geometry.m_Right.m_Left, geometry.m_Right.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), netCompositionData2.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight));
							num11 = MathUtils.Length(curve3.xz);
							float4 = MathUtils.StartTangent(curve3);
							float num16 = num10 * 2f * num11 + netCompositionObject.m_Position.y;
							if (num16 > 0.001f)
							{
								Bounds1 t4 = new Bounds1(0f, 1f);
								MathUtils.ClampLength(curve3.xz, ref t4, num16);
								position = MathUtils.Position(curve3, t4.max);
								x3 = MathUtils.Tangent(curve3, t4.max);
							}
							else
							{
								position = curve3.a;
								x3 = float4;
							}
							if (math.all(geometry.m_Left.m_Length <= 0.05f) && math.all(geometry.m_Right.m_Length < 0.05f))
							{
								float4 = @float;
								x3 = @float;
							}
						}
						float4.y = math.lerp(0f, float4.y, netCompositionObject.m_UseCurveRotation.x);
						x3.y = math.lerp(0f, x3.y, netCompositionObject.m_UseCurveRotation.x);
						float4 = math.normalizesafe(float4, new float3(0f, 0f, 1f));
						x3 = math.normalizesafe(x3, float4);
						quaternion q = quaternion.LookRotationSafe(float4, math.up());
						quaternion q2 = quaternion.LookRotationSafe(x3, math.up());
						quaternion rotation = math.slerp(q, q2, netCompositionObject.m_UseCurveRotation.y);
						Transform transform = new Transform(position, rotation);
						Transform transform2 = new Transform(netCompositionObject.m_Offset, netCompositionObject.m_Rotation);
						Transform transform3 = ObjectUtils.LocalToWorld(transform, transform2);
						if (netCompositionObject.m_Probability < 100)
						{
							netCompositionObject.m_Probability = math.clamp(Mathf.RoundToInt((float)netCompositionObject.m_Probability * (num11 / netGeometryData.m_EdgeLengthRange.max)), 1, netCompositionObject.m_Probability);
						}
						if (m_PrefabStreetLightData.HasComponent(netCompositionObject.m_Prefab))
						{
							Game.Prefabs.StreetLightData streetLightData = m_PrefabStreetLightData[netCompositionObject.m_Prefab];
							updateData.EnsureStreetLights(Allocator.Temp);
							int num17 = 0;
							while (true)
							{
								if (num17 < updateData.m_StreetLights.Length)
								{
									if (math.distancesq(updateData.m_StreetLights[num17].m_ObjectTransform.m_Position, transform3.m_Position) < 1f)
									{
										break;
									}
									num17++;
									continue;
								}
								ref NativeList<StreetLightData> reference = ref updateData.m_StreetLights;
								StreetLightData value2 = new StreetLightData
								{
									m_ParentTransform = transform,
									m_ObjectTransform = transform3,
									m_LocalTransform = transform2,
									m_Prefab = netCompositionObject.m_Prefab,
									m_Probability = netCompositionObject.m_Probability,
									m_Flags = netCompositionObject.m_Flags,
									m_Layer = streetLightData.m_Layer,
									m_Spacing = netCompositionObject.m_Spacing,
									m_Priority = num11,
									m_IsLowered = isLowered
								};
								reference.Add(in value2);
								break;
							}
							continue;
						}
						if (m_PrefabTrafficSignData.HasComponent(netCompositionObject.m_Prefab) || m_PrefabTrafficLightData.HasComponent(netCompositionObject.m_Prefab))
						{
							updateData.EnsureTrafficSigns(Allocator.Temp);
							ref NativeList<TrafficSignData> reference2 = ref updateData.m_TrafficSigns;
							TrafficSignData value3 = new TrafficSignData
							{
								m_ParentTransform = transform,
								m_ObjectTransform = transform3,
								m_LocalTransform = transform2,
								m_ForwardDirection = math.normalizesafe(math.forward(transform3.m_Rotation).xz),
								m_Prefab = netCompositionObject.m_Prefab,
								m_Probability = netCompositionObject.m_Probability,
								m_Flags = netCompositionObject.m_Flags,
								m_TrafficSignNeeds = trafficSignNeeds,
								m_IsLowered = isLowered
							};
							reference2.Add(in value3);
							continue;
						}
						if (m_PrefabUtilityObjectData.HasComponent(netCompositionObject.m_Prefab))
						{
							Game.Prefabs.UtilityObjectData utilityObjectData = m_PrefabUtilityObjectData[netCompositionObject.m_Prefab];
							float3 float5 = ObjectUtils.LocalToWorld(transform3, utilityObjectData.m_UtilityPosition);
							updateData.EnsureUtilityObjects(Allocator.Temp);
							int num18 = 0;
							while (num18 < updateData.m_UtilityObjects.Length)
							{
								if (!(math.distancesq(updateData.m_UtilityObjects[num18].m_UtilityPosition, float5) < 1f))
								{
									num18++;
									continue;
								}
								goto IL_0fec;
							}
							ref NativeList<UtilityObjectData> reference3 = ref updateData.m_UtilityObjects;
							UtilityObjectData value4 = new UtilityObjectData
							{
								m_UtilityPosition = float5
							};
							reference3.Add(in value4);
						}
						CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, transform, transform3, transform2, netCompositionObject.m_Flags, default(TrafficSignNeeds), ref updateData, netCompositionObject.m_Prefab, 0, netCompositionObject.m_Probability);
						IL_0fec:;
					}
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = m_SubLanes[value.m_Edge];
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					Entity subLane = dynamicBuffer2[j].m_SubLane;
					if (m_SecondaryLaneData.HasComponent(subLane))
					{
						continue;
					}
					if (m_UtilityLaneData.HasComponent(subLane))
					{
						PrefabRef prefabRef2 = m_PrefabRefData[subLane];
						UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef2.m_Prefab];
						if (!(utilityLaneData.m_NodeObjectPrefab != Entity.Null))
						{
							continue;
						}
						Curve curve4 = m_CurveData[subLane];
						float num19 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData.m_NodeObjectPrefab].m_Bounds));
						bool2 x4 = false;
						bool flag = false;
						if (m_EdgeLaneData.HasComponent(subLane))
						{
							if (!value.m_Middle)
							{
								EdgeLane edgeLane = m_EdgeLaneData[subLane];
								x4 = (!value.m_End & (edgeLane.m_EdgeDelta == 0f)) | (value.m_End & (edgeLane.m_EdgeDelta == 1f));
							}
						}
						else if (value.m_Middle)
						{
							x4 = new bool2(x: false, y: true);
							flag = true;
						}
						if (!math.any(x4))
						{
							continue;
						}
						updateData.EnsureUtilityNodes(Allocator.Temp);
						for (int k = 0; k < updateData.m_UtilityNodes.Length; k++)
						{
							UtilityNodeData value5 = updateData.m_UtilityNodes[k];
							if ((value5.m_UtilityTypes & utilityLaneData.m_UtilityTypes) == 0)
							{
								continue;
							}
							if (x4.x && math.distancesq(value5.m_Transform.m_Position, curve4.m_Bezier.a) < 0.01f)
							{
								value5.m_Unsure &= flag;
								if (!flag && num19 > value5.m_NodePriority)
								{
									value5.m_Prefab = utilityLaneData.m_NodeObjectPrefab;
									value5.m_NodePriority = num19;
								}
								if (num19 > value5.m_LanePriority)
								{
									value5.m_LanePriority = num19;
									value5.m_Count = 1;
									value5.m_Vertical = flag;
								}
								else if (num19 == value5.m_LanePriority)
								{
									value5.m_Count++;
									value5.m_Vertical |= flag;
								}
								updateData.m_UtilityNodes[k] = value5;
								x4.x = false;
								if (!x4.y)
								{
									break;
								}
							}
							if (x4.y && math.distancesq(value5.m_Transform.m_Position, curve4.m_Bezier.d) < 0.01f)
							{
								value5.m_Unsure &= flag;
								if (!flag && num19 > value5.m_NodePriority)
								{
									value5.m_Prefab = utilityLaneData.m_NodeObjectPrefab;
									value5.m_NodePriority = num19;
								}
								if (num19 > value5.m_LanePriority)
								{
									value5.m_LanePriority = num19;
									value5.m_Count = 1;
									value5.m_Vertical = flag;
								}
								else if (num19 == value5.m_LanePriority)
								{
									value5.m_Count++;
									value5.m_Vertical |= flag;
								}
								updateData.m_UtilityNodes[k] = value5;
								x4.y = false;
								if (!x4.x)
								{
									break;
								}
							}
						}
						if (x4.x)
						{
							UtilityNodeData value6 = new UtilityNodeData
							{
								m_Transform = new Transform(curve4.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve4.m_Bezier))),
								m_Prefab = utilityLaneData.m_NodeObjectPrefab,
								m_Count = 1,
								m_LanePriority = num19,
								m_NodePriority = math.select(num19, 0f, flag),
								m_UtilityTypes = utilityLaneData.m_UtilityTypes,
								m_Unsure = flag,
								m_Vertical = flag
							};
							updateData.m_UtilityNodes.Add(in value6);
						}
						if (x4.y)
						{
							UtilityNodeData value7 = new UtilityNodeData
							{
								m_Transform = new Transform(curve4.m_Bezier.d, NetUtils.GetNodeRotation(MathUtils.EndTangent(curve4.m_Bezier))),
								m_Prefab = utilityLaneData.m_NodeObjectPrefab,
								m_Count = 1,
								m_LanePriority = num19,
								m_NodePriority = math.select(num19, 0f, flag),
								m_UtilityTypes = utilityLaneData.m_UtilityTypes,
								m_Unsure = flag,
								m_Vertical = flag
							};
							updateData.m_UtilityNodes.Add(in value7);
						}
					}
					else
					{
						if (!m_CarLaneData.HasComponent(subLane) || m_MasterLaneData.HasComponent(subLane) || !m_EdgeLaneData.HasComponent(subLane))
						{
							continue;
						}
						Game.Net.CarLane carLane = m_CarLaneData[subLane];
						bool2 x5 = m_EdgeLaneData[subLane].m_EdgeDelta == math.select(0f, 1f, value.m_End);
						if (!math.any(x5))
						{
							continue;
						}
						Lane lane = m_LaneData[subLane];
						PathNode key = (x5.x ? lane.m_StartNode : lane.m_EndNode);
						if (x5.x && key.OwnerEquals(new PathNode(owner, 0)) && updateData.m_TrafficSigns.IsCreated && updateData.m_TrafficSigns.Length > num5)
						{
							Curve curve5 = m_CurveData[subLane];
							int num20 = -1;
							float num21 = float.MaxValue;
							float2 float6 = math.normalizesafe(MathUtils.StartTangent(curve5.m_Bezier).xz);
							float3 a = curve5.m_Bezier.a;
							a.xz += MathUtils.Right(float6) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
							for (int l = num5; l < updateData.m_TrafficSigns.Length; l++)
							{
								TrafficSignData trafficSignData = updateData.m_TrafficSigns[l];
								if (math.dot(float6, trafficSignData.m_ForwardDirection) > 0.70710677f)
								{
									float num22 = math.distance(a, trafficSignData.m_ObjectTransform.m_Position);
									if (num22 < num21)
									{
										num20 = l;
										num21 = num22;
									}
								}
							}
							if (num20 != -1)
							{
								TrafficSignData value8 = updateData.m_TrafficSigns[num20];
								value8.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.MoveableBridge);
								updateData.m_TrafficSigns[num20] = value8;
							}
						}
						if (!key.OwnerEquals(new PathNode(value.m_Edge, 0)))
						{
							continue;
						}
						updateData.EnsureTargetLanes(Allocator.Temp);
						CarLaneFlags carLaneFlags = carLane.m_Flags;
						if (m_SlaveLaneData.TryGetComponent(subLane, out var componentData))
						{
							for (int m = componentData.m_MinIndex; m <= componentData.m_MaxIndex; m++)
							{
								if (m != j && m_CarLaneData.TryGetComponent(dynamicBuffer2[m].m_SubLane, out var componentData2))
								{
									carLaneFlags |= componentData2.m_Flags;
								}
							}
						}
						TargetLaneData item = new TargetLaneData
						{
							m_CarLaneFlags = carLane.m_Flags,
							m_AndCarLaneFlags = carLaneFlags,
							m_SpeedLimit = carLane.m_DefaultSpeedLimit
						};
						if (!updateData.m_TargetLanes.TryAdd(key, item))
						{
							UnityEngine.Debug.Log($"SecondaryObjectSystem: Duplicate node for lane {subLane.Index}");
						}
					}
				}
			}
			hasStreetLights = updateData.m_StreetLights.IsCreated && updateData.m_StreetLights.Length != 0;
			bool flag2 = updateData.m_TrafficSigns.IsCreated && updateData.m_TrafficSigns.Length != 0;
			if (hasStreetLights)
			{
				while (updateData.m_StreetLights.Length != 0)
				{
					StreetLightLayer streetLightLayer = updateData.m_StreetLights[0].m_Layer;
					float num23 = -1f;
					int num24 = 0;
					float num25 = 0f;
					int num26 = 0;
					for (int n = 0; n < updateData.m_StreetLights.Length; n++)
					{
						StreetLightData streetLightData2 = updateData.m_StreetLights[n];
						if (streetLightData2.m_Layer == streetLightLayer)
						{
							float num27 = math.distance(x, streetLightData2.m_ObjectTransform.m_Position) + streetLightData2.m_Priority;
							if (num27 > num23)
							{
								num23 = num27;
								num24 = n;
							}
							num25 += streetLightData2.m_Spacing;
							num26++;
						}
					}
					if (num24 != 0)
					{
						StreetLightData value9 = updateData.m_StreetLights[num24];
						updateData.m_StreetLights[num24] = updateData.m_StreetLights[0];
						updateData.m_StreetLights[0] = value9;
					}
					int num28 = math.min(num26, Mathf.RoundToInt(num * (float)num26 / math.max(1f, num25)));
					num28 = math.select(num28, 2, num28 == 3 && num2 == 4);
					for (int num29 = 1; num29 < num28; num29++)
					{
						num23 = -1f;
						num24 = num29;
						for (int num30 = num29; num30 < updateData.m_StreetLights.Length; num30++)
						{
							StreetLightData streetLightData3 = updateData.m_StreetLights[num30];
							if (streetLightData3.m_Layer == streetLightLayer)
							{
								float num31 = float.MaxValue;
								for (int num32 = 0; num32 < num29; num32++)
								{
									num31 = math.min(num31, math.distance(streetLightData3.m_ObjectTransform.m_Position, updateData.m_StreetLights[num32].m_ObjectTransform.m_Position));
								}
								num31 += streetLightData3.m_Priority;
								if (num31 > num23)
								{
									num23 = num31;
									num24 = num30;
								}
							}
						}
						if (num24 != num29)
						{
							StreetLightData value10 = updateData.m_StreetLights[num24];
							updateData.m_StreetLights[num24] = updateData.m_StreetLights[num29];
							updateData.m_StreetLights[num29] = value10;
						}
					}
					for (int num33 = 0; num33 < num28; num33++)
					{
						StreetLightData streetLightData4 = updateData.m_StreetLights[num33];
						CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, streetLightData4.m_IsLowered, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, streetLightData4.m_ParentTransform, streetLightData4.m_ObjectTransform, streetLightData4.m_LocalTransform, streetLightData4.m_Flags, default(TrafficSignNeeds), ref updateData, streetLightData4.m_Prefab, 0, streetLightData4.m_Probability);
					}
					int num34 = 0;
					while (num34 < updateData.m_StreetLights.Length)
					{
						if (updateData.m_StreetLights[num34].m_Layer != streetLightLayer)
						{
							num34++;
						}
						else
						{
							updateData.m_StreetLights.RemoveAtSwapBack(num34);
						}
					}
				}
			}
			if (num2 == 0)
			{
				PrefabRef prefabRef3 = m_PrefabRefData[owner];
				if (m_DefaultNetLanes.TryGetBuffer(prefabRef3.m_Prefab, out var bufferData))
				{
					Game.Net.Node node = m_NetNodeData[owner];
					NetGeometryData netGeometryData2 = m_PrefabNetGeometryData[prefabRef3.m_Prefab];
					for (int num35 = 0; num35 < bufferData.Length; num35++)
					{
						NetCompositionLane netCompositionLane = new NetCompositionLane(bufferData[num35]);
						if ((netCompositionLane.m_Flags & LaneFlags.Utility) == 0 || (netCompositionLane.m_Flags & LaneFlags.FindAnchor) != 0)
						{
							continue;
						}
						bool flag3 = (netCompositionLane.m_Flags & LaneFlags.Invert) != 0;
						if (((uint)netCompositionLane.m_Flags & (uint)(flag3 ? 512 : 256)) == 0)
						{
							UtilityLaneData utilityLaneData2 = m_PrefabUtilityLaneData[netCompositionLane.m_Lane];
							if (utilityLaneData2.m_NodeObjectPrefab != Entity.Null)
							{
								float num36 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData2.m_NodeObjectPrefab].m_Bounds));
								netCompositionLane.m_Position.x = 0f - netCompositionLane.m_Position.x;
								float t5 = netCompositionLane.m_Position.x / math.max(1f, netGeometryData2.m_DefaultWidth) + 0.5f;
								float3 end2 = node.m_Position + math.rotate(node.m_Rotation, new float3(netGeometryData2.m_DefaultWidth * -0.5f, 0f, 0f));
								float3 position2 = math.lerp(node.m_Position + math.rotate(node.m_Rotation, new float3(netGeometryData2.m_DefaultWidth * 0.5f, 0f, 0f)), end2, t5);
								UtilityNodeData value11 = new UtilityNodeData
								{
									m_Transform = 
									{
										m_Position = position2
									}
								};
								value11.m_Transform.m_Position.y += netCompositionLane.m_Position.y;
								value11.m_Transform.m_Rotation = quaternion.identity;
								value11.m_Prefab = utilityLaneData2.m_NodeObjectPrefab;
								value11.m_Count = 1;
								value11.m_Elevation = netCompositionLane.m_Position.y;
								value11.m_LanePriority = num36;
								value11.m_NodePriority = num36;
								value11.m_UtilityTypes = utilityLaneData2.m_UtilityTypes;
								updateData.EnsureUtilityNodes(Allocator.Temp);
								updateData.m_UtilityNodes.Add(in value11);
							}
						}
					}
				}
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer3 = m_SubLanes[owner];
			for (int num37 = 0; num37 < dynamicBuffer3.Length; num37++)
			{
				Entity subLane2 = dynamicBuffer3[num37].m_SubLane;
				if (m_SecondaryLaneData.HasComponent(subLane2))
				{
					continue;
				}
				if (m_UtilityLaneData.HasComponent(subLane2))
				{
					PrefabRef prefabRef4 = m_PrefabRefData[subLane2];
					UtilityLaneData utilityLaneData3 = m_PrefabUtilityLaneData[prefabRef4.m_Prefab];
					if (!(utilityLaneData3.m_NodeObjectPrefab != Entity.Null))
					{
						continue;
					}
					Curve curve6 = m_CurveData[subLane2];
					if (curve6.m_Length <= 0.1f)
					{
						continue;
					}
					Game.Net.UtilityLane utilityLane = m_UtilityLaneData[subLane2];
					float num38 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData3.m_NodeObjectPrefab].m_Bounds));
					bool2 @bool = false;
					bool flag4 = (utilityLane.m_Flags & UtilityLaneFlags.VerticalConnection) != 0;
					updateData.EnsureUtilityNodes(Allocator.Temp);
					for (int num39 = 0; num39 < updateData.m_UtilityNodes.Length; num39++)
					{
						UtilityNodeData value12 = updateData.m_UtilityNodes[num39];
						if ((value12.m_UtilityTypes & utilityLaneData3.m_UtilityTypes) == 0)
						{
							continue;
						}
						if (!@bool.x && math.distancesq(value12.m_Transform.m_Position, curve6.m_Bezier.a) < 0.01f)
						{
							value12.m_Unsure = false;
							if (!flag4 && num38 > value12.m_NodePriority)
							{
								value12.m_Prefab = utilityLaneData3.m_NodeObjectPrefab;
								value12.m_NodePriority = num38;
							}
							if (num38 > value12.m_LanePriority)
							{
								value12.m_LanePriority = num38;
								value12.m_Count = 1;
								value12.m_Vertical = flag4;
							}
							else if (num38 == value12.m_LanePriority)
							{
								value12.m_Count++;
								value12.m_Vertical |= flag4;
							}
							updateData.m_UtilityNodes[num39] = value12;
							@bool.x = true;
							if (@bool.y)
							{
								break;
							}
						}
						if (!@bool.y && math.distancesq(value12.m_Transform.m_Position, curve6.m_Bezier.d) < 0.01f)
						{
							value12.m_Unsure = false;
							if (!flag4 && num38 > value12.m_NodePriority)
							{
								value12.m_Prefab = utilityLaneData3.m_NodeObjectPrefab;
								value12.m_NodePriority = num38;
							}
							if (num38 > value12.m_LanePriority)
							{
								value12.m_LanePriority = num38;
								value12.m_Count = 1;
								value12.m_Vertical = flag4;
							}
							else if (num38 == value12.m_LanePriority)
							{
								value12.m_Count++;
								value12.m_Vertical |= flag4;
							}
							updateData.m_UtilityNodes[num39] = value12;
							@bool.y = true;
							if (@bool.x)
							{
								break;
							}
						}
					}
					if (!@bool.x)
					{
						float elevation = 0f;
						if (m_NetElevationData.TryGetComponent(subLane2, out var componentData3) && componentData3.m_Elevation.x != float.MinValue)
						{
							elevation = componentData3.m_Elevation.x;
						}
						UtilityNodeData value13 = new UtilityNodeData
						{
							m_Transform = new Transform(curve6.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve6.m_Bezier))),
							m_Prefab = utilityLaneData3.m_NodeObjectPrefab,
							m_Count = 1,
							m_Elevation = elevation,
							m_LanePriority = num38,
							m_NodePriority = num38,
							m_UtilityTypes = utilityLaneData3.m_UtilityTypes,
							m_Vertical = flag4
						};
						updateData.m_UtilityNodes.Add(in value13);
					}
					if (!@bool.y)
					{
						float elevation2 = 0f;
						if (m_NetElevationData.TryGetComponent(subLane2, out var componentData4) && componentData4.m_Elevation.y != float.MinValue)
						{
							elevation2 = componentData4.m_Elevation.y;
						}
						UtilityNodeData value14 = new UtilityNodeData
						{
							m_Transform = new Transform(curve6.m_Bezier.d, NetUtils.GetNodeRotation(MathUtils.EndTangent(curve6.m_Bezier))),
							m_Prefab = utilityLaneData3.m_NodeObjectPrefab,
							m_Count = 1,
							m_Elevation = elevation2,
							m_LanePriority = num38,
							m_NodePriority = num38,
							m_UtilityTypes = utilityLaneData3.m_UtilityTypes,
							m_Vertical = flag4
						};
						updateData.m_UtilityNodes.Add(in value14);
					}
				}
				else
				{
					if (!flag2)
					{
						continue;
					}
					if (m_CarLaneData.HasComponent(subLane2))
					{
						if (m_MasterLaneData.HasComponent(subLane2))
						{
							continue;
						}
						Game.Net.CarLane carLane2 = m_CarLaneData[subLane2];
						Lane lane2 = m_LaneData[subLane2];
						Curve curve7 = m_CurveData[subLane2];
						PrefabRef prefabRef5 = m_PrefabRefData[subLane2];
						bool flag5 = (num3 | num4) != 0;
						if (flag5 && m_PrefabCarLaneData.TryGetComponent(prefabRef5.m_Prefab, out var componentData5))
						{
							flag5 = componentData5.m_RoadTypes == RoadTypes.Bicycle;
						}
						if ((carLane2.m_Flags & CarLaneFlags.Unsafe) != 0)
						{
							carLane2.m_Flags &= CarLaneFlags.Unsafe | CarLaneFlags.Highway | CarLaneFlags.RightLimit | CarLaneFlags.LeftLimit;
						}
						bool flag6 = (carLane2.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.RightLimit | CarLaneFlags.LeftLimit)) != 0;
						bool flag7 = m_LaneSignalData.HasComponent(subLane2);
						int num40 = -1;
						int num41 = -1;
						int num42 = -1;
						int num43 = -1;
						float num44 = float.MaxValue;
						float num45 = float.MaxValue;
						float num46 = float.MaxValue;
						float num47 = float.MaxValue;
						float2 float7 = math.normalizesafe(MathUtils.StartTangent(curve7.m_Bezier).xz);
						float2 float8 = math.normalizesafe(MathUtils.EndTangent(curve7.m_Bezier).xz);
						float3 a2 = curve7.m_Bezier.a;
						float3 a3 = curve7.m_Bezier.a;
						float3 d = curve7.m_Bezier.d;
						float3 d2 = curve7.m_Bezier.d;
						a2.xz += MathUtils.Right(float7) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
						a3.xz += MathUtils.Left(float7) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
						d.xz += MathUtils.Right(float8) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
						d2.xz += MathUtils.Left(float8) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
						for (int num48 = 0; num48 < updateData.m_TrafficSigns.Length; num48++)
						{
							TrafficSignData trafficSignData2 = updateData.m_TrafficSigns[num48];
							float num49 = math.dot(float7, trafficSignData2.m_ForwardDirection);
							float num50 = math.dot(float8, trafficSignData2.m_ForwardDirection);
							if (num49 < -0.70710677f)
							{
								float num51 = math.distance(a2, trafficSignData2.m_ObjectTransform.m_Position);
								float num52 = math.distance(a3, trafficSignData2.m_ObjectTransform.m_Position);
								if (num51 < num44)
								{
									num40 = num48;
									num44 = num51;
								}
								if (num52 < num45)
								{
									num41 = num48;
									num45 = num52;
								}
							}
							if (num50 > 0.70710677f)
							{
								float num53 = math.distance(d, trafficSignData2.m_ObjectTransform.m_Position);
								float num54 = math.distance(d2, trafficSignData2.m_ObjectTransform.m_Position);
								if (num53 < num46)
								{
									num42 = num48;
									num46 = num53;
								}
								if (num54 < num47)
								{
									num43 = num48;
									num47 = num54;
								}
							}
						}
						float num55 = math.atan2(float7.x, float7.y);
						float num56 = math.atan2(float8.x, float8.y);
						int num57 = Mathf.FloorToInt(num55 * (16f / MathF.PI)) & 0x1F;
						int num58 = (Mathf.FloorToInt(num56 * (16f / MathF.PI)) + 16) & 0x1F;
						if ((flag6 || flag7) && num40 != -1)
						{
							TrafficSignData value15 = updateData.m_TrafficSigns[num40];
							if (flag6 && updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var item2))
							{
								if ((carLane2.m_Flags & CarLaneFlags.Stop) != 0)
								{
									value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Stop);
								}
								if ((carLane2.m_Flags & item2.m_AndCarLaneFlags & CarLaneFlags.Yield) != 0)
								{
									value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Yield);
								}
								if (!flag5)
								{
									if ((carLane2.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
									{
										value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnLeft);
									}
									else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
									{
										uint num59 = (uint)(2032 << num57) | math.select(2032u >> 32 - num57, 0u, num57 == 0);
										if (((num3 | num4) & num59) != 0)
										{
											value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnLeft);
										}
									}
									if ((carLane2.m_Flags & CarLaneFlags.UTurnLeft) != 0)
									{
										value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnLeft);
									}
									else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
									{
										uint num60 = (uint)(14 << num57) | math.select(14u >> 32 - num57, 0u, num57 == 0);
										if (((num3 | num4) & num60) != 0)
										{
											value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnLeft);
										}
									}
									if ((carLane2.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnRight)) != 0)
									{
										value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnRight);
									}
									else if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
									{
										uint num61 = (uint)(532676608 << num57) | math.select(532676608u >> 32 - num57, 0u, num57 == 0);
										if (((num3 | num4) & num61) != 0)
										{
											value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnRight);
										}
									}
									if ((carLane2.m_Flags & CarLaneFlags.UTurnRight) != 0)
									{
										value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnRight);
									}
									else if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
									{
										uint num62 = (uint)(-536870912 << num57) | math.select(3758096384u >> 32 - num57, 0u, num57 == 0);
										if (((num3 | num4) & num62) != 0)
										{
											value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnRight);
										}
									}
								}
							}
							if (flag7)
							{
								LaneSignal laneSignal = m_LaneSignalData[subLane2];
								float num63 = math.dot(MathUtils.Right(float7), value15.m_ObjectTransform.m_Position.xz - curve7.m_Bezier.a.xz);
								if (num63 > 0f)
								{
									value15.m_TrafficSignNeeds.m_VehicleLanesRight = math.max(value15.m_TrafficSignNeeds.m_VehicleLanesRight, num63);
								}
								else
								{
									value15.m_TrafficSignNeeds.m_VehicleLanesLeft = math.max(value15.m_TrafficSignNeeds.m_VehicleLanesLeft, 0f - num63);
								}
								value15.m_TrafficSignNeeds.m_VehicleMask |= laneSignal.m_GroupMask;
							}
							updateData.m_TrafficSigns[num40] = value15;
						}
						if (flag6 && !flag5 && num41 != -1)
						{
							TrafficSignData value16 = updateData.m_TrafficSigns[num41];
							if (flag6 && updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var _))
							{
								if (m_LeftHandTraffic)
								{
									if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
									{
										uint num64 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
										if ((num3 & num64) != 0)
										{
											value16.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.DoNotEnter);
										}
									}
								}
								else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
								{
									uint num65 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
									if ((num3 & num65) != 0)
									{
										value16.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.DoNotEnter);
									}
								}
							}
							updateData.m_TrafficSigns[num41] = value16;
						}
						if (num42 != -1)
						{
							TrafficSignData value17 = updateData.m_TrafficSigns[num42];
							if (updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_EndNode, out var item4))
							{
								if (m_LeftHandTraffic)
								{
									if ((item4.m_CarLaneFlags & (CarLaneFlags.Highway | CarLaneFlags.LeftLimit)) == CarLaneFlags.LeftLimit && (carLane2.m_Flags & CarLaneFlags.TurnLeft) != 0)
									{
										value17.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Street);
									}
								}
								else if ((item4.m_CarLaneFlags & (CarLaneFlags.Highway | CarLaneFlags.RightLimit)) == CarLaneFlags.RightLimit && (carLane2.m_Flags & CarLaneFlags.TurnRight) != 0)
								{
									value17.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Street);
								}
								if (!flag5)
								{
									uint num66 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
									uint num67 = (uint)(536870896 << num58) | math.select(536870896u >> 32 - num58, 0u, num58 == 0);
									if ((num3 & num66) != 0 || (num4 & num67) != 0)
									{
										value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Oneway);
									}
								}
								if ((item4.m_CarLaneFlags & ~carLane2.m_Flags & CarLaneFlags.Highway) != 0)
								{
									value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Motorway);
								}
								if (math.abs(item4.m_SpeedLimit.x - carLane2.m_DefaultSpeedLimit) > 1f)
								{
									value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit);
									value17.m_TrafficSignNeeds.m_SpeedLimit2 = (ushort)Mathf.RoundToInt(item4.m_SpeedLimit.x * 3.6f);
								}
								value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.MoveableBridge);
								CarLaneFlags carLaneFlags2 = (m_LeftHandTraffic ? CarLaneFlags.ParkingLeft : CarLaneFlags.ParkingRight);
								if ((item4.m_CarLaneFlags & carLaneFlags2) != 0)
								{
									if (updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var item5))
									{
										if ((item5.m_CarLaneFlags & carLaneFlags2) == 0 && (carLane2.m_Flags & CarLaneFlags.Unsafe) == 0)
										{
											value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
										}
									}
									else
									{
										value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
									}
								}
							}
							updateData.m_TrafficSigns[num42] = value17;
						}
						if (num43 == -1)
						{
							continue;
						}
						TrafficSignData value18 = updateData.m_TrafficSigns[num43];
						if (updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_EndNode, out var item6))
						{
							CarLaneFlags carLaneFlags3 = (m_LeftHandTraffic ? CarLaneFlags.ParkingRight : CarLaneFlags.ParkingLeft);
							if ((item6.m_CarLaneFlags & carLaneFlags3) != 0)
							{
								if (updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var item7))
								{
									if ((item7.m_CarLaneFlags & carLaneFlags3) == 0 && (carLane2.m_Flags & CarLaneFlags.Unsafe) == 0)
									{
										value18.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
									}
								}
								else
								{
									value18.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
								}
							}
						}
						updateData.m_TrafficSigns[num43] = value18;
					}
					else
					{
						if (!m_PedestrianLaneData.HasComponent(subLane2) || (m_PedestrianLaneData[subLane2].m_Flags & PedestrianLaneFlags.Crosswalk) == 0 || !m_LaneSignalData.HasComponent(subLane2))
						{
							continue;
						}
						LaneSignal laneSignal2 = m_LaneSignalData[subLane2];
						Curve curve8 = m_CurveData[subLane2];
						int num68 = -1;
						int num69 = -1;
						float num70 = float.MaxValue;
						float num71 = float.MaxValue;
						float2 float9 = math.normalizesafe(MathUtils.StartTangent(curve8.m_Bezier).xz);
						float2 float10 = math.normalizesafe(MathUtils.EndTangent(curve8.m_Bezier).xz);
						for (int num72 = 0; num72 < updateData.m_TrafficSigns.Length; num72++)
						{
							TrafficSignData trafficSignData3 = updateData.m_TrafficSigns[num72];
							float num73 = 1f + math.distance(curve8.m_Bezier.a, trafficSignData3.m_ObjectTransform.m_Position);
							float num74 = 1f + math.distance(curve8.m_Bezier.d, trafficSignData3.m_ObjectTransform.m_Position);
							num73 *= 1f + math.abs(math.dot(float9, trafficSignData3.m_ForwardDirection));
							num74 *= 1f + math.abs(math.dot(float10, trafficSignData3.m_ForwardDirection));
							if (num73 < num70)
							{
								num68 = num72;
								num70 = num73;
							}
							if (num74 < num71)
							{
								num69 = num72;
								num71 = num74;
							}
						}
						if (num68 != -1)
						{
							TrafficSignData value19 = updateData.m_TrafficSigns[num68];
							if (math.dot(MathUtils.Right(float9), value19.m_ForwardDirection) > 0f)
							{
								value19.m_TrafficSignNeeds.m_CrossingLeftMask |= laneSignal2.m_GroupMask;
							}
							else
							{
								value19.m_TrafficSignNeeds.m_CrossingRightMask |= laneSignal2.m_GroupMask;
							}
							updateData.m_TrafficSigns[num68] = value19;
						}
						if (num69 != -1)
						{
							TrafficSignData value20 = updateData.m_TrafficSigns[num69];
							if (math.dot(MathUtils.Right(float10), value20.m_ForwardDirection) > 0f)
							{
								value20.m_TrafficSignNeeds.m_CrossingRightMask |= laneSignal2.m_GroupMask;
							}
							else
							{
								value20.m_TrafficSignNeeds.m_CrossingLeftMask |= laneSignal2.m_GroupMask;
							}
							updateData.m_TrafficSigns[num69] = value20;
						}
					}
				}
			}
			bool flag8 = updateData.m_UtilityNodes.IsCreated && updateData.m_UtilityNodes.Length != 0;
			if (flag2)
			{
				for (int num75 = 0; num75 < updateData.m_TrafficSigns.Length; num75++)
				{
					TrafficSignData trafficSignData4 = updateData.m_TrafficSigns[num75];
					trafficSignData4.m_TrafficSignNeeds.m_SignTypeMask &= ~trafficSignData4.m_TrafficSignNeeds.m_RemoveSignTypeMask;
					trafficSignData4.m_TrafficSignNeeds.m_SignTypeMask2 &= ~trafficSignData4.m_TrafficSignNeeds.m_RemoveSignTypeMask2;
					if (trafficSignData4.m_TrafficSignNeeds.m_SignTypeMask != 0 || trafficSignData4.m_TrafficSignNeeds.m_SignTypeMask2 != 0 || trafficSignData4.m_TrafficSignNeeds.m_VehicleMask != 0 || trafficSignData4.m_TrafficSignNeeds.m_CrossingLeftMask != 0 || trafficSignData4.m_TrafficSignNeeds.m_CrossingRightMask != 0)
					{
						CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, trafficSignData4.m_IsLowered, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, trafficSignData4.m_ParentTransform, trafficSignData4.m_ObjectTransform, trafficSignData4.m_LocalTransform, trafficSignData4.m_Flags, trafficSignData4.m_TrafficSignNeeds, ref updateData, trafficSignData4.m_Prefab, 0, trafficSignData4.m_Probability);
					}
				}
			}
			if (!flag8)
			{
				return;
			}
			for (int num76 = 0; num76 < updateData.m_UtilityNodes.Length; num76++)
			{
				UtilityNodeData utilityNodeData = updateData.m_UtilityNodes[num76];
				if (!utilityNodeData.m_Unsure && (utilityNodeData.m_Count != 2 || utilityNodeData.m_Vertical))
				{
					Transform localTransformData = default(Transform);
					localTransformData.m_Position.y += utilityNodeData.m_Elevation;
					CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered: false, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, utilityNodeData.m_Transform, utilityNodeData.m_Transform, localTransformData, SubObjectFlags.AnchorCenter, default(TrafficSignNeeds), ref updateData, utilityNodeData.m_Prefab, 0, 100);
				}
			}
		}

		private void AddNodeLanes(Entity node, Entity edge, ref bool forbidBicycles, ref UpdateSecondaryObjectsData updateData)
		{
			if (!m_SubLanes.TryGetBuffer(node, out var bufferData))
			{
				return;
			}
			if (forbidBicycles)
			{
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					if (!(value.m_Edge == edge))
					{
						Composition composition = m_NetCompositionData[value.m_Edge];
						NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition.m_Edge];
						bool flag = (((value.m_End == ((netCompositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0)) ? netCompositionData.m_Flags.m_Left : netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.ForbidSecondary) != 0;
						if (!flag && m_BorderDistrictData.TryGetComponent(value.m_Edge, out var componentData))
						{
							flag |= AreaUtils.CheckOption(componentData, DistrictOption.ForbidBicycles, ref m_DistrictData);
						}
						forbidBicycles &= flag;
					}
				}
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) == 0 || m_SecondaryLaneData.HasComponent(subLane.m_SubLane) || m_MasterLaneData.HasComponent(subLane.m_SubLane) || !m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2))
				{
					continue;
				}
				if ((componentData2.m_Flags & CarLaneFlags.Unsafe) == 0)
				{
					Lane lane = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData3) && componentData3.m_MasterIndex < bufferData.Length)
					{
						lane = m_LaneData[bufferData[componentData3.m_MasterIndex].m_SubLane];
					}
					if (forbidBicycles || (subLane.m_PathMethods & PathMethod.Bicycle) == 0)
					{
						componentData2.m_Flags |= CarLaneFlags.ForbidBicycles;
					}
					updateData.EnsureSourceLanes(Allocator.Temp);
					if (updateData.m_SourceLanes.TryGetValue(lane.m_EndNode, out var item))
					{
						item.m_CarLaneFlags |= componentData2.m_Flags;
						item.m_AndCarLaneFlags &= componentData2.m_Flags;
						item.m_SpeedLimit.x = math.min(item.m_SpeedLimit.x, componentData2.m_DefaultSpeedLimit);
						item.m_SpeedLimit.y = math.max(item.m_SpeedLimit.y, componentData2.m_DefaultSpeedLimit);
						updateData.m_SourceLanes[lane.m_EndNode] = item;
					}
					else
					{
						item = new TargetLaneData
						{
							m_CarLaneFlags = componentData2.m_Flags,
							m_AndCarLaneFlags = componentData2.m_Flags,
							m_SpeedLimit = componentData2.m_DefaultSpeedLimit
						};
						updateData.m_SourceLanes.Add(lane.m_EndNode, item);
					}
				}
				else if ((componentData2.m_Flags & CarLaneFlags.Forbidden) != 0)
				{
					Lane lane2 = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData4) && componentData4.m_MasterIndex < bufferData.Length)
					{
						lane2 = m_LaneData[bufferData[componentData4.m_MasterIndex].m_SubLane];
					}
					updateData.EnsureTargetLanes(Allocator.Temp);
					if (updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var item2))
					{
						item2.m_CarLaneFlags |= componentData2.m_Flags;
						item2.m_AndCarLaneFlags &= componentData2.m_Flags;
						item2.m_SpeedLimit.x = math.min(item2.m_SpeedLimit.x, componentData2.m_DefaultSpeedLimit);
						item2.m_SpeedLimit.y = math.max(item2.m_SpeedLimit.y, componentData2.m_DefaultSpeedLimit);
						updateData.m_TargetLanes[lane2.m_StartNode] = item2;
					}
					else
					{
						item2 = new TargetLaneData
						{
							m_CarLaneFlags = componentData2.m_Flags,
							m_AndCarLaneFlags = componentData2.m_Flags,
							m_SpeedLimit = componentData2.m_DefaultSpeedLimit
						};
						updateData.m_TargetLanes.Add(lane2.m_StartNode, item2);
					}
				}
			}
		}

		private void AddEdgeLanes(Entity edge, ref UpdateSecondaryObjectsData updateData)
		{
			if (!m_SubLanes.TryGetBuffer(edge, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) != 0 && !m_SecondaryLaneData.HasComponent(subLane.m_SubLane) && !m_MasterLaneData.HasComponent(subLane.m_SubLane) && m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData) && (componentData.m_Flags & CarLaneFlags.Unsafe) == 0)
				{
					Lane lane = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2) && componentData2.m_MasterIndex < bufferData.Length)
					{
						lane = m_LaneData[bufferData[componentData2.m_MasterIndex].m_SubLane];
					}
					if (updateData.m_SourceLanes.IsCreated && updateData.m_SourceLanes.TryGetValue(lane.m_StartNode, out var item) && (item.m_CarLaneFlags & CarLaneFlags.Approach) == 0)
					{
						item.m_CarLaneFlags |= CarLaneFlags.Approach;
						updateData.m_SourceLanes.TryAdd(lane.m_EndNode, item);
					}
					if (updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane.m_EndNode, out var item2) && (item2.m_CarLaneFlags & CarLaneFlags.Approach) == 0)
					{
						item2.m_CarLaneFlags |= CarLaneFlags.Approach;
						updateData.m_TargetLanes.TryAdd(lane.m_StartNode, item2);
					}
				}
			}
		}

		private void CreateSecondaryEdgeObjects(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, ref UpdateSecondaryObjectsData updateData, bool isTemp, bool isNative, Temp ownerTemp, out bool hasStreetLights, out bool alwaysLit)
		{
			Composition composition = m_NetCompositionData[owner];
			EdgeGeometry edgeGeometry = m_EdgeGeometryData[owner];
			PrefabRef prefabRef = m_PrefabRefData[owner];
			NetGeometryData netGeometryData = m_PrefabNetGeometryData[prefabRef.m_Prefab];
			float ownerElevation = 0f;
			if (m_NetElevationData.TryGetComponent(owner, out var componentData))
			{
				ownerElevation = math.cmin(componentData.m_Elevation);
			}
			m_SubReplacements.TryGetBuffer(owner, out var bufferData);
			NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition.m_Edge];
			DynamicBuffer<NetCompositionObject> dynamicBuffer = m_NetCompositionObjects[composition.m_Edge];
			bool flag = false;
			bool isLowered = ((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0;
			hasStreetLights = false;
			alwaysLit = (netCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
			bool2 @bool = new bool2((netCompositionData.m_Flags.m_Left & CompositionFlags.Side.ForbidSecondary) != 0, (netCompositionData.m_Flags.m_Right & CompositionFlags.Side.ForbidSecondary) != 0);
			if ((netCompositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
			{
				@bool = @bool.yx;
			}
			if (!math.all(@bool) && m_BorderDistrictData.TryGetComponent(owner, out var componentData2))
			{
				@bool |= AreaUtils.CheckOption(componentData2, DistrictOption.ForbidBicycles, ref m_DistrictData);
			}
			bool2 bool2 = @bool;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				NetCompositionObject netCompositionObject = dynamicBuffer[i];
				float num = edgeGeometry.m_Start.middleLength + edgeGeometry.m_End.middleLength;
				float num2 = netCompositionObject.m_Position.y;
				int num3 = 1;
				if ((netCompositionObject.m_Flags & SubObjectFlags.EvenSpacing) != 0)
				{
					NetCompositionData netCompositionData2 = m_PrefabNetCompositionData[composition.m_StartNode];
					NetCompositionData netCompositionData3 = m_PrefabNetCompositionData[composition.m_EndNode];
					if ((netCompositionData2.m_Flags.m_General & netCompositionObject.m_SpacingIgnore) == 0)
					{
						EdgeNodeGeometry geometry = m_StartNodeGeometryData[owner].m_Geometry;
						float x = (geometry.m_Left.middleLength + geometry.m_Right.middleLength) * 0.5f;
						x = math.min(x, netCompositionObject.m_Spacing * (1f / 3f));
						num += x;
						num2 -= x;
					}
					if ((netCompositionData3.m_Flags.m_General & netCompositionObject.m_SpacingIgnore) == 0)
					{
						EdgeNodeGeometry geometry2 = m_EndNodeGeometryData[owner].m_Geometry;
						float x2 = (geometry2.m_Left.middleLength + geometry2.m_Right.middleLength) * 0.5f;
						x2 = math.min(x2, netCompositionObject.m_Spacing * (1f / 3f));
						num += x2;
					}
				}
				if (num < netCompositionObject.m_MinLength)
				{
					continue;
				}
				if (netCompositionObject.m_Spacing > 0.1f)
				{
					num3 = Mathf.FloorToInt(num / netCompositionObject.m_Spacing + 0.5f);
					num3 = (((netCompositionObject.m_Flags & SubObjectFlags.EvenSpacing) == 0) ? math.select(num3, 1, (num3 == 0) & (num > netCompositionObject.m_Spacing * 0.1f)) : (num3 - 1));
					if (netCompositionObject.m_AvoidSpacing > 0.1f)
					{
						int num4 = Mathf.FloorToInt(num / netCompositionObject.m_AvoidSpacing + 0.5f);
						num4 = (((netCompositionObject.m_Flags & SubObjectFlags.EvenSpacing) == 0) ? math.select(num4, 1, (num4 == 0) & (num > netCompositionObject.m_AvoidSpacing * 0.1f)) : (num4 - 1));
						if ((num3 & 1) == (num4 & 1))
						{
							int2 @int = num3 + new int2(-1, 1);
							float2 @float = math.abs((float2)@int * netCompositionObject.m_Spacing - num);
							num3 = math.select(@int.x, @int.y, @float.y < @float.x || @int.x == 0);
						}
					}
				}
				if (num3 <= 0)
				{
					continue;
				}
				float t = netCompositionObject.m_Position.x / math.max(1f, netCompositionData.m_Width) + 0.5f;
				Bezier4x3 curve = MathUtils.Lerp(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, t);
				Bezier4x3 curve2 = MathUtils.Lerp(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, t);
				float num5 = math.lerp(netCompositionObject.m_CurveOffsetRange.x, netCompositionObject.m_CurveOffsetRange.y, random.NextFloat(1f));
				float num6;
				if ((netCompositionObject.m_Flags & SubObjectFlags.EvenSpacing) != 0)
				{
					num5 += 0.5f;
					num6 = num / (float)(num3 + 1);
				}
				else
				{
					num6 = num / (float)num3;
				}
				SubReplacementType subReplacementType = SubReplacementType.None;
				Game.Tools.AgeMask ageMask = (Game.Tools.AgeMask)0;
				bool flag2 = m_PrefabTrafficSignData.HasComponent(netCompositionObject.m_Prefab);
				bool flag3 = m_PrefabLaneDirectionData.HasComponent(netCompositionObject.m_Prefab);
				if (!hasStreetLights)
				{
					hasStreetLights = m_PrefabStreetLightData.HasComponent(netCompositionObject.m_Prefab);
				}
				if (m_PrefabPlaceableObjectData.TryGetComponent(netCompositionObject.m_Prefab, out var componentData3))
				{
					subReplacementType = componentData3.m_SubReplacementType;
				}
				if (subReplacementType != SubReplacementType.None && bufferData.IsCreated)
				{
					SubReplacementSide subReplacementSide = (((netCompositionObject.m_Flags & SubObjectFlags.OnMedian) == 0) ? ((netCompositionObject.m_Position.x >= 0f) ? SubReplacementSide.Right : SubReplacementSide.Left) : SubReplacementSide.Middle);
					for (int j = 0; j < bufferData.Length; j++)
					{
						SubReplacement subReplacement = bufferData[j];
						if (subReplacement.m_Type == subReplacementType && subReplacement.m_Side == subReplacementSide)
						{
							netCompositionObject.m_Prefab = subReplacement.m_Prefab;
							ageMask = subReplacement.m_AgeMask;
						}
					}
				}
				for (int k = 0; k < num3; k++)
				{
					float num7 = ((float)k + num5) * num6 + num2;
					float3 position;
					float3 x3;
					float3 x4;
					if (num7 > edgeGeometry.m_Start.middleLength)
					{
						Bounds1 t2 = new Bounds1(0f, 1f);
						MathUtils.ClampLength(MathUtils.Lerp(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, 0.5f).xz, ref t2, num7 - edgeGeometry.m_Start.middleLength);
						position = MathUtils.Position(curve2, t2.max);
						x3 = MathUtils.EndTangent(curve2);
						x4 = MathUtils.Tangent(curve2, t2.max);
					}
					else
					{
						Bounds1 t3 = new Bounds1(0f, 1f);
						MathUtils.ClampLength(MathUtils.Lerp(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, 0.5f).xz, ref t3, num7);
						position = MathUtils.Position(curve, t3.max);
						x3 = MathUtils.StartTangent(curve);
						x4 = MathUtils.Tangent(curve, t3.max);
					}
					x3.y = math.lerp(0f, x3.y, netCompositionObject.m_UseCurveRotation.x);
					x4.y = math.lerp(0f, x4.y, netCompositionObject.m_UseCurveRotation.x);
					x3 = math.normalizesafe(x3, new float3(0f, 0f, 1f));
					x4 = math.normalizesafe(x4, x3);
					quaternion q = quaternion.LookRotationSafe(x3, math.up());
					quaternion q2 = quaternion.LookRotationSafe(x4, math.up());
					quaternion rotation = math.slerp(q, q2, netCompositionObject.m_UseCurveRotation.y);
					Transform transform = new Transform(position, rotation);
					Transform transform2 = new Transform(netCompositionObject.m_Offset, netCompositionObject.m_Rotation);
					Transform transformData = ObjectUtils.LocalToWorld(transform, transform2);
					if (netCompositionObject.m_Probability < 100)
					{
						netCompositionObject.m_Probability = math.clamp(Mathf.RoundToInt((float)netCompositionObject.m_Probability * (num / netGeometryData.m_EdgeLengthRange.max)), 1, netCompositionObject.m_Probability);
					}
					TrafficSignNeeds trafficSignNeeds = new TrafficSignNeeds
					{
						m_Left = LaneDirectionType.None,
						m_Forward = LaneDirectionType.None,
						m_Right = LaneDirectionType.None
					};
					if ((flag2 || flag3) && GetClosestCarLane(owner, transformData.m_Position, 1f, out var result, out var hasTurningLanes, out var bicycleOnly, out var pedestrianOnly))
					{
						if (pedestrianOnly)
						{
							if (flag2)
							{
								trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.PedestrianOnly);
							}
						}
						else
						{
							Game.Net.CarLane carLane = m_CarLaneData[result];
							Lane lane = m_LaneData[result];
							if (m_SlaveLaneData.TryGetComponent(result, out var componentData4))
							{
								DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = m_SubLanes[owner];
								if (componentData4.m_MasterIndex < dynamicBuffer2.Length)
								{
									lane = m_LaneData[dynamicBuffer2[componentData4.m_MasterIndex].m_SubLane];
								}
								if (flag3)
								{
									if ((componentData4.m_Flags & SlaveLaneFlags.MergeLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Merge;
									}
									if ((componentData4.m_Flags & SlaveLaneFlags.MergeRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Merge;
									}
								}
							}
							if (!flag)
							{
								flag = true;
								Game.Net.Edge edge = m_NetEdgeData[owner];
								AddNodeLanes(edge.m_Start, owner, ref bool2.x, ref updateData);
								AddNodeLanes(edge.m_End, owner, ref bool2.y, ref updateData);
								AddEdgeLanes(owner, ref updateData);
							}
							if (flag3)
							{
								bool flag4 = false;
								if (!hasTurningLanes && updateData.m_TargetLanes.IsCreated && updateData.m_TargetLanes.TryGetValue(lane.m_EndNode, out var item))
								{
									flag4 = (item.m_CarLaneFlags & CarLaneFlags.Forbidden) != 0 && (item.m_CarLaneFlags & (CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
								}
								if (hasTurningLanes || flag4)
								{
									if ((carLane.m_Flags & CarLaneFlags.UTurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.UTurn;
									}
									if ((carLane.m_Flags & CarLaneFlags.UTurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.UTurn;
									}
									if ((carLane.m_Flags & CarLaneFlags.GentleTurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Gentle;
									}
									if ((carLane.m_Flags & CarLaneFlags.GentleTurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Gentle;
									}
									if ((carLane.m_Flags & CarLaneFlags.TurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Square;
									}
									if ((carLane.m_Flags & CarLaneFlags.TurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Square;
									}
									if ((carLane.m_Flags & CarLaneFlags.Forward) != 0)
									{
										trafficSignNeeds.m_Forward = LaneDirectionType.Straight;
									}
								}
							}
							if (flag2)
							{
								TargetLaneData item2 = default(TargetLaneData);
								EdgeLane componentData6;
								if (updateData.m_SourceLanes.IsCreated && updateData.m_SourceLanes.TryGetValue(lane.m_StartNode, out item2))
								{
									if ((carLane.m_Flags & ~item2.m_AndCarLaneFlags & CarLaneFlags.PublicOnly) != 0 && (item2.m_CarLaneFlags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.PublicOnly | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != CarLaneFlags.PublicOnly)
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.BusOnly);
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.TaxiOnly);
									}
									else if (math.any(math.abs(item2.m_SpeedLimit - carLane.m_DefaultSpeedLimit) > 1f))
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit);
										trafficSignNeeds.m_SpeedLimit = (ushort)Mathf.RoundToInt(carLane.m_DefaultSpeedLimit * 3.6f);
									}
									if (!bicycleOnly && math.any(@bool) && (item2.m_AndCarLaneFlags & CarLaneFlags.ForbidBicycles) == 0 && m_EdgeLaneData.TryGetComponent(result, out var componentData5) && ((componentData5.m_EdgeDelta.x < componentData5.m_EdgeDelta.y) ? @bool.x : @bool.y))
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoBicycles);
									}
								}
								else if (!bicycleOnly && math.any(@bool) && m_EdgeLaneData.TryGetComponent(result, out componentData6) && ((componentData6.m_EdgeDelta.x < componentData6.m_EdgeDelta.y) ? (@bool.x & !bool2.x) : (@bool.y & !bool2.y)))
								{
									trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoBicycles);
								}
								if ((carLane.m_Flags & CarLaneFlags.Roundabout) != 0)
								{
									if (m_LeftHandTraffic)
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.RoundaboutClockwise);
									}
									else
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.RoundaboutCounterclockwise);
									}
								}
								if (bicycleOnly)
								{
									trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.BicycleOnly);
								}
							}
						}
					}
					CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered, isNative, ageMask, ownerTemp, ownerElevation, transform, transformData, transform2, netCompositionObject.m_Flags, trafficSignNeeds, ref updateData, netCompositionObject.m_Prefab, 0, netCompositionObject.m_Probability);
				}
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer3 = m_SubLanes[owner];
			Transform transform3 = default(Transform);
			Transform transform4 = default(Transform);
			for (int l = 0; l < dynamicBuffer3.Length; l++)
			{
				Entity subLane = dynamicBuffer3[l].m_SubLane;
				if (m_SecondaryLaneData.HasComponent(subLane))
				{
					continue;
				}
				if (m_UtilityLaneData.HasComponent(subLane) && !m_EdgeLaneData.HasComponent(subLane))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[subLane];
					UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef2.m_Prefab];
					if (!(utilityLaneData.m_NodeObjectPrefab != Entity.Null))
					{
						continue;
					}
					Curve curve3 = m_CurveData[subLane];
					Lane lane2 = m_LaneData[subLane];
					bool flag5 = true;
					if (isTemp && m_TempData.TryGetComponent(subLane, out var componentData7))
					{
						flag5 = componentData7.m_Original == Entity.Null;
					}
					float num8 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData.m_NodeObjectPrefab].m_Bounds));
					updateData.EnsureUtilityNodes(Allocator.Temp);
					bool flag6 = false;
					for (int m = 0; m < updateData.m_UtilityNodes.Length; m++)
					{
						UtilityNodeData value = updateData.m_UtilityNodes[m];
						if ((value.m_UtilityTypes & utilityLaneData.m_UtilityTypes) != UtilityTypes.None && value.m_PathNode.Equals(lane2.m_StartNode))
						{
							if (num8 > value.m_LanePriority)
							{
								value.m_Prefab = utilityLaneData.m_NodeObjectPrefab;
								value.m_LanePriority = num8;
								value.m_Count = 1;
							}
							else if (num8 == value.m_LanePriority)
							{
								value.m_Count++;
							}
							value.m_IsNew &= flag5;
							updateData.m_UtilityNodes[m] = value;
							flag6 = true;
							break;
						}
					}
					if (!flag6)
					{
						float elevation = 0f;
						if (m_NetElevationData.TryGetComponent(subLane, out var componentData8) && componentData8.m_Elevation.x != float.MinValue)
						{
							elevation = componentData8.m_Elevation.x;
						}
						UtilityNodeData value2 = new UtilityNodeData
						{
							m_Transform = new Transform(curve3.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve3.m_Bezier))),
							m_Prefab = utilityLaneData.m_NodeObjectPrefab,
							m_PathNode = lane2.m_StartNode,
							m_Count = 1,
							m_Elevation = elevation,
							m_LanePriority = num8,
							m_UtilityTypes = utilityLaneData.m_UtilityTypes,
							m_Vertical = true,
							m_IsNew = flag5
						};
						updateData.m_UtilityNodes.Add(in value2);
					}
				}
				else
				{
					if (!m_TrackLaneData.HasComponent(subLane))
					{
						continue;
					}
					Game.Net.TrackLane trackLane = m_TrackLaneData[subLane];
					if ((trackLane.m_Flags & (TrackLaneFlags.StartingLane | TrackLaneFlags.EndingLane)) == 0)
					{
						continue;
					}
					PrefabRef prefabRef3 = m_PrefabRefData[subLane];
					TrackLaneData trackLaneData = m_PrefabTrackLaneData[prefabRef3.m_Prefab];
					if (!(trackLaneData.m_EndObjectPrefab != Entity.Null))
					{
						continue;
					}
					Curve curve4 = m_CurveData[subLane];
					if ((trackLane.m_Flags & TrackLaneFlags.StartingLane) != 0)
					{
						transform3.m_Position = curve4.m_Bezier.a;
						float3 value3 = MathUtils.StartTangent(curve4.m_Bezier);
						if (MathUtils.TryNormalize(ref value3))
						{
							transform3.m_Rotation = quaternion.LookRotation(value3, math.up());
						}
						else
						{
							transform3.m_Rotation = quaternion.identity;
						}
						transform3.m_Position.y += netCompositionData.m_SurfaceHeight.max;
						CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, transform3, transform3, default(Transform), (SubObjectFlags)0, default(TrafficSignNeeds), ref updateData, trackLaneData.m_EndObjectPrefab, 0, 100);
					}
					if ((trackLane.m_Flags & TrackLaneFlags.EndingLane) != 0)
					{
						transform4.m_Position = curve4.m_Bezier.d;
						float3 value4 = -MathUtils.EndTangent(curve4.m_Bezier);
						if (MathUtils.TryNormalize(ref value4))
						{
							transform4.m_Rotation = quaternion.LookRotation(value4, math.up());
						}
						else
						{
							transform4.m_Rotation = quaternion.identity;
						}
						transform4.m_Position.y += netCompositionData.m_SurfaceHeight.max;
						CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, transform4, transform4, default(Transform), (SubObjectFlags)0, default(TrafficSignNeeds), ref updateData, trackLaneData.m_EndObjectPrefab, 0, 100);
					}
				}
			}
			if (!updateData.m_UtilityNodes.IsCreated || updateData.m_UtilityNodes.Length == 0)
			{
				return;
			}
			for (int n = 0; n < dynamicBuffer3.Length; n++)
			{
				Entity subLane2 = dynamicBuffer3[n].m_SubLane;
				if (m_SecondaryLaneData.HasComponent(subLane2) || !m_UtilityLaneData.HasComponent(subLane2) || !m_EdgeLaneData.HasComponent(subLane2))
				{
					continue;
				}
				PrefabRef prefabRef4 = m_PrefabRefData[subLane2];
				UtilityLaneData utilityLaneData2 = m_PrefabUtilityLaneData[prefabRef4.m_Prefab];
				if (!(utilityLaneData2.m_NodeObjectPrefab != Entity.Null))
				{
					continue;
				}
				Lane lane3 = m_LaneData[subLane2];
				float num9 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData2.m_NodeObjectPrefab].m_Bounds));
				for (int num10 = 0; num10 < updateData.m_UtilityNodes.Length; num10++)
				{
					UtilityNodeData value5 = updateData.m_UtilityNodes[num10];
					if ((value5.m_UtilityTypes & utilityLaneData2.m_UtilityTypes) != UtilityTypes.None && value5.m_PathNode.EqualsIgnoreCurvePos(lane3.m_MiddleNode))
					{
						if (num9 > value5.m_LanePriority)
						{
							value5.m_LanePriority = num9;
							value5.m_Count = 2;
							value5.m_Vertical = false;
						}
						else if (num9 == value5.m_LanePriority)
						{
							value5.m_Count += 2;
						}
						value5.m_Prefab = utilityLaneData2.m_NodeObjectPrefab;
						updateData.m_UtilityNodes[num10] = value5;
					}
				}
			}
			for (int num11 = 0; num11 < updateData.m_UtilityNodes.Length; num11++)
			{
				UtilityNodeData utilityNodeData = updateData.m_UtilityNodes[num11];
				if (utilityNodeData.m_Count != 2 || utilityNodeData.m_Vertical)
				{
					Transform localTransformData = default(Transform);
					localTransformData.m_Position.y += utilityNodeData.m_Elevation;
					CreateSecondaryObject(jobIndex, ref random, owner, isTemp, utilityNodeData.m_IsNew, isLowered: false, isNative, (Game.Tools.AgeMask)0, ownerTemp, ownerElevation, utilityNodeData.m_Transform, utilityNodeData.m_Transform, localTransformData, SubObjectFlags.AnchorCenter, default(TrafficSignNeeds), ref updateData, utilityNodeData.m_Prefab, 0, 100);
				}
			}
		}

		private void CreateSecondaryAreaObjects(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, ref UpdateSecondaryObjectsData updateData, bool isTemp, bool isNative, Temp ownerTemp)
		{
			PrefabRef prefabRef = m_PrefabRefData[owner];
			DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[owner];
			if (!m_PrefabSubObjects.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Prefabs.SubObject subObject = bufferData[i];
				if ((subObject.m_Flags & SubObjectFlags.EdgePlacement) == 0)
				{
					continue;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Game.Areas.Node node = dynamicBuffer[j];
					float2 @float = math.normalizesafe(dynamicBuffer[math.select(j + 1, 0, j + 1 >= dynamicBuffer.Length)].m_Position.xz - node.m_Position.xz);
					quaternion rotation = quaternion.LookRotationSafe(new float3(@float.x, 0f, @float.y), math.up());
					Transform transform = new Transform(node.m_Position, rotation);
					Transform transform2 = new Transform(subObject.m_Position, subObject.m_Rotation);
					Transform transformData = ObjectUtils.LocalToWorld(transform, transform2);
					if (node.m_Elevation == float.MinValue)
					{
						subObject.m_Flags |= SubObjectFlags.OnGround;
						node.m_Elevation = 0f;
					}
					else
					{
						subObject.m_Flags &= ~SubObjectFlags.OnGround;
					}
					CreateSecondaryObject(jobIndex, ref random, owner, isTemp, isNew: false, isLowered: false, isNative, (Game.Tools.AgeMask)0, ownerTemp, node.m_Elevation, transform, transformData, transform2, subObject.m_Flags, default(TrafficSignNeeds), ref updateData, subObject.m_Prefab, 0, subObject.m_Probability);
				}
			}
		}

		private bool CheckRequirements(Entity owner, Entity prefab, bool isExplicit, ref UpdateSecondaryObjectsData updateData)
		{
			if (m_ObjectRequirements.TryGetBuffer(prefab, out var bufferData))
			{
				EnsurePlaceholderRequirements(owner, ref updateData);
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
						flag |= updateData.m_PlaceholderRequirements.Contains(objectRequirementElement.m_Requirement) || (isExplicit && (objectRequirementElement.m_Type & ObjectRequirementType.IgnoreExplicit) != 0);
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		private void CreateSecondaryObject(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, bool isTemp, bool isNew, bool isLowered, bool isNative, Game.Tools.AgeMask ageMask, Temp ownerTemp, float ownerElevation, Transform ownerTransform, Transform transformData, Transform localTransformData, SubObjectFlags flags, TrafficSignNeeds trafficSignNeeds, ref UpdateSecondaryObjectsData updateData, Entity prefab, int groupIndex, int probability)
		{
			if (!m_PrefabPlaceholderObjectData.TryGetComponent(prefab, out var componentData) || !m_PlaceholderObjects.TryGetBuffer(prefab, out var bufferData))
			{
				Entity groupPrefab = prefab;
				if (m_PrefabSpawnableObjectData.TryGetComponent(prefab, out var componentData2) && componentData2.m_RandomizationGroup != Entity.Null)
				{
					groupPrefab = componentData2.m_RandomizationGroup;
				}
				if (CheckRequirements(owner, prefab, isExplicit: true, ref updateData))
				{
					Unity.Mathematics.Random random2 = random;
					random.NextInt();
					random.NextInt();
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
						CreateSecondaryObject(jobIndex, ref random2, owner, isTemp, isNew, isLowered, isNative, ageMask, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, trafficSignNeeds, ref updateData, prefab, cacheTransform: false, 0, groupIndex, probability);
					}
				}
				return;
			}
			if (componentData.m_RandomizeGroupIndex)
			{
				groupIndex = random.NextInt();
			}
			float num = -1f;
			Entity prefab2 = Entity.Null;
			Entity groupPrefab2 = Entity.Null;
			Unity.Mathematics.Random random3 = default(Unity.Mathematics.Random);
			bool flag = false;
			int num2 = 0;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity entity = bufferData[i].m_Object;
				if (!CheckRequirements(owner, entity, isExplicit: false, ref updateData))
				{
					continue;
				}
				float num3 = 0f;
				bool flag2 = false;
				if (m_PrefabTrafficSignData.HasComponent(entity))
				{
					Game.Prefabs.TrafficSignData trafficSignData = m_PrefabTrafficSignData[entity];
					uint num4 = trafficSignData.m_TypeMask & trafficSignNeeds.m_SignTypeMask;
					int num5 = trafficSignNeeds.m_SpeedLimit;
					if (num4 == 0)
					{
						num4 = trafficSignData.m_TypeMask & trafficSignNeeds.m_SignTypeMask2;
						num5 = trafficSignNeeds.m_SpeedLimit2;
						if (num4 == 0)
						{
							continue;
						}
						flag2 = true;
					}
					float num6 = 10f + math.log2(num4);
					if ((num4 & Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit)) != 0)
					{
						num6 /= 1f + (float)math.abs(trafficSignData.m_SpeedLimit - num5);
					}
					num3 += num6;
				}
				if (m_PrefabTrafficLightData.HasComponent(entity))
				{
					TrafficLightData trafficLightData = m_PrefabTrafficLightData[entity];
					int num7 = 0;
					if (trafficSignNeeds.m_VehicleLanesLeft > 0f)
					{
						if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) == 0)
						{
							continue;
						}
						num7 += 10;
					}
					else if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) != 0)
					{
						if (trafficSignNeeds.m_VehicleMask == 0)
						{
							continue;
						}
						num7--;
					}
					if (trafficSignNeeds.m_VehicleLanesRight > 0f)
					{
						if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) == 0)
						{
							continue;
						}
						num7 += 10;
					}
					else if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) != 0)
					{
						if (trafficSignNeeds.m_VehicleMask == 0)
						{
							continue;
						}
						num7--;
					}
					if (trafficSignNeeds.m_CrossingLeftMask != 0 && trafficSignNeeds.m_CrossingRightMask != 0)
					{
						num7 = (((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) != (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) ? (num7 - 1) : (num7 + 10));
					}
					else if (trafficSignNeeds.m_CrossingLeftMask != 0)
					{
						if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) == TrafficLightType.CrossingLeft)
						{
							num7 += 10;
						}
						else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped)) == (TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped))
						{
							flag2 = true;
							num7 += 9;
						}
						else
						{
							if ((trafficLightData.m_Type & TrafficLightType.CrossingRight) != 0)
							{
								continue;
							}
							num7--;
						}
					}
					else if (trafficSignNeeds.m_CrossingRightMask != 0)
					{
						if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) == TrafficLightType.CrossingRight)
						{
							num7 += 10;
						}
						else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped)) == (TrafficLightType.CrossingLeft | TrafficLightType.AllowFlipped))
						{
							flag2 = true;
							num7 += 9;
						}
						else
						{
							if ((trafficLightData.m_Type & TrafficLightType.CrossingLeft) != 0)
							{
								continue;
							}
							num7--;
						}
					}
					else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) != 0)
					{
						continue;
					}
					if (num7 <= 0)
					{
						continue;
					}
					num3 += (float)(50 * num7);
					if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) != 0)
					{
						ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[entity];
						Bounds1 bounds = trafficLightData.m_ReachOffset - objectGeometryData.m_Bounds.min.x;
						if (bounds.min > math.max(trafficSignNeeds.m_VehicleLanesLeft, 1f))
						{
							continue;
						}
						bounds = trafficSignNeeds.m_VehicleLanesLeft - bounds;
						num3 -= 50f * math.max(0f, math.max(0f - bounds.min, bounds.max));
					}
					if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) != 0)
					{
						ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[entity];
						Bounds1 bounds2 = trafficLightData.m_ReachOffset + objectGeometryData2.m_Bounds.max.x;
						if (bounds2.min > math.max(trafficSignNeeds.m_VehicleLanesRight, 1f))
						{
							continue;
						}
						bounds2 = trafficSignNeeds.m_VehicleLanesRight - bounds2;
						num3 -= 50f * math.max(0f, math.max(0f - bounds2.min, bounds2.max));
					}
				}
				if (m_PrefabLaneDirectionData.HasComponent(entity))
				{
					LaneDirectionData laneDirectionData = m_PrefabLaneDirectionData[entity];
					if (trafficSignNeeds.m_Left == LaneDirectionType.None && trafficSignNeeds.m_Forward == LaneDirectionType.None && trafficSignNeeds.m_Right == LaneDirectionType.None)
					{
						continue;
					}
					int num8 = 0;
					num8 += 180 - math.abs(trafficSignNeeds.m_Left - laneDirectionData.m_Left);
					num8 += 180 - math.abs(trafficSignNeeds.m_Forward - laneDirectionData.m_Forward);
					num8 += 180 - math.abs(trafficSignNeeds.m_Right - laneDirectionData.m_Right);
					num3 += (float)num8 * 0.1f;
				}
				SpawnableObjectData spawnableObjectData = m_PrefabSpawnableObjectData[entity];
				Entity entity2 = ((spawnableObjectData.m_RandomizationGroup != Entity.Null) ? spawnableObjectData.m_RandomizationGroup : entity);
				Unity.Mathematics.Random random4 = random;
				random.NextInt();
				random.NextInt();
				if (updateData.m_SelectedSpawnabled.IsCreated && updateData.m_SelectedSpawnabled.TryGetValue(new PlaceholderKey(entity2, groupIndex), out var item2))
				{
					num3 += 0.5f;
					random4 = item2;
				}
				if (num3 > num)
				{
					num = num3;
					prefab2 = entity;
					groupPrefab2 = entity2;
					random3 = random4;
					flag = flag2;
					num2 = m_PrefabSpawnableObjectData[entity].m_Probability;
				}
				else if (num3 == num)
				{
					int probability2 = m_PrefabSpawnableObjectData[entity].m_Probability;
					num2 += probability2;
					if (random.NextInt(num2) < probability2)
					{
						prefab2 = entity;
						groupPrefab2 = entity2;
						random3 = random4;
						flag = flag2;
					}
				}
			}
			if (num2 <= 0)
			{
				return;
			}
			updateData.EnsureSelectedSpawnables(Allocator.Temp);
			updateData.m_SelectedSpawnabled.TryAdd(new PlaceholderKey(groupPrefab2, groupIndex), random3);
			if (random3.NextInt(100) < probability)
			{
				if (flag)
				{
					transformData.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), transformData.m_Rotation);
				}
				CreateSecondaryObject(jobIndex, ref random3, owner, isTemp, isNew, isLowered, isNative, ageMask, ownerTemp, ownerElevation, Entity.Null, ownerTransform, transformData, localTransformData, flags, trafficSignNeeds, ref updateData, prefab2, cacheTransform: false, 0, groupIndex, probability);
			}
		}

		private bool GetClosestCarLane(Entity owner, float3 position, float maxDistance, out Entity result, out bool hasTurningLanes, out bool bicycleOnly, out bool pedestrianOnly)
		{
			result = Entity.Null;
			hasTurningLanes = false;
			bicycleOnly = false;
			pedestrianOnly = false;
			if (!m_SubLanes.TryGetBuffer(owner, out var bufferData))
			{
				return false;
			}
			bool2 @bool = false;
			bool flag = false;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				bool flag2 = false;
				Game.Net.PedestrianLane componentData2;
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) != 0)
				{
					if (!m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData) || m_MasterLaneData.HasComponent(subLane.m_SubLane) || m_SecondaryLaneData.HasComponent(subLane.m_SubLane) || (componentData.m_Flags & CarLaneFlags.Unsafe) != 0)
					{
						continue;
					}
					if ((componentData.m_Flags & CarLaneFlags.Invert) != 0)
					{
						@bool.y |= (componentData.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
					}
					else
					{
						@bool.x |= (componentData.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
					}
					flag2 = (componentData.m_Flags & CarLaneFlags.Invert) != 0;
				}
				else if ((subLane.m_PathMethods & PathMethod.Pedestrian) == 0 || !m_PedestrianLaneData.TryGetComponent(subLane.m_SubLane, out componentData2) || (componentData2.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					continue;
				}
				float t;
				float num = MathUtils.Distance(m_CurveData[subLane.m_SubLane].m_Bezier, position, out t);
				if (num < maxDistance)
				{
					maxDistance = num;
					result = subLane.m_SubLane;
					flag = flag2;
					bicycleOnly = subLane.m_PathMethods == PathMethod.Bicycle;
					pedestrianOnly = subLane.m_PathMethods == PathMethod.Pedestrian;
				}
			}
			hasTurningLanes = (flag ? @bool.y : @bool.x);
			return result != Entity.Null;
		}

		private void CreateSecondaryObject(int jobIndex, ref Unity.Mathematics.Random random, Entity owner, bool isTemp, bool isNew, bool isLowered, bool isNative, Game.Tools.AgeMask ageMask, Temp ownerTemp, float ownerElevation, Entity oldSecondaryObject, Transform ownerTransform, Transform transformData, Transform localTransformData, SubObjectFlags flags, TrafficSignNeeds trafficSignNeeds, ref UpdateSecondaryObjectsData updateData, Entity prefab, bool cacheTransform, int parentMesh, int groupIndex, int probability)
		{
			bool flag = m_PrefabObjectGeometryData.HasComponent(prefab);
			PlaceableObjectData componentData;
			bool num = m_PrefabPlaceableObjectData.TryGetComponent(prefab, out componentData);
			bool flag2 = m_PrefabData.IsComponentEnabled(prefab);
			if ((flags & SubObjectFlags.AnchorTop) != 0)
			{
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefab];
				objectGeometryData.m_Bounds.max.y -= componentData.m_PlacementOffset.y;
				transformData.m_Position.y -= objectGeometryData.m_Bounds.max.y;
				localTransformData.m_Position.y -= objectGeometryData.m_Bounds.max.y;
			}
			else if ((flags & SubObjectFlags.AnchorCenter) != 0)
			{
				ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefab];
				float num2 = (objectGeometryData2.m_Bounds.max.y - objectGeometryData2.m_Bounds.min.y) * 0.5f;
				transformData.m_Position.y -= num2;
				localTransformData.m_Position.y -= num2;
			}
			Elevation component = new Elevation(ownerElevation, (math.abs(parentMesh) >= 1000) ? ElevationFlags.Stacked : ((ElevationFlags)0));
			if ((flags & SubObjectFlags.OnGround) == 0)
			{
				component.m_Elevation += localTransformData.m_Position.y;
				if (ownerElevation >= 0f && component.m_Elevation >= -0.5f && component.m_Elevation < 0f)
				{
					component.m_Elevation = 0f;
				}
				if (parentMesh < 0)
				{
					component.m_Flags |= ElevationFlags.OnGround;
				}
				else if (component.m_Elevation < 0f && isLowered)
				{
					component.m_Flags |= ElevationFlags.Lowered;
				}
			}
			else
			{
				if ((flags & (SubObjectFlags.AnchorTop | SubObjectFlags.AnchorCenter)) == 0)
				{
					transformData.m_Position.y = ownerTransform.m_Position.y - ownerElevation;
					localTransformData.m_Position.y = 0f - ownerElevation;
				}
				component.m_Elevation = 0f;
				component.m_Flags |= ElevationFlags.OnGround;
			}
			if (oldSecondaryObject == Entity.Null)
			{
				oldSecondaryObject = FindOldSecondaryObject(prefab, transformData, ref updateData);
			}
			uint num3 = random.NextUInt(268435456u);
			PseudoRandomSeed component2 = new PseudoRandomSeed((ushort)(num3 >> 12));
			if (num && componentData.m_RotationSymmetry != RotationSymmetry.None)
			{
				uint num4 = num3 & 0xFFF;
				if (componentData.m_RotationSymmetry != RotationSymmetry.Any)
				{
					num4 = (uint)(((int)num4 * (int)componentData.m_RotationSymmetry) & -4096) / (uint)componentData.m_RotationSymmetry;
				}
				float angle = (float)num4 * 0.0015339808f;
				transformData.m_Rotation = math.mul(quaternion.RotateY(angle), transformData.m_Rotation);
			}
			m_UpdateQueue.Enqueue(new UpdateData
			{
				m_Owner = owner,
				m_Prefab = prefab,
				m_Transform = transformData
			});
			if (oldSecondaryObject != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldSecondaryObject);
				Temp component3 = default(Temp);
				if (isTemp)
				{
					if (m_TempData.HasComponent(oldSecondaryObject))
					{
						component3 = m_TempData[oldSecondaryObject];
						component3.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
						if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
						{
							component3.m_Flags |= TempFlags.Modify;
						}
						if (isNew)
						{
							component3.m_Original = Entity.Null;
						}
						else
						{
							component3.m_Original = FindOriginalSecondaryObject(prefab, component3.m_Original, transformData, ref updateData);
						}
						m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, component3);
						if (component3.m_Original != Entity.Null && flag2 && m_PrefabTreeData.HasComponent(prefab) && m_TreeData.TryGetComponent(component3.m_Original, out var componentData2))
						{
							m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, componentData2);
						}
					}
					m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, transformData);
					if (!m_UpdatedData.HasComponent(oldSecondaryObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, default(Updated));
					}
				}
				else if (!transformData.Equals(m_TransformData[oldSecondaryObject]))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, transformData);
					if (!m_UpdatedData.HasComponent(oldSecondaryObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, default(Updated));
					}
				}
				if (m_PrefabStreetLightData.HasComponent(prefab))
				{
					StreetLight streetLight = default(StreetLight);
					bool flag3 = false;
					if (m_StreetLightData.TryGetComponent(oldSecondaryObject, out var componentData3))
					{
						streetLight = componentData3;
						flag3 = true;
					}
					if (m_RoadData.TryGetComponent(owner, out var componentData4))
					{
						StreetLightSystem.UpdateStreetLightState(ref streetLight, componentData4);
					}
					if (flag3)
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, streetLight);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, streetLight);
					}
				}
				if (m_PrefabTrafficLightData.HasComponent(prefab))
				{
					TrafficLight trafficLight = default(TrafficLight);
					bool flag4 = false;
					if (m_TrafficLightData.TryGetComponent(oldSecondaryObject, out var componentData5))
					{
						trafficLight = componentData5;
						flag4 = true;
					}
					trafficLight.m_GroupMask0 = trafficSignNeeds.m_VehicleMask;
					trafficLight.m_GroupMask1 = (ushort)(trafficSignNeeds.m_CrossingLeftMask | trafficSignNeeds.m_CrossingRightMask);
					if (m_TrafficLightsData.TryGetComponent(owner, out var componentData6))
					{
						TrafficLightSystem.UpdateTrafficLightState(componentData6, ref trafficLight);
					}
					if (flag4)
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, trafficLight);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, trafficLight);
					}
				}
				if (component3.m_Original == Entity.Null && m_PrefabTreeData.HasComponent(prefab))
				{
					Tree component4 = ObjectUtils.InitializeTreeState(ToolUtils.GetRandomAge(ref random, ageMask));
					m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, component4);
				}
				if (cacheTransform)
				{
					LocalTransformCache component5 = default(LocalTransformCache);
					component5.m_Position = localTransformData.m_Position;
					component5.m_Rotation = localTransformData.m_Rotation;
					component5.m_ParentMesh = parentMesh;
					component5.m_GroupIndex = groupIndex;
					component5.m_Probability = probability;
					component5.m_PrefabSubIndex = -1;
					if (m_LocalTransformCacheData.HasComponent(oldSecondaryObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, component5);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, component5);
					}
				}
				else if (m_LocalTransformCacheData.HasComponent(oldSecondaryObject))
				{
					m_CommandBuffer.RemoveComponent<LocalTransformCache>(jobIndex, oldSecondaryObject);
				}
				if (flag)
				{
					if (m_PseudoRandomSeedData.HasComponent(component3.m_Original))
					{
						component2 = m_PseudoRandomSeedData[component3.m_Original];
					}
					m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, component2);
				}
				if ((flags & SubObjectFlags.OnGround) == 0)
				{
					if (m_ElevationData.HasComponent(oldSecondaryObject))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldSecondaryObject, component);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldSecondaryObject, component);
					}
				}
				else if (m_ElevationData.HasComponent(oldSecondaryObject))
				{
					m_CommandBuffer.RemoveComponent<Elevation>(jobIndex, oldSecondaryObject);
				}
				return;
			}
			ObjectData objectData = m_PrefabObjectData[prefab];
			if (!objectData.m_Archetype.Valid)
			{
				return;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
			m_CommandBuffer.AddComponent(jobIndex, e, in m_SecondaryOwnerTypes);
			m_CommandBuffer.SetComponent(jobIndex, e, new Owner(owner));
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(prefab));
			m_CommandBuffer.SetComponent(jobIndex, e, transformData);
			Temp component6 = default(Temp);
			if (isTemp)
			{
				component6.m_Flags = ownerTemp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
				if ((ownerTemp.m_Flags & TempFlags.Replace) != 0)
				{
					component6.m_Flags |= TempFlags.Modify;
				}
				if (!isNew)
				{
					component6.m_Original = FindOriginalSecondaryObject(prefab, Entity.Null, transformData, ref updateData);
				}
				if (m_PrefabObjectGeometryData.HasComponent(prefab))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, in m_TempAnimationTypes);
					m_CommandBuffer.SetComponent(jobIndex, e, component6);
				}
				else
				{
					m_CommandBuffer.AddComponent(jobIndex, e, component6);
				}
				if (component6.m_Original != Entity.Null && flag2 && m_PrefabTreeData.HasComponent(prefab) && m_TreeData.TryGetComponent(component6.m_Original, out var componentData7))
				{
					m_CommandBuffer.SetComponent(jobIndex, e, componentData7);
				}
			}
			if (m_PrefabStreetLightData.HasComponent(prefab))
			{
				StreetLight streetLight2 = default(StreetLight);
				if (m_StreetLightData.TryGetComponent(component6.m_Original, out var componentData8))
				{
					streetLight2 = componentData8;
				}
				if (m_RoadData.TryGetComponent(owner, out var componentData9))
				{
					StreetLightSystem.UpdateStreetLightState(ref streetLight2, componentData9);
				}
				m_CommandBuffer.SetComponent(jobIndex, e, streetLight2);
			}
			if (m_PrefabTrafficLightData.HasComponent(prefab))
			{
				TrafficLight trafficLight2 = default(TrafficLight);
				if (m_TrafficLightData.TryGetComponent(component6.m_Original, out var componentData10))
				{
					trafficLight2 = componentData10;
				}
				trafficLight2.m_GroupMask0 = trafficSignNeeds.m_VehicleMask;
				trafficLight2.m_GroupMask1 = (ushort)(trafficSignNeeds.m_CrossingLeftMask | trafficSignNeeds.m_CrossingRightMask);
				if (m_TrafficLightsData.TryGetComponent(owner, out var componentData11))
				{
					TrafficLightSystem.UpdateTrafficLightState(componentData11, ref trafficLight2);
				}
				m_CommandBuffer.SetComponent(jobIndex, e, trafficLight2);
			}
			if (component6.m_Original == Entity.Null && m_PrefabTreeData.HasComponent(prefab))
			{
				Tree component7 = ObjectUtils.InitializeTreeState(ToolUtils.GetRandomAge(ref random, ageMask));
				m_CommandBuffer.SetComponent(jobIndex, e, component7);
			}
			if (isNative)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, default(Native));
			}
			if (cacheTransform)
			{
				LocalTransformCache component8 = default(LocalTransformCache);
				component8.m_Position = localTransformData.m_Position;
				component8.m_Rotation = localTransformData.m_Rotation;
				component8.m_ParentMesh = parentMesh;
				component8.m_GroupIndex = groupIndex;
				component8.m_Probability = probability;
				component8.m_PrefabSubIndex = -1;
				m_CommandBuffer.AddComponent(jobIndex, e, component8);
			}
			if (flag)
			{
				if (m_PseudoRandomSeedData.HasComponent(component6.m_Original))
				{
					component2 = m_PseudoRandomSeedData[component6.m_Original];
				}
				m_CommandBuffer.SetComponent(jobIndex, e, component2);
			}
			if ((flags & SubObjectFlags.OnGround) == 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, component);
			}
		}

		private void EnsurePlaceholderRequirements(Entity owner, ref UpdateSecondaryObjectsData updateData)
		{
			if (!updateData.m_RequirementsSearched)
			{
				updateData.EnsurePlaceholderRequirements(Allocator.Temp);
				if (0 == 0 && m_DefaultTheme != Entity.Null)
				{
					updateData.m_PlaceholderRequirements.Add(m_DefaultTheme);
				}
				updateData.m_RequirementsSearched = true;
			}
		}

		private Entity FindOldSecondaryObject(Entity prefab, Transform transform, ref UpdateSecondaryObjectsData updateData)
		{
			Entity result = Entity.Null;
			if (updateData.m_OldEntities.IsCreated && updateData.m_OldEntities.TryGetFirstValue(prefab, out var item, out var it))
			{
				result = item;
				float num = math.distance(m_TransformData[item].m_Position, transform.m_Position);
				NativeParallelMultiHashMapIterator<Entity> it2 = it;
				while (updateData.m_OldEntities.TryGetNextValue(out item, ref it))
				{
					float num2 = math.distance(m_TransformData[item].m_Position, transform.m_Position);
					if (num2 < num)
					{
						result = item;
						num = num2;
						it2 = it;
					}
				}
				updateData.m_OldEntities.Remove(it2);
			}
			return result;
		}

		private Entity FindOriginalSecondaryObject(Entity prefab, Entity original, Transform transform, ref UpdateSecondaryObjectsData updateData)
		{
			Entity result = Entity.Null;
			if (updateData.m_OriginalEntities.IsCreated && updateData.m_OriginalEntities.TryGetFirstValue(prefab, out var item, out var it))
			{
				if (item == original)
				{
					updateData.m_OriginalEntities.Remove(it);
					return original;
				}
				result = item;
				float num = math.distance(m_TransformData[item].m_Position, transform.m_Position);
				NativeParallelMultiHashMapIterator<Entity> it2 = it;
				while (updateData.m_OriginalEntities.TryGetNextValue(out item, ref it))
				{
					if (item == original)
					{
						updateData.m_OriginalEntities.Remove(it);
						return original;
					}
					float num2 = math.distance(m_TransformData[item].m_Position, transform.m_Position);
					if (num2 < num)
					{
						result = item;
						num = num2;
						it2 = it;
					}
				}
				updateData.m_OriginalEntities.Remove(it2);
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Curve> __Game_Net_Curve_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> __Game_Prefabs_SubLane_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ChangedDistrict> __Game_Areas_ChangedDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Secondary> __Game_Objects_Secondary_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ChangedDistrict> __Game_Areas_ChangedDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLightData> __Game_Prefabs_TrafficLightData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.TrafficSignData> __Game_Prefabs_TrafficSignData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.StreetLightData> __Game_Prefabs_StreetLightData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneDirectionData> __Game_Prefabs_LaneDirectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ThemeData> __Game_Prefabs_ThemeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderObjectData> __Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.UtilityObjectData> __Game_Prefabs_UtilityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLight> __Game_Objects_TrafficLight_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StreetLight> __Game_Objects_StreetLight_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficLights> __Game_Net_TrafficLights_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		public ComponentLookup<Road> __Game_Net_Road_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionObject> __Game_Prefabs_NetCompositionObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<DefaultNetLane> __Game_Prefabs_DefaultNetLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Curve_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>();
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Prefabs_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubLane>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Areas_ChangedDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ChangedDistrict>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Edge>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Secondary_RO_ComponentLookup = state.GetComponentLookup<Secondary>(isReadOnly: true);
			__Game_Areas_ChangedDistrict_RO_ComponentLookup = state.GetComponentLookup<ChangedDistrict>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BorderDistrict>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_TrafficLightData_RO_ComponentLookup = state.GetComponentLookup<TrafficLightData>(isReadOnly: true);
			__Game_Prefabs_TrafficSignData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.TrafficSignData>(isReadOnly: true);
			__Game_Prefabs_StreetLightData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.StreetLightData>(isReadOnly: true);
			__Game_Prefabs_LaneDirectionData_RO_ComponentLookup = state.GetComponentLookup<LaneDirectionData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_ThemeData_RO_ComponentLookup = state.GetComponentLookup<ThemeData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderObjectData>(isReadOnly: true);
			__Game_Prefabs_UtilityObjectData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.UtilityObjectData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Objects_TrafficLight_RO_ComponentLookup = state.GetComponentLookup<TrafficLight>(isReadOnly: true);
			__Game_Objects_StreetLight_RO_ComponentLookup = state.GetComponentLookup<StreetLight>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.SecondaryLane>(isReadOnly: true);
			__Game_Net_UtilityLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.UtilityLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_TrafficLights_RO_ComponentLookup = state.GetComponentLookup<TrafficLights>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Net_Road_RW_ComponentLookup = state.GetComponentLookup<Road>();
			__Game_Prefabs_NetCompositionObject_RO_BufferLookup = state.GetBufferLookup<NetCompositionObject>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
			__Game_Prefabs_DefaultNetLane_RO_BufferLookup = state.GetBufferLookup<DefaultNetLane>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ModificationBarrier4B m_ModificationBarrier;

	private EntityQuery m_ObjectQuery;

	private EntityQuery m_BorderDistrictQuery;

	private EntityQuery m_PolicyQuery;

	private EntityQuery m_LaneQuery;

	private ComponentTypeSet m_AppliedTypes;

	private ComponentTypeSet m_SecondaryOwnerTypes;

	private ComponentTypeSet m_TempAnimationTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<SubObject>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<ChangedDistrict>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Building>() }
		});
		m_BorderDistrictQuery = GetEntityQuery(ComponentType.ReadOnly<BorderDistrict>(), ComponentType.ReadOnly<SubObject>(), ComponentType.Exclude<Updated>(), ComponentType.Exclude<ChangedDistrict>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PolicyQuery = GetEntityQuery(ComponentType.ReadOnly<Modify>());
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.UtilityLane>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Owner>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		m_SecondaryOwnerTypes = new ComponentTypeSet(ComponentType.ReadWrite<Secondary>(), ComponentType.ReadWrite<Owner>());
		m_TempAnimationTypes = new ComponentTypeSet(ComponentType.ReadWrite<Temp>(), ComponentType.ReadWrite<Animation>(), ComponentType.ReadWrite<InterpolatedTransform>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_ObjectQuery.IsEmptyIgnoreFilter;
		bool flag2 = !m_LaneQuery.IsEmptyIgnoreFilter;
		NativeHashSet<Entity> districts;
		bool updatedDistricts = GetUpdatedDistricts(out districts);
		if (!flag && !flag2 && !updatedDistricts)
		{
			return;
		}
		NativeQueue<UpdateData> updateQueue = new NativeQueue<UpdateData>(Allocator.TempJob);
		if (flag || updatedDistricts)
		{
			UpdateObjects(updateQueue, flag, updatedDistricts, districts);
			if (updatedDistricts)
			{
				districts.Dispose(base.Dependency);
			}
		}
		if (flag2)
		{
			UpdateLanes(updateQueue);
		}
		updateQueue.Dispose(base.Dependency);
	}

	private bool GetUpdatedDistricts(out NativeHashSet<Entity> districts)
	{
		districts = default(NativeHashSet<Entity>);
		if (m_PolicyQuery.IsEmptyIgnoreFilter)
		{
			return false;
		}
		NativeArray<Modify> nativeArray = m_PolicyQuery.ToComponentDataArray<Modify>(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Modify modify = nativeArray[i];
			if (base.EntityManager.TryGetComponent<DistrictOptionData>(modify.m_Policy, out var component) && AreaUtils.HasOption(component, DistrictOption.ForbidBicycles))
			{
				if (!districts.IsCreated)
				{
					districts = new NativeHashSet<Entity>(nativeArray.Length, Allocator.TempJob);
				}
				districts.Add(modify.m_Entity);
			}
		}
		nativeArray.Dispose();
		return districts.IsCreated;
	}

	private void UpdateLanes(NativeQueue<UpdateData> updateQueue)
	{
		NativeParallelMultiHashMap<Entity, UpdateData> updateMap = new NativeParallelMultiHashMap<Entity, UpdateData>(100, Allocator.TempJob);
		FillUpdateMapJob jobData = new FillUpdateMapJob
		{
			m_UpdateQueue = updateQueue,
			m_UpdateMap = updateMap
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new SecondaryLaneAnchorJob
		{
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UtilityLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpdateMap = updateMap
		}, dependsOn: IJobExtensions.Schedule(jobData, base.Dependency), query: m_LaneQuery);
		updateMap.Dispose(jobHandle);
		base.Dependency = jobHandle;
	}

	private void UpdateObjects(NativeQueue<UpdateData> updateQueue, bool hasObjectUpdates, bool hasPolicyUpdates, NativeHashSet<Entity> districts)
	{
		NativeQueue<SubObjectOwnerData> ownerQueue = new NativeQueue<SubObjectOwnerData>(Allocator.TempJob);
		NativeList<SubObjectOwnerData> nativeList = new NativeList<SubObjectOwnerData>(Allocator.TempJob);
		JobHandle dependsOn = base.Dependency;
		if (hasObjectUpdates)
		{
			dependsOn = JobChunkExtensions.ScheduleParallel(new CheckSubObjectOwnersJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ChangedDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_ChangedDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ChangedDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_ChangedDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_AppliedTypes = m_AppliedTypes,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_OwnerQueue = ownerQueue.AsParallelWriter()
			}, m_ObjectQuery, dependsOn);
		}
		if (hasPolicyUpdates)
		{
			dependsOn = JobChunkExtensions.ScheduleParallel(new CheckBorderDistrictsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BorderDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_Districts = districts,
				m_OwnerQueue = ownerQueue.AsParallelWriter()
			}, m_BorderDistrictQuery, dependsOn);
		}
		CollectSubObjectOwnersJob jobData = new CollectSubObjectOwnersJob
		{
			m_OwnerQueue = ownerQueue,
			m_OwnerList = nativeList
		};
		UpdateSubObjectsJob jobData2 = new UpdateSubObjectsJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrafficLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficLightData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrafficSignData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficSignData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StreetLightData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneDirectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LaneDirectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabThemeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ThemeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabUtilityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_TrafficLight_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StreetLightData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_StreetLight_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetNodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetEdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_UtilityLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficLightsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrafficLights_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetCompositionObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PlaceholderObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ObjectRequirements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_DefaultNetLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_DefaultNetLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_RandomSeed = RandomSeed.Next(),
			m_DefaultTheme = m_CityConfigurationSystem.defaultTheme,
			m_AppliedTypes = m_AppliedTypes,
			m_SecondaryOwnerTypes = m_SecondaryOwnerTypes,
			m_TempAnimationTypes = m_TempAnimationTypes,
			m_OwnerList = nativeList.AsDeferredJobArray(),
			m_UpdateQueue = updateQueue.AsParallelWriter(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependsOn);
		JobHandle jobHandle2 = jobData2.Schedule(nativeList, 1, jobHandle);
		ownerQueue.Dispose(jobHandle);
		nativeList.Dispose(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
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
	public SecondaryObjectSystem()
	{
	}
}
