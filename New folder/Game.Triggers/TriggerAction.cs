using Unity.Entities;

namespace Game.Triggers;

public struct TriggerAction
{
	public TriggerType m_TriggerType;

	public Entity m_TriggerPrefab;

	public Entity m_PrimaryTarget;

	public Entity m_SecondaryTarget;

	public float m_Value;

	public TriggerAction(TriggerType triggerType, Entity triggerPrefab, Entity primaryTarget, Entity secondaryTarget, float value = 0f)
	{
		m_TriggerType = triggerType;
		m_TriggerPrefab = triggerPrefab;
		m_PrimaryTarget = primaryTarget;
		m_SecondaryTarget = secondaryTarget;
		m_Value = value;
	}

	public TriggerAction(TriggerType triggerType, Entity triggerPrefab, float value)
	{
		m_TriggerType = triggerType;
		m_TriggerPrefab = triggerPrefab;
		m_PrimaryTarget = Entity.Null;
		m_SecondaryTarget = Entity.Null;
		m_Value = value;
	}
}
