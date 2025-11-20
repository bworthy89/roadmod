using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneLevelUpResourceData : IBufferElementData
{
	public ResourceStack m_LevelUpResource;

	public int m_Level;
}
