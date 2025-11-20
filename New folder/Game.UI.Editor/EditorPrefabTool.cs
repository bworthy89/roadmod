using Colossal.Annotations;
using Game.Prefabs;
using Unity.Entities;

namespace Game.UI.Editor;

public class EditorPrefabTool : EditorTool
{
	public const string kToolId = "PrefabTool";

	[CanBeNull]
	private PrefabBase m_LastSelectedPrefab;

	public EditorPrefabTool(World world)
		: base(world)
	{
		base.id = "PrefabTool";
		base.icon = "Media/Editor/Object.svg";
		base.panel = world.GetOrCreateSystemManaged<PrefabToolPanelSystem>();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		m_ToolSystem.ActivatePrefabTool(m_LastSelectedPrefab);
	}

	protected override void OnDisable()
	{
		m_LastSelectedPrefab = m_ToolSystem.activePrefab;
		base.OnDisable();
	}
}
