using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Common;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Overridden : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
