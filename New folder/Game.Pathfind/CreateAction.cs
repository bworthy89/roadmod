using System;
using Unity.Collections;

namespace Game.Pathfind;

public struct CreateAction : IDisposable
{
	public NativeArray<CreateActionData> m_CreateData;

	public CreateAction(int size, Allocator allocator)
	{
		m_CreateData = new NativeArray<CreateActionData>(size, allocator);
	}

	public void Dispose()
	{
		m_CreateData.Dispose();
	}
}
