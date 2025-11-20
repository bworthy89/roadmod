using Unity.Entities;

namespace Game.Routes;

public struct ColorUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Route;

	public ColorUpdated(Entity route)
	{
		m_Route = route;
	}
}
