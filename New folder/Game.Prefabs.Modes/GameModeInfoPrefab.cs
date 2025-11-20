using System;
using System.Collections.Generic;
using Game.UI.Localization;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Setting Info/", new Type[] { })]
public class GameModeInfoPrefab : PrefabBase
{
	public ModeSetting m_ModeSetting;

	public string m_Image;

	public string m_DecorateImage;

	public GameModeRule[] m_Descriptions;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<GameModeInfoData>());
	}

	public GameModeInfo GetGameModeInfo()
	{
		LocalizedString[] array = null;
		if (m_Descriptions != null)
		{
			array = new LocalizedString[m_Descriptions.Length];
			for (int i = 0; i < m_Descriptions.Length; i++)
			{
				GameModeRule gameModeRule = m_Descriptions[i];
				if (gameModeRule.m_ArgName == string.Empty)
				{
					array[i] = LocalizedString.Id("Menu.GAME_MODE_RULES[" + gameModeRule.m_Term + "]");
					continue;
				}
				array[i] = new LocalizedString("Menu.GAME_MODE_RULES[" + gameModeRule.m_Term + "]", null, new Dictionary<string, ILocElement> { 
				{
					gameModeRule.m_ArgName ?? "",
					new LocalizedNumber<int>(gameModeRule.m_ArgValue, gameModeRule.GetUnit())
				} });
			}
		}
		return new GameModeInfo
		{
			id = ((m_ModeSetting == null) ? "" : m_ModeSetting.prefab.name),
			image = m_Image,
			decorateImage = m_DecorateImage,
			descriptions = (array ?? Array.Empty<LocalizedString>())
		};
	}
}
