using Unity.Entities;
using UnityEngine;

namespace Game.Tools;

public struct ColorDefinition : IComponentData, IQueryTypeParameter
{
	public Color32 m_Color;
}
