using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct AmbienceEmitterData : IComponentData, IQueryTypeParameter
{
	public GroupAmbienceType m_AmbienceType;

	public float m_Intensity;
}
