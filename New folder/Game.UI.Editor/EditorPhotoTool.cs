using Unity.Entities;

namespace Game.UI.Editor;

public class EditorPhotoTool : EditorTool
{
	public const string kToolId = "PhotoTool";

	public EditorPhotoTool(World world)
		: base(world)
	{
		base.id = "PhotoTool";
		base.icon = "Media/Editor/CinematicCameraOff.svg";
		base.panel = world.GetOrCreateSystemManaged<EditorPhotoModePanel>();
	}
}
