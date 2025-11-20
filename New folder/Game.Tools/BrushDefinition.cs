using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct BrushDefinition : IComponentData, IQueryTypeParameter
{
	public Entity m_Tool;

	public Line3.Segment m_Line;

	public float m_Angle;

	public float m_Size;

	public float m_Strength;

	public float m_Time;

	public float3 m_Target;

	public float3 m_Start;
}
