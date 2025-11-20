using System;
using System.Collections.Generic;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class ZoneSuitabilityInfomodePrefab : GradientInfomodeBasePrefab
{
	public AreaType m_AreaType;

	public bool m_Office;

	public override string infomodeTypeLocaleKey => "NetworkColor";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewAvailabilityData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewAvailabilityData
		{
			m_AreaType = m_AreaType,
			m_Office = m_Office
		});
	}
}
