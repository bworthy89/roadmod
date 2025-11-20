using UnityEngine.Scripting;

namespace Game.Rendering;

public class PreRenderSystem : GameSystemBase
{
	private RenderingSystem m_RenderingSystem;

	private UpdateSystem m_UpdateSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_RenderingSystem.PrepareRendering();
		m_UpdateSystem.Update(SystemUpdatePhase.PreCulling);
	}

	[Preserve]
	public PreRenderSystem()
	{
	}
}
