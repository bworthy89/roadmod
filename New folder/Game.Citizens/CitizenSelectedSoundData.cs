using System;
using Unity.Entities;

namespace Game.Citizens;

[InternalBufferCapacity(0)]
public struct CitizenSelectedSoundData : IBufferElementData, IEquatable<CitizenSelectedSoundData>
{
	public bool m_IsSickOrInjured;

	public CitizenAge m_Age;

	public CitizenHappiness m_Happiness;

	public Entity m_SelectedSound;

	public CitizenSelectedSoundData(bool isSickOrInjured, CitizenAge age, CitizenHappiness happiness, Entity selectedSound)
	{
		m_IsSickOrInjured = isSickOrInjured;
		m_Age = age;
		m_Happiness = happiness;
		m_SelectedSound = selectedSound;
	}

	public bool Equals(CitizenSelectedSoundData other)
	{
		if (!m_IsSickOrInjured.Equals(other.m_IsSickOrInjured) || !m_Age.Equals(other.m_Age))
		{
			return false;
		}
		if (!m_IsSickOrInjured)
		{
			return m_Happiness.Equals(other.m_Happiness);
		}
		return true;
	}

	public override int GetHashCode()
	{
		return (m_IsSickOrInjured, m_Age, m_Happiness).GetHashCode();
	}
}
