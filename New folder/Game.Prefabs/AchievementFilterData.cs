using Colossal.PSI.Common;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct AchievementFilterData : IBufferElementData
{
	[InputField]
	[Range(0f, 43f)]
	public AchievementId m_AchievementID;

	public bool m_Allow;
}
