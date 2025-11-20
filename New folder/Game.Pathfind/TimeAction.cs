using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct TimeAction : IDisposable
{
	public NativeQueue<TimeActionData> m_TimeData;

	public TimeAction(Allocator allocator)
	{
		m_TimeData = new NativeQueue<TimeActionData>(allocator);
	}

	public void Dispose()
	{
		m_TimeData.Dispose();
	}
}
