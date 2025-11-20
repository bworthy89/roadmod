using UnityEngine.Scripting;

namespace Game.Common;

public class ModificationSystem : GameSystemBase
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
		m_UpdateSystem.Update(SystemUpdatePhase.Modification1);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification2);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification2B);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification3);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification4);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification4B);
		m_UpdateSystem.Update(SystemUpdatePhase.Modification5);
		m_UpdateSystem.Update(SystemUpdatePhase.ModificationEnd);
	}

	[Preserve]
	public ModificationSystem()
	{
	}
}
