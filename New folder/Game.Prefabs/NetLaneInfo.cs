using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class NetLaneInfo
{
	public NetLanePrefab m_Lane;

	public float3 m_Position;

	public bool m_FindAnchor;
}
