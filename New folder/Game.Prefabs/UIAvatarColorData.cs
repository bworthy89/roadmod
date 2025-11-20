using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct UIAvatarColorData : IBufferElementData
{
	public Color32 m_Color;

	public UIAvatarColorData(Color32 color)
	{
		m_Color = color;
	}
}
