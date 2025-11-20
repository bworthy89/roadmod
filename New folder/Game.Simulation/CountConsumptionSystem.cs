using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class CountConsumptionSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CopyConsumptionJob : IJob
	{
		public NativeArray<int> m_Accumulator;

		public NativeArray<int> m_Consumptions;

		public void Execute()
		{
			for (int i = 0; i < m_Accumulator.Length; i++)
			{
				m_Consumptions[i] = ((m_Consumptions[i] == 0) ? m_Accumulator[i] : Mathf.RoundToInt((float)kUpdatesPerDay * math.lerp(m_Consumptions[i] / kUpdatesPerDay, m_Accumulator[i], 0.3f)));
				m_Accumulator[i] = 0;
			}
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private NativeArray<int> m_Consumptions;

	private NativeArray<int> m_ConsumptionAccumulator;

	private JobHandle m_ReadDeps;

	private JobHandle m_WriteDeps;

	private JobHandle m_CopyDeps;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public NativeArray<int> GetConsumptions(out JobHandle deps)
	{
		deps = m_CopyDeps;
		return m_Consumptions;
	}

	public NativeArray<int> GetConsumptionAccumulator(out JobHandle deps)
	{
		deps = m_WriteDeps;
		return m_ConsumptionAccumulator;
	}

	public void AddConsumptionReader(JobHandle deps)
	{
		m_ReadDeps = JobHandle.CombineDependencies(m_ReadDeps, deps);
	}

	public void AddConsumptionWriter(JobHandle deps)
	{
		m_WriteDeps = JobHandle.CombineDependencies(m_WriteDeps, deps);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Consumptions = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
		m_ConsumptionAccumulator = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ConsumptionAccumulator.Dispose();
		m_Consumptions.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CopyConsumptionJob jobData = new CopyConsumptionJob
		{
			m_Accumulator = m_ConsumptionAccumulator,
			m_Consumptions = m_Consumptions
		};
		base.Dependency = jobData.Schedule(JobHandle.CombineDependencies(m_ReadDeps, m_WriteDeps));
		m_CopyDeps = base.Dependency;
		m_WriteDeps = base.Dependency;
	}

	public void SetDefaults(Context context)
	{
		for (int i = 0; i < m_ConsumptionAccumulator.Length; i++)
		{
			m_ConsumptionAccumulator[i] = 0;
			m_Consumptions[i] = 0;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NativeArray<int> consumptionAccumulator = m_ConsumptionAccumulator;
		writer.Write(consumptionAccumulator);
		NativeArray<int> consumptions = m_Consumptions;
		writer.Write(consumptions);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> consumptionAccumulator = m_ConsumptionAccumulator;
			reader.Read(consumptionAccumulator);
			NativeArray<int> consumptions = m_Consumptions;
			reader.Read(consumptions);
		}
		else
		{
			NativeArray<int> subArray = m_ConsumptionAccumulator.GetSubArray(0, 40);
			reader.Read(subArray);
			NativeArray<int> subArray2 = m_Consumptions.GetSubArray(0, 40);
			reader.Read(subArray2);
			m_ConsumptionAccumulator[40] = 0;
			m_Consumptions[40] = 0;
		}
	}

	[Preserve]
	public CountConsumptionSystem()
	{
	}
}
