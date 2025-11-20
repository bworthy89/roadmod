using System;

namespace Game.Prefabs;

[Serializable]
public class NetLaneMeshInfo
{
	public RenderPrefab m_Mesh;

	public bool m_RequireSafe;

	public bool m_RequireLevelCrossing;

	public bool m_RequireEditor;

	public bool m_RequireTrackCrossing;

	public bool m_RequireClear;

	public bool m_RequireLeftHandTraffic;

	public bool m_RequireRightHandTraffic;
}
