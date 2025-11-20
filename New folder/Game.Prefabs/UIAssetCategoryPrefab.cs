using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UIAssetCategoryPrefab : UIGroupPrefab
{
	public UIAssetMenuPrefab m_Menu;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		if (m_Menu != null)
		{
			components.Add(ComponentType.ReadWrite<UIAssetCategoryData>());
		}
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Menu != null)
		{
			prefabs.Add(m_Menu);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Menu != null)
		{
			Entity entity2 = entityManager.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(m_Menu);
			entityManager.SetComponentData(entity, new UIAssetCategoryData(entity2));
			m_Menu.AddElement(entityManager, entity);
		}
	}
}
