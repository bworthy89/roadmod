using System;
using Colossal.Mathematics;
using Game.Routes;

namespace Game.Prefabs;

[Serializable]
public class RouteModifierInfo
{
	public RouteModifierType m_Type;

	public ModifierValueMode m_Mode;

	public Bounds1 m_Range;
}
