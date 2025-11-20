using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.Widgets;

public class RangeNAttribute : PropertyAttribute
{
	public float4 min { get; private set; }

	public float4 max { get; private set; }

	public RangeNAttribute(float min, float max, bool componentExpansion = true)
	{
		if (componentExpansion)
		{
			this.min = min;
			this.max = max;
		}
		else
		{
			this.min = new float4(min, float3.zero);
			this.max = new float4(max, float3.zero);
		}
	}

	public RangeNAttribute(float2 min, float2 max)
	{
		this.min = new float4(min, float2.zero);
		this.max = new float4(max, float2.zero);
	}

	public RangeNAttribute(float3 min, float3 max)
	{
		this.min = new float4(min, 0f);
		this.max = new float4(max, 0f);
	}

	public RangeNAttribute(float4 min, float4 max)
	{
		this.min = min;
		this.max = max;
	}
}
