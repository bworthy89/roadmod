using UnityEngine;

namespace Game.Prefabs;

public abstract class ColorInfomodeBasePrefab : InfomodeBasePrefab, IColorInfomode
{
	public Color m_Color;

	public Color color => m_Color;

	public override void GetColors(out Color color0, out Color color1, out Color color2, out float steps, out float speed, out float tiling, out float fill)
	{
		color0 = m_Color;
		color1 = m_Color;
		color2 = m_Color;
		steps = 1f;
		speed = 0f;
		tiling = 0f;
		fill = 0f;
	}
}
