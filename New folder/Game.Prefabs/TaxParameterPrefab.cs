using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class TaxParameterPrefab : PrefabBase
{
	public int2 m_TotalTaxLimits;

	public int2 m_ResidentialTaxLimits;

	public int2 m_CommercialTaxLimits;

	public int2 m_IndustrialTaxLimits;

	public int2 m_OfficeTaxLimits;

	public int2 m_JobLevelTaxLimits;

	public int2 m_ResourceTaxLimits;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TaxParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new TaxParameterData
		{
			m_TotalTaxLimits = m_TotalTaxLimits,
			m_ResidentialTaxLimits = m_ResidentialTaxLimits,
			m_CommercialTaxLimits = m_CommercialTaxLimits,
			m_IndustrialTaxLimits = m_IndustrialTaxLimits,
			m_OfficeTaxLimits = m_OfficeTaxLimits,
			m_JobLevelTaxLimits = m_JobLevelTaxLimits,
			m_ResourceTaxLimits = m_ResourceTaxLimits
		});
	}
}
