using Unity.Entities;

namespace Game.Buildings;

public struct BackSide : IComponentData, IQueryTypeParameter
{
	public Entity m_RoadEdge;

	public float m_CurvePosition;
}
