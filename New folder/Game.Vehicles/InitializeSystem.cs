using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Vehicles;

[CompilerGenerated]
public class InitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct TreeFixJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> m_CarTrailerLaneType;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public BufferLookup<LaneObject> m_LaneObjects;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CreatedType))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CarCurrentLane> nativeArray2 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
			NativeArray<CarTrailerLane> nativeArray3 = chunk.GetNativeArray(ref m_CarTrailerLaneType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				CarCurrentLane carCurrentLane = nativeArray2[i];
				if (m_LaneObjects.HasBuffer(carCurrentLane.m_Lane))
				{
					DynamicBuffer<LaneObject> buffer = m_LaneObjects[carCurrentLane.m_Lane];
					if (!CollectionUtils.ContainsValue(buffer, new LaneObject(entity)))
					{
						NetUtils.AddLaneObject(buffer, entity, carCurrentLane.m_CurvePosition.xy);
					}
					m_SearchTree.TryRemove(entity);
				}
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				CarTrailerLane carTrailerLane = nativeArray3[j];
				if (m_LaneObjects.HasBuffer(carTrailerLane.m_Lane))
				{
					DynamicBuffer<LaneObject> buffer2 = m_LaneObjects[carTrailerLane.m_Lane];
					if (!CollectionUtils.ContainsValue(buffer2, new LaneObject(entity2)))
					{
						NetUtils.AddLaneObject(buffer2, entity2, carTrailerLane.m_CurvePosition.xy);
					}
					m_SearchTree.TryRemove(entity2);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InitializeVehiclesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Train> m_TrainType;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> m_TripSourceType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> m_HelicopterType;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> m_BicycleType;

		[ReadOnly]
		public ComponentTypeHandle<Car> m_CarType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		public ComponentTypeHandle<CarNavigation> m_CarNavigationType;

		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		public ComponentTypeHandle<WatercraftNavigation> m_WatercraftNavigationType;

		public ComponentTypeHandle<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		public ComponentTypeHandle<AircraftNavigation> m_AircraftNavigationType;

		public ComponentTypeHandle<AircraftCurrentLane> m_AircraftCurrentLaneType;

		public ComponentTypeHandle<ParkedCar> m_ParkedCarType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<DeliveryTruck> m_DeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneLaneData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<CarTractorData> m_PrefabCarTractorData;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> m_PrefabCarTrailerData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TrainNavigation> m_TrainNavigationData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CarTrailerLane> m_CarTrailerLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<TrainBogieFrame> m_TrainBogieFrames;

		[NativeDisableParallelForRestriction]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<CarNavigation> nativeArray2 = chunk.GetNativeArray(ref m_CarNavigationType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<Car> nativeArray3 = chunk.GetNativeArray(ref m_CarType);
				NativeArray<CarCurrentLane> nativeArray4 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
				NativeArray<TripSource> nativeArray5 = chunk.GetNativeArray(ref m_TripSourceType);
				NativeArray<PathOwner> nativeArray6 = chunk.GetNativeArray(ref m_PathOwnerType);
				NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
				BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
				bool flag = chunk.Has(ref m_UnspawnedType);
				bool flag2 = chunk.Has(ref m_BicycleType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Car car = nativeArray3[i];
					CarCurrentLane carCurrentLane = nativeArray4[i];
					CarNavigation carNavigation = nativeArray2[i];
					PrefabRef prefabRef = nativeArray7[i];
					bool flag3 = math.asuint(carNavigation.m_MaxSpeed) >> 31 != 0 && (car.m_Flags & CarFlags.CannotReverse) != 0;
					bool flag4 = false;
					if (flag && nativeArray5.Length != 0)
					{
						TripSource tripSource = nativeArray5[i];
						PathOwner pathOwner = nativeArray6[i];
						DynamicBuffer<PathElement> path = bufferAccessor2[i];
						RoadTypes roadType = ((!flag2) ? RoadTypes.Car : RoadTypes.Bicycle);
						InitializeRoadVehicle(ref random, entity, roadType, tripSource, pathOwner, prefabRef, path);
						if (carCurrentLane.m_Lane == Entity.Null && path.Length > pathOwner.m_ElementIndex)
						{
							PathElement pathElement = path[pathOwner.m_ElementIndex];
							CarLaneFlags carLaneFlags = CarLaneFlags.FixedLane;
							if (m_ConnectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
							{
								carLaneFlags = (((componentData.m_Flags & ConnectionLaneFlags.Area) == 0) ? (carLaneFlags | CarLaneFlags.Connection) : (carLaneFlags | CarLaneFlags.Area));
							}
							carCurrentLane = new CarCurrentLane(pathElement, carLaneFlags);
						}
					}
					else if (flag3)
					{
						Transform value = m_TransformData[entity];
						CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
						float3 position = value.m_Position;
						if (carData.m_PivotOffset < 0f)
						{
							position += math.rotate(value.m_Rotation, new float3(0f, 0f, carData.m_PivotOffset));
						}
						float3 value2 = carNavigation.m_TargetPosition - position;
						if (MathUtils.TryNormalize(ref value2))
						{
							value.m_Rotation = quaternion.LookRotationSafe(value2, math.up());
							m_TransformData[entity] = value;
							ResetMeshBatches(entity);
							flag4 = true;
						}
					}
					if (((flag && nativeArray5.Length != 0) || flag3) && bufferAccessor.Length != 0)
					{
						Transform transform = m_TransformData[entity];
						DynamicBuffer<LayoutElement> dynamicBuffer = bufferAccessor[i];
						CarTractorData carTractorData = m_PrefabCarTractorData[prefabRef.m_Prefab];
						for (int j = 1; j < dynamicBuffer.Length; j++)
						{
							Entity vehicle = dynamicBuffer[j].m_Vehicle;
							CarTrailerLane carTrailerLane = m_CarTrailerLaneData[vehicle];
							PrefabRef prefabRef2 = m_PrefabRefData[vehicle];
							CarTrailerData carTrailerData = m_PrefabCarTrailerData[prefabRef2.m_Prefab];
							Transform transform2 = transform;
							transform2.m_Position += math.rotate(transform.m_Rotation, carTractorData.m_AttachPosition);
							transform2.m_Position -= math.rotate(transform2.m_Rotation, carTrailerData.m_AttachPosition);
							m_TransformData[vehicle] = transform2;
							if (carTrailerLane.m_Lane == Entity.Null)
							{
								m_CarTrailerLaneData[vehicle] = new CarTrailerLane(carCurrentLane);
							}
							if (flag4)
							{
								ResetMeshBatches(vehicle);
							}
							if (j + 1 < dynamicBuffer.Length)
							{
								transform = transform2;
								carTractorData = m_PrefabCarTractorData[prefabRef2.m_Prefab];
							}
						}
					}
					carCurrentLane.m_LanePosition = random.NextFloat(-0.25f, 0.25f);
					if (m_TransformData.HasComponent(carCurrentLane.m_Lane))
					{
						carCurrentLane.m_LaneFlags |= CarLaneFlags.TransformTarget;
					}
					nativeArray2[i] = new CarNavigation
					{
						m_TargetPosition = m_TransformData[entity].m_Position
					};
					nativeArray4[i] = carCurrentLane;
				}
				return;
			}
			NativeArray<WatercraftNavigation> nativeArray8 = chunk.GetNativeArray(ref m_WatercraftNavigationType);
			if (nativeArray8.Length != 0)
			{
				NativeArray<WatercraftCurrentLane> nativeArray9 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
				NativeArray<TripSource> nativeArray10 = chunk.GetNativeArray(ref m_TripSourceType);
				NativeArray<PathOwner> nativeArray11 = chunk.GetNativeArray(ref m_PathOwnerType);
				NativeArray<PrefabRef> nativeArray12 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<PathElement> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PathElementType);
				bool flag5 = chunk.Has(ref m_UnspawnedType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity2 = nativeArray[k];
					WatercraftCurrentLane value3 = nativeArray9[k];
					PrefabRef prefabRef3 = nativeArray12[k];
					WatercraftNavigation value4 = default(WatercraftNavigation);
					if (flag5 && nativeArray10.Length != 0)
					{
						TripSource tripSource2 = nativeArray10[k];
						PathOwner pathOwner2 = nativeArray11[k];
						DynamicBuffer<PathElement> path2 = bufferAccessor3[k];
						InitializeRoadVehicle(ref random, entity2, RoadTypes.Watercraft, tripSource2, pathOwner2, prefabRef3, path2);
						if (value3.m_Lane == Entity.Null && path2.Length > pathOwner2.m_ElementIndex)
						{
							PathElement pathElement2 = path2[pathOwner2.m_ElementIndex];
							WatercraftLaneFlags watercraftLaneFlags = WatercraftLaneFlags.FixedLane;
							if (m_ConnectionLaneData.TryGetComponent(pathElement2.m_Target, out var componentData2))
							{
								watercraftLaneFlags = (((componentData2.m_Flags & ConnectionLaneFlags.Area) == 0) ? (watercraftLaneFlags | WatercraftLaneFlags.Connection) : (watercraftLaneFlags | WatercraftLaneFlags.Area));
							}
							value3 = new WatercraftCurrentLane(pathElement2, watercraftLaneFlags);
						}
					}
					if (m_TransformData.HasComponent(value3.m_Lane))
					{
						value3.m_LaneFlags |= WatercraftLaneFlags.TransformTarget;
					}
					value4.m_TargetPosition = m_TransformData[entity2].m_Position;
					value4.m_TargetDirection = default(float3);
					nativeArray9[k] = value3;
					nativeArray8[k] = value4;
				}
				return;
			}
			NativeArray<AircraftNavigation> nativeArray13 = chunk.GetNativeArray(ref m_AircraftNavigationType);
			if (nativeArray13.Length != 0)
			{
				NativeArray<AircraftCurrentLane> nativeArray14 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
				NativeArray<TripSource> nativeArray15 = chunk.GetNativeArray(ref m_TripSourceType);
				NativeArray<PathOwner> nativeArray16 = chunk.GetNativeArray(ref m_PathOwnerType);
				NativeArray<PrefabRef> nativeArray17 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<PathElement> bufferAccessor4 = chunk.GetBufferAccessor(ref m_PathElementType);
				bool flag6 = chunk.Has(ref m_UnspawnedType);
				bool flag7 = chunk.Has(ref m_HelicopterType);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Entity entity3 = nativeArray[l];
					AircraftCurrentLane value5 = nativeArray14[l];
					PrefabRef prefabRef4 = nativeArray17[l];
					AircraftNavigation value6 = default(AircraftNavigation);
					if (flag6 && nativeArray15.Length != 0)
					{
						PathOwner pathOwner3 = nativeArray16[l];
						DynamicBuffer<PathElement> path3 = bufferAccessor4[l];
						InitializeRoadVehicle(ref random, entity3, flag7 ? RoadTypes.Helicopter : RoadTypes.Airplane, nativeArray15[l], pathOwner3, prefabRef4, path3);
						if (value5.m_Lane == Entity.Null && path3.Length > pathOwner3.m_ElementIndex)
						{
							PathElement pathElement3 = path3[pathOwner3.m_ElementIndex];
							AircraftLaneFlags aircraftLaneFlags = (AircraftLaneFlags)0u;
							if (m_ConnectionLaneData.HasComponent(pathElement3.m_Target))
							{
								aircraftLaneFlags |= AircraftLaneFlags.Connection;
							}
							value5 = new AircraftCurrentLane(pathElement3, aircraftLaneFlags);
						}
					}
					if (m_TransformData.HasComponent(value5.m_Lane))
					{
						value5.m_LaneFlags |= AircraftLaneFlags.TransformTarget;
						if (m_SpawnLocationData.HasComponent(value5.m_Lane))
						{
							PrefabRef prefabRef5 = m_PrefabRefData[value5.m_Lane];
							if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef5.m_Prefab, out var componentData3) && componentData3.m_ConnectionType == RouteConnectionType.Air)
							{
								value5.m_LaneFlags |= AircraftLaneFlags.ParkingSpace;
							}
						}
					}
					value6.m_TargetPosition = m_TransformData[entity3].m_Position;
					value6.m_TargetDirection = default(float3);
					nativeArray14[l] = value5;
					nativeArray13[l] = value6;
				}
				return;
			}
			NativeArray<ParkedCar> nativeArray18 = chunk.GetNativeArray(ref m_ParkedCarType);
			if (nativeArray18.Length != 0)
			{
				NativeArray<TripSource> nativeArray19 = chunk.GetNativeArray(ref m_TripSourceType);
				NativeArray<PrefabRef> nativeArray20 = chunk.GetNativeArray(ref m_PrefabRefType);
				if (!chunk.Has(ref m_UnspawnedType))
				{
					return;
				}
				for (int m = 0; m < nativeArray19.Length; m++)
				{
					Entity entity4 = nativeArray[m];
					ParkedCar value7 = nativeArray18[m];
					TripSource tripSource3 = nativeArray19[m];
					PrefabRef prefabRef6 = nativeArray20[m];
					if (!m_TransformData.HasComponent(tripSource3.m_Source))
					{
						continue;
					}
					float3 position2 = m_TransformData[tripSource3.m_Source].m_Position;
					if (FindParkingSpace(position2, tripSource3.m_Source, ref random, out value7.m_Lane, out value7.m_CurvePosition))
					{
						if (m_CurveData.HasComponent(value7.m_Lane))
						{
							Curve curve = m_CurveData[value7.m_Lane];
							if (m_ParkingLaneData.HasComponent(value7.m_Lane))
							{
								Game.Net.ParkingLane parkingLane = m_ParkingLaneData[value7.m_Lane];
								PrefabRef prefabRef7 = m_PrefabRefData[value7.m_Lane];
								ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef7.m_Prefab];
								ObjectGeometryData prefabGeometryData = m_PrefabObjectGeometryData[prefabRef6.m_Prefab];
								Transform ownerTransform = default(Transform);
								if (m_OwnerData.TryGetComponent(value7.m_Lane, out var componentData4) && m_TransformData.HasComponent(componentData4.m_Owner))
								{
									ownerTransform = m_TransformData[componentData4.m_Owner];
								}
								m_TransformData[entity4] = VehicleUtils.CalculateParkingSpaceTarget(parkingLane, parkingLaneData, prefabGeometryData, curve, ownerTransform, value7.m_CurvePosition);
							}
							else
							{
								Transform value8 = VehicleUtils.CalculateTransform(curve, value7.m_CurvePosition);
								if (m_ConnectionLaneData.HasComponent(value7.m_Lane))
								{
									Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[value7.m_Lane];
									if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
									{
										value7.m_CurvePosition = random.NextFloat(0f, 1f);
										value8.m_Position = VehicleUtils.GetConnectionParkingPosition(connectionLane, curve.m_Bezier, value7.m_CurvePosition);
									}
								}
								m_TransformData[entity4] = value8;
							}
						}
					}
					else
					{
						Transform value9 = m_TransformData[entity4];
						value7.m_CurvePosition = random.NextFloat(0f, 1f);
						Game.Net.ConnectionLane connectionLane2 = default(Game.Net.ConnectionLane);
						if (m_OutsideConnectionData.HasComponent(tripSource3.m_Source))
						{
							connectionLane2.m_Flags |= ConnectionLaneFlags.Outside;
						}
						value9.m_Position = VehicleUtils.GetConnectionParkingPosition(connectionLane2, new Bezier4x3(position2, position2, position2, position2), value7.m_CurvePosition);
						m_TransformData[entity4] = value9;
					}
					nativeArray18[m] = value7;
				}
				return;
			}
			NativeArray<Train> nativeArray21 = chunk.GetNativeArray(ref m_TrainType);
			if (nativeArray21.Length == 0)
			{
				return;
			}
			NativeArray<TripSource> nativeArray22 = chunk.GetNativeArray(ref m_TripSourceType);
			NativeArray<PathOwner> nativeArray23 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<PrefabRef> nativeArray24 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<LayoutElement> bufferAccessor5 = chunk.GetBufferAccessor(ref m_LayoutElementType);
			BufferAccessor<PathElement> bufferAccessor6 = chunk.GetBufferAccessor(ref m_PathElementType);
			NativeList<PathElement> laneBuffer = new NativeList<PathElement>(10, Allocator.Temp);
			bool flag8 = chunk.Has(ref m_UnspawnedType);
			bool flag9 = chunk.Has(ref m_TempType);
			for (int n = 0; n < nativeArray21.Length; n++)
			{
				Entity entity5 = nativeArray[n];
				ParkedTrain componentData5;
				bool flag10 = m_ParkedTrainData.TryGetComponent(entity5, out componentData5);
				DynamicBuffer<LayoutElement> value10;
				bool flag11 = CollectionUtils.TryGet(bufferAccessor5, n, out value10);
				if ((flag10 && flag9) || (flag8 && nativeArray22.Length != 0))
				{
					if (flag11)
					{
						PathOwner pathOwner4 = default(PathOwner);
						DynamicBuffer<PathElement> path4 = default(DynamicBuffer<PathElement>);
						if (!flag10)
						{
							pathOwner4 = nativeArray23[n];
							path4 = bufferAccessor6[n];
						}
						float length = VehicleUtils.CalculateLength(entity5, value10, ref m_PrefabRefData, ref m_PrefabTrainData);
						PathUtils.InitializeSpawnPath(path4, laneBuffer, componentData5.m_ParkingLocation, ref pathOwner4, length, ref m_CurveData, ref m_LaneData, ref m_EdgeLaneData, ref m_OwnerData, ref m_EdgeData, ref m_SpawnLocationData, ref m_ConnectedEdges, ref m_SubLanes);
						VehicleUtils.UpdateCarriageLocations(value10, laneBuffer, ref m_TrainData, ref m_ParkedTrainData, ref m_TrainCurrentLaneData, ref m_TrainNavigationData, ref m_TransformData, ref m_CurveData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabTrainData);
						if (!flag10)
						{
							nativeArray23[n] = pathOwner4;
						}
						laneBuffer.Clear();
					}
				}
				else if (m_TrainNavigationData.HasComponent(entity5))
				{
					Train train = nativeArray21[n];
					Transform transform3 = m_TransformData[entity5];
					PrefabRef prefabRef8 = nativeArray24[n];
					TrainData prefabTrainData = m_PrefabTrainData[prefabRef8.m_Prefab];
					VehicleUtils.CalculateTrainNavigationPivots(transform3, prefabTrainData, out var pivot, out var pivot2);
					if ((train.m_Flags & TrainFlags.Reversed) != 0)
					{
						CommonUtils.Swap(ref pivot, ref pivot2);
					}
					TrainNavigation value11 = default(TrainNavigation);
					value11.m_Front = new TrainBogiePosition(transform3);
					value11.m_Rear = new TrainBogiePosition(transform3);
					value11.m_Front.m_Position = pivot;
					value11.m_Rear.m_Position = pivot2;
					m_TrainNavigationData[entity5] = value11;
				}
				if (flag11)
				{
					UpdateBogieFrames(value10);
				}
			}
		}

		private void ResetMeshBatches(Entity entity)
		{
			if (m_MeshBatches.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					MeshBatch value = bufferData[i];
					value.m_MeshGroup = byte.MaxValue;
					value.m_MeshIndex = byte.MaxValue;
					value.m_TileIndex = byte.MaxValue;
					bufferData[i] = value;
				}
			}
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					ResetMeshBatches(bufferData2[j].m_SubObject);
				}
			}
		}

		private void InitializeRoadVehicle(ref Random random, Entity vehicle, RoadTypes roadType, TripSource tripSource, PathOwner pathOwner, PrefabRef prefabRef, DynamicBuffer<PathElement> path)
		{
			PathMethod pathMethod = ((roadType == RoadTypes.Bicycle) ? PathMethod.Bicycle : PathMethod.Road);
			if (m_SpawnLocations.HasBuffer(tripSource.m_Source))
			{
				DynamicBuffer<SpawnLocationElement> spawnLocations = m_SpawnLocations[tripSource.m_Source];
				bool positionFound;
				bool rotationFound;
				Transform transform = CalculatePathTransform(vehicle, pathOwner, path, roadType, out positionFound, out rotationFound);
				if (!positionFound || !rotationFound)
				{
					if ((roadType & RoadTypes.Car) != RoadTypes.None && m_DeliveryTruckData.HasComponent(vehicle))
					{
						pathMethod |= PathMethod.CargoLoading;
					}
					Transform transform2 = m_TransformData[tripSource.m_Source];
					bool positionFound2;
					bool rotationFound2;
					Transform transform3 = FindClosestSpawnLocation(ref random, transform, pathMethod, TrackTypes.None, roadType, spawnLocations, transform2.Equals(transform), out positionFound2, out rotationFound2);
					if (!positionFound && positionFound2)
					{
						transform.m_Position = transform3.m_Position;
						positionFound = true;
					}
					if (!rotationFound && rotationFound2)
					{
						transform.m_Rotation = transform3.m_Rotation;
						rotationFound = true;
					}
				}
				if (!rotationFound)
				{
					Transform transform4 = m_TransformData[tripSource.m_Source];
					PrefabRef prefabRef2 = m_PrefabRefData[tripSource.m_Source];
					float3 value;
					if (m_PrefabBuildingData.HasComponent(prefabRef2.m_Prefab))
					{
						BuildingData buildingData = m_PrefabBuildingData[prefabRef2.m_Prefab];
						value = BuildingUtils.CalculateFrontPosition(transform4, buildingData.m_LotSize.y) - transform.m_Position;
					}
					else
					{
						value = transform4.m_Position - transform.m_Position;
					}
					if (MathUtils.TryNormalize(ref value))
					{
						transform.m_Rotation = quaternion.LookRotationSafe(value, math.up());
						rotationFound = true;
					}
					if (!positionFound)
					{
						transform.m_Position = transform4.m_Position;
					}
					if (!rotationFound)
					{
						transform.m_Rotation = transform4.m_Rotation;
					}
				}
				m_TransformData[vehicle] = transform;
			}
			else if (m_RouteLaneData.HasComponent(tripSource.m_Source))
			{
				Transform value2 = m_TransformData[vehicle];
				RouteLane routeLane = m_RouteLaneData[tripSource.m_Source];
				float3 position = value2.m_Position;
				if (m_MasterLaneLaneData.TryGetComponent(routeLane.m_EndLane, out var componentData) && m_OwnerData.TryGetComponent(routeLane.m_EndLane, out var componentData2) && m_SubLanes.TryGetBuffer(componentData2.m_Owner, out var bufferData))
				{
					int index = NetUtils.ChooseClosestLane(componentData.m_MinIndex, componentData.m_MaxIndex, position, pathMethod, bufferData, ref m_CurveData, routeLane.m_EndCurvePos);
					routeLane.m_EndLane = bufferData[index].m_SubLane;
				}
				if (!m_CurveData.TryGetComponent(routeLane.m_EndLane, out var componentData3))
				{
					return;
				}
				value2.m_Position = MathUtils.Position(componentData3.m_Bezier, routeLane.m_EndCurvePos);
				float3 value3 = MathUtils.Tangent(componentData3.m_Bezier, routeLane.m_EndCurvePos);
				if (MathUtils.TryNormalize(ref value3))
				{
					value2.m_Rotation = quaternion.LookRotationSafe(value3, math.up());
					if (m_PrefabRefData.TryGetComponent(routeLane.m_EndLane, out var componentData4) && m_PrefabNetLaneData.TryGetComponent(componentData4.m_Prefab, out var componentData5) && m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData6))
					{
						float2 @float = MathUtils.Right(value3.xz);
						@float = math.select(@float, -@float, math.dot(@float, position.xz - value2.m_Position.xz) < 0f);
						value2.m_Position.xz += @float * ((componentData5.m_Width - componentData6.m_Size.x) * 0.5f);
					}
				}
				m_TransformData[vehicle] = value2;
			}
			else
			{
				if (!m_TransformData.HasComponent(tripSource.m_Source))
				{
					return;
				}
				bool positionFound3;
				bool rotationFound3;
				Transform value4 = CalculatePathTransform(vehicle, pathOwner, path, roadType, out positionFound3, out rotationFound3);
				if (!positionFound3 && !rotationFound3 && m_OutsideConnectionData.HasComponent(tripSource.m_Source))
				{
					bool positionFound4;
					bool rotationFound4;
					Transform transform5 = FindRandomConnectionLocation(ref random, roadType, tripSource.m_Source, out positionFound4, out rotationFound4);
					if (!positionFound3 && positionFound4)
					{
						value4.m_Position = transform5.m_Position;
						positionFound3 = true;
					}
					if (!rotationFound3 && rotationFound4)
					{
						value4.m_Rotation = transform5.m_Rotation;
						rotationFound3 = true;
					}
				}
				if (!rotationFound3)
				{
					Transform transform6 = m_TransformData[tripSource.m_Source];
					float3 value5 = transform6.m_Position - value4.m_Position;
					if (MathUtils.TryNormalize(ref value5))
					{
						value4.m_Rotation = quaternion.LookRotationSafe(value5, math.up());
						rotationFound3 = true;
					}
					if (!positionFound3)
					{
						value4.m_Position = transform6.m_Position;
					}
					if (!rotationFound3)
					{
						value4.m_Rotation = transform6.m_Rotation;
					}
				}
				m_TransformData[vehicle] = value4;
			}
		}

		private Transform FindRandomConnectionLocation(ref Random random, RoadTypes roadType, Entity source, out bool positionFound, out bool rotationFound)
		{
			Transform result = default(Transform);
			positionFound = false;
			rotationFound = false;
			int num = 0;
			float3 value = default(float3);
			if (m_OwnerData.TryGetComponent(source, out var componentData) && m_SubLanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Game.Net.SubLane subLane = bufferData[i];
					if (m_ConnectionLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2) && (componentData2.m_Flags & ConnectionLaneFlags.Road) != 0 && (componentData2.m_RoadTypes & roadType) != RoadTypes.None && random.NextInt(++num) == 0)
					{
						Curve curve = m_CurveData[subLane.m_SubLane];
						result.m_Position = MathUtils.Position(curve.m_Bezier, 0.5f);
						value = MathUtils.Tangent(curve.m_Bezier, 0.5f);
						positionFound = true;
					}
				}
			}
			if (m_SubLanes.TryGetBuffer(source, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Game.Net.SubLane subLane2 = bufferData2[j];
					if (m_ConnectionLaneData.TryGetComponent(subLane2.m_SubLane, out var componentData3) && (componentData3.m_Flags & ConnectionLaneFlags.Road) != 0 && (componentData3.m_RoadTypes & roadType) != RoadTypes.None && random.NextInt(++num) == 0)
					{
						Curve curve2 = m_CurveData[subLane2.m_SubLane];
						result.m_Position = MathUtils.Position(curve2.m_Bezier, 0.5f);
						value = MathUtils.Tangent(curve2.m_Bezier, 0.5f);
						positionFound = true;
					}
				}
			}
			if (positionFound && MathUtils.TryNormalize(ref value))
			{
				result.m_Rotation = quaternion.LookRotationSafe(-value, math.up());
				rotationFound = true;
			}
			return result;
		}

		private void UpdateBogieFrames(DynamicBuffer<LayoutElement> layout)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				if (m_TrainCurrentLaneData.TryGetComponent(vehicle, out var componentData))
				{
					DynamicBuffer<TrainBogieFrame> dynamicBuffer = m_TrainBogieFrames[vehicle];
					dynamicBuffer.ResizeUninitialized(4);
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						dynamicBuffer[j] = new TrainBogieFrame
						{
							m_FrontLane = componentData.m_Front.m_Lane,
							m_RearLane = componentData.m_Rear.m_Lane
						};
					}
				}
			}
		}

		private Transform CalculatePathTransform(Entity vehicle, PathOwner pathOwner, DynamicBuffer<PathElement> path, RoadTypes roadType, out bool positionFound, out bool rotationFound)
		{
			Transform result = m_TransformData[vehicle];
			positionFound = false;
			rotationFound = false;
			for (int i = pathOwner.m_ElementIndex; i < path.Length; i++)
			{
				PathElement pathElement = path[i];
				if (m_TransformData.HasComponent(pathElement.m_Target))
				{
					float3 position = m_TransformData[pathElement.m_Target].m_Position;
					if (positionFound)
					{
						float3 value = position - result.m_Position;
						if ((roadType & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
						{
							value.y = 0f;
						}
						if (MathUtils.TryNormalize(ref value))
						{
							result.m_Rotation = quaternion.LookRotationSafe(value, math.up());
							rotationFound = true;
							return result;
						}
					}
					else
					{
						result.m_Position = position;
						positionFound = true;
					}
				}
				else
				{
					if (!m_CurveData.HasComponent(pathElement.m_Target))
					{
						continue;
					}
					Curve curve = m_CurveData[pathElement.m_Target];
					float3 @float = MathUtils.Position(curve.m_Bezier, pathElement.m_TargetDelta.x);
					if (positionFound)
					{
						float3 value2 = @float - result.m_Position;
						if ((roadType & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
						{
							value2.y = 0f;
						}
						if (MathUtils.TryNormalize(ref value2))
						{
							result.m_Rotation = quaternion.LookRotationSafe(value2, math.up());
							rotationFound = true;
							return result;
						}
					}
					else
					{
						result.m_Position = @float;
						positionFound = true;
					}
					if (pathElement.m_TargetDelta.x != pathElement.m_TargetDelta.y)
					{
						float3 float2 = MathUtils.Tangent(curve.m_Bezier, pathElement.m_TargetDelta.x);
						float2 = math.select(float2, -float2, pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x);
						if ((roadType & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
						{
							float2.y = 0f;
						}
						if (MathUtils.TryNormalize(ref float2))
						{
							result.m_Rotation = quaternion.LookRotationSafe(float2, math.up());
							rotationFound = true;
							return result;
						}
					}
				}
			}
			return result;
		}

		private Transform FindClosestSpawnLocation(ref Random random, Transform compareTransform, PathMethod pathMethods, TrackTypes trackTypes, RoadTypes roadTypes, DynamicBuffer<SpawnLocationElement> spawnLocations, bool selectRandom, out bool positionFound, out bool rotationFound)
		{
			Transform result = compareTransform;
			positionFound = false;
			rotationFound = false;
			Entity entity = Entity.Null;
			float num = float.MaxValue;
			int num2 = 0;
			for (int i = 0; i < spawnLocations.Length; i++)
			{
				if (spawnLocations[i].m_Type != SpawnLocationType.SpawnLocation)
				{
					continue;
				}
				Entity spawnLocation = spawnLocations[i].m_SpawnLocation;
				PrefabRef prefabRef = m_PrefabRefData[spawnLocation];
				if (!m_PrefabSpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					continue;
				}
				if (componentData.m_ConnectionType != RouteConnectionType.Air || (pathMethods & PathMethod.Road) == 0 || (roadTypes & componentData.m_RoadTypes) == 0)
				{
					PathMethod pathMethod = componentData.m_ConnectionType switch
					{
						RouteConnectionType.Pedestrian => pathMethods & (PathMethod.Pedestrian | PathMethod.Bicycle), 
						RouteConnectionType.Cargo => pathMethods & PathMethod.CargoLoading, 
						RouteConnectionType.Road => pathMethods & PathMethod.Road, 
						RouteConnectionType.Track => pathMethods & PathMethod.Track, 
						RouteConnectionType.Air => pathMethods & PathMethod.Flying, 
						RouteConnectionType.Offroad => pathMethods & PathMethod.Offroad, 
						RouteConnectionType.Parking => (componentData.m_RoadTypes == RoadTypes.Bicycle) ? (pathMethods & PathMethod.Pedestrian) : (~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking)), 
						_ => ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking), 
					};
					if (pathMethod == ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking))
					{
						continue;
					}
					if (componentData.m_ConnectionType == RouteConnectionType.Pedestrian && componentData.m_ActivityMask.m_Mask == 0)
					{
						componentData.m_RoadTypes |= RoadTypes.Bicycle;
					}
					TrackTypes trackTypes2 = trackTypes & componentData.m_TrackTypes;
					RoadTypes roadTypes2 = roadTypes & componentData.m_RoadTypes;
					if (((pathMethod & PathMethod.Track) == 0 || trackTypes2 == TrackTypes.None) && ((pathMethod & (PathMethod.Road | PathMethod.CargoLoading | PathMethod.Offroad | PathMethod.Bicycle)) == 0 || roadTypes2 == RoadTypes.None))
					{
						continue;
					}
				}
				if (!m_TransformData.TryGetComponent(spawnLocation, out var componentData2))
				{
					continue;
				}
				if (selectRandom)
				{
					if (random.NextInt(++num2) == 0)
					{
						result.m_Position = componentData2.m_Position;
						positionFound = true;
						entity = spawnLocation;
					}
					continue;
				}
				float num3 = math.distance(componentData2.m_Position, compareTransform.m_Position);
				if (num3 < num)
				{
					result.m_Position = componentData2.m_Position;
					positionFound = true;
					entity = spawnLocation;
					num = num3;
				}
			}
			if (m_SpawnLocationData.HasComponent(entity))
			{
				Game.Objects.SpawnLocation spawnLocation2 = m_SpawnLocationData[entity];
				if (m_CurveData.HasComponent(spawnLocation2.m_ConnectedLane1))
				{
					Curve curve = m_CurveData[spawnLocation2.m_ConnectedLane1];
					float3 value = MathUtils.Position(curve.m_Bezier, spawnLocation2.m_CurvePosition1) - result.m_Position;
					if ((roadTypes & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
					{
						value.y = 0f;
					}
					if (MathUtils.TryNormalize(ref value))
					{
						result.m_Rotation = quaternion.LookRotationSafe(value, math.up());
						rotationFound = true;
						return result;
					}
					float3 value2 = MathUtils.Tangent(curve.m_Bezier, spawnLocation2.m_CurvePosition1);
					if ((roadTypes & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
					{
						value2.y = 0f;
					}
					if (MathUtils.TryNormalize(ref value2))
					{
						result.m_Rotation = quaternion.LookRotationSafe(value2, math.up());
						rotationFound = true;
						return result;
					}
				}
			}
			if (positionFound)
			{
				float3 value3 = result.m_Position - compareTransform.m_Position;
				if ((roadTypes & (RoadTypes.Watercraft | RoadTypes.Helicopter)) != RoadTypes.None)
				{
					value3.y = 0f;
				}
				if (MathUtils.TryNormalize(ref value3))
				{
					result.m_Rotation = quaternion.LookRotationSafe(value3, math.up());
					rotationFound = true;
					return result;
				}
			}
			return result;
		}

		private bool FindParkingSpace(float3 comparePosition, Entity source, ref Random random, out Entity lane, out float curvePos)
		{
			while (true)
			{
				if (m_SpawnLocations.HasBuffer(source))
				{
					DynamicBuffer<SpawnLocationElement> dynamicBuffer = m_SpawnLocations[source];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						if (dynamicBuffer[i].m_Type != SpawnLocationType.SpawnLocation)
						{
							continue;
						}
						Entity spawnLocation = dynamicBuffer[i].m_SpawnLocation;
						PrefabRef prefabRef = m_PrefabRefData[spawnLocation];
						if (m_PrefabSpawnLocationData[prefabRef.m_Prefab].m_ConnectionType != RouteConnectionType.Road || !m_SpawnLocationData.HasComponent(spawnLocation))
						{
							continue;
						}
						Game.Objects.SpawnLocation spawnLocation2 = m_SpawnLocationData[spawnLocation];
						if (m_OwnerData.HasComponent(spawnLocation2.m_ConnectedLane1))
						{
							Owner owner = m_OwnerData[spawnLocation2.m_ConnectedLane1];
							if (m_SubLanes.HasBuffer(owner.m_Owner) && FindParkingSpace(comparePosition, m_SubLanes[owner.m_Owner], ref random, out lane, out curvePos))
							{
								return true;
							}
						}
					}
				}
				if (m_SubLanes.HasBuffer(source) && FindParkingSpace(comparePosition, m_SubLanes[source], ref random, out lane, out curvePos))
				{
					return true;
				}
				if (m_BuildingData.HasComponent(source))
				{
					Building building = m_BuildingData[source];
					if (m_SubLanes.HasBuffer(building.m_RoadEdge) && FindParkingSpace(comparePosition, m_SubLanes[building.m_RoadEdge], ref random, out lane, out curvePos))
					{
						return true;
					}
				}
				if (!m_OwnerData.HasComponent(source))
				{
					break;
				}
				source = m_OwnerData[source].m_Owner;
			}
			lane = Entity.Null;
			curvePos = 0f;
			return false;
		}

		private bool FindParkingSpace(float3 comparePosition, DynamicBuffer<Game.Net.SubLane> lanes, ref Random random, out Entity lane, out float curvePos)
		{
			lane = Entity.Null;
			curvePos = 0f;
			float num = float.MaxValue;
			for (int i = 0; i < lanes.Length; i++)
			{
				Entity subLane = lanes[i].m_SubLane;
				if (m_ParkingLaneData.HasComponent(subLane))
				{
					float t;
					float num2 = MathUtils.Distance(m_CurveData[subLane].m_Bezier, comparePosition, out t);
					if (num2 < num)
					{
						num = num2;
						curvePos = t;
						lane = subLane;
					}
				}
				else if (m_ConnectionLaneData.HasComponent(subLane) && (m_ConnectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Parking) != 0)
				{
					float t2;
					float num3 = MathUtils.Distance(m_CurveData[subLane].m_Bezier, comparePosition, out t2);
					if (num3 < num)
					{
						num = num3;
						curvePos = random.NextFloat(0f, 1f);
						lane = subLane;
					}
				}
			}
			curvePos = math.clamp(curvePos, 0.05f, 0.95f);
			curvePos = random.NextFloat(math.max(0.05f, curvePos - 0.2f), math.min(0.95f, curvePos + 0.2f));
			return lane != Entity.Null;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Train> __Game_Vehicles_Train_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<CarNavigation> __Game_Vehicles_CarNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WatercraftNavigation> __Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftNavigation> __Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ParkedCar> __Game_Vehicles_ParkedCar_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTractorData> __Game_Prefabs_CarTractorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> __Game_Prefabs_CarTrailerData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RW_ComponentLookup;

		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RW_ComponentLookup;

		public ComponentLookup<TrainNavigation> __Game_Vehicles_TrainNavigation_RW_ComponentLookup;

		public ComponentLookup<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RW_ComponentLookup;

		public BufferLookup<TrainBogieFrame> __Game_Vehicles_TrainBogieFrame_RW_BufferLookup;

		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Train_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Train>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Helicopter>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Bicycle>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Car>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_CarNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarNavigation>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftNavigation>();
			__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>();
			__Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftNavigation>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>();
			__Game_Vehicles_ParkedCar_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedCar>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruck>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_CarTractorData_RO_ComponentLookup = state.GetComponentLookup<CarTractorData>(isReadOnly: true);
			__Game_Prefabs_CarTrailerData_RO_ComponentLookup = state.GetComponentLookup<CarTrailerData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>();
			__Game_Vehicles_ParkedTrain_RW_ComponentLookup = state.GetComponentLookup<ParkedTrain>();
			__Game_Vehicles_TrainNavigation_RW_ComponentLookup = state.GetComponentLookup<TrainNavigation>();
			__Game_Vehicles_CarTrailerLane_RW_ComponentLookup = state.GetComponentLookup<CarTrailerLane>();
			__Game_Vehicles_TrainBogieFrame_RW_BufferLookup = state.GetBufferLookup<TrainBogieFrame>();
			__Game_Rendering_MeshBatch_RW_BufferLookup = state.GetBufferLookup<MeshBatch>();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarTrailerLane>(isReadOnly: true);
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
		}
	}

	private Game.Objects.SearchSystem m_SearchSystem;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Vehicle>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitializeVehiclesJob jobData = new InitializeVehiclesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HelicopterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkedCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedCar_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarTractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTractorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarTrailerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTrailerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TrainNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainNavigation_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TrainBogieFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_TrainBogieFrame_RW_BufferLookup, ref base.CheckedStateRef),
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RW_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next()
		};
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new TreeFixJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarTrailerLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SearchTree = m_SearchSystem.GetMovingSearchTree(readOnly: false, out dependencies),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef)
		}, dependsOn: JobHandle.CombineDependencies(JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency), dependencies), query: m_VehicleQuery);
		m_SearchSystem.AddMovingSearchTreeWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public InitializeSystem()
	{
	}
}
