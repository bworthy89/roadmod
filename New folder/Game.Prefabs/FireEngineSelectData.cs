using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct FireEngineSelectData
{
	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<FireEngineData> m_FireEngineType;

	private ComponentTypeHandle<CarData> m_CarType;

	private ComponentTypeHandle<HelicopterData> m_HelicopterType;

	private ComponentLookup<ObjectData> m_ObjectData;

	private ComponentLookup<MovingObjectData> m_MovingObjectData;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<FireEngineData>(),
			ComponentType.ReadOnly<ObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.Any = new ComponentType[2]
		{
			ComponentType.ReadOnly<CarData>(),
			ComponentType.ReadOnly<HelicopterData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public FireEngineSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_FireEngineType = system.GetComponentTypeHandle<FireEngineData>(isReadOnly: true);
		m_CarType = system.GetComponentTypeHandle<CarData>(isReadOnly: true);
		m_HelicopterType = system.GetComponentTypeHandle<HelicopterData>(isReadOnly: true);
		m_ObjectData = system.GetComponentLookup<ObjectData>(isReadOnly: true);
		m_MovingObjectData = system.GetComponentLookup<MovingObjectData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_FireEngineType.Update(system);
		m_CarType.Update(system);
		m_HelicopterType.Update(system);
		m_ObjectData.Update(system);
		m_MovingObjectData.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public Entity SelectVehicle(ref Random random, ref float2 extinguishingCapacity, RoadTypes roadType)
	{
		return GetRandomVehicle(ref random, ref extinguishingCapacity, roadType);
	}

	public Entity CreateVehicle(EntityCommandBuffer commandBuffer, ref Random random, Transform transform, Entity source, Entity prefab, ref float2 extinguishingCapacity, RoadTypes roadType, bool parked)
	{
		if (prefab == Entity.Null)
		{
			prefab = GetRandomVehicle(ref random, ref extinguishingCapacity, roadType);
			if (prefab == Entity.Null)
			{
				return Entity.Null;
			}
		}
		Entity entity = commandBuffer.CreateEntity(GetArchetype(prefab, parked));
		commandBuffer.SetComponent(entity, transform);
		commandBuffer.SetComponent(entity, new PrefabRef(prefab));
		commandBuffer.SetComponent(entity, new PseudoRandomSeed(ref random));
		if (!parked)
		{
			commandBuffer.AddComponent(entity, new TripSource(source));
			commandBuffer.AddComponent(entity, default(Unspawned));
		}
		return entity;
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, Transform transform, Entity source, Entity prefab, ref float2 extinguishingCapacity, RoadTypes roadType, bool parked)
	{
		if (prefab == Entity.Null)
		{
			prefab = GetRandomVehicle(ref random, ref extinguishingCapacity, roadType);
			if (prefab == Entity.Null)
			{
				return Entity.Null;
			}
		}
		Entity entity = commandBuffer.CreateEntity(jobIndex, GetArchetype(prefab, parked));
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(prefab));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		if (!parked)
		{
			commandBuffer.AddComponent(jobIndex, entity, new TripSource(source));
			commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		}
		return entity;
	}

	private EntityArchetype GetArchetype(Entity prefab, bool parked)
	{
		if (parked)
		{
			return m_MovingObjectData[prefab].m_StoppedArchetype;
		}
		return m_ObjectData[prefab].m_Archetype;
	}

	private Entity GetRandomVehicle(ref Random random, ref float2 extinguishingCapacity, RoadTypes roadType)
	{
		Entity result = Entity.Null;
		float num = 0f;
		float num2 = 0f - extinguishingCapacity.x;
		int totalProbability = 0;
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			if (roadType != RoadTypes.Car)
			{
				if (roadType != RoadTypes.Helicopter || !chunk.Has(ref m_HelicopterType))
				{
					continue;
				}
			}
			else if (!chunk.Has(ref m_CarType))
			{
				continue;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<FireEngineData> nativeArray2 = chunk.GetNativeArray(ref m_FireEngineType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				if (!m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				FireEngineData fireEngineData = nativeArray2[j];
				float2 @float = fireEngineData.m_ExtinguishingCapacity - extinguishingCapacity;
				float num3 = math.max(math.min(0f, @float.x), @float.y);
				if (num3 != num2)
				{
					if ((num3 < 0f && num2 > num3) || (num2 >= 0f && num2 < num3))
					{
						continue;
					}
					num2 = num3;
					totalProbability = 0;
				}
				if (PickVehicle(ref random, 100, ref totalProbability))
				{
					result = nativeArray[j];
					num = fireEngineData.m_ExtinguishingCapacity;
				}
			}
		}
		extinguishingCapacity = num;
		return result;
	}

	private bool PickVehicle(ref Random random, int probability, ref int totalProbability)
	{
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
	}
}
