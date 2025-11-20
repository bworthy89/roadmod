using Game.Tools;
using Unity.Entities;

namespace Game.UI.Editor;

public class EditorBulldozeTool : EditorTool
{
	public const string kToolId = "BulldozeTool";

	public EditorBulldozeTool(World world)
		: base(world)
	{
		base.id = "BulldozeTool";
		base.icon = "Media/Editor/Bulldozer.svg";
		base.tool = world.GetOrCreateSystemManaged<BulldozeToolSystem>();
		base.panel = world.GetOrCreateSystemManaged<BulldozeToolPanel>();
		base.shortcut = "Bulldozer";
	}
}
