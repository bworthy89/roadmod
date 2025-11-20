using Game.City;
using Game.Economy;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct TransportTrainCarriageSelectData
{
	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<CargoTransportVehicleData> m_CargoTransportVehicleType;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<CargoTransportVehicleData>(),
			ComponentType.ReadOnly<TrainCarriageData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public TransportTrainCarriageSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_CargoTransportVehicleType = system.GetComponentTypeHandle<CargoTransportVehicleData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_CargoTransportVehicleType.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public Entity SelectCarriagePrefab(ref Random random, Resource resource, int amount)
	{
		Entity result = Entity.Null;
		int num = -amount;
		int totalProbability = 0;
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CargoTransportVehicleData> nativeArray2 = chunk.GetNativeArray(ref m_CargoTransportVehicleType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				CargoTransportVehicleData cargoTransportVehicleData = nativeArray2[j];
				if ((cargoTransportVehicleData.m_Resources & resource) == Resource.NoResource || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				int num2 = cargoTransportVehicleData.m_CargoCapacity - amount;
				if (num2 != num)
				{
					if ((num2 < 0 && num > num2) || (num >= 0 && num < num2))
					{
						continue;
					}
					num = num2;
					totalProbability = 0;
				}
				if (PickVehicle(ref random, 100, ref totalProbability))
				{
					result = nativeArray[j];
				}
			}
		}
		return result;
	}

	private bool PickVehicle(ref Random random, int probability, ref int totalProbability)
	{
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
	}
}
