using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewObjectStatusData : IComponentData, IQueryTypeParameter
{
	public ObjectStatusType m_Type;

	public Bounds1 m_Range;
}
