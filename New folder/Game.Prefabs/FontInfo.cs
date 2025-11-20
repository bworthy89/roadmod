using System;
using UnityEngine;

namespace Game.Prefabs;

[Serializable]
public class FontInfo
{
	public Font m_Font;

	public int m_SamplingPointSize = 90;

	public int m_AtlasPadding = 9;

	public int m_AtlasWidth = 1024;

	public int m_AtlasHeight = 1024;
}
