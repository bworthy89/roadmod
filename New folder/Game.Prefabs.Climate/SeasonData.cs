using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Prefabs.Climate;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct SeasonData : IComponentData, IQueryTypeParameter
{
}
