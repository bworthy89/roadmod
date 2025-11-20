using System;
using UnityEngine;

namespace Game.Prefabs;

[Serializable]
public class UITrendThresholds
{
	[Tooltip("Proportion of the actual value over which the medium trend arrows will be shown.")]
	public float m_Medium;

	[Tooltip("Proportion of the actual value over which the high trend arrows will be shown.")]
	public float m_High;
}
