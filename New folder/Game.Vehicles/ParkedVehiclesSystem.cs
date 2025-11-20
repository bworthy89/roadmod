using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Vehicles;

[CompilerGenerated]
public class ParkedVehiclesSystem : GameSystemBase
{
	private struct ParkingLocation
	{
		public Game.Net.ParkingLane m_ParkingLane;

		public ParkingLaneData m_ParkingLaneData;

		public TrackTypes m_TrackTypes;

		public SpawnLocationType m_SpawnLocationType;

		public Transform m_OwnerTransform;

		public Curve m_Curve;

		public Entity m_Lane;

		public float2 m_MaxSize;

		public float m_CurvePos;
	}

	private struct DeletedVehicleData
	{
		public Entity m_Entity;

		public Entity m_SecondaryPrefab;

		public Transform m_Transform;
	}

	[BurstCompile]
	private struct FindParkingLocationsJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocationElements;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public Entity m_Entity;

		public NativeList<ParkingLocation> m_Locations;

		public void Execute()
		{
			if (!m_SpawnLocationElements.TryGetBuffer(m_Entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				SpawnLocationElement spawnLocationElement = bufferData[i];
				switch (spawnLocationElement.m_Type)
				{
				case SpawnLocationType.SpawnLocation:
				{
					if (m_PrefabRefData.TryGetComponent(spawnLocationElement.m_SpawnLocation, out var componentData2) && m_PrefabSpawnLocationData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
					{
						CheckSpawnLocation(spawnLocationElement.m_SpawnLocation, componentData3);
					}
					break;
				}
				case SpawnLocationType.ParkingLane:
				{
					if (m_ParkingLaneData.TryGetComponent(spawnLocationElement.m_SpawnLocation, out var componentData))
					{
						CheckParkingLane(spawnLocationElement.m_SpawnLocation, componentData);
					}
					break;
				}
				}
			}
		}

		private void CheckSpawnLocation(Entity entity, SpawnLocationData spawnLocationData)
		{
			if (((spawnLocationData.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None && spawnLocationData.m_ConnectionType == RouteConnectionType.Air) || spawnLocationData.m_ConnectionType == RouteConnectionType.Track)
			{
				Transform ownerTransform = m_TransformData[entity];
				ref NativeList<ParkingLocation> reference = ref m_Locations;
				ParkingLocation value = new ParkingLocation
				{
					m_ParkingLaneData = new ParkingLaneData
					{
						m_RoadTypes = spawnLocationData.m_RoadTypes
					},
					m_TrackTypes = spawnLocationData.m_TrackTypes,
					m_SpawnLocationType = SpawnLocationType.SpawnLocation,
					m_OwnerTransform = ownerTransform,
					m_Lane = entity,
					m_MaxSize = float.MaxValue,
					m_CurvePos = 0f
				};
				reference.Add(in value);
			}
		}

		private void CheckParkingLane(Entity lane, Game.Net.ParkingLane parkingLane)
		{
			if ((parkingLane.m_Flags & (ParkingLaneFlags.VirtualLane | ParkingLaneFlags.SpecialVehicles)) != ParkingLaneFlags.SpecialVehicles)
			{
				return;
			}
			Curve curve = m_CurveData[lane];
			PrefabRef prefabRef = m_PrefabRefData[lane];
			DynamicBuffer<LaneOverlap> dynamicBuffer = m_LaneOverlaps[lane];
			ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef.m_Prefab];
			float2 parkingSize = VehicleUtils.GetParkingSize(parkingLaneData);
			Transform componentData = default(Transform);
			if (m_OwnerData.TryGetComponent(lane, out var componentData2))
			{
				m_TransformData.TryGetComponent(componentData2.m_Owner, out componentData);
			}
			if (parkingLaneData.m_SlotInterval == 0f)
			{
				return;
			}
			int parkingSlotCount = NetUtils.GetParkingSlotCount(curve, parkingLane, parkingLaneData);
			float parkingSlotInterval = NetUtils.GetParkingSlotInterval(curve, parkingLane, parkingLaneData, parkingSlotCount);
			float3 x = curve.m_Bezier.a;
			float2 @float = 0f;
			float num = 0f;
			float num2 = math.max((parkingLane.m_Flags & (ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane)) switch
			{
				ParkingLaneFlags.StartingLane => curve.m_Length - (float)parkingSlotCount * parkingSlotInterval, 
				ParkingLaneFlags.EndingLane => 0f, 
				_ => (curve.m_Length - (float)parkingSlotCount * parkingSlotInterval) * 0.5f, 
			}, 0f);
			int i = -1;
			float2 float2 = 2f;
			int num3 = 0;
			if (num3 < dynamicBuffer.Length)
			{
				LaneOverlap laneOverlap = dynamicBuffer[num3++];
				float2 = new float2((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd) * 0.003921569f;
			}
			for (int j = 1; j <= 16; j++)
			{
				float num4 = (float)j * 0.0625f;
				float3 float3 = MathUtils.Position(curve.m_Bezier, num4);
				for (num += math.distance(x, float3); num >= num2 || (j == 16 && i < parkingSlotCount); i++)
				{
					@float.y = math.select(num4, math.lerp(@float.x, num4, num2 / num), num2 < num);
					bool flag = false;
					if (float2.x < @float.y)
					{
						flag = true;
						if (float2.y <= @float.y)
						{
							float2 = 2f;
							while (num3 < dynamicBuffer.Length)
							{
								LaneOverlap laneOverlap2 = dynamicBuffer[num3++];
								float2 float4 = new float2((int)laneOverlap2.m_ThisStart, (int)laneOverlap2.m_ThisEnd) * 0.003921569f;
								if (float4.y > @float.y)
								{
									float2 = float4;
									break;
								}
							}
						}
					}
					if (!flag && i >= 0 && i < parkingSlotCount)
					{
						float curvePos = math.lerp(@float.x, @float.y, 0.5f);
						ref NativeList<ParkingLocation> reference = ref m_Locations;
						ParkingLocation value = new ParkingLocation
						{
							m_ParkingLane = parkingLane,
							m_ParkingLaneData = parkingLaneData,
							m_SpawnLocationType = SpawnLocationType.ParkingLane,
							m_OwnerTransform = componentData,
							m_Curve = curve,
							m_Lane = lane,
							m_MaxSize = parkingSize,
							m_CurvePos = curvePos
						};
						reference.Add(in value);
					}
					num -= num2;
					@float.x = @float.y;
					num2 = parkingSlotInterval;
				}
				x = float3;
			}
		}
	}

	[BurstCompile]
	private struct CollectDeletedVehiclesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			m_DeletedVehicleMap.Capacity = num;
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Transform> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TransformType);
				NativeArray<Controller> nativeArray3 = archetypeChunk.GetNativeArray(ref m_ControllerType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<LayoutElement> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_LayoutElementType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity = nativeArray[k];
					Transform transform = nativeArray2[k];
					PrefabRef prefabRef = nativeArray4[k];
					DeletedVehicleData item = new DeletedVehicleData
					{
						m_Entity = entity,
						m_Transform = transform
					};
					if (!m_PrefabData.HasEnabledComponent(prefabRef.m_Prefab) || (CollectionUtils.TryGet(nativeArray3, k, out var value) && value.m_Controller != entity))
					{
						continue;
					}
					if (CollectionUtils.TryGet(bufferAccessor, k, out var value2))
					{
						item.m_SecondaryPrefab = GetSecondaryPrefab(prefabRef.m_Prefab, value2, ref m_PrefabRefData, ref m_PrefabData, out var validLayout);
						if (!validLayout)
						{
							continue;
						}
					}
					m_DeletedVehicleMap.Add(prefabRef.m_Prefab, item);
				}
			}
		}
	}

	[BurstCompile]
	private struct DuplicateVehiclesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> m_PrefabMovingObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public ComponentLookup<TrainObjectData> m_PrefabTrainObjectData;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		public NativeList<ParkingLocation> m_Locations;

		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Transform parentTransform = m_TransformData[m_Entity];
			Transform inverseParentTransform = ObjectUtils.InverseTransform(m_TransformData[m_Temp.m_Original]);
			DynamicBuffer<OwnedVehicle> dynamicBuffer = m_OwnedVehicles[m_Temp.m_Original];
			NativeList<LayoutElement> nativeList = default(NativeList<LayoutElement>);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				OwnedVehicle ownedVehicle = dynamicBuffer[i];
				bool flag = m_ParkedCarData.HasComponent(ownedVehicle.m_Vehicle);
				bool flag2 = m_ParkedTrainData.HasComponent(ownedVehicle.m_Vehicle);
				if (!flag && !flag2)
				{
					continue;
				}
				PrefabRef component = m_PrefabRefData[ownedVehicle.m_Vehicle];
				if (!m_PrefabObjectGeometryData.TryGetComponent(component.m_Prefab, out var componentData) || !m_PrefabMovingObjectData.TryGetComponent(component.m_Prefab, out var componentData2) || !m_PrefabData.IsComponentEnabled(component.m_Prefab))
				{
					continue;
				}
				Entity secondaryPrefab = Entity.Null;
				if (m_LayoutElements.TryGetBuffer(ownedVehicle.m_Vehicle, out var bufferData))
				{
					secondaryPrefab = GetSecondaryPrefab(component.m_Prefab, bufferData, ref m_PrefabRefData, ref m_PrefabData, out var validLayout);
					if (!validLayout)
					{
						continue;
					}
				}
				NativeArray<LayoutElement> v = default(NativeArray<LayoutElement>);
				Transform transform = ObjectUtils.WorldToLocal(inverseParentTransform, m_TransformData[ownedVehicle.m_Vehicle]);
				transform = ObjectUtils.LocalToWorld(parentTransform, transform);
				bool flag3 = (m_Temp.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) != 0;
				Entity lane = Entity.Null;
				float curvePosition = 0f;
				if (!flag3)
				{
					RoadTypes roadType = RoadTypes.None;
					TrackTypes trackType = TrackTypes.None;
					TrainData componentData3;
					if (m_HelicopterData.HasComponent(ownedVehicle.m_Vehicle))
					{
						roadType = RoadTypes.Helicopter;
					}
					else if (m_PrefabTrainData.TryGetComponent(component.m_Prefab, out componentData3))
					{
						trackType = componentData3.m_TrackType;
					}
					else
					{
						roadType = RoadTypes.Car;
					}
					SelectParkingSpace(componentData, roadType, trackType, ref transform, out lane, out curvePosition);
				}
				Entity entity = FindDeletedVehicle(component.m_Prefab, secondaryPrefab, transform, m_DeletedVehicleMap);
				if (((m_LayoutElements.TryGetBuffer(entity, out var bufferData2) && bufferData2.Length != 0) || (bufferData.IsCreated && bufferData.Length != 0)) && !AreEqual(entity, ownedVehicle.m_Vehicle, bufferData2, bufferData))
				{
					entity = Entity.Null;
				}
				if ((flag && m_ParkedCarData.HasComponent(entity)) || (flag2 && m_ParkedTrainData.HasComponent(entity)))
				{
					if (bufferData2.IsCreated && bufferData2.Length != 0)
					{
						v = bufferData2.AsNativeArray();
						for (int j = 0; j < bufferData2.Length; j++)
						{
							Entity vehicle = bufferData2[j].m_Vehicle;
							m_CommandBuffer.RemoveComponent<Deleted>(vehicle);
							m_CommandBuffer.AddComponent<Updated>(vehicle);
						}
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Deleted>(entity);
						m_CommandBuffer.AddComponent<Updated>(entity);
					}
				}
				else if (bufferData.IsCreated && bufferData.Length != 0)
				{
					if (!nativeList.IsCreated)
					{
						nativeList = new NativeList<LayoutElement>(bufferData.Length, Allocator.Temp);
					}
					for (int k = 0; k < bufferData.Length; k++)
					{
						Entity vehicle2 = bufferData[k].m_Vehicle;
						PrefabRef component2 = m_PrefabRefData[vehicle2];
						Entity entity2;
						if (vehicle2 == ownedVehicle.m_Vehicle)
						{
							entity2 = ((!m_PrefabTrainObjectData.TryGetComponent(component2.m_Prefab, out var componentData4)) ? m_CommandBuffer.CreateEntity(componentData2.m_StoppedArchetype) : m_CommandBuffer.CreateEntity(componentData4.m_StoppedControllerArchetype));
							entity = entity2;
						}
						else
						{
							MovingObjectData movingObjectData = m_PrefabMovingObjectData[component2.m_Prefab];
							entity2 = m_CommandBuffer.CreateEntity(movingObjectData.m_StoppedArchetype);
						}
						nativeList.Add(new LayoutElement(entity2));
						m_CommandBuffer.SetComponent(entity2, component2);
						m_CommandBuffer.AddComponent(entity2, default(Animation));
						m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
					}
					v = nativeList.AsArray();
				}
				else
				{
					entity = ((!m_PrefabTrainObjectData.TryGetComponent(component.m_Prefab, out var componentData5)) ? m_CommandBuffer.CreateEntity(componentData2.m_StoppedArchetype) : m_CommandBuffer.CreateEntity(componentData5.m_StoppedControllerArchetype));
					m_CommandBuffer.SetComponent(entity, component);
					m_CommandBuffer.AddComponent(entity, default(Animation));
					m_CommandBuffer.AddComponent(entity, default(InterpolatedTransform));
				}
				m_CommandBuffer.AddComponent(entity, new Owner(m_Entity));
				Temp component3 = default(Temp);
				if (!flag3 && lane == Entity.Null)
				{
					component3.m_Flags = TempFlags.Delete | TempFlags.Hidden;
				}
				else
				{
					component3.m_Flags = m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate);
				}
				if (flag3 && m_UnspawnedData.HasComponent(ownedVehicle.m_Vehicle))
				{
					component3.m_Flags |= TempFlags.Hidden;
				}
				if (v.IsCreated)
				{
					for (int l = 0; l < v.Length; l++)
					{
						Entity vehicle3 = v[l].m_Vehicle;
						component3.m_Original = bufferData[l].m_Vehicle;
						if (flag)
						{
							m_CommandBuffer.SetComponent(vehicle3, new ParkedCar(lane, curvePosition));
						}
						if (flag2)
						{
							m_CommandBuffer.SetComponent(vehicle3, new ParkedTrain(lane));
							if (m_TrainData.TryGetComponent(component3.m_Original, out var componentData6))
							{
								componentData6.m_Flags &= TrainFlags.Reversed;
								m_CommandBuffer.SetComponent(vehicle3, componentData6);
							}
						}
						m_CommandBuffer.AddComponent(vehicle3, component3);
						m_CommandBuffer.SetComponent(vehicle3, transform);
						m_CommandBuffer.SetComponent(vehicle3, m_PseudoRandomSeedData[component3.m_Original]);
						m_CommandBuffer.AddComponent(component3.m_Original, default(Hidden));
						m_CommandBuffer.AddComponent(component3.m_Original, default(BatchesUpdated));
					}
				}
				else
				{
					component3.m_Original = ownedVehicle.m_Vehicle;
					m_CommandBuffer.SetComponent(entity, new ParkedCar(lane, curvePosition));
					m_CommandBuffer.AddComponent(entity, component3);
					m_CommandBuffer.SetComponent(entity, transform);
					m_CommandBuffer.SetComponent(entity, m_PseudoRandomSeedData[component3.m_Original]);
					m_CommandBuffer.AddComponent(component3.m_Original, default(Hidden));
					m_CommandBuffer.AddComponent(component3.m_Original, default(BatchesUpdated));
				}
				if (!nativeList.IsCreated || nativeList.Length == 0)
				{
					continue;
				}
				for (int m = 0; m < v.Length; m++)
				{
					if (m_ControllerData.HasComponent(bufferData[m].m_Vehicle))
					{
						m_CommandBuffer.SetComponent(v[m].m_Vehicle, new Controller(entity));
					}
				}
				m_CommandBuffer.SetBuffer<LayoutElement>(entity).CopyFrom(v);
				nativeList.Clear();
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private bool AreEqual(Entity entity1, Entity entity2, DynamicBuffer<LayoutElement> layout1, DynamicBuffer<LayoutElement> layout2)
		{
			if (!layout1.IsCreated || !layout2.IsCreated)
			{
				return false;
			}
			if (layout1.Length != layout2.Length)
			{
				return false;
			}
			for (int i = 0; i < layout1.Length; i++)
			{
				Entity vehicle = layout1[i].m_Vehicle;
				Entity vehicle2 = layout2[i].m_Vehicle;
				if (vehicle == entity1 != (vehicle2 == entity2))
				{
					return false;
				}
				if (m_PrefabRefData[vehicle].m_Prefab != m_PrefabRefData[vehicle2].m_Prefab)
				{
					return false;
				}
			}
			return true;
		}

		private bool SelectParkingSpace(ObjectGeometryData objectGeometryData, RoadTypes roadType, TrackTypes trackType, ref Transform transform, out Entity lane, out float curvePosition)
		{
			float offset;
			float2 parkingSize = VehicleUtils.GetParkingSize(objectGeometryData, out offset);
			Transform transform2 = default(Transform);
			float num = float.MaxValue;
			int num2 = -1;
			for (int i = 0; i < m_Locations.Length; i++)
			{
				ParkingLocation parkingLocation = m_Locations[i];
				if (!math.any(parkingSize > parkingLocation.m_MaxSize) && ((parkingLocation.m_ParkingLaneData.m_RoadTypes & roadType) != RoadTypes.None || (parkingLocation.m_TrackTypes & trackType) != TrackTypes.None))
				{
					Transform transform3 = ((parkingLocation.m_SpawnLocationType != SpawnLocationType.ParkingLane) ? parkingLocation.m_OwnerTransform : VehicleUtils.CalculateParkingSpaceTarget(parkingLocation.m_ParkingLane, parkingLocation.m_ParkingLaneData, objectGeometryData, parkingLocation.m_Curve, parkingLocation.m_OwnerTransform, parkingLocation.m_CurvePos));
					float num3 = math.distancesq(transform.m_Position, transform3.m_Position);
					if (num3 < num)
					{
						transform2 = transform3;
						num = num3;
						num2 = i;
					}
				}
			}
			if (num2 != -1)
			{
				ParkingLocation parkingLocation2 = m_Locations[num2];
				transform = transform2;
				lane = parkingLocation2.m_Lane;
				curvePosition = parkingLocation2.m_CurvePos;
				if (parkingLocation2.m_SpawnLocationType == SpawnLocationType.ParkingLane && parkingLocation2.m_ParkingLaneData.m_SlotAngle <= 0.25f)
				{
					if (offset > 0f)
					{
						Bounds1 t = new Bounds1(curvePosition, 1f);
						MathUtils.ClampLength(parkingLocation2.m_Curve.m_Bezier, ref t, offset);
						curvePosition = t.max;
					}
					else if (offset < 0f)
					{
						Bounds1 t2 = new Bounds1(0f, curvePosition);
						MathUtils.ClampLengthInverse(parkingLocation2.m_Curve.m_Bezier, ref t2, 0f - offset);
						curvePosition = t2.min;
					}
					transform = VehicleUtils.CalculateParkingSpaceTarget(parkingLocation2.m_ParkingLane, parkingLocation2.m_ParkingLaneData, objectGeometryData, parkingLocation2.m_Curve, parkingLocation2.m_OwnerTransform, curvePosition);
				}
				m_Locations.RemoveAtSwapBack(num2);
				return true;
			}
			transform = default(Transform);
			lane = Entity.Null;
			curvePosition = 0f;
			return false;
		}
	}

	[BurstCompile]
	private struct SpawnPoliceCarsJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> m_PrefabPoliceStationData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public PoliceCarSelectData m_PoliceCarSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabPoliceStationData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null && UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data2, ref m_PrefabRefData, ref m_PrefabPoliceStationData, ref m_InstalledUpgrades))
			{
				data.m_PatrolCarCapacity -= data2.m_PatrolCarCapacity;
				data.m_PoliceHelicopterCapacity -= data2.m_PoliceHelicopterCapacity;
			}
			for (int i = 0; i < data.m_PatrolCarCapacity; i++)
			{
				CreateVehicle(ref random, data, RoadTypes.Car);
			}
			for (int j = 0; j < data.m_PoliceHelicopterCapacity; j++)
			{
				CreateVehicle(ref random, data, RoadTypes.Helicopter);
			}
		}

		private void CreateVehicle(ref Random random, PoliceStationData policeStationData, RoadTypes roadType)
		{
			PolicePurpose purposeMask = policeStationData.m_PurposeMask;
			Entity entity = m_PoliceCarSelectData.SelectVehicle(ref random, ref purposeMask, roadType);
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_PoliceCarSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, ref purposeMask, roadType, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			m_CommandBuffer.SetComponent(entity2, new PoliceCar(PoliceCarFlags.Empty | PoliceCarFlags.Disabled, 0, policeStationData.m_PurposeMask & purposeMask));
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	[BurstCompile]
	private struct SpawnFireEnginesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<FireStationData> m_PrefabFireStationData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public FireEngineSelectData m_FireEngineSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabFireStationData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null && UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data2, ref m_PrefabRefData, ref m_PrefabFireStationData, ref m_InstalledUpgrades))
			{
				data.m_FireEngineCapacity -= data2.m_FireEngineCapacity;
				data.m_FireHelicopterCapacity -= data2.m_FireHelicopterCapacity;
				data.m_DisasterResponseCapacity -= data2.m_DisasterResponseCapacity;
			}
			for (int i = 0; i < data.m_FireEngineCapacity; i++)
			{
				CreateVehicle(ref random, ref data, RoadTypes.Car);
			}
			for (int j = 0; j < data.m_FireHelicopterCapacity; j++)
			{
				CreateVehicle(ref random, ref data, RoadTypes.Helicopter);
			}
		}

		private void CreateVehicle(ref Random random, ref FireStationData fireStationData, RoadTypes roadType)
		{
			float2 extinguishingCapacity = new float2(float.Epsilon, float.MaxValue);
			Entity entity = m_FireEngineSelectData.SelectVehicle(ref random, ref extinguishingCapacity, roadType);
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_FireEngineSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, ref extinguishingCapacity, roadType, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			FireEngineFlags fireEngineFlags = FireEngineFlags.Disabled;
			if (fireStationData.m_DisasterResponseCapacity > 0)
			{
				fireEngineFlags |= FireEngineFlags.DisasterResponse;
				fireStationData.m_DisasterResponseCapacity--;
			}
			m_CommandBuffer.SetComponent(entity2, new FireEngine(fireEngineFlags, 0, extinguishingCapacity.y, fireStationData.m_VehicleEfficiency));
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	[BurstCompile]
	private struct SpawnHealthcareVehiclesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<HospitalData> m_PrefabHospitalData;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> m_PrefabDeathcareFacilityData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabHospitalData, ref m_InstalledUpgrades);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data2, ref m_PrefabRefData, ref m_PrefabDeathcareFacilityData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null)
			{
				if (UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data3, ref m_PrefabRefData, ref m_PrefabHospitalData, ref m_InstalledUpgrades))
				{
					data.m_AmbulanceCapacity -= data3.m_AmbulanceCapacity;
					data.m_MedicalHelicopterCapacity -= data3.m_MedicalHelicopterCapacity;
				}
				if (UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data4, ref m_PrefabRefData, ref m_PrefabDeathcareFacilityData, ref m_InstalledUpgrades))
				{
					data2.m_HearseCapacity -= data4.m_HearseCapacity;
				}
			}
			for (int i = 0; i < data.m_AmbulanceCapacity; i++)
			{
				CreateVehicle(ref random, HealthcareRequestType.Ambulance, RoadTypes.Car);
			}
			for (int j = 0; j < data2.m_HearseCapacity; j++)
			{
				CreateVehicle(ref random, HealthcareRequestType.Hearse, RoadTypes.Car);
			}
			for (int k = 0; k < data.m_MedicalHelicopterCapacity; k++)
			{
				CreateVehicle(ref random, HealthcareRequestType.Ambulance, RoadTypes.Helicopter);
			}
		}

		private void CreateVehicle(ref Random random, HealthcareRequestType healthcareType, RoadTypes roadType)
		{
			Entity entity = m_HealthcareVehicleSelectData.SelectVehicle(ref random, healthcareType, roadType);
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_HealthcareVehicleSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, healthcareType, roadType, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			if (healthcareType == HealthcareRequestType.Ambulance)
			{
				m_CommandBuffer.SetComponent(entity2, new Ambulance(Entity.Null, Entity.Null, AmbulanceFlags.Disabled));
			}
			else
			{
				m_CommandBuffer.SetComponent(entity2, new Hearse(Entity.Null, HearseFlags.Disabled));
			}
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	[BurstCompile]
	private struct SpawnTransportVehiclesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public ComponentLookup<PrisonData> m_PrefabPrisonData;

		[ReadOnly]
		public ComponentLookup<EmergencyShelterData> m_PrefabEmergencyShelterData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabTransportDepotData, ref m_InstalledUpgrades);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data2, ref m_PrefabRefData, ref m_PrefabPrisonData, ref m_InstalledUpgrades);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data3, ref m_PrefabRefData, ref m_PrefabEmergencyShelterData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null)
			{
				if (UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data4, ref m_PrefabRefData, ref m_PrefabTransportDepotData, ref m_InstalledUpgrades))
				{
					data.m_VehicleCapacity -= data4.m_VehicleCapacity;
				}
				if (UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data5, ref m_PrefabRefData, ref m_PrefabPrisonData, ref m_InstalledUpgrades))
				{
					data2.m_PrisonVanCapacity -= data5.m_PrisonVanCapacity;
				}
				if (UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data6, ref m_PrefabRefData, ref m_PrefabEmergencyShelterData, ref m_InstalledUpgrades))
				{
					data3.m_VehicleCapacity -= data6.m_VehicleCapacity;
				}
			}
			NativeList<LayoutElement> layoutBuffer = default(NativeList<LayoutElement>);
			RoadTypes roadType = RoadTypes.None;
			TrackTypes trackType = TrackTypes.None;
			bool flag = false;
			switch (data.m_TransportType)
			{
			case TransportType.Bus:
				roadType = RoadTypes.Car;
				break;
			case TransportType.Taxi:
				roadType = RoadTypes.Car;
				break;
			case TransportType.Train:
				trackType = TrackTypes.Train;
				flag = true;
				break;
			case TransportType.Tram:
				trackType = TrackTypes.Tram;
				break;
			case TransportType.Subway:
				trackType = TrackTypes.Subway;
				break;
			default:
				data.m_VehicleCapacity = 0;
				break;
			}
			for (int i = 0; i < data.m_VehicleCapacity; i++)
			{
				PublicTransportPurpose publicTransportPurpose = (PublicTransportPurpose)0;
				Resource cargoResource = Resource.NoResource;
				if (flag && random.NextBool())
				{
					cargoResource = Resource.Food;
				}
				else
				{
					publicTransportPurpose = ((data.m_TransportType != TransportType.Taxi) ? PublicTransportPurpose.TransportLine : ((PublicTransportPurpose)0));
				}
				CreateVehicle(ref random, data.m_TransportType, data.m_EnergyTypes, data.m_SizeClass, publicTransportPurpose, cargoResource, roadType, trackType, (PublicTransportFlags)0u, ref layoutBuffer);
			}
			for (int j = 0; j < data2.m_PrisonVanCapacity; j++)
			{
				CreateVehicle(ref random, TransportType.Bus, EnergyTypes.FuelAndElectricity, SizeClass.Large, PublicTransportPurpose.PrisonerTransport, Resource.NoResource, RoadTypes.Car, TrackTypes.None, PublicTransportFlags.PrisonerTransport, ref layoutBuffer);
			}
			for (int k = 0; k < data3.m_VehicleCapacity; k++)
			{
				CreateVehicle(ref random, TransportType.Bus, EnergyTypes.FuelAndElectricity, SizeClass.Large, PublicTransportPurpose.Evacuation, Resource.NoResource, RoadTypes.Car, TrackTypes.None, PublicTransportFlags.Evacuating, ref layoutBuffer);
			}
			if (layoutBuffer.IsCreated)
			{
				layoutBuffer.Dispose();
			}
		}

		private void CreateVehicle(ref Random random, TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResource, RoadTypes roadType, TrackTypes trackType, PublicTransportFlags publicTransportFlags, ref NativeList<LayoutElement> layoutBuffer)
		{
			int2 passengerCapacity = 0;
			int2 cargoCapacity = 0;
			if (cargoResource != Resource.NoResource)
			{
				cargoCapacity = new int2(1, int.MaxValue);
			}
			else
			{
				passengerCapacity = new int2(1, int.MaxValue);
			}
			m_TransportVehicleSelectData.SelectVehicle(ref random, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResource, out var primaryPrefab, out var secondaryPrefab, ref passengerCapacity, ref cargoCapacity);
			if (!m_PrefabObjectGeometryData.TryGetComponent(primaryPrefab, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, trackType, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity = FindDeletedVehicle(primaryPrefab, secondaryPrefab, transform, m_DeletedVehicleMap);
			NativeArray<LayoutElement> nativeArray = default(NativeArray<LayoutElement>);
			if (entity != Entity.Null)
			{
				if (m_LayoutElements.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
				{
					nativeArray = bufferData.AsNativeArray();
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity vehicle = bufferData[i].m_Vehicle;
						m_CommandBuffer.RemoveComponent<Deleted>(0, vehicle);
						m_CommandBuffer.SetComponent(0, vehicle, transform);
						m_CommandBuffer.AddComponent<Updated>(0, vehicle);
					}
				}
				else
				{
					m_CommandBuffer.RemoveComponent<Deleted>(0, entity);
					m_CommandBuffer.SetComponent(0, entity, transform);
					m_CommandBuffer.AddComponent<Updated>(0, entity);
				}
			}
			else
			{
				StackList<VehicleModel> vehicleModels = stackalloc VehicleModel[1];
				vehicleModels.AddNoResize(new VehicleModel
				{
					m_PrimaryPrefab = primaryPrefab,
					m_SecondaryPrefab = secondaryPrefab
				});
				entity = m_TransportVehicleSelectData.CreateVehicle(m_CommandBuffer, 0, ref random, transform, m_Entity, vehicleModels, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResource, ref passengerCapacity, ref cargoCapacity, parked: true, ref layoutBuffer);
				if (layoutBuffer.IsCreated && layoutBuffer.Length != 0)
				{
					nativeArray = layoutBuffer.AsArray();
					if (m_IsTemp)
					{
						for (int j = 0; j < layoutBuffer.Length; j++)
						{
							Entity vehicle2 = layoutBuffer[j].m_Vehicle;
							m_CommandBuffer.AddComponent(0, vehicle2, default(Animation));
							m_CommandBuffer.AddComponent(0, vehicle2, default(InterpolatedTransform));
						}
					}
				}
				else if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(0, entity, default(Animation));
					m_CommandBuffer.AddComponent(0, entity, default(InterpolatedTransform));
				}
			}
			if (transportType == TransportType.Taxi)
			{
				m_CommandBuffer.SetComponent(0, entity, new Taxi(TaxiFlags.Disabled));
			}
			if (publicTransportPurpose != 0)
			{
				m_CommandBuffer.SetComponent(0, entity, new PublicTransport
				{
					m_State = (PublicTransportFlags.Disabled | publicTransportFlags)
				});
			}
			if (cargoResource != Resource.NoResource)
			{
				m_CommandBuffer.SetComponent(0, entity, new CargoTransport
				{
					m_State = CargoTransportFlags.Disabled
				});
			}
			if (nativeArray.IsCreated)
			{
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity vehicle3 = nativeArray[k].m_Vehicle;
					if (roadType != RoadTypes.None)
					{
						m_CommandBuffer.SetComponent(0, vehicle3, new ParkedCar(lane, curvePosition));
					}
					if (trackType != TrackTypes.None)
					{
						m_CommandBuffer.SetComponent(0, vehicle3, new ParkedTrain(lane));
					}
				}
			}
			else
			{
				m_CommandBuffer.SetComponent(0, entity, new ParkedCar(lane, curvePosition));
			}
			m_CommandBuffer.AddComponent(0, entity, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				if (nativeArray.IsCreated)
				{
					for (int l = 0; l < nativeArray.Length; l++)
					{
						Entity vehicle4 = nativeArray[l].m_Vehicle;
						m_CommandBuffer.AddComponent(0, vehicle4, component);
					}
				}
				else
				{
					m_CommandBuffer.AddComponent(0, entity, component);
				}
			}
			if (layoutBuffer.IsCreated)
			{
				layoutBuffer.Clear();
			}
		}
	}

	[BurstCompile]
	private struct SpawnPostVansJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> m_PrefabPostFacilityData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public PostVanSelectData m_PostVanSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabPostFacilityData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null && UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data2, ref m_PrefabRefData, ref m_PrefabPostFacilityData, ref m_InstalledUpgrades))
			{
				data.m_PostVanCapacity -= data2.m_PostVanCapacity;
			}
			for (int i = 0; i < data.m_PostVanCapacity; i++)
			{
				CreateVehicle(ref random, RoadTypes.Car);
			}
		}

		private void CreateVehicle(ref Random random, RoadTypes roadType)
		{
			int2 mailCapacity = new int2(1, int.MaxValue);
			Entity entity = m_PostVanSelectData.SelectVehicle(ref random, ref mailCapacity);
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_PostVanSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			m_CommandBuffer.SetComponent(entity2, new PostVan(PostVanFlags.Disabled, 0, 0));
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	[BurstCompile]
	private struct SpawnMaintenanceVehiclesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<MaintenanceDepotData> m_PrefabMaintenanceDepotData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public MaintenanceVehicleSelectData m_MaintenanceVehicleSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabMaintenanceDepotData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null && UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data2, ref m_PrefabRefData, ref m_PrefabMaintenanceDepotData, ref m_InstalledUpgrades))
			{
				data.m_VehicleCapacity -= data2.m_VehicleCapacity;
			}
			for (int i = 0; i < data.m_VehicleCapacity; i++)
			{
				CreateVehicle(ref random, data, RoadTypes.Car);
			}
		}

		private void CreateVehicle(ref Random random, MaintenanceDepotData maintenanceDepotData, RoadTypes roadType)
		{
			Entity entity = m_MaintenanceVehicleSelectData.SelectVehicle(ref random, MaintenanceType.None, maintenanceDepotData.m_MaintenanceType, GetMaxVehicleSize(m_Locations, roadType));
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_MaintenanceVehicleSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, MaintenanceType.None, maintenanceDepotData.m_MaintenanceType, float.MaxValue, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			m_CommandBuffer.SetComponent(entity2, new MaintenanceVehicle(MaintenanceVehicleFlags.Disabled, 0, maintenanceDepotData.m_VehicleEfficiency));
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	[BurstCompile]
	private struct SpawnGarbageTrucksJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_PrefabGarbageFacilityData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public Temp m_Temp;

		[ReadOnly]
		public bool m_IsTemp;

		[ReadOnly]
		public GarbageTruckSelectData m_GarbageTruckSelectData;

		public NativeList<ParkingLocation> m_Locations;

		[NativeDisableContainerSafetyRestriction]
		public NativeParallelMultiHashMap<Entity, DeletedVehicleData> m_DeletedVehicleMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Random random = m_PseudoRandomSeedData[m_Entity].GetRandom(PseudoRandomSeed.kParkedCars);
			UpgradeUtils.TryGetCombinedComponent(m_Entity, out var data, ref m_PrefabRefData, ref m_PrefabGarbageFacilityData, ref m_InstalledUpgrades);
			if (m_Temp.m_Original != Entity.Null && UpgradeUtils.TryGetCombinedComponent(m_Temp.m_Original, out var data2, ref m_PrefabRefData, ref m_PrefabGarbageFacilityData, ref m_InstalledUpgrades))
			{
				data.m_VehicleCapacity -= data2.m_VehicleCapacity;
			}
			for (int i = 0; i < data.m_VehicleCapacity; i++)
			{
				CreateVehicle(ref random, data, RoadTypes.Car);
			}
		}

		private void CreateVehicle(ref Random random, GarbageFacilityData garbageFacilityData, RoadTypes roadType)
		{
			int2 garbageCapacity = new int2(1, int.MaxValue);
			Entity entity = m_GarbageTruckSelectData.SelectVehicle(ref random, ref garbageCapacity);
			if (!m_PrefabObjectGeometryData.TryGetComponent(entity, out var componentData) || !SelectParkingSpace(ref random, m_Locations, componentData, roadType, TrackTypes.None, out var transform, out var lane, out var curvePosition))
			{
				return;
			}
			Entity entity2 = FindDeletedVehicle(entity, Entity.Null, transform, m_DeletedVehicleMap);
			if (entity2 != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(entity2);
				m_CommandBuffer.SetComponent(entity2, transform);
				m_CommandBuffer.AddComponent<Updated>(entity2);
			}
			else
			{
				entity2 = m_GarbageTruckSelectData.CreateVehicle(m_CommandBuffer, ref random, transform, m_Entity, entity, ref garbageCapacity, parked: true);
				if (m_IsTemp)
				{
					m_CommandBuffer.AddComponent(entity2, default(Animation));
					m_CommandBuffer.AddComponent(entity2, default(InterpolatedTransform));
				}
			}
			GarbageTruckFlags garbageTruckFlags = GarbageTruckFlags.Disabled;
			if (garbageFacilityData.m_IndustrialWasteOnly)
			{
				garbageTruckFlags |= GarbageTruckFlags.IndustrialWasteOnly;
			}
			m_CommandBuffer.SetComponent(entity2, new GarbageTruck(garbageTruckFlags, 0));
			m_CommandBuffer.SetComponent(entity2, new ParkedCar(lane, curvePosition));
			m_CommandBuffer.AddComponent(entity2, new Owner(m_Entity));
			if (m_IsTemp)
			{
				Temp component = new Temp
				{
					m_Flags = (m_Temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden | TempFlags.Duplicate))
				};
				m_CommandBuffer.AddComponent(entity2, component);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> __Game_Prefabs_MovingObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainObjectData> __Game_Prefabs_TrainObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> __Game_Prefabs_PoliceStationData_RO_ComponentLookup;

		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<FireStationData> __Game_Prefabs_FireStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HospitalData> __Game_Prefabs_HospitalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> __Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrisonData> __Game_Prefabs_PrisonData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EmergencyShelterData> __Game_Prefabs_EmergencyShelterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> __Game_Prefabs_PostFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceDepotData> __Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Prefabs_MovingObjectData_RO_ComponentLookup = state.GetComponentLookup<MovingObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Prefabs_TrainObjectData_RO_ComponentLookup = state.GetComponentLookup<TrainObjectData>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_PoliceStationData_RO_ComponentLookup = state.GetComponentLookup<PoliceStationData>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RW_BufferLookup = state.GetBufferLookup<InstalledUpgrade>();
			__Game_Prefabs_FireStationData_RO_ComponentLookup = state.GetComponentLookup<FireStationData>(isReadOnly: true);
			__Game_Prefabs_HospitalData_RO_ComponentLookup = state.GetComponentLookup<HospitalData>(isReadOnly: true);
			__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup = state.GetComponentLookup<DeathcareFacilityData>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
			__Game_Prefabs_PrisonData_RO_ComponentLookup = state.GetComponentLookup<PrisonData>(isReadOnly: true);
			__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup = state.GetComponentLookup<EmergencyShelterData>(isReadOnly: true);
			__Game_Prefabs_PostFacilityData_RO_ComponentLookup = state.GetComponentLookup<PostFacilityData>(isReadOnly: true);
			__Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup = state.GetComponentLookup<MaintenanceDepotData>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
		}
	}

	private ModificationBarrier4B m_ModificationBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_DeletedVehicleQuery;

	private EntityQuery m_PoliceCarQuery;

	private EntityQuery m_FireEngineQuery;

	private EntityQuery m_HealthcareVehicleQuery;

	private EntityQuery m_TransportVehicleQuery;

	private EntityQuery m_PostVanQuery;

	private EntityQuery m_MaintenanceVehicleQuery;

	private EntityQuery m_GarbageTruckQuery;

	private PoliceCarSelectData m_PoliceCarSelectData;

	private FireEngineSelectData m_FireEngineSelectData;

	private HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private PostVanSelectData m_PostVanSelectData;

	private MaintenanceVehicleSelectData m_MaintenanceVehicleSelectData;

	private GarbageTruckSelectData m_GarbageTruckSelectData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<OwnedVehicle>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Temp>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Applied>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>()
			}
		});
		m_DeletedVehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Temp>());
		m_PoliceCarQuery = GetEntityQuery(PoliceCarSelectData.GetEntityQueryDesc());
		m_FireEngineQuery = GetEntityQuery(FireEngineSelectData.GetEntityQueryDesc());
		m_HealthcareVehicleQuery = GetEntityQuery(HealthcareVehicleSelectData.GetEntityQueryDesc());
		m_TransportVehicleQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_PostVanQuery = GetEntityQuery(PostVanSelectData.GetEntityQueryDesc());
		m_MaintenanceVehicleQuery = GetEntityQuery(MaintenanceVehicleSelectData.GetEntityQueryDesc());
		m_GarbageTruckQuery = GetEntityQuery(GarbageTruckSelectData.GetEntityQueryDesc());
		m_PoliceCarSelectData = new PoliceCarSelectData(this);
		m_FireEngineSelectData = new FireEngineSelectData(this);
		m_HealthcareVehicleSelectData = new HealthcareVehicleSelectData(this);
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_PostVanSelectData = new PostVanSelectData(this);
		m_MaintenanceVehicleSelectData = new MaintenanceVehicleSelectData(this);
		m_GarbageTruckSelectData = new GarbageTruckSelectData(this);
		RequireForUpdate(m_BuildingQuery);
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_BuildingQuery.ToArchetypeChunkArray(Allocator.Temp);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		base.EntityManager.CompleteDependencyBeforeRO<PrefabRef>();
		base.EntityManager.CompleteDependencyBeforeRO<Temp>();
		try
		{
			NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap = default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>);
			JobHandle deletedVehiclesDeps = default(JobHandle);
			JobHandle dependency = base.Dependency;
			JobHandle jobHandle = default(JobHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<Temp> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity = nativeArray2[j];
					Temp value;
					bool flag = CollectionUtils.TryGet(nativeArray3, j, out value);
					if ((value.m_Flags & (TempFlags.Delete | TempFlags.Select | TempFlags.Duplicate)) != 0)
					{
						continue;
					}
					NativeList<ParkingLocation> parkingLocations = default(NativeList<ParkingLocation>);
					JobHandle parkingLocationDeps = default(JobHandle);
					if (flag && value.m_Original != Entity.Null)
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						DuplicateVehicles(entity, value, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.PoliceStation>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnPoliceCars(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.FireStation>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnFireEngines(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.Hospital>(entity) || base.EntityManager.HasComponent<Game.Buildings.DeathcareFacility>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnHealthcareVehicles(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.TransportDepot>(entity) || base.EntityManager.HasComponent<Game.Buildings.Prison>(entity) || base.EntityManager.HasComponent<Game.Buildings.EmergencyShelter>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnTransportVehicles(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.PostFacility>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnPostVans(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.MaintenanceDepot>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnMaintenanceVehicles(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (base.EntityManager.HasComponent<Game.Buildings.GarbageFacility>(entity))
					{
						FindParkingLocations(entity, ref parkingLocations, dependency, ref parkingLocationDeps);
						if (flag)
						{
							CollectDeletedVehicles(ref deletedVehicleMap, dependency, ref deletedVehiclesDeps);
						}
						SpawnGarbageTrucks(entity, value, flag, parkingLocations, deletedVehicleMap, ref parkingLocationDeps, ref deletedVehiclesDeps);
					}
					if (parkingLocations.IsCreated)
					{
						parkingLocations.Dispose(parkingLocationDeps);
						jobHandle = JobHandle.CombineDependencies(jobHandle, parkingLocationDeps);
					}
				}
			}
			if (deletedVehicleMap.IsCreated)
			{
				deletedVehicleMap.Dispose(deletedVehiclesDeps);
			}
			base.Dependency = jobHandle;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void FindParkingLocations(Entity entity, ref NativeList<ParkingLocation> parkingLocations, JobHandle inputDeps, ref JobHandle parkingLocationDeps)
	{
		if (!parkingLocations.IsCreated)
		{
			parkingLocations = new NativeList<ParkingLocation>(100, Allocator.TempJob);
			JobHandle jobHandle = IJobExtensions.Schedule(new FindParkingLocationsJob
			{
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocationElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
				m_Entity = entity,
				m_Locations = parkingLocations
			}, inputDeps);
			parkingLocationDeps = jobHandle;
		}
	}

	private void CollectDeletedVehicles(ref NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, JobHandle inputDeps, ref JobHandle deletedVehiclesDeps)
	{
		if (!deletedVehicleMap.IsCreated)
		{
			deletedVehicleMap = new NativeParallelMultiHashMap<Entity, DeletedVehicleData>(0, Allocator.TempJob);
			JobHandle outJobHandle;
			CollectDeletedVehiclesJob jobData = new CollectDeletedVehiclesJob
			{
				m_Chunks = m_DeletedVehicleQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedVehicleMap = deletedVehicleMap
			};
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, outJobHandle));
			jobData.m_Chunks.Dispose(jobHandle);
			deletedVehiclesDeps = jobHandle;
		}
	}

	private void DuplicateVehicles(Entity entity, Temp temp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		JobHandle jobHandle = IJobExtensions.Schedule(new DuplicateVehiclesJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMovingObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MovingObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = deletedVehicleMap,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps));
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		parkingLocationDeps = jobHandle;
		deletedVehiclesDeps = jobHandle;
	}

	private void SpawnPoliceCars(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_PoliceCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_PoliceCarQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnPoliceCarsJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_PoliceCarSelectData = m_PoliceCarSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_PoliceCarSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnFireEngines(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_FireEngineSelectData.PreUpdate(this, m_CityConfigurationSystem, m_FireEngineQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnFireEnginesJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_FireEngineSelectData = m_FireEngineSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_FireEngineSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnHealthcareVehicles(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_HealthcareVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_HealthcareVehicleQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnHealthcareVehiclesJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HospitalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeathcareFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_HealthcareVehicleSelectData = m_HealthcareVehicleSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_HealthcareVehicleSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnTransportVehicles(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_TransportVehicleQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnTransportVehiclesJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPrisonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrisonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEmergencyShelterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_TransportVehicleSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnPostVans(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_PostVanSelectData.PreUpdate(this, m_CityConfigurationSystem, m_PostVanQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnPostVansJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PostFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_PostVanSelectData = m_PostVanSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_PostVanSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnMaintenanceVehicles(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_MaintenanceVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_MaintenanceVehicleQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnMaintenanceVehiclesJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMaintenanceDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_MaintenanceVehicleSelectData = m_MaintenanceVehicleSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_MaintenanceVehicleSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private void SpawnGarbageTrucks(Entity entity, Temp temp, bool isTemp, NativeList<ParkingLocation> parkingLocations, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedVehicleMap, ref JobHandle parkingLocationDeps, ref JobHandle deletedVehiclesDeps)
	{
		m_GarbageTruckSelectData.PreUpdate(this, m_CityConfigurationSystem, m_GarbageTruckQuery, Allocator.TempJob, out var jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new SpawnGarbageTrucksJob
		{
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_Entity = entity,
			m_Temp = temp,
			m_IsTemp = isTemp,
			m_GarbageTruckSelectData = m_GarbageTruckSelectData,
			m_Locations = parkingLocations,
			m_DeletedVehicleMap = (isTemp ? deletedVehicleMap : default(NativeParallelMultiHashMap<Entity, DeletedVehicleData>)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, isTemp ? JobHandle.CombineDependencies(parkingLocationDeps, deletedVehiclesDeps, jobHandle) : JobHandle.CombineDependencies(parkingLocationDeps, jobHandle));
		m_GarbageTruckSelectData.PostUpdate(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
		parkingLocationDeps = jobHandle2;
		if (isTemp)
		{
			deletedVehiclesDeps = jobHandle2;
		}
	}

	private static Entity GetSecondaryPrefab(Entity primaryPrefab, DynamicBuffer<LayoutElement> layoutElements, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<PrefabData> prefabDatas, out bool validLayout)
	{
		Entity entity = Entity.Null;
		validLayout = true;
		for (int i = 0; i < layoutElements.Length; i++)
		{
			Entity vehicle = layoutElements[i].m_Vehicle;
			if (prefabRefs.TryGetComponent(vehicle, out var componentData) && componentData.m_Prefab != primaryPrefab)
			{
				if (entity == Entity.Null)
				{
					entity = componentData.m_Prefab;
				}
				validLayout &= prefabDatas.HasEnabledComponent(componentData.m_Prefab);
			}
		}
		return entity;
	}

	private static float4 GetMaxVehicleSize(NativeList<ParkingLocation> locations, RoadTypes roadType)
	{
		float4 @float = 0f;
		for (int i = 0; i < locations.Length; i++)
		{
			ParkingLocation parkingLocation = locations[i];
			@float = math.select(@float, parkingLocation.m_MaxSize.xyxy, (parkingLocation.m_MaxSize.xxyy > @float.xxww) & ((parkingLocation.m_ParkingLaneData.m_RoadTypes & roadType) != 0));
		}
		return @float;
	}

	private static bool SelectParkingSpace(ref Random random, NativeList<ParkingLocation> locations, ObjectGeometryData objectGeometryData, RoadTypes roadType, TrackTypes trackType, out Transform transform, out Entity lane, out float curvePosition)
	{
		float offset;
		float2 parkingSize = VehicleUtils.GetParkingSize(objectGeometryData, out offset);
		int num = 0;
		int num2 = -1;
		for (int i = 0; i < locations.Length; i++)
		{
			ParkingLocation parkingLocation = locations[i];
			if (!math.any(parkingSize > parkingLocation.m_MaxSize) && ((parkingLocation.m_ParkingLaneData.m_RoadTypes & roadType) != RoadTypes.None || (parkingLocation.m_TrackTypes & trackType) != TrackTypes.None))
			{
				int num3 = 100;
				num += num3;
				if (random.NextInt(num) < num3)
				{
					num2 = i;
				}
			}
		}
		if (num2 != -1)
		{
			ParkingLocation parkingLocation2 = locations[num2];
			lane = parkingLocation2.m_Lane;
			curvePosition = parkingLocation2.m_CurvePos;
			if (parkingLocation2.m_SpawnLocationType == SpawnLocationType.ParkingLane)
			{
				if (parkingLocation2.m_ParkingLaneData.m_SlotAngle <= 0.25f)
				{
					if (offset > 0f)
					{
						Bounds1 t = new Bounds1(curvePosition, 1f);
						MathUtils.ClampLength(parkingLocation2.m_Curve.m_Bezier, ref t, offset);
						curvePosition = t.max;
					}
					else if (offset < 0f)
					{
						Bounds1 t2 = new Bounds1(0f, curvePosition);
						MathUtils.ClampLengthInverse(parkingLocation2.m_Curve.m_Bezier, ref t2, 0f - offset);
						curvePosition = t2.min;
					}
				}
				transform = VehicleUtils.CalculateParkingSpaceTarget(parkingLocation2.m_ParkingLane, parkingLocation2.m_ParkingLaneData, objectGeometryData, parkingLocation2.m_Curve, parkingLocation2.m_OwnerTransform, curvePosition);
			}
			else
			{
				transform = parkingLocation2.m_OwnerTransform;
			}
			locations.RemoveAtSwapBack(num2);
			return true;
		}
		transform = default(Transform);
		lane = Entity.Null;
		curvePosition = 0f;
		return false;
	}

	private static Entity FindDeletedVehicle(Entity primaryPrefab, Entity secondaryPrefab, Transform transform, NativeParallelMultiHashMap<Entity, DeletedVehicleData> deletedMap)
	{
		Entity entity = Entity.Null;
		if (deletedMap.IsCreated && deletedMap.TryGetFirstValue(primaryPrefab, out var item, out var it))
		{
			float num = float.MaxValue;
			NativeParallelMultiHashMapIterator<Entity> it2 = default(NativeParallelMultiHashMapIterator<Entity>);
			do
			{
				if (!(item.m_SecondaryPrefab != secondaryPrefab))
				{
					float num2 = math.distance(item.m_Transform.m_Position, transform.m_Position);
					if (num2 < num)
					{
						entity = item.m_Entity;
						num = num2;
						it2 = it;
					}
				}
			}
			while (deletedMap.TryGetNextValue(out item, ref it));
			if (entity != Entity.Null)
			{
				deletedMap.Remove(it2);
			}
		}
		return entity;
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
	public ParkedVehiclesSystem()
	{
	}
}
