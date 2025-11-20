using System.Collections.Generic;
using Game.Input;
using Game.Tools;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

public class InputHintsTooltipSystem : TooltipSystemBase
{
	private ToolSystem m_ToolSystem;

	private ToolBaseSystem m_LastActiveTool;

	private InputManager.ControlScheme m_ControlScheme;

	private readonly Dictionary<ProxyAction, InputHintTooltip> m_Tooltips = new Dictionary<ProxyAction, InputHintTooltip>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ToolBaseSystem activeTool = m_ToolSystem.activeTool;
		InputManager.DeviceType deviceType = InputManager.instance.activeControlScheme switch
		{
			InputManager.ControlScheme.Gamepad => InputManager.DeviceType.Gamepad, 
			InputManager.ControlScheme.KeyboardAndMouse => InputManager.DeviceType.Mouse, 
			_ => InputManager.DeviceType.None, 
		};
		if (m_LastActiveTool != activeTool || m_ControlScheme != InputManager.instance.activeControlScheme)
		{
			m_LastActiveTool = activeTool;
			m_ControlScheme = InputManager.instance.activeControlScheme;
			m_Tooltips.Clear();
			if (m_LastActiveTool != null)
			{
				foreach (IProxyAction action in activeTool.actions)
				{
					if (!(action is UIBaseInputAction.IState state))
					{
						if (action is ProxyAction proxyAction && !m_Tooltips.ContainsKey(proxyAction))
						{
							m_Tooltips.Add(proxyAction, new InputHintTooltip(proxyAction, deviceType));
						}
						continue;
					}
					foreach (ProxyAction action2 in state.actions)
					{
						if (!m_Tooltips.ContainsKey(action2))
						{
							m_Tooltips.Add(action2, new InputHintTooltip(action2, deviceType));
						}
					}
				}
			}
		}
		foreach (var (proxyAction3, inputHintTooltip2) in m_Tooltips)
		{
			if ((proxyAction3.mask & deviceType) != InputManager.DeviceType.None && proxyAction3.displayOverride != null && proxyAction3.displayOverride.priority != -1)
			{
				inputHintTooltip2.Refresh();
				AddMouseTooltip(inputHintTooltip2);
			}
		}
	}

	[Preserve]
	public InputHintsTooltipSystem()
	{
	}
}
