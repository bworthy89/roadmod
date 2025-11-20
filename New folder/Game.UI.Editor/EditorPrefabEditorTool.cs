using Unity.Entities;

namespace Game.UI.Editor;

public class EditorPrefabEditorTool : EditorTool
{
	public const string kToolId = "PrefabEditorTool";

	public EditorPrefabEditorTool(World world)
		: base(world)
	{
		base.id = "PrefabEditorTool";
		base.icon = "Media/Editor/EditPrefab.svg";
		base.panel = world.GetOrCreateSystemManaged<PrefabEdítorPanelSystem>();
	}
}
