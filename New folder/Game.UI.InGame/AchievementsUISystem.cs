using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class AchievementsUISystem : UISystemBase
{
	private enum AchievementTabStatus
	{
		Available,
		Hidden,
		ModsDisabled,
		OptionsDisabled
	}

	private const string kGroup = "achievements";

	private CityConfigurationSystem m_CityConfigurationSystem;

	private RawValueBinding m_AchievementsBinding;

	private GetterValueBinding<int> m_TabStatusBinding;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		PlatformManager.instance.onAchievementUpdated += UpdateAchievements;
		AddBinding(m_AchievementsBinding = new RawValueBinding("achievements", "achievements", BindAchievements));
		AddBinding(m_TabStatusBinding = new GetterValueBinding<int>("achievements", "achievementTabStatus", GetAchievementTabStatus));
	}

	private int GetAchievementTabStatus()
	{
		if (PlatformManager.instance.CountAchievements() == 0)
		{
			return 1;
		}
		if (m_CityConfigurationSystem.usedMods.Count > 0)
		{
			return 2;
		}
		if (!PlatformManager.instance.achievementsEnabled)
		{
			return 3;
		}
		return 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
	{
		m_TabStatusBinding.Update();
	}

	private void UpdateAchievements(IAchievementsSupport backend, AchievementId id)
	{
		m_AchievementsBinding.Update();
	}

	private void BindAchievements(IJsonWriter binder)
	{
		int num = PlatformManager.instance.CountAchievements();
		m_TabStatusBinding.Update();
		if (num > 0)
		{
			binder.ArrayBegin(num);
			foreach (IAchievement item in PlatformManager.instance.EnumerateAchievements())
			{
				BindAchievement(binder, item);
			}
			binder.ArrayEnd();
		}
		else
		{
			binder.ArrayBegin(0u);
			binder.ArrayEnd();
		}
	}

	private void BindAchievement(IJsonWriter binder, IAchievement achievement)
	{
		binder.TypeBegin("achievements.Achievement");
		binder.PropertyName("localeKey");
		binder.Write(achievement.internalName);
		bool flag = !achievement.achieved;
		binder.PropertyName("imagePath");
		binder.Write(GetImagePath(achievement, flag));
		binder.PropertyName("locked");
		binder.Write(flag);
		binder.PropertyName("isIncremental");
		binder.Write(achievement.isIncremental);
		binder.PropertyName("progress");
		binder.Write(achievement.progress);
		binder.PropertyName("maxProgress");
		binder.Write(achievement.maxProgress);
		binder.PropertyName("dlcImage");
		binder.Write(GetDlcImage(achievement.dlcId));
		binder.PropertyName("isDevelopment");
		binder.Write(achievement is DevelopmentAchievementsManager.DevelopmentAchievement);
		binder.TypeEnd();
	}

	private static string GetDlcImage(DlcId dlcId)
	{
		if (!(dlcId != DlcId.BaseGame))
		{
			return null;
		}
		return "Media/DLC/" + PlatformManager.instance.GetDlcName(dlcId) + ".svg";
	}

	private static string GetImagePath(IAchievement achievement, bool locked)
	{
		return "Media/Game/Achievements/" + achievement.internalName + (locked ? "_locked" : "") + ".png";
	}

	[Preserve]
	public AchievementsUISystem()
	{
	}
}
