using Unity.Entities;

namespace Game.Events;

public struct Submerge : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public float m_Depth;
}
