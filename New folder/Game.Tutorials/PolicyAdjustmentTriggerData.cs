using Unity.Entities;

namespace Game.Tutorials;

public struct PolicyAdjustmentTriggerData : IComponentData, IQueryTypeParameter
{
	public PolicyAdjustmentTriggerFlags m_Flags;

	public PolicyAdjustmentTriggerTargetFlags m_TargetFlags;

	public PolicyAdjustmentTriggerData(PolicyAdjustmentTriggerFlags flags, PolicyAdjustmentTriggerTargetFlags targetFlags)
	{
		m_Flags = flags;
		m_TargetFlags = targetFlags;
	}
}
