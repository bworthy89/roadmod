using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Tutorials;
using Game.UI;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Phases/", new Type[] { })]
public class TutorialBalloonPrefab : TutorialPhasePrefab
{
	[Serializable]
	public class BalloonUITarget : IJsonWritable
	{
		[NotNull]
		public PrefabBase m_UITagProvider;

		public BalloonDirection m_BalloonDirection;

		public BalloonAlignment m_BalloonAlignment;

		public BalloonUITarget()
		{
			m_BalloonDirection = BalloonDirection.up;
			m_BalloonAlignment = BalloonAlignment.center;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(TypeNames.kTutorialBalloonUITarget);
			writer.PropertyName("uiTag");
			writer.Write((m_UITagProvider != null) ? m_UITagProvider.uiTag : string.Empty);
			writer.PropertyName("direction");
			writer.Write(m_BalloonDirection.ToString());
			writer.PropertyName("alignment");
			writer.Write(m_BalloonAlignment.ToString());
			writer.TypeEnd();
		}
	}

	public enum BalloonDirection
	{
		up,
		down,
		left,
		right
	}

	public enum BalloonAlignment
	{
		start,
		center,
		end
	}

	[NotNull]
	public BalloonUITarget[] m_UITargets;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_UITargets.Length; i++)
		{
			if (m_UITargets[i].m_UITagProvider != null)
			{
				prefabs.Add(m_UITargets[i].m_UITagProvider);
			}
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TutorialPhaseData
		{
			m_Type = TutorialPhaseType.Balloon,
			m_OverrideCompletionDelay = m_OverrideCompletionDelay
		});
	}

	public override void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		base.GenerateTutorialLinks(entityManager, linkedPrefabs);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_UITargets.Length; i++)
		{
			if (m_UITargets[i].m_UITagProvider != null)
			{
				linkedPrefabs.Add(existingSystemManaged.GetEntity(m_UITargets[i].m_UITagProvider));
			}
		}
	}
}
