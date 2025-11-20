using Unity.Entities;

namespace Game.Common;

public struct BufferedEntity
{
	public Entity m_Value;

	public bool m_Stored;

	public BufferedEntity(Entity value, bool stored)
	{
		m_Value = value;
		m_Stored = stored;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}", "m_Value", m_Value, "m_Stored", m_Stored);
	}
}
