using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class RoadSection : InfoSectionBase
{
	private float[] m_Volume;

	private float[] m_Flow;

	protected override string group => "RoadSection";

	private int roadElementCount { get; set; }

	private float length { get; set; }

	private float bestCondition { get; set; }

	private float worstCondition { get; set; }

	private float condition { get; set; }

	private float upkeep { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Volume = new float[5];
		m_Flow = new float[5];
	}

	protected override void Reset()
	{
		for (int i = 0; i < 5; i++)
		{
			m_Volume[i] = 0f;
			m_Flow[i] = 0f;
		}
		length = 0f;
		bestCondition = 100f;
		worstCondition = 0f;
		condition = 0f;
		upkeep = 0f;
		roadElementCount = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.HasComponent<Aggregate>(selectedEntity) && base.EntityManager.HasComponent<AggregateElement>(selectedEntity);
	}

	protected override void OnProcess()
	{
		DynamicBuffer<AggregateElement> buffer = base.EntityManager.GetBuffer<AggregateElement>(selectedEntity, isReadOnly: true);
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity edge = buffer[i].m_Edge;
			if (base.EntityManager.TryGetComponent<Curve>(edge, out var component))
			{
				length += component.m_Length;
			}
			if (base.EntityManager.TryGetComponent<Road>(edge, out var component2))
			{
				roadElementCount++;
				float4 @float = (component2.m_TrafficFlowDistance0 + component2.m_TrafficFlowDistance1) * 16f;
				float4 float2 = NetUtils.GetTrafficFlowSpeed(component2) * 100f;
				m_Volume[0] += @float.x * 4f / 24f;
				m_Volume[1] += @float.y * 4f / 24f;
				m_Volume[2] += @float.z * 4f / 24f;
				m_Volume[3] += @float.w * 4f / 24f;
				m_Flow[0] += float2.x;
				m_Flow[1] += float2.y;
				m_Flow[2] += float2.z;
				m_Flow[3] += float2.w;
			}
			if (base.EntityManager.TryGetComponent<NetCondition>(edge, out var component3))
			{
				float2 wear = component3.m_Wear;
				if (wear.x > worstCondition)
				{
					worstCondition = wear.x;
				}
				if (wear.y > worstCondition)
				{
					worstCondition = wear.y;
				}
				if (wear.x < bestCondition)
				{
					bestCondition = wear.x;
				}
				if (wear.y < bestCondition)
				{
					bestCondition = wear.y;
				}
				condition += math.csum(wear) * 0.5f;
			}
			if (base.EntityManager.TryGetComponent<PrefabRef>(edge, out var component4) && base.EntityManager.TryGetComponent<PlaceableNetData>(component4.m_Prefab, out var component5))
			{
				upkeep += component5.m_DefaultUpkeepCost;
			}
		}
		if (roadElementCount > 0)
		{
			m_Volume[0] /= roadElementCount;
			m_Volume[1] /= roadElementCount;
			m_Volume[2] /= roadElementCount;
			m_Volume[3] /= roadElementCount;
			m_Volume[4] = m_Volume[0];
			m_Flow[0] /= roadElementCount;
			m_Flow[1] /= roadElementCount;
			m_Flow[2] /= roadElementCount;
			m_Flow[3] /= roadElementCount;
			m_Flow[4] = m_Flow[0];
			bestCondition = 100f - bestCondition / 10f * 100f;
			worstCondition = 100f - worstCondition / 10f * 100f;
			condition = condition / 10f * 100f;
			condition = 100f - condition / (float)roadElementCount;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("volumeData");
		if (roadElementCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.ArrayBegin(m_Volume.Length);
			for (int i = 0; i < m_Volume.Length; i++)
			{
				writer.Write(m_Volume[i]);
			}
			writer.ArrayEnd();
		}
		writer.PropertyName("flowData");
		if (roadElementCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.ArrayBegin(m_Flow.Length);
			for (int j = 0; j < m_Flow.Length; j++)
			{
				writer.Write(m_Flow[j]);
			}
			writer.ArrayEnd();
		}
		writer.PropertyName("length");
		writer.Write(length);
		writer.PropertyName("bestCondition");
		if (roadElementCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(bestCondition);
		}
		writer.PropertyName("worstCondition");
		if (roadElementCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(worstCondition);
		}
		writer.PropertyName("condition");
		if (roadElementCount == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(condition);
		}
		writer.PropertyName("upkeep");
		writer.Write(upkeep);
	}

	[Preserve]
	public RoadSection()
	{
	}
}
