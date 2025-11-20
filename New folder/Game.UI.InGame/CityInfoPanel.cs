namespace Game.UI.InGame;

public class CityInfoPanel : TabbedGamePanel
{
	public enum Tab
	{
		Demand,
		CityPolicies
	}

	public override bool blocking => true;

	public override LayoutPosition position => LayoutPosition.Center;
}
