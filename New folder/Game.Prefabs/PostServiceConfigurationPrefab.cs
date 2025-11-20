using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class PostServiceConfigurationPrefab : PrefabBase
{
	public ServicePrefab m_PostServicePrefab;

	public int m_MaxMailAccumulation = 2000;

	public int m_MailAccumulationTolerance = 10;

	public int m_OutgoingMailPercentage = 15;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_PostServicePrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PostConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new PostConfigurationData
		{
			m_PostServicePrefab = orCreateSystemManaged.GetEntity(m_PostServicePrefab),
			m_MaxMailAccumulation = m_MaxMailAccumulation,
			m_MailAccumulationTolerance = m_MailAccumulationTolerance,
			m_OutgoingMailPercentage = m_OutgoingMailPercentage
		});
	}
}
