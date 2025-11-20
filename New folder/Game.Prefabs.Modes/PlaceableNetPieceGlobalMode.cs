using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class PlaceableNetPieceGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_ConstructionCostMultiplier;

		public float m_ElevationCostMultiplier;

		public float m_UpkeepCostMultiplier;

		public ComponentTypeHandle<PlaceableNetPieceData> m_PlaceableNetPieceType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PlaceableNetPieceData> nativeArray = chunk.GetNativeArray(ref m_PlaceableNetPieceType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PlaceableNetPieceData value = nativeArray[i];
				value.m_ConstructionCost = (uint)((float)value.m_ConstructionCost * m_ConstructionCostMultiplier);
				value.m_ElevationCost = (uint)((float)value.m_ElevationCost * m_ElevationCostMultiplier);
				value.m_UpkeepCost *= m_UpkeepCostMultiplier;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_ConstructionCostMultiplier;

	public float m_ElevationCostMultiplier;

	public float m_UpkeepCostMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<PlaceableNetPieceData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PlaceableNetPieceData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_ConstructionCostMultiplier = m_ConstructionCostMultiplier,
			m_ElevationCostMultiplier = m_ElevationCostMultiplier,
			m_UpkeepCostMultiplier = m_UpkeepCostMultiplier,
			m_PlaceableNetPieceType = entityManager.GetComponentTypeHandle<PlaceableNetPieceData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<PlaceableNetPiece>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			PlaceableNetPieceData componentData = entityManager.GetComponentData<PlaceableNetPieceData>(entity);
			componentData.m_ConstructionCost = component.m_ConstructionCost;
			componentData.m_ElevationCost = component.m_ElevationCost;
			componentData.m_UpkeepCost = component.m_UpkeepCost;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
