using Unity.Entities;

namespace Game.Routes;

public struct PathSource : IComponentData, IQueryTypeParameter
{
	public Entity m_Entity;

	public int m_UpdateFrame;
}
