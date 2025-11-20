using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct WaterSourceDefinition : IComponentData, IQueryTypeParameter
{
	public float3 m_Position;

	public int m_ConstantDepth;

	public float m_Radius;

	public float m_Multiplier;

	public float m_Polluted;

	public float m_Height;

	public int m_SourceId;

	public int m_SourceNameId;
}
