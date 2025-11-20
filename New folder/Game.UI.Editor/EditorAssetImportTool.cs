using Unity.Entities;

namespace Game.UI.Editor;

public class EditorAssetImportTool : EditorTool
{
	public const string kToolId = "AssetImportTool";

	public EditorAssetImportTool(World world)
		: base(world)
	{
		base.id = "AssetImportTool";
		base.uiTag = "UITagPrefab:AssetImportButton";
		base.icon = "Media/Editor/AssetImport.svg";
		base.panel = world.GetOrCreateSystemManaged<AssetImportPanel>();
	}
}
