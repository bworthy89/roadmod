using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct DensityAction : IDisposable
{
	public NativeQueue<DensityActionData> m_DensityData;

	public DensityAction(Allocator allocator)
	{
		m_DensityData = new NativeQueue<DensityActionData>(allocator);
	}

	public void Dispose()
	{
		m_DensityData.Dispose();
	}
}
