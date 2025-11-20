using Game.Prefabs;
using Unity.Entities;

namespace Game.Simulation;

public struct TransportUsageEvent
{
	public Entity m_Building;

	public TransportType m_TransportType;

	public int m_TransportedPassenger;

	public int m_TransportedCargo;
}
