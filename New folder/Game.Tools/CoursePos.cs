using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct CoursePos
{
	public Entity m_Entity;

	public float3 m_Position;

	public quaternion m_Rotation;

	public float2 m_Elevation;

	public float m_CourseDelta;

	public float m_SplitPosition;

	public CoursePosFlags m_Flags;

	public int m_ParentMesh;
}
