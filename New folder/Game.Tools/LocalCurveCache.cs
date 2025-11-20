using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Tools;

public struct LocalCurveCache : IComponentData, IQueryTypeParameter
{
	public Bezier4x3 m_Curve;
}
