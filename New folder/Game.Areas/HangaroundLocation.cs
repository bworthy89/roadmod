using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct HangaroundLocation : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
