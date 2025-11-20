using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
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
public class AircraftNavigationSystem : GameSystemBase
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
		public ComponentTypeHandle<Aircraft> m_AircraftType;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> m_HelicopterType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<AircraftNavigation> m_NavigationType;

		public ComponentTypeHandle<AircraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public ComponentTypeHandle<Blocker> m_BlockerType;

		public ComponentTypeHandle<Odometer> m_OdometerType;

		public BufferTypeHandle<AircraftNavigationLane> m_NavigationLaneType;

		public BufferTypeHandle<PathElement> m_PathElementType;

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
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> m_TakeoffLocationData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Aircraft> m_AircraftData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AircraftData> m_PrefabAircraftData;

		[ReadOnly]
		public ComponentLookup<HelicopterData> m_PrefabHelicopterData;

		[ReadOnly]
		public ComponentLookup<AirplaneData> m_PrefabAirplaneData;

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
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public AirwayHelpers.AirwayData m_AirwayData;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public NativeQueue<AircraftNavigationHelpers.LaneReservation>.ParallelWriter m_LaneReservations;

		public NativeQueue<AircraftNavigationHelpers.LaneEffects>.ParallelWriter m_LaneEffects;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<AircraftNavigation> nativeArray4 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<PathOwner> nativeArray6 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<AircraftNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_NavigationLaneType);
			BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
			bool isHelicopter = chunk.Has(ref m_HelicopterType);
			if (nativeArray3.Length != 0)
			{
				NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
				NativeArray<Aircraft> nativeArray9 = chunk.GetNativeArray(ref m_AircraftType);
				NativeArray<Blocker> nativeArray10 = chunk.GetNativeArray(ref m_BlockerType);
				NativeArray<Odometer> nativeArray11 = chunk.GetNativeArray(ref m_OdometerType);
				bool flag = nativeArray11.Length != 0;
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray[i];
					Game.Objects.Transform transform = nativeArray2[i];
					Moving moving = nativeArray3[i];
					Target target = nativeArray8[i];
					Aircraft aircraft = nativeArray9[i];
					AircraftNavigation navigation = nativeArray4[i];
					AircraftCurrentLane currentLane = nativeArray5[i];
					Blocker blocker = nativeArray10[i];
					PathOwner pathOwner = nativeArray6[i];
					PrefabRef prefabRefData = nativeArray7[i];
					DynamicBuffer<AircraftNavigationLane> navigationLanes = bufferAccessor[i];
					DynamicBuffer<PathElement> pathElements = bufferAccessor2[i];
					AircraftData prefabAircraftData = m_PrefabAircraftData[prefabRefData.m_Prefab];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRefData.m_Prefab];
					AircraftNavigationHelpers.CurrentLaneCache currentLaneCache = new AircraftNavigationHelpers.CurrentLaneCache(ref currentLane, m_PrefabRefData, m_MovingObjectSearchTree);
					int priority = VehicleUtils.GetPriority(prefabAircraftData);
					Odometer odometer = default(Odometer);
					if (flag)
					{
						odometer = nativeArray11[i];
					}
					UpdateNavigationLanes(priority, entity, isHelicopter, transform, target, aircraft, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements);
					UpdateNavigationTarget(priority, entity, isHelicopter, transform, moving, aircraft, prefabRefData, prefabAircraftData, objectGeometryData, ref navigation, ref currentLane, ref blocker, ref odometer, navigationLanes);
					ReserveNavigationLanes(priority, prefabAircraftData, aircraft, ref navigation, ref currentLane, navigationLanes);
					currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
					nativeArray4[i] = navigation;
					nativeArray5[i] = currentLane;
					nativeArray6[i] = pathOwner;
					nativeArray10[i] = blocker;
					if (flag)
					{
						nativeArray11[i] = odometer;
					}
				}
			}
			else
			{
				for (int j = 0; j < chunk.Count; j++)
				{
					Entity entity2 = nativeArray[j];
					Game.Objects.Transform transform2 = nativeArray2[j];
					AircraftNavigation navigation2 = nativeArray4[j];
					AircraftCurrentLane currentLane2 = nativeArray5[j];
					PathOwner pathOwnerData = nativeArray6[j];
					PrefabRef prefabRef = nativeArray7[j];
					DynamicBuffer<AircraftNavigationLane> navigationLanes2 = bufferAccessor[j];
					DynamicBuffer<PathElement> pathElements2 = bufferAccessor2[j];
					ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					AircraftNavigationHelpers.CurrentLaneCache currentLaneCache2 = new AircraftNavigationHelpers.CurrentLaneCache(ref currentLane2, m_PrefabRefData, m_MovingObjectSearchTree);
					UpdateStopped(isHelicopter, transform2, ref currentLane2, ref pathOwnerData, navigationLanes2, pathElements2);
					currentLaneCache2.CheckChanges(entity2, ref currentLane2, m_LaneObjectBuffer, m_LaneObjects, transform2, default(Moving), navigation2, objectGeometryData2);
					nativeArray5[j] = currentLane2;
					nativeArray6[j] = pathOwnerData;
				}
			}
		}

		private void UpdateStopped(bool isHelicopter, Game.Objects.Transform transform, ref AircraftCurrentLane currentLaneData, ref PathOwner pathOwnerData, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
		{
			if (currentLaneData.m_Lane == Entity.Null || (currentLaneData.m_LaneFlags & AircraftLaneFlags.Obsolete) != 0)
			{
				TryFindCurrentLane(ref currentLaneData, transform, isHelicopter);
				navigationLanes.Clear();
				pathElements.Clear();
				pathOwnerData.m_ElementIndex = 0;
				pathOwnerData.m_State |= PathFlags.Obsolete;
			}
		}

		private void UpdateNavigationLanes(int priority, Entity entity, bool isHelicopter, Game.Objects.Transform transform, Target target, Aircraft watercraft, ref AircraftCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
		{
			int invalidPath = 10000000;
			if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & AircraftLaneFlags.Obsolete) != 0)
			{
				invalidPath = -1;
				TryFindCurrentLane(ref currentLane, transform, isHelicopter);
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 && (pathOwner.m_State & PathFlags.Append) == 0)
			{
				ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
			}
			else if ((pathOwner.m_State & PathFlags.Updated) == 0)
			{
				FillNavigationPaths(priority, entity, isHelicopter, transform, target, watercraft, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements, ref invalidPath);
			}
			if (invalidPath != 10000000)
			{
				ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
				pathElements.Clear();
				pathOwner.m_ElementIndex = 0;
				pathOwner.m_State |= PathFlags.Obsolete;
			}
		}

		private void ClearNavigationLanes(ref AircraftCurrentLane currentLane, DynamicBuffer<AircraftNavigationLane> navigationLanes, int invalidPath)
		{
			currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
			if (invalidPath > 0)
			{
				for (int i = 0; i < navigationLanes.Length; i++)
				{
					if ((navigationLanes[i].m_Flags & AircraftLaneFlags.Reserved) == 0)
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

		private void TryFindCurrentLane(ref AircraftCurrentLane currentLaneData, Game.Objects.Transform transformData, bool isHelicopter)
		{
			currentLaneData.m_LaneFlags &= ~AircraftLaneFlags.Obsolete;
			currentLaneData.m_Lane = Entity.Null;
			float3 position = transformData.m_Position;
			float num = 100f;
			Bounds3 bounds = new Bounds3(position - num, position + num);
			AircraftNavigationHelpers.FindLaneIterator iterator = new AircraftNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = position,
				m_MinDistance = num,
				m_Result = currentLaneData,
				m_CarType = (isHelicopter ? RoadTypes.Helicopter : RoadTypes.Airplane),
				m_SubLanes = m_Lanes,
				m_CarLaneData = m_CarLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabCarLaneData = m_PrefabCarLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			iterator.Iterate(ref m_AirwayData);
			currentLaneData = iterator.m_Result;
		}

		private void FillNavigationPaths(int priority, Entity entity, bool isHelicopter, Game.Objects.Transform transform, Target target, Aircraft aircraft, ref AircraftCurrentLane currentLaneData, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref int invalidPath)
		{
			if ((currentLaneData.m_LaneFlags & AircraftLaneFlags.EndOfPath) != 0)
			{
				return;
			}
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
						AircraftNavigationLane elem = default(AircraftNavigationLane);
						if (i > 0)
						{
							AircraftNavigationLane value = navigationLanes[i - 1];
							if ((value.m_Flags & AircraftLaneFlags.Airway) != 0)
							{
								if (!GetTransformTarget(ref elem.m_Lane, target))
								{
									value.m_Flags |= AircraftLaneFlags.EndOfPath;
									navigationLanes[i - 1] = value;
									break;
								}
								elem.m_Flags |= AircraftLaneFlags.EndOfPath | AircraftLaneFlags.TransformTarget;
								if ((aircraft.m_Flags & AircraftFlags.StayMidAir) != 0)
								{
									elem.m_Flags |= AircraftLaneFlags.Airway;
								}
								navigationLanes.Add(elem);
							}
							else if ((value.m_Flags & AircraftLaneFlags.TransformTarget) != 0 || (aircraft.m_Flags & AircraftFlags.StayOnTaxiway) != 0 || !GetTransformTarget(ref elem.m_Lane, target))
							{
								value.m_Flags |= AircraftLaneFlags.EndOfPath;
								navigationLanes[i - 1] = value;
							}
							else
							{
								elem.m_Flags |= AircraftLaneFlags.EndOfPath | AircraftLaneFlags.TransformTarget;
								navigationLanes.Add(elem);
							}
						}
						else if ((currentLaneData.m_LaneFlags & AircraftLaneFlags.Airway) != 0)
						{
							if (!GetTransformTarget(ref elem.m_Lane, target))
							{
								currentLaneData.m_LaneFlags |= AircraftLaneFlags.EndOfPath;
								break;
							}
							elem.m_Flags |= AircraftLaneFlags.EndOfPath | AircraftLaneFlags.TransformTarget;
							if ((aircraft.m_Flags & AircraftFlags.StayMidAir) != 0)
							{
								elem.m_Flags |= AircraftLaneFlags.Airway;
							}
							navigationLanes.Add(elem);
						}
						else if ((currentLaneData.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0 || (aircraft.m_Flags & AircraftFlags.StayOnTaxiway) != 0 || !GetTransformTarget(ref elem.m_Lane, target))
						{
							currentLaneData.m_LaneFlags |= AircraftLaneFlags.EndOfPath;
						}
						else
						{
							elem.m_Flags |= AircraftLaneFlags.EndOfPath | AircraftLaneFlags.TransformTarget;
							navigationLanes.Add(elem);
						}
						break;
					}
					PathElement pathElement = pathElements[pathOwner.m_ElementIndex++];
					AircraftNavigationLane elem2 = new AircraftNavigationLane
					{
						m_Lane = pathElement.m_Target,
						m_CurvePosition = pathElement.m_TargetDelta
					};
					AircraftLaneFlags aircraftLaneFlags = ((i <= 0) ? currentLaneData.m_LaneFlags : navigationLanes[i - 1].m_Flags);
					if ((aircraftLaneFlags & AircraftLaneFlags.Airway) != 0 && (aircraft.m_Flags & AircraftFlags.StayMidAir) != 0)
					{
						elem2.m_Flags |= AircraftLaneFlags.Airway;
					}
					if (m_CarLaneData.HasComponent(elem2.m_Lane))
					{
						if ((m_CarLaneData[elem2.m_Lane].m_Flags & Game.Net.CarLaneFlags.Runway) != 0)
						{
							elem2.m_Flags |= AircraftLaneFlags.Runway;
						}
						navigationLanes.Add(elem2);
						continue;
					}
					if (m_ConnectionLaneData.HasComponent(elem2.m_Lane))
					{
						if ((m_ConnectionLaneData[elem2.m_Lane].m_Flags & ConnectionLaneFlags.Airway) != 0)
						{
							elem2.m_Flags |= AircraftLaneFlags.Airway;
							navigationLanes.Add(elem2);
							break;
						}
						elem2.m_Flags |= AircraftLaneFlags.Connection;
						navigationLanes.Add(elem2);
						continue;
					}
					if (m_TakeoffLocationData.HasComponent(elem2.m_Lane))
					{
						if (isHelicopter)
						{
							elem2.m_Flags |= AircraftLaneFlags.TransformTarget;
							if ((aircraft.m_Flags & AircraftFlags.StayMidAir) == 0 && m_SpawnLocationData.HasComponent(elem2.m_Lane))
							{
								elem2.m_Flags |= AircraftLaneFlags.ParkingSpace;
							}
							navigationLanes.Add(elem2);
						}
						continue;
					}
					if (!m_SpawnLocationData.HasComponent(elem2.m_Lane))
					{
						invalidPath = i;
						break;
					}
					if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
					{
						pathOwner.m_ElementIndex--;
						break;
					}
					if ((aircraft.m_Flags & AircraftFlags.StayOnTaxiway) == 0 || pathElements.Length > pathOwner.m_ElementIndex)
					{
						elem2.m_Flags |= AircraftLaneFlags.TransformTarget;
						navigationLanes.Add(elem2);
					}
				}
				else
				{
					AircraftNavigationLane aircraftNavigationLane = navigationLanes[i];
					if (!m_PrefabRefData.HasComponent(aircraftNavigationLane.m_Lane))
					{
						invalidPath = i;
						break;
					}
					if ((aircraftNavigationLane.m_Flags & AircraftLaneFlags.EndOfPath) != 0)
					{
						break;
					}
				}
			}
		}

		private bool GetTransformTarget(ref Entity entity, Target target)
		{
			if (m_PropertyRenterData.TryGetComponent(target.m_Target, out var componentData))
			{
				target.m_Target = componentData.m_Property;
			}
			if (m_TransformData.HasComponent(target.m_Target))
			{
				entity = target.m_Target;
				return true;
			}
			if (m_PositionDataFromEntity.HasComponent(target.m_Target))
			{
				entity = target.m_Target;
				return true;
			}
			return false;
		}

		private void CheckBlocker(Aircraft aircraft, bool isHelicopter, ref AircraftCurrentLane currentLane, ref Blocker blocker, ref AircraftLaneSpeedIterator laneIterator)
		{
			if (laneIterator.m_Blocker != blocker.m_Blocker)
			{
				currentLane.m_LaneFlags &= ~AircraftLaneFlags.IgnoreBlocker;
			}
			if (laneIterator.m_Blocker != Entity.Null && m_MovingDataFromEntity.HasComponent(laneIterator.m_Blocker) && m_AircraftData.HasComponent(laneIterator.m_Blocker) && (m_AircraftData[laneIterator.m_Blocker].m_Flags & ~aircraft.m_Flags & AircraftFlags.Blocking) != 0 && laneIterator.m_MaxSpeed < 1f)
			{
				currentLane.m_LaneFlags |= AircraftLaneFlags.IgnoreBlocker;
			}
			float num = math.select(0.91800004f, 3.06f, isHelicopter);
			blocker.m_Blocker = laneIterator.m_Blocker;
			blocker.m_Type = laneIterator.m_BlockerType;
			blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(laneIterator.m_MaxSpeed * num), 0, 255);
		}

		private void UpdateNavigationTarget(int priority, Entity entity, bool isHelicopter, Game.Objects.Transform transform, Moving moving, Aircraft aircraft, PrefabRef prefabRefData, AircraftData prefabAircraftData, ObjectGeometryData prefabObjectGeometryData, ref AircraftNavigation navigation, ref AircraftCurrentLane currentLane, ref Blocker blocker, ref Odometer odometer, DynamicBuffer<AircraftNavigationLane> navigationLanes)
		{
			float num = 4f / 15f;
			float num2 = math.length(moving.m_Velocity);
			float3 position = transform.m_Position;
			if ((currentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
			{
				if (isHelicopter)
				{
					HelicopterData helicopterData = m_PrefabHelicopterData[prefabRefData.m_Prefab];
					Bounds1 bounds = (((currentLane.m_LaneFlags & (AircraftLaneFlags.Connection | AircraftLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(helicopterData, num2, num) : new Bounds1(0f, helicopterData.m_FlyingMaxSpeed));
					if ((currentLane.m_LaneFlags & AircraftLaneFlags.SkipLane) != 0)
					{
						navigation.m_TargetPosition = transform.m_Position;
					}
					float3 @float = navigation.m_TargetPosition - transform.m_Position;
					float num3 = math.length(@float.xz);
					float num4 = math.length(@float);
					float num5 = math.saturate((math.dot(moving.m_Velocity, @float) + 1f) / (num2 * num4 + 1f));
					num5 = 1f - math.sqrt(1f - num5 * num5);
					float position2 = VehicleUtils.GetMaxBrakingSpeed(distance: math.lerp(num3, num4, num5), prefabHelicopterData: helicopterData, timeStep: num + 0.5f);
					navigation.m_MaxSpeed = MathUtils.Clamp(position2, bounds);
					blocker.m_Blocker = Entity.Null;
					blocker.m_Type = BlockerType.None;
					blocker.m_MaxSpeed = byte.MaxValue;
					float brakingDistance = VehicleUtils.GetBrakingDistance(helicopterData, helicopterData.m_FlyingMaxSpeed, num);
					brakingDistance = math.max(brakingDistance, 750f);
					brakingDistance += navigation.m_MaxSpeed * num + 1f;
					currentLane.m_Duration += num;
					currentLane.m_Distance += num2 * num;
					odometer.m_Distance += num2 * num;
					if (num3 < brakingDistance)
					{
						while (true)
						{
							bool flag = (currentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0;
							if (flag && navigationLanes.Length != 0 && (navigationLanes[0].m_Flags & AircraftLaneFlags.Airway) != 0)
							{
								currentLane.m_LaneFlags &= ~AircraftLaneFlags.Landing;
								flag = false;
							}
							if ((currentLane.m_LaneFlags & AircraftLaneFlags.SkipLane) == 0)
							{
								if ((currentLane.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0)
								{
									navigation.m_TargetDirection = default(float3);
									bool num6 = MoveTarget(position, ref navigation.m_TargetPosition, brakingDistance, currentLane.m_Lane);
									if (!flag)
									{
										UpdateTargetHeight(ref navigation.m_TargetPosition, currentLane.m_Lane, helicopterData);
									}
									if (num6)
									{
										break;
									}
								}
								else
								{
									navigation.m_TargetDirection = default(float3);
									Curve curve = m_CurveData[currentLane.m_Lane];
									bool num7 = MoveTarget(position, ref navigation.m_TargetPosition, brakingDistance, curve.m_Bezier, ref currentLane.m_CurvePosition);
									if (!flag)
									{
										UpdateTargetHeight(ref navigation.m_TargetPosition, currentLane.m_Lane, helicopterData);
									}
									if (num7)
									{
										break;
									}
								}
							}
							if (flag)
							{
								@float = navigation.m_TargetPosition - transform.m_Position;
								num4 = math.length(@float);
								if (!(num4 >= 1f) && !(num2 >= 0.1f))
								{
									currentLane.m_LaneFlags &= ~(AircraftLaneFlags.Flying | AircraftLaneFlags.Landing);
									if (navigationLanes.Length == 0)
									{
										currentLane.m_LaneFlags |= AircraftLaneFlags.EndReached;
									}
								}
								break;
							}
							if (navigationLanes.Length == 0)
							{
								@float = navigation.m_TargetPosition - transform.m_Position;
								num4 = math.length(@float);
								if (num4 < 1f && num2 < 0.1f)
								{
									currentLane.m_LaneFlags |= AircraftLaneFlags.EndReached;
								}
								break;
							}
							AircraftNavigationLane aircraftNavigationLane = navigationLanes[0];
							if (!m_PrefabRefData.HasComponent(aircraftNavigationLane.m_Lane))
							{
								break;
							}
							aircraftNavigationLane.m_Flags |= AircraftLaneFlags.Flying;
							if ((aircraftNavigationLane.m_Flags & AircraftLaneFlags.Airway) == 0)
							{
								aircraftNavigationLane.m_Flags |= AircraftLaneFlags.Landing;
							}
							ApplySideEffects(ref currentLane, prefabRefData, helicopterData);
							currentLane.m_Lane = aircraftNavigationLane.m_Lane;
							currentLane.m_CurvePosition = aircraftNavigationLane.m_CurvePosition.xxy;
							currentLane.m_LaneFlags = aircraftNavigationLane.m_Flags;
							navigationLanes.RemoveAt(0);
						}
					}
				}
				else
				{
					AirplaneData airplaneData = m_PrefabAirplaneData[prefabRefData.m_Prefab];
					Bounds1 bounds2 = (((currentLane.m_LaneFlags & (AircraftLaneFlags.Connection | AircraftLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(airplaneData, num2, num) : new Bounds1(airplaneData.m_FlyingSpeed.x, airplaneData.m_FlyingSpeed.y));
					float3 float2 = navigation.m_TargetPosition - transform.m_Position;
					float num8 = math.length(float2.xz);
					float num9 = math.length(float2);
					float num10 = math.saturate((math.dot(moving.m_Velocity, float2) + 1f) / (num2 * num9 + 1f));
					num10 = 1f - math.sqrt(1f - num10 * num10);
					float num11 = math.lerp(num8, num9, num10);
					float num12;
					if ((currentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
					{
						num12 = prefabAircraftData.m_GroundMaxSpeed;
					}
					else if ((currentLane.m_LaneFlags & (AircraftLaneFlags.Approaching | AircraftLaneFlags.TakingOff)) != 0)
					{
						num12 = airplaneData.m_FlyingSpeed.y;
						num11 += 1500f;
					}
					else
					{
						num12 = VehicleUtils.GetBrakingDistance(airplaneData, airplaneData.m_FlyingSpeed.y, num);
						num12 -= VehicleUtils.GetBrakingDistance(airplaneData, prefabAircraftData.m_GroundMaxSpeed, num) + 1500f;
						num12 = math.max(num12, 1500f);
						num11 += 1500f;
					}
					if ((currentLane.m_LaneFlags & AircraftLaneFlags.Connection) != 0)
					{
						airplaneData.m_FlyingBraking = 277.77777f;
						navigation.m_MaxSpeed = VehicleUtils.GetMaxBrakingSpeed(airplaneData, num11, 0f, num + 0.5f);
					}
					else
					{
						float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(airplaneData, num11, prefabAircraftData.m_GroundMaxSpeed, num + 0.5f);
						navigation.m_MaxSpeed = MathUtils.Clamp(maxBrakingSpeed, bounds2);
					}
					blocker.m_Blocker = Entity.Null;
					blocker.m_Type = BlockerType.None;
					blocker.m_MaxSpeed = byte.MaxValue;
					if (currentLane.m_Lane == Entity.Null)
					{
						return;
					}
					num12 += navigation.m_MaxSpeed * num + 1f;
					currentLane.m_Duration += num;
					currentLane.m_Distance += num2 * num;
					odometer.m_Distance += num2 * num;
					if (num8 < num12)
					{
						while (true)
						{
							bool flag2 = (currentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0;
							if ((currentLane.m_LaneFlags & AircraftLaneFlags.Approaching) != 0)
							{
								if (flag2)
								{
									float3 float3 = MathUtils.Position(m_CurveData[currentLane.m_Lane].m_Bezier, currentLane.m_CurvePosition.x);
									num8 = math.length((float3 - transform.m_Position).xz);
									if (num8 >= num12)
									{
										num8 -= num12;
										navigation.m_TargetPosition.xz = float3.xz - navigation.m_TargetDirection.xz * num8;
										navigation.m_TargetPosition.y = math.min(navigation.m_TargetPosition.y, float3.y + num8 * math.tan(airplaneData.m_ClimbAngle));
										break;
									}
									currentLane.m_LaneFlags &= ~AircraftLaneFlags.Approaching;
									continue;
								}
								currentLane.m_LaneFlags |= AircraftLaneFlags.Landing;
								num12 = prefabAircraftData.m_GroundMaxSpeed;
								num12 += navigation.m_MaxSpeed * num + 1f;
								flag2 = true;
							}
							else if ((currentLane.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
							{
								if ((currentLane.m_LaneFlags & AircraftLaneFlags.Airway) != 0)
								{
									num8 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
									if (num8 >= num12)
									{
										break;
									}
									currentLane.m_LaneFlags &= ~AircraftLaneFlags.TakingOff;
									continue;
								}
							}
							else if ((currentLane.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0)
							{
								navigation.m_TargetDirection = default(float3);
								bool num13 = MoveTarget(position, ref navigation.m_TargetPosition, num12, currentLane.m_Lane);
								if (!flag2)
								{
									UpdateTargetHeight(ref navigation.m_TargetPosition, currentLane.m_Lane, airplaneData);
								}
								if (num13)
								{
									break;
								}
							}
							else
							{
								navigation.m_TargetDirection = default(float3);
								Curve curve2 = m_CurveData[currentLane.m_Lane];
								bool num14 = MoveTarget(position, ref navigation.m_TargetPosition, num12, curve2.m_Bezier, ref currentLane.m_CurvePosition);
								if (!flag2)
								{
									UpdateTargetHeight(ref navigation.m_TargetPosition, currentLane.m_Lane, airplaneData);
								}
								if (num14)
								{
									break;
								}
							}
							if (flag2)
							{
								if (navigationLanes.Length == 0)
								{
									break;
								}
							}
							else if (navigationLanes.Length == 0)
							{
								num8 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
								if (num8 < num2 * 2f)
								{
									currentLane.m_LaneFlags |= AircraftLaneFlags.EndReached;
								}
								break;
							}
							AircraftNavigationLane aircraftNavigationLane2 = navigationLanes[0];
							aircraftNavigationLane2.m_Flags |= AircraftLaneFlags.Flying;
							if ((currentLane.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
							{
								if ((currentLane.m_LaneFlags & AircraftLaneFlags.Runway) != 0)
								{
									aircraftNavigationLane2.m_Flags |= AircraftLaneFlags.TakingOff;
									if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.Airway) != 0)
									{
										float3 targetPosition = MathUtils.Position(m_CurveData[aircraftNavigationLane2.m_Lane].m_Bezier, aircraftNavigationLane2.m_CurvePosition.x);
										UpdateTargetHeight(ref targetPosition, aircraftNavigationLane2.m_Lane, airplaneData);
										num8 = math.length((targetPosition - navigation.m_TargetPosition).xz);
										float2 float4 = math.normalizesafe(moving.m_Velocity.xz);
										navigation.m_TargetDirection.y = 0f;
										navigation.m_TargetDirection.xz = float4;
										navigation.m_TargetPosition.xz = navigation.m_TargetPosition.xz + float4 * num8;
										navigation.m_TargetPosition.y = math.min(targetPosition.y, navigation.m_TargetPosition.y + num8 * math.tan(airplaneData.m_ClimbAngle));
									}
								}
							}
							else if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.Airway) == 0)
							{
								if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.Connection) != 0)
								{
									aircraftNavigationLane2.m_Flags |= AircraftLaneFlags.Landing;
								}
								else
								{
									if (!flag2)
									{
										currentLane.m_LaneFlags |= AircraftLaneFlags.Approaching;
										Curve curve3 = m_CurveData[aircraftNavigationLane2.m_Lane];
										float3 float5 = MathUtils.Position(curve3.m_Bezier, aircraftNavigationLane2.m_CurvePosition.x);
										float3 float6 = MathUtils.Position(curve3.m_Bezier, aircraftNavigationLane2.m_CurvePosition.y);
										float2 = float5 - navigation.m_TargetPosition;
										float2 value = float6.xz - float5.xz;
										if (!MathUtils.TryNormalize(ref value))
										{
											value = math.normalizesafe(float2.xz);
										}
										num8 = math.length(float2.xz);
										navigation.m_TargetDirection.y = 0f;
										navigation.m_TargetDirection.xz = value;
										navigation.m_TargetPosition.xz = float5.xz - value * num8;
										navigation.m_TargetPosition.y = math.min(navigation.m_TargetPosition.y, float5.y + num8 * math.tan(airplaneData.m_ClimbAngle));
										break;
									}
									aircraftNavigationLane2.m_Flags |= currentLane.m_LaneFlags & (AircraftLaneFlags.Approaching | AircraftLaneFlags.Landing);
								}
							}
							ApplySideEffects(ref currentLane, prefabRefData, airplaneData);
							currentLane.m_Lane = aircraftNavigationLane2.m_Lane;
							currentLane.m_CurvePosition = aircraftNavigationLane2.m_CurvePosition.xxy;
							currentLane.m_LaneFlags = aircraftNavigationLane2.m_Flags;
							navigationLanes.RemoveAt(0);
						}
					}
					if ((currentLane.m_LaneFlags & (AircraftLaneFlags.Approaching | AircraftLaneFlags.Landing)) == AircraftLaneFlags.Landing)
					{
						float2 = navigation.m_TargetPosition - transform.m_Position;
						if (transform.m_Position.y < navigation.m_TargetPosition.y + 1f)
						{
							currentLane.m_LaneFlags &= ~AircraftLaneFlags.Flying;
						}
					}
				}
			}
			else
			{
				if ((currentLane.m_LaneFlags & AircraftLaneFlags.Connection) != 0)
				{
					prefabAircraftData.m_GroundMaxSpeed = 277.77777f;
					prefabAircraftData.m_GroundAcceleration = 277.77777f;
					prefabAircraftData.m_GroundBraking = 277.77777f;
				}
				if (m_CurveData.HasComponent(currentLane.m_Lane))
				{
					Curve curve4 = m_CurveData[currentLane.m_Lane];
					PrefabRef prefabRef = m_PrefabRefData[currentLane.m_Lane];
					NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
					float2 value2 = MathUtils.Tangent(curve4.m_Bezier, currentLane.m_CurvePosition.x).xz;
					if (MathUtils.TryNormalize(ref value2))
					{
						position.xz -= MathUtils.Right(value2) * ((netLaneData.m_Width - prefabObjectGeometryData.m_Size.x) * currentLane.m_LanePosition * 0.5f);
					}
				}
				Bounds1 speedRange = (((currentLane.m_LaneFlags & (AircraftLaneFlags.Connection | AircraftLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(prefabAircraftData, num2, num) : new Bounds1(0f, prefabAircraftData.m_GroundMaxSpeed));
				AircraftLaneSpeedIterator laneIterator = new AircraftLaneSpeedIterator
				{
					m_TransformData = m_TransformData,
					m_MovingData = m_MovingDataFromEntity,
					m_AircraftData = m_AircraftData,
					m_LaneReservationData = m_LaneReservationData,
					m_CurveData = m_CurveData,
					m_CarLaneData = m_CarLaneData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabAircraftData = m_PrefabAircraftData,
					m_LaneOverlapData = m_LaneOverlaps,
					m_LaneObjectData = m_LaneObjects,
					m_Entity = entity,
					m_Ignore = (((currentLane.m_LaneFlags & AircraftLaneFlags.IgnoreBlocker) != 0) ? blocker.m_Blocker : Entity.Null),
					m_Priority = priority,
					m_TimeStep = num,
					m_SafeTimeStep = num + 0.5f,
					m_PrefabAircraft = prefabAircraftData,
					m_PrefabObjectGeometry = prefabObjectGeometryData,
					m_SpeedRange = speedRange,
					m_MaxSpeed = speedRange.max,
					m_CanChangeLane = 1f,
					m_CurrentPosition = position
				};
				if ((currentLane.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0)
				{
					laneIterator.IterateTarget(navigation.m_TargetPosition);
					navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
				}
				else
				{
					if (currentLane.m_Lane == Entity.Null)
					{
						navigation.m_MaxSpeed = math.max(0f, num2 - prefabAircraftData.m_GroundBraking * num);
						blocker.m_Blocker = Entity.Null;
						blocker.m_Type = BlockerType.None;
						blocker.m_MaxSpeed = byte.MaxValue;
						return;
					}
					if (!laneIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_CurvePosition))
					{
						int num15 = 0;
						while (true)
						{
							if (num15 < navigationLanes.Length)
							{
								AircraftNavigationLane aircraftNavigationLane3 = navigationLanes[num15];
								if ((aircraftNavigationLane3.m_Flags & AircraftLaneFlags.TransformTarget) == 0)
								{
									if ((aircraftNavigationLane3.m_Flags & AircraftLaneFlags.Connection) != 0)
									{
										laneIterator.m_PrefabAircraft.m_GroundMaxSpeed = 277.77777f;
										laneIterator.m_PrefabAircraft.m_GroundAcceleration = 277.77777f;
										laneIterator.m_PrefabAircraft.m_GroundBraking = 277.77777f;
										laneIterator.m_SpeedRange = new Bounds1(0f, 277.77777f);
									}
									else if ((currentLane.m_LaneFlags & AircraftLaneFlags.Connection) != 0)
									{
										goto IL_0ffd;
									}
									bool test = aircraftNavigationLane3.m_Lane == currentLane.m_Lane;
									float minOffset = math.select(-1f, currentLane.m_CurvePosition.y, test);
									if (laneIterator.IterateNextLane(aircraftNavigationLane3.m_Lane, aircraftNavigationLane3.m_CurvePosition, minOffset))
									{
										break;
									}
									num15++;
									continue;
								}
								VehicleUtils.CalculateTransformPosition(ref laneIterator.m_CurrentPosition, aircraftNavigationLane3.m_Lane, m_TransformData, m_PositionDataFromEntity, m_PrefabRefData, m_PrefabBuildingData);
							}
							goto IL_0ffd;
							IL_0ffd:
							laneIterator.IterateTarget(laneIterator.m_CurrentPosition);
							break;
						}
					}
					navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
					CheckBlocker(aircraft, isHelicopter, ref currentLane, ref blocker, ref laneIterator);
				}
				float num16 = math.length((navigation.m_TargetPosition - transform.m_Position).xz);
				float num17 = navigation.m_MaxSpeed * num + 1f;
				currentLane.m_Duration += num;
				currentLane.m_Distance += num2 * num;
				odometer.m_Distance += num2 * num;
				if (num16 < num17)
				{
					while (true)
					{
						if ((currentLane.m_LaneFlags & AircraftLaneFlags.TransformTarget) != 0)
						{
							navigation.m_TargetDirection = default(float3);
							if ((currentLane.m_LaneFlags & AircraftLaneFlags.EndReached) == 0 && MoveTarget(position, ref navigation.m_TargetPosition, num17, currentLane.m_Lane))
							{
								break;
							}
						}
						else
						{
							navigation.m_TargetDirection = default(float3);
							Curve curve5 = m_CurveData[currentLane.m_Lane];
							if (MoveTarget(position, ref navigation.m_TargetPosition, num17, curve5.m_Bezier, ref currentLane.m_CurvePosition))
							{
								ApplyLanePosition(ref navigation.m_TargetPosition, ref currentLane, prefabObjectGeometryData);
								break;
							}
						}
						if (navigationLanes.Length == 0)
						{
							if (m_CurveData.HasComponent(currentLane.m_Lane))
							{
								navigation.m_TargetDirection = MathUtils.Tangent(m_CurveData[currentLane.m_Lane].m_Bezier, currentLane.m_CurvePosition.z);
								ApplyLanePosition(ref navigation.m_TargetPosition, ref currentLane, prefabObjectGeometryData);
							}
							if (math.length((navigation.m_TargetPosition - transform.m_Position).xz) < 1f && num2 < 0.1f)
							{
								currentLane.m_LaneFlags |= AircraftLaneFlags.EndReached;
							}
							break;
						}
						AircraftNavigationLane aircraftNavigationLane4 = navigationLanes[0];
						if (!m_PrefabRefData.HasComponent(aircraftNavigationLane4.m_Lane))
						{
							break;
						}
						if ((currentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
						{
							if ((aircraftNavigationLane4.m_Flags & AircraftLaneFlags.Runway) != 0)
							{
								aircraftNavigationLane4.m_Flags |= AircraftLaneFlags.Landing;
							}
						}
						else if ((aircraftNavigationLane4.m_Flags & (AircraftLaneFlags.Runway | AircraftLaneFlags.Airway)) != 0)
						{
							aircraftNavigationLane4.m_Flags |= AircraftLaneFlags.TakingOff;
						}
						if ((currentLane.m_LaneFlags & AircraftLaneFlags.Connection) != 0 && (aircraftNavigationLane4.m_Flags & AircraftLaneFlags.Connection) == 0)
						{
							if (math.length((navigation.m_TargetPosition - transform.m_Position).xz) >= 1f || num2 > 3f)
							{
								break;
							}
							aircraftNavigationLane4.m_Flags |= AircraftLaneFlags.ResetSpeed;
						}
						ApplySideEffects(ref currentLane, prefabRefData, prefabAircraftData);
						currentLane.m_Lane = aircraftNavigationLane4.m_Lane;
						currentLane.m_CurvePosition = aircraftNavigationLane4.m_CurvePosition.xxy;
						currentLane.m_LaneFlags = aircraftNavigationLane4.m_Flags;
						navigationLanes.RemoveAt(0);
					}
				}
				if ((currentLane.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
				{
					if (isHelicopter)
					{
						HelicopterData helicopterData2 = m_PrefabHelicopterData[prefabRefData.m_Prefab];
						currentLane.m_LaneFlags |= AircraftLaneFlags.Flying;
						UpdateTargetHeight(ref navigation.m_TargetPosition, currentLane.m_Lane, helicopterData2);
					}
					else if (num2 >= m_PrefabAirplaneData[prefabRefData.m_Prefab].m_FlyingSpeed.x || (currentLane.m_LaneFlags & AircraftLaneFlags.Airway) != 0)
					{
						currentLane.m_LaneFlags |= AircraftLaneFlags.Flying;
					}
				}
			}
			if ((currentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
			{
				if (isHelicopter)
				{
					HelicopterData helicopterData3 = m_PrefabHelicopterData[prefabRefData.m_Prefab];
					float num18 = 0f;
					switch (helicopterData3.m_HelicopterType)
					{
					case HelicopterType.Helicopter:
						num18 = 100f;
						break;
					case HelicopterType.Rocket:
						num18 = 10000f;
						break;
					}
					if ((currentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
					{
						float3 targetPosition2 = navigation.m_TargetPosition;
						UpdateTargetHeight(ref targetPosition2, currentLane.m_Lane, helicopterData3);
						GetCollisionHeightTarget(entity, transform, ref navigation, ref blocker, prefabObjectGeometryData, targetPosition2);
						float3 float7 = targetPosition2 - transform.m_Position;
						float num19 = math.length(float7.xz);
						navigation.m_MinClimbAngle = math.atan(float7.y * 4f / num18);
						navigation.m_MinClimbAngle = math.min(navigation.m_MinClimbAngle, math.asin(math.saturate(num19 * 2f / num18 - 1f)));
					}
					else
					{
						float num20 = navigation.m_TargetPosition.y - transform.m_Position.y;
						navigation.m_MinClimbAngle = math.atan(num20 * 4f / num18) * 1.15f;
					}
				}
				else
				{
					AirplaneData airplaneData2 = m_PrefabAirplaneData[prefabRefData.m_Prefab];
					if ((currentLane.m_LaneFlags & (AircraftLaneFlags.Approaching | AircraftLaneFlags.Landing)) == (AircraftLaneFlags.Approaching | AircraftLaneFlags.Landing))
					{
						float3 targetPosition3 = navigation.m_TargetPosition;
						UpdateTargetHeight(ref targetPosition3, currentLane.m_Lane, airplaneData2);
						float3 float8 = navigation.m_TargetPosition - transform.m_Position;
						float num21 = math.max(0f, math.length(float8.xz) - prefabAircraftData.m_GroundMaxSpeed - navigation.m_MaxSpeed * num - 1f);
						float8.y += num21 * math.tan(airplaneData2.m_ClimbAngle);
						float8.y = math.min(float8.y, targetPosition3.y - transform.m_Position.y);
						navigation.m_MinClimbAngle = math.atan(float8.y * 0.02f) * airplaneData2.m_ClimbAngle / (MathF.PI / 2f);
					}
					else
					{
						float num22 = navigation.m_TargetPosition.y - transform.m_Position.y;
						navigation.m_MinClimbAngle = math.atan(num22 * 0.02f) * airplaneData2.m_ClimbAngle / (MathF.PI / 2f);
					}
				}
			}
			else
			{
				navigation.m_MinClimbAngle = -MathF.PI / 2f;
			}
		}

		private void GetCollisionHeightTarget(Entity entity, Game.Objects.Transform transform, ref AircraftNavigation navigation, ref Blocker blocker, ObjectGeometryData prefabObjectGeometryData, float3 targetPos)
		{
			AircraftNavigationHelpers.AircraftCollisionIterator iterator = new AircraftNavigationHelpers.AircraftCollisionIterator
			{
				m_Ignore = entity,
				m_Line = new Line3.Segment(transform.m_Position, navigation.m_TargetPosition),
				m_AircraftData = m_AircraftData,
				m_TransformData = m_TransformData,
				m_ClosestT = 2f
			};
			m_MovingObjectSearchTree.Iterate(ref iterator);
			if (iterator.m_Result != Entity.Null)
			{
				Game.Objects.Transform transform2 = m_TransformData[iterator.m_Result];
				PrefabRef prefabRef = m_PrefabRefData[iterator.m_Result];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				blocker.m_Blocker = iterator.m_Result;
				blocker.m_Type = BlockerType.Continuing;
				blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(navigation.m_MaxSpeed * 3.06f), 0, 255);
				float valueToClamp = transform2.m_Position.y + objectGeometryData.m_Bounds.max.y - prefabObjectGeometryData.m_Bounds.min.y + 50f;
				navigation.m_TargetPosition.y = math.clamp(valueToClamp, navigation.m_TargetPosition.y, targetPos.y);
			}
		}

		private void ApplyLanePosition(ref float3 targetPosition, ref AircraftCurrentLane currentLaneData, ObjectGeometryData prefabObjectGeometryData)
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

		private void ApplySideEffects(ref AircraftCurrentLane currentLaneData, PrefabRef prefabRefData, AircraftData prefabAircraftData)
		{
			if (m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				Game.Net.CarLane carLaneData = m_CarLaneData[currentLaneData.m_Lane];
				float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabAircraftData, carLaneData);
				float num = math.select(currentLaneData.m_Distance / currentLaneData.m_Duration, maxDriveSpeed, currentLaneData.m_Duration == 0f);
				float relativeSpeed = num / maxDriveSpeed;
				float3 sideEffects = default(float3);
				if (m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab))
				{
					VehicleSideEffectData vehicleSideEffectData = m_PrefabSideEffectData[prefabRefData.m_Prefab];
					float num2 = num / prefabAircraftData.m_GroundMaxSpeed;
					num2 = math.saturate(num2 * num2);
					sideEffects = math.lerp(vehicleSideEffectData.m_Min, vehicleSideEffectData.m_Max, num2);
					sideEffects *= new float3(currentLaneData.m_Distance, currentLaneData.m_Duration, currentLaneData.m_Duration);
				}
				m_LaneEffects.Enqueue(new AircraftNavigationHelpers.LaneEffects(currentLaneData.m_Lane, sideEffects, relativeSpeed));
			}
			currentLaneData.m_Duration = 0f;
			currentLaneData.m_Distance = 0f;
		}

		private void ApplySideEffects(ref AircraftCurrentLane currentLaneData, PrefabRef prefabRefData, HelicopterData prefabHelicopterData)
		{
			currentLaneData.m_Duration = 0f;
			currentLaneData.m_Distance = 0f;
		}

		private void ApplySideEffects(ref AircraftCurrentLane currentLaneData, PrefabRef prefabRefData, AirplaneData prefabAirplaneData)
		{
			currentLaneData.m_Duration = 0f;
			currentLaneData.m_Distance = 0f;
		}

		private void ReserveNavigationLanes(int priority, AircraftData prefabAircraftData, Aircraft watercraftData, ref AircraftNavigation navigationData, ref AircraftCurrentLane currentLaneData, DynamicBuffer<AircraftNavigationLane> navigationLanes)
		{
			float timeStep = 4f / 15f;
			if (m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				Curve curve = m_CurveData[currentLaneData.m_Lane];
				float num = math.max(0f, VehicleUtils.GetBrakingDistance(prefabAircraftData, navigationData.m_MaxSpeed, timeStep) - 0.01f);
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
					AircraftNavigationLane value = navigationLanes[num2];
					if (m_CarLaneData.HasComponent(value.m_Lane))
					{
						curve = m_CurveData[value.m_Lane];
						float offset = math.min(value.m_CurvePosition.y, value.m_CurvePosition.x + num / math.max(1E-06f, curve.m_Length));
						if (m_LaneReservationData.HasComponent(value.m_Lane))
						{
							m_LaneReservations.Enqueue(new AircraftNavigationHelpers.LaneReservation(value.m_Lane, offset, priority));
						}
						num -= curve.m_Length * math.abs(value.m_CurvePosition.y - value.m_CurvePosition.x);
						value.m_Flags |= AircraftLaneFlags.Reserved;
						navigationLanes[num2++] = value;
						continue;
					}
					break;
				}
			}
			else
			{
				currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.x;
			}
		}

		private void UpdateTargetHeight(ref float3 targetPosition, Entity target, HelicopterData helicopterData)
		{
			switch (helicopterData.m_HelicopterType)
			{
			case HelicopterType.Helicopter:
				targetPosition.y = 100f;
				break;
			case HelicopterType.Rocket:
				targetPosition.y = 10000f;
				break;
			}
			targetPosition.y += WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, targetPosition);
		}

		private void UpdateTargetHeight(ref float3 targetPosition, Entity target, AirplaneData airplaneData)
		{
			targetPosition.y = 1000f;
			targetPosition.y += WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, targetPosition);
		}

		private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Entity target)
		{
			if (VehicleUtils.CalculateTransformPosition(ref targetPosition, target, m_TransformData, m_PositionDataFromEntity, m_PrefabRefData, m_PrefabBuildingData))
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

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLaneReservationsJob : IJob
	{
		public NativeQueue<AircraftNavigationHelpers.LaneReservation> m_LaneReservationQueue;

		public ComponentLookup<LaneReservation> m_LaneReservationData;

		public void Execute()
		{
			AircraftNavigationHelpers.LaneReservation item;
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

		public NativeQueue<AircraftNavigationHelpers.LaneEffects> m_LaneEffectsQueue;

		public void Execute()
		{
			AircraftNavigationHelpers.LaneEffects item;
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
		public ComponentTypeHandle<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftNavigation> __Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;

		public BufferTypeHandle<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

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
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftData> __Game_Prefabs_AircraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HelicopterData> __Game_Prefabs_HelicopterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AirplaneData> __Game_Prefabs_AirplaneData_RO_ComponentLookup;

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
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RW_ComponentLookup;

		public ComponentLookup<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Vehicles_Aircraft_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Aircraft>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Helicopter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftNavigation>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
			__Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();
			__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_TakeoffLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TakeoffLocation>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_Aircraft_RO_ComponentLookup = state.GetComponentLookup<Aircraft>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AircraftData_RO_ComponentLookup = state.GetComponentLookup<AircraftData>(isReadOnly: true);
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Prefabs_AirplaneData_RO_ComponentLookup = state.GetComponentLookup<AirplaneData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup = state.GetComponentLookup<VehicleSideEffectData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Net_LaneReservation_RW_ComponentLookup = state.GetComponentLookup<LaneReservation>();
			__Game_Net_Pollution_RW_ComponentLookup = state.GetComponentLookup<Game.Net.Pollution>();
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private AirwaySystem m_AirwaySystem;

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
		return 10;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Aircraft>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<PathElement>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<AircraftNavigation>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		m_LaneObjectUpdater = new LaneObjectUpdater(this);
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<AircraftNavigationHelpers.LaneReservation> laneReservationQueue = new NativeQueue<AircraftNavigationHelpers.LaneReservation>(Allocator.TempJob);
		NativeQueue<AircraftNavigationHelpers.LaneEffects> laneEffectsQueue = new NativeQueue<AircraftNavigationHelpers.LaneEffects>(Allocator.TempJob);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle deps;
		UpdateNavigationJob jobData = new UpdateNavigationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HelicopterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OdometerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TakeoffLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TakeoffLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AircraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAirplaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AirplaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSideEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies2),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_AirwayData = m_AirwaySystem.GetAirwayData(),
			m_LaneObjectBuffer = m_LaneObjectUpdater.Begin(Allocator.TempJob),
			m_LaneReservations = laneReservationQueue.AsParallelWriter(),
			m_LaneEffects = laneEffectsQueue.AsParallelWriter()
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
		JobHandle job = JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(dependsOn: JobHandle.CombineDependencies(job, deps), jobData: jobData, query: m_VehicleQuery);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle);
		laneReservationQueue.Dispose(jobHandle2);
		laneEffectsQueue.Dispose(jobHandle3);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		JobHandle job2 = m_LaneObjectUpdater.Apply(this, jobHandle);
		base.Dependency = JobHandle.CombineDependencies(job2, jobHandle2, jobHandle3);
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
	public AircraftNavigationSystem()
	{
	}
}
