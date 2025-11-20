using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class PlaceableNetGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_XPRewardMultiplier;

		public ComponentTypeHandle<PlaceableNetData> m_PlaceableNetType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PlaceableNetData> nativeArray = chunk.GetNativeArray(ref m_PlaceableNetType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PlaceableNetData value = nativeArray[i];
				value.m_XPReward = (int)((float)value.m_XPReward * m_XPRewardMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_XPRewardMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<PlaceableNetData>(),
			ComponentType.ReadOnly<PlaceableInfoviewItem>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<FenceData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PlaceableNetData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_XPRewardMultiplier = m_XPRewardMultiplier,
			m_PlaceableNetType = entityManager.GetComponentTypeHandle<PlaceableNetData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<PlaceableNet>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			PlaceableNetData componentData = entityManager.GetComponentData<PlaceableNetData>(entity);
			componentData.m_XPReward = component.m_XPReward;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
