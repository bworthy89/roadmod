using Unity.Entities;

namespace Game.Prefabs;

public struct UIObjectData : IComponentData, IQueryTypeParameter
{
	public Entity m_Group;

	public int m_Priority;
}
