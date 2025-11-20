using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class RenterObject : ComponentBase
{
	public bool m_RequireEmpty;

	public bool m_RequireRenter;

	public bool m_RequireGoodWealth;

	public bool m_RequireDogs;

	public bool m_RequireHomeless;

	public bool m_RequireChildren;

	public bool m_RequireTeens;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ObjectRequirementElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<ObjectRequirementElement> buffer = entityManager.GetBuffer<ObjectRequirementElement>(entity);
		int length = buffer.Length;
		ObjectRequirementFlags objectRequirementFlags = (ObjectRequirementFlags)0;
		ObjectRequirementFlags objectRequirementFlags2 = (ObjectRequirementFlags)0;
		if (m_RequireEmpty)
		{
			objectRequirementFlags2 |= ObjectRequirementFlags.Renter;
		}
		if (m_RequireRenter)
		{
			objectRequirementFlags |= ObjectRequirementFlags.Renter;
		}
		if (m_RequireGoodWealth)
		{
			objectRequirementFlags |= ObjectRequirementFlags.GoodWealth;
		}
		if (m_RequireDogs)
		{
			objectRequirementFlags |= ObjectRequirementFlags.Dogs;
		}
		if (m_RequireHomeless)
		{
			objectRequirementFlags |= ObjectRequirementFlags.Homeless;
		}
		if (!m_RequireChildren && !m_RequireTeens)
		{
			buffer.Add(new ObjectRequirementElement(objectRequirementFlags, objectRequirementFlags2, length));
		}
		if (m_RequireChildren)
		{
			buffer.Add(new ObjectRequirementElement(objectRequirementFlags | ObjectRequirementFlags.Children, objectRequirementFlags2, length));
		}
		if (m_RequireTeens)
		{
			buffer.Add(new ObjectRequirementElement(objectRequirementFlags | ObjectRequirementFlags.Teens, objectRequirementFlags2, length));
		}
	}
}
