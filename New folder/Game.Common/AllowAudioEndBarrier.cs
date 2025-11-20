using UnityEngine.Scripting;

namespace Game.Common;

public class AllowAudioEndBarrier : GameSystemBase
{
	private AudioEndBarrier m_AudioEndBarrier;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AudioEndBarrier = base.World.GetOrCreateSystemManaged<AudioEndBarrier>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_AudioEndBarrier.AllowUsage();
	}

	[Preserve]
	public AllowAudioEndBarrier()
	{
	}
}
