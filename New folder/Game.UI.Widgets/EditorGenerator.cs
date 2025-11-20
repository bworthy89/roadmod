using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Annotations;
using Colossal.OdinSerializer;
using Colossal.OdinSerializer.Utilities;
using Colossal.Reflection;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Editor;
using Game.UI.Localization;
using UnityEngine;

namespace Game.UI.Widgets;

public class EditorGenerator : IEditorGenerator
{
	public static readonly List<IFieldBuilderFactory> kFactories = new List<IFieldBuilderFactory>
	{
		new CustomFieldBuilders(),
		new ToggleFieldBuilders(),
		new IntFieldBuilders(),
		new UIntFieldBuilders(),
		new TimeFieldBuilders(),
		new FloatFieldBuilders(),
		new BoundsFieldBuilders(),
		new BezierFieldBuilders(),
		new StringFieldBuilders(),
		new ColorFieldBuilders(),
		new AnimationCurveFieldBuilders(),
		new EnumFieldBuilders(),
		new PopupValueFieldBuilders()
	};

	private static readonly CustomSerializationPolicy kMemberFilter = new CustomSerializationPolicy("EditorGenerator", allowNonSerializableTypes: true, (MemberInfo member) => member is FieldInfo { IsPublic: not false } fieldInfo && !fieldInfo.IsDefined<NonSerializedAttribute>(inherit: true) && !fieldInfo.IsDefined<HideInInspector>() && !fieldInfo.IsDefined<HideInEditorAttribute>() && fieldInfo.Name != "active" && fieldInfo.Name != "dirty" && fieldInfo.Name != "m_Dirty" && (!typeof(PrefabBase).IsAssignableFrom(fieldInfo.DeclaringType) || fieldInfo.Name != "components"));

	public int maxLevel { get; set; } = 5;

	public static bool sBypassValueLimits { get; set; }

	public IWidget Build(IValueAccessor accessor, object[] attributes, int level, string path)
	{
		if (level > maxLevel)
		{
			return new ValueField
			{
				accessor = new ObjectAccessor<string>(string.Empty)
			};
		}
		return BuildMemberImpl(accessor, attributes, level, path);
	}

	[CanBeNull]
	private IWidget TryBuildField(IValueAccessor accessor, object[] attributes, string path)
	{
		FieldBuilder fieldBuilder = TryCreateFieldBuilder(accessor.valueType, attributes);
		if (fieldBuilder != null)
		{
			IWidget widget = fieldBuilder(accessor);
			widget.path = path;
			return widget;
		}
		return null;
	}

	[CanBeNull]
	private FieldBuilder TryCreateFieldBuilder(Type memberType, object[] attributes)
	{
		foreach (IFieldBuilderFactory kFactory in kFactories)
		{
			FieldBuilder fieldBuilder = kFactory.TryCreate(memberType, attributes);
			if (fieldBuilder != null)
			{
				return fieldBuilder;
			}
		}
		return null;
	}

	[CanBeNull]
	public PagedList TryBuildList(IValueAccessor accessor, int level, string path, object[] attributes)
	{
		IListAdapter listAdapter = TryBuildListAdapter(accessor, level, path, attributes);
		if (listAdapter != null)
		{
			return new PagedList
			{
				adapter = listAdapter,
				level = level,
				path = path
			};
		}
		return null;
	}

	public IListAdapter TryBuildListAdapter(IValueAccessor accessor, int level, string path, object[] attributes)
	{
		if (WidgetReflectionUtils.IsListType(accessor.valueType))
		{
			Type listElementType = WidgetReflectionUtils.GetListElementType(accessor.valueType);
			if (listElementType != null)
			{
				bool flag = attributes.Any((object attr) => attr is FixedLengthAttribute);
				MemberInfo listElementLabelMember = WidgetReflectionUtils.GetListElementLabelMember(listElementType);
				if (accessor.valueType.IsArray)
				{
					return new ArrayAdapter
					{
						accessor = new CastAccessor<Array>(accessor),
						elementType = listElementType,
						generator = this,
						level = level,
						path = path,
						resizable = !flag,
						attributes = attributes,
						labelMember = listElementLabelMember
					};
				}
				return new ListAdapter
				{
					accessor = new CastAccessor<IList>(accessor),
					listType = accessor.valueType,
					elementType = listElementType,
					generator = this,
					level = level,
					path = path,
					resizable = !flag,
					attributes = attributes,
					labelMember = listElementLabelMember
				};
			}
		}
		return null;
	}

