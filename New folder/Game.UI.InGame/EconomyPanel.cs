namespace Game.UI.InGame;

public class EconomyPanel : TabbedGamePanel
{
	public enum Tab
	{
		Budget,
		Loan,
		Taxation,
		Services,
		Production
	}

	public override bool blocking => true;

	public override LayoutPosition position => LayoutPosition.Center;
}
