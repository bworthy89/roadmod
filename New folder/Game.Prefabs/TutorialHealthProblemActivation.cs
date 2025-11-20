using System;
using System.Collections.Generic;
using Game.Citizens;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Activation/", new Type[] { typeof(TutorialPrefab) })]
public class TutorialHealthProblemActivation : TutorialActivation
{
	public HealthProblemFlags m_Flags;

	public int m_RequiredCount = 10;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadOnly<HealthProblemActivationData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new HealthProblemActivationData
		{
			m_Require = m_Flags,
			m_RequiredCount = m_RequiredCount
		});
	}
}
