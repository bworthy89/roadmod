using System.Collections.Generic;
using UnityEngine;

namespace Game.Input;

[CreateAssetMenu(menuName = "Colossal/UI/UIInputOverrideAction")]
public class UIInputOverrideAction : UIBaseInputAction
{
	public UIBaseInputAction m_Source;

	public bool m_OverridePriority;

	public override IReadOnlyList<UIInputActionPart> actionParts => m_Source.actionParts;

	public override IProxyAction GetState(string source)
	{
		return m_Source.GetState(source, delegate(string overrideSource, ProxyAction action, InputManager.DeviceType mask, Transform transform)
		{
			if (m_OverridePriority)
			{
				if ((mask & m_DisplayMask) != InputManager.DeviceType.None)
				{
					return new DisplayNameOverride(overrideSource, action, m_AliasName, (int)m_DisplayPriority, transform);
				}
				return (DisplayNameOverride)null;
			}
			return ((mask & m_Source.m_DisplayMask) != InputManager.DeviceType.None) ? new DisplayNameOverride(overrideSource, action, m_AliasName, (int)m_Source.m_DisplayPriority, transform) : null;
		});
	}

	public override IProxyAction GetState(string source, DisplayGetter displayNameGetter)
	{
		return m_Source.GetState(source, displayNameGetter);
	}
}
