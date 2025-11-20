using System;

namespace Game.Prefabs;

[ComponentMenu("Pathfind/", new Type[] { })]
public class PathfindPrefab : PrefabBase
{
	public bool m_TrackTrafficFlow = true;
}
