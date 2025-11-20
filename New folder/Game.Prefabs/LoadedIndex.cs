using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct LoadedIndex : IBufferElementData
{
	public int m_Index;
}
