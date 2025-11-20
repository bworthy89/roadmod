using System;
using Unity.Entities;

namespace Game.Prefabs;

[Serializable]
public struct TriggerConditionData : IBufferElementData
{
	public TriggerConditionType m_Type;

	public float m_Value;
}
