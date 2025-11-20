using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, Inherited = true)]
public class SettingsUIDeveloperAttribute : Attribute
{
}
