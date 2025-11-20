using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Debug;
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI.Debug;

public static class DebugWidgetBuilders
{
	public static IEnumerable<IWidget> BuildWidgets(ObservableList<DebugUI.Widget> debugWidgets)
	{
		foreach (DebugUI.Widget debugWidget in debugWidgets)
		{
			if (debugWidget.isEditorOnly)
			{
				continue;
			}
			IWidget widget = TryBuildWidget(debugWidget);
			if (widget != null)
			{
				if (widget is INamed named)
				{
					named.displayName = LocalizedString.Value(debugWidget.displayName);
				}
				yield return widget;
			}
		}
	}

	[CanBeNull]
	private static IWidget TryBuildWidget(DebugUI.Widget debugWidget)
	{
		if (debugWidget is DebugUI.Foldout debugWidget2)
		{
			return BuildExpandableGroup(debugWidget2);
		}
		if (debugWidget is DebugUI.Container debugWidget3)
		{
			return BuildGroup(debugWidget3);
		}
		if (debugWidget is DebugUI.ValueTuple debugWidget4)
		{
			return BuildGroup(debugWidget4);
		}
		if (debugWidget is DebugUI.Button debugWidget5)
		{
			return BuildButton(debugWidget5);
		}
		if (debugWidget is DebugUI.Value debugWidget6)
		{
			return BuildValueField(debugWidget6);
		}
		if (debugWidget is DebugUI.BoolField debugWidget7)
		{
			return BuildToggleField(debugWidget7);
		}
		if (debugWidget is DebugUI.IntField debugWidget8)
		{
			return BuildIntField(debugWidget8);
		}
		if (debugWidget is Game.Debug.IntInputField debugWidget9)
		{
			return BuildIntInputField(debugWidget9);
		}
		if (debugWidget is DebugUI.UIntField debugWidget10)
		{
			return BuildUIntField(debugWidget10);
		}
		if (debugWidget is DebugUI.FloatField debugWidget11)
		{
			return BuildFloatField(debugWidget11);
		}
		if (debugWidget is DebugUI.Vector2Field debugWidget12)
		{
			return BuildFloat2Field(debugWidget12);
		}
		if (debugWidget is DebugUI.Vector3Field debugWidget13)
		{
			return BuildFloat3Field(debugWidget13);
		}
		if (debugWidget is DebugUI.Vector4Field debugWidget14)
		{
			return BuildFloat4Field(debugWidget14);
		}
		if (debugWidget is DebugUI.EnumField debugWidget15)
		{
			return BuildEnumField(debugWidget15);
		}
		if (debugWidget is DebugUI.BitField debugWidget16)
		{
			return BuildFlagsField(debugWidget16);
		}
		if (debugWidget is DebugUI.ColorField debugWidget17)
		{
			return BuildColorField(debugWidget17);
		}
		if (debugWidget is DebugUI.TextField debugWidget18)
		{
			return BuildStringInputField(debugWidget18);
		}
		return null;
	}

	private static ExpandableGroup BuildExpandableGroup(DebugUI.Foldout debugWidget)
	{
		return new ExpandableGroup(new DelegateAccessor<bool>(() => debugWidget.opened, delegate(bool value)
		{
			debugWidget.opened = value;
		}))
		{
			children = new List<IWidget>(BuildWidgets(debugWidget.children))
		};
	}

	private static Group BuildGroup(DebugUI.Container debugWidget)
	{
		return new Group
		{
			children = new List<IWidget>(BuildWidgets(debugWidget.children))
		};
	}

	private static Group BuildGroup(DebugUI.ValueTuple debugWidget)
	{
		List<IWidget> list = new List<IWidget>(debugWidget.values.Length);
		string[] array = (debugWidget.parent as DebugUI.Foldout)?.columnLabels;
		for (int i = 0; i < debugWidget.values.Length; i++)
		{
			list.Add(new ValueField(debugWidget.values[i])
			{
				path = i,
				displayName = ((array != null && array.Length > i) ? array[i] : string.Empty)
			});
		}
		return new Group
		{
			children = list
		};
	}

	private static Button BuildButton(DebugUI.Button debugWidget)
	{
		return new Button
		{
			action = debugWidget.action
		};
	}

	private static ValueField BuildValueField(DebugUI.Value debugWidget)
	{
		return new ValueField(debugWidget);
	}

	private static ToggleField BuildToggleField(DebugUI.BoolField debugWidget)
	{
		return new ToggleField
		{
			accessor = new DebugFieldAccessor<bool>(debugWidget)
		};
	}

	private static IntField<int> BuildIntField(DebugUI.IntField debugWidget)
	{
		int num = Invoke(debugWidget.min, int.MinValue);
		int num2 = Invoke(debugWidget.max, int.MaxValue);
		DebugFieldAccessor<int> accessor = new DebugFieldAccessor<int>(debugWidget);
		if (num > int.MinValue && num2 < int.MaxValue)
		{
			return new IntSliderField
			{
				min = num,
				max = num2,
				step = debugWidget.incStep,
				stepMultiplier = debugWidget.intStepMult,
				accessor = accessor
			};
		}
		return new IntArrowField
		{
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.intStepMult,
			accessor = accessor
		};
	}

