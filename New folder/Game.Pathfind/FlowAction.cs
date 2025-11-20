using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct FlowAction : IDisposable
{
	public NativeQueue<FlowActionData> m_FlowData;

	public FlowAction(Allocator allocator)
	{
		m_FlowData = new NativeQueue<FlowActionData>(allocator);
	}

	public void Dispose()
	{
		m_FlowData.Dispose();
	}
}
