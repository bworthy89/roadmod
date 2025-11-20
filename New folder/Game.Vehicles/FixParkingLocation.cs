using Unity.Entities;

namespace Game.Vehicles;

public struct FixParkingLocation : IComponentData, IQueryTypeParameter
{
	public Entity m_ChangeLane;

	public Entity m_ResetLocation;

	public FixParkingLocation(Entity changeLane, Entity resetLocation)
	{
		m_ChangeLane = changeLane;
		m_ResetLocation = resetLocation;
	}
}