	private static UIntField BuildUIntField(DebugUI.UIntField debugWidget)
	{
		uint num = Invoke(debugWidget.min, 0u);
		uint num2 = Invoke(debugWidget.max, uint.MaxValue);
		DebugFieldAccessor<uint> accessor = new DebugFieldAccessor<uint>(debugWidget);
		if (num != 0 && num2 < uint.MaxValue)
		{
			return new UIntSliderField
			{
				min = num,
				max = num2,
				step = debugWidget.incStep,
				stepMultiplier = debugWidget.intStepMult,
				accessor = accessor
			};
		}
		return new UIntArrowField
		{
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.intStepMult,
			accessor = accessor
		};
	}

	private static FloatField<double> BuildFloatField(DebugUI.FloatField debugWidget)
	{
		float num = Invoke(debugWidget.min, float.MinValue);
		float num2 = Invoke(debugWidget.max, float.MaxValue);
		DebugFieldCastAccessor<double, float> accessor = new DebugFieldCastAccessor<double, float>(debugWidget, (float value) => value, (double value) => (float)value);
		if (num > float.MinValue && num2 < float.MaxValue)
		{
			return new FloatSliderField
			{
				min = num,
				max = num2,
				step = debugWidget.incStep,
				stepMultiplier = debugWidget.incStepMult,
				accessor = accessor
			};
		}
		return new FloatArrowField
		{
			min = num,
			max = num2,
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.incStepMult,
			accessor = accessor
		};
	}

	private static Float2SliderField BuildFloat2Field(DebugUI.Vector2Field debugWidget)
	{
		return new Float2SliderField
		{
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.incStepMult,
			fractionDigits = debugWidget.decimals,
			accessor = new DebugFieldCastAccessor<float2, Vector2>(debugWidget, ToFloat, FromFloat)
		};
		static Vector2 FromFloat(float2 value)
		{
			return value;
		}
		static float2 ToFloat(Vector2 value)
		{
			return value;
		}
	}

	private static Float3SliderField BuildFloat3Field(DebugUI.Vector3Field debugWidget)
	{
		return new Float3SliderField
		{
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.incStepMult,
			fractionDigits = debugWidget.decimals,
			accessor = new DebugFieldCastAccessor<float3, Vector3>(debugWidget, ToFloat, FromFloat)
		};
		static Vector3 FromFloat(float3 value)
		{
			return value;
		}
		static float3 ToFloat(Vector3 value)
		{
			return value;
		}
	}

	private static Float4SliderField BuildFloat4Field(DebugUI.Vector4Field debugWidget)
	{
		return new Float4SliderField
		{
			step = debugWidget.incStep,
			stepMultiplier = debugWidget.incStepMult,
			fractionDigits = debugWidget.decimals,
			accessor = new DebugFieldCastAccessor<float4, Vector4>(debugWidget, ToFloat, FromFloat)
		};
		static Vector4 FromFloat(float4 value)
		{
			return value;
		}
		static float4 ToFloat(Vector4 value)
		{
			return value;
		}
	}

	private static EnumField BuildEnumField(DebugUI.EnumField debugWidget)
	{
		return new EnumField
		{
			enumMembers = BuildEnumMembers(debugWidget),
			accessor = new DelegateAccessor<ulong>(() => (ulong)debugWidget.GetValue(), delegate(ulong value)
			{
				debugWidget.SetValue((int)value);
			})
		};
	}

	private static FlagsField BuildFlagsField(DebugUI.BitField debugWidget)
	{
		if (!EnumFieldBuilders.GetConverters(debugWidget.enumType, out var fromObject, out var toObject))
		{
			fromObject = (object value) => (ulong)(long)value;
			toObject = (ulong value) => (long)value;
		}
		return new FlagsField
		{
			enumMembers = BuildEnumMembers(debugWidget),
			accessor = new DelegateAccessor<ulong>(() => fromObject(debugWidget.GetValue()), delegate(ulong value)
			{
				debugWidget.SetValue(toObject(value));
			})
		};
	}

	private static EnumMember[] BuildEnumMembers<T>(DebugUI.EnumField<T> debugWidget)
	{
		EnumMember[] array = new EnumMember[debugWidget.enumValues.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new EnumMember((ulong)debugWidget.enumValues[i], debugWidget.enumNames[i].text);
		}
		return array;
	}

	private static ColorField BuildColorField(DebugUI.ColorField debugWidget)
	{
		return new ColorField
		{
			hdr = debugWidget.hdr,
			showAlpha = debugWidget.showAlpha,
			accessor = new DebugFieldAccessor<Color>(debugWidget)
		};
	}

	private static StringInputField BuildStringInputField(DebugUI.TextField debugWidget)
	{
		return new StringInputField
		{
			accessor = new DebugFieldAccessor<string>(debugWidget)
		};
	}

	private static IntInputField BuildIntInputField(Game.Debug.IntInputField debugWidget)
	{
		return new IntInputField(debugWidget);
	}

	private static T Invoke<T>(Func<T> func, T fallback)
	{
		if (func == null)
		{
			return fallback;
		}
		return func();
	}
}
