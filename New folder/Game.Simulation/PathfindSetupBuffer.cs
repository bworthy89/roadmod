using Game.Pathfind;
using Unity.Collections;

namespace Game.Simulation;

public struct PathfindSetupBuffer : IPathfindTargetBuffer
{
	public NativeQueue<PathfindSetupTarget>.ParallelWriter m_Queue;

	public int m_SetupIndex;

	public void Enqueue(PathTarget pathTarget)
	{
		m_Queue.Enqueue(new PathfindSetupTarget
		{
			m_SetupIndex = m_SetupIndex,
			m_PathTarget = pathTarget
		});
	}
}
