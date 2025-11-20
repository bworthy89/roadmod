using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
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
public class ReferencesSystem : GameSystemBase
{
	private struct ResourceNeedingUpdate
	{
		public Entity m_BuildingEntity;

		public Entity m_DeliveryVehicle;
	}

	[BurstCompile]
	public struct InitializeCurrentLaneJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_LaneData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<CarCurrentLane> nativeArray2 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Transform transform = nativeArray[i];
				CarCurrentLane value = nativeArray2[i];
				if (m_CarLaneData.HasComponent(value.m_Lane))
				{
					Game.Net.CarLane carLane = m_CarLaneData[value.m_Lane];
					Entity owner = m_OwnerData[value.m_Lane].m_Owner;
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_LaneData[owner];
					value.m_Lane = Entity.Null;
					float3 curvePosition = value.m_CurvePosition;
					float num = float.MaxValue;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subLane = dynamicBuffer[j].m_SubLane;
						if (!m_CarLaneData.HasComponent(subLane) || m_MasterLaneData.HasComponent(subLane))
						{
							continue;
						}
						Game.Net.CarLane carLane2 = m_CarLaneData[subLane];
						if (carLane2.m_CarriagewayGroup == carLane.m_CarriagewayGroup)
						{
							float3 curvePosition2 = math.select(curvePosition, 1f - curvePosition.zyx, ((carLane.m_Flags ^ carLane2.m_Flags) & Game.Net.CarLaneFlags.Invert) != 0);
							float num2 = math.lengthsq(MathUtils.Position(m_CurveData[subLane].m_Bezier, curvePosition2.x) - transform.m_Position);
							if (num2 < num)
							{
								value.m_Lane = subLane;
								value.m_CurvePosition = curvePosition2;
								num = num2;
							}
						}
					}
				}
				nativeArray2[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLayoutReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public ComponentLookup<Controller> m_ControllerData;

		public BufferLookup<LayoutElement> m_Layouts;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (m_Layouts.HasBuffer(entity))
				{
					DynamicBuffer<LayoutElement> dynamicBuffer = m_Layouts[entity];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity vehicle = dynamicBuffer[j].m_Vehicle;
						if (vehicle != entity && vehicle != Entity.Null && !m_DeletedData.HasComponent(vehicle) && m_ControllerData.HasComponent(vehicle))
						{
							Controller value = m_ControllerData[vehicle];
							if (value.m_Controller == entity)
							{
								value.m_Controller = Entity.Null;
								m_ControllerData[vehicle] = value;
							}
						}
					}
				}
				if (m_ControllerData.HasComponent(entity))
				{
					Controller controller = m_ControllerData[entity];
					if (controller.m_Controller != entity && controller.m_Controller != Entity.Null && !m_DeletedData.HasComponent(controller.m_Controller) && m_Layouts.HasBuffer(controller.m_Controller))
					{
						CollectionUtils.RemoveValue(m_Layouts[controller.m_Controller], new LayoutElement(entity));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateVehicleReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> m_PersonalCarType;

		[ReadOnly]
		public ComponentTypeHandle<DeliveryTruck> m_DeliveryTruckType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> m_CarTrailerLaneType;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> m_AircraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> m_TrainCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> m_ParkedCarType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> m_ParkedTrainType;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> m_BicycleType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Moving> m_MovingType;

		[ReadOnly]
		public ComponentTypeHandle<Odometer> m_OdometerType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentRoute> m_CurrentRouteType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> m_BlockedLaneType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> m_CargoTransportVehicleData;

		[ReadOnly]
		public ComponentLookup<TaxiData> m_TaxiData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> m_ResourceNeedingBufs;

		public ComponentLookup<CarKeeper> m_CarKeepers;

		public ComponentLookup<BicycleOwner> m_BicycleOwners;

		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepots;

		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public BufferLookup<GuestVehicle> m_GuestVehicles;

		public BufferLookup<LaneObject> m_LaneObjects;

		public BufferLookup<RouteVehicle> m_RouteVehicles;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<ResourceNeedingUpdate>.ParallelWriter m_ResourceNeedingUpdates;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_CreatedType))
			{
				NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
				for (int i = 0; i < nativeArray4.Length; i++)
				{
					Entity vehicle = nativeArray[i];
					Owner owner = nativeArray4[i];
					if (m_OwnedVehicles.TryGetBuffer(owner.m_Owner, out var bufferData))
					{
						CollectionUtils.TryAddUniqueValue(bufferData, new OwnedVehicle(vehicle));
					}
				}
				NativeArray<PersonalCar> nativeArray5 = chunk.GetNativeArray(ref m_PersonalCarType);
				bool flag = chunk.Has(ref m_BicycleType);
				for (int j = 0; j < nativeArray5.Length; j++)
				{
					Entity entity = nativeArray[j];
					PersonalCar personalCar = nativeArray5[j];
					CarKeeper componentData2;
					if (flag)
					{
						if (m_BicycleOwners.TryGetComponent(personalCar.m_Keeper, out var componentData))
						{
							componentData.m_Bicycle = entity;
							m_BicycleOwners[personalCar.m_Keeper] = componentData;
						}
					}
					else if (m_CarKeepers.TryGetComponent(personalCar.m_Keeper, out componentData2))
					{
						componentData2.m_Car = entity;
						m_CarKeepers[personalCar.m_Keeper] = componentData2;
					}
				}
				if (chunk.Has(ref m_DeliveryTruckType))
				{
					NativeArray<Target> nativeArray6 = chunk.GetNativeArray(ref m_TargetType);
					for (int k = 0; k < nativeArray6.Length; k++)
					{
						Entity entity2 = nativeArray[k];
						Target target = nativeArray6[k];
						if (m_GuestVehicles.HasBuffer(target.m_Target))
						{
							m_GuestVehicles[target.m_Target].Add(new GuestVehicle(entity2));
						}
						if (m_ResourceNeedingBufs.HasBuffer(target.m_Target))
						{
							m_ResourceNeedingUpdates.Enqueue(new ResourceNeedingUpdate
							{
								m_BuildingEntity = target.m_Target,
								m_DeliveryVehicle = entity2
							});
						}
					}
				}
				NativeArray<CarCurrentLane> nativeArray7 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
				for (int l = 0; l < nativeArray7.Length; l++)
				{
					Entity entity3 = nativeArray[l];
					CarCurrentLane carCurrentLane = nativeArray7[l];
					if (m_LaneObjects.HasBuffer(carCurrentLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[carCurrentLane.m_Lane], entity3, carCurrentLane.m_CurvePosition.xy);
					}
					else
					{
						Transform transform = nativeArray2[l];
						PrefabRef prefabRef = nativeArray3[l];
						ObjectGeometryData geometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
						Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
						m_SearchTree.Add(entity3, new QuadTreeBoundsXZ(bounds));
					}
					if (m_LaneObjects.HasBuffer(carCurrentLane.m_ChangeLane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[carCurrentLane.m_ChangeLane], entity3, carCurrentLane.m_CurvePosition.xy);
					}
				}
				NativeArray<CarTrailerLane> nativeArray8 = chunk.GetNativeArray(ref m_CarTrailerLaneType);
				for (int m = 0; m < nativeArray8.Length; m++)
				{
					Entity entity4 = nativeArray[m];
					CarTrailerLane carTrailerLane = nativeArray8[m];
					if (m_LaneObjects.HasBuffer(carTrailerLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[carTrailerLane.m_Lane], entity4, carTrailerLane.m_CurvePosition.xy);
					}
					else
					{
						Transform transform2 = nativeArray2[m];
						PrefabRef prefabRef2 = nativeArray3[m];
						ObjectGeometryData geometryData2 = m_ObjectGeometryData[prefabRef2.m_Prefab];
						Bounds3 bounds2 = ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, geometryData2);
						m_SearchTree.Add(entity4, new QuadTreeBoundsXZ(bounds2));
					}
					if (m_LaneObjects.HasBuffer(carTrailerLane.m_NextLane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[carTrailerLane.m_NextLane], entity4, carTrailerLane.m_NextPosition.xy);
					}
				}
				NativeArray<WatercraftCurrentLane> nativeArray9 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
				for (int n = 0; n < nativeArray9.Length; n++)
				{
					Entity entity5 = nativeArray[n];
					WatercraftCurrentLane watercraftCurrentLane = nativeArray9[n];
					if (m_LaneObjects.HasBuffer(watercraftCurrentLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[watercraftCurrentLane.m_Lane], entity5, watercraftCurrentLane.m_CurvePosition.xy);
					}
					else
					{
						Transform transform3 = nativeArray2[n];
						PrefabRef prefabRef3 = nativeArray3[n];
						ObjectGeometryData geometryData3 = m_ObjectGeometryData[prefabRef3.m_Prefab];
						Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform3.m_Position, transform3.m_Rotation, geometryData3);
						m_SearchTree.Add(entity5, new QuadTreeBoundsXZ(bounds3));
					}
					if (m_LaneObjects.HasBuffer(watercraftCurrentLane.m_ChangeLane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[watercraftCurrentLane.m_ChangeLane], entity5, watercraftCurrentLane.m_CurvePosition.xy);
					}
				}
				NativeArray<AircraftCurrentLane> nativeArray10 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
				for (int num = 0; num < nativeArray10.Length; num++)
				{
					Entity entity6 = nativeArray[num];
					AircraftCurrentLane aircraftCurrentLane = nativeArray10[num];
					if (m_LaneObjects.HasBuffer(aircraftCurrentLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[aircraftCurrentLane.m_Lane], entity6, aircraftCurrentLane.m_CurvePosition.xy);
					}
					if (!m_LaneObjects.HasBuffer(aircraftCurrentLane.m_Lane) || (aircraftCurrentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
					{
						Transform transform4 = nativeArray2[num];
						PrefabRef prefabRef4 = nativeArray3[num];
						ObjectGeometryData geometryData4 = m_ObjectGeometryData[prefabRef4.m_Prefab];
						Bounds3 bounds4 = ObjectUtils.CalculateBounds(transform4.m_Position, transform4.m_Rotation, geometryData4);
						m_SearchTree.Add(entity6, new QuadTreeBoundsXZ(bounds4));
					}
				}
				NativeArray<TrainCurrentLane> nativeArray11 = chunk.GetNativeArray(ref m_TrainCurrentLaneType);
				for (int num2 = 0; num2 < nativeArray11.Length; num2++)
				{
					Entity laneObject = nativeArray[num2];
					TrainCurrentLane currentLane = nativeArray11[num2];
					TrainNavigationHelpers.GetCurvePositions(ref currentLane, out var pos, out var pos2);
					if (m_LaneObjects.TryGetBuffer(currentLane.m_Front.m_Lane, out var bufferData2))
					{
						NetUtils.AddLaneObject(bufferData2, laneObject, pos);
					}
					if (currentLane.m_Rear.m_Lane != currentLane.m_Front.m_Lane && m_LaneObjects.TryGetBuffer(currentLane.m_Rear.m_Lane, out bufferData2))
					{
						NetUtils.AddLaneObject(bufferData2, laneObject, pos2);
					}
				}
				NativeArray<ParkedCar> nativeArray12 = chunk.GetNativeArray(ref m_ParkedCarType);
				for (int num3 = 0; num3 < nativeArray12.Length; num3++)
				{
					Entity entity7 = nativeArray[num3];
					ParkedCar parkedCar = nativeArray12[num3];
					if (m_LaneObjects.TryGetBuffer(parkedCar.m_Lane, out var bufferData3))
					{
						NetUtils.AddLaneObject(bufferData3, entity7, parkedCar.m_CurvePosition);
						continue;
					}
					Transform transform5 = nativeArray2[num3];
					PrefabRef prefabRef5 = nativeArray3[num3];
					ObjectGeometryData geometryData5 = m_ObjectGeometryData[prefabRef5.m_Prefab];
					Bounds3 bounds5 = ObjectUtils.CalculateBounds(transform5.m_Position, transform5.m_Rotation, geometryData5);
					m_SearchTree.Add(entity7, new QuadTreeBoundsXZ(bounds5));
				}
				NativeArray<ParkedTrain> nativeArray13 = chunk.GetNativeArray(ref m_ParkedTrainType);
				for (int num4 = 0; num4 < nativeArray13.Length; num4++)
				{
					Entity laneObject2 = nativeArray[num4];
					ParkedTrain parkedTrain = nativeArray13[num4];
					TrainNavigationHelpers.GetCurvePositions(ref parkedTrain, out var pos3, out var pos4);
					if (m_LaneObjects.TryGetBuffer(parkedTrain.m_FrontLane, out var bufferData4))
					{
						NetUtils.AddLaneObject(bufferData4, laneObject2, pos3);
					}
					if (parkedTrain.m_RearLane != parkedTrain.m_FrontLane && m_LaneObjects.TryGetBuffer(parkedTrain.m_RearLane, out bufferData4))
					{
						NetUtils.AddLaneObject(bufferData4, laneObject2, pos4);
					}
				}
				NativeArray<CurrentRoute> nativeArray14 = chunk.GetNativeArray(ref m_CurrentRouteType);
				for (int num5 = 0; num5 < nativeArray14.Length; num5++)
				{
					Entity vehicle2 = nativeArray[num5];
					CurrentRoute currentRoute = nativeArray14[num5];
					if (m_RouteVehicles.TryGetBuffer(currentRoute.m_Route, out var bufferData5))
					{
						CollectionUtils.TryAddUniqueValue(bufferData5, new RouteVehicle(vehicle2));
					}
				}
				return;
			}
			bool flag2 = chunk.Has(ref m_MovingType);
			NativeArray<Owner> nativeArray15 = chunk.GetNativeArray(ref m_OwnerType);
			if (nativeArray15.Length != 0)
			{
				NativeArray<Odometer> nativeArray16 = chunk.GetNativeArray(ref m_OdometerType);
				NativeArray<PrefabRef> nativeArray17 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int num6 = 0; num6 < nativeArray15.Length; num6++)
				{
					Entity vehicle3 = nativeArray[num6];
					Owner owner2 = nativeArray15[num6];
					if (m_OwnedVehicles.TryGetBuffer(owner2.m_Owner, out var bufferData6))
					{
						CollectionUtils.RemoveValue(bufferData6, new OwnedVehicle(vehicle3));
					}
					if (nativeArray16.Length == 0 || !m_TransportDepots.HasComponent(owner2.m_Owner))
					{
						continue;
					}
					Odometer odometer = nativeArray16[num6];
					PrefabRef prefabRef6 = nativeArray17[num6];
					Game.Buildings.TransportDepot value = m_TransportDepots[owner2.m_Owner];
					CargoTransportVehicleData componentData4;
					TaxiData componentData5;
					if (m_PublicTransportVehicleData.TryGetComponent(prefabRef6.m_Prefab, out var componentData3))
					{
						if (componentData3.m_MaintenanceRange > 0.1f)
						{
							value.m_MaintenanceRequirement += math.saturate(odometer.m_Distance / componentData3.m_MaintenanceRange);
							m_TransportDepots[owner2.m_Owner] = value;
						}
					}
					else if (m_CargoTransportVehicleData.TryGetComponent(prefabRef6.m_Prefab, out componentData4))
					{
						if (componentData4.m_MaintenanceRange > 0.1f)
						{
							value.m_MaintenanceRequirement += math.saturate(odometer.m_Distance / componentData4.m_MaintenanceRange);
							m_TransportDepots[owner2.m_Owner] = value;
						}
					}
					else if (m_TaxiData.TryGetComponent(prefabRef6.m_Prefab, out componentData5) && componentData5.m_MaintenanceRange > 0.1f)
					{
						value.m_MaintenanceRequirement += math.saturate(odometer.m_Distance / componentData5.m_MaintenanceRange);
						m_TransportDepots[owner2.m_Owner] = value;
					}
				}
			}
			NativeArray<PersonalCar> nativeArray18 = chunk.GetNativeArray(ref m_PersonalCarType);
			bool flag3 = chunk.Has(ref m_BicycleType);
			for (int num7 = 0; num7 < nativeArray18.Length; num7++)
			{
				Entity entity8 = nativeArray[num7];
				PersonalCar personalCar2 = nativeArray18[num7];
				CarKeeper componentData7;
				if (flag3)
				{
					if (m_BicycleOwners.TryGetComponent(personalCar2.m_Keeper, out var componentData6) && componentData6.m_Bicycle == entity8)
					{
						componentData6.m_Bicycle = Entity.Null;
						m_BicycleOwners[personalCar2.m_Keeper] = componentData6;
					}
				}
				else if (m_CarKeepers.TryGetComponent(personalCar2.m_Keeper, out componentData7) && componentData7.m_Car == entity8)
				{
					componentData7.m_Car = Entity.Null;
					m_CarKeepers[personalCar2.m_Keeper] = componentData7;
				}
			}
			if (chunk.Has(ref m_DeliveryTruckType))
			{
				NativeArray<Target> nativeArray19 = chunk.GetNativeArray(ref m_TargetType);
				for (int num8 = 0; num8 < nativeArray19.Length; num8++)
				{
					Entity vehicle4 = nativeArray[num8];
					Target target2 = nativeArray19[num8];
					if (m_GuestVehicles.HasBuffer(target2.m_Target))
					{
						CollectionUtils.RemoveValue(m_GuestVehicles[target2.m_Target], new GuestVehicle(vehicle4));
					}
				}
			}
			NativeArray<CarCurrentLane> nativeArray20 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
			for (int num9 = 0; num9 < nativeArray20.Length; num9++)
			{
				Entity entity9 = nativeArray[num9];
				CarCurrentLane carCurrentLane2 = nativeArray20[num9];
				if (m_LaneObjects.HasBuffer(carCurrentLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[carCurrentLane2.m_Lane], entity9);
					if (!flag2 && m_CarLaneData.HasComponent(carCurrentLane2.m_Lane))
					{
						AddLaneComponent(carCurrentLane2.m_Lane, default(PathfindUpdated));
					}
				}
				else
				{
					m_SearchTree.TryRemove(entity9);
				}
				if (m_LaneObjects.HasBuffer(carCurrentLane2.m_ChangeLane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[carCurrentLane2.m_ChangeLane], entity9);
					if (!flag2 && m_CarLaneData.HasComponent(carCurrentLane2.m_ChangeLane))
					{
						AddLaneComponent(carCurrentLane2.m_ChangeLane, default(PathfindUpdated));
					}
				}
			}
			NativeArray<CarTrailerLane> nativeArray21 = chunk.GetNativeArray(ref m_CarTrailerLaneType);
			for (int num10 = 0; num10 < nativeArray21.Length; num10++)
			{
				Entity entity10 = nativeArray[num10];
				CarTrailerLane carTrailerLane2 = nativeArray21[num10];
				if (m_LaneObjects.HasBuffer(carTrailerLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[carTrailerLane2.m_Lane], entity10);
					if (!flag2 && m_CarLaneData.HasComponent(carTrailerLane2.m_Lane))
					{
						AddLaneComponent(carTrailerLane2.m_Lane, default(PathfindUpdated));
					}
				}
				else
				{
					m_SearchTree.TryRemove(entity10);
				}
				if (m_LaneObjects.HasBuffer(carTrailerLane2.m_NextLane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[carTrailerLane2.m_NextLane], entity10);
					if (!flag2 && m_CarLaneData.HasComponent(carTrailerLane2.m_NextLane))
					{
						AddLaneComponent(carTrailerLane2.m_NextLane, default(PathfindUpdated));
					}
				}
			}
			NativeArray<WatercraftCurrentLane> nativeArray22 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
			for (int num11 = 0; num11 < nativeArray22.Length; num11++)
			{
				Entity entity11 = nativeArray[num11];
				WatercraftCurrentLane watercraftCurrentLane2 = nativeArray22[num11];
				if (m_LaneObjects.HasBuffer(watercraftCurrentLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[watercraftCurrentLane2.m_Lane], entity11);
				}
				else
				{
					m_SearchTree.TryRemove(entity11);
				}
				if (m_LaneObjects.HasBuffer(watercraftCurrentLane2.m_ChangeLane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[watercraftCurrentLane2.m_ChangeLane], entity11);
				}
			}
			NativeArray<AircraftCurrentLane> nativeArray23 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
			for (int num12 = 0; num12 < nativeArray23.Length; num12++)
			{
				Entity entity12 = nativeArray[num12];
				AircraftCurrentLane aircraftCurrentLane2 = nativeArray23[num12];
				if (m_LaneObjects.HasBuffer(aircraftCurrentLane2.m_Lane))
				{
					NetUtils.RemoveLaneObject(m_LaneObjects[aircraftCurrentLane2.m_Lane], entity12);
				}
				if (!m_LaneObjects.HasBuffer(aircraftCurrentLane2.m_Lane) || (aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
				{
					m_SearchTree.TryRemove(entity12);
				}
			}
			NativeArray<TrainCurrentLane> nativeArray24 = chunk.GetNativeArray(ref m_TrainCurrentLaneType);
			for (int num13 = 0; num13 < nativeArray24.Length; num13++)
			{
				Entity laneObject3 = nativeArray[num13];
				TrainCurrentLane trainCurrentLane = nativeArray24[num13];
				if (m_LaneObjects.TryGetBuffer(trainCurrentLane.m_Front.m_Lane, out var bufferData7))
				{
					NetUtils.RemoveLaneObject(bufferData7, laneObject3);
				}
				if (trainCurrentLane.m_Rear.m_Lane != trainCurrentLane.m_Front.m_Lane && m_LaneObjects.TryGetBuffer(trainCurrentLane.m_Rear.m_Lane, out bufferData7))
				{
					NetUtils.RemoveLaneObject(bufferData7, laneObject3);
				}
			}
			NativeArray<ParkedCar> nativeArray25 = chunk.GetNativeArray(ref m_ParkedCarType);
			for (int num14 = 0; num14 < nativeArray25.Length; num14++)
			{
				Entity entity13 = nativeArray[num14];
				ParkedCar parkedCar2 = nativeArray25[num14];
				if (m_LaneObjects.TryGetBuffer(parkedCar2.m_Lane, out var bufferData8))
				{
					NetUtils.RemoveLaneObject(bufferData8, entity13);
					if (m_ParkingLaneData.HasComponent(parkedCar2.m_Lane) || m_GarageLaneData.HasComponent(parkedCar2.m_Lane))
					{
						AddLaneComponent(parkedCar2.m_Lane, default(PathfindUpdated));
					}
				}
				else
				{
					m_SearchTree.TryRemove(entity13);
					if (m_SpawnLocationData.HasComponent(parkedCar2.m_Lane))
					{
						m_CommandBuffer.AddComponent(parkedCar2.m_Lane, default(PathfindUpdated));
					}
				}
			}
			NativeArray<ParkedTrain> nativeArray26 = chunk.GetNativeArray(ref m_ParkedTrainType);
			for (int num15 = 0; num15 < nativeArray26.Length; num15++)
			{
				Entity laneObject4 = nativeArray[num15];
				ParkedTrain parkedTrain2 = nativeArray26[num15];
				if (m_LaneObjects.TryGetBuffer(parkedTrain2.m_FrontLane, out var bufferData9))
				{
					NetUtils.RemoveLaneObject(bufferData9, laneObject4);
				}
				if (parkedTrain2.m_RearLane != parkedTrain2.m_FrontLane && m_LaneObjects.TryGetBuffer(parkedTrain2.m_RearLane, out bufferData9))
				{
					NetUtils.RemoveLaneObject(bufferData9, laneObject4);
				}
				if (m_SpawnLocationData.HasComponent(parkedTrain2.m_ParkingLocation))
				{
					m_CommandBuffer.AddComponent(parkedTrain2.m_ParkingLocation, default(PathfindUpdated));
				}
			}
			NativeArray<CurrentRoute> nativeArray27 = chunk.GetNativeArray(ref m_CurrentRouteType);
			for (int num16 = 0; num16 < nativeArray27.Length; num16++)
			{
				Entity vehicle5 = nativeArray[num16];
				CurrentRoute currentRoute2 = nativeArray27[num16];
				if (m_RouteVehicles.TryGetBuffer(currentRoute2.m_Route, out var bufferData10))
				{
					CollectionUtils.RemoveValue(bufferData10, new RouteVehicle(vehicle5));
				}
			}
			BufferAccessor<BlockedLane> bufferAccessor = chunk.GetBufferAccessor(ref m_BlockedLaneType);
			for (int num17 = 0; num17 < bufferAccessor.Length; num17++)
			{
				Entity laneObject5 = nativeArray[num17];
				DynamicBuffer<BlockedLane> dynamicBuffer = bufferAccessor[num17];
				for (int num18 = 0; num18 < dynamicBuffer.Length; num18++)
				{
					BlockedLane blockedLane = dynamicBuffer[num18];
					if (m_LaneObjects.HasBuffer(blockedLane.m_Lane))
					{
						NetUtils.RemoveLaneObject(m_LaneObjects[blockedLane.m_Lane], laneObject5);
						if (!flag2 && m_CarLaneData.HasComponent(blockedLane.m_Lane))
						{
							AddLaneComponent(blockedLane.m_Lane, default(PathfindUpdated));
						}
					}
				}
			}
		}

		private void AddLaneComponent<T>(Entity lane, T component) where T : unmanaged, IComponentData
		{
			m_CommandBuffer.AddComponent(lane, component);
			if (!m_SlaveLaneData.HasComponent(lane) || !m_OwnerData.HasComponent(lane))
			{
				return;
			}
			uint num = m_SlaveLaneData[lane].m_Group;
			Entity owner = m_OwnerData[lane].m_Owner;
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (m_MasterLaneData.HasComponent(subLane) && m_MasterLaneData[subLane].m_Group == num)
				{
					m_CommandBuffer.AddComponent(subLane, component);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public ComponentLookup<Controller> __Game_Vehicles_Controller_RW_ComponentLookup;

		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> __Game_Objects_BlockedLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiData> __Game_Prefabs_TaxiData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> __Game_Buildings_ResourceNeeding_RO_BufferLookup;

		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RW_ComponentLookup;

		public ComponentLookup<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RW_ComponentLookup;

		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferLookup;

		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RW_BufferLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Vehicles_Controller_RW_ComponentLookup = state.GetComponentLookup<Controller>();
			__Game_Vehicles_LayoutElement_RW_BufferLookup = state.GetBufferLookup<LayoutElement>();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PersonalCar>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarTrailerLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Bicycle>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Vehicles_Odometer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentRoute>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_BlockedLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<BlockedLane>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_TaxiData_RO_ComponentLookup = state.GetComponentLookup<TaxiData>(isReadOnly: true);
			__Game_Buildings_ResourceNeeding_RO_BufferLookup = state.GetBufferLookup<ResourceNeeding>(isReadOnly: true);
			__Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
			__Game_Citizens_BicycleOwner_RW_ComponentLookup = state.GetComponentLookup<BicycleOwner>();
			__Game_Buildings_TransportDepot_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.TransportDepot>();
			__Game_Vehicles_OwnedVehicle_RW_BufferLookup = state.GetBufferLookup<OwnedVehicle>();
			__Game_Vehicles_GuestVehicle_RW_BufferLookup = state.GetBufferLookup<GuestVehicle>();
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
			__Game_Routes_RouteVehicle_RW_BufferLookup = state.GetBufferLookup<RouteVehicle>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private Game.Objects.SearchSystem m_SearchSystem;

	private EntityQuery m_CarQuery;

	private EntityQuery m_VehicleQuery;

	private EntityQuery m_LayoutQuery;

	private NativeQueue<ResourceNeedingUpdate> m_ResourceNeedingUpdateQueue;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_ResourceNeedingUpdateQueue = new NativeQueue<ResourceNeedingUpdate>(Allocator.Persistent);
		m_CarQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.Exclude<Temp>());
		m_VehicleQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Vehicle>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_LayoutQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<LayoutElement>(),
				ComponentType.ReadOnly<Controller>()
			}
		});
		RequireAnyForUpdate(m_CarQuery, m_VehicleQuery, m_LayoutQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ResourceNeedingUpdateQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = default(JobHandle);
		if (!m_CarQuery.IsEmptyIgnoreFilter)
		{
			jobHandle = JobChunkExtensions.ScheduleParallel(new InitializeCurrentLaneJob
			{
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef)
			}, m_CarQuery, base.Dependency);
		}
		JobHandle job = default(JobHandle);
		if (!m_LayoutQuery.IsEmptyIgnoreFilter)
		{
			job = JobChunkExtensions.Schedule(new UpdateLayoutReferencesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Layouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_LayoutQuery, base.Dependency);
		}
		JobHandle jobHandle2 = default(JobHandle);
		if (!m_VehicleQuery.IsEmptyIgnoreFilter)
		{
			jobHandle2 = JobChunkExtensions.Schedule(new UpdateVehicleReferencesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PersonalCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeliveryTruckType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarTrailerLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrainCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkedCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ParkedTrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BicycleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OdometerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Odometer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentRouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BlockedLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_BlockedLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CargoTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TaxiData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourceNeedingBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ResourceNeeding_RO_BufferLookup, ref base.CheckedStateRef),
				m_CarKeepers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup, ref base.CheckedStateRef),
				m_BicycleOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RW_ComponentLookup, ref base.CheckedStateRef),
				m_TransportDepots = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TransportDepot_RW_ComponentLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferLookup, ref base.CheckedStateRef),
				m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RW_BufferLookup, ref base.CheckedStateRef),
				m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
				m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RW_BufferLookup, ref base.CheckedStateRef),
				m_SearchTree = m_SearchSystem.GetMovingSearchTree(readOnly: false, out var dependencies),
				m_ResourceNeedingUpdates = m_ResourceNeedingUpdateQueue.AsParallelWriter(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
			}, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle, dependencies));
			m_SearchSystem.AddMovingSearchTreeWriter(jobHandle2);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		}
		base.Dependency = JobHandle.CombineDependencies(jobHandle, job, jobHandle2);
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
	public ReferencesSystem()
	{
	}
}
