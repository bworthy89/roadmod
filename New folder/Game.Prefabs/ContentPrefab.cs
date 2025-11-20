using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Content/", new Type[] { })]
public class ContentPrefab : PrefabBase
{
	public bool IsAvailable()
	{
		foreach (ComponentBase component in components)
		{
			if (component is ContentRequirementBase contentRequirementBase && !contentRequirementBase.CheckRequirement())
			{
				return false;
			}
		}
		return true;
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ContentData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ContentData componentData = entityManager.GetComponentData<ContentData>(entity);
		if (TryGet<DlcRequirement>(out var component))
		{
			componentData.m_Flags |= ContentFlags.RequireDlc;
			componentData.m_DlcID = component.m_Dlc.id;
		}
		if (Has<PdxLoginRequirement>())
		{
			componentData.m_Flags |= ContentFlags.RequirePdxLogin;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
