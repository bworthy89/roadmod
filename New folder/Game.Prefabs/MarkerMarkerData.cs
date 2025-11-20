using Unity.Entities;

namespace Game.Prefabs;

public struct MarkerMarkerData : IComponentData, IQueryTypeParameter
{
	public MarkerType m_MarkerType;
}
