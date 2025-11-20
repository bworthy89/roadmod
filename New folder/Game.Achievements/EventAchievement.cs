using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.Achievements;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct EventAchievement : IComponentData, IQueryTypeParameter
{
}
