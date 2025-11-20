using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewMarkerData : IComponentData, IQueryTypeParameter
{
	public MarkerType m_Type;
}
