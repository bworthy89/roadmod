namespace Game.UI.InGame;

public class TransportationOverviewPanel : TabbedGamePanel
{
	public enum Tab
	{
		PublicTransport,
		Cargo
	}

	public override bool blocking => true;

	public override LayoutPosition position => LayoutPosition.Center;
}
