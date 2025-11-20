using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct InfoviewMode : IBufferElementData
{
	public Entity m_Mode;

	public int m_Priority;

	public bool m_Supplemental;

	public bool m_Optional;

	public InfoviewMode(Entity mode, int priority, bool supplemental, bool optional)
	{
		m_Mode = mode;
		m_Priority = priority;
		m_Supplemental = supplemental;
		m_Optional = optional;
	}
}
