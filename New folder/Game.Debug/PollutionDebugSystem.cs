using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class PollutionDebugSystem : BaseDebugSystem
{
	private struct PollutionGizmoJob : IJob
	{
		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		public GizmoBatcher m_GizmoBatcher;

		public bool m_GroundOption;

		public bool m_AirOption;

		public bool m_NoiseOption;

		public float m_BaseHeight;

		public void Execute()
		{
			float3 @float = new float3(0f, m_BaseHeight, 0f);
			if (m_GroundOption)
			{
				for (int i = 0; i < m_PollutionMap.Length; i++)
				{
					GroundPollution groundPollution = m_PollutionMap[i];
					if (groundPollution.m_Pollution > 0)
					{
						float3 cellCenter = GroundPollutionSystem.GetCellCenter(i);
						cellCenter.y += (float)groundPollution.m_Pollution / 400f;
						Color color = ((groundPollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(groundPollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)groundPollution.m_Pollution / 8000f));
						m_GizmoBatcher.DrawWireCube(cellCenter + @float, new float3(10f, (float)groundPollution.m_Pollution / 200f, 10f), color);
					}
				}
			}
			if (m_AirOption)
			{
				for (int j = 0; j < m_AirPollutionMap.Length; j++)
				{
					AirPollution airPollution = m_AirPollutionMap[j];
					if (airPollution.m_Pollution > 0)
					{
						float3 cellCenter2 = AirPollutionSystem.GetCellCenter(j);
						cellCenter2.y += 200f;
						Color color2 = ((airPollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(airPollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)airPollution.m_Pollution / 8000f));
						m_GizmoBatcher.DrawWireCone(cellCenter2 + @float, 10f, cellCenter2 + @float + new float3(0f, (float)airPollution.m_Pollution / 50f, 0f), 10f, color2);
					}
				}
			}
			if (!m_NoiseOption)
			{
				return;
			}
			for (int k = 0; k < m_NoisePollutionMap.Length; k++)
			{
				NoisePollution noisePollution = m_NoisePollutionMap[k];
				if (noisePollution.m_Pollution > 0)
				{
					float3 cellCenter3 = NoisePollutionSystem.GetCellCenter(k);
					cellCenter3.y += 50f + (float)noisePollution.m_Pollution / 400f;
					Color color3 = ((noisePollution.m_Pollution >= 8000) ? Color.Lerp(Color.yellow, Color.red, math.saturate((float)(noisePollution.m_Pollution - 8000) / 8000f)) : Color.Lerp(Color.green, Color.yellow, (float)noisePollution.m_Pollution / 8000f));
					m_GizmoBatcher.DrawWireCube(cellCenter3 + @float, new float3(10f, (float)noisePollution.m_Pollution / 200f, 10f), color3);
				}
			}
		}
	}

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private ClimateSystem m_ClimateSystem;

	private GizmosSystem m_GizmosSystem;

	private Option m_GroundOption;

	private Option m_AirOption;

	private Option m_NoiseOption;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_GroundOption = AddOption("Ground pollution", defaultEnabled: true);
		m_AirOption = AddOption("Air pollution", defaultEnabled: true);
		m_NoiseOption = AddOption("Noise pollution", defaultEnabled: true);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle jobHandle = new PollutionGizmoJob
		{
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies4),
			m_AirOption = m_AirOption.enabled,
			m_GroundOption = m_GroundOption.enabled,
			m_NoiseOption = m_NoiseOption.enabled,
			m_BaseHeight = m_ClimateSystem.temperatureBaseHeight
		}.Schedule(JobHandle.CombineDependencies(dependencies2, dependencies3, JobHandle.CombineDependencies(inputDeps, dependencies4, dependencies)));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_AirPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		return jobHandle;
	}

	[Preserve]
	public PollutionDebugSystem()
	{
	}
}
