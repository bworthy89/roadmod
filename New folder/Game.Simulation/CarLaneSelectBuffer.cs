using Unity.Collections;

namespace Game.Simulation;

public struct CarLaneSelectBuffer
{
	private NativeArray<float> m_Buffer;

	public NativeArray<float> Ensure()
	{
		if (!m_Buffer.IsCreated)
		{
			m_Buffer = new NativeArray<float>(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
		}
		return m_Buffer;
	}

	public void Dispose()
	{
	}
}
