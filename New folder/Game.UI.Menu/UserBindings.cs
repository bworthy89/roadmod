using System;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.SceneFlow;

namespace Game.UI.Menu;

public class UserBindings : CompositeBinding, IDisposable
{
	private const string kGroup = "user";

	private ValueBinding<bool> m_userInfoVisible;

	private ValueBinding<bool> m_userInfoActionAvailable;

	private ValueBinding<string> m_AvatarBinding;

	private ValueBinding<string> m_UserIDBinding;

	private ValueBinding<string> m_SwitchUserHintOverload;

	private static int s_AvatarVersion;

	private bool IsUserInfoVisible
	{
		get
		{
			if (!GameManager.instance.configuration.disableUserSection)
			{
				return PlatformManager.instance.isUserInfoVisible;
			}
			return false;
		}
	}

	private bool IsUserInfoActionAvailable
	{
		get
		{
			if (IsUserInfoVisible)
			{
				if (!PlatformManager.instance.supportsUserSection)
				{
					return PlatformManager.instance.supportsUserSwitching;
				}
				return true;
			}
			return false;
		}
	}

	public UserBindings()
	{
		GameManager.instance.onGameLoadingComplete += OnMainMenuReached;
		AddBinding(m_userInfoVisible = new ValueBinding<bool>("user", "userInfoVisible", IsUserInfoVisible));
		AddBinding(m_userInfoActionAvailable = new ValueBinding<bool>("user", "userInfoActionAvailable", IsUserInfoActionAvailable));
		string initialValue = string.Format("{0}/UserAvatar#{1}?size={2}", "useravatar://", s_AvatarVersion++, AvatarSize.Auto);
		AddBinding(m_AvatarBinding = new ValueBinding<string>("user", "avatar", initialValue, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_UserIDBinding = new ValueBinding<string>("user", "userID", PlatformManager.instance.userName, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_SwitchUserHintOverload = new ValueBinding<string>("user", "switchUserHintOverload", getSwitchUserHintOverload(), ValueWriters.Nullable(new StringWriter())));
		AddBinding(new TriggerBinding("user", "switchUser", SwitchUser));
		PlatformManager.instance.onStatusChanged += delegate(IPlatformServiceIntegration psi)
		{
			if (PlatformManager.instance.IsPrincipalOverlayIntegration(psi))
			{
				m_userInfoVisible.Update(IsUserInfoVisible);
				m_userInfoActionAvailable.Update(IsUserInfoActionAvailable);
			}
		};
		PlatformManager.instance.onUserUpdated += delegate(IUserSupport psi, UserChangedFlags flags)
		{
			if (PlatformManager.instance.IsPrincipalUserIntegration(psi))
			{
				if (flags.HasChanged(UserChangedFlags.Name))
				{
					m_UserIDBinding.Update(PlatformManager.instance.userName);
				}
				if (flags.HasChanged(UserChangedFlags.Avatar))
				{
					m_AvatarBinding.Update(string.Format("{0}/UserAvatar#{1}?size={2}", "useravatar://", s_AvatarVersion++, AvatarSize.Auto));
				}
			}
		};
	}

	private void OnMainMenuReached(Purpose purpose, GameMode mode)
	{
		if (mode == GameMode.MainMenu)
		{
			m_userInfoVisible.Update(IsUserInfoVisible);
			m_userInfoActionAvailable.Update(IsUserInfoActionAvailable);
		}
	}

	public void Dispose()
	{
		GameManager.instance.onGameLoadingComplete -= OnMainMenuReached;
	}

	public string getSwitchUserHintOverload()
	{
		if (PlatformManager.instance.supportsUserSwitching)
		{
			return null;
		}
		return "Steam Overlay";
	}

	private void SwitchUser()
	{
		if (m_userInfoVisible.value)
		{
			PlatformManager instance = PlatformManager.instance;
			if (instance.supportsUserSwitching)
			{
				GameManager.instance.SetScreenActive<SwitchUserScreen>();
			}
			else if (instance.supportsUserSection)
			{
				instance.ShowOverlay(Page.Community);
			}
		}
	}
}
