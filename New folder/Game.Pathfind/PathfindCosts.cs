using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathfindCosts
{
	public float4 m_Value;

	public PathfindCosts(float time, float behaviour, float money, float comfort)
	{
		m_Value = new float4(time, behaviour, money, comfort);
	}
}
