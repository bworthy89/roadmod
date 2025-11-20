using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Triggers/", new Type[] { })]
public class TutorialObjectPlacementTriggerPrefab : TutorialTriggerPrefabBase
{
	[Serializable]
	public class ObjectPlacementTarget
	{
		[NotNull]
		public PrefabBase m_Target;

		public ObjectPlacementTriggerFlags m_Flags;
	}

	[NotNull]
	public ObjectPlacementTarget[] m_Targets;

	public int m_RequiredCount;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		ObjectPlacementTarget[] targets = m_Targets;
		foreach (ObjectPlacementTarget objectPlacementTarget in targets)
		{
			prefabs.Add(objectPlacementTarget.m_Target);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectPlacementTriggerData>());
		components.Add(ComponentType.ReadWrite<ObjectPlacementTriggerCountData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<ObjectPlacementTriggerData> buffer = entityManager.GetBuffer<ObjectPlacementTriggerData>(entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		ObjectPlacementTarget[] targets = m_Targets;
		foreach (ObjectPlacementTarget objectPlacementTarget in targets)
		{
			if (existingSystemManaged.TryGetEntity(objectPlacementTarget.m_Target, out var entity2))
			{
				buffer.Add(new ObjectPlacementTriggerData(entity2, objectPlacementTarget.m_Flags));
			}
		}
		entityManager.SetComponentData(entity, new ObjectPlacementTriggerCountData(math.max(m_RequiredCount, 1)));
	}

	protected override void GenerateBlinkTags()
	{
		base.GenerateBlinkTags();
		ObjectPlacementTarget[] targets = m_Targets;
		foreach (ObjectPlacementTarget objectPlacementTarget in targets)
		{
			if (objectPlacementTarget.m_Target.TryGet<UIObject>(out var component))
			{
				if (component.m_Group is UIAssetCategoryPrefab uIAssetCategoryPrefab && uIAssetCategoryPrefab.m_Menu != null)
				{
					AddBlinkTagAtPosition(objectPlacementTarget.m_Target.uiTag, 0);
					AddBlinkTagAtPosition(uIAssetCategoryPrefab.uiTag, 1);
					AddBlinkTagAtPosition(uIAssetCategoryPrefab.m_Menu.uiTag, 2);
				}
				else if (objectPlacementTarget.m_Target.Has(typeof(ServiceUpgrade)))
				{
					AddBlinkTagAtPosition(objectPlacementTarget.m_Target.uiTag, 0);
				}
			}
		}
	}

	public override void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		base.GenerateTutorialLinks(entityManager, linkedPrefabs);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Targets.Length; i++)
		{
			linkedPrefabs.Add(existingSystemManaged.GetEntity(m_Targets[i].m_Target));
		}
	}
}
