using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ProcessingCompanyGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_InputMultiplier;

		public float m_OutputMultiplier;

		public ComponentTypeHandle<IndustrialProcessData> m_ProcessingType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<IndustrialProcessData> nativeArray = chunk.GetNativeArray(ref m_ProcessingType);
			for (int i = 0; i < chunk.Count; i++)
			{
				IndustrialProcessData value = nativeArray[i];
				value.m_Input1.m_Amount = (int)((float)value.m_Input1.m_Amount * m_InputMultiplier);
				value.m_Input2.m_Amount = (int)((float)value.m_Input2.m_Amount * m_InputMultiplier);
				value.m_Output.m_Amount = (int)((float)value.m_Output.m_Amount * m_OutputMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Header("Modify the all input requirements and output production of processing companies.")]
	public float m_InputMultiplier;

	public float m_OutputMultiplier;

	private Dictionary<Entity, IndustrialProcessData> m_OriginalData;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<IndustrialProcessData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<IndustrialProcessData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_OriginalData = new Dictionary<Entity, IndustrialProcessData>(entities.Length);
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			IndustrialProcessData componentData = entityManager.GetComponentData<IndustrialProcessData>(entity);
			m_OriginalData.Add(entity, componentData);
		}
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_InputMultiplier = m_InputMultiplier,
			m_OutputMultiplier = m_OutputMultiplier,
			m_ProcessingType = entityManager.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (m_OriginalData.TryGetValue(entity, out var value))
			{
				entityManager.SetComponentData(entity, value);
			}
			else
			{
				m_OriginalData.Add(entity, entityManager.GetComponentData<IndustrialProcessData>(entity));
			}
		}
	}
}
