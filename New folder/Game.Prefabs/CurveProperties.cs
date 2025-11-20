using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Serialization;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class CurveProperties : ComponentBase
{
	public int m_TilingCount;

	public float m_OverrideLength;

	public float m_SmoothingDistance;

	public bool m_GeometryTiling;

	public bool m_StraightTiling;

	public bool m_InvertCurve;

	public bool m_SubFlow;

	[FormerlySerializedAs("m_Hanging")]
	public bool m_HangingSwaying;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}
}
