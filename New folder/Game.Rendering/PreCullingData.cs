using Unity.Entities;

namespace Game.Rendering;

public struct PreCullingData
{
	public Entity m_Entity;

	public PreCullingFlags m_Flags;

	public sbyte m_UpdateFrame;

	public byte m_Timer;
}
