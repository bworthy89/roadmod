using Colossal.PSI.Common;
using Colossal.UI;
using Game.Input;
using Game.SceneFlow;

namespace Game.PSI;

public class VirtualKeyboard : TextInputHandler
{
	public VirtualKeyboard()
	{
		PlatformManager.instance.onInputDismissed += delegate(IVirtualKeyboardSupport psi, string text)
		{
			if (!psi.passThroughVKeyboard)
			{
				RefreshText(text);
			}
		};
	}

	private string GetVkTitle()
	{
		string attribute = base.proxy.GetAttribute("vk-title");
		if (!string.IsNullOrEmpty(attribute))
		{
			return attribute;
		}
		return "Input";
	}

	private string GetVkDescription()
	{
		string attribute = base.proxy.GetAttribute("vk-description");
		if (!string.IsNullOrEmpty(attribute))
		{
			return attribute;
		}
		return string.Empty;
	}

	private InputType TextToInputType(string text)
	{
		return text switch
		{
			"text" => InputType.Text, 
			"password" => InputType.Password, 
			"email" => InputType.Email, 
			_ => InputType.Other, 
		};
	}

	private InputType GetVkType()
	{
		string attribute = base.proxy.GetAttribute("vk-type");
		return TextToInputType(attribute);
	}

	protected override void OnFocusCallback(string str)
	{
		if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.Gamepad)
		{
			bool flag = PlatformManager.instance.ShowVirtualKeyboard(GetVkType(), GetVkTitle(), GetVkDescription(), 100, str);
			GameManager.UIInputSystem.emulateBackspaceOnTextEvent = flag && PlatformManager.instance.passThroughVKeyboard;
		}
	}

	protected override void OnBlurCallback()
	{
		GameManager.UIInputSystem.emulateBackspaceOnTextEvent = false;
		PlatformManager.instance.DismissVirtualKeyboard();
	}
}
