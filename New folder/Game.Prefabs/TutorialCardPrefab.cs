using System;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Phases/", new Type[] { })]
public class TutorialCardPrefab : TutorialPhasePrefab
{
	public bool m_CenterCard;

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TutorialPhaseData
		{
			m_Type = ((!m_CenterCard) ? TutorialPhaseType.Card : TutorialPhaseType.CenterCard),
			m_OverrideCompletionDelay = m_OverrideCompletionDelay
		});
	}
}
