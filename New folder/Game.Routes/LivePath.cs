using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Routes;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct LivePath : IComponentData, IQueryTypeParameter
{
}
