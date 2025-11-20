using UnityEngine.Scripting;

namespace Game.UI;

public class UIUpdateSystem : GameSystemBase
{
	private UpdateSystem m_UpdateSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_UpdateSystem.Update(SystemUpdatePhase.UIUpdate);
	}

	[Preserve]
	public UIUpdateSystem()
	{
	}
}
