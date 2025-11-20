using Unity.Entities;

namespace Game.Prefabs;

public struct TransportRequirementData : IComponentData, IQueryTypeParameter
{
	public Entity m_BuildingPrefab;

	public TransportType m_TransportType;

	public int m_FilterID;

	public int m_MinimumTransportedPassenger;

	public int m_MinimumTransportedCargo;
}
