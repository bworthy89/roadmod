using System.Collections.Generic;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public abstract class TutorialTriggerPrefabBase : PrefabBase
{
	private Dictionary<int, List<string>> m_BlinkDict;

	public bool m_DisplayUI = true;

	public virtual bool phaseBranching => false;

	public override bool ignoreUnlockDependencies => true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TutorialTriggerData>());
	}

	protected virtual void GenerateBlinkTags()
	{
		if (m_BlinkDict == null)
		{
			m_BlinkDict = new Dictionary<int, List<string>>();
		}
		else
		{
			m_BlinkDict.Clear();
		}
	}

	protected void AddBlinkTag(string tag)
	{
		AddBlinkTagAtPosition(tag, 0);
	}

	protected void AddBlinkTagAtPosition(string tag, int position)
	{
		if (!m_BlinkDict.ContainsKey(position))
		{
			m_BlinkDict[position] = new List<string>();
		}
		if (!m_BlinkDict[position].Contains(tag))
		{
			m_BlinkDict[position].Add(tag);
		}
	}

	public Dictionary<int, List<string>> GetBlinkTags()
	{
		return m_BlinkDict;
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		GenerateBlinkTags();
	}

	public virtual void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
	}
}
