using System;
using UnityEngine.Scripting;

namespace Game.Debug;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class DebugContainerAttribute : PreserveAttribute
{
}
