using System;
using Game.Areas;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class TreeObjectGlobal : EntityQueryModePrefab, IMapResourceMultiplier
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_WoodAmountMultiplier;

		public ComponentTypeHandle<TreeData> m_TreeType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<TreeData> nativeArray = chunk.GetNativeArray(ref m_TreeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				TreeData value = nativeArray[i];
				value.m_WoodAmount *= m_WoodAmountMultiplier;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_WoodAmountMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<PlantData>(),
			ComponentType.ReadOnly<TreeData>(),
			ComponentType.ReadOnly<GrowthScaleData>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<TreeData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_WoodAmountMultiplier = m_WoodAmountMultiplier,
			m_TreeType = entityManager.GetComponentTypeHandle<TreeData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<TreeObject>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			TreeData componentData = entityManager.GetComponentData<TreeData>(entity);
			componentData.m_WoodAmount = component.m_WoodAmount;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public bool TryGetMultiplier(MapFeature feature, out float multiplier)
	{
		if (feature == MapFeature.Forest)
		{
			multiplier = m_WoodAmountMultiplier;
			return true;
		}
		multiplier = 1f;
		return false;
	}
}
