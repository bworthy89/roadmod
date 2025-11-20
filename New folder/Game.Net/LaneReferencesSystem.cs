using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class LaneReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateLaneReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> m_ParkingLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		public BufferLookup<SubLane> m_Lanes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (m_Lanes.TryGetBuffer(nativeArray2[i].m_Owner, out var bufferData))
					{
						CollectionUtils.RemoveValue(bufferData, new SubLane(nativeArray[i], ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking)));
					}
				}
				return;
			}
			NativeArray<ParkingLane> nativeArray3 = chunk.GetNativeArray(ref m_ParkingLaneType);
			NativeArray<ConnectionLane> nativeArray4 = chunk.GetNativeArray(ref m_ConnectionLaneType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			PathMethod pathMethod = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
			if (chunk.Has(ref m_PedestrianLaneType))
			{
				pathMethod |= PathMethod.Pedestrian;
			}
			if (chunk.Has(ref m_TrackLaneType))
			{
				pathMethod |= PathMethod.Track;
			}
			bool flag = chunk.Has(ref m_CarLaneType);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				DynamicBuffer<SubLane> buffer = m_Lanes[nativeArray2[j].m_Owner];
				PathMethod pathMethod2 = pathMethod;
				if (flag)
				{
					PrefabRef prefabRef = nativeArray5[j];
					if (m_PrefabCarLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
					{
						if ((componentData.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Road;
						}
						if ((componentData.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Bicycle;
						}
					}
				}
				if (CollectionUtils.TryGet(nativeArray3, j, out var value))
				{
					PrefabRef prefabRef2 = nativeArray5[j];
					if (m_PrefabParkingLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						if ((componentData2.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 = (((value.m_Flags & ParkingLaneFlags.SpecialVehicles) == 0) ? (pathMethod2 | (PathMethod.Parking | PathMethod.Boarding)) : (pathMethod2 | (PathMethod.Boarding | PathMethod.SpecialParking)));
						}
						if ((componentData2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.BicycleParking;
						}
					}
				}
				if (CollectionUtils.TryGet(nativeArray4, j, out var value2))
				{
					if ((value2.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						pathMethod2 |= PathMethod.Pedestrian;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						pathMethod2 |= PathMethod.Road;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Track) != 0)
					{
						pathMethod2 |= PathMethod.Track;
					}
					if ((value2.m_Flags & ConnectionLaneFlags.Parking) != 0)
					{
						if ((value2.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.Parking | PathMethod.Boarding;
						}
						if ((value2.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
						{
							pathMethod2 |= PathMethod.BicycleParking;
						}
					}
				}
				CollectionUtils.TryAddUniqueValue(buffer, new SubLane(nativeArray[j], pathMethod2));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillNodeMapJob : IJob
	{
		public NativeQueue<Lane> m_SkipLaneQueue;

		public NativeHashMap<PathNode, PathNode> m_PathNodeMap;

		public void Execute()
		{
			Lane item;
			while (m_SkipLaneQueue.TryDequeue(out item))
			{
				PathNode item2 = new PathNode(item.m_MiddleNode, secondaryNode: false);
				m_PathNodeMap.TryAdd(item.m_StartNode, item2);
				m_PathNodeMap.TryAdd(item.m_MiddleNode, item2);
				m_PathNodeMap.TryAdd(item.m_EndNode, item2);
			}
		}
	}

	[BurstCompile]
	private struct FixSkippedLanesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		[ReadOnly]
		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public NativeHashMap<PathNode, PathNode> m_PathNodeMap;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			if (bufferAccessor.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<SubLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubLaneType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[i];
				DynamicBuffer<SubLane> bufferData = bufferAccessor2[i];
				RefRW<Lane> refRW;
				for (int j = 0; j < bufferData.Length; j++)
				{
					SubLane subLane = bufferData[j];
					if ((subLane.m_PathMethods & (PathMethod.Pedestrian | PathMethod.Road | PathMethod.Bicycle)) != 0)
					{
						refRW = m_LaneData.GetRefRW(subLane.m_SubLane);
						ref Lane valueRW = ref refRW.ValueRW;
						UpdatePathNode(ref valueRW.m_StartNode, nativeArray2.Length != 0);
						UpdatePathNode(ref valueRW.m_EndNode, nativeArray2.Length != 0);
					}
				}
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					ConnectedEdge connectedEdge = dynamicBuffer[k];
					if (!m_SubLanes.TryGetBuffer(connectedEdge.m_Edge, out bufferData))
					{
						continue;
					}
					Edge edge = m_EdgeData[connectedEdge.m_Edge];
					bool2 x = new bool2(edge.m_Start == entity, edge.m_End == entity);
					if (!math.any(x))
					{
						continue;
					}
					float num = math.select(0f, 1f, x.y);
					int segmentIndex = math.select(0, 4, x.y);
					bool flag = m_UpdatedData.HasComponent(connectedEdge.m_Edge);
					for (int l = 0; l < bufferData.Length; l++)
					{
						SubLane subLane2 = bufferData[l];
						if ((subLane2.m_PathMethods & (PathMethod.Pedestrian | PathMethod.Road | PathMethod.Track | PathMethod.Bicycle)) == 0 || !m_EdgeLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData))
						{
							continue;
						}
						bool2 x2 = componentData.m_EdgeDelta == num;
						if (math.any(x2))
						{
							refRW = m_LaneData.GetRefRW(subLane2.m_SubLane);
							ref Lane valueRW2 = ref refRW.ValueRW;
							bool flag2 = ((!x2.x) ? UpdatePathNode(ref valueRW2.m_EndNode, valueRW2.m_MiddleNode, segmentIndex, entity, nativeArray2.Length != 0) : UpdatePathNode(ref valueRW2.m_StartNode, valueRW2.m_MiddleNode, segmentIndex, entity, nativeArray2.Length != 0));
							if (flag2 && !flag)
							{
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, subLane2.m_SubLane, default(PathfindUpdated));
							}
						}
					}
				}
			}
		}

		private void UpdatePathNode(ref PathNode pathNode, bool isTemp)
		{
			PathNode pathNode2 = pathNode.StripCurvePos();
			if (m_PathNodeMap.TryGetValue(new PathNode(pathNode2, isTemp), out var item))
			{
				pathNode = item;
			}
		}

		private bool UpdatePathNode(ref PathNode pathNode, PathNode middleNode, int segmentIndex, Entity ownerNode, bool isTemp)
		{
			PathNode pathNode2 = pathNode;
			if (pathNode.OwnerEquals(new PathNode(ownerNode, 0)))
			{
				pathNode2 = middleNode;
				pathNode2.SetSegmentIndex((byte)segmentIndex);
			}
			if (!m_PathNodeMap.TryGetValue(new PathNode(pathNode2, isTemp), out var item))
			{
				item = pathNode2;
			}
			if (!pathNode.Equals(item))
			{
				pathNode = item;
				return true;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLaneIndicesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Road> m_RoadType;

		[ReadOnly]
		public ComponentTypeHandle<TramTrack> m_TramTrackType;

		[ReadOnly]
		public ComponentTypeHandle<TrainTrack> m_TrainTrackType;

		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeList<SubLaneOrder> list = new NativeList<SubLaneOrder>(256, Allocator.Temp);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubLane> bufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
			bool flag = false;
			TrackTypes trackTypes = TrackTypes.None;
			if (chunk.Has(ref m_NodeType))
			{
				flag = chunk.Has(ref m_RoadType);
				if (flag)
				{
					if (chunk.Has(ref m_TramTrackType))
					{
						trackTypes |= TrackTypes.Tram;
					}
					if (chunk.Has(ref m_TrainTrackType))
					{
						trackTypes |= TrackTypes.Train;
					}
				}
			}
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<SubLane> dynamicBuffer = bufferAccessor[i];
				TrackTypes trackTypes2 = TrackTypes.None;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					SubLaneOrder value = new SubLaneOrder
					{
						m_SubLane = dynamicBuffer[j]
					};
					if (flag && (value.m_SubLane.m_PathMethods & PathMethod.Track) != 0)
					{
						PrefabRef prefabRef = m_PrefabRefData[value.m_SubLane.m_SubLane];
						if (m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							trackTypes2 |= componentData.m_TrackTypes;
						}
					}
					SlaveLane componentData3;
					if (m_MasterLaneData.TryGetComponent(value.m_SubLane.m_SubLane, out var componentData2))
					{
						value.m_Group = componentData2.m_Group;
						value.m_Index = -1;
						value.m_FullLane = false;
					}
					else if (m_SlaveLaneData.TryGetComponent(value.m_SubLane.m_SubLane, out componentData3))
					{
						value.m_Group = componentData3.m_Group;
						value.m_Index = (componentData3.m_SubIndex << 16) | j;
						value.m_FullLane = (componentData3.m_Flags & (SlaveLaneFlags.StartingLane | SlaveLaneFlags.EndingLane)) == 0;
						value.m_MergeLane = (componentData3.m_Flags & SlaveLaneFlags.MergingLane) != 0;
					}
					else if (m_SecondaryLaneData.HasComponent(value.m_SubLane.m_SubLane))
					{
						value.m_Group = uint.MaxValue;
						value.m_Index = j;
					}
					else
					{
						value.m_Group = 4294967294u;
						value.m_Index = j;
					}
					list.Add(in value);
				}
				list.Sort();
				int num = 0;
				while (num < list.Length)
				{
					SubLaneOrder subLaneOrder = list[num];
					if (subLaneOrder.m_Group < 4294967294u)
					{
						int num2 = num;
						int k = num + 1;
						int num3 = math.select(0, 1, subLaneOrder.m_FullLane);
						bool flag2 = false;
						for (; k < list.Length; k++)
						{
							SubLaneOrder subLaneOrder2 = list[k];
							if (subLaneOrder2.m_Group != subLaneOrder.m_Group)
							{
								break;
							}
							bool flag3 = (subLaneOrder2.m_SubLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) == PathMethod.Bicycle;
							num3 += math.select(0, 1, subLaneOrder2.m_FullLane && !flag3);
							flag2 = flag2 || flag3;
						}
						int num4 = -1;
						if (subLaneOrder.m_Index == -1)
						{
							num2++;
							MasterLane value2 = m_MasterLaneData[subLaneOrder.m_SubLane.m_SubLane];
							value2.m_Flags &= ~MasterLaneFlags.HasBikeOnlyLane;
							if (flag2)
							{
								value2.m_Flags |= MasterLaneFlags.HasBikeOnlyLane;
							}
							value2.m_MinIndex = (ushort)num2;
							value2.m_MaxIndex = (ushort)(k - 1);
							m_MasterLaneData[subLaneOrder.m_SubLane.m_SubLane] = value2;
							num4 = num;
							dynamicBuffer[num++] = subLaneOrder.m_SubLane;
						}
						Lane lane = default(Lane);
						Lane lane2 = default(Lane);
						bool flag4 = false;
						if (num < k)
						{
							SubLaneOrder subLaneOrder3 = list[num];
							lane2 = m_LaneData[subLaneOrder3.m_SubLane.m_SubLane];
						}
						while (num < k)
						{
							subLaneOrder = list[num];
							SlaveLane value3 = m_SlaveLaneData[subLaneOrder.m_SubLane.m_SubLane];
							value3.m_Flags &= ~(SlaveLaneFlags.MultipleLanes | SlaveLaneFlags.OpenStartLeft | SlaveLaneFlags.OpenStartRight | SlaveLaneFlags.OpenEndLeft | SlaveLaneFlags.OpenEndRight);
							Lane lane3 = lane2;
							if (num > num2 && subLaneOrder.m_MergeLane == flag4)
							{
								if (!lane.m_StartNode.Equals(lane3.m_StartNode))
								{
									value3.m_Flags |= SlaveLaneFlags.OpenStartLeft;
								}
								if (!lane.m_EndNode.Equals(lane3.m_EndNode))
								{
									value3.m_Flags |= SlaveLaneFlags.OpenEndLeft;
								}
							}
							if (num + 1 < k)
							{
								SubLaneOrder subLaneOrder4 = list[num + 1];
								lane2 = m_LaneData[subLaneOrder4.m_SubLane.m_SubLane];
								if (subLaneOrder.m_MergeLane == subLaneOrder4.m_MergeLane)
								{
									if (!lane2.m_StartNode.Equals(lane3.m_StartNode))
									{
										value3.m_Flags |= SlaveLaneFlags.OpenStartRight;
									}
									if (!lane2.m_EndNode.Equals(lane3.m_EndNode))
									{
										value3.m_Flags |= SlaveLaneFlags.OpenEndRight;
									}
								}
							}
							lane = lane3;
							flag4 = subLaneOrder.m_MergeLane;
							value3.m_MinIndex = (ushort)num2;
							value3.m_MaxIndex = (ushort)(k - 1);
							value3.m_SubIndex = (ushort)num;
							value3.m_MasterIndex = (ushort)math.select(num4, num, num4 == -1);
							if (num3 >= 2)
							{
								value3.m_Flags |= SlaveLaneFlags.MultipleLanes;
							}
							m_SlaveLaneData[subLaneOrder.m_SubLane.m_SubLane] = value3;
							dynamicBuffer[num++] = subLaneOrder.m_SubLane;
						}
					}
					else
					{
						dynamicBuffer[num++] = subLaneOrder.m_SubLane;
					}
				}
				if (trackTypes != trackTypes2)
				{
					Entity e = nativeArray[i];
					if (((uint)trackTypes & (uint)(byte)(~(int)trackTypes2) & 2) != 0)
					{
						m_CommandBuffer.RemoveComponent<TramTrack>(unfilteredChunkIndex, e);
					}
					if (((uint)trackTypes2 & (uint)(byte)(~(int)trackTypes) & 2) != 0)
					{
						m_CommandBuffer.AddComponent<TramTrack>(unfilteredChunkIndex, e);
					}
					if (((uint)trackTypes & (uint)(byte)(~(int)trackTypes2) & 1) != 0)
					{
						m_CommandBuffer.RemoveComponent<TrainTrack>(unfilteredChunkIndex, e);
					}
					if (((uint)trackTypes2 & (uint)(byte)(~(int)trackTypes) & 1) != 0)
					{
						m_CommandBuffer.AddComponent<TrainTrack>(unfilteredChunkIndex, e);
					}
				}
				list.Clear();
			}
			list.Dispose();
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct SubLaneOrder : IComparable<SubLaneOrder>
	{
		public uint m_Group;

		public int m_Index;

		public SubLane m_SubLane;

		public bool m_FullLane;

		public bool m_MergeLane;

		public int CompareTo(SubLaneOrder other)
		{
			int num = math.select(0, math.select(1, -1, other.m_Group > m_Group), other.m_Group != m_Group);
			return math.select(num, m_Index - other.m_Index, num == 0);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingLane> __Game_Net_ParkingLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		public BufferLookup<SubLane> __Game_Net_SubLane_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		public ComponentLookup<Lane> __Game_Net_Lane_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TramTrack> __Game_Net_TramTrack_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainTrack> __Game_Net_TrainTrack_RO_ComponentTypeHandle;

		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RW_ComponentLookup;

		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PedestrianLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrackLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConnectionLane>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RW_BufferLookup = state.GetBufferLookup<SubLane>();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Net_Lane_RW_ComponentLookup = state.GetComponentLookup<Lane>();
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
			__Game_Net_TramTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TramTrack>(isReadOnly: true);
			__Game_Net_TrainTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainTrack>(isReadOnly: true);
			__Game_Net_SubLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>();
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Net_MasterLane_RW_ComponentLookup = state.GetComponentLookup<MasterLane>();
			__Game_Net_SlaveLane_RW_ComponentLookup = state.GetComponentLookup<SlaveLane>();
		}
	}

	private ModificationBarrier4B m_ModificationBarrier;

	private EntityQuery m_LanesQuery;

	private EntityQuery m_UpdatedOwnersQuery;

	private EntityQuery m_AllOwnersQuery;

	private NativeQueue<Lane> m_SkipLaneQueue;

	private JobHandle m_SkipLaneDeps;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_LanesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Lane>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<SecondaryLane>() }
		});
		m_UpdatedOwnersQuery = GetEntityQuery(ComponentType.ReadWrite<SubLane>(), ComponentType.ReadOnly<Updated>());
		m_AllOwnersQuery = GetEntityQuery(ComponentType.ReadWrite<SubLane>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	public NativeQueue<Lane> GetSkipLaneQueue()
	{
		m_SkipLaneQueue = new NativeQueue<Lane>(Allocator.TempJob);
		return m_SkipLaneQueue;
	}

	public void AddSkipLaneWriter(JobHandle dependency)
	{
		m_SkipLaneDeps = dependency;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllOwnersQuery : m_UpdatedOwnersQuery);
		if (!m_LanesQuery.IsEmptyIgnoreFilter)
		{
			UpdateLaneReferencesJob jobData = new UpdateLaneReferencesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkingLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RW_BufferLookup, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.Schedule(jobData, m_LanesQuery, base.Dependency);
		}
		if (m_SkipLaneQueue.IsCreated)
		{
			NativeHashMap<PathNode, PathNode> pathNodeMap = new NativeHashMap<PathNode, PathNode>(100, Allocator.TempJob);
			FillNodeMapJob jobData2 = new FillNodeMapJob
			{
				m_SkipLaneQueue = m_SkipLaneQueue,
				m_PathNodeMap = pathNodeMap
			};
			FixSkippedLanesJob jobData3 = new FixSkippedLanesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PathNodeMap = pathNodeMap,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle = IJobExtensions.Schedule(jobData2, m_SkipLaneDeps);
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData3, m_UpdatedOwnersQuery, JobHandle.CombineDependencies(jobHandle, base.Dependency));
			m_SkipLaneQueue.Dispose(jobHandle);
			pathNodeMap.Dispose(jobHandle2);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			base.Dependency = jobHandle2;
		}
		if (!query.IsEmptyIgnoreFilter)
		{
			UpdateLaneIndicesJob jobData4 = new UpdateLaneIndicesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RoadType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TramTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrainTrackType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData4, query, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public LaneReferencesSystem()
	{
	}
}
