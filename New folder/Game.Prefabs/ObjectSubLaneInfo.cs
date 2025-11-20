using System;
using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class ObjectSubLaneInfo
{
	public NetLanePrefab m_LanePrefab;

	public Bezier4x3 m_BezierCurve;

	public int2 m_NodeIndex = new int2(-1, -1);

	public int2 m_ParentMesh = new int2(-1, -1);
}
