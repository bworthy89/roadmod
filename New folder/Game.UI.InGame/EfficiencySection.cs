using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class EfficiencySection : InfoSectionBase
{
	private struct EfficiencyFactor : IJsonWritable
	{
		private Game.Buildings.EfficiencyFactor factor;

		private int value;

		private int result;

		public EfficiencyFactor(Game.Buildings.EfficiencyFactor factor, int value, int result)
		{
			this.factor = factor;
			this.value = value;
			this.result = result;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(EfficiencyFactor).FullName);
			writer.PropertyName("factor");
			writer.Write(Enum.GetName(typeof(Game.Buildings.EfficiencyFactor), factor));
			writer.PropertyName("value");
			writer.Write(value);
			writer.PropertyName("result");
			writer.Write(result);
			writer.TypeEnd();
		}
	}

	protected override string group => "EfficiencySection";

	private int efficiency { get; set; }

	private List<EfficiencyFactor> factors { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		factors = new List<EfficiencyFactor>(32);
	}

	protected override void Reset()
	{
		efficiency = 0;
		factors.Clear();
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Building>(selectedEntity) && base.EntityManager.HasComponent<Efficiency>(selectedEntity) && !base.EntityManager.HasComponent<Abandoned>(selectedEntity) && !base.EntityManager.HasComponent<Destroyed>(selectedEntity))
		{
			if (CompanyUIUtils.HasCompany(base.EntityManager, selectedEntity, selectedPrefab, out var company))
			{
				return company != Entity.Null;
			}
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
		if (base.visible)
		{
			DynamicBuffer<Efficiency> buffer = base.EntityManager.GetBuffer<Efficiency>(selectedEntity, isReadOnly: true);
			m_Dirty = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer)) != efficiency;
		}
	}

	protected override void OnProcess()
	{
		DynamicBuffer<Efficiency> buffer = base.EntityManager.GetBuffer<Efficiency>(selectedEntity, isReadOnly: true);
		efficiency = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer));
		using NativeArray<Efficiency> array = buffer.ToNativeArray(Allocator.Temp);
		array.Sort();
		factors.Clear();
		if (array.Length == 0)
		{
			return;
		}
		if (efficiency > 0)
		{
			float num = 100f;
			{
				foreach (Efficiency item in array)
				{
					float num2 = math.max(0f, item.m_Efficiency);
					num *= num2;
					int num3 = math.max(-99, (int)math.round(100f * num2) - 100);
					int result = math.max(1, (int)math.round(num));
					if (num3 != 0)
					{
						factors.Add(new EfficiencyFactor(item.m_Factor, num3, result));
					}
				}
				return;
			}
		}
		foreach (Efficiency item2 in array)
		{
			if (math.max(0f, item2.m_Efficiency) == 0f)
			{
				factors.Add(new EfficiencyFactor(item2.m_Factor, -100, -100));
				if ((int)item2.m_Factor <= 3)
				{
					break;
				}
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("efficiency");
		writer.Write(efficiency);
		writer.PropertyName("factors");
		writer.ArrayBegin(factors.Count);
		for (int i = 0; i < factors.Count; i++)
		{
			writer.Write(factors[i]);
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public EfficiencySection()
	{
	}
}
