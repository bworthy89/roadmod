using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetGeometryEdgeState : IBufferElementData
{
	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public CompositionFlags m_State;
}
