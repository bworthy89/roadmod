using Game.Prefabs;

namespace Game.UI.InGame;

public static class PollutionUIUtils
{
	public static PollutionThreshold GetPollutionKey(UIPollutionThresholds data, float pollution)
	{
		if (pollution > (float)data.m_High)
		{
			return PollutionThreshold.High;
		}
		if (pollution > (float)data.m_Medium)
		{
			return PollutionThreshold.Medium;
		}
		if (!(pollution > (float)data.m_Low))
		{
			return PollutionThreshold.None;
		}
		return PollutionThreshold.Low;
	}
}
