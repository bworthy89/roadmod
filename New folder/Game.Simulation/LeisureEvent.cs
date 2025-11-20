using Unity.Entities;

namespace Game.Simulation;

public struct LeisureEvent
{
	public Entity m_Citizen;

	public Entity m_Provider;

	public int m_Efficiency;
}
