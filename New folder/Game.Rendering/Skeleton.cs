using Colossal.Collections;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct Skeleton : IBufferElementData, IEmptySerializable
{
	public NativeHeapBlock m_BufferAllocation;

	public int m_BoneOffset;

	public int m_LayerOffset;

	public bool m_CurrentUpdated;

	public bool m_HistoryUpdated;

	public bool m_RequireHistory;
}
