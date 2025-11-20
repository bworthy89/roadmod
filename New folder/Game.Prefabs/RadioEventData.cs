using Game.Audio.Radio;
using Unity.Entities;

namespace Game.Prefabs;

public struct RadioEventData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;

	public Radio.SegmentType m_SegmentType;

	public int m_EmergencyFrameDelay;
}
