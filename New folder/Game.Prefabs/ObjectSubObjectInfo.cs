using System;
using Game.UI.Widgets;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[Serializable]
public class ObjectSubObjectInfo
{
	public ObjectPrefab m_Object;

	[InputField]
	[RangeN(-10000f, 10000f, true)]
	public float3 m_Position;

	public quaternion m_Rotation;

	public int m_ParentMesh;

	public int m_GroupIndex;

	[Range(0f, 100f)]
	public int m_Probability = 100;
}
