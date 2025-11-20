using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct AreasConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_DefaultDistrictPrefab;

	public Bounds1 m_BuildableLandMaxSlope;
}
