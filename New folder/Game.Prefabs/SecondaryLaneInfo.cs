using System;

namespace Game.Prefabs;

[Serializable]
public class SecondaryLaneInfo
{
	public NetLanePrefab m_Lane;

	public bool m_RequireSafe;

	public bool m_RequireUnsafe;

	public bool m_RequireSingle;

	public bool m_RequireMultiple;

	public bool m_RequireAllowPassing;

	public bool m_RequireForbidPassing;

	public bool m_RequireMerge;

	public bool m_RequireContinue;

	public bool m_RequireSafeMaster;

	public bool m_RequireRoundabout;

	public bool m_RequireNotRoundabout;

	public SecondaryNetLaneFlags GetFlags()
	{
		SecondaryNetLaneFlags secondaryNetLaneFlags = (SecondaryNetLaneFlags)0;
		if (m_RequireSafe)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireSafe;
		}
		if (m_RequireUnsafe)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireUnsafe;
		}
		if (m_RequireSingle)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireSingle;
		}
		if (m_RequireMultiple)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireMultiple;
		}
		if (m_RequireAllowPassing)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireAllowPassing;
		}
		if (m_RequireForbidPassing)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireForbidPassing;
		}
		if (m_RequireMerge)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireMerge;
		}
		if (m_RequireContinue)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireContinue;
		}
		if (m_RequireSafeMaster)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireSafeMaster;
		}
		if (m_RequireRoundabout)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireRoundabout;
		}
		if (m_RequireNotRoundabout)
		{
			secondaryNetLaneFlags |= SecondaryNetLaneFlags.RequireNotRoundabout;
		}
		return secondaryNetLaneFlags;
	}
}
