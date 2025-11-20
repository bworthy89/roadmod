using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct UpdateAction : IDisposable
{
	public NativeArray<UpdateActionData> m_UpdateData;

	public UpdateAction(int size, Allocator allocator)
	{
		m_UpdateData = new NativeArray<UpdateActionData>(size, allocator);
	}

	public void Dispose()
	{
		m_UpdateData.Dispose();
	}
}
