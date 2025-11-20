using Unity.Entities;

namespace Game.Prefabs;

public struct StreetLightData : IComponentData, IQueryTypeParameter
{
	public StreetLightLayer m_Layer;
}
