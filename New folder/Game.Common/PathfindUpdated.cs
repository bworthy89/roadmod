using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Common;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct PathfindUpdated : IComponentData, IQueryTypeParameter
{
}
