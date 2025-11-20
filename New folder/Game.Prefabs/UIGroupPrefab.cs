using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public abstract class UIGroupPrefab : PrefabBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UIGroupElement>());
		components.Add(ComponentType.ReadWrite<UnlockRequirement>());
		components.Add(ComponentType.ReadWrite<Locked>());
	}

	public void AddElement(EntityManager entityManager, Entity entity)
	{
		Entity entity2 = entityManager.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(this);
		entityManager.GetBuffer<UIGroupElement>(entity2).Add(new UIGroupElement(entity));
		entityManager.GetBuffer<UnlockRequirement>(entity2).Add(new UnlockRequirement(entity, UnlockFlags.RequireAny));
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAny));
		base.LateInitialize(entityManager, entity);
	}
}
