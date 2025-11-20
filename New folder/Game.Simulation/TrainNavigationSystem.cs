using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
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
public class TrainNavigationSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNavigationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public ComponentTypeHandle<Blocker> m_BlockerType;

		public ComponentTypeHandle<Odometer> m_OdometerType;

		public BufferTypeHandle<TrainNavigationLane> m_NavigationLaneType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TrainNavigation> m_NavigationData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TrainCurrentLane> m_CurrentLaneData;

		[ReadOnly]
		public ComponentLookup<Car> m_CarData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<LaneSignal> m_LaneSignalData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<VehicleSideEffectData> m_PrefabSideEffectData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public NativeQueue<TrainNavigationHelpers.LaneEffects>.ParallelWriter m_LaneEffects;

		public NativeQueue<TrainNavigationHelpers.LaneSignal>.ParallelWriter m_LaneSignals;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Target> nativeArray2 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<Blocker> nativeArray3 = chunk.GetNativeArray(ref m_BlockerType);
			NativeArray<Odometer> nativeArray4 = chunk.GetNativeArray(ref m_OdometerType);
			NativeArray<PathOwner> nativeArray5 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<TrainNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_NavigationLaneType);
			BufferAccessor<LayoutElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LayoutElementType);
			BufferAccessor<PathElement> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PathElementType);
			NativeList<TrainNavigationHelpers.CurrentLaneCache> nativeList = new NativeList<TrainNavigationHelpers.CurrentLaneCache>(10, Allocator.Temp);
			bool flag = nativeArray4.Length != 0;
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity controller = nativeArray[i];
				Target target = nativeArray2[i];
				Blocker blocker = nativeArray3[i];
				PathOwner pathOwner = nativeArray5[i];
				DynamicBuffer<TrainNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<LayoutElement> layout = bufferAccessor2[i];
				DynamicBuffer<PathElement> pathElements = bufferAccessor3[i];
				if (layout.Length != 0)
				{
					for (int j = 0; j < layout.Length; j++)
					{
						Entity vehicle = layout[j].m_Vehicle;
						TrainCurrentLane currentLane = m_CurrentLaneData[vehicle];
						nativeList.Add(new TrainNavigationHelpers.CurrentLaneCache(ref currentLane, m_LaneData));
						m_CurrentLaneData[vehicle] = currentLane;
					}
					Entity vehicle2 = layout[0].m_Vehicle;
					Game.Objects.Transform transform = m_TransformData[vehicle2];
					Moving moving = m_MovingData[vehicle2];
					Train train = m_TrainData[vehicle2];
					TrainNavigation navigation = m_NavigationData[vehicle2];
					TrainCurrentLane currentLane2 = m_CurrentLaneData[vehicle2];
					PrefabRef prefabRef = m_PrefabRefData[vehicle2];
					TrainData prefabTrainData = m_PrefabTrainData[prefabRef.m_Prefab];
					ObjectGeometryData prefabObjectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					int priority = VehicleUtils.GetPriority(prefabTrainData);
					Odometer odometer = default(Odometer);
					if (flag)
					{
						odometer = nativeArray4[i];
					}
					UpdateTrainLimits(ref prefabTrainData, layout);
					UpdateNavigationLanes(transform, train, target, prefabTrainData, ref currentLane2, ref blocker, ref pathOwner, navigationLanes, layout, pathElements);
					UpdateNavigationTarget(priority, controller, transform, moving, train, target, prefabRef, prefabTrainData, prefabObjectGeometryData, ref navigation, ref currentLane2, ref blocker, ref odometer, navigationLanes, layout);
					TryReserveNavigationLanes(train, prefabTrainData, ref navigation, ref currentLane2, navigationLanes);
					m_NavigationData[vehicle2] = navigation;
					m_CurrentLaneData[vehicle2] = currentLane2;
					for (int k = 0; k < layout.Length; k++)
					{
						Entity vehicle3 = layout[k].m_Vehicle;
						TrainCurrentLane currentLane3 = m_CurrentLaneData[vehicle3];
						nativeList[k].CheckChanges(vehicle3, currentLane3, m_LaneObjectBuffer);
					}
					nativeArray5[i] = pathOwner;
					nativeArray3[i] = blocker;
					nativeList.Clear();
					if (flag)
					{
						nativeArray4[i] = odometer;
					}
				}
			}
			nativeList.Dispose();
		}

		private void UpdateNavigationLanes(Game.Objects.Transform transform, Train train, Target target, TrainData prefabTrainData, ref TrainCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<TrainNavigationLane> navigationLanes, DynamicBuffer<LayoutElement> layout, DynamicBuffer<PathElement> pathElements)
		{
			int invalidPath = 0;
			if (!HasValidLanes(currentLane))
			{
				invalidPath++;
				TryFindCurrentLane(ref currentLane, transform, train, prefabTrainData);
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 && (pathOwner.m_State & PathFlags.Append) == 0)
			{
				navigationLanes.Clear();
				currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.Return;
			}
			else if ((pathOwner.m_State & PathFlags.Updated) == 0)
			{
				FillNavigationPaths(target, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements, ref invalidPath);
			}
			for (int i = 1; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				TrainCurrentLane currentLane2 = m_CurrentLaneData[vehicle];
				if (!HasValidLanes(currentLane2))
				{
					Game.Objects.Transform transform2 = m_TransformData[vehicle];
					Train train2 = m_TrainData[vehicle];
					TryFindCurrentLane(ref currentLane2, transform2, train2, prefabTrainData);
					m_CurrentLaneData[vehicle] = currentLane2;
				}
			}
			if (invalidPath != 0)
			{
				navigationLanes.Clear();
				pathElements.Clear();
				pathOwner.m_ElementIndex = 0;
				pathOwner.m_State |= PathFlags.Obsolete;
				currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.Return;
			}
		}

		private bool HasValidLanes(TrainCurrentLane currentLaneData)
		{
			if (currentLaneData.m_Front.m_Lane == Entity.Null)
			{
				return false;
			}
			if (currentLaneData.m_Rear.m_Lane == Entity.Null)
			{
				return false;
			}
			if (currentLaneData.m_FrontCache.m_Lane == Entity.Null)
			{
				return false;
			}
			if (currentLaneData.m_RearCache.m_Lane == Entity.Null)
			{
				return false;
			}
			if ((currentLaneData.m_Front.m_LaneFlags & TrainLaneFlags.Obsolete) != 0)
			{
				return false;
			}
			return true;
		}

		private void TryFindCurrentLane(ref TrainCurrentLane currentLane, Game.Objects.Transform transform, Train train, TrainData prefabTrainData)
		{
			currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.Obsolete;
			currentLane.m_Front.m_Lane = Entity.Null;
			currentLane.m_Rear.m_Lane = Entity.Null;
			currentLane.m_FrontCache.m_Lane = Entity.Null;
			currentLane.m_RearCache.m_Lane = Entity.Null;
			VehicleUtils.CalculateTrainNavigationPivots(transform, prefabTrainData, out var pivot, out var pivot2);
			if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
			{
				CommonUtils.Swap(ref pivot, ref pivot2);
			}
			float num = 100f;
			Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(pivot, pivot2), num);
			TrainNavigationHelpers.FindLaneIterator iterator = new TrainNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_FrontPivot = pivot,
				m_RearPivot = pivot2,
				m_MinDistance = num,
				m_Result = currentLane,
				m_TrackType = prefabTrainData.m_TrackType,
				m_SubLanes = m_Lanes,
				m_TrackLaneData = m_TrackLaneData,
				m_CurveData = m_CurveData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabTrackLaneData = m_PrefabTrackLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
		}

		private void FillNavigationPaths(Target target, ref TrainCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<TrainNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref int invalidPath)
		{
			if ((currentLane.m_Front.m_LaneFlags & TrainLaneFlags.EndOfPath) != 0)
			{
				return;
			}
			for (int i = 0; i < 10000; i++)
			{
				TrainNavigationLane elem;
				if (i >= navigationLanes.Length)
				{
					if (pathOwner.m_ElementIndex >= pathElements.Length || (pathOwner.m_ElementIndex + 1 >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0))
					{
						break;
					}
					PathElement pathElement = pathElements[pathOwner.m_ElementIndex++];
					elem = new TrainNavigationLane
					{
						m_Lane = pathElement.m_Target,
						m_CurvePosition = pathElement.m_TargetDelta
					};
					if (m_TrackLaneData.HasComponent(elem.m_Lane))
					{
						Game.Net.TrackLane trackLane = m_TrackLaneData[elem.m_Lane];
						if (pathOwner.m_ElementIndex >= pathElements.Length)
						{
							elem.m_Flags |= TrainLaneFlags.EndOfPath;
						}
						else
						{
							if ((pathElement.m_Flags & PathElementFlags.Return) != 0)
							{
								elem.m_Flags |= TrainLaneFlags.Return;
							}
							if (((trackLane.m_Flags & (TrackLaneFlags.Twoway | TrackLaneFlags.Switch | TrackLaneFlags.DiamondCrossing | TrackLaneFlags.CrossingTraffic)) != 0 && (trackLane.m_Flags & TrackLaneFlags.MergingTraffic) == 0) || (pathElement.m_Flags & PathElementFlags.Reverse) != 0)
							{
								elem.m_Flags |= TrainLaneFlags.KeepClear;
							}
						}
						if ((trackLane.m_Flags & TrackLaneFlags.Exclusive) != 0)
						{
							elem.m_Flags |= TrainLaneFlags.Exclusive;
						}
						if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
						{
							elem.m_Flags |= TrainLaneFlags.TurnLeft;
						}
						if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
						{
							elem.m_Flags |= TrainLaneFlags.TurnRight;
						}
						navigationLanes.Add(elem);
					}
					else
					{
						if (!m_ConnectionLaneData.HasComponent(elem.m_Lane))
						{
							if (m_EntityLookup.Exists(elem.m_Lane))
							{
								if (pathOwner.m_ElementIndex >= pathElements.Length)
								{
									if (navigationLanes.Length > 0)
									{
										TrainNavigationLane value = navigationLanes[navigationLanes.Length - 1];
										value.m_Flags |= TrainLaneFlags.EndOfPath;
										navigationLanes[navigationLanes.Length - 1] = value;
									}
									else
									{
										currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.EndOfPath;
									}
									elem.m_Flags |= TrainLaneFlags.ParkingSpace;
									navigationLanes.Add(elem);
									break;
								}
								continue;
							}
							invalidPath++;
							break;
						}
						elem.m_Flags |= TrainLaneFlags.Connection;
						if (pathOwner.m_ElementIndex >= pathElements.Length)
						{
							elem.m_Flags |= TrainLaneFlags.EndOfPath;
						}
						navigationLanes.Add(elem);
					}
				}
				else
				{
					elem = navigationLanes[i];
					if (!m_EntityLookup.Exists(elem.m_Lane))
					{
						invalidPath++;
						break;
					}
				}
				if ((elem.m_Flags & TrainLaneFlags.EndOfPath) != 0 || (elem.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.KeepClear | TrainLaneFlags.Connection)) == 0)
				{
					break;
				}
			}
		}

		private void UpdateTrainLimits(ref TrainData prefabTrainData, DynamicBuffer<LayoutElement> layout)
		{
			for (int i = 1; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				PrefabRef prefabRef = m_PrefabRefData[vehicle];
				TrainData trainData = m_PrefabTrainData[prefabRef.m_Prefab];
				prefabTrainData.m_MaxSpeed = math.min(prefabTrainData.m_MaxSpeed, trainData.m_MaxSpeed);
				prefabTrainData.m_Acceleration = math.min(prefabTrainData.m_Acceleration, trainData.m_Acceleration);
				prefabTrainData.m_Braking = math.min(prefabTrainData.m_Braking, trainData.m_Braking);
			}
		}

		private void UpdateNavigationTarget(int priority, Entity controller, Game.Objects.Transform transform, Moving moving, Train train, Target target, PrefabRef prefabRef, TrainData prefabTrainData, ObjectGeometryData prefabObjectGeometryData, ref TrainNavigation navigation, ref TrainCurrentLane currentLane, ref Blocker blocker, ref Odometer odometer, DynamicBuffer<TrainNavigationLane> navigationLanes, DynamicBuffer<LayoutElement> layout)
		{
			float num = 4f / 15f;
			float num2 = navigation.m_Speed;
			bool flag = ((currentLane.m_Front.m_LaneFlags | currentLane.m_FrontCache.m_LaneFlags | currentLane.m_Rear.m_LaneFlags | currentLane.m_RearCache.m_LaneFlags) & TrainLaneFlags.Connection) != 0;
			for (int i = 1; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				TrainCurrentLane trainCurrentLane = m_CurrentLaneData[vehicle];
				flag |= ((trainCurrentLane.m_Front.m_LaneFlags | trainCurrentLane.m_FrontCache.m_LaneFlags | trainCurrentLane.m_Rear.m_LaneFlags | trainCurrentLane.m_RearCache.m_LaneFlags) & TrainLaneFlags.Connection) != 0;
			}
			if (flag)
			{
				prefabTrainData.m_MaxSpeed = 277.77777f;
				prefabTrainData.m_Acceleration = 277.77777f;
				prefabTrainData.m_Braking = 277.77777f;
			}
			else
			{
				num2 = math.min(num2, prefabTrainData.m_MaxSpeed);
			}
			Bounds1 speedRange = ((!flag && (currentLane.m_Front.m_LaneFlags & TrainLaneFlags.ResetSpeed) == 0) ? VehicleUtils.CalculateSpeedRange(prefabTrainData, num2, num) : new Bounds1(0f, prefabTrainData.m_MaxSpeed));
			VehicleUtils.CalculateTrainNavigationPivots(transform, prefabTrainData, out var pivot, out var pivot2);
			if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
			{
				CommonUtils.Swap(ref pivot, ref pivot2);
				prefabTrainData.m_BogieOffsets = prefabTrainData.m_BogieOffsets.yx;
				prefabTrainData.m_AttachOffsets = prefabTrainData.m_AttachOffsets.yx;
			}
			bool flag2 = blocker.m_Type == BlockerType.Temporary;
			TrainLaneSpeedIterator trainLaneSpeedIterator = new TrainLaneSpeedIterator
			{
				m_TransformData = m_TransformData,
				m_MovingData = m_MovingData,
				m_CarData = m_CarData,
				m_TrainData = m_TrainData,
				m_CurveData = m_CurveData,
				m_TrackLaneData = m_TrackLaneData,
				m_ControllerData = m_ControllerData,
				m_LaneReservationData = m_LaneReservationData,
				m_LaneSignalData = m_LaneSignalData,
				m_CreatureData = m_CreatureData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
				m_PrefabCarData = m_PrefabCarData,
				m_PrefabTrainData = m_PrefabTrainData,
				m_PrefabRefData = m_PrefabRefData,
				m_LaneOverlapData = m_LaneOverlaps,
				m_LaneObjectData = m_LaneObjects,
				m_Controller = controller,
				m_Priority = priority,
				m_TimeStep = num,
				m_SafeTimeStep = num + 0.5f,
				m_CurrentSpeed = num2,
				m_SpeedRange = speedRange,
				m_RearPosition = pivot2,
				m_PushBlockers = ((currentLane.m_Front.m_LaneFlags & TrainLaneFlags.PushBlockers) != 0),
				m_MaxSpeed = speedRange.max,
				m_CurrentPosition = pivot
			};
			if (currentLane.m_Front.m_Lane == Entity.Null)
			{
				navigation.m_Speed = math.max(0f, num2 - prefabTrainData.m_Braking * num);
				blocker.m_Blocker = Entity.Null;
				blocker.m_Type = BlockerType.None;
				blocker.m_MaxSpeed = byte.MaxValue;
				return;
			}
			if ((currentLane.m_Front.m_LaneFlags & TrainLaneFlags.HighBeams) == 0 && prefabTrainData.m_TrackType != TrackTypes.Tram && m_TrackLaneData.HasComponent(currentLane.m_Front.m_Lane) && (m_TrackLaneData[currentLane.m_Front.m_Lane].m_Flags & TrackLaneFlags.Station) == 0)
			{
				currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.HighBeams;
			}
			bool flag3 = false;
			bool needSignal = false;
			for (int num3 = layout.Length - 1; num3 >= 1; num3--)
			{
				Entity vehicle2 = layout[num3].m_Vehicle;
				TrainCurrentLane trainCurrentLane2 = m_CurrentLaneData[vehicle2];
				PrefabRef prefabRef2 = m_PrefabRefData[vehicle2];
				TrainData prefabTrain = m_PrefabTrainData[prefabRef2.m_Prefab];
				trainLaneSpeedIterator.m_PrefabTrain = prefabTrain;
				trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_RearCache.m_Lane, out needSignal);
				if (needSignal)
				{
					m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, trainCurrentLane2.m_RearCache.m_Lane, priority));
				}
				trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_Rear.m_Lane, out needSignal);
				if (needSignal)
				{
					m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, trainCurrentLane2.m_Rear.m_Lane, priority));
				}
				trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_FrontCache.m_Lane, out needSignal);
				if (needSignal)
				{
					m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, trainCurrentLane2.m_FrontCache.m_Lane, priority));
				}
				trainLaneSpeedIterator.IteratePrevLane(trainCurrentLane2.m_Front.m_Lane, out needSignal);
				if (needSignal)
				{
					m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, trainCurrentLane2.m_Front.m_Lane, priority));
				}
			}
			bool flag4 = (currentLane.m_Front.m_LaneFlags & TrainLaneFlags.Exclusive) != 0;
			bool skipCurrent = false;
			if (!flag4 && navigationLanes.Length != 0)
			{
				skipCurrent = (navigationLanes[0].m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Exclusive)) == (TrainLaneFlags.Reserved | TrainLaneFlags.Exclusive);
			}
			trainLaneSpeedIterator.m_PrefabTrain = prefabTrainData;
			trainLaneSpeedIterator.m_PrefabObjectGeometry = prefabObjectGeometryData;
			trainLaneSpeedIterator.IteratePrevLane(currentLane.m_RearCache.m_Lane, out needSignal);
			if (needSignal)
			{
				m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, currentLane.m_RearCache.m_Lane, priority));
			}
			trainLaneSpeedIterator.IteratePrevLane(currentLane.m_Rear.m_Lane, out needSignal);
			if (needSignal)
			{
				m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, currentLane.m_Rear.m_Lane, priority));
			}
			trainLaneSpeedIterator.IteratePrevLane(currentLane.m_FrontCache.m_Lane, out needSignal);
			if (needSignal)
			{
				m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, currentLane.m_FrontCache.m_Lane, priority));
			}
			bool num4 = trainLaneSpeedIterator.IterateFirstLane(currentLane.m_Front.m_Lane, currentLane.m_Front.m_CurvePosition, flag4, flag, skipCurrent, out needSignal);
			if (needSignal)
			{
				m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, currentLane.m_Front.m_Lane, priority));
			}
			if (!num4)
			{
				if ((currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) == 0)
				{
					int num5 = 0;
					while (num5 < navigationLanes.Length)
					{
						TrainNavigationLane trainNavigationLane = navigationLanes[num5];
						currentLane.m_Front.m_LaneFlags |= trainNavigationLane.m_Flags & (TrainLaneFlags.TurnLeft | TrainLaneFlags.TurnRight);
						bool flag5 = trainNavigationLane.m_Lane == currentLane.m_Front.m_Lane;
						if ((trainNavigationLane.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Connection)) == 0)
						{
							while ((trainNavigationLane.m_Flags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.BlockReserve)) == 0 && ++num5 < navigationLanes.Length)
							{
								trainNavigationLane = navigationLanes[num5];
							}
							trainLaneSpeedIterator.IterateTarget(trainNavigationLane.m_Lane, flag5);
						}
						else
						{
							if ((trainNavigationLane.m_Flags & TrainLaneFlags.Connection) != 0)
							{
								trainLaneSpeedIterator.m_PrefabTrain.m_MaxSpeed = 277.77777f;
								trainLaneSpeedIterator.m_PrefabTrain.m_Acceleration = 277.77777f;
								trainLaneSpeedIterator.m_PrefabTrain.m_Braking = 277.77777f;
								trainLaneSpeedIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
							}
							float minOffset = math.select(-1f, currentLane.m_Front.m_CurvePosition.z, flag5);
							bool num6 = trainLaneSpeedIterator.IterateNextLane(trainNavigationLane.m_Lane, trainNavigationLane.m_CurvePosition, minOffset, (trainNavigationLane.m_Flags & TrainLaneFlags.Exclusive) != 0, flag5 || flag, out needSignal);
							if (needSignal)
							{
								m_LaneSignals.Enqueue(new TrainNavigationHelpers.LaneSignal(controller, trainNavigationLane.m_Lane, priority));
							}
							if (!num6)
							{
								if ((trainNavigationLane.m_Flags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) != 0)
								{
									break;
								}
								num5++;
								continue;
							}
						}
						goto IL_07ae;
					}
				}
				flag3 = trainLaneSpeedIterator.IterateTarget();
			}
			goto IL_07ae;
			IL_07ae:
			navigation.m_Speed = trainLaneSpeedIterator.m_MaxSpeed;
			float num7 = math.select(1.8360001f, 2.2949998f, (prefabTrainData.m_TrackType & TrackTypes.Tram) != 0);
			blocker.m_Blocker = trainLaneSpeedIterator.m_Blocker;
			blocker.m_Type = trainLaneSpeedIterator.m_BlockerType;
			blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(trainLaneSpeedIterator.m_MaxSpeed * num7), 0, 255);
			bool num8 = blocker.m_Type == BlockerType.Temporary;
			if (num8 != flag2 || currentLane.m_Duration >= 30f)
			{
				ApplySideEffects(ref currentLane, currentLane.m_Front.m_Lane, prefabRef, prefabTrainData);
			}
			if (num8)
			{
				if (currentLane.m_Duration >= 5f)
				{
					currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.PushBlockers;
				}
			}
			else if (currentLane.m_Duration >= 5f)
			{
				currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.PushBlockers;
			}
			float num9 = num2 * num;
			currentLane.m_Duration += num;
			currentLane.m_Distance += num9;
			odometer.m_Distance += num9;
			TrainLaneFlags trainLaneFlags = TrainLaneFlags.EndOfPath | TrainLaneFlags.EndReached;
			if ((currentLane.m_Front.m_LaneFlags & trainLaneFlags) == trainLaneFlags)
			{
				return;
			}
			float num10 = navigation.m_Speed * num;
			TrainBogieCache tempCache = default(TrainBogieCache);
			bool resetCache = ShouldResetCache(currentLane.m_Front, currentLane.m_FrontCache);
			while (true)
			{
				Curve curve = m_CurveData[currentLane.m_Front.m_Lane];
				bool flag6 = curve.m_Length > 0.1f;
				if (flag6 && MoveTarget(pivot, ref navigation.m_Front, num10, curve.m_Bezier, ref currentLane.m_Front.m_CurvePosition))
				{
					if (!flag3 || !(navigation.m_Speed < 0.1f) || !(num2 < 0.1f))
					{
						break;
					}
					currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.EndReached;
					if ((currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) != 0)
					{
						break;
					}
					for (int j = 0; j < navigationLanes.Length; j++)
					{
						TrainLaneFlags trainLaneFlags2 = navigationLanes[j].m_Flags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return);
						if (trainLaneFlags2 != 0)
						{
							currentLane.m_Front.m_LaneFlags |= trainLaneFlags2;
							navigationLanes.RemoveRange(0, j + 1);
							break;
						}
					}
					break;
				}
				if (navigationLanes.Length == 0 || (currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.Return)) != 0)
				{
					if (flag3 && navigation.m_Speed < 0.1f && num2 < 0.1f)
					{
						currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.EndReached;
					}
					break;
				}
				TrainNavigationLane navLane = navigationLanes[0];
				if ((navLane.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Connection)) == 0 || !m_EntityLookup.Exists(navLane.m_Lane))
				{
					break;
				}
				if (flag && (navLane.m_Flags & TrainLaneFlags.Connection) == 0)
				{
					navLane.m_Flags |= TrainLaneFlags.ResetSpeed;
				}
				if ((currentLane.m_Front.m_LaneFlags & TrainLaneFlags.HighBeams) != 0 && prefabTrainData.m_TrackType != TrackTypes.Tram && m_TrackLaneData.TryGetComponent(navLane.m_Lane, out var componentData) && (componentData.m_Flags & TrackLaneFlags.Station) == 0)
				{
					navLane.m_Flags |= TrainLaneFlags.HighBeams;
				}
				if (flag6)
				{
					tempCache = currentLane.m_FrontCache;
					currentLane.m_FrontCache = new TrainBogieCache(currentLane.m_Front);
				}
				TrainLaneFlags trainLaneFlags3 = currentLane.m_Front.m_LaneFlags & TrainLaneFlags.PushBlockers;
				ApplySideEffects(ref currentLane, currentLane.m_Front.m_Lane, prefabRef, prefabTrainData);
				currentLane.m_Front = new TrainBogieLane(navLane);
				currentLane.m_Front.m_LaneFlags |= trainLaneFlags3;
				navigationLanes.RemoveAt(0);
			}
			ClampPosition(ref navigation.m_Front.m_Position, pivot, num10);
			navigation.m_Front.m_Direction = math.normalizesafe(navigation.m_Front.m_Direction);
			float3 position = navigation.m_Front.m_Position;
			float num11 = math.csum(prefabTrainData.m_BogieOffsets);
			currentLane.m_Front.m_CurvePosition.z = currentLane.m_Front.m_CurvePosition.y;
			UpdateFollowerBogie(ref currentLane.m_Rear, ref currentLane.m_RearCache, ref navigation.m_Rear, ref resetCache, ref tempCache, ref currentLane.m_FrontCache, currentLane.m_Front, position, num11);
			if (layout.Length == 1)
			{
				currentLane.m_RearCache = new TrainBogieCache(currentLane.m_Rear);
			}
			else
			{
				position = navigation.m_Rear.m_Position;
				num11 = prefabTrainData.m_AttachOffsets.y - prefabTrainData.m_BogieOffsets.y;
			}
			TrainCurrentLane trainCurrentLane3 = currentLane;
			for (int k = 1; k < layout.Length; k++)
			{
				Entity vehicle3 = layout[k].m_Vehicle;
				Train train2 = m_TrainData[vehicle3];
				TrainCurrentLane currentLaneData = m_CurrentLaneData[vehicle3];
				TrainNavigation value = m_NavigationData[vehicle3];
				PrefabRef prefabRefData = m_PrefabRefData[vehicle3];
				TrainData prefabTrainData2 = m_PrefabTrainData[prefabRefData.m_Prefab];
				if ((train2.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
				{
					prefabTrainData2.m_BogieOffsets = prefabTrainData2.m_BogieOffsets.yx;
					prefabTrainData2.m_AttachOffsets = prefabTrainData2.m_AttachOffsets.yx;
				}
				value.m_Speed = navigation.m_Speed;
				currentLaneData.m_Duration += num;
				currentLaneData.m_Distance += num9;
				Entity lane = currentLaneData.m_Front.m_Lane;
				num11 += prefabTrainData2.m_AttachOffsets.x - prefabTrainData2.m_BogieOffsets.x;
				UpdateFollowerBogie(ref currentLaneData.m_Front, ref currentLaneData.m_FrontCache, ref value.m_Front, ref resetCache, ref tempCache, ref trainCurrentLane3.m_RearCache, trainCurrentLane3.m_Rear, position, num11);
				if (currentLaneData.m_Front.m_Lane != lane || currentLaneData.m_Duration >= 30f)
				{
					ApplySideEffects(ref currentLaneData, lane, prefabRefData, prefabTrainData2);
				}
				position = value.m_Front.m_Position;
				num11 = math.csum(prefabTrainData2.m_BogieOffsets);
				UpdateFollowerBogie(ref currentLaneData.m_Rear, ref currentLaneData.m_RearCache, ref value.m_Rear, ref resetCache, ref tempCache, ref currentLaneData.m_FrontCache, currentLaneData.m_Front, position, num11);
				if (k == 1)
				{
					currentLane = trainCurrentLane3;
				}
				else
				{
					m_CurrentLaneData[layout[k - 1].m_Vehicle] = trainCurrentLane3;
				}
				if (k == layout.Length - 1)
				{
					currentLaneData.m_RearCache = new TrainBogieCache(currentLaneData.m_Rear);
				}
				else
				{
					position = value.m_Rear.m_Position;
					num11 = prefabTrainData2.m_AttachOffsets.y - prefabTrainData2.m_BogieOffsets.y;
				}
				trainCurrentLane3 = currentLaneData;
				m_CurrentLaneData[vehicle3] = currentLaneData;
				m_NavigationData[vehicle3] = value;
			}
		}

		private void ClampPosition(ref float3 position, float3 original, float maxDistance)
		{
			position = original + MathUtils.ClampLength(position - original, maxDistance);
		}

		private bool ShouldResetCache(TrainBogieLane bogie, TrainBogieCache cache)
		{
			if (math.all(bogie.m_CurvePosition == bogie.m_CurvePosition.x) && math.all(cache.m_CurvePosition == bogie.m_CurvePosition.x))
			{
				return cache.m_Lane == bogie.m_Lane;
			}
			return false;
		}

		private void UpdateFollowerBogie(ref TrainBogieLane bogie, ref TrainBogieCache cache, ref TrainBogiePosition position, ref bool resetCache, ref TrainBogieCache tempCache, ref TrainBogieCache nextCache, TrainBogieLane nextBogie, float3 followPosition, float followDistance)
		{
			TrainBogieCache trainBogieCache = default(TrainBogieCache);
			float3 @float = position.m_Position - followPosition;
			if (resetCache)
			{
				if (bogie.m_Lane == nextBogie.m_Lane)
				{
					tempCache = default(TrainBogieCache);
					nextCache = new TrainBogieCache(nextBogie);
					nextCache.m_CurvePosition.x = bogie.m_CurvePosition.w;
				}
				else if (bogie.m_Lane != Entity.Null && nextBogie.m_Lane != Entity.Null)
				{
					tempCache = new TrainBogieCache(bogie);
					nextCache = new TrainBogieCache(nextBogie);
					Curve curve = m_CurveData[bogie.m_Lane];
					Curve curve2 = m_CurveData[nextBogie.m_Lane];
					float3 position2 = MathUtils.Position(curve.m_Bezier, bogie.m_CurvePosition.w);
					MathUtils.Distance(position: MathUtils.Position(curve2.m_Bezier, nextBogie.m_CurvePosition.x), curve: curve.m_Bezier, t: out tempCache.m_CurvePosition.y);
					MathUtils.Distance(curve2.m_Bezier, position2, out nextCache.m_CurvePosition.x);
				}
			}
			resetCache = ShouldResetCache(bogie, cache);
			while (true)
			{
				if (bogie.m_Lane != Entity.Null)
				{
					Curve curve3 = m_CurveData[bogie.m_Lane];
					if (bogie.m_Lane == nextBogie.m_Lane && bogie.m_CurvePosition.w == nextBogie.m_CurvePosition.w)
					{
						float w = bogie.m_CurvePosition.w;
						bogie.m_CurvePosition.zw = nextBogie.m_CurvePosition.y;
						if (MoveFollowerTarget(followPosition, ref position, followDistance, curve3.m_Bezier, ref bogie.m_CurvePosition))
						{
							bogie.m_CurvePosition.w = w;
							break;
						}
						bogie.m_CurvePosition.w = w;
					}
					else
					{
						bogie.m_CurvePosition.z = bogie.m_CurvePosition.w;
						if (MoveFollowerTarget(followPosition, ref position, followDistance, curve3.m_Bezier, ref bogie.m_CurvePosition))
						{
							break;
						}
					}
				}
				if (nextBogie.m_Lane == bogie.m_Lane && nextBogie.m_CurvePosition.xw.Equals(bogie.m_CurvePosition.xw))
				{
					break;
				}
				trainBogieCache = cache;
				cache = new TrainBogieCache(bogie);
				if (tempCache.m_Lane != Entity.Null)
				{
					bogie = new TrainBogieLane(tempCache);
					tempCache = default(TrainBogieCache);
				}
				else
				{
					bogie = new TrainBogieLane(nextCache);
					nextCache = new TrainBogieCache(nextBogie);
				}
			}
			float3 value = position.m_Position - followPosition;
			if (math.dot(value, @float) <= 0f)
			{
				value = @float;
				position.m_Direction = -@float;
			}
			if (MathUtils.TryNormalize(ref value, followDistance))
			{
				position.m_Position = followPosition + value;
				position.m_Direction = math.normalizesafe(position.m_Direction);
			}
			tempCache = trainBogieCache;
		}

		private void ApplySideEffects(ref TrainCurrentLane currentLaneData, Entity lane, PrefabRef prefabRefData, TrainData prefabTrainData)
		{
			if (m_TrackLaneData.HasComponent(lane) && (currentLaneData.m_Duration != 0f || currentLaneData.m_Distance != 0f))
			{
				Game.Net.TrackLane trackLaneData = m_TrackLaneData[lane];
				Curve curve = m_CurveData[lane];
				float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabTrainData, trackLaneData);
				float num = 1f / math.max(1f, curve.m_Length);
				float3 sideEffects = default(float3);
				if (m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab))
				{
					VehicleSideEffectData vehicleSideEffectData = m_PrefabSideEffectData[prefabRefData.m_Prefab];
					float num2 = math.select(currentLaneData.m_Distance / currentLaneData.m_Duration, maxDriveSpeed, currentLaneData.m_Duration == 0f) / prefabTrainData.m_MaxSpeed;
					num2 = math.saturate(num2 * num2);
					sideEffects = math.lerp(vehicleSideEffectData.m_Min, vehicleSideEffectData.m_Max, num2);
					float x = math.min(1f, currentLaneData.m_Distance * num);
					sideEffects *= new float3(x, currentLaneData.m_Duration, currentLaneData.m_Duration);
				}
				maxDriveSpeed = math.min(prefabTrainData.m_MaxSpeed, trackLaneData.m_SpeedLimit);
				float2 flow = new float2(currentLaneData.m_Duration * maxDriveSpeed, currentLaneData.m_Distance) * num;
				m_LaneEffects.Enqueue(new TrainNavigationHelpers.LaneEffects(lane, sideEffects, flow));
			}
			currentLaneData.m_Duration = 0f;
			currentLaneData.m_Distance = 0f;
		}

		private void TryReserveNavigationLanes(Train trainData, TrainData prefabTrainData, ref TrainNavigation navigationData, ref TrainCurrentLane currentLaneData, DynamicBuffer<TrainNavigationLane> navigationLanes)
		{
			float timeStep = 4f / 15f;
			if ((trainData.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
			{
				prefabTrainData.m_BogieOffsets = prefabTrainData.m_BogieOffsets.yx;
				prefabTrainData.m_AttachOffsets = prefabTrainData.m_AttachOffsets.yx;
			}
			if (!(currentLaneData.m_Front.m_Lane != Entity.Null))
			{
				return;
			}
			Curve curve = m_CurveData[currentLaneData.m_Front.m_Lane];
			float brakingDistance = VehicleUtils.GetBrakingDistance(prefabTrainData, navigationData.m_Speed, timeStep);
			brakingDistance = math.max(0f, brakingDistance - 0.01f);
			float num = brakingDistance;
			float num2 = prefabTrainData.m_AttachOffsets.x - prefabTrainData.m_BogieOffsets.x + 2f;
			num2 += VehicleUtils.GetSignalDistance(prefabTrainData, navigationData.m_Speed);
			if (currentLaneData.m_Front.m_CurvePosition.w > currentLaneData.m_Front.m_CurvePosition.x)
			{
				currentLaneData.m_Front.m_CurvePosition.z = currentLaneData.m_Front.m_CurvePosition.y + num / math.max(1E-06f, curve.m_Length);
				currentLaneData.m_Front.m_CurvePosition.z = math.min(currentLaneData.m_Front.m_CurvePosition.z, currentLaneData.m_Front.m_CurvePosition.w);
			}
			else
			{
				currentLaneData.m_Front.m_CurvePosition.z = currentLaneData.m_Front.m_CurvePosition.y - num / math.max(1E-06f, curve.m_Length);
				currentLaneData.m_Front.m_CurvePosition.z = math.max(currentLaneData.m_Front.m_CurvePosition.z, currentLaneData.m_Front.m_CurvePosition.w);
			}
			num -= curve.m_Length * math.abs(currentLaneData.m_Front.m_CurvePosition.w - currentLaneData.m_Front.m_CurvePosition.y);
			int num3 = 0;
			bool flag = num > 0f;
			bool flag2 = num + num2 > 0f || (currentLaneData.m_Front.m_LaneFlags & TrainLaneFlags.KeepClear) != 0;
			while (flag2 && num3 < navigationLanes.Length)
			{
				TrainNavigationLane value = navigationLanes[num3];
				if ((value.m_Flags & TrainLaneFlags.ParkingSpace) != 0)
				{
					break;
				}
				if (m_TrackLaneData.HasComponent(value.m_Lane))
				{
					value.m_Flags |= TrainLaneFlags.TryReserve;
					if (flag)
					{
						value.m_Flags |= TrainLaneFlags.FullReserve;
					}
					else
					{
						value.m_Flags &= ~TrainLaneFlags.FullReserve;
					}
					navigationLanes[num3] = value;
				}
				num -= m_CurveData[value.m_Lane].m_Length * math.abs(value.m_CurvePosition.y - value.m_CurvePosition.x);
				flag = num > 0f;
				flag2 = num + num2 > 0f || (value.m_Flags & TrainLaneFlags.KeepClear) != 0;
				num3++;
			}
		}

		private bool MoveTarget(float3 comparePosition, ref TrainBogiePosition targetPosition, float minDistance, Bezier4x3 curve, ref float4 curveDelta)
		{
			float3 @float = MathUtils.Position(curve, curveDelta.w);
			if (math.distance(comparePosition, @float) < minDistance)
			{
				float t = math.lerp(curveDelta.y, curveDelta.w, 0.5f);
				float3 y = MathUtils.Position(curve, t);
				if (math.distance(comparePosition, y) < minDistance)
				{
					curveDelta.y = curveDelta.w;
					targetPosition.m_Position = @float;
					targetPosition.m_Direction = MathUtils.Tangent(curve, curveDelta.w);
					targetPosition.m_Direction *= math.sign(curveDelta.w - curveDelta.x);
					return false;
				}
			}
			float3 float2 = MathUtils.Position(curve, curveDelta.y);
			if (math.distance(comparePosition, float2) >= minDistance)
			{
				targetPosition.m_Position = float2;
				targetPosition.m_Direction = MathUtils.Tangent(curve, curveDelta.y);
				targetPosition.m_Direction *= math.sign(curveDelta.w - curveDelta.x);
				return true;
			}
			float2 yw = curveDelta.yw;
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(yw.x, yw.y, 0.5f);
				float3 y2 = MathUtils.Position(curve, num);
				if (math.distance(comparePosition, y2) < minDistance)
				{
					yw.x = num;
				}
				else
				{
					yw.y = num;
				}
			}
			curveDelta.y = yw.y;
			targetPosition.m_Position = MathUtils.Position(curve, yw.y);
			targetPosition.m_Direction = MathUtils.Tangent(curve, yw.y);
			targetPosition.m_Direction *= math.sign(curveDelta.w - curveDelta.x);
			return true;
		}

		private bool MoveFollowerTarget(float3 comparePosition, ref TrainBogiePosition targetPosition, float maxDistance, Bezier4x3 curve, ref float4 curveDelta)
		{
			float3 @float = MathUtils.Position(curve, curveDelta.w);
			if (math.distance(comparePosition, @float) > maxDistance)
			{
				curveDelta.y = curveDelta.w;
				targetPosition.m_Position = @float;
				targetPosition.m_Direction = MathUtils.Tangent(curve, curveDelta.w);
				targetPosition.m_Direction *= math.sign(curveDelta.w - curveDelta.x);
				return false;
			}
			float2 yw = curveDelta.yw;
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(yw.x, yw.y, 0.5f);
				float3 y = MathUtils.Position(curve, num);
				if (math.distance(comparePosition, y) > maxDistance)
				{
					yw.x = num;
				}
				else
				{
					yw.y = num;
				}
			}
			curveDelta.y = yw.x;
			targetPosition.m_Position = MathUtils.Position(curve, yw.x);
			targetPosition.m_Direction = MathUtils.Tangent(curve, yw.x);
			targetPosition.m_Direction *= math.sign(curveDelta.w - curveDelta.x);
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLaneSignalsJob : IJob
	{
		public NativeQueue<TrainNavigationHelpers.LaneSignal> m_LaneSignalQueue;

		public ComponentLookup<LaneSignal> m_LaneSignalData;

		public void Execute()
		{
			int count = m_LaneSignalQueue.Count;
			for (int i = 0; i < count; i++)
			{
				TrainNavigationHelpers.LaneSignal laneSignal = m_LaneSignalQueue.Dequeue();
				LaneSignal value = m_LaneSignalData[laneSignal.m_Lane];
				if (laneSignal.m_Priority > value.m_Priority)
				{
					value.m_Petitioner = laneSignal.m_Petitioner;
					value.m_Priority = laneSignal.m_Priority;
					m_LaneSignalData[laneSignal.m_Lane] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateLaneReservationsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutType;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_CurrentLaneData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		public ComponentLookup<LaneReservation> m_LaneReservationData;

		public BufferTypeHandle<TrainNavigationLane> m_NavigationLaneType;

		public void Execute()
		{
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ReserveCurrentLanes(m_Chunks[i]);
			}
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				TryReserveNavigationLanes(m_Chunks[j]);
			}
		}

		private void ReserveCurrentLanes(ArchetypeChunk chunk)
		{
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<LayoutElement> dynamicBuffer = bufferAccessor[i];
				Entity prevLane = Entity.Null;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity vehicle = dynamicBuffer[j].m_Vehicle;
					TrainCurrentLane currentLaneData = m_CurrentLaneData[vehicle];
					ReserveCurrentLanes(vehicle, currentLaneData, ref prevLane, 98);
				}
			}
		}

		private void ReserveCurrentLanes(Entity entity, TrainCurrentLane currentLaneData, ref Entity prevLane, int priority)
		{
			if (currentLaneData.m_Front.m_Lane != Entity.Null && currentLaneData.m_Front.m_Lane != prevLane)
			{
				ReserveLane(entity, currentLaneData.m_Front.m_Lane, priority);
			}
			if (currentLaneData.m_FrontCache.m_Lane != Entity.Null && currentLaneData.m_FrontCache.m_Lane != currentLaneData.m_Front.m_Lane)
			{
				ReserveLane(entity, currentLaneData.m_FrontCache.m_Lane, priority);
			}
			if (currentLaneData.m_Rear.m_Lane != Entity.Null && currentLaneData.m_Rear.m_Lane != currentLaneData.m_FrontCache.m_Lane)
			{
				ReserveLane(entity, currentLaneData.m_Rear.m_Lane, priority);
			}
			if (currentLaneData.m_RearCache.m_Lane != Entity.Null && currentLaneData.m_RearCache.m_Lane != currentLaneData.m_Rear.m_Lane)
			{
				ReserveLane(entity, currentLaneData.m_RearCache.m_Lane, priority);
			}
			prevLane = currentLaneData.m_RearCache.m_Lane;
		}

		private void ReserveLane(Entity entity, Entity lane, int priority)
		{
			if (!m_LaneReservationData.HasComponent(lane))
			{
				return;
			}
			ref LaneReservation valueRW = ref m_LaneReservationData.GetRefRW(lane).ValueRW;
			if (priority > valueRW.m_Next.m_Priority)
			{
				if (priority >= valueRW.m_Prev.m_Priority)
				{
					valueRW.m_Blocker = entity;
				}
				valueRW.m_Next.m_Priority = (byte)priority;
			}
		}

		private void TryReserveNavigationLanes(ArchetypeChunk chunk)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutType);
			BufferAccessor<TrainNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_NavigationLaneType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				DynamicBuffer<LayoutElement> layout = bufferAccessor[i];
				DynamicBuffer<TrainNavigationLane> navigationLanes = bufferAccessor2[i];
				if (layout.Length >= 1)
				{
					Entity vehicle = layout[0].m_Vehicle;
					TrainCurrentLane trainCurrentLane = m_CurrentLaneData[vehicle];
					int priority = VehicleUtils.GetPriority(m_PrefabTrainData[prefabRef.m_Prefab]);
					TryReserveNavigationLanes(vehicle, navigationLanes, layout, trainCurrentLane.m_Front.m_Lane, 98, priority);
				}
			}
		}

		private void TryReserveNavigationLanes(Entity entity, DynamicBuffer<TrainNavigationLane> navigationLanes, DynamicBuffer<LayoutElement> layout, Entity prevLane, int priority, int fullPriority)
		{
			Entity entity2 = prevLane;
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < navigationLanes.Length; i++)
			{
				ref TrainNavigationLane reference = ref navigationLanes.ElementAt(i);
				if ((reference.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.TryReserve | TrainLaneFlags.Connection)) == 0)
				{
					break;
				}
				if ((reference.m_Flags & (TrainLaneFlags.Reserved | TrainLaneFlags.Connection)) != 0)
				{
					num = i;
					num2 = i;
				}
				else
				{
					if (!(reference.m_Lane == prevLane) && (reference.m_Flags & TrainLaneFlags.Exclusive) != 0 && !CanReserveLane(reference.m_Lane, layout))
					{
						reference.m_Flags |= TrainLaneFlags.BlockReserve;
						num2 = num;
						break;
					}
					reference.m_Flags &= ~TrainLaneFlags.BlockReserve;
					num = math.select(num, i, num == i - 1 && reference.m_Lane == prevLane);
					num2 = i;
				}
				prevLane = reference.m_Lane;
			}
			prevLane = entity2;
			for (int j = 0; j <= num2; j++)
			{
				ref TrainNavigationLane reference2 = ref navigationLanes.ElementAt(j);
				if (reference2.m_Lane != prevLane)
				{
					bool test = (reference2.m_Flags & (TrainLaneFlags.TryReserve | TrainLaneFlags.FullReserve)) == (TrainLaneFlags.TryReserve | TrainLaneFlags.FullReserve);
					int priority2 = math.select(priority, fullPriority, test);
					ReserveLane(entity, reference2.m_Lane, priority2);
				}
				if ((reference2.m_Flags & TrainLaneFlags.TryReserve) != 0)
				{
					reference2.m_Flags &= ~(TrainLaneFlags.TryReserve | TrainLaneFlags.FullReserve);
					reference2.m_Flags |= TrainLaneFlags.Reserved;
				}
				prevLane = reference2.m_Lane;
			}
		}

		private bool CanReserveLane(Entity lane, DynamicBuffer<LayoutElement> layout)
		{
			if (m_LaneReservationData.HasComponent(lane) && m_LaneReservationData[lane].GetPriority() != 0)
			{
				for (int i = 0; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					TrainCurrentLane trainCurrentLane = m_CurrentLaneData[vehicle];
					if (trainCurrentLane.m_Front.m_Lane == lane || trainCurrentLane.m_FrontCache.m_Lane == lane || trainCurrentLane.m_Rear.m_Lane == lane || trainCurrentLane.m_RearCache.m_Lane == lane)
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}
	}

	[BurstCompile]
	private struct ApplyLaneEffectsJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<LaneDeteriorationData> m_LaneDeteriorationData;

		public ComponentLookup<Game.Net.Pollution> m_PollutionData;

		public ComponentLookup<LaneCondition> m_LaneConditionData;

		public ComponentLookup<LaneFlow> m_LaneFlowData;

		public NativeQueue<TrainNavigationHelpers.LaneEffects> m_LaneEffectsQueue;

		public void Execute()
		{
			int count = m_LaneEffectsQueue.Count;
			for (int i = 0; i < count; i++)
			{
				TrainNavigationHelpers.LaneEffects laneEffects = m_LaneEffectsQueue.Dequeue();
				Entity owner = m_OwnerData[laneEffects.m_Lane].m_Owner;
				if (m_LaneConditionData.TryGetComponent(laneEffects.m_Lane, out var componentData))
				{
					PrefabRef prefabRef = m_PrefabRefData[laneEffects.m_Lane];
					if (m_LaneDeteriorationData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
					{
						componentData.m_Wear = math.min(componentData.m_Wear + laneEffects.m_SideEffects.x * componentData2.m_TrafficFactor, 10f);
						m_LaneConditionData[laneEffects.m_Lane] = componentData;
					}
				}
				if (m_LaneFlowData.TryGetComponent(laneEffects.m_Lane, out var componentData3))
				{
					componentData3.m_Next += laneEffects.m_Flow;
					m_LaneFlowData[laneEffects.m_Lane] = componentData3;
				}
				if (m_PollutionData.TryGetComponent(owner, out var componentData4))
				{
					componentData4.m_Pollution += laneEffects.m_SideEffects.yz;
					m_PollutionData[owner] = componentData4;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;

		public BufferTypeHandle<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<TrainNavigation> __Game_Vehicles_TrainNavigation_RW_ComponentLookup;

		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleSideEffectData> __Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneDeteriorationData> __Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup;

		public ComponentLookup<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentLookup;

		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RW_ComponentLookup;

		public ComponentLookup<LaneFlow> __Game_Net_LaneFlow_RW_ComponentLookup;

		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
			__Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();
			__Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<TrainNavigationLane>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_TrainNavigation_RW_ComponentLookup = state.GetComponentLookup<TrainNavigation>();
			__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>();
			__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup = state.GetComponentLookup<VehicleSideEffectData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Net_LaneReservation_RW_ComponentLookup = state.GetComponentLookup<LaneReservation>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup = state.GetComponentLookup<LaneDeteriorationData>(isReadOnly: true);
			__Game_Net_Pollution_RW_ComponentLookup = state.GetComponentLookup<Game.Net.Pollution>();
			__Game_Net_LaneCondition_RW_ComponentLookup = state.GetComponentLookup<LaneCondition>();
			__Game_Net_LaneFlow_RW_ComponentLookup = state.GetComponentLookup<LaneFlow>();
			__Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_VehicleQuery;

	private LaneObjectUpdater m_LaneObjectUpdater;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 3;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Train>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<Moving>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<PathElement>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<TrainCurrentLane>(), ComponentType.ReadWrite<TrainNavigation>(), ComponentType.ReadWrite<TrainNavigationLane>(), ComponentType.ReadWrite<LayoutElement>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		m_LaneObjectUpdater = new LaneObjectUpdater(this);
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<TrainNavigationHelpers.LaneEffects> laneEffectsQueue = new NativeQueue<TrainNavigationHelpers.LaneEffects>(Allocator.TempJob);
		NativeQueue<TrainNavigationHelpers.LaneSignal> laneSignalQueue = new NativeQueue<TrainNavigationHelpers.LaneSignal>(Allocator.TempJob);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_VehicleQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle dependencies;
		UpdateNavigationJob jobData = new UpdateNavigationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OdometerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainNavigation_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSideEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_LaneObjectBuffer = m_LaneObjectUpdater.Begin(Allocator.TempJob),
			m_LaneEffects = laneEffectsQueue.AsParallelWriter(),
			m_LaneSignals = laneSignalQueue.AsParallelWriter()
		};
		UpdateLaneReservationsJob jobData2 = new UpdateLaneReservationsJob
		{
			m_Chunks = chunks,
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RW_ComponentLookup, ref base.CheckedStateRef),
			m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		ApplyLaneEffectsJob jobData3 = new ApplyLaneEffectsJob
		{
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneDeteriorationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Pollution_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneFlowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneFlow_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneEffectsQueue = laneEffectsQueue
		};
		UpdateLaneSignalsJob jobData4 = new UpdateLaneSignalsJob
		{
			m_LaneSignalQueue = laneSignalQueue,
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, outJobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle);
		JobHandle jobHandle4 = IJobExtensions.Schedule(jobData4, jobHandle);
		laneEffectsQueue.Dispose(jobHandle3);
		laneSignalQueue.Dispose(jobHandle4);
		chunks.Dispose(jobHandle2);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		JobHandle job = m_LaneObjectUpdater.Apply(this, jobHandle);
		base.Dependency = JobUtils.CombineDependencies(job, jobHandle2, jobHandle4, jobHandle3);
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
	public TrainNavigationSystem()
	{
	}
}
