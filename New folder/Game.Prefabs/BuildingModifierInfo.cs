using System;
using Colossal.Mathematics;
using Game.Buildings;

namespace Game.Prefabs;

[Serializable]
public class BuildingModifierInfo
{
	public BuildingModifierType m_Type;

	public ModifierValueMode m_Mode;

	public Bounds1 m_Range;
}
