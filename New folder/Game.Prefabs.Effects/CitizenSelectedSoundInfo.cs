using System;
using Game.Citizens;
using UnityEngine;

namespace Game.Prefabs.Effects;

[Serializable]
public class CitizenSelectedSoundInfo
{
	public CitizenAge m_Age;

	[Tooltip("when 'Is Sick Or Injured' was checked, the happiness value will be ignored")]
	public bool m_IsSickOrInjured;

	[Tooltip("this value will be ignored when 'Is Sick Or Injured' was checked")]
	public CitizenHappiness m_Happiness;

	public EffectPrefab m_SelectedSound;
}
