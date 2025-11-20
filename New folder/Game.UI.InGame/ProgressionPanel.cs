namespace Game.UI.InGame;

public class ProgressionPanel : TabbedGamePanel
{
	public enum Tab
	{
		Development,
		Milestones,
		Achievements
	}

	public override bool blocking => true;

	public override LayoutPosition position => LayoutPosition.Center;

	public override bool retainProperties => true;
}
