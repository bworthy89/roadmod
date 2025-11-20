using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

public struct EffectColorData : IComponentData, IQueryTypeParameter
{
	public Color m_Color;

	public EffectColorSource m_Source;

	public float3 m_VaritationRanges;
}
