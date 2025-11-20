using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct AmbienceEmitter : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