	[CanBeNull]
	private ExpandableGroup TryBuildGroup(IValueAccessor accessor, int level, string path)
	{
		if (accessor.valueType.IsSerializable && !typeof(ComponentBase).IsAssignableFrom(accessor.valueType))
		{
			return new ExpandableGroup
			{
				path = path,
				children = BuildMembers(accessor, level, path).ToArray()
			};
		}
		return null;
	}

	[NotNull]
	public IEnumerable<IWidget> BuildMembers(IValueAccessor accessor, int level, string parentPath)
	{
		List<MemberInfo> list = new List<MemberInfo>();
		list.AddRange(GetSpecialMembers(accessor.valueType));
		list.AddRange(FormatterUtilities.GetSerializableMembers(accessor.valueType, kMemberFilter));
		return list.Select((MemberInfo member) => BuildMember(accessor, member, level, parentPath));
	}

	private MemberInfo[] GetSpecialMembers(Type type)
	{
		if (type.InheritsFrom(typeof(PrefabBase)))
		{
			return typeof(UnityEngine.Object).GetMember("name", MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public);
		}
		return Array.Empty<MemberInfo>();
	}

	public static T NamedWidget<T>(T widget, LocalizedString displayName, LocalizedString tooltip) where T : IWidget
	{
		if (widget is INamed named)
		{
			named.displayName = displayName;
		}
		if (widget is ITooltipTarget tooltipTarget)
		{
			tooltipTarget.tooltip = tooltip;
		}
		return widget;
	}

	[NotNull]
	private IWidget BuildMember(IValueAccessor parent, MemberInfo member, int level, string parentPath)
	{
		IValueAccessor accessor = ValueAccessorUtils.CreateMemberAccessor(parent, member);
		IWidget widget = BuildMemberImpl(accessor, member.GetCustomAttributes(inherit: false), level, parentPath + "." + member.Name);
		if (widget is INamed named)
		{
			InspectorNameAttribute attribute = member.GetAttribute<InspectorNameAttribute>();
			EditorNameAttribute attribute2 = member.GetAttribute<EditorNameAttribute>();
			string text = ((attribute2 != null) ? attribute2.displayName : ((attribute == null) ? WidgetReflectionUtils.NicifyVariableName(member.Name) : attribute.displayName));
			named.displayName = LocalizedString.IdWithFallback(text, text);
		}
		if (widget is ITooltipTarget tooltipTarget)
		{
			tooltipTarget.tooltip = (member.TryGetAttribute<TooltipAttribute>(out var attribute3) ? LocalizedString.IdWithFallback(GetMemberTooltipLocaleId(member), attribute3.tooltip) : LocalizedString.Id(GetMemberTooltipLocaleId(member)));
		}
		return widget;
	}

	[NotNull]
	private IWidget BuildMemberImpl(IValueAccessor accessor, object[] attributes, int level, string path)
	{
		IWidget widget = TryBuildField(accessor, attributes, path);
		if (widget == null)
		{
			widget = TryBuildList(accessor, level, path, attributes);
		}
		if (widget == null)
		{
			widget = TryBuildGroup(accessor, level, path);
		}
		if (widget == null)
		{
			widget = BuildUnknownMember(accessor.valueType);
		}
		return widget;
	}

	private ValueField BuildUnknownMember(Type memberType)
	{
		string name = memberType.Name;
		return new ValueField
		{
			accessor = new ObjectAccessor<string>(name)
		};
	}

	private static string GetMemberTooltipLocaleId(MemberInfo member)
	{
		string text = ((member.DeclaringType != null) ? (member.DeclaringType.TypeName(includeNamespace: true) + "." + member.Name) : member.Name);
		return "Editor.TOOLTIP[" + text + "]";
	}
}
