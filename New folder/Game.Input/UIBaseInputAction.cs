using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Input;

public abstract class UIBaseInputAction : ScriptableObject
{
	public delegate DisplayNameOverride DisplayGetter(string name, ProxyAction action, InputManager.DeviceType mask, Transform transform);

	public interface IState
	{
		IReadOnlyList<ProxyAction> actions { get; }
	}

	public enum Priority
	{
		Custom = -1000,
		Disabled = -1,
		Tooltip = 0,
		A = 20,
		X = 30,
		Y = 40,
		B = 50,
		DPad = 55,
		Bumper = 60,
		Trigger = 70
	}

	public enum ProcessAs
	{
		AutoDetect,
		Button,
		Axis,
		Vector2
	}

	[Flags]
	public enum Transform
	{
		None = 0,
		Down = 1,
		Up = 2,
		Left = 4,
		Right = 8,
		Negative = 5,
		Positive = 0xA,
		Vertical = 3,
		Horizontal = 0xC,
		Press = 0xF
	}

	public string m_AliasName;

	public Priority m_DisplayPriority = Priority.Disabled;

	public InputManager.DeviceType m_DisplayMask = InputManager.DeviceType.Gamepad;

	public bool m_ShowInOptions;

	public OptionGroupOverride m_OptionGroupOverride;

	public string aliasName => m_AliasName;

	public int displayPriority => (int)m_DisplayPriority;

	public bool showInOptions => m_ShowInOptions;

	public OptionGroupOverride optionGroupOverride => m_OptionGroupOverride;

	public abstract IReadOnlyList<UIInputActionPart> actionParts { get; }

	public DisplayNameOverride GetDisplayName(UIInputActionPart actionPart, string source)
	{
		if ((actionPart.m_Mask & m_DisplayMask) != InputManager.DeviceType.None)
		{
			return new DisplayNameOverride(source, actionPart.GetProxyAction(), m_AliasName, (int)m_DisplayPriority, actionPart.m_Transform);
		}
		return null;
	}

	public abstract IProxyAction GetState(string source);

	public abstract IProxyAction GetState(string source, DisplayGetter displayNameGetter);
}
