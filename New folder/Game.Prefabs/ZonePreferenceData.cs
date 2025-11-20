using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ZonePreferenceData : IComponentData, IQueryTypeParameter
{
	public float m_ResidentialSignificanceServices;

	public float m_ResidentialSignificanceWorkplaces;

	public float m_ResidentialSignificanceLandValue;

	public float3 m_ResidentialSignificancePollution;

	public float m_ResidentialNeutralLandValue;

	public float m_CommercialSignificanceConsumers;

	public float m_CommercialSignificanceCompetitors;

	public float m_CommercialSignificanceWorkplaces;

	public float m_CommercialSignificanceLandValue;

	public float m_CommercialNeutralLandValue;

	public float m_IndustrialSignificanceInput;

	public float m_IndustrialSignificanceOutside;

	public float m_IndustrialSignificanceLandValue;

	public float m_IndustrialNeutralLandValue;

	public float m_OfficeSignificanceEmployees;

	public float m_OfficeSignificanceServices;
}
