using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public class SettingsUISearchHiddenAttribute : Attribute
{
}
