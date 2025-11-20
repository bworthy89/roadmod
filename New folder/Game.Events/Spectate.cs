using Unity.Entities;

namespace Game.Events;

public struct Spectate : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;
}
