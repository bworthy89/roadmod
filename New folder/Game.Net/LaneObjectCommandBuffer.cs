using Colossal.Collections;
using Colossal.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct LaneObjectCommandBuffer
{
	private NativeParallelQueue<LaneObjectAction>.Writer m_LaneActionQueue;

	private NativeQueue<TreeObjectAction>.ParallelWriter m_TreeActionQueue;

	public LaneObjectCommandBuffer(NativeParallelQueue<LaneObjectAction>.Writer laneActionQueue, NativeQueue<TreeObjectAction>.ParallelWriter treeActionQueue)
	{
		m_LaneActionQueue = laneActionQueue;
		m_TreeActionQueue = treeActionQueue;
	}

	public void Remove(Entity lane, Entity entity)
	{
		m_LaneActionQueue.Enqueue(new LaneObjectAction(lane, entity));
	}

	public void Remove(Entity entity)
	{
		m_TreeActionQueue.Enqueue(new TreeObjectAction(entity));
	}

	public void Add(Entity lane, Entity entity, float2 curvePosition)
	{
		m_LaneActionQueue.Enqueue(new LaneObjectAction(lane, entity, curvePosition));
	}

	public void Add(Entity entity, Bounds3 bounds)
	{
		m_TreeActionQueue.Enqueue(new TreeObjectAction(entity, bounds));
	}

	public void Update(Entity lane, Entity entity, float2 curvePosition)
	{
		m_LaneActionQueue.Enqueue(new LaneObjectAction(lane, entity, entity, curvePosition));
	}

	public void Update(Entity entity, Bounds3 bounds)
	{
		m_TreeActionQueue.Enqueue(new TreeObjectAction(entity, entity, bounds));
	}
}
