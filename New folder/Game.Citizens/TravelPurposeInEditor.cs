using System;
using Game.Economy;

namespace Game.Citizens;

[Serializable]
public struct TravelPurposeInEditor
{
	public Purpose m_Purpose;

	public int m_Data;

	public ResourceInEditor m_Resource;
}
