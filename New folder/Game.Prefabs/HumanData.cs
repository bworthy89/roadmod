using Unity.Entities;

namespace Game.Prefabs;

public struct HumanData : IComponentData, IQueryTypeParameter
{
	public float m_WalkSpeed;

	public float m_RunSpeed;

	public float m_Acceleration;
}
