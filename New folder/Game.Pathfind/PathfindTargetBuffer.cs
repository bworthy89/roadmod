using Unity.Collections;

namespace Game.Pathfind;

public struct PathfindTargetBuffer : IPathfindTargetBuffer
{
	private UnsafeQueue<PathTarget>.ParallelWriter m_Queue;

	public void Enqueue(PathTarget pathTarget)
	{
		m_Queue.Enqueue(pathTarget);
	}

	public static implicit operator PathfindTargetBuffer(UnsafeQueue<PathTarget>.ParallelWriter queue)
	{
		return new PathfindTargetBuffer
		{
			m_Queue = queue
		};
	}
}
