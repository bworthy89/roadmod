using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game;

public class EndFrameBarrier : SafeCommandBufferSystem
{
	private Stopwatch m_Stopwatch;

	public JobHandle producerHandle { get; private set; }

	public float lastElapsedTime { get; private set; }

	public float currentElapsedTime => (float)m_Stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Stopwatch = new Stopwatch();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Stopwatch.Stop();
		base.OnDestroy();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Preserve]
	protected override void OnUpdate()
	{
		m_Stopwatch.Stop();
		lastElapsedTime = (float)m_Stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
		m_Stopwatch.Reset();
		producerHandle.Complete();
		producerHandle = default(JobHandle);
		m_Stopwatch.Start();
		base.OnUpdate();
	}

	public new void AddJobHandleForProducer(JobHandle producerJob)
	{
		producerHandle = JobHandle.CombineDependencies(producerHandle, producerJob);
	}

	[Preserve]
	public EndFrameBarrier()
	{
	}
}
