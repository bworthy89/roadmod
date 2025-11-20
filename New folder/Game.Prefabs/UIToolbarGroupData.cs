using Unity.Entities;

namespace Game.Prefabs;

public struct UIToolbarGroupData : IComponentData, IQueryTypeParameter
{
	public int m_Priority;
}
