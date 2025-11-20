using Unity.Entities;

namespace Game.UI.Editor;

public class EditorTerrainTool : EditorTool
{
	public const string kToolId = "TerrainTool";

	public EditorTerrainTool(World world)
		: base(world)
	{
		base.id = "TerrainTool";
		base.uiTag = "UITagPrefab:ModifyTerrainButton";
		base.icon = "Media/Editor/Terrain.svg";
		base.panel = world.GetOrCreateSystemManaged<TerrainPanelSystem>();
	}
}
