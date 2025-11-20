using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct BrushCell : IBufferElementData
{
	public float m_Opacity;
}
