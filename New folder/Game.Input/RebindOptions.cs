using System;
using UnityEngine;

namespace Game.Input;

[Flags]
public enum RebindOptions
{
	None = 0,
	[InspectorName("Key Only")]
	Key = 1,
	[InspectorName("Modifiers Only")]
	Modifiers = 2,
	[InspectorName("Key and Modifiers")]
	All = 3
}
