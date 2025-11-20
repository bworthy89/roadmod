using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetSubSection : IBufferElementData
{
	public Entity m_SubSection;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public NetSectionFlags m_SectionAll;

	public NetSectionFlags m_SectionAny;

	public NetSectionFlags m_SectionNone;
}
