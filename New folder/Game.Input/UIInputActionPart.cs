using System;
using UnityEngine.InputSystem;

namespace Game.Input;

[Serializable]
public class UIInputActionPart
{
	public InputActionReference m_Action;

	public UIBaseInputAction.ProcessAs m_ProcessAs;

	public UIBaseInputAction.Transform m_Transform;

	public InputManager.DeviceType m_Mask = InputManager.DeviceType.All;

	public ProxyAction GetProxyAction()
	{
		return InputManager.instance.FindAction(m_Action.action);
	}

	public bool TryGetProxyAction(out ProxyAction action)
	{
		return InputManager.instance.TryFindAction(m_Action.action, out action);
	}
}
