using Unity.Entities;

namespace Game.Prefabs;

public struct DestructibleObjectData : IComponentData, IQueryTypeParameter
{
	public float m_FireHazard;

	public float m_StructuralIntegrity;
}
