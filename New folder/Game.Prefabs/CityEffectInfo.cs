using System;
using Game.City;

namespace Game.Prefabs;

[Serializable]
public class CityEffectInfo
{
	public CityModifierType m_Type;

	public ModifierValueMode m_Mode;

	public float m_Delta;
}
