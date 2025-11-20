using System.Runtime.InteropServices;
using Unity.Entities;

namespace Game.UI;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct UIEconomyConfigurationData : IComponentData, IQueryTypeParameter
{
}
