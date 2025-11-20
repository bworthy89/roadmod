using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct PolicySliderData : IComponentData, IQueryTypeParameter
{
	public Bounds1 m_Range;

	public float m_Default;

	public float m_Step;

	public int m_Unit;
}
