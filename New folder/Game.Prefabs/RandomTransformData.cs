using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct RandomTransformData : IComponentData, IQueryTypeParameter
{
	public Bounds3 m_AngleRange;

	public Bounds3 m_PositionRange;
}
