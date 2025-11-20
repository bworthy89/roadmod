using Game.UI.Localization;
using UnityEngine;

namespace Game.Prefabs;

public abstract class GradientInfomodeBasePrefab : InfomodeBasePrefab, IGradientInfomode
{
	private static readonly CachedLocalizedStringBuilder<string> kLabels = CachedLocalizedStringBuilder<string>.Id((string hash) => "Infoviews.LABEL[" + hash + "]");

	public Color m_Low = Color.red;

	public Color m_Medium = Color.yellow;

	public Color m_High = Color.green;

	public int m_Steps = 11;

	public GradientLegendType m_LegendType;

	[Tooltip("Good, Bad, Low, High, Weak, Strong, Old, Young")]
	public string m_LowLabelId;

	public string m_MediumLabelId;

	[Tooltip("Good, Bad, Low, High, Weak, Strong, Old, Young")]
	public string m_HighLabelId;

	public Color lowColor => Opaque(m_Low);

	public Color mediumColor => Opaque(m_Medium);

	public Color highColor => Opaque(m_High);

	public GradientLegendType legendType => m_LegendType;

	public LocalizedString? lowLabel => GetLabel(m_LowLabelId);

	public LocalizedString? mediumLabel => GetLabel(m_MediumLabelId);

	public LocalizedString? highLabel => GetLabel(m_HighLabelId);

	private static LocalizedString? GetLabel(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return kLabels[id];
	}

	public override void GetColors(out Color color0, out Color color1, out Color color2, out float steps, out float speed, out float tiling, out float fill)
	{
		color0 = m_Low;
		color1 = m_Medium;
		color2 = m_High;
		steps = m_Steps;
		speed = 0f;
		tiling = 0f;
		fill = 0f;
	}

	private static Color Opaque(Color color)
	{
		color.a = 1f;
		return color;
	}
}
