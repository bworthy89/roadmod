using Colossal.Collections;
using Game.Areas;
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

public struct WorkVehicleSelectData
{
	private struct VehicleData
	{
		public Entity m_Entity;

		public WorkVehicleData m_WorkVehicleData;

		public CarTrailerData m_TrailerData;

		public CarTractorData m_TractorData;

		public ObjectData m_ObjectData;
	}

	private NativeList<ArchetypeChunk> m_PrefabChunks;

	private VehicleSelectRequirementData m_RequirementData;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<WorkVehicleData> m_WorkVehicleDataType;

	private ComponentTypeHandle<CarTrailerData> m_CarTrailerDataType;

	private ComponentTypeHandle<CarTractorData> m_CarTractorDataType;

	private ComponentTypeHandle<CarData> m_CarDataType;

	private ComponentTypeHandle<WatercraftData> m_WatercraftDataType;

	private ComponentTypeHandle<ObjectData> m_ObjectDataType;

	public static EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<WorkVehicleData>(),
			ComponentType.ReadOnly<ObjectData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		entityQueryDesc.Any = new ComponentType[2]
		{
			ComponentType.ReadOnly<CarData>(),
			ComponentType.ReadOnly<WatercraftData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<Locked>() };
		return entityQueryDesc;
	}

	public WorkVehicleSelectData(SystemBase system)
	{
		m_PrefabChunks = default(NativeList<ArchetypeChunk>);
		m_RequirementData = new VehicleSelectRequirementData(system);
		m_EntityType = system.GetEntityTypeHandle();
		m_WorkVehicleDataType = system.GetComponentTypeHandle<WorkVehicleData>(isReadOnly: true);
		m_CarTrailerDataType = system.GetComponentTypeHandle<CarTrailerData>(isReadOnly: true);
		m_CarTractorDataType = system.GetComponentTypeHandle<CarTractorData>(isReadOnly: true);
		m_CarDataType = system.GetComponentTypeHandle<CarData>(isReadOnly: true);
		m_WatercraftDataType = system.GetComponentTypeHandle<WatercraftData>(isReadOnly: true);
		m_ObjectDataType = system.GetComponentTypeHandle<ObjectData>(isReadOnly: true);
	}

	public void PreUpdate(SystemBase system, CityConfigurationSystem cityConfigurationSystem, EntityQuery query, Allocator allocator, out JobHandle jobHandle)
	{
		m_PrefabChunks = query.ToArchetypeChunkListAsync(allocator, out jobHandle);
		m_RequirementData.Update(system, cityConfigurationSystem);
		m_EntityType.Update(system);
		m_WorkVehicleDataType.Update(system);
		m_CarTrailerDataType.Update(system);
		m_CarTractorDataType.Update(system);
		m_CarDataType.Update(system);
		m_WatercraftDataType.Update(system);
		m_ObjectDataType.Update(system);
	}

	public void PostUpdate(JobHandle jobHandle)
	{
		m_PrefabChunks.Dispose(jobHandle);
	}

	public void ListVehicles(RoadTypes roadTypes, SizeClass sizeClass, VehicleWorkType workType, MapFeature mapFeature, Resource resource, NativeList<Entity> prefabs)
	{
		Random random = Random.CreateFromIndex(0u);
		PickVehicleData(ref random, default(DynamicBuffer<VehicleModel>), prefabs, roadTypes, sizeClass, workType, mapFeature, resource, out var _, out var _, out var _, out var _);
	}

	public void SelectVehicle(ref Random random, RoadTypes roadTypes, SizeClass sizeClass, VehicleWorkType workType, MapFeature mapFeature, Resource resource, out Entity prefab)
	{
		prefab = Entity.Null;
		if (PickVehicleData(ref random, default(DynamicBuffer<VehicleModel>), default(NativeList<Entity>), roadTypes, sizeClass, workType, mapFeature, resource, out var bestFirst, out var bestSecond, out var bestThird, out var bestForth))
		{
			if (bestFirst.m_WorkVehicleData.m_MaxWorkAmount != 0f)
			{
				prefab = bestFirst.m_Entity;
			}
			else if (bestSecond.m_WorkVehicleData.m_MaxWorkAmount != 0f)
			{
				prefab = bestSecond.m_Entity;
			}
			else if (bestThird.m_WorkVehicleData.m_MaxWorkAmount != 0f)
			{
				prefab = bestThird.m_Entity;
			}
			else if (bestForth.m_WorkVehicleData.m_MaxWorkAmount != 0f)
			{
				prefab = bestForth.m_Entity;
			}
		}
	}

	public Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, DynamicBuffer<VehicleModel> vehicleModels, RoadTypes roadTypes, SizeClass sizeClass, VehicleWorkType workType, MapFeature mapFeature, Resource resource, ref float workAmount, Transform transform, Entity source, WorkVehicleFlags state)
	{
		if (!PickVehicleData(ref random, vehicleModels, default(NativeList<Entity>), roadTypes, sizeClass, workType, mapFeature, resource, out var bestFirst, out var bestSecond, out var bestThird, out var bestForth))
		{
			workAmount = 0f;
			return Entity.Null;
		}
		float workAmount2 = workAmount;
		Entity entity = CreateVehicle(commandBuffer, jobIndex, ref random, bestFirst, workType, ref workAmount2, transform, source, state);
		if (bestSecond.m_Entity != Entity.Null)
		{
			DynamicBuffer<LayoutElement> dynamicBuffer = commandBuffer.AddBuffer<LayoutElement>(jobIndex, entity);
			dynamicBuffer.Add(new LayoutElement(entity));
			Entity entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, bestSecond, workType, ref workAmount2, transform, source, state & WorkVehicleFlags.ExtractorVehicle);
			commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
			dynamicBuffer.Add(new LayoutElement(entity2));
			if (bestThird.m_Entity != Entity.Null)
			{
				entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, bestThird, workType, ref workAmount2, transform, source, state & WorkVehicleFlags.ExtractorVehicle);
				commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(entity2));
			}
			if (bestForth.m_Entity != Entity.Null)
			{
				entity2 = CreateVehicle(commandBuffer, jobIndex, ref random, bestForth, workType, ref workAmount2, transform, source, state & WorkVehicleFlags.ExtractorVehicle);
				commandBuffer.SetComponent(jobIndex, entity2, new Controller(entity));
				dynamicBuffer.Add(new LayoutElement(entity2));
			}
		}
		workAmount -= workAmount2;
		return entity;
	}

	private Entity CreateVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, ref Random random, VehicleData data, VehicleWorkType workType, ref float workAmount, Transform transform, Entity source, WorkVehicleFlags state)
	{
		Game.Vehicles.WorkVehicle component = new Game.Vehicles.WorkVehicle
		{
			m_State = state
		};
		if (workType == data.m_WorkVehicleData.m_WorkType && workAmount > 0f)
		{
			component.m_WorkAmount = math.min(workAmount, data.m_WorkVehicleData.m_MaxWorkAmount);
			workAmount -= component.m_WorkAmount;
		}
		Entity entity = commandBuffer.CreateEntity(jobIndex, data.m_ObjectData.m_Archetype);
		commandBuffer.SetComponent(jobIndex, entity, transform);
		commandBuffer.SetComponent(jobIndex, entity, component);
		commandBuffer.SetComponent(jobIndex, entity, new PrefabRef(data.m_Entity));
		commandBuffer.SetComponent(jobIndex, entity, new PseudoRandomSeed(ref random));
		commandBuffer.AddComponent(jobIndex, entity, new TripSource(source));
		commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
		return entity;
	}

	private bool PickVehicleData(ref Random random, DynamicBuffer<VehicleModel> vehicleModels, NativeList<Entity> prefabs, RoadTypes roadTypes, SizeClass sizeClass, VehicleWorkType workType, MapFeature mapFeature, Resource resource, out VehicleData bestFirst, out VehicleData bestSecond, out VehicleData bestThird, out VehicleData bestForth)
	{
		bestFirst = default(VehicleData);
		bestSecond = default(VehicleData);
		bestThird = default(VehicleData);
		bestForth = default(VehicleData);
		int totalProbability = 0;
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkVehicleData> nativeArray2 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTrailerData> nativeArray3 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<CarData> nativeArray5 = chunk.GetNativeArray(ref m_CarDataType);
			NativeArray<WatercraftData> nativeArray6 = chunk.GetNativeArray(ref m_WatercraftDataType);
			NativeArray<ObjectData> nativeArray7 = chunk.GetNativeArray(ref m_ObjectDataType);
			if ((nativeArray5.Length != 0 && (roadTypes & RoadTypes.Car) == 0) || (nativeArray6.Length != 0 && (roadTypes & RoadTypes.Watercraft) == 0))
			{
				continue;
			}
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray2[j]
				};
				if (vehicleData.m_WorkVehicleData.m_WorkType == VehicleWorkType.None || vehicleData.m_WorkVehicleData.m_WorkType != workType || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || vehicleData.m_WorkVehicleData.m_MaxWorkAmount == 0f)
				{
					continue;
				}
				if (sizeClass != SizeClass.Undefined)
				{
					WatercraftData value2;
					if (CollectionUtils.TryGet(nativeArray5, j, out var value))
					{
						if (value.m_SizeClass != SizeClass.Undefined && value.m_SizeClass != sizeClass)
						{
							continue;
						}
					}
					else if (CollectionUtils.TryGet(nativeArray6, j, out value2) && value2.m_SizeClass != SizeClass.Undefined && value2.m_SizeClass != sizeClass)
					{
						continue;
					}
				}
				if (!m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray[j];
				vehicleData.m_ObjectData = nativeArray7[j];
				if (!CheckVehicleModel(vehicleModels, vehicleData.m_Entity))
				{
					continue;
				}
				if (prefabs.IsCreated)
				{
					prefabs.Add(in vehicleData.m_Entity);
					continue;
				}
				bool flag = false;
				if (nativeArray3.Length != 0)
				{
					vehicleData.m_TrailerData = nativeArray3[j];
					flag = true;
				}
				if (nativeArray4.Length != 0)
				{
					vehicleData.m_TractorData = nativeArray4[j];
					if (vehicleData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						CheckTrailers(workType, mapFeature, resource, flag, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
						continue;
					}
				}
				if (flag)
				{
					CheckTractors(workType, mapFeature, resource, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
				}
				else if (PickVehicle(ref random, 100, ref totalProbability))
				{
					bestFirst = vehicleData;
					bestSecond = default(VehicleData);
					bestThird = default(VehicleData);
					bestForth = default(VehicleData);
				}
			}
		}
		return bestFirst.m_Entity != Entity.Null;
	}

	private bool CheckVehicleModel(DynamicBuffer<VehicleModel> vehicleModels, Entity prefab)
	{
		if (vehicleModels.IsEmpty)
		{
			return true;
		}
		for (int i = 0; i < vehicleModels.Length; i++)
		{
			if (vehicleModels[i].m_PrimaryPrefab == prefab)
			{
				return true;
			}
		}
		return false;
	}

	private void CheckTrailers(VehicleWorkType workType, MapFeature mapFeature, Resource resource, bool firstIsTrailer, VehicleData firstData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
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
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || vehicleData.m_WorkVehicleData.m_MaxWorkAmount != 0f || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TrailerData = nativeArray[j];
				if (firstData.m_TractorData.m_TrailerType != vehicleData.m_TrailerData.m_TrailerType || (firstData.m_TractorData.m_FixedTrailer != Entity.Null && firstData.m_TractorData.m_FixedTrailer != vehicleData.m_Entity) || (vehicleData.m_TrailerData.m_FixedTractor != Entity.Null && vehicleData.m_TrailerData.m_FixedTractor != firstData.m_Entity))
				{
					continue;
				}
				vehicleData.m_ObjectData = nativeArray5[j];
				if (nativeArray4.Length != 0)
				{
					vehicleData.m_TractorData = nativeArray4[j];
					if (vehicleData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						CheckTrailers(workType, mapFeature, resource, firstIsTrailer, firstData, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
						continue;
					}
				}
				if (firstIsTrailer)
				{
					CheckTractors(workType, mapFeature, resource, firstData, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
				}
				else if (PickVehicle(ref random, 100, ref totalProbability))
				{
					bestFirst = firstData;
					bestSecond = vehicleData;
					bestThird = default(VehicleData);
					bestForth = default(VehicleData);
				}
			}
		}
	}

	private void CheckTrailers(VehicleWorkType workType, MapFeature mapFeature, Resource resource, bool firstIsTrailer, VehicleData firstData, VehicleData secondData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
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
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || vehicleData.m_WorkVehicleData.m_MaxWorkAmount != 0f || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TrailerData = nativeArray[j];
				if (secondData.m_TractorData.m_TrailerType != vehicleData.m_TrailerData.m_TrailerType || (secondData.m_TractorData.m_FixedTrailer != Entity.Null && secondData.m_TractorData.m_FixedTrailer != vehicleData.m_Entity) || (vehicleData.m_TrailerData.m_FixedTractor != Entity.Null && vehicleData.m_TrailerData.m_FixedTractor != secondData.m_Entity))
				{
					continue;
				}
				vehicleData.m_ObjectData = nativeArray5[j];
				if (nativeArray4.Length != 0)
				{
					vehicleData.m_TractorData = nativeArray4[j];
					if (vehicleData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						if (!firstIsTrailer)
						{
							CheckTrailers(workType, mapFeature, resource, firstData, secondData, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
						}
						continue;
					}
				}
				if (firstIsTrailer)
				{
					CheckTractors(workType, mapFeature, resource, firstData, secondData, vehicleData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
				}
				else if (PickVehicle(ref random, 100, ref totalProbability))
				{
					bestFirst = firstData;
					bestSecond = secondData;
					bestThird = vehicleData;
					bestForth = default(VehicleData);
				}
			}
		}
	}

	private void CheckTrailers(VehicleWorkType workType, MapFeature mapFeature, Resource resource, VehicleData firstData, VehicleData secondData, VehicleData thirdData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
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
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTractorData> nativeArray4 = chunk.GetNativeArray(ref m_CarTractorDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || vehicleData.m_WorkVehicleData.m_MaxWorkAmount != 0f || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TrailerData = nativeArray[j];
				if (thirdData.m_TractorData.m_TrailerType != vehicleData.m_TrailerData.m_TrailerType || (thirdData.m_TractorData.m_FixedTrailer != Entity.Null && thirdData.m_TractorData.m_FixedTrailer != vehicleData.m_Entity) || (vehicleData.m_TrailerData.m_FixedTractor != Entity.Null && vehicleData.m_TrailerData.m_FixedTractor != thirdData.m_Entity))
				{
					continue;
				}
				vehicleData.m_ObjectData = nativeArray5[j];
				if (nativeArray4.Length != 0)
				{
					vehicleData.m_TractorData = nativeArray4[j];
					if (vehicleData.m_TractorData.m_FixedTrailer != Entity.Null)
					{
						continue;
					}
				}
				if (PickVehicle(ref random, 100, ref totalProbability))
				{
					bestFirst = firstData;
					bestSecond = secondData;
					bestThird = thirdData;
					bestForth = vehicleData;
				}
			}
		}
	}

	private void CheckTractors(VehicleWorkType workType, MapFeature mapFeature, Resource resource, VehicleData secondData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTrailerData> nativeArray4 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TractorData = nativeArray[j];
				if (vehicleData.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(vehicleData.m_TractorData.m_FixedTrailer != Entity.Null) || !(vehicleData.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != vehicleData.m_Entity)))
				{
					vehicleData.m_ObjectData = nativeArray5[j];
					if (nativeArray4.Length != 0)
					{
						vehicleData.m_TrailerData = nativeArray4[j];
						CheckTractors(workType, mapFeature, resource, vehicleData, secondData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
					}
					else if (PickVehicle(ref random, 100, ref totalProbability))
					{
						bestFirst = vehicleData;
						bestSecond = secondData;
						bestThird = default(VehicleData);
						bestForth = default(VehicleData);
					}
				}
			}
		}
	}

	private void CheckTractors(VehicleWorkType workType, MapFeature mapFeature, Resource resource, VehicleData secondData, VehicleData thirdData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
	{
		for (int i = 0; i < m_PrefabChunks.Length; i++)
		{
			ArchetypeChunk chunk = m_PrefabChunks[i];
			NativeArray<CarTractorData> nativeArray = chunk.GetNativeArray(ref m_CarTractorDataType);
			if (nativeArray.Length == 0)
			{
				continue;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<CarTrailerData> nativeArray4 = chunk.GetNativeArray(ref m_CarTrailerDataType);
			NativeArray<ObjectData> nativeArray5 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TractorData = nativeArray[j];
				if (vehicleData.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(vehicleData.m_TractorData.m_FixedTrailer != Entity.Null) || !(vehicleData.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != vehicleData.m_Entity)))
				{
					vehicleData.m_ObjectData = nativeArray5[j];
					if (nativeArray4.Length != 0)
					{
						vehicleData.m_TrailerData = nativeArray4[j];
						CheckTractors(workType, mapFeature, resource, vehicleData, secondData, thirdData, ref random, ref bestFirst, ref bestSecond, ref bestThird, ref bestForth, ref totalProbability);
					}
					else if (PickVehicle(ref random, 100, ref totalProbability))
					{
						bestFirst = vehicleData;
						bestSecond = secondData;
						bestThird = thirdData;
						bestForth = default(VehicleData);
					}
				}
			}
		}
	}

	private void CheckTractors(VehicleWorkType workType, MapFeature mapFeature, Resource resource, VehicleData secondData, VehicleData thirdData, VehicleData forthData, ref Random random, ref VehicleData bestFirst, ref VehicleData bestSecond, ref VehicleData bestThird, ref VehicleData bestForth, ref int totalProbability)
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
			NativeArray<WorkVehicleData> nativeArray3 = chunk.GetNativeArray(ref m_WorkVehicleDataType);
			NativeArray<ObjectData> nativeArray4 = chunk.GetNativeArray(ref m_ObjectDataType);
			VehicleSelectRequirementData.Chunk chunk2 = m_RequirementData.GetChunk(chunk);
			for (int j = 0; j < nativeArray.Length; j++)
			{
				VehicleData vehicleData = new VehicleData
				{
					m_WorkVehicleData = nativeArray3[j]
				};
				if ((vehicleData.m_WorkVehicleData.m_WorkType != VehicleWorkType.None && vehicleData.m_WorkVehicleData.m_WorkType != workType) || ((vehicleData.m_WorkVehicleData.m_MapFeature != MapFeature.None || vehicleData.m_WorkVehicleData.m_Resources != Resource.NoResource) && vehicleData.m_WorkVehicleData.m_MapFeature != mapFeature && (vehicleData.m_WorkVehicleData.m_Resources & resource) == Resource.NoResource) || !m_RequirementData.CheckRequirements(ref chunk2, j))
				{
					continue;
				}
				vehicleData.m_Entity = nativeArray2[j];
				vehicleData.m_TractorData = nativeArray[j];
				if (vehicleData.m_TractorData.m_TrailerType == secondData.m_TrailerData.m_TrailerType && (!(vehicleData.m_TractorData.m_FixedTrailer != Entity.Null) || !(vehicleData.m_TractorData.m_FixedTrailer != secondData.m_Entity)) && (!(secondData.m_TrailerData.m_FixedTractor != Entity.Null) || !(secondData.m_TrailerData.m_FixedTractor != vehicleData.m_Entity)))
				{
					vehicleData.m_ObjectData = nativeArray4[j];
					if (PickVehicle(ref random, 100, ref totalProbability))
					{
						bestFirst = vehicleData;
						bestSecond = secondData;
						bestThird = thirdData;
						bestForth = forthData;
					}
				}
			}
		}
	}

	private bool PickVehicle(ref Random random, int probability, ref int totalProbability)
	{
		totalProbability += probability;
		return random.NextInt(totalProbability) < probability;
	}
}
