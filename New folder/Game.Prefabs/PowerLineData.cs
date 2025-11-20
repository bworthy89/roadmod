using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct PowerLineData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
