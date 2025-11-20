using System;
using Colossal;
using UnityEngine;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, Inherited = true)]
public class SettingsUIPlatformAttribute : Attribute
{
	private readonly Platform m_Platforms;

	private readonly bool m_DebugConditional;

	public bool IsPlatformSet(RuntimePlatform platform)
	{
		return m_Platforms.IsPlatformSet(platform, m_DebugConditional);
	}

	public SettingsUIPlatformAttribute(Platform platforms, bool debugConditional = false)
	{
		m_Platforms = platforms;
		m_DebugConditional = debugConditional;
	}
}
