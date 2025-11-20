using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathfindPath
{
	public Entity m_Target;

	public float2 m_TargetDelta;

	public PathElementFlags m_Flags;
}
