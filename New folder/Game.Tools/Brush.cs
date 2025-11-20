using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct Brush : IComponentData, IQueryTypeParameter
{
	public Entity m_Tool;

	public float3 m_Position;

	public float3 m_Target;

	public float3 m_Start;

	public float m_Angle;

	public float m_Size;

	public float m_Strength;

	public float m_Opacity;
}
