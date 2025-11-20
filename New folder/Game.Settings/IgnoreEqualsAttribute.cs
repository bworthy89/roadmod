using System;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property)]
internal class IgnoreEqualsAttribute : Attribute
{
}
