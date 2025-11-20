using System;
using UnityEngine.Scripting;

namespace Game.Debug;

[AttributeUsage(AttributeTargets.Method)]
public class DebugTabAttribute : PreserveAttribute
{
	public readonly string name;

	public readonly int priority;

	public DebugTabAttribute(string name, int priority = 0)
	{
		this.name = name;
		this.priority = priority;
	}
}
