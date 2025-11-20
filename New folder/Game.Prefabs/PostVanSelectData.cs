using Game.City;
using Game.Common;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct PostVanSelectData
{
	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PostVanData> m_PostVanType;

	private ComponentLookup<ObjectData> m_ObjectData;

	private ComponentLookup<MovingObjectData> m_MovingObjectData;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[4]
		{
			ComponentType.ReadOnly<PostVanData>(),
			ComponentType.ReadOnly<CarData>(),
			ComponentType.ReadOnly<ObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public PostVanSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_PostVanType = system.GetComponentTypeHandle<PostVanData>(isReadOnly: true);
		m_ObjectData = system.GetComponentLookup<ObjectData>(isReadOnly: true);
		m_MovingObjectData = system.GetComponentLookup<MovingObjectData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_PostVanType.Update(system);
		m_ObjectData.Update(system);
		m_MovingObjectData.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public Entity CreateVehicle(EntityCommandBuffer commandBuffer, ref Random random, Transform transform, Entity source, Entity vehiclePrefab, bool parked)
	{
		if (vehiclePrefab == Entity.Null)
		{
			return Entity.Null;
		}
		Entity entity = commandBuffer.CreateEntity(GetArchetype(vehiclePrefab, parked));
		commandBuffer.SetComponent(entity, transform);
		commandBuffer.SetComponent(entity, new PrefabRef(vehiclePrefab));
		commandBuffer.SetComponent(entity, new PseudoRandomSeed(ref random));
		if (!parked)
		{
			commandBuffer.AddComponent(entity, new TripSource(source));
			commandBuffer.AddComponent(entity, default(Unspawned));
		}
		return entity;
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, Transform transform, Entity source, Entity vehiclePrefab, bool parked)
	{
		if (vehiclePrefab == Entity.Null)
		{
			return Entity.Null;
		}
		Entity entity = commandBuffer.CreateEntity(jobIndex, GetArchetype(vehiclePrefab, parked));
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(vehiclePrefab));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		if (!parked)
		{
			commandBuffer.AddComponent(jobIndex, entity, new TripSource(source));
			commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		}
		return entity;
	}

	public Entity SelectVehicle(ref Random random, ref int2 mailCapacity)
	{
		Entity result = Entity.Null;
		int num = 0;
		int num2 = -mailCapacity.x;
		int totalProbability = 0;
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PostVanData> nativeArray2 = chunk.GetNativeArray(ref m_PostVanType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				if (!m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				PostVanData postVanData = nativeArray2[j];
				int2 @int = postVanData.m_MailCapacity - mailCapacity;
				int num3 = math.max(math.min(0, @int.x), @int.y);
				if (num3 != num2)
				{
					if ((num3 < 0 && num2 > num3) || (num2 >= 0 && num2 < num3))
					{
						continue;
					}
					num2 = num3;
					totalProbability = 0;
				}
				if (PickVehicle(ref random, 100, ref totalProbability))
				{
					result = nativeArray[j];
					num = postVanData.m_MailCapacity;
				}
			}
		}
		mailCapacity = num;
		return result;
	}

	private EntityArchetype GetArchetype(Entity prefab, bool parked)
	{
		if (parked)
		{
			return m_MovingObjectData[prefab].m_StoppedArchetype;
		}
		return m_ObjectData[prefab].m_Archetype;
	}

	private bool PickVehicle(ref Random random, int probability, ref int totalProbability)
	{
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
	}
}
