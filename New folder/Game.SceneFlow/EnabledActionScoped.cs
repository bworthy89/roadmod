#define UNITY_ASSERTIONS
using System;
using Game.Input;
using Unity.Assertions;
using UnityEngine.InputSystem;

namespace Game.SceneFlow;

public class EnabledActionScoped : IDisposable
{
	private readonly OverlayBindings m_Bindings;

	private readonly ProxyAction m_Proxy;

	private readonly DisplayNameOverride m_NameOverride;

	private readonly Func<OverlayScreen, bool> m_ShouldBeEnabled;

	public EnabledActionScoped(GameManager manager, string actionMapName, string actionName, Func<OverlayScreen, bool> shouldBeEnabled = null, string displayProperty = null, int displayPriority = 20)
	{
		m_Proxy = Game.Input.InputManager.instance.FindAction(actionMapName, actionName);
		m_Bindings = manager.userInterface.overlayBindings;
		m_NameOverride = new DisplayNameOverride("EnabledActionScoped", m_Proxy, displayProperty, displayPriority);
		Assert.IsNotNull(m_Proxy);
		m_ShouldBeEnabled = shouldBeEnabled;
		m_Bindings.onScreenActivated += HandleScreenChange;
	}

	private void HandleScreenChange(OverlayScreen screen)
	{
		bool flag = m_ShouldBeEnabled == null || m_ShouldBeEnabled(screen);
		m_Proxy.enabled = flag;
		m_NameOverride.active = flag;
	}

	public void Dispose()
	{
		m_Bindings.onScreenActivated -= HandleScreenChange;
		m_Proxy.enabled = false;
		m_NameOverride.Dispose();
	}

	public static implicit operator InputAction(EnabledActionScoped scoped)
	{
		return scoped.m_Proxy.sourceAction;
	}
}
