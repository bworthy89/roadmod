using Colossal.PSI.PdxSdk;
using UnityEngine.Scripting;

namespace Game.Dlc;

[Preserve]
public class PdxSdkDlcsMapping : PdxSdkDlcMapper
{
	private static readonly string[] kCS1TreasureHuntIds = new string[8] { "BusCO01", "BusCO02", "BusCOMirrored01", "BusCOMirrored02", "TramCarCO01", "TramEngineCO01", "AirplanePassengerCO01", "FountainPlaza01" };

	public PdxSdkDlcsMapping()
	{
		Map(Dlc.CS1TreasureHunt, kCS1TreasureHuntIds);
	}
}
