using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
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
public class WatercraftNavigationSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNavigationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Moving> m_MovingType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<Watercraft> m_WatercraftType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<WatercraftNavigation> m_NavigationType;

		public ComponentTypeHandle<WatercraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public ComponentTypeHandle<Blocker> m_BlockerType;

		public ComponentTypeHandle<Odometer> m_OdometerType;

		public BufferTypeHandle<WatercraftNavigationLane> m_NavigationLaneType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<LaneSignal> m_LaneSignalData;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLaneData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> m_TakeoffLocationData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Watercraft> m_WatercraftData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<VehicleSideEffectData> m_PrefabSideEffectData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public NativeQueue<WatercraftNavigationHelpers.LaneReservation>.ParallelWriter m_LaneReservations;

		public NativeQueue<WatercraftNavigationHelpers.LaneEffects>.ParallelWriter m_LaneEffects;

		public NativeQueue<WatercraftNavigationHelpers.LaneSignal>.ParallelWriter m_LaneSignals;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Moving> nativeArray = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Blocker> nativeArray2 = chunk.GetNativeArray(ref m_BlockerType);
			NativeArray<WatercraftCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (nativeArray.Length != 0)
			{
				NativeArray<Entity> nativeArray4 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Game.Objects.Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<Target> nativeArray6 = chunk.GetNativeArray(ref m_TargetType);
				NativeArray<Watercraft> nativeArray7 = chunk.GetNativeArray(ref m_WatercraftType);
				NativeArray<WatercraftNavigation> nativeArray8 = chunk.GetNativeArray(ref m_NavigationType);
				NativeArray<Odometer> nativeArray9 = chunk.GetNativeArray(ref m_OdometerType);
				NativeArray<PathOwner> nativeArray10 = chunk.GetNativeArray(ref m_PathOwnerType);
				NativeArray<PrefabRef> nativeArray11 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<WatercraftNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_NavigationLaneType);
				BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
				WatercraftLaneSelectBuffer laneSelectBuffer = default(WatercraftLaneSelectBuffer);
				bool flag = nativeArray9.Length != 0;
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray4[i];
					Game.Objects.Transform transform = nativeArray5[i];
					Moving moving = nativeArray[i];
					Target target = nativeArray6[i];
					Watercraft watercraft = nativeArray7[i];
					WatercraftNavigation navigation = nativeArray8[i];
					WatercraftCurrentLane currentLane = nativeArray3[i];
					Blocker blocker = nativeArray2[i];
					PathOwner pathOwner = nativeArray10[i];
					PrefabRef prefabRefData = nativeArray11[i];
					DynamicBuffer<WatercraftNavigationLane> navigationLanes = bufferAccessor[i];
					DynamicBuffer<PathElement> pathElements = bufferAccessor2[i];
					WatercraftData prefabWatercraftData = m_PrefabWatercraftData[prefabRefData.m_Prefab];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRefData.m_Prefab];
					WatercraftNavigationHelpers.CurrentLaneCache currentLaneCache = new WatercraftNavigationHelpers.CurrentLaneCache(ref currentLane, m_EntityLookup, m_MovingObjectSearchTree);
					int priority = VehicleUtils.GetPriority(prefabWatercraftData);
					Odometer odometer = default(Odometer);
					if (flag)
					{
						odometer = nativeArray9[i];
					}
					UpdateNavigationLanes(ref random, priority, entity, transform, target, watercraft, ref laneSelectBuffer, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements);
					UpdateNavigationTarget(priority, entity, transform, moving, watercraft, pathOwner, prefabRefData, prefabWatercraftData, objectGeometryData, ref navigation, ref currentLane, ref blocker, ref odometer, navigationLanes, pathElements);
					ReserveNavigationLanes(priority, prefabWatercraftData, watercraft, ref navigation, ref currentLane, navigationLanes);
					currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
					nativeArray8[i] = navigation;
					nativeArray3[i] = currentLane;
					nativeArray10[i] = pathOwner;
					nativeArray2[i] = blocker;
					if (flag)
					{
						nativeArray9[i] = odometer;
					}
				}
				laneSelectBuffer.Dispose();
				return;
			}
			for (int j = 0; j < chunk.Count; j++)
			{
				WatercraftCurrentLane value = nativeArray3[j];
				Blocker value2 = nativeArray2[j];
				if ((value.m_LaneFlags & WatercraftLaneFlags.QueueReached) != 0 && (!m_WatercraftData.HasComponent(value2.m_Blocker) || (m_WatercraftData[value2.m_Blocker].m_Flags & WatercraftFlags.Queueing) == 0))
				{
					value.m_LaneFlags &= ~WatercraftLaneFlags.QueueReached;
					value2 = default(Blocker);
				}
				nativeArray3[j] = value;
				nativeArray2[j] = value2;
			}
		}

		private void UpdateNavigationLanes(ref Unity.Mathematics.Random random, int priority, Entity entity, Game.Objects.Transform transform, Target target, Watercraft watercraft, ref WatercraftLaneSelectBuffer laneSelectBuffer, ref WatercraftCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<WatercraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
		{
			int invalidPath = 10000000;
			if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & WatercraftLaneFlags.Obsolete) != 0)
			{
				invalidPath = -1;
				TryFindCurrentLane(ref currentLane, transform);
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 && (pathOwner.m_State & PathFlags.Append) == 0)
			{
				ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
			}
			else if ((pathOwner.m_State & PathFlags.Updated) == 0)
			{
				FillNavigationPaths(ref random, priority, entity, transform, target, watercraft, ref laneSelectBuffer, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements, ref invalidPath);
			}
			if (invalidPath != 10000000)
			{
				ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
				pathElements.Clear();
				pathOwner.m_ElementIndex = 0;
				pathOwner.m_State |= PathFlags.Obsolete;
			}
		}

		private void ClearNavigationLanes(ref WatercraftCurrentLane currentLane, DynamicBuffer<WatercraftNavigationLane> navigationLanes, int invalidPath)
		{
			currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
			if (invalidPath > 0)
			{
				for (int i = 0; i < navigationLanes.Length; i++)
				{
					if ((navigationLanes[i].m_Flags & WatercraftLaneFlags.Reserved) == 0)
					{
						invalidPath = math.min(i, invalidPath);
						break;
					}
				}
			}
			invalidPath = math.max(invalidPath, 0);
			if (invalidPath < navigationLanes.Length)
			{
				navigationLanes.RemoveRange(invalidPath, navigationLanes.Length - invalidPath);
			}
		}

		private void TryFindCurrentLane(ref WatercraftCurrentLane currentLaneData, Game.Objects.Transform transformData)
		{
			currentLaneData.m_LaneFlags &= ~(WatercraftLaneFlags.TransformTarget | WatercraftLaneFlags.Obsolete | WatercraftLaneFlags.Area);
			currentLaneData.m_Lane = Entity.Null;
			currentLaneData.m_ChangeLane = Entity.Null;
			float3 position = transformData.m_Position;
			float num = 100f;
			Bounds3 bounds = new Bounds3(position - num, position + num);
			WatercraftNavigationHelpers.FindLaneIterator iterator = new WatercraftNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = position,
				m_MinDistance = num,
				m_Result = currentLaneData,
				m_CarType = RoadTypes.Watercraft,
				m_SubLanes = m_Lanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_CarLaneData = m_CarLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_MasterLaneData = m_MasterLaneData,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabCarLaneData = m_PrefabCarLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			m_AreaSearchTree.Iterate(ref iterator);
			currentLaneData = iterator.m_Result;
		}

		private void FillNavigationPaths(ref Unity.Mathematics.Random random, int priority, Entity entity, Game.Objects.Transform transform, Target target, Watercraft watercraft, ref WatercraftLaneSelectBuffer laneSelectBuffer, ref WatercraftCurrentLane currentLaneData, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<WatercraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref int invalidPath)
		{
			if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.EndOfPath) == 0)
			{
				for (int i = 0; i < 8; i++)
				{
					if (i >= navigationLanes.Length)
					{
						i = navigationLanes.Length;
						if (pathOwner.m_ElementIndex >= pathElements.Length)
						{
							if ((pathOwner.m_State & PathFlags.Pending) != 0)
							{
								break;
							}
							WatercraftNavigationLane navLaneData = default(WatercraftNavigationLane);
							if (i > 0)
							{
								WatercraftNavigationLane value = navigationLanes[i - 1];
								if ((value.m_Flags & WatercraftLaneFlags.TransformTarget) == 0 && (watercraft.m_Flags & (WatercraftFlags.StayOnWaterway | WatercraftFlags.AnyLaneTarget)) != (WatercraftFlags.StayOnWaterway | WatercraftFlags.AnyLaneTarget) && GetTransformTarget(ref navLaneData.m_Lane, target))
								{
									if ((value.m_Flags & WatercraftLaneFlags.GroupTarget) == 0)
									{
										Entity lane = navLaneData.m_Lane;
										navLaneData.m_Lane = value.m_Lane;
										navLaneData.m_Flags = value.m_Flags & (WatercraftLaneFlags.Connection | WatercraftLaneFlags.Area);
										navLaneData.m_CurvePosition = value.m_CurvePosition.yy;
										float3 position = default(float3);
										if (VehicleUtils.CalculateTransformPosition(ref position, lane, m_TransformData, m_PositionData, m_PrefabRefData, m_PrefabBuildingData))
										{
											UpdateSlaveLane(ref navLaneData, position);
											Align(ref navLaneData, position);
										}
										if ((watercraft.m_Flags & WatercraftFlags.StayOnWaterway) != 0)
										{
											navLaneData.m_Flags |= WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.GroupTarget;
											navigationLanes.Add(navLaneData);
											currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
											break;
										}
										navLaneData.m_Flags |= WatercraftLaneFlags.GroupTarget;
										navigationLanes.Add(navLaneData);
										currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
										continue;
									}
									navLaneData.m_Flags |= WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.TransformTarget;
									navigationLanes.Add(navLaneData);
									currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
									break;
								}
								value.m_Flags |= WatercraftLaneFlags.EndOfPath;
								navigationLanes[i - 1] = value;
								currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
								break;
							}
							if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.TransformTarget) != 0 || (watercraft.m_Flags & WatercraftFlags.StayOnWaterway) != 0 || !GetTransformTarget(ref navLaneData.m_Lane, target))
							{
								currentLaneData.m_LaneFlags |= WatercraftLaneFlags.EndOfPath;
								break;
							}
							navLaneData.m_Flags |= WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.TransformTarget;
							navigationLanes.Add(navLaneData);
							currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
							break;
						}
						PathElement pathElement = pathElements[pathOwner.m_ElementIndex++];
						WatercraftNavigationLane navLaneData2 = new WatercraftNavigationLane
						{
							m_Lane = pathElement.m_Target,
							m_CurvePosition = pathElement.m_TargetDelta
						};
						if (!m_CarLaneData.HasComponent(navLaneData2.m_Lane))
						{
							if (m_ConnectionLaneData.HasComponent(navLaneData2.m_Lane))
							{
								Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[navLaneData2.m_Lane];
								navLaneData2.m_Flags |= WatercraftLaneFlags.FixedLane;
								if ((connectionLane.m_Flags & ConnectionLaneFlags.Area) != 0)
								{
									navLaneData2.m_Flags |= WatercraftLaneFlags.Area;
								}
								else
								{
									navLaneData2.m_Flags |= WatercraftLaneFlags.Connection;
								}
								currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
								navigationLanes.Add(navLaneData2);
								continue;
							}
							if (!m_TransformData.HasComponent(navLaneData2.m_Lane))
							{
								invalidPath = i;
								return;
							}
							if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
							{
								pathOwner.m_ElementIndex--;
								break;
							}
							if (!m_TakeoffLocationData.HasComponent(navLaneData2.m_Lane) && ((watercraft.m_Flags & WatercraftFlags.StayOnWaterway) == 0 || pathElements.Length > pathOwner.m_ElementIndex))
							{
								navLaneData2.m_Flags |= WatercraftLaneFlags.TransformTarget;
								navigationLanes.Add(navLaneData2);
								if (i > 0)
								{
									float3 position2 = m_TransformData[navLaneData2.m_Lane].m_Position;
									WatercraftNavigationLane navLaneData3 = navigationLanes[i - 1];
									UpdateSlaveLane(ref navLaneData3, position2);
									navigationLanes[i - 1] = navLaneData3;
								}
								currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
							}
						}
						else
						{
							navLaneData2.m_Flags |= WatercraftLaneFlags.UpdateOptimalLane;
							currentLaneData.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
							if (i == 0 && (currentLaneData.m_LaneFlags & (WatercraftLaneFlags.FixedLane | WatercraftLaneFlags.Connection)) == WatercraftLaneFlags.FixedLane)
							{
								GetSlaveLaneFromMasterLane(ref random, ref navLaneData2, currentLaneData);
							}
							else
							{
								GetSlaveLaneFromMasterLane(ref random, ref navLaneData2);
							}
							navigationLanes.Add(navLaneData2);
						}
					}
					else
					{
						WatercraftNavigationLane watercraftNavigationLane = navigationLanes[i];
						if (!m_EntityLookup.Exists(watercraftNavigationLane.m_Lane))
						{
							invalidPath = i;
							return;
						}
						if ((watercraftNavigationLane.m_Flags & WatercraftLaneFlags.EndOfPath) != 0)
						{
							break;
						}
					}
				}
			}
			if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.UpdateOptimalLane) == 0)
			{
				return;
			}
			currentLaneData.m_LaneFlags &= ~WatercraftLaneFlags.UpdateOptimalLane;
			if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.IsBlocked) != 0)
			{
				if (IsBlockedLane(currentLaneData.m_Lane, currentLaneData.m_CurvePosition.xz))
				{
					currentLaneData.m_CurvePosition.z = currentLaneData.m_CurvePosition.y;
					invalidPath = -1;
					return;
				}
				for (int j = 0; j < navigationLanes.Length; j++)
				{
					WatercraftNavigationLane watercraftNavigationLane2 = navigationLanes[j];
					if (IsBlockedLane(watercraftNavigationLane2.m_Lane, watercraftNavigationLane2.m_CurvePosition))
					{
						currentLaneData.m_CurvePosition.z = currentLaneData.m_CurvePosition.y;
						invalidPath = j;
						return;
					}
				}
				currentLaneData.m_LaneFlags &= ~(WatercraftLaneFlags.FixedLane | WatercraftLaneFlags.IsBlocked);
				currentLaneData.m_LaneFlags |= WatercraftLaneFlags.IgnoreBlocker;
			}
			WatercraftLaneSelectIterator watercraftLaneSelectIterator = new WatercraftLaneSelectIterator
			{
				m_OwnerData = m_OwnerData,
				m_LaneData = m_LaneData,
				m_SlaveLaneData = m_SlaveLaneData,
				m_LaneReservationData = m_LaneReservationData,
				m_MovingData = m_MovingData,
				m_WatercraftData = m_WatercraftData,
				m_Lanes = m_Lanes,
				m_LaneObjects = m_LaneObjects,
				m_Entity = entity,
				m_Blocker = blocker.m_Blocker,
				m_Priority = priority
			};
			watercraftLaneSelectIterator.SetBuffer(ref laneSelectBuffer);
			if (navigationLanes.Length != 0)
			{
				WatercraftNavigationLane watercraftNavigationLane3 = navigationLanes[navigationLanes.Length - 1];
				watercraftLaneSelectIterator.CalculateLaneCosts(watercraftNavigationLane3, navigationLanes.Length - 1);
				for (int num = navigationLanes.Length - 2; num >= 0; num--)
				{
					WatercraftNavigationLane watercraftNavigationLane4 = navigationLanes[num];
					watercraftLaneSelectIterator.CalculateLaneCosts(watercraftNavigationLane4, watercraftNavigationLane3, num);
					watercraftNavigationLane3 = watercraftNavigationLane4;
				}
				watercraftLaneSelectIterator.UpdateOptimalLane(ref currentLaneData, navigationLanes[0]);
				for (int k = 0; k < navigationLanes.Length; k++)
				{
					WatercraftNavigationLane navLaneData4 = navigationLanes[k];
					watercraftLaneSelectIterator.UpdateOptimalLane(ref navLaneData4);
					navLaneData4.m_Flags &= ~WatercraftLaneFlags.Reserved;
					navigationLanes[k] = navLaneData4;
				}
			}
			else if (currentLaneData.m_CurvePosition.x != currentLaneData.m_CurvePosition.z)
			{
				watercraftLaneSelectIterator.UpdateOptimalLane(ref currentLaneData, default(WatercraftNavigationLane));
			}
		}

		private bool IsBlockedLane(Entity lane, float2 range)
		{
			if (m_SlaveLaneData.HasComponent(lane))
			{
				SlaveLane slaveLane = m_SlaveLaneData[lane];
				Entity owner = m_OwnerData[lane].m_Owner;
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[owner];
				int num = slaveLane.m_MinIndex - 1;
				if (num < 0 || num > dynamicBuffer.Length)
				{
					return false;
				}
				lane = dynamicBuffer[num].m_SubLane;
				if (!m_MasterLaneData.HasComponent(lane))
				{
					return false;
				}
			}
			if (!m_CarLaneData.HasComponent(lane))
			{
				return false;
			}
			Game.Net.CarLane carLane = m_CarLaneData[lane];
			if (carLane.m_BlockageEnd < carLane.m_BlockageStart)
			{
				return false;
			}
			if (math.min(range.x, range.y) <= (float)(int)carLane.m_BlockageEnd * 0.003921569f)
			{
				return math.max(range.x, range.y) >= (float)(int)carLane.m_BlockageStart * 0.003921569f;
			}
			return false;
		}

		private bool GetTransformTarget(ref Entity entity, Target target)
		{
			if (m_PropertyRenterData.HasComponent(target.m_Target))
			{
				target.m_Target = m_PropertyRenterData[target.m_Target].m_Property;
			}
			if (m_TransformData.HasComponent(target.m_Target))
			{
				entity = target.m_Target;
				return true;
			}
			if (m_PositionData.HasComponent(target.m_Target))
			{
				entity = target.m_Target;
				return true;
			}
			return false;
		}

		private void UpdateSlaveLane(ref WatercraftNavigationLane navLaneData, float3 targetPosition)
		{
			if (m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
			{
				SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
				Entity owner = m_OwnerData[navLaneData.m_Lane].m_Owner;
				DynamicBuffer<Game.Net.SubLane> lanes = m_Lanes[owner];
				int index = NetUtils.ChooseClosestLane(slaveLane.m_MinIndex, slaveLane.m_MaxIndex, targetPosition, PathMethod.Road, lanes, ref m_CurveData, navLaneData.m_CurvePosition.y);
				navLaneData.m_Lane = lanes[index].m_SubLane;
			}
			navLaneData.m_Flags |= WatercraftLaneFlags.FixedLane;
		}

		private void Align(ref WatercraftNavigationLane navLaneData, float3 targetPosition)
		{
			if (m_CurveData.HasComponent(navLaneData.m_Lane))
			{
				Curve curve = m_CurveData[navLaneData.m_Lane];
				float3 @float = MathUtils.Position(curve.m_Bezier, navLaneData.m_CurvePosition.y);
				if (math.dot(MathUtils.Right(MathUtils.Tangent(curve.m_Bezier, navLaneData.m_CurvePosition.y).xz), targetPosition.xz - @float.xz) > 0f)
				{
					navLaneData.m_Flags |= WatercraftLaneFlags.AlignRight;
				}
				else
				{
					navLaneData.m_Flags |= WatercraftLaneFlags.AlignLeft;
				}
			}
		}

		private void GetSlaveLaneFromMasterLane(ref Unity.Mathematics.Random random, ref WatercraftNavigationLane navLaneData, WatercraftCurrentLane currentLaneData)
		{
			if (m_MasterLaneData.HasComponent(navLaneData.m_Lane))
			{
				MasterLane masterLane = m_MasterLaneData[navLaneData.m_Lane];
				Owner owner = m_OwnerData[navLaneData.m_Lane];
				DynamicBuffer<Game.Net.SubLane> lanes = m_Lanes[owner.m_Owner];
				if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.TransformTarget) != 0)
				{
					float3 position = default(float3);
					if (VehicleUtils.CalculateTransformPosition(ref position, currentLaneData.m_Lane, m_TransformData, m_PositionData, m_PrefabRefData, m_PrefabBuildingData))
					{
						int index = NetUtils.ChooseClosestLane(masterLane.m_MinIndex, masterLane.m_MaxIndex, position, PathMethod.Road, lanes, ref m_CurveData, navLaneData.m_CurvePosition.y);
						navLaneData.m_Lane = lanes[index].m_SubLane;
						navLaneData.m_Flags |= WatercraftLaneFlags.FixedStart;
					}
					else
					{
						int index2 = random.NextInt(masterLane.m_MinIndex, masterLane.m_MaxIndex + 1);
						navLaneData.m_Lane = lanes[index2].m_SubLane;
					}
				}
				else
				{
					float3 comparePosition = MathUtils.Position(m_CurveData[currentLaneData.m_Lane].m_Bezier, currentLaneData.m_CurvePosition.z);
					int index3 = NetUtils.ChooseClosestLane(masterLane.m_MinIndex, masterLane.m_MaxIndex, comparePosition, PathMethod.Road, lanes, ref m_CurveData, navLaneData.m_CurvePosition.x);
					navLaneData.m_Lane = lanes[index3].m_SubLane;
					navLaneData.m_Flags |= WatercraftLaneFlags.FixedStart;
				}
			}
			else
			{
				navLaneData.m_Flags |= WatercraftLaneFlags.FixedLane;
			}
		}

		private void GetSlaveLaneFromMasterLane(ref Unity.Mathematics.Random random, ref WatercraftNavigationLane navLaneData)
		{
			if (m_MasterLaneData.HasComponent(navLaneData.m_Lane))
			{
				MasterLane masterLane = m_MasterLaneData[navLaneData.m_Lane];
				Entity owner = m_OwnerData[navLaneData.m_Lane].m_Owner;
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[owner];
				int index = random.NextInt(masterLane.m_MinIndex, masterLane.m_MaxIndex + 1);
				navLaneData.m_Lane = dynamicBuffer[index].m_SubLane;
			}
			else
			{
				navLaneData.m_Flags |= WatercraftLaneFlags.FixedLane;
			}
		}

		private void CheckBlocker(Watercraft watercraftData, ref WatercraftCurrentLane currentLane, ref Blocker blocker, ref WatercraftLaneSpeedIterator laneIterator)
		{
			if (laneIterator.m_Blocker != blocker.m_Blocker)
			{
				currentLane.m_LaneFlags &= ~(WatercraftLaneFlags.IgnoreBlocker | WatercraftLaneFlags.QueueReached);
			}
			if (laneIterator.m_Blocker != Entity.Null)
			{
				if (!m_MovingData.HasComponent(laneIterator.m_Blocker))
				{
					if (m_WatercraftData.HasComponent(laneIterator.m_Blocker))
					{
						if ((m_WatercraftData[laneIterator.m_Blocker].m_Flags & WatercraftFlags.Queueing) != 0 && (currentLane.m_LaneFlags & WatercraftLaneFlags.Queue) != 0)
						{
							if (laneIterator.m_MaxSpeed < 1f)
							{
								currentLane.m_LaneFlags |= WatercraftLaneFlags.QueueReached;
							}
						}
						else
						{
							currentLane.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
							if (laneIterator.m_MaxSpeed < 1f)
							{
								currentLane.m_LaneFlags |= WatercraftLaneFlags.IsBlocked;
							}
						}
					}
					else
					{
						currentLane.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
						if (laneIterator.m_MaxSpeed < 1f)
						{
							currentLane.m_LaneFlags |= WatercraftLaneFlags.IsBlocked;
						}
					}
				}
				else if (laneIterator.m_Blocker != blocker.m_Blocker)
				{
					currentLane.m_LaneFlags |= WatercraftLaneFlags.UpdateOptimalLane;
				}
			}
			blocker.m_Blocker = laneIterator.m_Blocker;
			blocker.m_Type = laneIterator.m_BlockerType;
			blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(laneIterator.m_MaxSpeed * 4.5899997f), 0, 255);
		}

		private void UpdateNavigationTarget(int priority, Entity entity, Game.Objects.Transform transform, Moving moving, Watercraft watercraft, PathOwner pathOwner, PrefabRef prefabRefData, WatercraftData prefabWatercraftData, ObjectGeometryData prefabObjectGeometryData, ref WatercraftNavigation navigation, ref WatercraftCurrentLane currentLane, ref Blocker blocker, ref Odometer odometer, DynamicBuffer<WatercraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
		{
			float num = 4f / 15f;
			float num2 = math.length(moving.m_Velocity.xz);
			float speedLimitFactor = 1f;
			if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Connection) != 0)
			{
				prefabWatercraftData.m_MaxSpeed = 277.77777f;
				prefabWatercraftData.m_Acceleration = 277.77777f;
				prefabWatercraftData.m_Braking = 277.77777f;
			}
			else
			{
				num2 = math.min(num2, prefabWatercraftData.m_MaxSpeed);
			}
			Bounds1 speedRange = (((currentLane.m_LaneFlags & (WatercraftLaneFlags.ResetSpeed | WatercraftLaneFlags.Connection)) == 0) ? VehicleUtils.CalculateSpeedRange(prefabWatercraftData, num2, num) : new Bounds1(0f, prefabWatercraftData.m_MaxSpeed));
			VehicleUtils.CalculateShipNavigationPivots(transform, prefabObjectGeometryData, out var pivot, out var pivot2);
			float num3 = math.distance(pivot.xz, pivot2.xz);
			float3 position = transform.m_Position;
			if ((currentLane.m_LaneFlags & (WatercraftLaneFlags.TransformTarget | WatercraftLaneFlags.Area)) == 0 && m_CurveData.TryGetComponent(currentLane.m_Lane, out var componentData))
			{
				PrefabRef prefabRef = m_PrefabRefData[currentLane.m_Lane];
				NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
				float2 value = MathUtils.Tangent(componentData.m_Bezier, currentLane.m_CurvePosition.x).xz;
				if (MathUtils.TryNormalize(ref value))
				{
					position.xz -= MathUtils.Right(value) * ((netLaneData.m_Width - prefabObjectGeometryData.m_Size.x) * currentLane.m_LanePosition * 0.5f);
				}
			}
			WatercraftLaneSpeedIterator laneIterator = new WatercraftLaneSpeedIterator
			{
				m_TransformData = m_TransformData,
				m_MovingData = m_MovingData,
				m_WatercraftData = m_WatercraftData,
				m_LaneReservationData = m_LaneReservationData,
				m_LaneSignalData = m_LaneSignalData,
				m_CurveData = m_CurveData,
				m_CarLaneData = m_CarLaneData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
				m_PrefabWatercraftData = m_PrefabWatercraftData,
				m_LaneOverlapData = m_LaneOverlaps,
				m_LaneObjectData = m_LaneObjects,
				m_Entity = entity,
				m_Ignore = (((currentLane.m_LaneFlags & WatercraftLaneFlags.IgnoreBlocker) != 0) ? blocker.m_Blocker : Entity.Null),
				m_Priority = priority,
				m_TimeStep = num,
				m_SafeTimeStep = num + 0.5f,
				m_SpeedLimitFactor = speedLimitFactor,
				m_CurrentSpeed = num2,
				m_PrefabWatercraft = prefabWatercraftData,
				m_PrefabObjectGeometry = prefabObjectGeometryData,
				m_SpeedRange = speedRange,
				m_MaxSpeed = speedRange.max,
				m_CanChangeLane = 1f,
				m_CurrentPosition = position
			};
			if ((currentLane.m_LaneFlags & WatercraftLaneFlags.TransformTarget) != 0)
			{
				laneIterator.IterateTarget(navigation.m_TargetPosition);
				navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
				blocker.m_Blocker = Entity.Null;
				blocker.m_Type = BlockerType.None;
				blocker.m_MaxSpeed = byte.MaxValue;
			}
			else
			{
				if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Area) == 0)
				{
					if (currentLane.m_Lane == Entity.Null)
					{
						navigation.m_MaxSpeed = math.max(0f, num2 - prefabWatercraftData.m_Braking * num);
						blocker.m_Blocker = Entity.Null;
						blocker.m_Type = BlockerType.None;
						blocker.m_MaxSpeed = byte.MaxValue;
						return;
					}
					if (currentLane.m_ChangeLane != Entity.Null)
					{
						if (!laneIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_ChangeLane, currentLane.m_CurvePosition, currentLane.m_ChangeProgress))
						{
							goto IL_03fd;
						}
					}
					else if (!laneIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_CurvePosition))
					{
						goto IL_03fd;
					}
					goto IL_05be;
				}
				laneIterator.IterateTarget(navigation.m_TargetPosition, 11.111112f);
				navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
				blocker.m_Blocker = Entity.Null;
				blocker.m_Type = BlockerType.None;
				blocker.m_MaxSpeed = byte.MaxValue;
			}
			goto IL_05da;
			IL_05be:
			navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
			CheckBlocker(watercraft, ref currentLane, ref blocker, ref laneIterator);
			goto IL_05da;
			IL_05da:
			float num4 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
			float num5 = navigation.m_MaxSpeed * num + 1f + num3 * 0.5f;
			float num6 = num5;
			if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Area) != 0)
			{
				float brakingDistance = VehicleUtils.GetBrakingDistance(prefabWatercraftData, navigation.m_MaxSpeed, num);
				num6 = math.max(num5, brakingDistance + 1f + num3 * 0.5f);
				num4 = math.select(num4, 0f, currentLane.m_ChangeProgress != 0f);
			}
			if (currentLane.m_ChangeLane != Entity.Null)
			{
				float num7 = 0.05f;
				float num8 = 1f + prefabObjectGeometryData.m_Size.z * num7 * 0.5f;
				float2 x = new float2(0.4f, 0.6f * math.saturate(num2 * num7));
				x *= laneIterator.m_CanChangeLane * num;
				x.x = math.min(x.x, math.max(0f, 1f - currentLane.m_ChangeProgress));
				currentLane.m_ChangeProgress = math.min(num8, currentLane.m_ChangeProgress + math.csum(x));
				if (currentLane.m_ChangeProgress == num8)
				{
					ApplySideEffects(ref currentLane, speedLimitFactor, prefabRefData, prefabWatercraftData);
					currentLane.m_Lane = currentLane.m_ChangeLane;
					currentLane.m_ChangeLane = Entity.Null;
				}
			}
			currentLane.m_Duration += num;
			currentLane.m_Distance += num2 * num;
			odometer.m_Distance += num2 * num;
			if (num4 < num6)
			{
				while (true)
				{
					if ((currentLane.m_LaneFlags & WatercraftLaneFlags.TransformTarget) != 0)
					{
						navigation.m_TargetDirection = default(float3);
						if (MoveTarget(position, ref navigation.m_TargetPosition, num5, currentLane.m_Lane))
						{
							break;
						}
					}
					else if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Area) != 0)
					{
						navigation.m_TargetDirection = default(float3);
						currentLane.m_LanePosition = math.clamp(currentLane.m_LanePosition, -0.5f, 0.5f);
						float navigationSize = VehicleUtils.GetNavigationSize(prefabObjectGeometryData);
						bool num9 = MoveAreaTarget(transform.m_Position, pathOwner, navigationLanes, pathElements, ref navigation.m_TargetPosition, num6, currentLane.m_Lane, ref currentLane.m_CurvePosition, currentLane.m_LanePosition, navigationSize);
						currentLane.m_ChangeProgress = 0f;
						if (num9)
						{
							break;
						}
					}
					else
					{
						navigation.m_TargetDirection = default(float3);
						if (currentLane.m_ChangeLane != Entity.Null)
						{
							Curve curve = m_CurveData[currentLane.m_Lane];
							Curve curve2 = m_CurveData[currentLane.m_ChangeLane];
							if (MoveTarget(position, ref navigation.m_TargetPosition, num5, curve.m_Bezier, curve2.m_Bezier, currentLane.m_ChangeProgress, ref currentLane.m_CurvePosition))
							{
								ApplyLanePosition(ref navigation.m_TargetPosition, ref currentLane, prefabObjectGeometryData);
								break;
							}
						}
						else
						{
							Curve curve3 = m_CurveData[currentLane.m_Lane];
							if (MoveTarget(position, ref navigation.m_TargetPosition, num5, curve3.m_Bezier, ref currentLane.m_CurvePosition))
							{
								ApplyLanePosition(ref navigation.m_TargetPosition, ref currentLane, prefabObjectGeometryData);
								break;
							}
						}
					}
					if (navigationLanes.Length == 0)
					{
						if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Area) == 0 && m_CurveData.HasComponent(currentLane.m_Lane))
						{
							navigation.m_TargetDirection = MathUtils.Tangent(m_CurveData[currentLane.m_Lane].m_Bezier, currentLane.m_CurvePosition.z);
							ApplyLanePosition(ref navigation.m_TargetPosition, ref currentLane, prefabObjectGeometryData);
						}
						num4 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
						if (num4 < 1f && num2 < 0.1f)
						{
							currentLane.m_LaneFlags |= WatercraftLaneFlags.EndReached;
						}
						break;
					}
					WatercraftNavigationLane watercraftNavigationLane = navigationLanes[0];
					if (!m_EntityLookup.Exists(watercraftNavigationLane.m_Lane))
					{
						break;
					}
					if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Connection) != 0)
					{
						if ((watercraftNavigationLane.m_Flags & WatercraftLaneFlags.TransformTarget) != 0)
						{
							watercraftNavigationLane.m_Flags |= WatercraftLaneFlags.ResetSpeed;
						}
						else if ((watercraftNavigationLane.m_Flags & WatercraftLaneFlags.Connection) == 0)
						{
							num4 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
							if (num4 >= 1f || num2 > 3f)
							{
								break;
							}
							watercraftNavigationLane.m_Flags |= WatercraftLaneFlags.ResetSpeed;
						}
					}
					ApplySideEffects(ref currentLane, speedLimitFactor, prefabRefData, prefabWatercraftData);
					currentLane.m_Lane = watercraftNavigationLane.m_Lane;
					currentLane.m_ChangeLane = Entity.Null;
					currentLane.m_ChangeProgress = 0f;
					currentLane.m_CurvePosition = watercraftNavigationLane.m_CurvePosition.xxy;
					currentLane.m_LaneFlags = watercraftNavigationLane.m_Flags;
					if ((currentLane.m_LaneFlags & (WatercraftLaneFlags.AlignLeft | WatercraftLaneFlags.AlignRight)) != 0)
					{
						currentLane.m_LanePosition = math.select(-1f, 1f, (currentLane.m_LaneFlags & WatercraftLaneFlags.AlignRight) != 0);
					}
					else
					{
						currentLane.m_LanePosition = 0f;
					}
					navigationLanes.RemoveAt(0);
				}
			}
			if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Area) != 0)
			{
				VehicleCollisionIterator vehicleCollisionIterator = new VehicleCollisionIterator
				{
					m_OwnerData = m_OwnerData,
					m_TransformData = m_TransformData,
					m_MovingData = m_MovingData,
					m_ControllerData = m_ControllerData,
					m_CreatureData = m_CreatureData,
					m_CurveData = m_CurveData,
					m_AreaLaneData = m_AreaLaneData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabLaneData = m_PrefabNetLaneData,
					m_AreaNodes = m_AreaNodes,
					m_StaticObjectSearchTree = m_StaticObjectSearchTree,
					m_MovingObjectSearchTree = m_MovingObjectSearchTree,
					m_Entity = entity,
					m_CurrentLane = currentLane.m_Lane,
					m_CurvePosition = currentLane.m_CurvePosition.z,
					m_TimeStep = num,
					m_PrefabObjectGeometry = prefabObjectGeometryData,
					m_SpeedRange = speedRange,
					m_CurrentPosition = transform.m_Position,
					m_CurrentVelocity = moving.m_Velocity,
					m_MinDistance = num6,
					m_TargetPosition = navigation.m_TargetPosition,
					m_MaxSpeed = navigation.m_MaxSpeed,
					m_LanePosition = currentLane.m_LanePosition,
					m_Blocker = blocker.m_Blocker,
					m_BlockerType = blocker.m_Type
				};
				if (vehicleCollisionIterator.m_MaxSpeed != 0f)
				{
					vehicleCollisionIterator.IterateFirstLane(currentLane.m_Lane);
					vehicleCollisionIterator.m_MaxSpeed = math.select(vehicleCollisionIterator.m_MaxSpeed, 0f, vehicleCollisionIterator.m_MaxSpeed < 0.1f);
					if (!navigation.m_TargetPosition.Equals(vehicleCollisionIterator.m_TargetPosition))
					{
						navigation.m_TargetPosition = vehicleCollisionIterator.m_TargetPosition;
						currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, vehicleCollisionIterator.m_LanePosition, 0.1f);
						currentLane.m_ChangeProgress = 1f;
					}
					navigation.m_MaxSpeed = vehicleCollisionIterator.m_MaxSpeed;
					blocker.m_Blocker = vehicleCollisionIterator.m_Blocker;
					blocker.m_Type = vehicleCollisionIterator.m_BlockerType;
					blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(vehicleCollisionIterator.m_MaxSpeed * 4.5899997f), 0, 255);
				}
			}
			navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, math.distance(transform.m_Position.xz, navigation.m_TargetPosition.xz) / num);
			return;
			IL_03fd:
			int num10 = 0;
			while (true)
			{
				if (num10 < navigationLanes.Length)
				{
					WatercraftNavigationLane value2 = navigationLanes[num10];
					if ((value2.m_Flags & (WatercraftLaneFlags.TransformTarget | WatercraftLaneFlags.Area)) == 0)
					{
						if ((value2.m_Flags & WatercraftLaneFlags.Connection) != 0)
						{
							laneIterator.m_PrefabWatercraft.m_MaxSpeed = 277.77777f;
							laneIterator.m_PrefabWatercraft.m_Acceleration = 277.77777f;
							laneIterator.m_PrefabWatercraft.m_Braking = 277.77777f;
							laneIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
						}
						else if ((currentLane.m_LaneFlags & WatercraftLaneFlags.Connection) != 0)
						{
							goto IL_05b0;
						}
						bool test = (value2.m_Lane == currentLane.m_Lane) | (value2.m_Lane == currentLane.m_ChangeLane);
						bool ignoreSignal = (value2.m_Flags & WatercraftLaneFlags.IgnoreSignal) != 0;
						float minOffset = math.select(-1f, currentLane.m_CurvePosition.y, test);
						bool needSignal;
						bool num11 = laneIterator.IterateNextLane(value2.m_Lane, value2.m_CurvePosition, minOffset, ignoreSignal, out needSignal);
						if (needSignal)
						{
							if ((value2.m_Flags & WatercraftLaneFlags.NeedSignal) == 0)
							{
								value2.m_Flags |= (WatercraftLaneFlags)(CheckNeedSignal(prefabObjectGeometryData, value2.m_Lane) ? 512 : 1024);
								navigationLanes[num10] = value2;
							}
							if ((value2.m_Flags & WatercraftLaneFlags.NeedSignal) != 0)
							{
								m_LaneSignals.Enqueue(new WatercraftNavigationHelpers.LaneSignal(entity, value2.m_Lane, priority));
							}
						}
						if (num11)
						{
							break;
						}
						num10++;
						continue;
					}
					VehicleUtils.CalculateTransformPosition(ref laneIterator.m_CurrentPosition, value2.m_Lane, m_TransformData, m_PositionData, m_PrefabRefData, m_PrefabBuildingData);
				}
				goto IL_05b0;
				IL_05b0:
				laneIterator.IterateTarget(laneIterator.m_CurrentPosition);
				break;
			}
			goto IL_05be;
		}

		private bool CheckNeedSignal(ObjectGeometryData prefabObjectGeometryData, Entity lane)
		{
			if (m_OwnerData.TryGetComponent(lane, out var componentData) && m_NodeData.TryGetComponent(componentData.m_Owner, out var componentData2) && m_PrefabRefData.TryGetComponent(componentData.m_Owner, out var componentData3) && m_PrefabNetGeometryData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
			{
				return WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, componentData2.m_Position) + prefabObjectGeometryData.m_Bounds.max.y > componentData2.m_Position.y + componentData4.m_ElevatedHeightRange.min;
			}
			return true;
		}

		private void ApplyLanePosition(ref float3 targetPosition, ref WatercraftCurrentLane currentLaneData, ObjectGeometryData prefabObjectGeometryData)
		{
			if (m_CurveData.HasComponent(currentLaneData.m_Lane))
			{
				Curve curve = m_CurveData[currentLaneData.m_Lane];
				PrefabRef prefabRef = m_PrefabRefData[currentLaneData.m_Lane];
				NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
				float2 value = MathUtils.Tangent(curve.m_Bezier, currentLaneData.m_CurvePosition.x).xz;
				if (MathUtils.TryNormalize(ref value))
				{
					targetPosition.xz += MathUtils.Right(value) * ((netLaneData.m_Width - prefabObjectGeometryData.m_Size.x) * currentLaneData.m_LanePosition * 0.5f);
				}
			}
		}

		private void ApplySideEffects(ref WatercraftCurrentLane currentLaneData, float speedLimitFactor, PrefabRef prefabRefData, WatercraftData prefabWatercraftData)
		{
			if (m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				Game.Net.CarLane carLaneData = m_CarLaneData[currentLaneData.m_Lane];
				carLaneData.m_SpeedLimit *= speedLimitFactor;
				float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabWatercraftData, carLaneData);
				float num = math.select(currentLaneData.m_Distance / currentLaneData.m_Duration, maxDriveSpeed, currentLaneData.m_Duration == 0f);
				float relativeSpeed = num / maxDriveSpeed;
				float3 sideEffects = default(float3);
				if (m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab))
				{
					VehicleSideEffectData vehicleSideEffectData = m_PrefabSideEffectData[prefabRefData.m_Prefab];
					float num2 = num / prefabWatercraftData.m_MaxSpeed;
					num2 = math.saturate(num2 * num2);
					sideEffects = math.lerp(vehicleSideEffectData.m_Min, vehicleSideEffectData.m_Max, num2);
					sideEffects *= new float3(currentLaneData.m_Distance, currentLaneData.m_Duration, currentLaneData.m_Duration);
				}
				m_LaneEffects.Enqueue(new WatercraftNavigationHelpers.LaneEffects(currentLaneData.m_Lane, sideEffects, relativeSpeed));
			}
			currentLaneData.m_Duration = 0f;
			currentLaneData.m_Distance = 0f;
		}

		private void ReserveNavigationLanes(int priority, WatercraftData prefabWatercraftData, Watercraft watercraftData, ref WatercraftNavigation navigationData, ref WatercraftCurrentLane currentLaneData, DynamicBuffer<WatercraftNavigationLane> navigationLanes)
		{
			float timeStep = 4f / 15f;
			if (!m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				return;
			}
			Curve curve = m_CurveData[currentLaneData.m_Lane];
			float num = math.max(0f, VehicleUtils.GetBrakingDistance(prefabWatercraftData, navigationData.m_MaxSpeed, timeStep) - 0.01f);
			currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.x + num / math.max(1E-06f, curve.m_Length);
			num -= curve.m_Length * math.abs(currentLaneData.m_CurvePosition.z - currentLaneData.m_CurvePosition.x);
			int num2 = 0;
			if (!(currentLaneData.m_CurvePosition.y > currentLaneData.m_CurvePosition.z))
			{
				return;
			}
			currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.z;
			while (num2 < navigationLanes.Length && num > 0f)
			{
				WatercraftNavigationLane value = navigationLanes[num2];
				if (m_CarLaneData.HasComponent(value.m_Lane))
				{
					curve = m_CurveData[value.m_Lane];
					float offset = math.min(value.m_CurvePosition.y, value.m_CurvePosition.x + num / math.max(1E-06f, curve.m_Length));
					if (m_LaneReservationData.HasComponent(value.m_Lane))
					{
						m_LaneReservations.Enqueue(new WatercraftNavigationHelpers.LaneReservation(value.m_Lane, offset, priority));
					}
					num -= curve.m_Length * math.abs(value.m_CurvePosition.y - value.m_CurvePosition.x);
					value.m_Flags |= WatercraftLaneFlags.Reserved;
					navigationLanes[num2++] = value;
					continue;
				}
				break;
			}
		}

		private void ReserveOtherLanesInGroup(Entity lane, int priority)
		{
			if (!m_SlaveLaneData.HasComponent(lane))
			{
				return;
			}
			SlaveLane slaveLane = m_SlaveLaneData[lane];
			Owner owner = m_OwnerData[lane];
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
			int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
			for (int i = slaveLane.m_MinIndex; i <= num; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (subLane != lane && m_LaneReservationData.HasComponent(subLane))
				{
					m_LaneReservations.Enqueue(new WatercraftNavigationHelpers.LaneReservation(subLane, 0f, priority));
				}
			}
		}

		private bool MoveAreaTarget(float3 comparePosition, PathOwner pathOwner, DynamicBuffer<WatercraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, float minDistance, Entity target, ref float3 curveDelta, float lanePosition, float navigationSize)
		{
			if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete | PathFlags.Updated)) != 0)
			{
				return true;
			}
			Entity owner = m_OwnerData[target].m_Owner;
			AreaLane areaLane = m_AreaLaneData[target];
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[owner];
			int num = math.min(pathOwner.m_ElementIndex, pathElements.Length);
			NativeArray<PathElement> subArray = pathElements.AsNativeArray().GetSubArray(num, pathElements.Length - num);
			num = 0;
			bool flag = curveDelta.z < curveDelta.x;
			float lanePosition2 = math.select(lanePosition, 0f - lanePosition, flag);
			if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
			{
				float3 position = nodes[areaLane.m_Nodes.x].m_Position;
				float3 position2 = nodes[areaLane.m_Nodes.y].m_Position;
				float3 position3 = nodes[areaLane.m_Nodes.w].m_Position;
				if (VehicleUtils.SetTriangleTarget(position, position2, position3, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, isSingle: true, m_TransformData, m_AreaLaneData, m_CurveData))
				{
					return true;
				}
				curveDelta.y = curveDelta.z;
			}
			else
			{
				bool4 @bool = new bool4(curveDelta.yz < 0.5f, curveDelta.yz > 0.5f);
				int2 @int = math.select(areaLane.m_Nodes.x, areaLane.m_Nodes.w, @bool.zw);
				float3 position4 = nodes[@int.x].m_Position;
				float3 position5 = nodes[areaLane.m_Nodes.y].m_Position;
				float3 position6 = nodes[areaLane.m_Nodes.z].m_Position;
				float3 position7 = nodes[@int.y].m_Position;
				if (math.any(@bool.xy & @bool.wz))
				{
					if (VehicleUtils.SetAreaTarget(position4, position4, position5, position6, position7, owner, nodes, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, flag, m_TransformData, m_AreaLaneData, m_CurveData, m_OwnerData))
					{
						return true;
					}
					curveDelta.y = 0.5f;
					@bool.xz = false;
				}
				if (VehicleUtils.GetPathElement(num, navigationLanes, subArray, out var pathElement) && m_OwnerData.TryGetComponent(pathElement.m_Target, out var componentData) && componentData.m_Owner == owner)
				{
					bool4 bool2 = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
					if (math.any(!@bool.xz) & math.any(@bool.yw) & math.any(bool2.xy & bool2.wz))
					{
						AreaLane areaLane2 = m_AreaLaneData[pathElement.m_Target];
						@int = math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, bool2.zw);
						position4 = nodes[@int.x].m_Position;
						float3 prev = math.select(position5, position6, position4.Equals(position5));
						position5 = nodes[areaLane2.m_Nodes.y].m_Position;
						position6 = nodes[areaLane2.m_Nodes.z].m_Position;
						position7 = nodes[@int.y].m_Position;
						bool flag2 = pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x;
						if (VehicleUtils.SetAreaTarget(lanePosition: math.select(lanePosition, 0f - lanePosition, flag2), prev2: prev, prev: position4, left: position5, right: position6, next: position7, areaEntity: owner, nodes: nodes, comparePosition: comparePosition, elementIndex: num + 1, navigationLanes: navigationLanes, pathElements: subArray, targetPosition: ref targetPosition, minDistance: minDistance, curveDelta: pathElement.m_TargetDelta.y, navigationSize: navigationSize, isBackward: flag2, transforms: m_TransformData, areaLanes: m_AreaLaneData, curves: m_CurveData, owners: m_OwnerData))
						{
							return true;
						}
					}
					curveDelta.y = curveDelta.z;
					return false;
				}
				if (VehicleUtils.SetTriangleTarget(position5, position6, position7, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, isSingle: false, m_TransformData, m_AreaLaneData, m_CurveData))
				{
					return true;
				}
				curveDelta.y = curveDelta.z;
			}
			return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
		}

		private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Entity target)
		{
			if (VehicleUtils.CalculateTransformPosition(ref targetPosition, target, m_TransformData, m_PositionData, m_PrefabRefData, m_PrefabBuildingData))
			{
				return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
			}
			return false;
		}

		private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Bezier4x3 curve, ref float3 curveDelta)
		{
			float3 @float = MathUtils.Position(curve, curveDelta.z);
			if (math.distance(comparePosition.xz, @float.xz) < minDistance)
			{
				curveDelta.x = curveDelta.z;
				targetPosition = @float;
				return false;
			}
			float2 xz = curveDelta.xz;
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(xz.x, xz.y, 0.5f);
				float3 float2 = MathUtils.Position(curve, num);
				if (math.distance(comparePosition.xz, float2.xz) < minDistance)
				{
					xz.x = num;
				}
				else
				{
					xz.y = num;
				}
			}
			curveDelta.x = xz.y;
			targetPosition = MathUtils.Position(curve, xz.y);
			return true;
		}

		private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Bezier4x3 curve1, Bezier4x3 curve2, float curveSelect, ref float3 curveDelta)
		{
			curveSelect = math.saturate(curveSelect);
			float3 start = MathUtils.Position(curve1, curveDelta.z);
			float3 end = MathUtils.Position(curve2, curveDelta.z);
			float3 @float = math.lerp(start, end, curveSelect);
			if (math.distance(comparePosition.xz, @float.xz) < minDistance)
			{
				curveDelta.x = curveDelta.z;
				targetPosition = @float;
				return false;
			}
			float2 xz = curveDelta.xz;
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(xz.x, xz.y, 0.5f);
				float3 start2 = MathUtils.Position(curve1, num);
				float3 end2 = MathUtils.Position(curve2, num);
				float3 float2 = math.lerp(start2, end2, curveSelect);
				if (math.distance(comparePosition.xz, float2.xz) < minDistance)
				{
					xz.x = num;
				}
				else
				{
					xz.y = num;
				}
			}
			curveDelta.x = xz.y;
			float3 start3 = MathUtils.Position(curve1, xz.y);
			float3 end3 = MathUtils.Position(curve2, xz.y);
			targetPosition = math.lerp(start3, end3, curveSelect);
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct GroupLaneReservationsJob : IJob
	{
		public NativeQueue<WatercraftNavigationHelpers.LaneReservation> m_LaneReservationQueue;

		public NativeList<WatercraftNavigationHelpers.LaneReservation> m_LaneReservationList;

		public NativeList<int2> m_LaneReservationGroups;

		public void Execute()
		{
			WatercraftNavigationHelpers.LaneReservation item;
			while (m_LaneReservationQueue.TryDequeue(out item))
			{
				m_LaneReservationList.Add(in item);
			}
			m_LaneReservationList.Sort();
			Entity entity = Entity.Null;
			int x = 0;
			for (int i = 0; i < m_LaneReservationList.Length; i++)
			{
				item = m_LaneReservationList[i];
				if (entity != item.m_Lane)
				{
					if (entity != Entity.Null)
					{
						m_LaneReservationGroups.Add(new int2(x, i));
					}
					entity = item.m_Lane;
					x = i;
				}
			}
			if (entity != Entity.Null)
			{
				m_LaneReservationGroups.Add(new int2(x, m_LaneReservationList.Length));
			}
		}
	}

	[BurstCompile]
	private struct UpdateLaneSignalsJob : IJob
	{
		public NativeQueue<WatercraftNavigationHelpers.LaneSignal> m_LaneSignalQueue;

		public ComponentLookup<LaneSignal> m_LaneSignalData;

		public void Execute()
		{
			WatercraftNavigationHelpers.LaneSignal item;
			while (m_LaneSignalQueue.TryDequeue(out item))
			{
				LaneSignal value = m_LaneSignalData[item.m_Lane];
				if (item.m_Priority > value.m_Priority)
				{
					value.m_Petitioner = item.m_Petitioner;
					value.m_Priority = item.m_Priority;
					m_LaneSignalData[item.m_Lane] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateLaneReservationsJob : IJob
	{
		public NativeQueue<WatercraftNavigationHelpers.LaneReservation> m_LaneReservationQueue;

		public ComponentLookup<LaneReservation> m_LaneReservationData;

		public void Execute()
		{
			WatercraftNavigationHelpers.LaneReservation item;
			while (m_LaneReservationQueue.TryDequeue(out item))
			{
				ref LaneReservation valueRW = ref m_LaneReservationData.GetRefRW(item.m_Lane).ValueRW;
				if (item.m_Offset > valueRW.m_Next.m_Offset)
				{
					valueRW.m_Next.m_Offset = item.m_Offset;
				}
				if (item.m_Priority > valueRW.m_Next.m_Priority)
				{
					if (item.m_Priority >= valueRW.m_Prev.m_Priority)
					{
						valueRW.m_Blocker = Entity.Null;
					}
					valueRW.m_Next.m_Priority = item.m_Priority;
				}
			}
		}
	}

	[BurstCompile]
	private struct ApplyLaneEffectsJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		public ComponentLookup<Game.Net.Pollution> m_PollutionData;

		public NativeQueue<WatercraftNavigationHelpers.LaneEffects> m_LaneEffectsQueue;

		public void Execute()
		{
			WatercraftNavigationHelpers.LaneEffects item;
			while (m_LaneEffectsQueue.TryDequeue(out item))
			{
				Entity owner = m_OwnerData[item.m_Lane].m_Owner;
				if (m_PollutionData.TryGetComponent(owner, out var componentData))
				{
					componentData.m_Pollution += item.m_SideEffects.yz;
					m_PollutionData[owner] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<WatercraftNavigation> __Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;

		public BufferTypeHandle<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Watercraft> __Game_Vehicles_Watercraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftData> __Game_Prefabs_WatercraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleSideEffectData> __Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RW_ComponentLookup;

		public ComponentLookup<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentLookup;

		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Watercraft>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftNavigation>();
			__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
			__Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();
			__Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<WatercraftNavigationLane>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Routes_TakeoffLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TakeoffLocation>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Watercraft_RO_ComponentLookup = state.GetComponentLookup<Watercraft>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WatercraftData_RO_ComponentLookup = state.GetComponentLookup<WatercraftData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup = state.GetComponentLookup<VehicleSideEffectData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Net_LaneReservation_RW_ComponentLookup = state.GetComponentLookup<LaneReservation>();
			__Game_Net_Pollution_RW_ComponentLookup = state.GetComponentLookup<Game.Net.Pollution>();
			__Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_VehicleQuery;

	private LaneObjectUpdater m_LaneObjectUpdater;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 8;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Watercraft>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<PathElement>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<WatercraftCurrentLane>(), ComponentType.ReadWrite<WatercraftNavigation>(), ComponentType.ReadWrite<WatercraftNavigationLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		m_LaneObjectUpdater = new LaneObjectUpdater(this);
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<WatercraftNavigationHelpers.LaneReservation> laneReservationQueue = new NativeQueue<WatercraftNavigationHelpers.LaneReservation>(Allocator.TempJob);
		NativeQueue<WatercraftNavigationHelpers.LaneEffects> laneEffectsQueue = new NativeQueue<WatercraftNavigationHelpers.LaneEffects>(Allocator.TempJob);
		NativeQueue<WatercraftNavigationHelpers.LaneSignal> laneSignalQueue = new NativeQueue<WatercraftNavigationHelpers.LaneSignal>(Allocator.TempJob);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle deps;
		UpdateNavigationJob jobData = new UpdateNavigationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OdometerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TakeoffLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TakeoffLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Watercraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WatercraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSideEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies2),
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies4),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_LaneObjectBuffer = m_LaneObjectUpdater.Begin(Allocator.TempJob),
			m_LaneReservations = laneReservationQueue.AsParallelWriter(),
			m_LaneEffects = laneEffectsQueue.AsParallelWriter(),
			m_LaneSignals = laneSignalQueue.AsParallelWriter()
		};
		UpdateLaneReservationsJob jobData2 = new UpdateLaneReservationsJob
		{
			m_LaneReservationQueue = laneReservationQueue,
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		ApplyLaneEffectsJob jobData3 = new ApplyLaneEffectsJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Pollution_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneEffectsQueue = laneEffectsQueue
		};
		UpdateLaneSignalsJob jobData4 = new UpdateLaneSignalsJob
		{
			m_LaneSignalQueue = laneSignalQueue,
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies4, deps));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle);
		JobHandle jobHandle4 = IJobExtensions.Schedule(jobData4, jobHandle);
		laneReservationQueue.Dispose(jobHandle2);
		laneEffectsQueue.Dispose(jobHandle3);
		laneSignalQueue.Dispose(jobHandle4);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		JobHandle job = m_LaneObjectUpdater.Apply(this, jobHandle);
		base.Dependency = JobUtils.CombineDependencies(job, jobHandle2, jobHandle3, jobHandle4);
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
	public WatercraftNavigationSystem()
	{
	}
}
