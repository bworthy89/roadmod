using Colossal.PSI.Steamworks;
using UnityEngine.Scripting;

namespace Game.Dlc;

[Preserve]
public class SteamworksDlcsMapping : SteamworksDlcMapper
{
	private const uint kProjectWashingtonId = 2427731u;

	private const uint kProjectCaliforniaId = 2427730u;

	private const uint kExpansionPass = 2472660u;

	private const uint kProjectFloridaId = 2427740u;

	private const uint kProjectNewJerseyId = 2427743u;

	public SteamworksDlcsMapping()
	{
		Map(Dlc.LandmarkBuildings, 2427731u);
		Map(Dlc.SanFranciscoSet, 2427730u);
		Map(Dlc.BridgesAndPorts, 2427743u);
	}
}
