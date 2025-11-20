using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class NetSubObjectInfo
{
	public ObjectPrefab m_Object;

	public float3 m_Position;

	public quaternion m_Rotation;

	public NetObjectPlacement m_Placement;

	public int m_FixedIndex;

	public float m_Spacing;

	public bool m_AnchorTop;

	public bool m_AnchorCenter;

	public bool m_RequireElevated;

	public bool m_RequireOutsideConnection;

	public bool m_RequireDeadEnd;

	public bool m_RequireOrphan;
}
