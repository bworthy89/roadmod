using Unity.Entities;

namespace Game.Prefabs;

public struct XPParameterData : IComponentData, IQueryTypeParameter
{
	public float m_XPPerPopulation;

	public float m_XPPerHappiness;
}
