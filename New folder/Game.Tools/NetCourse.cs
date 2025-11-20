using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct NetCourse : IComponentData, IQueryTypeParameter
{
	public CoursePos m_StartPosition;

	public CoursePos m_EndPosition;

	public Bezier4x3 m_Curve;

	public float2 m_Elevation;

	public float m_Length;

	public int m_FixedIndex;
}
