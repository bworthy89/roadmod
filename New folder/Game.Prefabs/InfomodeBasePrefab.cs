using System.Collections.Generic;

namespace Game.Prefabs;

public abstract class InfomodeBasePrefab : InfomodePrefab
{
	public InfomodeGroupPrefab[] m_IncludeInGroups;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_IncludeInGroups != null)
		{
			for (int i = 0; i < m_IncludeInGroups.Length; i++)
			{
				prefabs.Add(m_IncludeInGroups[i]);
			}
		}
	}
}
