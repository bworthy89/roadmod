using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct DeleteAction : IDisposable
{
	public NativeArray<DeleteActionData> m_DeleteData;

	public DeleteAction(int size, Allocator allocator)
	{
		m_DeleteData = new NativeArray<DeleteActionData>(size, allocator);
	}

	public void Dispose()
	{
		m_DeleteData.Dispose();
	}
}
