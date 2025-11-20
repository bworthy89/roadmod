using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Controller : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Controller;

	public Controller(Entity controller)
	{
		m_Controller = controller;
	}
}
