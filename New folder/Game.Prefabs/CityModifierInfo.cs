using System;
using Colossal.Mathematics;
using Game.City;

namespace Game.Prefabs;

[Serializable]
public class CityModifierInfo
{
	public CityModifierType m_Type;

	public ModifierValueMode m_Mode;

	public Bounds1 m_Range;
}
