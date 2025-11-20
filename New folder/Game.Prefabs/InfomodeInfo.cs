using System;

namespace Game.Prefabs;

[Serializable]
public class InfomodeInfo : IComparable<InfomodeInfo>
{
	public InfomodePrefab m_Mode;

	public int m_Priority;

	public bool m_Supplemental;

	public bool m_Optional;

	public int CompareTo(InfomodeInfo other)
	{
		return m_Priority - other.m_Priority;
	}
}
