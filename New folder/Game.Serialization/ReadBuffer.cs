using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Serialization;

public class ReadBuffer : IReadBuffer
{
	public NativeArray<byte> buffer { get; private set; }

	public NativeReference<int> position { get; private set; }

	public ReadBuffer(int size)
	{
		buffer = new NativeArray<byte>(size, Allocator.TempJob);
		position = new NativeReference<int>(0, Allocator.TempJob);
	}

	public void Done(JobHandle handle)
	{
		buffer.Dispose(handle);
		position.Dispose(handle);
	}

	public void Done()
	{
		buffer.Dispose();
		position.Dispose();
	}
}
