using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GameModeInfoData : IComponentData, IQueryTypeParameter
{
}
