using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public interface ICombineBuffer<T> where T : unmanaged, IBufferElementData
{
	void Combine(NativeList<T> result);
}
