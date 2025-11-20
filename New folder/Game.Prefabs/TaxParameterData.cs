using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct TaxParameterData : IComponentData, IQueryTypeParameter, IEquatable<TaxParameterData>
{
	public int2 m_TotalTaxLimits;

	public int2 m_ResidentialTaxLimits;

	public int2 m_CommercialTaxLimits;

	public int2 m_IndustrialTaxLimits;

	public int2 m_OfficeTaxLimits;

	public int2 m_JobLevelTaxLimits;

	public int2 m_ResourceTaxLimits;

	public bool Equals(TaxParameterData other)
	{
		if (m_CommercialTaxLimits.Equals(other.m_CommercialTaxLimits) && m_IndustrialTaxLimits.Equals(other.m_IndustrialTaxLimits) && m_JobLevelTaxLimits.Equals(other.m_JobLevelTaxLimits) && m_OfficeTaxLimits.Equals(other.m_OfficeTaxLimits) && m_ResidentialTaxLimits.Equals(other.m_ResidentialTaxLimits) && m_ResourceTaxLimits.Equals(other.m_ResourceTaxLimits))
		{
			return m_TotalTaxLimits.Equals(other.m_TotalTaxLimits);
		}
		return false;
	}
}
