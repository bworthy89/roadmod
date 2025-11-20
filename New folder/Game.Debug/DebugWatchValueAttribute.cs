using System;

namespace Game.Debug;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class DebugWatchValueAttribute : Attribute
{
	public string color { get; set; }

	public int updateInterval { get; set; } = -1;

	public int historyLength { get; set; } = 128;
}
