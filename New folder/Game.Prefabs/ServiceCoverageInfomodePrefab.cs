using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class ServiceCoverageInfomodePrefab : GradientInfomodeBasePrefab
{
	public CoverageService m_Service;

	public Bounds1 m_Range = new Bounds1(0f, 5f);

	public override string infomodeTypeLocaleKey => "NetworkColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewCoverageData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewCoverageData
		{
			m_Service = m_Service,
			m_Range = m_Range
		});
	}
}
