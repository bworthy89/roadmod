using System;
using Colossal.Mathematics;
using Game.Areas;

namespace Game.Prefabs;

[Serializable]
public class DistrictModifierInfo
{
	public DistrictModifierType m_Type;

	public ModifierValueMode m_Mode;

	public Bounds1 m_Range;
}
