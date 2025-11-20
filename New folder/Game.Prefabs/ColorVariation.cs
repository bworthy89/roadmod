using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct ColorVariation : IBufferElementData
{
	public ColorSet m_ColorSet;

	public ColorGroupID m_GroupID;

	public ColorSyncFlags m_SyncFlags;

	public ColorSourceType m_ColorSourceType;

	public byte m_Probability;

	public sbyte m_ExternalChannel0;

	public sbyte m_ExternalChannel1;

	public sbyte m_ExternalChannel2;

	public byte m_HueRange;

	public byte m_SaturationRange;

	public byte m_ValueRange;

	public byte m_AlphaRange0;

	public byte m_AlphaRange1;

	public byte m_AlphaRange2;

	public bool hasExternalChannels => (m_ExternalChannel0 >= 0) | (m_ExternalChannel1 >= 0) | (m_ExternalChannel2 >= 0);

	public bool hasVariationRanges => (m_HueRange != 0) | (m_SaturationRange != 0) | (m_ValueRange != 0);

	public bool hasAlphaRanges => (m_AlphaRange0 != 0) | (m_AlphaRange1 != 0) | (m_AlphaRange2 != 0);

	public int GetExternalChannelIndex(int colorIndex)
	{
		return colorIndex switch
		{
			0 => m_ExternalChannel0, 
			1 => m_ExternalChannel1, 
			2 => m_ExternalChannel2, 
			_ => -1, 
		};
	}

	public void SetExternalChannelIndex(int colorIndex, int channelIndex)
	{
		switch (colorIndex)
		{
		case 0:
			m_ExternalChannel0 = (sbyte)channelIndex;
			break;
		case 1:
			m_ExternalChannel1 = (sbyte)channelIndex;
			break;
		case 2:
			m_ExternalChannel2 = (sbyte)channelIndex;
			break;
		}
	}
}
