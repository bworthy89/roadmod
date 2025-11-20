#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Economy;
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
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TrafficSpawnerAISystem : GameSystemBase
{
	[BurstCompile]
	private struct TrafficSpawnerTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TrafficSpawner> m_TrafficSpawnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> m_CreatureDataType;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> m_ResidentDataType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<TrafficSpawnerData> m_PrefabTrafficSpawnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<RandomTrafficRequest> m_RandomTrafficRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocationElements;

		[ReadOnly]
		public float m_Loading;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_VehicleRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public PersonalCarSelectData m_PersonalCarSelectData;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CreaturePrefabChunks;

		[ReadOnly]
		public ComponentTypeSet m_CurrentLaneTypesRelative;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TrafficSpawner> nativeArray2 = chunk.GetNativeArray(ref m_TrafficSpawnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<ServiceDispatch> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Buildings.TrafficSpawner trafficSpawner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor[i];
				Tick(unfilteredChunkIndex, entity, ref random, trafficSpawner, prefabRef, dispatches);
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Random random, Game.Buildings.TrafficSpawner trafficSpawner, PrefabRef prefabRef, DynamicBuffer<ServiceDispatch> dispatches)
		{
			TrafficSpawnerData prefabTrafficSpawnerData = m_PrefabTrafficSpawnerData[prefabRef.m_Prefab];
			float num = prefabTrafficSpawnerData.m_SpawnRate * 4.266667f;
			float value = random.NextFloat(num * 0.5f, num * 1.5f);
			if (MathUtils.RoundToIntRandom(ref random, value) > 0 && !m_RandomTrafficRequestData.HasComponent(trafficSpawner.m_TrafficRequest))
			{
				RequestVehicle(jobIndex, ref random, entity, prefabTrafficSpawnerData);
			}
			for (int i = 0; i < dispatches.Length; i++)
			{
				Entity request = dispatches[i].m_Request;
				if (m_RandomTrafficRequestData.HasComponent(request))
				{
					int num2 = ((!(m_Loading < 0.9f)) ? 1 : (((prefabTrafficSpawnerData.m_RoadType & RoadTypes.Airplane) != RoadTypes.None) ? random.NextInt(2) : (((prefabTrafficSpawnerData.m_TrackType & TrackTypes.Train) == 0) ? 2 : 0)));
					for (int j = 0; j < num2; j++)
					{
						SpawnVehicle(jobIndex, ref random, entity, request, prefabTrafficSpawnerData);
					}
					dispatches.RemoveAt(i--);
				}
				else if (!m_ServiceRequestData.HasComponent(request))
				{
					dispatches.RemoveAt(i--);
				}
			}
		}

		private void RequestVehicle(int jobIndex, ref Random random, Entity entity, TrafficSpawnerData prefabTrafficSpawnerData)
		{
			SizeClass sizeClass = SizeClass.Small;
			RandomTrafficRequestFlags randomTrafficRequestFlags = (RandomTrafficRequestFlags)0;
			if ((prefabTrafficSpawnerData.m_RoadType & RoadTypes.Car) != RoadTypes.None)
			{
				int num = random.NextInt(100);
				if (num < 20)
				{
					sizeClass = SizeClass.Large;
					randomTrafficRequestFlags |= RandomTrafficRequestFlags.DeliveryTruck;
				}
				else if (num < 25)
				{
					sizeClass = SizeClass.Large;
					randomTrafficRequestFlags |= RandomTrafficRequestFlags.TransportVehicle;
				}
			}
			else
			{
				sizeClass = SizeClass.Large;
				randomTrafficRequestFlags |= RandomTrafficRequestFlags.TransportVehicle;
			}
			if (prefabTrafficSpawnerData.m_NoSlowVehicles)
			{
				randomTrafficRequestFlags |= RandomTrafficRequestFlags.NoSlowVehicles;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_VehicleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new RandomTrafficRequest(entity, prefabTrafficSpawnerData.m_RoadType, prefabTrafficSpawnerData.m_TrackType, EnergyTypes.FuelAndElectricity, sizeClass, randomTrafficRequestFlags));
			m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
		}

		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, TrafficSpawnerData prefabTrafficSpawnerData)
		{
			if (!m_RandomTrafficRequestData.TryGetComponent(request, out var componentData) || !m_PathInformationData.TryGetComponent(request, out var componentData2) || !m_PrefabRefData.HasComponent(componentData2.m_Destination))
			{
				return;
			}
			uint delay = random.NextUInt(256u);
			Entity entity2 = entity;
			Transform transform = m_TransformData[entity];
			int num = 0;
			m_PathElements.TryGetBuffer(request, out var bufferData);
			if (m_Loading < 0.9f)
			{
				delay = 0u;
				entity2 = Entity.Null;
				if (bufferData.IsCreated && bufferData.Length >= 5)
				{
					num = random.NextInt(2, bufferData.Length * 3 / 4);
					PathElement pathElement = bufferData[num];
					if (m_CurveData.TryGetComponent(pathElement.m_Target, out var componentData3))
					{
						float3 @float = MathUtils.Tangent(componentData3.m_Bezier, pathElement.m_TargetDelta.x);
						@float = math.select(@float, -@float, pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x);
						transform.m_Position = MathUtils.Position(componentData3.m_Bezier, pathElement.m_TargetDelta.x);
						transform.m_Rotation = quaternion.LookRotationSafe(@float, math.up());
					}
				}
			}
			Entity entity3 = Entity.Null;
			if ((componentData.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != 0)
			{
				Resource randomResource = GetRandomResource(ref random);
				m_DeliveryTruckSelectData.GetCapacityRange(Resource.NoResource, out var _, out var max);
				int amount = random.NextInt(1, max + max / 10 + 1);
				int returnAmount = 0;
				DeliveryTruckFlags deliveryTruckFlags = DeliveryTruckFlags.DummyTraffic;
				if (random.NextInt(100) < 75)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
				}
				if (m_DeliveryTruckSelectData.TrySelectItem(ref random, randomResource, amount, out var item))
				{
					entity3 = m_DeliveryTruckSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, ref m_PrefabDeliveryTruckData, ref m_PrefabObjectData, item, randomResource, Resource.NoResource, ref amount, ref returnAmount, transform, entity2, deliveryTruckFlags, delay);
				}
				int maxCount = 1;
				if (CreatePassengers(jobIndex, entity3, item.m_Prefab1, transform, driver: true, ref maxCount, ref random) > 0)
				{
					m_CommandBuffer.AddBuffer<Passenger>(jobIndex, entity3);
				}
			}
			else if ((componentData.m_Flags & RandomTrafficRequestFlags.TransportVehicle) != 0)
			{
				TransportType transportType = TransportType.None;
				PublicTransportPurpose publicTransportPurpose = (PublicTransportPurpose)0;
				Resource resource = Resource.NoResource;
				int2 passengerCapacity = 0;
				int2 cargoCapacity = 0;
				if ((componentData.m_RoadType & RoadTypes.Car) != RoadTypes.None)
				{
					transportType = TransportType.Bus;
					publicTransportPurpose = PublicTransportPurpose.TransportLine;
					passengerCapacity = new int2(1, int.MaxValue);
				}
				else if ((componentData.m_RoadType & RoadTypes.Airplane) != RoadTypes.None)
				{
					transportType = TransportType.Airplane;
					if (random.NextInt(100) < 25)
					{
						resource = Resource.Food;
						cargoCapacity = new int2(1, int.MaxValue);
					}
					else
					{
						publicTransportPurpose = PublicTransportPurpose.TransportLine;
						passengerCapacity = new int2(1, int.MaxValue);
					}
				}
				else if ((componentData.m_RoadType & RoadTypes.Watercraft) != RoadTypes.None)
				{
					transportType = TransportType.Ship;
					if (random.NextInt(100) < 50)
					{
						resource = Resource.Food;
						cargoCapacity = new int2(1, int.MaxValue);
					}
					else
					{
						publicTransportPurpose = PublicTransportPurpose.TransportLine;
						passengerCapacity = new int2(1, int.MaxValue);
					}
				}
				else if ((componentData.m_TrackType & TrackTypes.Train) != TrackTypes.None)
				{
					transportType = TransportType.Train;
					if (random.NextInt(100) < 50)
					{
						resource = Resource.Food;
						cargoCapacity = new int2(1, int.MaxValue);
					}
					else
					{
						publicTransportPurpose = PublicTransportPurpose.TransportLine;
						passengerCapacity = new int2(1, int.MaxValue);
					}
				}
				entity3 = m_TransportVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, transform, entity2, default(NativeList<VehicleModel>), transportType, componentData.m_EnergyTypes, componentData.m_SizeClass, publicTransportPurpose, resource, ref passengerCapacity, ref cargoCapacity, parked: false);
				if (entity3 != Entity.Null)
				{
					if (publicTransportPurpose != 0)
					{
						m_CommandBuffer.SetComponent(jobIndex, entity3, new Game.Vehicles.PublicTransport
						{
							m_State = PublicTransportFlags.DummyTraffic
						});
					}
					if (resource != Resource.NoResource)
					{
						m_CommandBuffer.SetComponent(jobIndex, entity3, new Game.Vehicles.CargoTransport
						{
							m_State = CargoTransportFlags.DummyTraffic
						});
						DynamicBuffer<LoadingResources> dynamicBuffer = m_CommandBuffer.SetBuffer<LoadingResources>(jobIndex, entity3);
						int num2 = random.NextInt(1, math.min(5, cargoCapacity.y + 1));
						int num3 = random.NextInt(num2, cargoCapacity.y + cargoCapacity.y / 10 + 1);
						int num4 = 0;
						for (int i = 0; i < num2; i++)
						{
							int num5 = random.NextInt(1, 100000);
							num4 += num5;
							dynamicBuffer.Add(new LoadingResources
							{
								m_Resource = GetRandomResource(ref random),
								m_Amount = num5
							});
						}
						for (int j = 0; j < num2; j++)
						{
							LoadingResources value = dynamicBuffer[j];
							int amount2 = value.m_Amount;
							value.m_Amount = (int)(((long)amount2 * (long)num3 + (num4 >> 1)) / num4);
							num4 -= amount2;
							num3 -= value.m_Amount;
							dynamicBuffer[j] = value;
						}
					}
				}
			}
			else
			{
				int maxCount2 = random.NextInt(1, 6);
				int num6 = random.NextInt(1, 6);
				if (random.NextInt(20) == 0)
				{
					maxCount2 += 5;
					num6 += 5;
				}
				else if (random.NextInt(10) == 0)
				{
					num6 += 5;
					if (random.NextInt(10) == 0)
					{
						num6 += 5;
					}
				}
				bool noSlowVehicles = prefabTrafficSpawnerData.m_NoSlowVehicles | ((componentData.m_Flags & RandomTrafficRequestFlags.NoSlowVehicles) != 0);
				entity3 = m_PersonalCarSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, maxCount2, num6, avoidTrailers: false, noSlowVehicles, bicycle: false, transform, entity2, Entity.Null, PersonalCarFlags.DummyTraffic, stopped: false, delay, out var trailer, out var vehiclePrefab, out var trailerPrefab);
				CreatePassengers(jobIndex, entity3, vehiclePrefab, transform, driver: true, ref maxCount2, ref random);
				CreatePassengers(jobIndex, trailer, trailerPrefab, transform, driver: false, ref maxCount2, ref random);
			}
			if (entity3 == Entity.Null)
			{
				return;
			}
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Target(componentData2.m_Destination));
			m_CommandBuffer.AddComponent(jobIndex, entity3, new Owner(entity));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity3, completed: true));
			if (entity2 == Entity.Null)
			{
				if ((componentData.m_RoadType & RoadTypes.Car) != RoadTypes.None)
				{
					CarCurrentLane component = default(CarCurrentLane);
					component.m_LaneFlags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
					m_CommandBuffer.SetComponent(jobIndex, entity3, component);
				}
				else if ((componentData.m_RoadType & RoadTypes.Airplane) != RoadTypes.None)
				{
					AircraftCurrentLane component2 = default(AircraftCurrentLane);
					component2.m_LaneFlags |= AircraftLaneFlags.ResetSpeed | AircraftLaneFlags.Flying;
					m_CommandBuffer.SetComponent(jobIndex, entity3, component2);
				}
				else if ((componentData.m_RoadType & RoadTypes.Watercraft) != RoadTypes.None)
				{
					WatercraftCurrentLane component3 = default(WatercraftCurrentLane);
					component3.m_LaneFlags |= WatercraftLaneFlags.ResetSpeed;
					m_CommandBuffer.SetComponent(jobIndex, entity3, component3);
				}
			}
			if (bufferData.IsCreated && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity3);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity3, new PathOwner(num, PathFlags.Updated));
				if ((componentData.m_Flags & RandomTrafficRequestFlags.DeliveryTruck) != 0)
				{
					m_CommandBuffer.SetComponent(jobIndex, entity3, componentData2);
				}
			}
		}

		private int CreatePassengers(int jobIndex, Entity vehicleEntity, Entity vehiclePrefab, Transform transform, bool driver, ref int maxCount, ref Random random)
		{
			int num = 0;
			if (maxCount > 0 && m_ActivityLocationElements.TryGetBuffer(vehiclePrefab, out var bufferData))
			{
				ActivityMask activityMask = new ActivityMask(ActivityType.Driving);
				activityMask.m_Mask |= new ActivityMask(ActivityType.Biking).m_Mask;
				int num2 = 0;
				int num3 = -1;
				float num4 = float.MinValue;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ActivityLocationElement activityLocationElement = bufferData[i];
					if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
					{
						num2++;
						bool test = ((activityLocationElement.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != 0 && m_LeftHandTraffic) || ((activityLocationElement.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != 0 && !m_LeftHandTraffic);
						activityLocationElement.m_Position.x = math.select(activityLocationElement.m_Position.x, 0f - activityLocationElement.m_Position.x, test);
						if ((!(math.abs(activityLocationElement.m_Position.x) >= 0.5f) || activityLocationElement.m_Position.x >= 0f == m_LeftHandTraffic) && activityLocationElement.m_Position.z > num4)
						{
							num3 = i;
							num4 = activityLocationElement.m_Position.z;
						}
					}
				}
				int num5 = 100;
				if (driver && num3 != -1)
				{
					maxCount--;
					num2--;
				}
				if (num2 > maxCount)
				{
					num5 = maxCount * 100 / num2;
				}
				Relative component = default(Relative);
				for (int j = 0; j < bufferData.Length; j++)
				{
					ActivityLocationElement activityLocationElement2 = bufferData[j];
					if ((activityLocationElement2.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0 && ((driver && j == num3) || random.NextInt(100) >= num5))
					{
						component.m_Position = activityLocationElement2.m_Position;
						component.m_Rotation = activityLocationElement2.m_Rotation;
						component.m_BoneIndex = new int3(0, -1, -1);
						Citizen citizenData = default(Citizen);
						if (random.NextBool())
						{
							citizenData.m_State |= CitizenFlags.Male;
						}
						if (driver)
						{
							citizenData.SetAge(CitizenAge.Adult);
						}
						else
						{
							citizenData.SetAge((CitizenAge)random.NextInt(4));
						}
						citizenData.m_PseudoRandom = (ushort)(random.NextUInt() % 65536);
						CreatureData creatureData;
						PseudoRandomSeed randomSeed;
						Entity entity = ObjectEmergeSystem.SelectResidentPrefab(citizenData, m_CreaturePrefabChunks, m_EntityType, ref m_CreatureDataType, ref m_ResidentDataType, out creatureData, out randomSeed);
						ObjectData objectData = m_PrefabObjectData[entity];
						PrefabRef component2 = new PrefabRef
						{
							m_Prefab = entity
						};
						Game.Creatures.Resident component3 = default(Game.Creatures.Resident);
						component3.m_Flags |= ResidentFlags.InVehicle | ResidentFlags.DummyTraffic;
						CurrentVehicle component4 = new CurrentVehicle
						{
							m_Vehicle = vehicleEntity
						};
						component4.m_Flags |= CreatureVehicleFlags.Ready;
						if (driver && j == num3)
						{
							component4.m_Flags |= CreatureVehicleFlags.Leader | CreatureVehicleFlags.Driver;
						}
						Entity e = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
						m_CommandBuffer.RemoveComponent(jobIndex, e, in m_CurrentLaneTypesRelative);
						m_CommandBuffer.SetComponent(jobIndex, e, transform);
						m_CommandBuffer.SetComponent(jobIndex, e, component2);
						m_CommandBuffer.SetComponent(jobIndex, e, component3);
						m_CommandBuffer.SetComponent(jobIndex, e, randomSeed);
						m_CommandBuffer.AddComponent(jobIndex, e, component4);
						m_CommandBuffer.AddComponent(jobIndex, e, component);
						num++;
					}
				}
			}
			return num;
		}

		private Resource GetRandomResource(ref Random random)
		{
			return random.NextInt(31) switch
			{
				0 => Resource.Grain, 
				1 => Resource.ConvenienceFood, 
				2 => Resource.Food, 
				3 => Resource.Vegetables, 
				4 => Resource.Meals, 
				5 => Resource.Wood, 
				6 => Resource.Timber, 
				7 => Resource.Paper, 
				8 => Resource.Furniture, 
				9 => Resource.Vehicles, 
				10 => Resource.UnsortedMail, 
				11 => Resource.Oil, 
				12 => Resource.Petrochemicals, 
				13 => Resource.Ore, 
				14 => Resource.Plastics, 
				15 => Resource.Metals, 
				16 => Resource.Electronics, 
				17 => Resource.Coal, 
				18 => Resource.Stone, 
				19 => Resource.Livestock, 
				20 => Resource.Cotton, 
				21 => Resource.Steel, 
				22 => Resource.Minerals, 
				23 => Resource.Concrete, 
				24 => Resource.Machinery, 
				25 => Resource.Chemicals, 
				26 => Resource.Pharmaceuticals, 
				27 => Resource.Beverages, 
				28 => Resource.Textiles, 
				29 => Resource.Garbage, 
				30 => Resource.Fish, 
				_ => Resource.NoResource, 
			};
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
		public ComponentTypeHandle<Game.Buildings.TrafficSpawner> __Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResidentData> __Game_Prefabs_ResidentData_RO_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TrafficSpawnerData> __Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RandomTrafficRequest> __Game_Simulation_RandomTrafficRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TrafficSpawner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreatureData>(isReadOnly: true);
			__Game_Prefabs_ResidentData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResidentData>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup = state.GetComponentLookup<TrafficSpawnerData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup = state.GetComponentLookup<RandomTrafficRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_PersonalCarQuery;

	private EntityQuery m_TransportVehicleQuery;

	private EntityQuery m_CreaturePrefabQuery;

	private SimulationSystem m_SimulationSystem;

	private ClimateSystem m_ClimateSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityArchetype m_TrafficRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_CurrentLaneTypesRelative;

	private PersonalCarSelectData m_PersonalCarSelectData;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase == SystemUpdatePhase.LoadSimulation)
		{
			return 16;
		}
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		if (phase == SystemUpdatePhase.LoadSimulation)
		{
			return 2;
		}
		return 32;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_PersonalCarSelectData = new PersonalCarSelectData(this);
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TrafficSpawner>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_PersonalCarQuery = GetEntityQuery(PersonalCarSelectData.GetEntityQueryDesc());
		m_TransportVehicleQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_CreaturePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureData>(), ComponentType.ReadOnly<PrefabData>());
		m_TrafficRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<RandomTrafficRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
		m_CurrentLaneTypesRelative = new ComponentTypeSet(new ComponentType[5]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<HumanNavigation>(),
			ComponentType.ReadWrite<HumanCurrentLane>(),
			ComponentType.ReadWrite<Blocker>()
		});
		RequireForUpdate(m_BuildingQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_PersonalCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_PersonalCarQuery, Allocator.TempJob, out var jobHandle);
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_TransportVehicleQuery, Allocator.TempJob, out var jobHandle2);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> creaturePrefabChunks = m_CreaturePrefabQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(new TrafficSpawnerTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TrafficSpawnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_TrafficSpawner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatureDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResidentDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResidentData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabTrafficSpawnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficSpawnerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomTrafficRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_RandomTrafficRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocationElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Loading = m_SimulationSystem.loadingProgress,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_RandomSeed = RandomSeed.Next(),
			m_VehicleRequestArchetype = m_TrafficRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_PersonalCarSelectData = m_PersonalCarSelectData,
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_CreaturePrefabChunks = creaturePrefabChunks,
			m_CurrentLaneTypesRelative = m_CurrentLaneTypesRelative,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_BuildingQuery, JobUtils.CombineDependencies(base.Dependency, jobHandle, jobHandle2, outJobHandle));
		m_PersonalCarSelectData.PostUpdate(jobHandle3);
		m_TransportVehicleSelectData.PostUpdate(jobHandle3);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
		creaturePrefabChunks.Dispose(jobHandle3);
		base.Dependency = jobHandle3;
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
	public TrafficSpawnerAISystem()
	{
	}
}
