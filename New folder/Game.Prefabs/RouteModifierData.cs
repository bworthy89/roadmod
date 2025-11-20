using Colossal.Mathematics;
using Game.Routes;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct RouteModifierData : IBufferElementData
{
	public RouteModifierType m_Type;

	public ModifierValueMode m_Mode;

	public Bounds1 m_Range;

	public RouteModifierData(RouteModifierType type, ModifierValueMode mode, Bounds1 range)
	{
		m_Type = type;
		m_Mode = mode;
		m_Range = range;
	}
}
