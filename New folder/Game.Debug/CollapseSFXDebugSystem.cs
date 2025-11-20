using System.Runtime.CompilerServices;
using Colossal;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class CollapseSFXDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct CollapseSfxCoverageGizmoJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PreFabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public BufferLookup<Effect> m_EffectsBuffs;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PreFabRefType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				_ = nativeArray3[i];
				Game.Objects.Transform transform = nativeArray2[i];
				if (!m_EffectsBuffs.TryGetBuffer(prefab, out var bufferData))
				{
					continue;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					Effect effect = bufferData[j];
					if (effect.m_Effect == m_BuildingConfigurationData.m_CollapseSFX)
					{
						float3 @float = ObjectUtils.LocalToWorld(transform, effect.m_Position);
						m_GizmoBatcher.DrawLine(@float, @float + new float3(0f, 200f, 0f), UnityEngine.Color.yellow);
					}
					else if (effect.m_Effect == m_BuildingConfigurationData.m_FireLoopSFX || effect.m_Effect == m_BuildingConfigurationData.m_FireSpotSFX)
					{
						float3 float2 = ObjectUtils.LocalToWorld(transform, effect.m_Position);
						m_GizmoBatcher.DrawLine(float2, float2 + new float3(0f, 200f, 0f), UnityEngine.Color.red);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_BuildingEffectGroup;

	private EntityQuery m_ConfigurationQuery;

	private GizmosSystem m_GizmosSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_BuildingEffectGroup = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_BuildingEffectGroup);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CollapseSfxCoverageGizmoJob
		{
			m_EntityType = GetEntityTypeHandle(),
			m_PreFabRefType = GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
			m_TransformType = GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
			m_EffectsBuffs = GetBufferLookup<Effect>(isReadOnly: true),
			m_BuildingConfigurationData = m_ConfigurationQuery.GetSingleton<BuildingConfigurationData>(),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, m_BuildingEffectGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return JobHandle.CombineDependencies(inputDeps, jobHandle);
	}

	[Preserve]
	public CollapseSFXDebugSystem()
	{
	}
}
