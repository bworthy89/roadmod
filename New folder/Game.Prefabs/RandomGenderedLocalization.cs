using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Localization/", new Type[] { })]
public class RandomGenderedLocalization : RandomLocalization
{
	public string m_MaleID;

	public string m_FemaleID;

	protected override int GetLocalizationCount()
	{
		int localizationCount = base.GetLocalizationCount();
		int localizationIndexCount = RandomLocalization.GetLocalizationIndexCount(base.prefab, m_MaleID);
		int localizationIndexCount2 = RandomLocalization.GetLocalizationIndexCount(base.prefab, m_FemaleID);
		int num = math.min(localizationCount, math.min(localizationIndexCount, localizationIndexCount2));
		if (localizationCount != num || localizationIndexCount != num || localizationIndexCount2 != num)
		{
			ComponentBase.baseLog.WarnFormat(base.prefab, "All gendered localization IDs should have the same variation count: {0} ({1}), {2} ({3}), {4} ({5})", m_LocalizationID, localizationCount, m_MaleID, localizationIndexCount, m_FemaleID, localizationIndexCount2);
		}
		return num;
	}
}
