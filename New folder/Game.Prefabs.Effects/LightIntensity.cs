using System;
using System.Linq;
using Game.Rendering;
using Game.UI.Widgets;
using UnityEngine.Serialization;

namespace Game.Prefabs.Effects;

[Serializable]
public class LightIntensity
{
	[FormerlySerializedAs("m_LuxIntensity, m_Intensity")]
	public float m_Intensity = 10f;

	public LightUnit m_LightUnit = LightUnit.Lux;

	public static EnumMember[] ConvertToEnumMembers<TEnum>() where TEnum : Enum
	{
		return (from TEnum e in Enum.GetValues(typeof(TEnum))
			select new EnumMember(Convert.ToUInt64(e), e.ToString())).ToArray();
	}
}
