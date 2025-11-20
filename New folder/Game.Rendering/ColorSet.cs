using UnityEngine;

namespace Game.Rendering;

public struct ColorSet
{
	public Color m_Channel0;

	public Color m_Channel1;

	public Color m_Channel2;

	public Color this[int index]
	{
		get
		{
			return index switch
			{
				0 => m_Channel0, 
				1 => m_Channel1, 
				2 => m_Channel2, 
				_ => default(Color), 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				m_Channel0 = value;
				break;
			case 1:
				m_Channel1 = value;
				break;
			case 2:
				m_Channel2 = value;
				break;
			}
		}
	}

	public ColorSet(Color color)
	{
		m_Channel0 = color;
		m_Channel1 = color;
		m_Channel2 = color;
	}
}
