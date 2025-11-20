using Colossal;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Debug;

public class LightDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct LightGizmoJob : IJob
	{
		[ReadOnly]
		public bool m_SpotOption;

		[ReadOnly]
		public bool m_PositionOption;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<HDRPDotsInputs.PunctualLightData> m_punctualLights;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
		}
	}

	private EntityQuery m_LightEffectPrefabQuery;

	private GizmosSystem m_GizmosSystem;

	private Option m_SpotOption;

	private Option m_PositionOption;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_LightEffectPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<LightEffectData>(), ComponentType.ReadOnly<PrefabData>());
		m_PositionOption = AddOption("Show positions", defaultEnabled: false);
		m_SpotOption = AddOption("Spot Lights Cones", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		HDRPDotsInputs.punctualLightsJobHandle.Complete();
		if (HDRPDotsInputs.s_punctualLightdata.Length != 0)
		{
			JobHandle dependencies;
			LightGizmoJob jobData = new LightGizmoJob
			{
				m_SpotOption = m_SpotOption.enabled,
				m_PositionOption = m_PositionOption.enabled,
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
			};
			jobData.m_punctualLights = new NativeArray<HDRPDotsInputs.PunctualLightData>(HDRPDotsInputs.s_punctualLightdata.AsArray(), Allocator.Persistent);
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependencies);
			m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	[Preserve]
	public LightDebugSystem()
	{
	}
}
