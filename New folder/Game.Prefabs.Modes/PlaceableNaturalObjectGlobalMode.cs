using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class PlaceableNaturalObjectGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_ConstructionCostMultiplier;

		public float m_XPRewardMultiplier;

		public ComponentTypeHandle<PlaceableObjectData> m_PlaceableObjectType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PlaceableObjectData> nativeArray = chunk.GetNativeArray(ref m_PlaceableObjectType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PlaceableObjectData value = nativeArray[i];
				value.m_ConstructionCost = (uint)((float)value.m_ConstructionCost * m_ConstructionCostMultiplier);
				value.m_XPReward = (int)((float)value.m_XPReward * m_XPRewardMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Header("Modify the construction cost and XP reward of placeable natural objects: Tree, Plant")]
	public float m_ConstructionCostMultiplier;

	public float m_XPRewardMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<PlaceableObjectData>(),
			ComponentType.ReadOnly<PlaceableInfoviewItem>()
		};
		entityQueryDesc.Any = new ComponentType[2]
		{
			ComponentType.ReadOnly<TreeData>(),
			ComponentType.ReadOnly<PlantData>()
		};
		entityQueryDesc.None = new ComponentType[2]
		{
			ComponentType.ReadOnly<BuildingData>(),
			ComponentType.ReadOnly<BuildingExtensionData>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PlaceableObjectData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_ConstructionCostMultiplier = m_ConstructionCostMultiplier,
			m_XPRewardMultiplier = m_XPRewardMultiplier,
			m_PlaceableObjectType = entityManager.GetComponentTypeHandle<PlaceableObjectData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<PlaceableObject>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
			componentData.m_ConstructionCost = component.m_ConstructionCost;
			componentData.m_XPReward = component.m_XPReward;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
