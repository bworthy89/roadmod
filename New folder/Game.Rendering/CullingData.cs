using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Rendering;

public struct CullingData
{
	public Bounds3 m_Bounds;

	public uint m_LodData1;

	public uint m_LodData2;

	public int4 lodRange
	{
		get
		{
			return new int4((int)m_LodData1, (int)(m_LodData1 >> 5), (int)(m_LodData1 >> 10), (int)(m_LodData1 >> 15)) & 31;
		}
		set
		{
			m_LodData1 = (m_LodData1 & 0xFFF00000u) | (uint)value.x | (uint)(value.y << 5) | (uint)((value.z << 10) | (value.w << 15));
		}
	}

	public int4 lodFade
	{
		get
		{
			return new int4((int)(m_LodData2 & 0xFF), (int)((m_LodData2 >> 8) & 0xFF), (int)((m_LodData2 >> 16) & 0xFF), (int)(m_LodData2 >> 24));
		}
		set
		{
			m_LodData2 = (uint)(value.x | (value.y << 8) | (value.z << 16) | (value.w << 24));
		}
	}

	public int lodOffset
	{
		get
		{
			return (int)m_LodData1 >> 23;
		}
		set
		{
			m_LodData1 = (m_LodData1 & 0x7FFFFF) | (uint)(value << 23);
		}
	}

	public bool isHidden
	{
		get
		{
			return (m_LodData1 & 0x100000) != 0;
		}
		set
		{
			m_LodData1 = (value ? (m_LodData1 | 0x100000) : (m_LodData1 & 0xFFEFFFFFu));
		}
	}

	public bool isFading
	{
		get
		{
			return (m_LodData1 & 0x200000) != 0;
		}
		set
		{
			m_LodData1 = (value ? (m_LodData1 | 0x200000) : (m_LodData1 & 0xFFDFFFFFu));
		}
	}
}
