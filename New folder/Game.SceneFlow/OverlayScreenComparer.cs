using System.Collections.Generic;

namespace Game.SceneFlow;

internal class OverlayScreenComparer : IComparer<OverlayScreen>
{
	public int Compare(OverlayScreen x, OverlayScreen y)
	{
		int num = (int)x;
		return num.CompareTo((int)y);
	}
}
