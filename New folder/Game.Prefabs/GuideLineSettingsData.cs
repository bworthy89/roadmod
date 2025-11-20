using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct GuideLineSettingsData : IComponentData, IQueryTypeParameter
{
	public Color m_VeryLowPriorityColor;

	public Color m_LowPriorityColor;

	public Color m_MediumPriorityColor;

	public Color m_HighPriorityColor;

	public Color m_PositiveFeedbackColor;
}
