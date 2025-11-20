using Colossal.PSI.Common;

namespace Game.Dlc;

[DlcDescription]
public static class Dlc
{
	public const int BridgesAndPortsId = 5;

	[Dlc(0)]
	public static readonly DlcId LandmarkBuildings;

	[Dlc(1)]
	public static readonly DlcId SanFranciscoSet;

	[Dlc(2)]
	public static readonly DlcId CS1TreasureHunt;

	[Dlc(5)]
	public static readonly DlcId BridgesAndPorts;
}
