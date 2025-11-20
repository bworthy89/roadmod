using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct CitizenParametersData : IComponentData, IQueryTypeParameter
{
	public float m_DivorceRate;

	public float m_LookForPartnerRate;

	public float2 m_LookForPartnerTypeRate;

	public float m_BaseBirthRate;

	public float m_AdultFemaleBirthRateBonus;

	public float m_StudentBirthRateAdjust;

	public float m_SwitchJobRate;

	public float m_LookForNewJobEmployableRate;
}
