using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.UI;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct CustomName : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
