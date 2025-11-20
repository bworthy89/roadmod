using System;

namespace Game.Prefabs;

[Serializable]
public class SecondaryLaneInfo2
{
	public NetLanePrefab m_Lane;

	public bool m_RequireStop;

	public bool m_RequireYield;

	public bool m_RequirePavement;

	public bool m_RequireContinue;

	public SecondaryNetLaneFlags GetFlags()
	{
		SecondaryNetLaneFlags secondaryNetLaneFlags = (SecondaryNetLaneFlags)0;
		if (m_RequireStop)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireStop;
		}
		if (m_RequireYield)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireYield;
		}
		if (m_RequirePavement)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequirePavement;
		}
		if (m_RequireContinue)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireContinue;
		}
		return secondaryNetLaneFlags;
	}
}
