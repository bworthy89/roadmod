using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Tutorials;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct TutorialFireTelemetry : IComponentData, IQueryTypeParameter
{
}
