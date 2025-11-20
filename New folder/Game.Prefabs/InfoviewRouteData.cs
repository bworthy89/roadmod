using Game.Routes;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewRouteData : IComponentData, IQueryTypeParameter
{
	public RouteType m_Type;
}
