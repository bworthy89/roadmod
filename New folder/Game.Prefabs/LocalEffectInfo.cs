using System;
using Game.Buildings;

namespace Game.Prefabs;

[Serializable]
public class LocalEffectInfo
{
	public LocalModifierType m_Type;

	public ModifierValueMode m_Mode;

	public float m_Delta;

	public ModifierRadiusCombineMode m_RadiusCombineMode;

	public float m_Radius;
}
