using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneBuiltLevelUpdate
{
	public Entity m_Zone;

	public int m_FromLevel;

	public int m_ToLevel;

	public int m_Squares;
}
