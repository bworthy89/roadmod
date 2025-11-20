using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class LaneOverlapSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddNonUpdatedEdgesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public NativeList<Entity> m_Entities;

		public void Execute()
		{
			NativeHashSet<Entity> nativeHashSet = new NativeHashSet<Entity>(m_Entities.Length, Allocator.Temp);
			for (int i = 0; i < m_Entities.Length; i++)
			{
				if (!m_ConnectedEdges.TryGetBuffer(m_Entities[i], out var bufferData))
				{
					continue;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					Entity value = bufferData[j].m_Edge;
					if (!m_UpdatedData.HasComponent(value) && nativeHashSet.Add(value))
					{
						m_Entities.Add(in value);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateLaneFlagsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public BufferTypeHandle<LaneOverlap> m_LaneOverlapType;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		public ComponentTypeHandle<NodeLane> m_NodeLaneType;

		public ComponentTypeHandle<CarLane> m_CarLaneType;

		public ComponentTypeHandle<TrackLane> m_TrackLaneType;

		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlapData;

		[ReadOnly]
		public NativeParallelMultiHashMap<PathNode, LaneSourceData> m_SourceMap;

		[ReadOnly]
		public NativeParallelMultiHashMap<PathNode, LaneTargetData> m_TargetMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Lane> nativeArray2 = chunk.GetNativeArray(ref m_LaneType);
			NativeArray<CarLane> nativeArray3 = chunk.GetNativeArray(ref m_CarLaneType);
			NativeArray<TrackLane> nativeArray4 = chunk.GetNativeArray(ref m_TrackLaneType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<EdgeLane> nativeArray6 = chunk.GetNativeArray(ref m_EdgeLaneType);
			if (nativeArray6.Length != 0)
			{
				for (int i = 0; i < nativeArray6.Length; i++)
				{
					Lane lane = nativeArray2[i];
					EdgeLane value = nativeArray6[i];
					bool flag = nativeArray3.Length != 0;
					bool trackLanes = nativeArray4.Length != 0 && !flag;
					if (lane.m_StartNode.OwnerEquals(lane.m_MiddleNode))
					{
						value.m_ConnectedStartCount = (byte)math.clamp(CalculateConnectedSources(lane.m_StartNode, flag, trackLanes), 0, 255);
					}
					else
					{
						value.m_ConnectedStartCount = 1;
					}
					if (lane.m_EndNode.OwnerEquals(lane.m_MiddleNode))
					{
						value.m_ConnectedEndCount = (byte)math.clamp(CalculateConnectedTargets(lane.m_EndNode, flag, trackLanes), 0, 255);
					}
					else
					{
						value.m_ConnectedEndCount = 1;
					}
					if (value.m_ConnectedStartCount == 0 && nativeArray5.Length != 0 && (value.m_EdgeDelta.x == 0f || value.m_EdgeDelta.x == 1f) && m_EdgeData.TryGetComponent(nativeArray5[i].m_Owner, out var componentData) && m_OutsideConnectionData.HasComponent((value.m_EdgeDelta.x == 0f) ? componentData.m_Start : componentData.m_End))
					{
						value.m_ConnectedStartCount = 1;
					}
					if (value.m_ConnectedEndCount == 0 && nativeArray5.Length != 0 && (value.m_EdgeDelta.y == 0f || value.m_EdgeDelta.y == 1f) && m_EdgeData.TryGetComponent(nativeArray5[i].m_Owner, out var componentData2) && m_OutsideConnectionData.HasComponent((value.m_EdgeDelta.y == 0f) ? componentData2.m_Start : componentData2.m_End))
					{
						value.m_ConnectedEndCount = 1;
					}
					nativeArray6[i] = value;
				}
				if (nativeArray3.Length != 0)
				{
					NativeParallelHashSet<PathNode> nativeParallelHashSet = default(NativeParallelHashSet<PathNode>);
					NativeList<LaneTargetData> nativeList = default(NativeList<LaneTargetData>);
					NativeArray<Curve> nativeArray7 = chunk.GetNativeArray(ref m_CurveType);
					NativeArray<SlaveLane> nativeArray8 = chunk.GetNativeArray(ref m_SlaveLaneType);
					bool flag2 = chunk.Has(ref m_MasterLaneType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Lane lane2 = nativeArray2[j];
						EdgeLane edgeLane = nativeArray6[j];
						Curve curve = nativeArray7[j];
						CarLane value2 = nativeArray3[j];
						SlaveLane slaveLane = default(SlaveLane);
						if (nativeArray8.Length != 0)
						{
							slaveLane = nativeArray8[j];
							slaveLane.m_Flags &= ~(SlaveLaneFlags.MergingLane | SlaveLaneFlags.SplitLeft | SlaveLaneFlags.SplitRight | SlaveLaneFlags.MergeLeft | SlaveLaneFlags.MergeRight);
						}
						value2.m_Flags &= ~(CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.LevelCrossing | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.Forward | CarLaneFlags.Approach | CarLaneFlags.Roundabout | CarLaneFlags.ForbidPassing | CarLaneFlags.RightOfWay | CarLaneFlags.TrafficLights);
						bool flag3 = (value2.m_Flags & CarLaneFlags.Highway) != 0;
						if (flag3 && CollectionUtils.TryGet(nativeArray5, j, out var value3) && m_CompositionData.TryGetComponent(value3.m_Owner, out var componentData3) && m_PrefabCompositionData.TryGetComponent(componentData3.m_Edge, out var componentData4) && (componentData4.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes | CompositionState.Multilane)) == (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes | CompositionState.Multilane))
						{
							value2.m_Flags |= CarLaneFlags.ForbidPassing;
						}
						if (value2.m_Curviness > math.select(MathF.PI / 180f, MathF.PI / 360f, flag3) || edgeLane.m_ConnectedEndCount == 0)
						{
							value2.m_Flags |= CarLaneFlags.ForbidPassing;
						}
						if ((edgeLane.m_ConnectedStartCount == 0) | (edgeLane.m_ConnectedEndCount == 0))
						{
							if (nativeArray5.Length != 0 && edgeLane.m_ConnectedEndCount == 0)
							{
								slaveLane.m_Flags |= GetMergeLaneFlags(nativeArray5[j].m_Owner, slaveLane, lane2, (value2.m_Flags & CarLaneFlags.Invert) != 0);
								if ((slaveLane.m_Flags & (SlaveLaneFlags.MergeLeft | SlaveLaneFlags.MergeRight)) != 0)
								{
									value2.m_Flags |= CarLaneFlags.Approach;
								}
							}
							else
							{
								slaveLane.m_Flags |= SlaveLaneFlags.MergingLane;
							}
						}
						bool flag4 = false;
						if (lane2.m_StartNode.OwnerEquals(lane2.m_MiddleNode) && m_SourceMap.TryGetFirstValue(lane2.m_StartNode, out var item, out var it))
						{
							do
							{
								if (item.m_IsTrack)
								{
									continue;
								}
								if (!item.m_IsEdge)
								{
									bool flag5 = false;
									bool flag6 = false;
									do
									{
										flag5 |= (item.m_SlaveFlags & SlaveLaneFlags.OpenEndLeft) != 0;
										flag6 |= (item.m_SlaveFlags & SlaveLaneFlags.OpenEndRight) != 0;
									}
									while (m_SourceMap.TryGetNextValue(out item, ref it));
									if (!flag5)
									{
										slaveLane.m_Flags |= SlaveLaneFlags.SplitLeft;
									}
									if (!flag6)
									{
										slaveLane.m_Flags |= SlaveLaneFlags.SplitRight;
									}
								}
								break;
							}
							while (m_SourceMap.TryGetNextValue(out item, ref it));
						}
						if (lane2.m_EndNode.OwnerEquals(lane2.m_MiddleNode) && m_TargetMap.TryGetFirstValue(lane2.m_EndNode, out var item2, out var it2))
						{
							do
							{
								if (item2.m_IsTrack)
								{
									continue;
								}
								bool flag7 = !item2.m_IsEdge;
								if (item2.m_IsEdge)
								{
									if (!flag3)
									{
										item2.m_CarFlags &= ~CarLaneFlags.ForbidPassing;
									}
									value2.m_Flags |= item2.m_CarFlags;
									bool flag8 = !item2.m_EndNode.OwnerEquals(lane2.m_MiddleNode);
									if (!flag8 && m_TargetMap.TryGetFirstValue(item2.m_EndNode, out item2, out it2))
									{
										do
										{
											if (!item2.m_IsTrack)
											{
												flag8 = true;
												break;
											}
										}
										while (m_TargetMap.TryGetNextValue(out item2, ref it2));
									}
									if (!flag8 && flag3)
									{
										value2.m_Flags |= CarLaneFlags.ForbidPassing;
									}
								}
								if (item2.m_IsEdge)
								{
									break;
								}
								bool flag9 = false;
								bool flag10 = false;
								do
								{
									if (item2.m_IsTrack)
									{
										continue;
									}
									if ((item2.m_CarFlags & CarLaneFlags.Roundabout) != 0)
									{
										if (!flag4)
										{
											if (nativeParallelHashSet.IsCreated)
											{
												nativeParallelHashSet.Clear();
												nativeList.Clear();
											}
											else
											{
												nativeParallelHashSet = new NativeParallelHashSet<PathNode>(32, Allocator.Temp);
												nativeList = new NativeList<LaneTargetData>(32, Allocator.Temp);
											}
											nativeParallelHashSet.Add(lane2.m_EndNode);
											flag4 = true;
										}
										nativeList.Add(in item2);
										CarLaneFlags carLaneFlags = item2.m_CarFlags & (CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.RightOfWay);
										if (carLaneFlags != 0)
										{
											value2.m_Flags |= carLaneFlags | CarLaneFlags.Approach;
										}
									}
									else
									{
										CarLaneFlags carLaneFlags2 = item2.m_CarFlags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.LevelCrossing | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.Forward | CarLaneFlags.ForbidPassing | CarLaneFlags.RightOfWay | CarLaneFlags.TrafficLights);
										if (!flag2 && item2.m_IsMaster)
										{
											carLaneFlags2 = (CarLaneFlags)((uint)carLaneFlags2 & 0xFFE1FFCDu);
										}
										if (!flag2 && (carLaneFlags2 & CarLaneFlags.Yield) != 0 && (item2.m_IsMaster || !HasRoadOverlaps(item2.m_Entity)))
										{
											carLaneFlags2 = (CarLaneFlags)((uint)carLaneFlags2 & 0xFFFFFBFFu);
										}
										if (((uint)carLaneFlags2 & 0xFDFFFFFFu) != 0)
										{
											value2.m_Flags |= CarLaneFlags.Approach;
										}
										value2.m_Flags |= carLaneFlags2;
									}
									flag9 |= (item2.m_SlaveFlags & SlaveLaneFlags.OpenStartLeft) != 0;
									flag10 |= (item2.m_SlaveFlags & SlaveLaneFlags.OpenStartRight) != 0;
								}
								while (m_TargetMap.TryGetNextValue(out item2, ref it2));
								if (flag7)
								{
									if (!flag9)
									{
										slaveLane.m_Flags |= SlaveLaneFlags.SplitLeft;
									}
									if (!flag10)
									{
										slaveLane.m_Flags |= SlaveLaneFlags.SplitRight;
									}
								}
								break;
							}
							while (m_TargetMap.TryGetNextValue(out item2, ref it2));
						}
						if (flag4)
						{
							float2 @float = math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier).xz);
							CarLaneFlags carLaneFlags3 = CarLaneFlags.Approach | CarLaneFlags.Roundabout | CarLaneFlags.ForbidPassing;
							int num = 0;
							while (num < nativeList.Length)
							{
								LaneTargetData laneTargetData = nativeList[num++];
								if (!nativeParallelHashSet.Add(laneTargetData.m_EndNode))
								{
									continue;
								}
								bool flag11 = false;
								if (m_TargetMap.TryGetFirstValue(laneTargetData.m_EndNode, out item2, out it2))
								{
									do
									{
										if ((item2.m_CarFlags & CarLaneFlags.Roundabout) != 0)
										{
											item2.m_TurnAmount += laneTargetData.m_TurnAmount;
											if (math.abs(laneTargetData.m_TurnAmount) < MathF.PI * 2f)
											{
												nativeList.Add(in item2);
											}
											flag11 = true;
										}
									}
									while (m_TargetMap.TryGetNextValue(out item2, ref it2));
								}
								if (flag11)
								{
									continue;
								}
								Curve curve2 = m_CurveData[laneTargetData.m_Entity];
								float2 endDirection = -@float;
								if (curve2.m_Length > 0.1f)
								{
									endDirection = math.normalizesafe(-MathUtils.EndTangent(curve2.m_Bezier).xz);
								}
								if (NetUtils.IsTurn(curve.m_Bezier.d.xz, @float, curve2.m_Bezier.d.xz, endDirection, out var right, out var gentle, out var uturn))
								{
									if (laneTargetData.m_TurnAmount > 0f == right)
									{
										carLaneFlags3 = (CarLaneFlags)((!gentle) ? ((!uturn) ? ((uint)carLaneFlags3 | (uint)(right ? 32 : 16)) : ((uint)carLaneFlags3 | (uint)(right ? 131072 : 2))) : ((uint)carLaneFlags3 | (uint)(right ? 524288 : 262144)));
									}
								}
								else if (math.abs(laneTargetData.m_TurnAmount) < MathF.PI / 2f)
								{
									carLaneFlags3 |= CarLaneFlags.Forward;
								}
							}
							if ((carLaneFlags3 & (CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.Forward)) != 0)
							{
								carLaneFlags3 = (CarLaneFlags)((uint)carLaneFlags3 & 0xFFFDFFFDu);
							}
							value2.m_Flags |= carLaneFlags3;
						}
						nativeArray3[j] = value2;
						if (nativeArray8.Length != 0)
						{
							nativeArray8[j] = slaveLane;
						}
					}
					if (nativeParallelHashSet.IsCreated)
					{
						nativeParallelHashSet.Dispose();
					}
					if (nativeList.IsCreated)
					{
						nativeList.Dispose();
					}
				}
			}
			else if (nativeArray3.Length != 0)
			{
				NativeArray<NodeLane> nativeArray9 = chunk.GetNativeArray(ref m_NodeLaneType);
				NativeArray<MasterLane> nativeArray10 = chunk.GetNativeArray(ref m_MasterLaneType);
				BufferAccessor<LaneOverlap> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneOverlapType);
				if (nativeArray9.Length != 0)
				{
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						if ((nativeArray3[k].m_Flags & CarLaneFlags.Roundabout) != 0)
						{
							Lane lane3 = nativeArray2[k];
							NodeLane value4 = nativeArray9[k];
							if (lane3.m_EndNode.OwnerEquals(lane3.m_MiddleNode) && CalculateConnectedTargets(lane3.m_EndNode, carLanes: true, trackLanes: false) == 0)
							{
								value4.m_SharedEndCount = byte.MaxValue;
							}
							if (lane3.m_StartNode.OwnerEquals(lane3.m_MiddleNode) && CalculateConnectedSources(lane3.m_StartNode, carLanes: true, trackLanes: false) == 0)
							{
								value4.m_SharedStartCount = byte.MaxValue;
							}
							nativeArray9[k] = value4;
						}
					}
				}
				for (int l = 0; l < nativeArray3.Length; l++)
				{
					CarLane value5 = nativeArray3[l];
					value5.m_LaneCrossCount = 0;
					if (nativeArray10.Length != 0)
					{
						MasterLane masterLane = nativeArray10[l];
						Owner owner = nativeArray5[l];
						int num2 = 256;
						if (m_SubLanes.HasBuffer(owner.m_Owner))
						{
							DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
							for (int m = masterLane.m_MinIndex; m <= masterLane.m_MaxIndex; m++)
							{
								Entity subLane = dynamicBuffer[m].m_SubLane;
								if (m_LaneOverlapData.TryGetBuffer(subLane, out var bufferData))
								{
									int num3 = 0;
									for (int n = 0; n < bufferData.Length; n++)
									{
										LaneOverlap laneOverlap = bufferData[n];
										num3 += math.select(0, 1, ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleStart | OverlapFlags.MergeMiddleEnd | OverlapFlags.Unsafe | OverlapFlags.Road)) == OverlapFlags.Road) | ((laneOverlap.m_Flags & (OverlapFlags.Road | OverlapFlags.MergeFlip)) == (OverlapFlags.Road | OverlapFlags.MergeFlip)));
									}
									num3 = math.min(255, num3);
									num2 = math.min(num3, num2);
								}
							}
						}
						value5.m_LaneCrossCount = (byte)math.select(num2, 0, num2 == 256);
					}
					else if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<LaneOverlap> dynamicBuffer2 = bufferAccessor[l];
						int num4 = 0;
						for (int num5 = 0; num5 < dynamicBuffer2.Length; num5++)
						{
							LaneOverlap laneOverlap2 = dynamicBuffer2[num5];
							num4 += math.select(0, 1, ((laneOverlap2.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleStart | OverlapFlags.MergeMiddleEnd | OverlapFlags.Unsafe | OverlapFlags.Road)) == OverlapFlags.Road) | ((laneOverlap2.m_Flags & (OverlapFlags.Road | OverlapFlags.MergeFlip)) == (OverlapFlags.Road | OverlapFlags.MergeFlip)));
						}
						value5.m_LaneCrossCount = (byte)math.min(255, num4);
					}
					nativeArray3[l] = value5;
				}
			}
			if (nativeArray4.Length == 0)
			{
				return;
			}
			BufferAccessor<LaneOverlap> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LaneOverlapType);
			for (int num6 = 0; num6 < nativeArray4.Length; num6++)
			{
				Entity entity = nativeArray[num6];
				TrackLane value6 = nativeArray4[num6];
				Lane lane4 = nativeArray2[num6];
				value6.m_Flags &= ~(TrackLaneFlags.Switch | TrackLaneFlags.DiamondCrossing | TrackLaneFlags.CrossingTraffic | TrackLaneFlags.MergingTraffic | TrackLaneFlags.DoubleSwitch);
				value6.m_Flags |= TrackLaneFlags.StartingLane | TrackLaneFlags.EndingLane;
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<LaneOverlap> dynamicBuffer3 = bufferAccessor2[num6];
					OverlapFlags overlapFlags = (OverlapFlags)0;
					for (int num7 = 0; num7 < dynamicBuffer3.Length; num7++)
					{
						LaneOverlap laneOverlap3 = dynamicBuffer3[num7];
						if ((laneOverlap3.m_Flags & OverlapFlags.Track) != 0)
						{
							OverlapFlags overlapFlags2 = laneOverlap3.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd);
							overlapFlags |= overlapFlags2;
							if (overlapFlags2 == (OverlapFlags)0)
							{
								value6.m_Flags |= TrackLaneFlags.DiamondCrossing;
							}
							else if (overlapFlags == (OverlapFlags.MergeStart | OverlapFlags.MergeEnd))
							{
								value6.m_Flags |= TrackLaneFlags.Switch | TrackLaneFlags.DoubleSwitch;
							}
							else
							{
								value6.m_Flags |= TrackLaneFlags.Switch;
							}
						}
						else if (nativeArray6.Length == 0)
						{
							if ((laneOverlap3.m_Flags & OverlapFlags.MergeEnd) != 0)
							{
								value6.m_Flags |= TrackLaneFlags.MergingTraffic;
							}
							else if ((laneOverlap3.m_Flags & OverlapFlags.MergeStart) == 0)
							{
								value6.m_Flags |= TrackLaneFlags.CrossingTraffic;
							}
						}
					}
				}
				if (!lane4.m_StartNode.OwnerEquals(lane4.m_MiddleNode))
				{
					value6.m_Flags &= ~TrackLaneFlags.StartingLane;
				}
				if (!lane4.m_EndNode.OwnerEquals(lane4.m_MiddleNode))
				{
					value6.m_Flags &= ~TrackLaneFlags.EndingLane;
				}
				if ((value6.m_Flags & TrackLaneFlags.StartingLane) != 0 && m_SourceMap.TryGetFirstValue(lane4.m_StartNode, out var item3, out var it3))
				{
					do
					{
						if (item3.m_Entity != entity && item3.m_IsTrack)
						{
							value6.m_Flags &= ~TrackLaneFlags.StartingLane;
							break;
						}
					}
					while (m_SourceMap.TryGetNextValue(out item3, ref it3));
				}
				if ((value6.m_Flags & TrackLaneFlags.StartingLane) != 0 && m_TargetMap.TryGetFirstValue(lane4.m_StartNode, out var item4, out var it4))
				{
					do
					{
						if (item4.m_Entity != entity && item4.m_IsTrack)
						{
							value6.m_Flags &= ~TrackLaneFlags.StartingLane;
							break;
						}
					}
					while (m_TargetMap.TryGetNextValue(out item4, ref it4));
				}
				if ((value6.m_Flags & TrackLaneFlags.EndingLane) != 0 && m_SourceMap.TryGetFirstValue(lane4.m_EndNode, out var item5, out var it5))
				{
					do
					{
						if (item5.m_Entity != entity && item5.m_IsTrack)
						{
							value6.m_Flags &= ~TrackLaneFlags.EndingLane;
							break;
						}
					}
					while (m_SourceMap.TryGetNextValue(out item5, ref it5));
				}
				if ((value6.m_Flags & TrackLaneFlags.EndingLane) != 0 && m_TargetMap.TryGetFirstValue(lane4.m_EndNode, out var item6, out var it6))
				{
					do
					{
						if (item6.m_Entity != entity && item6.m_IsTrack)
						{
							value6.m_Flags &= ~TrackLaneFlags.EndingLane;
							break;
						}
					}
					while (m_TargetMap.TryGetNextValue(out item6, ref it6));
				}
				if (nativeArray6.Length != 0 && nativeArray5.Length != 0 && (value6.m_Flags & (TrackLaneFlags.StartingLane | TrackLaneFlags.EndingLane)) != 0)
				{
					EdgeLane edgeLane2 = nativeArray6[num6];
					if ((value6.m_Flags & TrackLaneFlags.StartingLane) != 0 && (edgeLane2.m_EdgeDelta.x == 0f || edgeLane2.m_EdgeDelta.x == 1f) && m_EdgeData.TryGetComponent(nativeArray5[num6].m_Owner, out var componentData5) && m_OutsideConnectionData.HasComponent((edgeLane2.m_EdgeDelta.x == 0f) ? componentData5.m_Start : componentData5.m_End))
					{
						value6.m_Flags &= ~TrackLaneFlags.StartingLane;
					}
					if ((value6.m_Flags & TrackLaneFlags.EndingLane) != 0 && (edgeLane2.m_EdgeDelta.y == 0f || edgeLane2.m_EdgeDelta.y == 1f) && m_EdgeData.TryGetComponent(nativeArray5[num6].m_Owner, out var componentData6) && m_OutsideConnectionData.HasComponent((edgeLane2.m_EdgeDelta.y == 0f) ? componentData6.m_Start : componentData6.m_End))
					{
						value6.m_Flags &= ~TrackLaneFlags.EndingLane;
					}
				}
				nativeArray4[num6] = value6;
			}
		}

		private SlaveLaneFlags GetMergeLaneFlags(Entity owner, SlaveLane slaveLane, Lane lane, bool invert)
		{
			bool flag = false;
			bool flag2 = false;
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = slaveLane.m_MinIndex; i <= slaveLane.m_MaxIndex; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					Lane other = m_LaneData[subLane];
					if (lane.Equals(other))
					{
						if (flag)
						{
							if (!invert)
							{
								return SlaveLaneFlags.MergingLane | SlaveLaneFlags.MergeLeft;
							}
							return SlaveLaneFlags.MergingLane | SlaveLaneFlags.MergeRight;
						}
						flag2 = true;
					}
					if (!other.m_EndNode.OwnerEquals(other.m_MiddleNode) || !m_TargetMap.ContainsKey(other.m_EndNode))
					{
						continue;
					}
					if (flag2)
					{
						if (!invert)
						{
							return SlaveLaneFlags.MergingLane | SlaveLaneFlags.MergeRight;
						}
						return SlaveLaneFlags.MergingLane | SlaveLaneFlags.MergeLeft;
					}
					flag = true;
				}
			}
			return SlaveLaneFlags.MergingLane;
		}

		private bool HasRoadOverlaps(Entity entity)
		{
			if (m_LaneOverlapData.HasBuffer(entity))
			{
				DynamicBuffer<LaneOverlap> dynamicBuffer = m_LaneOverlapData[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if ((dynamicBuffer[i].m_Flags & (OverlapFlags.Unsafe | OverlapFlags.Road)) == OverlapFlags.Road)
					{
						return true;
					}
				}
			}
			return false;
		}

		private int CalculateConnectedSources(PathNode node, bool carLanes, bool trackLanes)
		{
			int num = 0;
			if (m_SourceMap.TryGetFirstValue(node, out var item, out var it))
			{
				do
				{
					if ((item.m_IsRoad && carLanes) | (item.m_IsTrack && trackLanes))
					{
						num++;
					}
				}
				while (m_SourceMap.TryGetNextValue(out item, ref it));
			}
			return num;
		}

		private int CalculateConnectedTargets(PathNode node, bool carLanes, bool trackLanes)
		{
			int num = 0;
			if (m_TargetMap.TryGetFirstValue(node, out var item, out var it))
			{
				do
				{
					if ((item.m_IsRoad && carLanes) | (item.m_IsTrack && trackLanes))
					{
						num++;
					}
				}
				while (m_TargetMap.TryGetNextValue(out item, ref it));
			}
			return num;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct LaneSourceData
	{
		public Entity m_Entity;

		public PathNode m_StartNode;

		public SlaveLaneFlags m_SlaveFlags;

		public bool m_IsEdge;

		public bool m_IsRoad;

		public bool m_IsTrack;
	}

	private struct LaneTargetData
	{
		public Entity m_Entity;

		public PathNode m_EndNode;

		public CarLaneFlags m_CarFlags;

		public SlaveLaneFlags m_SlaveFlags;

		public float m_TurnAmount;

		public bool m_IsEdge;

		public bool m_IsMaster;

		public bool m_IsRoad;

		public bool m_IsTrack;
	}

	[BurstCompile]
	private struct CollectLaneDirectionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> m_SlaveLaneType;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> m_MasterLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		public NativeParallelMultiHashMap<PathNode, LaneSourceData>.ParallelWriter m_SourceMap;

		public NativeParallelMultiHashMap<PathNode, LaneTargetData>.ParallelWriter m_TargetMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Lane> nativeArray2 = chunk.GetNativeArray(ref m_LaneType);
			bool flag = chunk.Has(ref m_EdgeLaneType);
			NativeArray<CarLane> nativeArray3 = chunk.GetNativeArray(ref m_CarLaneType);
			if (nativeArray3.Length != 0)
			{
				NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
				NativeArray<SlaveLane> nativeArray5 = chunk.GetNativeArray(ref m_SlaveLaneType);
				NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
				bool isMaster = chunk.Has(ref m_MasterLaneType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					Lane lane = nativeArray2[i];
					Curve curve = nativeArray4[i];
					CarLane carLane = nativeArray3[i];
					bool flag2 = false;
					if ((carLane.m_Flags & (CarLaneFlags.LevelCrossing | CarLaneFlags.TrafficLights)) != 0 && (carLane.m_Flags & CarLaneFlags.Unsafe) != 0 && !flag)
					{
						PrefabRef prefabRef = nativeArray6[i];
						if (m_PrefabCarLaneData.TryGetComponent(prefabRef, out var componentData) && componentData.m_RoadTypes == RoadTypes.Bicycle)
						{
							carLane.m_Flags &= CarLaneFlags.LevelCrossing | CarLaneFlags.TrafficLights;
							flag2 = true;
						}
					}
					if ((carLane.m_Flags & CarLaneFlags.Unsafe) != 0)
					{
						continue;
					}
					SlaveLaneFlags slaveFlags = (SlaveLaneFlags)0;
					SlaveLaneFlags slaveFlags2 = (SlaveLaneFlags)0;
					CarLaneFlags carLaneFlags = ~(CarLaneFlags.Unsafe | CarLaneFlags.UTurnLeft | CarLaneFlags.Invert | CarLaneFlags.SideConnection | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.LevelCrossing | CarLaneFlags.Twoway | CarLaneFlags.IsSecured | CarLaneFlags.Runway | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.SecondaryStart | CarLaneFlags.SecondaryEnd | CarLaneFlags.ForbidBicycles | CarLaneFlags.PublicOnly | CarLaneFlags.Highway | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.Forward | CarLaneFlags.Approach | CarLaneFlags.Roundabout | CarLaneFlags.RightLimit | CarLaneFlags.LeftLimit | CarLaneFlags.ForbidPassing | CarLaneFlags.RightOfWay | CarLaneFlags.TrafficLights | CarLaneFlags.ParkingLeft | CarLaneFlags.ParkingRight | CarLaneFlags.Forbidden | CarLaneFlags.AllowEnter);
					if (flag)
					{
						if ((carLane.m_Flags & CarLaneFlags.Highway) != 0 && carLane.m_Curviness > MathF.PI / 360f)
						{
							carLaneFlags |= CarLaneFlags.ForbidPassing;
						}
					}
					else
					{
						if (CollectionUtils.TryGet(nativeArray5, i, out var value) && !flag2)
						{
							slaveFlags = value.m_Flags & (SlaveLaneFlags.OpenEndLeft | SlaveLaneFlags.OpenEndRight);
							slaveFlags2 = value.m_Flags & (SlaveLaneFlags.OpenStartLeft | SlaveLaneFlags.OpenStartRight);
						}
						carLaneFlags = carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.LevelCrossing | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.Forward | CarLaneFlags.Roundabout | CarLaneFlags.ForbidPassing | CarLaneFlags.RightOfWay | CarLaneFlags.TrafficLights);
					}
					m_SourceMap.Add(lane.m_EndNode, new LaneSourceData
					{
						m_Entity = entity,
						m_StartNode = lane.m_StartNode,
						m_SlaveFlags = slaveFlags,
						m_IsEdge = flag,
						m_IsRoad = true
					});
					m_TargetMap.Add(lane.m_StartNode, new LaneTargetData
					{
						m_Entity = entity,
						m_EndNode = lane.m_EndNode,
						m_SlaveFlags = slaveFlags2,
						m_CarFlags = carLaneFlags,
						m_TurnAmount = CalculateTurnAmount(curve),
						m_IsEdge = flag,
						m_IsMaster = isMaster,
						m_IsRoad = true
					});
				}
			}
			else if (!chunk.Has(ref m_ConnectionLaneType))
			{
				bool isTrack = chunk.Has(ref m_TrackLaneType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Lane lane2 = nativeArray2[j];
					m_SourceMap.Add(lane2.m_EndNode, new LaneSourceData
					{
						m_Entity = entity2,
						m_StartNode = lane2.m_StartNode,
						m_IsEdge = flag,
						m_IsTrack = isTrack
					});
					m_TargetMap.Add(lane2.m_StartNode, new LaneTargetData
					{
						m_Entity = entity2,
						m_EndNode = lane2.m_EndNode,
						m_IsEdge = flag,
						m_IsTrack = isTrack
					});
				}
			}
		}

		private float CalculateTurnAmount(Curve curve)
		{
			float2 @float = math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz);
			float2 y = math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier).xz);
			float num = math.acos(math.clamp(math.dot(@float, y), -1f, 1f));
			return math.select(num, 0f - num, math.dot(MathUtils.Right(@float), y) < 0f);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct OverlapData
	{
		public Entity m_Entity;

		public LaneOverlap m_Overlap;

		public byte m_SharedStartDelta;

		public byte m_SharedEndDelta;
	}

	[BurstCompile]
	private struct ApplyExtraOverlapsJob : IJob
	{
		public ComponentLookup<NodeLane> m_NodeLaneData;

		public BufferLookup<LaneOverlap> m_Overlaps;

		public NativeQueue<OverlapData> m_ExtraOverlaps;

		public void Execute()
		{
			OverlapData item;
			while (m_ExtraOverlaps.TryDequeue(out item))
			{
				if (item.m_Overlap.m_Other != Entity.Null)
				{
					m_Overlaps[item.m_Entity].Add(item.m_Overlap);
				}
				if (item.m_SharedStartDelta != 0 || item.m_SharedEndDelta != 0)
				{
					ref NodeLane valueRW = ref m_NodeLaneData.GetRefRW(item.m_Entity).ValueRW;
					valueRW.m_SharedStartCount = (byte)math.min(254, valueRW.m_SharedStartCount + item.m_SharedStartDelta);
					valueRW.m_SharedEndCount = (byte)math.min(254, valueRW.m_SharedEndCount + item.m_SharedEndDelta);
				}
			}
		}
	}

	[BurstCompile]
	private struct SortLaneOverlapsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LaneOverlap> m_Overlaps;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (m_Overlaps.TryGetBuffer(subLane, out var bufferData) && !m_SecondaryLaneData.HasComponent(subLane))
				{
					bufferData.AsNativeArray().Sort();
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateLaneOverlapsJob : IJobParallelForDefer
	{
		private struct FindOverlapStackItem
		{
			public Bezier4x2 m_Curve1;

			public Bezier4x2 m_Curve2;

			public float2 m_CurvePos1;

			public float2 m_CurvePos2;

			public int2 m_Iterations;
		}

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LaneOverlap> m_Overlaps;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public bool m_UpdateAll;

		public NativeQueue<OverlapData>.ParallelWriter m_ExtraOverlaps;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			Edge componentData;
			bool isEdge = m_EdgeData.TryGetComponent(entity, out componentData);
			UpdateOverlaps(entity, m_SubLanes[entity], componentData, isEdge);
		}

		private void UpdateOverlaps(Entity entity, DynamicBuffer<SubLane> lanes, Edge edge, bool isEdge)
		{
			for (int i = 0; i < lanes.Length; i++)
			{
				Entity subLane = lanes[i].m_SubLane;
				if (!m_Overlaps.TryGetBuffer(subLane, out var bufferData) || m_SecondaryLaneData.HasComponent(subLane))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				Curve curve = m_CurveData[subLane];
				Lane laneData = m_LaneData[subLane];
				NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
				if ((prefabLaneData.m_Flags & (LaneFlags.Parking | LaneFlags.Virtual)) == (LaneFlags.Parking | LaneFlags.Virtual))
				{
					continue;
				}
				CarLane carLane = default(CarLane);
				if ((prefabLaneData.m_Flags & LaneFlags.Road) != 0)
				{
					carLane = m_CarLaneData[subLane];
				}
				PedestrianLane pedestrianLane = default(PedestrianLane);
				if ((prefabLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
				{
					pedestrianLane = m_PedestrianLaneData[subLane];
				}
				float angle = 0f;
				if ((prefabLaneData.m_Flags & LaneFlags.Parking) != 0 && m_ParkingLaneData.TryGetComponent(subLane, out var componentData) && m_PrefabParkingLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && componentData2.m_SlotAngle > 0.25f)
				{
					angle = math.select(MathF.PI / 2f - componentData2.m_SlotAngle, componentData2.m_SlotAngle - MathF.PI / 2f, (componentData.m_Flags & ParkingLaneFlags.ParkingLeft) != 0);
				}
				bool isTrain = false;
				if ((prefabLaneData.m_Flags & LaneFlags.Track) != 0 && m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					isTrain = (componentData3.m_TrackTypes & (TrackTypes.Train | TrackTypes.Subway)) != 0;
				}
				NodeLane componentData4;
				bool flag = m_NodeLaneData.TryGetComponent(subLane, out componentData4);
				bufferData.Clear();
				componentData4.m_SharedStartCount = 0;
				componentData4.m_SharedEndCount = 0;
				CheckOverlaps(subLane, laneData, curve, carLane, pedestrianLane, bufferData, angle, flag, isEdge, differentOwner: false, isTrain, ref componentData4, prefabLaneData, lanes, i);
				if (isEdge && !flag)
				{
					if ((laneData.m_StartNode.OwnerEquals(new PathNode(edge.m_Start, 0)) || laneData.m_EndNode.OwnerEquals(new PathNode(edge.m_Start, 0))) && (m_UpdateAll || m_UpdatedData.HasComponent(edge.m_Start)) && m_SubLanes.TryGetBuffer(edge.m_Start, out var bufferData2))
					{
						CheckOverlaps(subLane, laneData, curve, carLane, pedestrianLane, bufferData, angle, isNodeLane1: false, isEdge: true, differentOwner: true, isTrain, ref componentData4, prefabLaneData, bufferData2, bufferData2.Length);
					}
					if ((laneData.m_StartNode.OwnerEquals(new PathNode(edge.m_End, 0)) || laneData.m_EndNode.OwnerEquals(new PathNode(edge.m_End, 0))) && (m_UpdateAll || m_UpdatedData.HasComponent(edge.m_End)) && m_SubLanes.TryGetBuffer(edge.m_End, out var bufferData3))
					{
						CheckOverlaps(subLane, laneData, curve, carLane, pedestrianLane, bufferData, angle, isNodeLane1: false, isEdge: true, differentOwner: true, isTrain, ref componentData4, prefabLaneData, bufferData3, bufferData3.Length);
					}
				}
				if (flag)
				{
					m_NodeLaneData[subLane] = componentData4;
				}
			}
		}

		private void CheckOverlaps(Entity lane1, Lane laneData1, Curve curve1, CarLane carLane1, PedestrianLane pedestrianLane1, DynamicBuffer<LaneOverlap> overlaps1, float angle1, bool isNodeLane1, bool isEdge, bool differentOwner, bool isTrain1, ref NodeLane nodeLane1, NetLaneData prefabLaneData1, DynamicBuffer<SubLane> lanes, int count)
		{
			for (int i = 0; i < count; i++)
			{
				Entity subLane = lanes[i].m_SubLane;
				DynamicBuffer<LaneOverlap> bufferData = default(DynamicBuffer<LaneOverlap>);
				if (differentOwner)
				{
					if (!m_Overlaps.HasBuffer(subLane))
					{
						continue;
					}
				}
				else if (!m_Overlaps.TryGetBuffer(subLane, out bufferData))
				{
					continue;
				}
				if (m_SecondaryLaneData.HasComponent(subLane))
				{
					continue;
				}
				Curve curve2 = m_CurveData[subLane];
				Lane lane2 = m_LaneData[subLane];
				if ((laneData1.m_StartNode.Equals(lane2.m_EndNode) && math.dot(math.normalizesafe(MathUtils.StartTangent(curve1.m_Bezier).xz), math.normalizesafe(MathUtils.EndTangent(curve2.m_Bezier).xz)) >= 0f) || (laneData1.m_EndNode.Equals(lane2.m_StartNode) && math.dot(math.normalizesafe(MathUtils.EndTangent(curve1.m_Bezier).xz), math.normalizesafe(MathUtils.StartTangent(curve2.m_Bezier).xz)) >= 0f))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				NetLaneData netLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
				if ((netLaneData.m_Flags & LaneFlags.Parking) != 0)
				{
					if ((prefabLaneData1.m_Flags & LaneFlags.Road) == 0 || !isNodeLane1 || (netLaneData.m_Flags & LaneFlags.Virtual) != 0)
					{
						continue;
					}
				}
				else if ((prefabLaneData1.m_Flags & LaneFlags.Parking) != 0 && (netLaneData.m_Flags & LaneFlags.Road) == 0)
				{
					continue;
				}
				CarLane carLane2 = default(CarLane);
				if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
				{
					carLane2 = m_CarLaneData[subLane];
				}
				PedestrianLane pedestrianLane2 = default(PedestrianLane);
				if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
				{
					pedestrianLane2 = m_PedestrianLaneData[subLane];
				}
				float angle2 = 0f;
				if ((netLaneData.m_Flags & LaneFlags.Parking) != 0 && m_ParkingLaneData.TryGetComponent(subLane, out var componentData) && m_PrefabParkingLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && componentData2.m_SlotAngle > 0.25f)
				{
					angle2 = math.select(MathF.PI / 2f - componentData2.m_SlotAngle, componentData2.m_SlotAngle - MathF.PI / 2f, (componentData.m_Flags & ParkingLaneFlags.ParkingLeft) != 0);
				}
				bool flag = false;
				if ((netLaneData.m_Flags & LaneFlags.Parking) != 0 && m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					flag = (componentData3.m_TrackTypes & (TrackTypes.Train | TrackTypes.Subway)) != 0;
				}
				NodeLane componentData4;
				bool flag2 = m_NodeLaneData.TryGetComponent(subLane, out componentData4);
				if (!flag2 && ((prefabLaneData1.m_Flags & LaneFlags.Parking) != 0 || (isEdge && !isNodeLane1)))
				{
					continue;
				}
				Bezier4x2 xz = curve1.m_Bezier.xz;
				Bezier4x2 xz2 = curve2.m_Bezier.xz;
				float2 radius = (prefabLaneData1.m_Width + nodeLane1.m_WidthOffset) * 0.4f;
				float2 radius2 = (netLaneData.m_Width + componentData4.m_WidthOffset) * 0.4f;
				if (!FindOverlapRange(xz, xz2, radius, radius2, angle1, angle2, out var t))
				{
					continue;
				}
				float num = CalculateParallelism(xz, xz2, t);
				OverlapFlags overlapFlags = (OverlapFlags)0;
				OverlapFlags overlapFlags2 = (OverlapFlags)0;
				if ((prefabLaneData1.m_Flags & LaneFlags.Road) != 0)
				{
					overlapFlags2 |= OverlapFlags.Road;
				}
				if ((prefabLaneData1.m_Flags & LaneFlags.Track) != 0)
				{
					overlapFlags2 |= OverlapFlags.Track;
				}
				if ((prefabLaneData1.m_Flags & LaneFlags.OnWater) != 0)
				{
					overlapFlags2 |= OverlapFlags.Water;
				}
				if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
				{
					overlapFlags |= OverlapFlags.Road;
				}
				if ((netLaneData.m_Flags & LaneFlags.Track) != 0)
				{
					overlapFlags |= OverlapFlags.Track;
				}
				if ((netLaneData.m_Flags & LaneFlags.OnWater) != 0)
				{
					overlapFlags |= OverlapFlags.Water;
				}
				if (laneData1.m_StartNode.Equals(lane2.m_StartNode))
				{
					overlapFlags |= OverlapFlags.MergeStart;
					overlapFlags2 |= OverlapFlags.MergeStart;
				}
				else if (laneData1.m_StartNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode))
				{
					overlapFlags |= OverlapFlags.MergeStart;
					overlapFlags2 |= OverlapFlags.MergeMiddleStart;
				}
				else if (lane2.m_StartNode.EqualsIgnoreCurvePos(laneData1.m_MiddleNode))
				{
					overlapFlags |= OverlapFlags.MergeMiddleStart;
					overlapFlags2 |= OverlapFlags.MergeStart;
				}
				else if (laneData1.m_StartNode.Equals(lane2.m_EndNode))
				{
					overlapFlags |= OverlapFlags.MergeStart | OverlapFlags.MergeFlip;
					overlapFlags2 |= OverlapFlags.MergeEnd | OverlapFlags.MergeFlip;
				}
				if (laneData1.m_EndNode.Equals(lane2.m_EndNode))
				{
					overlapFlags |= OverlapFlags.MergeEnd;
					overlapFlags2 |= OverlapFlags.MergeEnd;
				}
				else if (laneData1.m_EndNode.EqualsIgnoreCurvePos(lane2.m_MiddleNode))
				{
					overlapFlags |= OverlapFlags.MergeEnd;
					overlapFlags2 |= OverlapFlags.MergeMiddleEnd;
				}
				else if (lane2.m_EndNode.EqualsIgnoreCurvePos(laneData1.m_MiddleNode))
				{
					overlapFlags |= OverlapFlags.MergeMiddleEnd;
					overlapFlags2 |= OverlapFlags.MergeEnd;
				}
				else if (laneData1.m_EndNode.Equals(lane2.m_StartNode))
				{
					overlapFlags |= OverlapFlags.MergeEnd | OverlapFlags.MergeFlip;
					overlapFlags2 |= OverlapFlags.MergeStart | OverlapFlags.MergeFlip;
				}
				if ((carLane1.m_Flags & CarLaneFlags.Unsafe) != 0 || (pedestrianLane1.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					overlapFlags2 |= OverlapFlags.Unsafe;
				}
				if ((carLane2.m_Flags & CarLaneFlags.Unsafe) != 0 || (pedestrianLane2.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					overlapFlags |= OverlapFlags.Unsafe;
				}
				if ((carLane1.m_Flags & CarLaneFlags.Approach) != 0)
				{
					carLane1.m_Flags &= ~(CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.RightOfWay);
				}
				if ((carLane2.m_Flags & CarLaneFlags.Approach) != 0)
				{
					carLane2.m_Flags &= ~(CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.RightOfWay);
				}
				LaneFlags laneFlags = prefabLaneData1.m_Flags ^ netLaneData.m_Flags;
				CarLaneFlags carLaneFlags = carLane1.m_Flags ^ carLane2.m_Flags;
				int num2 = 0;
				int num3 = 0;
				float2 @float;
				if (((overlapFlags | overlapFlags2) & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd)) == 0)
				{
					@float = math.max(y: new float2(curve1.m_Length, curve2.m_Length) * (t.yw - t.xz), x: 1f);
					@float *= num / @float.yx;
					overlapFlags |= OverlapFlags.OverlapLeft | OverlapFlags.OverlapRight;
					overlapFlags2 |= OverlapFlags.OverlapLeft | OverlapFlags.OverlapRight;
				}
				else
				{
					@float = num;
					if ((carLaneFlags & CarLaneFlags.Unsafe) == 0 && (laneFlags & LaneFlags.Road) == 0)
					{
						if (isNodeLane1 && (overlapFlags & OverlapFlags.MergeStart) != 0)
						{
							nodeLane1.m_SharedStartCount = (byte)math.min(254, nodeLane1.m_SharedStartCount + 1);
						}
						if (flag2 && (overlapFlags2 & OverlapFlags.MergeStart) != 0)
						{
							num2 = 1;
						}
						if (isNodeLane1 && (overlapFlags & OverlapFlags.MergeEnd) != 0)
						{
							nodeLane1.m_SharedEndCount = (byte)math.min(254, nodeLane1.m_SharedEndCount + 1);
						}
						if (flag2 && (overlapFlags2 & OverlapFlags.MergeEnd) != 0)
						{
							num3 = 1;
						}
						if (flag2 && !differentOwner)
						{
							componentData4.m_SharedStartCount = (byte)math.min(254, componentData4.m_SharedStartCount + num2);
							componentData4.m_SharedEndCount = (byte)math.min(254, componentData4.m_SharedEndCount + num3);
							m_NodeLaneData[subLane] = componentData4;
						}
					}
					float2 xz3;
					if ((overlapFlags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd)) == OverlapFlags.MergeStart)
					{
						xz3 = MathUtils.Tangent(curve1.m_Bezier, t.y).xz;
						float2 float2 = curve1.m_Bezier.d.xz - MathUtils.Position(curve1.m_Bezier, math.lerp(t.x, t.y, 0.5f)).xz;
						xz3 = math.select(xz3, float2, math.dot(MathUtils.StartTangent(curve1.m_Bezier).xz, float2) >= 0f);
					}
					else if ((overlapFlags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd)) == OverlapFlags.MergeEnd)
					{
						xz3 = MathUtils.Tangent(curve1.m_Bezier, t.x).xz;
						float2 float3 = MathUtils.Position(curve1.m_Bezier, math.lerp(t.x, t.y, 0.5f)).xz - curve1.m_Bezier.a.xz;
						xz3 = math.select(xz3, float3, math.dot(MathUtils.EndTangent(curve1.m_Bezier).xz, float3) >= 0f);
					}
					else
					{
						xz3 = curve1.m_Bezier.d.xz - curve1.m_Bezier.a.xz;
					}
					float2 xz4;
					if ((overlapFlags2 & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd)) == OverlapFlags.MergeStart)
					{
						xz4 = MathUtils.Tangent(curve2.m_Bezier, t.w).xz;
						float2 float4 = curve2.m_Bezier.d.xz - MathUtils.Position(curve2.m_Bezier, math.lerp(t.z, t.w, 0.5f)).xz;
						xz4 = math.select(xz4, float4, math.dot(MathUtils.StartTangent(curve2.m_Bezier).xz, float4) >= 0f);
					}
					else if ((overlapFlags2 & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd)) == OverlapFlags.MergeEnd)
					{
						xz4 = MathUtils.Tangent(curve2.m_Bezier, t.z).xz;
						float2 float5 = MathUtils.Position(curve2.m_Bezier, math.lerp(t.z, t.w, 0.5f)).xz - curve2.m_Bezier.a.xz;
						xz4 = math.select(xz4, float5, math.dot(MathUtils.EndTangent(curve2.m_Bezier).xz, float5) >= 0f);
					}
					else
					{
						xz4 = curve2.m_Bezier.d.xz - curve2.m_Bezier.a.xz;
					}
					bool flag3 = math.dot(MathUtils.Right(xz3), xz4) > 0f == ((overlapFlags & OverlapFlags.MergeFlip) == 0);
					if ((overlapFlags & OverlapFlags.MergeStart) != 0)
					{
						t.x = 0f;
						overlapFlags = (OverlapFlags)((uint)overlapFlags | (uint)(flag3 ? 8 : 4));
					}
					if ((overlapFlags2 & OverlapFlags.MergeStart) != 0)
					{
						t.z = 0f;
						overlapFlags2 = (OverlapFlags)((uint)overlapFlags2 | (uint)(flag3 ? 4 : 8));
					}
					if ((overlapFlags & OverlapFlags.MergeEnd) != 0)
					{
						t.y = 1f;
						overlapFlags = (OverlapFlags)((uint)overlapFlags | (uint)(flag3 ? 4 : 8));
					}
					if ((overlapFlags2 & OverlapFlags.MergeEnd) != 0)
					{
						t.w = 1f;
						overlapFlags2 = (OverlapFlags)((uint)overlapFlags2 | (uint)(flag3 ? 8 : 4));
					}
				}
				if ((prefabLaneData1.m_Flags & LaneFlags.Road) != 0 && flag)
				{
					overlapFlags |= OverlapFlags.Slow;
				}
				if ((netLaneData.m_Flags & LaneFlags.Road) != 0 && isTrain1)
				{
					overlapFlags2 |= OverlapFlags.Slow;
				}
				int num4 = 0;
				if (((prefabLaneData1.m_Flags | netLaneData.m_Flags) & LaneFlags.Pedestrian) != 0)
				{
					if ((prefabLaneData1.m_Flags & LaneFlags.Road) != 0 && (pedestrianLane2.m_Flags & PedestrianLaneFlags.Unsafe) == 0)
					{
						num4 = 1;
					}
					else if ((netLaneData.m_Flags & LaneFlags.Road) != 0 && (pedestrianLane1.m_Flags & PedestrianLaneFlags.Unsafe) == 0)
					{
						num4 = -1;
					}
				}
				else if ((overlapFlags & (OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart)) == 0)
				{
					if ((carLaneFlags & CarLaneFlags.Stop) != 0)
					{
						num4 = math.select(1, -1, (carLane2.m_Flags & CarLaneFlags.Stop) != 0);
					}
					else if ((carLaneFlags & CarLaneFlags.Yield) != 0)
					{
						num4 = math.select(1, -1, (carLane2.m_Flags & CarLaneFlags.Yield) != 0);
					}
					else if ((carLaneFlags & CarLaneFlags.RightOfWay) != 0)
					{
						num4 = math.select(1, -1, (carLane1.m_Flags & CarLaneFlags.RightOfWay) != 0);
					}
					else if ((carLaneFlags & CarLaneFlags.Unsafe) != 0)
					{
						num4 = math.select(1, -1, (carLane2.m_Flags & CarLaneFlags.Unsafe) != 0);
					}
					else
					{
						float2 float6 = math.lerp(t.xz, t.yw, 0.5f);
						float2 xz5 = MathUtils.Tangent(curve1.m_Bezier, float6.x).xz;
						float2 xz6 = MathUtils.Tangent(curve2.m_Bezier, float6.y).xz;
						num4 = math.csum(math.select(test: ((!m_LeftHandTraffic) ? new float2(math.dot(MathUtils.Left(xz5), xz6), math.dot(MathUtils.Left(xz6), xz5)) : new float2(math.dot(MathUtils.Right(xz5), xz6), math.dot(MathUtils.Right(xz6), xz5))) > 0f, falseValue: default(int2), trueValue: new int2(1, -1)));
					}
				}
				if ((netLaneData.m_Flags & LaneFlags.Parking) == 0)
				{
					overlaps1.Add(new LaneOverlap(subLane, t, overlapFlags, @float.x, num4));
				}
				if (differentOwner)
				{
					if (num2 != 0 || num3 != 0 || (prefabLaneData1.m_Flags & LaneFlags.Parking) == 0)
					{
						OverlapData value = new OverlapData
						{
							m_Entity = subLane,
							m_SharedStartDelta = (byte)num2,
							m_SharedEndDelta = (byte)num3
						};
						if ((prefabLaneData1.m_Flags & LaneFlags.Parking) == 0)
						{
							value.m_Overlap = new LaneOverlap(lane1, t.zwxy, overlapFlags2, @float.y, -num4);
						}
						m_ExtraOverlaps.Enqueue(value);
					}
				}
				else if ((prefabLaneData1.m_Flags & LaneFlags.Parking) == 0)
				{
					bufferData.Add(new LaneOverlap(lane1, t.zwxy, overlapFlags2, @float.y, -num4));
				}
			}
		}

		private static float CalculateParallelism(Bezier4x2 curve1, Bezier4x2 curve2, float4 overlapRange)
		{
			float2 @float = math.lerp(overlapRange.xz, overlapRange.yw, 0.5f);
			float2 x = math.normalizesafe(MathUtils.Tangent(curve1, overlapRange.x));
			float2 x2 = math.normalizesafe(MathUtils.Tangent(curve1, @float.x));
			float2 x3 = math.normalizesafe(MathUtils.Tangent(curve1, overlapRange.y));
			float2 y = math.normalizesafe(MathUtils.Tangent(curve2, overlapRange.z));
			float2 y2 = math.normalizesafe(MathUtils.Tangent(curve2, @float.y));
			float2 y3 = math.normalizesafe(MathUtils.Tangent(curve2, overlapRange.w));
			return math.max(0f, (math.dot(x, y) + math.dot(x2, y2) + math.dot(x3, y3)) * (1f / 3f));
		}

		private unsafe static bool FindOverlapRange(Bezier4x2 curve1, Bezier4x2 curve2, float2 radius1, float2 radius2, float angle1, float angle2, out float4 t)
		{
			bool result = false;
			t = new float4(2f, -1f, 2f, -1f);
			FindOverlapStackItem* ptr = stackalloc FindOverlapStackItem[13];
			int num = 0;
			float2 @float = default(float2);
			@float.x = MathUtils.Length(curve1);
			@float.y = MathUtils.Length(curve2);
			@float = math.sqrt(@float / math.max(0.1f, math.min(new float2(radius1.x, radius2.x), new float2(radius1.y, radius2.y))));
			float2 float2 = 1f / math.cos(new float2(angle1, angle2));
			float2 float3 = math.tan(new float2(angle1, angle2));
			int2 x = default(int2);
			x.x = Mathf.RoundToInt(@float.x);
			x.y = Mathf.RoundToInt(@float.y);
			x = math.min(x, 4);
			ptr[num++] = new FindOverlapStackItem
			{
				m_Curve1 = curve1,
				m_Curve2 = curve2,
				m_CurvePos1 = new float2(0f, 1f),
				m_CurvePos2 = new float2(0f, 1f),
				m_Iterations = x
			};
			while (num != 0)
			{
				ref FindOverlapStackItem reference = ref ptr[--num];
				float2 x2 = math.lerp(radius1.xx, radius1.yy, reference.m_CurvePos1);
				float2 x3 = math.lerp(radius2.xx, radius2.yy, reference.m_CurvePos2);
				Bounds2 bounds = MathUtils.Bounds(reference.m_Curve1);
				Bounds2 bounds2 = MathUtils.Bounds(reference.m_Curve2);
				float2 x4 = new float2(math.cmax(x2), math.cmax(x3)) * float2;
				float num2 = math.csum(x4);
				if (!(MathUtils.DistanceSquared(bounds, bounds2) < num2 * num2))
				{
					continue;
				}
				x = reference.m_Iterations - 1;
				if (math.all(x < 0))
				{
					float2 value = MathUtils.Right(MathUtils.StartTangent(reference.m_Curve1));
					float2 value2 = MathUtils.Right(MathUtils.EndTangent(reference.m_Curve1));
					float2 value3 = MathUtils.Right(MathUtils.StartTangent(reference.m_Curve2));
					float2 value4 = MathUtils.Right(MathUtils.EndTangent(reference.m_Curve2));
					MathUtils.TryNormalize(ref value);
					MathUtils.TryNormalize(ref value2);
					MathUtils.TryNormalize(ref value3);
					MathUtils.TryNormalize(ref value4);
					value *= x2.x;
					value2 *= x2.y;
					value3 *= x3.x;
					value4 *= x3.y;
					value += MathUtils.Left(value) * float3.x;
					value2 += MathUtils.Left(value2) * float3.x;
					value3 += MathUtils.Left(value3) * float3.y;
					value4 += MathUtils.Left(value4) * float3.y;
					Quad2 quad = new Quad2(reference.m_Curve1.a + value, reference.m_Curve1.d + value2, reference.m_Curve1.d - value2, reference.m_Curve1.a - value);
					Quad2 quad2 = new Quad2(reference.m_Curve2.a + value3, reference.m_Curve2.d + value4, reference.m_Curve2.d - value4, reference.m_Curve2.a - value3);
					Line2.Segment ab = quad.ab;
					Line2.Segment dc = quad.dc;
					Line2.Segment ad = quad.ad;
					Line2.Segment bc = quad.bc;
					Line2.Segment ab2 = quad2.ab;
					Line2.Segment dc2 = quad2.dc;
					Line2.Segment ad2 = quad2.ad;
					Line2.Segment bc2 = quad2.bc;
					bounds = MathUtils.Expand(bounds, x4.x);
					bounds2 = MathUtils.Expand(bounds2, x4.y);
					float2 t2;
					if (MathUtils.Intersect(MathUtils.Bounds(ab), bounds2))
					{
						if (MathUtils.Intersect(ab, ab2, out t2))
						{
							t2 = math.lerp(new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.x), new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.y), t2);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ab, dc2, out t2))
						{
							t2 = math.lerp(new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.x), new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.y), t2);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ab, ad2, out t2))
						{
							t2 = new float2(math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, t2.x), reference.m_CurvePos2.x);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ab, bc2, out t2))
						{
							t2 = new float2(math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, t2.x), reference.m_CurvePos2.y);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
					}
					if (MathUtils.Intersect(MathUtils.Bounds(dc), bounds2))
					{
						if (MathUtils.Intersect(dc, ab2, out t2))
						{
							t2 = math.lerp(new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.x), new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.y), t2);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(dc, dc2, out t2))
						{
							t2 = math.lerp(new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.x), new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.y), t2);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(dc, ad2, out t2))
						{
							t2 = new float2(math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, t2.x), reference.m_CurvePos2.x);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(dc, bc2, out t2))
						{
							t2 = new float2(math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, t2.x), reference.m_CurvePos2.y);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
					}
					if (MathUtils.Intersect(MathUtils.Bounds(ad), bounds2))
					{
						if (MathUtils.Intersect(ad, ab2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.x, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, t2.y));
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ad, dc2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.x, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, t2.y));
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ad, ad2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.x);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(ad, bc2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.x, reference.m_CurvePos2.y);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
					}
					if (MathUtils.Intersect(MathUtils.Bounds(bc), bounds2))
					{
						if (MathUtils.Intersect(bc, ab2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.y, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, t2.y));
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(bc, dc2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.y, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, t2.y));
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(bc, ad2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.x);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
						if (MathUtils.Intersect(bc, bc2, out t2))
						{
							t2 = new float2(reference.m_CurvePos1.y, reference.m_CurvePos2.y);
							t.xz = math.min(t.xz, t2);
							t.yw = math.max(t.yw, t2);
							result = true;
						}
					}
				}
				else if (math.all(x >= 0))
				{
					MathUtils.Divide(reference.m_Curve1, out var output, out var output2, 0.5f);
					MathUtils.Divide(reference.m_Curve2, out var output3, out var output4, 0.5f);
					float3 float4 = new float3(reference.m_CurvePos1.x, math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, 0.5f), reference.m_CurvePos1.y);
					float3 float5 = new float3(reference.m_CurvePos2.x, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, 0.5f), reference.m_CurvePos2.y);
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output,
						m_Curve2 = output3,
						m_CurvePos1 = float4.xy,
						m_CurvePos2 = float5.xy,
						m_Iterations = x
					};
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output,
						m_Curve2 = output4,
						m_CurvePos1 = float4.xy,
						m_CurvePos2 = float5.yz,
						m_Iterations = x
					};
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output2,
						m_Curve2 = output3,
						m_CurvePos1 = float4.yz,
						m_CurvePos2 = float5.xy,
						m_Iterations = x
					};
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output2,
						m_Curve2 = output4,
						m_CurvePos1 = float4.yz,
						m_CurvePos2 = float5.yz,
						m_Iterations = x
					};
				}
				else if (x.x >= 0)
				{
					MathUtils.Divide(reference.m_Curve1, out var output5, out var output6, 0.5f);
					curve2 = reference.m_Curve2;
					float3 float6 = new float3(reference.m_CurvePos1.x, math.lerp(reference.m_CurvePos1.x, reference.m_CurvePos1.y, 0.5f), reference.m_CurvePos1.y);
					float2 curvePos = reference.m_CurvePos2;
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output5,
						m_Curve2 = curve2,
						m_CurvePos1 = float6.xy,
						m_CurvePos2 = curvePos,
						m_Iterations = x
					};
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = output6,
						m_Curve2 = curve2,
						m_CurvePos1 = float6.yz,
						m_CurvePos2 = curvePos,
						m_Iterations = x
					};
				}
				else
				{
					curve1 = reference.m_Curve1;
					MathUtils.Divide(reference.m_Curve2, out var output7, out var output8, 0.5f);
					float2 curvePos2 = reference.m_CurvePos1;
					float3 float7 = new float3(reference.m_CurvePos2.x, math.lerp(reference.m_CurvePos2.x, reference.m_CurvePos2.y, 0.5f), reference.m_CurvePos2.y);
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = curve1,
						m_Curve2 = output7,
						m_CurvePos1 = curvePos2,
						m_CurvePos2 = float7.xy,
						m_Iterations = x
					};
					ptr[num++] = new FindOverlapStackItem
					{
						m_Curve1 = curve1,
						m_Curve2 = output8,
						m_CurvePos1 = curvePos2,
						m_CurvePos2 = float7.yz,
						m_Iterations = x
					};
				}
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RW_ComponentLookup;

		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RW_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MasterLane> __Game_Net_MasterLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NodeLane> __Game_Net_NodeLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarLane> __Game_Net_CarLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<TrackLane> __Game_Net_TrackLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<SlaveLane> __Game_Net_SlaveLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<ParkingLane>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Net_NodeLane_RW_ComponentLookup = state.GetComponentLookup<NodeLane>();
			__Game_Net_LaneOverlap_RW_BufferLookup = state.GetBufferLookup<LaneOverlap>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrackLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MasterLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferTypeHandle = state.GetBufferTypeHandle<LaneOverlap>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<OutsideConnection>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_EdgeLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>();
			__Game_Net_NodeLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NodeLane>();
			__Game_Net_CarLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarLane>();
			__Game_Net_TrackLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrackLane>();
			__Game_Net_SlaveLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SlaveLane>();
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
		}
	}

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_UpdatedOwnersQuery;

	private EntityQuery m_UpdatedLanesQuery;

	private EntityQuery m_AllOwnersQuery;

	private EntityQuery m_AllLanesQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_UpdatedOwnersQuery = GetEntityQuery(ComponentType.ReadOnly<SubLane>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>());
		m_UpdatedLanesQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<SecondaryLane>());
		m_AllOwnersQuery = GetEntityQuery(ComponentType.ReadOnly<SubLane>(), ComponentType.Exclude<Deleted>());
		m_AllLanesQuery = GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<SecondaryLane>());
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

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		EntityQuery entityQuery = (loaded ? m_AllOwnersQuery : m_UpdatedOwnersQuery);
		EntityQuery query = (loaded ? m_AllLanesQuery : m_UpdatedLanesQuery);
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			int capacity = query.CalculateEntityCount();
			NativeParallelMultiHashMap<PathNode, LaneSourceData> sourceMap = new NativeParallelMultiHashMap<PathNode, LaneSourceData>(capacity, Allocator.TempJob);
			NativeParallelMultiHashMap<PathNode, LaneTargetData> targetMap = new NativeParallelMultiHashMap<PathNode, LaneTargetData>(capacity, Allocator.TempJob);
			JobHandle outJobHandle;
			NativeList<Entity> nativeList = entityQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			NativeQueue<OverlapData> extraOverlaps = new NativeQueue<OverlapData>(Allocator.TempJob);
			if (!loaded)
			{
				outJobHandle = IJobExtensions.Schedule(new AddNonUpdatedEdgesJob
				{
					m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
					m_Entities = nativeList
				}, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
			}
			UpdateLaneOverlapsJob jobData = new UpdateLaneOverlapsJob
			{
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Overlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RW_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList.AsDeferredJobArray(),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_UpdateAll = loaded,
				m_ExtraOverlaps = extraOverlaps.AsParallelWriter()
			};
			ApplyExtraOverlapsJob jobData2 = new ApplyExtraOverlapsJob
			{
				m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Overlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RW_BufferLookup, ref base.CheckedStateRef),
				m_ExtraOverlaps = extraOverlaps
			};
			SortLaneOverlapsJob jobData3 = new SortLaneOverlapsJob
			{
				m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_Overlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RW_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList.AsDeferredJobArray()
			};
			CollectLaneDirectionsJob jobData4 = new CollectLaneDirectionsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SourceMap = sourceMap.AsParallelWriter(),
				m_TargetMap = targetMap.AsParallelWriter()
			};
			UpdateLaneFlagsJob jobData5 = new UpdateLaneFlagsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MasterLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LaneOverlapType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SlaveLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_SlaveLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneOverlapData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
				m_SourceMap = sourceMap,
				m_TargetMap = targetMap
			};
			JobHandle dependsOn = jobData.Schedule(nativeList, 1, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
			JobHandle jobHandle = IJobExtensions.Schedule(jobData2, dependsOn);
			JobHandle jobHandle2 = jobData3.Schedule(nativeList, 1, jobHandle);
			JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(dependsOn: JobHandle.CombineDependencies(jobHandle2, JobChunkExtensions.ScheduleParallel(jobData4, query, base.Dependency)), jobData: jobData5, query: query);
			sourceMap.Dispose(jobHandle3);
			targetMap.Dispose(jobHandle3);
			nativeList.Dispose(jobHandle2);
			extraOverlaps.Dispose(jobHandle);
			base.Dependency = jobHandle3;
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
	public LaneOverlapSystem()
	{
	}
}
