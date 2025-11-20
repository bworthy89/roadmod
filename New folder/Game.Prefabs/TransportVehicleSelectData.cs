using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct TransportVehicleSelectData
{
	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PublicTransportVehicleData> m_PublicTransportVehicleType;

	private ComponentTypeHandle<CargoTransportVehicleData> m_CargoTransportVehicleType;

	private ComponentTypeHandle<TrainData> m_TrainType;

	private ComponentTypeHandle<TrainEngineData> m_TrainEngineType;

	private ComponentTypeHandle<TrainCarriageData> m_TrainCarriageType;

	private ComponentTypeHandle<MultipleUnitTrainData> m_MultipleUnitTrainType;

	private ComponentTypeHandle<TaxiData> m_TaxiType;

	private ComponentTypeHandle<CarData> m_CarType;

	private ComponentTypeHandle<AircraftData> m_AircraftType;

	private ComponentTypeHandle<AirplaneData> m_AirplaneType;

	private ComponentTypeHandle<HelicopterData> m_HelicopterType;

	private ComponentTypeHandle<WatercraftData> m_WatercraftType;

	private ComponentLookup<ObjectData> m_ObjectData;

	private ComponentLookup<MovingObjectData> m_MovingObjectData;

	private ComponentLookup<TrainObjectData> m_TrainObjectData;

	private ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleData;

	private ComponentLookup<CargoTransportVehicleData> m_CargoTransportVehicleData;

	private BufferLookup<VehicleCarriageElement> m_VehicleCarriages;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<VehicleData>(),
			ComponentType.ReadOnly<ObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.Any = new ComponentType[4]
		{
			ComponentType.ReadOnly<PublicTransportVehicleData>(),
			ComponentType.ReadOnly<CargoTransportVehicleData>(),
			ComponentType.ReadOnly<TrainEngineData>(),
			ComponentType.ReadOnly<TaxiData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public TransportVehicleSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_PublicTransportVehicleType = system.GetComponentTypeHandle<PublicTransportVehicleData>(isReadOnly: true);
		m_CargoTransportVehicleType = system.GetComponentTypeHandle<CargoTransportVehicleData>(isReadOnly: true);
		m_TrainType = system.GetComponentTypeHandle<TrainData>(isReadOnly: true);
		m_TrainEngineType = system.GetComponentTypeHandle<TrainEngineData>(isReadOnly: true);
		m_TrainCarriageType = system.GetComponentTypeHandle<TrainCarriageData>(isReadOnly: true);
		m_MultipleUnitTrainType = system.GetComponentTypeHandle<MultipleUnitTrainData>(isReadOnly: true);
		m_TaxiType = system.GetComponentTypeHandle<TaxiData>(isReadOnly: true);
		m_CarType = system.GetComponentTypeHandle<CarData>(isReadOnly: true);
		m_AircraftType = system.GetComponentTypeHandle<AircraftData>(isReadOnly: true);
		m_AirplaneType = system.GetComponentTypeHandle<AirplaneData>(isReadOnly: true);
		m_HelicopterType = system.GetComponentTypeHandle<HelicopterData>(isReadOnly: true);
		m_WatercraftType = system.GetComponentTypeHandle<WatercraftData>(isReadOnly: true);
		m_ObjectData = system.GetComponentLookup<ObjectData>(isReadOnly: true);
		m_MovingObjectData = system.GetComponentLookup<MovingObjectData>(isReadOnly: true);
		m_TrainObjectData = system.GetComponentLookup<TrainObjectData>(isReadOnly: true);
		m_PublicTransportVehicleData = system.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
		m_CargoTransportVehicleData = system.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
		m_VehicleCarriages = system.GetBufferLookup<VehicleCarriageElement>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_PublicTransportVehicleType.Update(system);
		m_CargoTransportVehicleType.Update(system);
		m_TrainType.Update(system);
		m_TrainEngineType.Update(system);
		m_TrainCarriageType.Update(system);
		m_MultipleUnitTrainType.Update(system);
		m_TaxiType.Update(system);
		m_CarType.Update(system);
		m_AircraftType.Update(system);
		m_AirplaneType.Update(system);
		m_HelicopterType.Update(system);
		m_WatercraftType.Update(system);
		m_ObjectData.Update(system);
		m_MovingObjectData.Update(system);
		m_TrainObjectData.Update(system);
		m_PublicTransportVehicleData.Update(system);
		m_CargoTransportVehicleData.Update(system);
		m_VehicleCarriages.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public void ListVehicles(TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResources, NativeList<Entity> primaryPrefabs, NativeList<Entity> secondaryPrefabs, bool ignoreTheme = false)
	{
		Random random = Random.CreateFromIndex(0u);
		int2 passengerCapacity = ((publicTransportPurpose != 0) ? new int2(1, int.MaxValue) : ((int2)0));
		int2 cargoCapacity = ((cargoResources != Resource.NoResource) ? new int2(1, int.MaxValue) : ((int2)0));
		GetRandomVehicle(ref random, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResources, default(NativeList<VehicleModel>), primaryPrefabs, secondaryPrefabs, ignoreTheme, out var _, out var _, out var _, ref passengerCapacity, ref cargoCapacity);
	}

	public void SelectVehicle(ref Random random, TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResources, out Entity primaryPrefab, out Entity secondaryPrefab, ref int2 passengerCapacity, ref int2 cargoCapacity)
	{
		primaryPrefab = GetRandomVehicle(ref random, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResources, default(NativeList<VehicleModel>), default(NativeList<Entity>), default(NativeList<Entity>), ignoreTheme: false, out var _, out var _, out secondaryPrefab, ref passengerCapacity, ref cargoCapacity);
	}

	public Entity CreateVehicle<TVehicleModelList>(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, Transform transform, Entity source, TVehicleModelList vehicleModels, TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResources, ref int2 passengerCapacity, ref int2 cargoCapacity, bool parked) where TVehicleModelList : unmanaged, INativeList<VehicleModel>
	{
		NativeList<LayoutElement> layout = default(NativeList<LayoutElement>);
		Entity result = CreateVehicle(commandBuffer, jobIndex, ref random, transform, source, vehicleModels, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResources, ref passengerCapacity, ref cargoCapacity, parked, ref layout);
		if (layout.IsCreated)
		{
			layout.Dispose();
		}
		return result;
	}

	public Entity CreateVehicle<TVehicleModelList>(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, Transform transform, Entity source, TVehicleModelList vehicleModels, TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResources, ref int2 passengerCapacity, ref int2 cargoCapacity, bool parked, ref NativeList<LayoutElement> layout) where TVehicleModelList : unmanaged, INativeList<VehicleModel>
	{
		bool isMultipleUnitTrain;
		int unitCount;
		Entity secondaryResult;
		Entity randomVehicle = GetRandomVehicle(ref random, transportType, energyTypes, sizeClass, publicTransportPurpose, cargoResources, vehicleModels, default(NativeList<Entity>), default(NativeList<Entity>), ignoreTheme: false, out isMultipleUnitTrain, out unitCount, out secondaryResult, ref passengerCapacity, ref cargoCapacity);
		if (randomVehicle == Entity.Null)
		{
			return Entity.Null;
		}
		Entity entity = ((transportType != TransportType.Train && transportType != TransportType.Tram && transportType != TransportType.Subway) ? commandBuffer.CreateEntity(jobIndex, GetArchetype(randomVehicle, controller: false, parked)) : commandBuffer.CreateEntity(jobIndex, GetArchetype(randomVehicle, controller: true, parked)));
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(randomVehicle));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		AddTransportComponents(commandBuffer, jobIndex, publicTransportPurpose, entity);
		if (!parked && source != Entity.Null)
		{
			commandBuffer.AddComponent(jobIndex, entity, new TripSource(source));
			commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		}
		bool flag = false;
		if (transportType == TransportType.Train || transportType == TransportType.Tram || transportType == TransportType.Subway)
		{
			commandBuffer.SetComponent(jobIndex, entity, new Controller(entity));
			flag = true;
			if (layout.IsCreated)
			{
				layout.Clear();
			}
			else
			{
				layout = new NativeList<LayoutElement>(32, Allocator.Temp);
			}
		}
		if (flag)
		{
			int num = 0;
			if (isMultipleUnitTrain)
			{
				layout.Add(new LayoutElement(entity));
			}
			Entity entity2 = (isMultipleUnitTrain ? randomVehicle : secondaryResult);
			if (entity2 != Entity.Null)
			{
				EntityArchetype archetype = GetArchetype(entity2, controller: false, parked);
				for (int i = 0; i < unitCount; i++)
				{
					if (!isMultipleUnitTrain || i != 0)
					{
						Game.Vehicles.TrainFlags trainFlags = (Game.Vehicles.TrainFlags)0u;
						if (!isMultipleUnitTrain && i != 0 && random.NextBool())
						{
							trainFlags |= Game.Vehicles.TrainFlags.Reversed;
						}
						Entity entity3 = commandBuffer.CreateEntity(jobIndex, archetype);
						commandBuffer.SetComponent(jobIndex, entity3, transform);
						commandBuffer.SetComponent(jobIndex, entity3, new PrefabRef(entity2));
						commandBuffer.SetComponent(jobIndex, entity3, new Controller(entity));
						commandBuffer.SetComponent(jobIndex, entity3, new Train(trainFlags));
						commandBuffer.SetComponent(jobIndex, entity3, new PseudoRandomSeed(ref random));
						AddTransportComponents(commandBuffer, jobIndex, publicTransportPurpose, entity3);
						if (!parked && source != Entity.Null)
						{
							commandBuffer.AddComponent(jobIndex, entity3, new TripSource(source));
							commandBuffer.AddComponent(jobIndex, entity3, default(Unspawned));
						}
						layout.Add(new LayoutElement(entity3));
					}
					if (!m_VehicleCarriages.HasBuffer(entity2))
					{
						continue;
					}
					DynamicBuffer<VehicleCarriageElement> dynamicBuffer = m_VehicleCarriages[entity2];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						VehicleCarriageElement vehicleCarriageElement = dynamicBuffer[j];
						if (vehicleCarriageElement.m_Prefab == Entity.Null)
						{
							num += vehicleCarriageElement.m_Count.x;
							continue;
						}
						EntityArchetype archetype2 = GetArchetype(vehicleCarriageElement.m_Prefab, controller: false, parked);
						for (int k = 0; k < vehicleCarriageElement.m_Count.x; k++)
						{
							Game.Vehicles.TrainFlags trainFlags2 = (Game.Vehicles.TrainFlags)0u;
							switch (vehicleCarriageElement.m_Direction)
							{
							case VehicleCarriageDirection.Reversed:
								trainFlags2 |= Game.Vehicles.TrainFlags.Reversed;
								break;
							case VehicleCarriageDirection.Random:
								if (random.NextBool())
								{
									trainFlags2 |= Game.Vehicles.TrainFlags.Reversed;
								}
								break;
							}
							Entity entity4 = commandBuffer.CreateEntity(jobIndex, archetype2);
							commandBuffer.SetComponent(jobIndex, entity4, transform);
							commandBuffer.SetComponent(jobIndex, entity4, new PrefabRef(vehicleCarriageElement.m_Prefab));
							commandBuffer.SetComponent(jobIndex, entity4, new Controller(entity));
							commandBuffer.SetComponent(jobIndex, entity4, new Train(trainFlags2));
							commandBuffer.SetComponent(jobIndex, entity4, new PseudoRandomSeed(ref random));
							AddTransportComponents(commandBuffer, jobIndex, publicTransportPurpose, entity4);
							if (!parked && source != Entity.Null)
							{
								commandBuffer.AddComponent(jobIndex, entity4, new TripSource(source));
								commandBuffer.AddComponent(jobIndex, entity4, default(Unspawned));
							}
							layout.Add(new LayoutElement(entity4));
						}
					}
				}
			}
			if (!isMultipleUnitTrain)
			{
				layout.Add(new LayoutElement(entity));
				num--;
			}
			if (num > 0)
			{
				EntityArchetype archetype3 = GetArchetype(randomVehicle, controller: false, parked);
				for (int l = 0; l < num; l++)
				{
					Game.Vehicles.TrainFlags trainFlags3 = (Game.Vehicles.TrainFlags)0u;
					if (random.NextBool())
					{
						trainFlags3 |= Game.Vehicles.TrainFlags.Reversed;
					}
					Entity entity5 = commandBuffer.CreateEntity(jobIndex, archetype3);
					commandBuffer.SetComponent(jobIndex, entity5, transform);
					commandBuffer.SetComponent(jobIndex, entity5, new PrefabRef(randomVehicle));
					commandBuffer.SetComponent(jobIndex, entity5, new Controller(entity));
					commandBuffer.SetComponent(jobIndex, entity5, new Train(trainFlags3));
					commandBuffer.SetComponent(jobIndex, entity5, new PseudoRandomSeed(ref random));
					AddTransportComponents(commandBuffer, jobIndex, publicTransportPurpose, entity5);
					if (!parked && source != Entity.Null)
					{
						commandBuffer.AddComponent(jobIndex, entity5, new TripSource(source));
						commandBuffer.AddComponent(jobIndex, entity5, default(Unspawned));
					}
					layout.Add(new LayoutElement(entity5));
				}
			}
			commandBuffer.SetBuffer<LayoutElement>(jobIndex, entity).CopyFrom(layout.AsArray());
		}
		return entity;
	}

	private void AddTransportComponents(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, PublicTransportPurpose publicTransportPurpose, Entity entity)
	{
		if ((publicTransportPurpose & PublicTransportPurpose.TransportLine) != 0)
		{
			commandBuffer.AddComponent(jobIndex, entity, default(PassengerTransport));
		}
		if ((publicTransportPurpose & PublicTransportPurpose.Evacuation) != 0)
		{
			commandBuffer.AddComponent(jobIndex, entity, default(EvacuatingTransport));
		}
		if ((publicTransportPurpose & PublicTransportPurpose.PrisonerTransport) != 0)
		{
			commandBuffer.AddComponent(jobIndex, entity, default(PrisonerTransport));
		}
	}

	private EntityArchetype GetArchetype(Entity prefab, bool controller, bool parked)
	{
		if (controller)
		{
			TrainObjectData trainObjectData = m_TrainObjectData[prefab];
			if (!parked)
			{
				return trainObjectData.m_ControllerArchetype;
			}
			return trainObjectData.m_StoppedControllerArchetype;
		}
		if (parked)
		{
			return m_MovingObjectData[prefab].m_StoppedArchetype;
		}
		return m_ObjectData[prefab].m_Archetype;
	}

	private Entity GetRandomVehicle<TVehicleModelList>(ref Random random, TransportType transportType, EnergyTypes energyTypes, SizeClass sizeClass, PublicTransportPurpose publicTransportPurpose, Resource cargoResources, TVehicleModelList vehicleModels, NativeList<Entity> primaryPrefabs, NativeList<Entity> secondaryPrefabs, bool ignoreTheme, out bool isMultipleUnitTrain, out int unitCount, out Entity secondaryResult, ref int2 passengerCapacity, ref int2 cargoCapacity) where TVehicleModelList : unmanaged, INativeList<VehicleModel>
	{
		Entity entity = Entity.Null;
		secondaryResult = Entity.Null;
		int2 @int = 0;
		int2 int2 = 0;
		isMultipleUnitTrain = false;
		unitCount = 1;
		int num = 1;
		TrackTypes trackTypes = TrackTypes.None;
		HelicopterType helicopterType = HelicopterType.Helicopter;
		switch (transportType)
		{
		case TransportType.Train:
			trackTypes = TrackTypes.Train;
			break;
		case TransportType.Tram:
			trackTypes = TrackTypes.Tram;
			break;
		case TransportType.Subway:
			trackTypes = TrackTypes.Subway;
			break;
		case TransportType.Helicopter:
			helicopterType = HelicopterType.Helicopter;
			break;
		case TransportType.Rocket:
			helicopterType = HelicopterType.Rocket;
			break;
		}
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			switch (transportType)
			{
			case TransportType.Bus:
			{
				NativeArray<CarData> nativeArray18 = chunk.GetNativeArray(ref m_CarType);
				if (nativeArray18.Length == 0)
				{
					break;
				}
				NativeArray<Entity> nativeArray19 = chunk.GetNativeArray(m_EntityType);
				NativeArray<PublicTransportVehicleData> nativeArray20 = chunk.GetNativeArray(ref m_PublicTransportVehicleType);
				if (nativeArray20.Length == 0)
				{
					break;
				}
				VehicleSelectRequirementData.Chunk chunk6 = m_RequirementData.GetChunk(chunk);
				for (int num6 = 0; num6 < nativeArray18.Length; num6++)
				{
					if ((nativeArray20[num6].m_PurposeMask & publicTransportPurpose) == 0)
					{
						continue;
					}
					CarData carData2 = nativeArray18[num6];
					if ((carData2.m_EnergyType != EnergyTypes.None && (carData2.m_EnergyType & energyTypes) == 0) || carData2.m_SizeClass != sizeClass)
					{
						continue;
					}
					Entity value6 = nativeArray19[num6];
					bool flag9 = ValidatePrimaryModel(vehicleModels, value6);
					if (m_RequirementData.CheckRequirements(ref chunk6, num6, ignoreTheme || flag9))
					{
						int num7 = math.select(0, 2, flag9);
						num7 += math.select(0, 1, (carData2.m_EnergyType & EnergyTypes.Fuel) != 0);
						if (primaryPrefabs.IsCreated)
						{
							primaryPrefabs.Add(in value6);
						}
						if (PickVehicle(ref random, 100, num7, ref @int.x, ref int2.x))
						{
							entity = value6;
							passengerCapacity = nativeArray20[num6].m_PassengerCapacity;
						}
					}
				}
				break;
			}
			case TransportType.Train:
			case TransportType.Tram:
			case TransportType.Subway:
			{
				NativeArray<TrainData> nativeArray10 = chunk.GetNativeArray(ref m_TrainType);
				if (nativeArray10.Length == 0)
				{
					break;
				}
				NativeArray<Entity> nativeArray11 = chunk.GetNativeArray(m_EntityType);
				NativeArray<TrainEngineData> nativeArray12 = chunk.GetNativeArray(ref m_TrainEngineType);
				NativeArray<PublicTransportVehicleData> nativeArray13 = chunk.GetNativeArray(ref m_PublicTransportVehicleType);
				NativeArray<CargoTransportVehicleData> nativeArray14 = chunk.GetNativeArray(ref m_CargoTransportVehicleType);
				bool flag3 = nativeArray12.Length != 0;
				bool flag4 = chunk.Has(ref m_TrainCarriageType);
				bool flag5 = chunk.Has(ref m_MultipleUnitTrainType);
				VehicleSelectRequirementData.Chunk chunk4 = m_RequirementData.GetChunk(chunk);
				if ((flag3 && flag5) || (flag4 && !flag5))
				{
					if (publicTransportPurpose != (PublicTransportPurpose)0 != (nativeArray13.Length != 0) || cargoResources != Resource.NoResource != (nativeArray14.Length != 0))
					{
						break;
					}
					for (int l = 0; l < nativeArray10.Length; l++)
					{
						if ((publicTransportPurpose != 0 && (nativeArray13[l].m_PurposeMask & publicTransportPurpose) == 0) || (cargoResources != Resource.NoResource && (nativeArray14[l].m_Resources & cargoResources) == Resource.NoResource))
						{
							continue;
						}
						TrainData trainData = nativeArray10[l];
						if ((trainData.m_EnergyType != EnergyTypes.None && (trainData.m_EnergyType & energyTypes) == 0) || trainData.m_TrackType != trackTypes)
						{
							continue;
						}
						Entity value3 = nativeArray11[l];
						bool flag6 = ValidatePrimaryModel(vehicleModels, value3);
						if (!m_RequirementData.CheckRequirements(ref chunk4, l, ignoreTheme || flag6))
						{
							continue;
						}
						int num3 = math.select(0, 2, flag6);
						num3 += math.select(0, 1, (trainData.m_EnergyType & EnergyTypes.Fuel) != 0);
						if (primaryPrefabs.IsCreated)
						{
							primaryPrefabs.Add(in value3);
						}
						if (PickVehicle(ref random, 100, num3, ref @int.x, ref int2.x))
						{
							isMultipleUnitTrain = flag5;
							if (flag3)
							{
								unitCount = nativeArray12[l].m_Count.x;
							}
							entity = value3;
							if (publicTransportPurpose != 0)
							{
								passengerCapacity = nativeArray13[l].m_PassengerCapacity;
							}
							if (cargoResources != Resource.NoResource)
							{
								cargoCapacity = nativeArray14[l].m_CargoCapacity;
							}
						}
					}
				}
				else
				{
					if (!flag3 || flag5)
					{
						break;
					}
					for (int m = 0; m < nativeArray10.Length; m++)
					{
						TrainData trainData2 = nativeArray10[m];
						if ((trainData2.m_EnergyType != EnergyTypes.None && (trainData2.m_EnergyType & energyTypes) == 0) || trainData2.m_TrackType != trackTypes)
						{
							continue;
						}
						Entity value4 = nativeArray11[m];
						bool flag7 = ValidateSecondaryModel(vehicleModels, value4);
						if (m_RequirementData.CheckRequirements(ref chunk4, m, ignoreTheme || flag7))
						{
							int num4 = math.select(0, 2, flag7);
							num4 += math.select(0, 1, (trainData2.m_EnergyType & EnergyTypes.Fuel) != 0);
							if (secondaryPrefabs.IsCreated)
							{
								secondaryPrefabs.Add(in value4);
							}
							if (PickVehicle(ref random, 100, num4, ref @int.y, ref int2.y))
							{
								num = nativeArray12[m].m_Count.x;
								secondaryResult = value4;
							}
						}
					}
				}
				break;
			}
			case TransportType.Taxi:
			{
				NativeArray<CarData> nativeArray15 = chunk.GetNativeArray(ref m_CarType);
				if (nativeArray15.Length == 0)
				{
					break;
				}
				NativeArray<Entity> nativeArray16 = chunk.GetNativeArray(m_EntityType);
				NativeArray<TaxiData> nativeArray17 = chunk.GetNativeArray(ref m_TaxiType);
				if (nativeArray17.Length == 0)
				{
					break;
				}
				VehicleSelectRequirementData.Chunk chunk5 = m_RequirementData.GetChunk(chunk);
				for (int n = 0; n < nativeArray15.Length; n++)
				{
					CarData carData = nativeArray15[n];
					if ((carData.m_EnergyType != EnergyTypes.None && (carData.m_EnergyType & energyTypes) == 0) || carData.m_SizeClass != sizeClass)
					{
						continue;
					}
					Entity value5 = nativeArray16[n];
					bool flag8 = ValidatePrimaryModel(vehicleModels, value5);
					if (m_RequirementData.CheckRequirements(ref chunk5, n, ignoreTheme || flag8))
					{
						int num5 = math.select(0, 2, flag8);
						num5 += math.select(0, 1, (carData.m_EnergyType & EnergyTypes.Electricity) != 0);
						if (primaryPrefabs.IsCreated)
						{
							primaryPrefabs.Add(in value5);
						}
						if (PickVehicle(ref random, 100, num5, ref @int.x, ref int2.x))
						{
							entity = value5;
							passengerCapacity = nativeArray17[n].m_PassengerCapacity;
						}
					}
				}
				break;
			}
			case TransportType.Ship:
			case TransportType.Ferry:
			{
				NativeArray<WatercraftData> nativeArray6 = chunk.GetNativeArray(ref m_WatercraftType);
				if (nativeArray6.Length == 0)
				{
					break;
				}
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				NativeArray<PublicTransportVehicleData> nativeArray8 = chunk.GetNativeArray(ref m_PublicTransportVehicleType);
				NativeArray<CargoTransportVehicleData> nativeArray9 = chunk.GetNativeArray(ref m_CargoTransportVehicleType);
				if (publicTransportPurpose != (PublicTransportPurpose)0 != (nativeArray8.Length != 0) || cargoResources != Resource.NoResource != (nativeArray9.Length != 0))
				{
					break;
				}
				VehicleSelectRequirementData.Chunk chunk3 = m_RequirementData.GetChunk(chunk);
				for (int k = 0; k < nativeArray6.Length; k++)
				{
					WatercraftData watercraftData = nativeArray6[k];
					if ((watercraftData.m_EnergyType != EnergyTypes.None && (watercraftData.m_EnergyType & energyTypes) == 0) || watercraftData.m_SizeClass != sizeClass || (publicTransportPurpose != 0 && (nativeArray8[k].m_PurposeMask & publicTransportPurpose) == 0) || (cargoResources != Resource.NoResource && (nativeArray9[k].m_Resources & cargoResources) == Resource.NoResource))
					{
						continue;
					}
					Entity value2 = nativeArray7[k];
					bool flag2 = ValidatePrimaryModel(vehicleModels, value2);
					if (!m_RequirementData.CheckRequirements(ref chunk3, k, ignoreTheme || flag2))
					{
						continue;
					}
					int num2 = math.select(0, 2, flag2);
					num2 += math.select(0, 1, (watercraftData.m_EnergyType & EnergyTypes.Fuel) != 0);
					if (primaryPrefabs.IsCreated)
					{
						primaryPrefabs.Add(in value2);
					}
					if (PickVehicle(ref random, 100, num2, ref @int.x, ref int2.x))
					{
						entity = value2;
						if (publicTransportPurpose != 0)
						{
							passengerCapacity = nativeArray8[k].m_PassengerCapacity;
						}
						if (cargoResources != Resource.NoResource)
						{
							cargoCapacity = nativeArray9[k].m_CargoCapacity;
						}
					}
				}
				break;
			}
			case TransportType.Airplane:
			{
				if (!chunk.Has(ref m_AirplaneType))
				{
					break;
				}
				NativeArray<Entity> nativeArray21 = chunk.GetNativeArray(m_EntityType);
				NativeArray<AircraftData> nativeArray22 = chunk.GetNativeArray(ref m_AircraftType);
				NativeArray<PublicTransportVehicleData> nativeArray23 = chunk.GetNativeArray(ref m_PublicTransportVehicleType);
				NativeArray<CargoTransportVehicleData> nativeArray24 = chunk.GetNativeArray(ref m_CargoTransportVehicleType);
				if (publicTransportPurpose != (PublicTransportPurpose)0 != (nativeArray23.Length != 0) || cargoResources != Resource.NoResource != (nativeArray24.Length != 0))
				{
					break;
				}
				VehicleSelectRequirementData.Chunk chunk7 = m_RequirementData.GetChunk(chunk);
				for (int num8 = 0; num8 < nativeArray22.Length; num8++)
				{
					if (nativeArray22[num8].m_SizeClass != sizeClass || (publicTransportPurpose != 0 && (nativeArray23[num8].m_PurposeMask & publicTransportPurpose) == 0) || (cargoResources != Resource.NoResource && (nativeArray24[num8].m_Resources & cargoResources) == Resource.NoResource))
					{
						continue;
					}
					Entity value7 = nativeArray21[num8];
					bool flag10 = ValidatePrimaryModel(vehicleModels, value7);
					if (!m_RequirementData.CheckRequirements(ref chunk7, num8, ignoreTheme || flag10))
					{
						continue;
					}
					int priority2 = math.select(0, 2, flag10);
					if (primaryPrefabs.IsCreated)
					{
						primaryPrefabs.Add(in value7);
					}
					if (PickVehicle(ref random, 100, priority2, ref @int.x, ref int2.x))
					{
						entity = value7;
						if (publicTransportPurpose != 0)
						{
							passengerCapacity = nativeArray23[num8].m_PassengerCapacity;
						}
						if (cargoResources != Resource.NoResource)
						{
							cargoCapacity = nativeArray24[num8].m_CargoCapacity;
						}
					}
				}
				break;
			}
			case TransportType.Helicopter:
			case TransportType.Rocket:
			{
				NativeArray<HelicopterData> nativeArray = chunk.GetNativeArray(ref m_HelicopterType);
				if (nativeArray.Length == 0)
				{
					break;
				}
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				NativeArray<AircraftData> nativeArray3 = chunk.GetNativeArray(ref m_AircraftType);
				NativeArray<PublicTransportVehicleData> nativeArray4 = chunk.GetNativeArray(ref m_PublicTransportVehicleType);
				NativeArray<CargoTransportVehicleData> nativeArray5 = chunk.GetNativeArray(ref m_CargoTransportVehicleType);
				if (publicTransportPurpose != (PublicTransportPurpose)0 != (nativeArray4.Length != 0) || cargoResources != Resource.NoResource != (nativeArray5.Length != 0))
				{
					break;
				}
				VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (nativeArray3[j].m_SizeClass != sizeClass || (publicTransportPurpose != 0 && (nativeArray4[j].m_PurposeMask & publicTransportPurpose) == 0) || (cargoResources != Resource.NoResource && (nativeArray5[j].m_Resources & cargoResources) == Resource.NoResource) || nativeArray[j].m_HelicopterType != helicopterType)
					{
						continue;
					}
					Entity value = nativeArray2[j];
					bool flag = ValidatePrimaryModel(vehicleModels, value);
					if (!m_RequirementData.CheckRequirements(ref chunk2, j, ignoreTheme || flag))
					{
						continue;
					}
					int priority = math.select(0, 2, flag);
					if (primaryPrefabs.IsCreated)
					{
						primaryPrefabs.Add(in value);
					}
					if (PickVehicle(ref random, 100, priority, ref @int.x, ref int2.x))
					{
						entity = value;
						if (publicTransportPurpose != 0)
						{
							passengerCapacity = nativeArray4[j].m_PassengerCapacity;
						}
						if (cargoResources != Resource.NoResource)
						{
							cargoCapacity = nativeArray5[j].m_CargoCapacity;
						}
					}
				}
				break;
			}
			}
		}
		if (isMultipleUnitTrain)
		{
			secondaryResult = Entity.Null;
		}
		else
		{
			unitCount = num;
		}
		bool flag11 = false;
		if (transportType == TransportType.Train || transportType == TransportType.Tram || transportType == TransportType.Subway)
		{
			flag11 = true;
		}
		if (flag11)
		{
			passengerCapacity.y = 0;
			cargoCapacity.y = 0;
			int num9 = 0;
			if (isMultipleUnitTrain)
			{
				passengerCapacity.y += passengerCapacity.x;
				cargoCapacity.y += cargoCapacity.x;
			}
			Entity entity2 = (isMultipleUnitTrain ? entity : secondaryResult);
			if (entity2 != Entity.Null)
			{
				for (int num10 = 0; num10 < unitCount; num10++)
				{
					if (isMultipleUnitTrain && num10 != 0)
					{
						passengerCapacity.y += passengerCapacity.x;
						cargoCapacity.y += cargoCapacity.x;
					}
					if (!m_VehicleCarriages.HasBuffer(entity2))
					{
						continue;
					}
					DynamicBuffer<VehicleCarriageElement> dynamicBuffer = m_VehicleCarriages[entity2];
					for (int num11 = 0; num11 < dynamicBuffer.Length; num11++)
					{
						VehicleCarriageElement vehicleCarriageElement = dynamicBuffer[num11];
						if (vehicleCarriageElement.m_Prefab == Entity.Null)
						{
							num9 += vehicleCarriageElement.m_Count.x;
							continue;
						}
						if (publicTransportPurpose != 0 && m_PublicTransportVehicleData.TryGetComponent(vehicleCarriageElement.m_Prefab, out var componentData))
						{
							passengerCapacity.y += componentData.m_PassengerCapacity * vehicleCarriageElement.m_Count.x;
						}
						if (cargoResources != Resource.NoResource && m_CargoTransportVehicleData.TryGetComponent(vehicleCarriageElement.m_Prefab, out var componentData2))
						{
							cargoCapacity.y += componentData2.m_CargoCapacity * vehicleCarriageElement.m_Count.x;
						}
					}
				}
			}
			if (!isMultipleUnitTrain)
			{
				passengerCapacity.y += passengerCapacity.x;
				cargoCapacity.y += cargoCapacity.x;
				num9--;
			}
			if (num9 > 0)
			{
				passengerCapacity.y += passengerCapacity.x * num9;
				cargoCapacity.y += cargoCapacity.x * num9;
			}
			passengerCapacity.x = passengerCapacity.y;
			cargoCapacity.x = cargoCapacity.y;
		}
		return entity;
	}

	private bool ValidatePrimaryModel<TVehicleModelList>(TVehicleModelList vehicleModels, Entity primaryPrefab) where TVehicleModelList : unmanaged, INativeList<VehicleModel>
	{
		if (!vehicleModels.IsEmpty)
		{
			for (int i = 0; i < vehicleModels.Length; i++)
			{
				if (primaryPrefab == vehicleModels[i].m_PrimaryPrefab)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool ValidateSecondaryModel<TVehicleModelList>(TVehicleModelList vehicleModels, Entity secondaryPrefab) where TVehicleModelList : unmanaged, INativeList<VehicleModel>
	{
		if (!vehicleModels.IsEmpty)
		{
			for (int i = 0; i < vehicleModels.Length; i++)
			{
				if (secondaryPrefab == vehicleModels[i].m_SecondaryPrefab)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PickVehicle(ref Random random, int probability, int priority, ref int totalProbability, ref int selectedPriority)
	{
		if (priority < selectedPriority)
		{
			return false;
		}
		if (priority > selectedPriority)
		{
			totalProbability = 0;
			selectedPriority = priority;
		}
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
	}
}
