using UnityEngine.Scripting;

namespace Game;

public class AllowBarrier<T> : GameSystemBase where T : SafeCommandBufferSystem
{
	private T m_Barrier;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Barrier = base.World.GetOrCreateSystemManaged<T>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_Barrier.AllowUsage();
	}

	[Preserve]
	public AllowBarrier()
	{
	}
}
