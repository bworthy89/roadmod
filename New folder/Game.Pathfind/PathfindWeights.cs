using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathfindWeights
{
	public float4 m_Value;

	public float time => m_Value.x;

	public float money => m_Value.z;

	public PathfindWeights(float time, float behaviour, float money, float comfort)
	{
		m_Value = new float4(time, behaviour, money, comfort);
	}

	public static PathfindWeights operator *(float x, PathfindWeights w)
	{
		return new PathfindWeights
		{
			m_Value = x * w.m_Value
		};
	}
}
