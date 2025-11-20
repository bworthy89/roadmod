using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct CoverageResult
{
	public Entity m_Target;

	public float2 m_TargetCost;
}
