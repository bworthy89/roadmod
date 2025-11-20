using System;
using System.Collections.Generic;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Triggers/", new Type[] { })]
public class TutorialPolicyAdjustmentTriggerPrefab : TutorialTriggerPrefabBase
{
	public PolicyAdjustmentTriggerFlags m_Flags;

	public PolicyAdjustmentTriggerTargetFlags m_TargetFlags;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PolicyAdjustmentTriggerData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PolicyAdjustmentTriggerData(m_Flags, m_TargetFlags));
	}
}
