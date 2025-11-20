using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewNetGeometryData : IComponentData, IQueryTypeParameter
{
	public NetType m_Type;
}
