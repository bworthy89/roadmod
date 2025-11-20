using Game.Buildings;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct InfoviewLocalEffectData : IComponentData, IQueryTypeParameter
{
	public LocalModifierType m_Type;

	public float4 m_Color;
}
