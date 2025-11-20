using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Net;

public struct LaneObjectUpdater
{
	[BurstCompile]
	private struct UpdateLaneObjectsJob : IJobParallelFor
	{
		public NativeParallelQueue<LaneObjectAction>.Reader m_LaneActions;

		[NativeDisableParallelForRestriction]
		public BufferLookup<LaneObject> m_LaneObjects;

		public void Execute(int index)
		{
			NativeParallelQueue<LaneObjectAction>.Enumerator enumerator = m_LaneActions.GetEnumerator(index);
			while (enumerator.MoveNext())
			{
				LaneObjectAction current = enumerator.Current;
				if (!m_LaneObjects.TryGetBuffer(current.m_Lane, out var bufferData))
				{
					continue;
				}
				if (current.m_Add == current.m_Remove)
				{
					if (current.m_Add != Entity.Null)
					{
						NetUtils.UpdateLaneObject(bufferData, current.m_Add, current.m_CurvePosition);
					}
					continue;
				}
				if (current.m_Remove != Entity.Null)
				{
					NetUtils.RemoveLaneObject(bufferData, current.m_Remove);
				}
				if (current.m_Add != Entity.Null)
				{
					NetUtils.AddLaneObject(bufferData, current.m_Add, current.m_CurvePosition);
				}
			}
			enumerator.Dispose();
		}
	}

	[BurstCompile]
	private struct UpdateTreeObjectsJob : IJob
	{
		public NativeQueue<TreeObjectAction> m_TreeActions;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public void Execute()
		{
			TreeObjectAction item;
			while (m_TreeActions.TryDequeue(out item))
			{
				if (item.m_Add == item.m_Remove)
				{
					if (item.m_Add != Entity.Null)
					{
						m_SearchTree.Update(item.m_Add, new QuadTreeBoundsXZ(item.m_Bounds));
					}
					continue;
				}
				if (item.m_Remove != Entity.Null)
				{
					m_SearchTree.TryRemove(item.m_Remove);
				}
				if (item.m_Add != Entity.Null && !m_SearchTree.TryAdd(item.m_Add, new QuadTreeBoundsXZ(item.m_Bounds)))
				{
					float3 @float = MathUtils.Center(item.m_Bounds);
					UnityEngine.Debug.Log($"Entity already added to search tree ({item.m_Add.Index}: {@float.x}, {@float.y}, {@float.z})");
				}
			}
		}
	}

	private Game.Objects.SearchSystem m_SearchSystem;

	private BufferLookup<LaneObject> m_LaneObjects;

	private NativeParallelQueue<LaneObjectAction> m_LaneActionQueue;

	private NativeQueue<TreeObjectAction> m_TreeActionQueue;

	public LaneObjectUpdater(SystemBase system)
	{
		m_SearchSystem = system.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_LaneObjects = system.GetBufferLookup<LaneObject>();
		m_LaneActionQueue = default(NativeParallelQueue<LaneObjectAction>);
		m_TreeActionQueue = default(NativeQueue<TreeObjectAction>);
	}

	public LaneObjectCommandBuffer Begin(Allocator allocator)
	{
		m_LaneActionQueue = new NativeParallelQueue<LaneObjectAction>(allocator);
		m_TreeActionQueue = new NativeQueue<TreeObjectAction>(allocator);
		return new LaneObjectCommandBuffer(m_LaneActionQueue.AsWriter(), m_TreeActionQueue.AsParallelWriter());
	}

	public JobHandle Apply(SystemBase system, JobHandle dependencies)
	{
		m_LaneObjects.Update(system);
		UpdateLaneObjectsJob jobData = new UpdateLaneObjectsJob
		{
			m_LaneActions = m_LaneActionQueue.AsReader(),
			m_LaneObjects = m_LaneObjects
		};
		JobHandle dependencies2;
		UpdateTreeObjectsJob jobData2 = new UpdateTreeObjectsJob
		{
			m_TreeActions = m_TreeActionQueue,
			m_SearchTree = m_SearchSystem.GetMovingSearchTree(readOnly: false, out dependencies2)
		};
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, m_LaneActionQueue.HashRange, 1, dependencies);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(dependencies, dependencies2));
		m_LaneActionQueue.Dispose(jobHandle);
		m_TreeActionQueue.Dispose(jobHandle2);
		m_SearchSystem.AddMovingSearchTreeWriter(jobHandle2);
		return jobHandle;
	}
}
