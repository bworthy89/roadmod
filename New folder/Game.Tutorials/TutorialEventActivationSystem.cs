using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialEventActivationSystem : GameSystemBase
{
	protected EntityCommandBufferSystem m_BarrierSystem;

	private NativeQueue<Entity> m_ActivationQueue;

	private JobHandle m_InputDependencies;

	public NativeQueue<Entity> GetQueue(out JobHandle dependency)
	{
		dependency = m_InputDependencies;
		return m_ActivationQueue;
	}

	public void AddQueueWriter(JobHandle dependency)
	{
		m_InputDependencies = JobHandle.CombineDependencies(m_InputDependencies, dependency);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_ActivationQueue = new NativeQueue<Entity>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ActivationQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_InputDependencies.Complete();
		EntityCommandBuffer entityCommandBuffer = m_BarrierSystem.CreateCommandBuffer();
		Entity item;
		while (m_ActivationQueue.TryDequeue(out item))
		{
			entityCommandBuffer.AddComponent<TutorialActivated>(item);
		}
	}

	[Preserve]
	public TutorialEventActivationSystem()
	{
	}
}
