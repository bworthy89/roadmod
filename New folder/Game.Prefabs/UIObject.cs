using System;
using System.Collections.Generic;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UIObject : ComponentBase
{
	public UIGroupPrefab m_Group;

	public int m_Priority;

	[CustomField(typeof(UIIconField))]
	public string m_Icon;

	public bool m_IsDebugObject;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			UIGroupPrefab uIGroupPrefab = m_Group;
			if (uIGroupPrefab is UIAssetCategoryPrefab category)
			{
				yield return "UI" + category.name;
				if (category.name.StartsWith("Props"))
				{
					yield return "UIProps";
				}
				if (category.m_Menu != null)
				{
					yield return "UI" + category.m_Menu.name;
				}
			}
		}
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Group != null)
		{
			prefabs.Add(m_Group);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if (!m_IsDebugObject || UnityEngine.Debug.isDebugBuild)
		{
			components.Add(ComponentType.ReadWrite<UIObjectData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (!m_IsDebugObject || UnityEngine.Debug.isDebugBuild)
		{
			Entity entity2 = Entity.Null;
			if (m_Group != null)
			{
				entity2 = entityManager.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(m_Group);
				m_Group.AddElement(entityManager, entity);
			}
			entityManager.SetComponentData(entity, new UIObjectData
			{
				m_Group = entity2,
				m_Priority = m_Priority
			});
		}
	}
}
