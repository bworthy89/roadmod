using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewNetStatusData : IComponentData, IQueryTypeParameter
{
	public NetStatusType m_Type;

	public Bounds1 m_Range;

	public float m_Tiling;
}
