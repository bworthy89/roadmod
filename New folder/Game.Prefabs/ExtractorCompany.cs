using System;
using System.Collections.Generic;
using Game.Companies;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Companies/", new Type[] { typeof(CompanyPrefab) })]
public class ExtractorCompany : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ExtractorCompanyData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Companies.ExtractorCompany>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
	}
}
