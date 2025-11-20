using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BrushData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;

	public int m_Priority;

	public int2 m_Resolution;
}
