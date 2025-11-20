using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct GroupAmbienceData : IComponentData, IQueryTypeParameter
{
	public GroupAmbienceType m_AmbienceType;
}
