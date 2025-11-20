using Game.City;
using Game.Common;
using Game.Objects;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct PersonalCarSelectData
{
	private struct CarData
	{
		public Entity m_Entity;

		public PersonalCarData m_PersonalCarData;

		public CarTrailerData m_TrailerData;

		public CarTractorData m_TractorData;

		public ObjectData m_ObjectData;

		public MovingObjectData m_MovingObjectData;
	}

	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<Game.Prefabs.CarData> m_CarDataType;

	private ComponentTypeHandle<PersonalCarData> m_PersonalCarDataType;

	private ComponentTypeHandle<CarTrailerData> m_CarTrailerDataType;

	private ComponentTypeHandle<CarTractorData> m_CarTractorDataType;

	private ComponentTypeHandle<ObjectData> m_ObjectDataType;

	private ComponentTypeHandle<MovingObjectData> m_MovingObjectDataType;

	private ComponentTypeHandle<BicycleData> m_BicycleDataType;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[4]
		{
			ComponentType.ReadOnly<PersonalCarData>(),
			ComponentType.ReadOnly<Game.Prefabs.CarData>(),
			ComponentType.ReadOnly<MovingObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public PersonalCarSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_CarDataType = system.GetComponentTypeHandle<Game.Prefabs.CarData>(isReadOnly: true);
		m_PersonalCarDataType = system.GetComponentTypeHandle<PersonalCarData>(isReadOnly: true);
		m_CarTrailerDataType = system.GetComponentTypeHandle<CarTrailerData>(isReadOnly: true);
		m_CarTractorDataType = system.GetComponentTypeHandle<CarTractorData>(isReadOnly: true);
		m_ObjectDataType = system.GetComponentTypeHandle<ObjectData>(isReadOnly: true);
		m_MovingObjectDataType = system.GetComponentTypeHandle<MovingObjectData>(isReadOnly: true);
		m_BicycleDataType = system.GetComponentTypeHandle<BicycleData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_CarDataType.Update(system);
		m_PersonalCarDataType.Update(system);
		m_CarTrailerDataType.Update(system);
		m_CarTractorDataType.Update(system);
		m_ObjectDataType.Update(system);
		m_MovingObjectDataType.Update(system);
		m_BicycleDataType.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public Entity SelectVehiclePrefab(ref Random random, int passengerAmount, int baggageAmount, bool avoidTrailers, bool noSlowVehicles, bool bicycle, out Entity trailerPrefab)
	{
		if (GetVehicleData(ref random, passengerAmount, baggageAmount, avoidTrailers, noSlowVehicles, bicycle, out var bestFirst, out var bestSecond))
		{
			trailerPrefab = bestSecond.m_Entity;
			return bestFirst.m_Entity;
		}
		trailerPrefab = Entity.Null;
		return Entity.Null;
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, int passengerAmount, int baggageAmount, bool avoidTrailers, bool noSlowVehicles, bool bicycle, Transform transform, Entity source, Entity keeper, PersonalCarFlags state, bool stopped, uint delay = 0u)
	{
		Entity trailer;
		Entity vehiclePrefab;
		Entity trailerPrefab;
		return CreateVehicle(commandBuffer, jobIndex, ref random, passengerAmount, baggageAmount, avoidTrailers, noSlowVehicles, bicycle, transform, source, keeper, state, stopped, delay, out trailer, out vehiclePrefab, out trailerPrefab);
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, int passengerAmount, int baggageAmount, bool avoidTrailers, bool noSlowVehicles, bool bicycle, Transform transform, Entity source, Entity keeper, PersonalCarFlags state, bool stopped, uint delay, out Entity trailer, out Entity vehiclePrefab, out Entity trailerPrefab)
	{
		trailer = Entity.Null;
		vehiclePrefab = Entity.Null;
		trailerPrefab = Entity.Null;
		if (GetVehicleData(ref random, passengerAmount, baggageAmount, avoidTrailers, noSlowVehicles, bicycle, out var bestFirst, out var bestSecond))
		{
			Entity entity = CreateVehicle(commandBuffer, jobIndex, ref random, bestFirst, transform, source, keeper, state, stopped, delay);
			vehiclePrefab = bestFirst.m_Entity;
			if (bestSecond.m_Entity != Entity.Null)
			{
				DynamicBuffer<LayoutElement> dynamicBuffer = commandBuffer.AddBuffer<LayoutElement>(jobIndex, entity);
				dynamicBuffer.Add(new LayoutElement(entity));
				trailer = CreateVehicle(commandBuffer, jobIndex, ref random, bestSecond, transform, source, Entity.Null, (PersonalCarFlags)0u, stopped, delay);
				trailerPrefab = bestSecond.m_Entity;
				commandBuffer.SetComponent(jobIndex, trailer, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(trailer));
			}
			return entity;
		}
		return Entity.Null;
	}

	public Entity CreateVehicle(EntityCommandBuffer commandBuffer, ref Random random, int passengerAmount, int baggageAmount, bool avoidTrailers, bool noSlowVehicles, bool bicycle, Transform transform, Entity source, Entity keeper, PersonalCarFlags state, bool stopped, uint delay = 0u)
	{
		if (GetVehicleData(ref random, passengerAmount, baggageAmount, avoidTrailers, noSlowVehicles, bicycle, out var bestFirst, out var bestSecond))
		{
			Entity entity = CreateVehicle(commandBuffer, ref random, bestFirst, transform, source, keeper, state, stopped, delay);
			if (bestSecond.m_Entity != Entity.Null)
			{
				DynamicBuffer<LayoutElement> dynamicBuffer = commandBuffer.AddBuffer<LayoutElement>(entity);
				dynamicBuffer.Add(new LayoutElement(entity));
				Entity entity2 = CreateVehicle(commandBuffer, ref random, bestSecond, transform, source, Entity.Null, (PersonalCarFlags)0u, stopped, delay);
				commandBuffer.SetComponent(entity2, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(entity2));
			}
			return entity;
		}
		return Entity.Null;
	}

	public Entity CreateTrailer(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, int passengerAmount, int baggageAmount, bool noSlowVehicles, Entity tractorPrefab, Transform tractorTransform, PersonalCarFlags state, bool stopped, uint delay = 0u)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PersonalCarData> nativeArray2 = chunk.GetNativeArray(ref m_PersonalCarDataType);
			NativeArray<CarTractorData> nativeArray3 = chunk.GetNativeArray(ref m_CarTractorDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				if (!(nativeArray[j] != tractorPrefab) && m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					CarData firstData = new CarData
					{
						m_Entity = tractorPrefab,
						m_PersonalCarData = nativeArray2[j],
						m_TractorData = nativeArray3[j]
					};
					CarData bestFirst = default(CarData);
					CarData bestSecond = default(CarData);
					CalculateProbability(passengerAmount, baggageAmount, firstData, default(CarData), out var probability, out var offset);
					CheckTrailers(passengerAmount, baggageAmount, 0, firstData, emptyOnly: false, noSlowVehicles, ref random, ref bestFirst, ref bestSecond, ref probability, ref offset);
					if (bestSecond.m_Entity == Entity.Null)
					{
						return Entity.Null;
					}
					Transform transform = tractorTransform;
					transform.m_Position += math.rotate(tractorTransform.m_Rotation, firstData.m_TractorData.m_AttachPosition);
					transform.m_Position -= math.rotate(transform.m_Rotation, bestSecond.m_TrailerData.m_AttachPosition);
					return CreateVehicle(commandBuffer, jobIndex, ref random, bestSecond, transform, Entity.Null, Entity.Null, (PersonalCarFlags)0u, stopped, delay);
				}
			}
		}
		return Entity.Null;
	}

	private bool GetVehicleData(ref Random random, int passengerAmount, int baggageAmount, bool avoidTrailers, bool noSlowVehicles, bool bicycle, out CarData bestFirst, out CarData bestSecond)
	{
		bestFirst = default(CarData);
		bestSecond = default(CarData);
		int totalProbability = 0;
		int bestOffset = -11 - (passengerAmount + baggageAmount);
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			if (chunk.Has(ref m_BicycleDataType) != bicycle)
			{
				continue;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Prefabs.CarData> nativeArray2 = chunk.GetNativeArray(ref m_CarDataType);
			NativeArray<PersonalCarData> nativeArray3 = chunk.GetNativeArray(ref m_PersonalCarDataType);
			NativeArray<CarTrailerData> nativeArray4 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			NativeArray<CarTractorData> nativeArray5 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<ObjectData> nativeArray6 = chunk.GetNativeArray(ref m_ObjectDataType);
			NativeArray<MovingObjectData> nativeArray7 = chunk.GetNativeArray(ref m_MovingObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Game.Prefabs.CarData carData = nativeArray2[j];
				if ((noSlowVehicles && carData.m_MaxSpeed < 22.222223f) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				CarData carData2 = new CarData
				{
					m_PersonalCarData = nativeArray3[j]
				};
				if (carData2.m_PersonalCarData.m_PassengerCapacity == 0 && carData2.m_PersonalCarData.m_BaggageCapacity == 0)
				{
					continue;
				}
				carData2.m_Entity = nativeArray[j];
				carData2.m_ObjectData = nativeArray6[j];
				carData2.m_MovingObjectData = nativeArray7[j];
				bool flag = false;
				if (nativeArray4.Length != 0)
				{
					carData2.m_TrailerData = nativeArray4[j];
					flag = true;
				}
				if (nativeArray5.Length != 0)
				{
					carData2.m_TractorData = nativeArray5[j];
					if (carData2.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						if (!flag)
						{
							int extraOffset = math.select(0, -1, avoidTrailers);
							CheckTrailers(passengerAmount, baggageAmount, extraOffset, carData2, emptyOnly: true, noSlowVehicles, ref random, ref bestFirst, ref bestSecond, ref totalProbability, ref bestOffset);
						}
						continue;
					}
				}
				if (flag)
				{
					int extraOffset2 = math.select(0, -1, avoidTrailers);
					CheckTractors(passengerAmount, baggageAmount, extraOffset2, carData2, noSlowVehicles, ref random, ref bestFirst, ref bestSecond, ref totalProbability, ref bestOffset);
					continue;
				}
				CalculateProbability(passengerAmount, baggageAmount, carData2, default(CarData), out var probability, out var offset);
				if (PickVehicle(ref random, probability, offset, ref totalProbability, ref bestOffset))
				{
					bestFirst = carData2;
					bestSecond = default(CarData);
				}
			}
		}
		return bestFirst.m_Entity != Entity.Null;
	}

	private Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, CarData data, Transform transform, Entity source, Entity keeper, PersonalCarFlags state, bool stopped, uint delay)
	{
		Entity entity = ((!stopped) ? commandBuffer.CreateEntity(jobIndex, data.m_ObjectData.m_Archetype) : commandBuffer.CreateEntity(jobIndex, data.m_MovingObjectData.m_StoppedArchetype));
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, new Game.Vehicles.PersonalCar(keeper, state));
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(data.m_Entity));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		if (source != Entity.Null)
		{
			commandBuffer.AddComponent(jobIndex, entity, new TripSource(source, delay));
			commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		}
		return entity;
	}

	private Entity CreateVehicle(EntityCommandBuffer commandBuffer, ref Random random, CarData data, Transform transform, Entity source, Entity keeper, PersonalCarFlags state, bool stopped, uint delay)
	{
		Entity entity = ((!stopped) ? commandBuffer.CreateEntity(data.m_ObjectData.m_Archetype) : commandBuffer.CreateEntity(data.m_MovingObjectData.m_StoppedArchetype));
		commandBuffer.SetComponent(entity, transform);
		commandBuffer.SetComponent(entity, new Game.Vehicles.PersonalCar(keeper, state));
		commandBuffer.SetComponent(entity, new PrefabRef(data.m_Entity));
		commandBuffer.SetComponent(entity, new PseudoRandomSeed(ref random));
		if (source != Entity.Null)
		{
			commandBuffer.AddComponent(entity, new TripSource(source, delay));
			commandBuffer.AddComponent(entity, default(Unspawned));
		}
		return entity;
	}

	private void CheckTrailers(int passengerAmount, int baggageAmount, int extraOffset, CarData firstData, bool emptyOnly, bool noSlowVehicles, ref Random random, ref CarData bestFirst, ref CarData bestSecond, ref int totalProbability, ref int bestOffset)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTrailerData> nativeArray = chunk.GetNativeArray(ref m_CarTrailerDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Prefabs.CarData> nativeArray3 = chunk.GetNativeArray(ref m_CarDataType);
			NativeArray<PersonalCarData> nativeArray4 = chunk.GetNativeArray(ref m_PersonalCarDataType);
			NativeArray<CarTractorData> nativeArray5 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<ObjectData> nativeArray6 = chunk.GetNativeArray(ref m_ObjectDataType);
			NativeArray<MovingObjectData> nativeArray7 = chunk.GetNativeArray(ref m_MovingObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Game.Prefabs.CarData carData = nativeArray3[j];
				if ((noSlowVehicles && carData.m_MaxSpeed < 22.222223f) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				CarData carData2 = new CarData
				{
					m_PersonalCarData = nativeArray4[j]
				};
				if (emptyOnly && (carData2.m_PersonalCarData.m_PassengerCapacity != 0 || carData2.m_PersonalCarData.m_BaggageCapacity != 0))
				{
					continue;
				}
				carData2.m_Entity = nativeArray2[j];
				carData2.m_TrailerData = nativeArray[j];
				if (firstData.m_TractorData.m_TrailerType != carData2.m_TrailerData.m_TrailerType || (firstData.m_TractorData.m_FixedTrailer != Entity.Null && firstData.m_TractorData.m_FixedTrailer != carData2.m_Entity) || (carData2.m_TrailerData.m_FixedTractor != Entity.Null && carData2.m_TrailerData.m_FixedTractor != firstData.m_Entity))
				{
					continue;
				}
				carData2.m_ObjectData = nativeArray6[j];
				carData2.m_MovingObjectData = nativeArray7[j];
				if (nativeArray5.Length != 0)
				{
					carData2.m_TractorData = nativeArray5[j];
					if (carData2.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						continue;
					}
				}
				CalculateProbability(passengerAmount, baggageAmount, firstData, carData2, out var probability, out var offset);
				if (PickVehicle(ref random, probability, offset + extraOffset, ref totalProbability, ref bestOffset))
				{
					bestFirst = firstData;
					bestSecond = carData2;
				}
			}
		}
	}

	private void CheckTractors(int passengerAmount, int baggageAmount, int extraOffset, CarData secondData, bool noSlowVehicles, ref Random random, ref CarData bestFirst, ref CarData bestSecond, ref int totalProbability, ref int bestOffset)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0 || chunk.Has(ref m_CarTrailerDataType))
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Prefabs.CarData> nativeArray3 = chunk.GetNativeArray(ref m_CarDataType);
			NativeArray<PersonalCarData> nativeArray4 = chunk.GetNativeArray(ref m_PersonalCarDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			NativeArray<MovingObjectData> nativeArray6 = chunk.GetNativeArray(ref m_MovingObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Game.Prefabs.CarData carData = nativeArray3[j];
				if ((noSlowVehicles && carData.m_MaxSpeed < 22.222223f) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				CarData carData2 = new CarData
				{
					m_PersonalCarData = nativeArray4[j],
					m_Entity = nativeArray2[j],
					m_TractorData = nativeArray[j]
				};
				if (carData2.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(carData2.m_TractorData.m_FixedTrailer != Entity.Null) || !(carData2.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != carData2.m_Entity)))
				{
					carData2.m_ObjectData = nativeArray5[j];
					carData2.m_MovingObjectData = nativeArray6[j];
					CalculateProbability(passengerAmount, baggageAmount, carData2, secondData, out var probability, out var offset);
					if (PickVehicle(ref random, probability, offset + extraOffset, ref totalProbability, ref bestOffset))
					{
						bestFirst = carData2;
						bestSecond = secondData;
					}
				}
			}
		}
	}

	private void CalculateProbability(int passengerAmount, int baggageAmount, CarData firstData, CarData secondData, out int probability, out int offset)
	{
		int num = firstData.m_PersonalCarData.m_PassengerCapacity + secondData.m_PersonalCarData.m_PassengerCapacity;
		int num2 = firstData.m_PersonalCarData.m_BaggageCapacity + secondData.m_PersonalCarData.m_BaggageCapacity;
		int num3 = num - passengerAmount;
		int num4 = num2 - baggageAmount;
		offset = math.min(0, num3) + math.min(0, num4);
		offset = math.select(0, offset - 10, offset != 0) + math.min(0, 4 - num3) + math.min(0, 4 - num4);
		probability = firstData.m_PersonalCarData.m_Probability;
		probability = math.select(probability, probability * secondData.m_PersonalCarData.m_Probability / 50, secondData.m_Entity != Entity.Null);
		probability = math.max(1, probability / ((1 << math.max(0, num3)) + (1 << math.max(0, num4))));
	}

	private bool PickVehicle(ref Random random, int probability, int offset, ref int totalProbability, ref int bestOffset)
	{
		if (offset == bestOffset)
		{
			totalProbability += probability;
			return random.NextInt(totalProbability) < probability;
		}
		if (offset > bestOffset)
		{
			totalProbability = probability;
			bestOffset = offset;
			return true;
		}
		return false;
	}
}
