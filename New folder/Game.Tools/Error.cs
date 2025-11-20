using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Tools;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Error : IComponentData, IQueryTypeParameter
{
}
