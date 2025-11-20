using System;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class ObjectSubAreaInfo
{
	public AreaPrefab m_AreaPrefab;

	[InputField]
	[RangeN(-10000f, 10000f, true)]
	public float3[] m_NodePositions;

	public int[] m_ParentMeshes;
}
