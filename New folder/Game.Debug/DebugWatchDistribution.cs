using System;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Debug;

public class DebugWatchDistribution : IDisposable
{
	public struct ClearJob : IJob
	{
		public NativeQueue<int> m_RawData;

		public void Execute()
		{
			m_RawData.Clear();
		}
	}

	private NativeQueue<int> m_RawData;

	private JobHandle m_Deps;

	private bool m_Persistent;

	private bool m_Relative;

	public bool Persistent => m_Persistent;

	public bool Relative => m_Relative;

	public bool IsEnabled => m_RawData.IsCreated;

	public DebugWatchDistribution(bool persistent = false, bool relative = false)
	{
		m_Persistent = persistent;
		m_Relative = relative;
	}

	public NativeQueue<int> GetQueue(bool clear, out JobHandle deps)
	{
		if (!IsEnabled)
		{
			throw new Exception("cannot get data queue from disabled DebugWatchDistribution");
		}
		if (clear)
		{
			ClearJob jobData = new ClearJob
			{
				m_RawData = m_RawData
			};
			m_Deps = jobData.Schedule(m_Deps);
		}
		deps = m_Deps;
		return m_RawData;
	}

	public void AddWriter(JobHandle handle)
	{
		if (!IsEnabled)
		{
			throw new Exception("cannot add writer to disabled DebugWatchDistribution");
		}
		m_Deps = JobHandle.CombineDependencies(m_Deps, handle);
	}

	public void Enable()
	{
		if (!IsEnabled)
		{
			m_RawData = new NativeQueue<int>(Allocator.Persistent);
			m_Deps = default(JobHandle);
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			m_Deps.Complete();
			m_RawData.Dispose();
		}
	}

	public void Dispose()
	{
		Disable();
	}
}
