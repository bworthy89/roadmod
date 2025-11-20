using System;
using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[Obsolete]
public struct Warehouse : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
