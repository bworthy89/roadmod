using Colossal.Collections;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct Batch : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public NativeHeapBlock m_BatchAllocation;

	public int m_AllocatedSize;

	public int m_BatchIndex;

	public int m_VisibleCount;

	public int m_MetaIndex;
}
