using Game.City;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct MaintenanceVehicleSelectData
{
	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<MaintenanceVehicleData> m_MaintenanceVehicleType;

	private ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryDataType;

	private ComponentLookup<ObjectData> m_ObjectData;

	private ComponentLookup<MovingObjectData> m_MovingObjectData;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[4]
		{
			ComponentType.ReadOnly<MaintenanceVehicleData>(),
			ComponentType.ReadOnly<CarData>(),
			ComponentType.ReadOnly<ObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public MaintenanceVehicleSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_MaintenanceVehicleType = system.GetComponentTypeHandle<MaintenanceVehicleData>(isReadOnly: true);
		m_ObjectGeometryDataType = system.GetComponentTypeHandle<ObjectGeometryData>(isReadOnly: true);
		m_ObjectData = system.GetComponentLookup<ObjectData>(isReadOnly: true);
		m_MovingObjectData = system.GetComponentLookup<MovingObjectData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_MaintenanceVehicleType.Update(system);
		m_ObjectGeometryDataType.Update(system);
		m_ObjectData.Update(system);
		m_MovingObjectData.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public Entity SelectVehicle(ref Random random, MaintenanceType allMaintenanceTypes, MaintenanceType anyMaintenanceTypes, float4 maxParkingSizes)
	{
		return GetRandomVehicle(ref random, allMaintenanceTypes, anyMaintenanceTypes, maxParkingSizes);
	}

	public Entity CreateVehicle(EntityCommandBuffer commandBuffer, ref Random random, Transform transform, Entity source, Entity prefab, MaintenanceType allMaintenanceTypes, MaintenanceType anyMaintenanceTypes, float4 maxParkingSizes, bool parked)
	{
		if (prefab == Entity.Null)
		{
			prefab = GetRandomVehicle(ref random, allMaintenanceTypes, anyMaintenanceTypes, maxParkingSizes);
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

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, Transform transform, Entity source, Entity prefab, MaintenanceType allMaintenanceTypes, MaintenanceType anyMaintenanceTypes, float4 maxParkingSizes, bool parked)
	{
		if (prefab == Entity.Null)
		{
			prefab = GetRandomVehicle(ref random, allMaintenanceTypes, anyMaintenanceTypes, maxParkingSizes);
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

	private Entity GetRandomVehicle(ref Random random, MaintenanceType allMaintenanceTypes, MaintenanceType anyMaintenanceTypes, float4 maxParkingSizes)
	{
		Entity result = Entity.Null;
		int totalProbability = 0;
		int num = 100;
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<MaintenanceVehicleData> nativeArray2 = chunk.GetNativeArray(ref m_MaintenanceVehicleType);
			NativeArray<ObjectGeometryData> nativeArray3 = chunk.GetNativeArray(ref m_ObjectGeometryDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				MaintenanceVehicleData maintenanceVehicleData = nativeArray2[j];
				if ((maintenanceVehicleData.m_MaintenanceType & allMaintenanceTypes) != allMaintenanceTypes || ((maintenanceVehicleData.m_MaintenanceType & anyMaintenanceTypes) == 0 && anyMaintenanceTypes != MaintenanceType.None) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				float offset;
				bool4 @bool = VehicleUtils.GetParkingSize(nativeArray3[j], out offset).xyxy > maxParkingSizes;
				if (math.all(@bool | @bool.yxwz))
				{
					continue;
				}
				int num2 = math.select(math.countbits((int)(maintenanceVehicleData.m_MaintenanceType ^ allMaintenanceTypes)), 0, allMaintenanceTypes == MaintenanceType.None);
				if (num2 <= num)
				{
					if (num2 < num)
					{
						totalProbability = 0;
						num = num2;
					}
					if (PickVehicle(ref random, 100, ref totalProbability))
					{
						result = nativeArray[j];
					}
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
