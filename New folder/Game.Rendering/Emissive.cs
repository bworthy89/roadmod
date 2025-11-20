using Colossal.Collections;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct Emissive : IBufferElementData, IEmptySerializable
{
	public NativeHeapBlock m_BufferAllocation;

	public int m_LightOffset;

	public bool m_Updated;
}
