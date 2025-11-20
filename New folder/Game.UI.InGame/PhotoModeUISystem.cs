using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colossal.Collections.Generic;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.Achievements;
using Game.Common;
using Game.Input;
using Game.Reflection;
using Game.Rendering;
using Game.Rendering.CinematicCamera;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class PhotoModeUISystem : UISystemBase
{
	public class Tab
	{
		public string id { get; set; }

		public string icon { get; set; }

		public List<IWidget> items { get; set; } = new List<IWidget>();

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("photoMode.Tab");
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.TypeEnd();
		}
	}

	public const string kGroup = "photoMode";

	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private RenderingSystem m_RenderingSystem;

	private PlanetarySystem m_PlanetarySystem;

	private PhotoModeRenderSystem m_PhotoModeRenderSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private CinematicCameraUISystem m_CinematicCameraUISystem;

	private BulldozeToolSystem m_BulldozeTool;

	private InputBarrier m_ToolBarrier;

	private ValueBinding<bool> m_OverlayHiddenBinding;

	private GetterValueBinding<bool> m_OrbitCameraActiveBinding;

	private GetterValueBinding<float> m_FieldOfViewBinding;

	private GetterValueBinding<float> m_TimeOfDayBinding;

	private GetterValueBinding<float> m_SaturationBinding;

	private RawMapBinding<Entity> m_AdjustmentCategoriesBinding;

	private bool m_TimeOfDayChanged;

	private ValueBinding<bool> m_CinematicCameraVisibleBinding;

	private ValueBinding<string> m_ActiveTabBinding;

	private RawValueBinding m_TabNamesBinding;

	private WidgetBindings m_WidgetBindings;

	public bool orbitMode { get; set; }

	private List<Tab> tabs { get; set; } = new List<Tab>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_PhotoModeRenderSystem = base.World.GetOrCreateSystemManaged<PhotoModeRenderSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_CinematicCameraUISystem = base.World.GetOrCreateSystemManaged<CinematicCameraUISystem>();
		m_BulldozeTool = base.World.GetOrCreateSystemManaged<BulldozeToolSystem>();
		m_ToolBarrier = InputManager.instance.CreateMapBarrier("Tool", "PhotoModeUISystem");
		tabs = BuildProperties();
		InjectPresets();
		AddUpdateBinding(m_WidgetBindings = new WidgetBindings("photoMode"));
		m_WidgetBindings.AddDefaultBindings();
		AddBinding(new TriggerBinding<string>("photoMode", "selectTab", SelectTab));
		AddBinding(new TriggerBinding<bool>("photoMode", "setCinematicCameraVisible", SetCinematicCameraVisible));
		AddBinding(m_CinematicCameraVisibleBinding = new ValueBinding<bool>("photoMode", "cinematicCameraVisible", initialValue: false));
		AddBinding(m_ActiveTabBinding = new ValueBinding<string>("photoMode", "activeTab", string.Empty));
		AddBinding(m_TabNamesBinding = new RawValueBinding("photoMode", "tabs", BindTabNames));
		SelectTab("Camera");
		AddBinding(m_OverlayHiddenBinding = new ValueBinding<bool>("photoMode", "overlayHidden", initialValue: false));
		AddBinding(m_OrbitCameraActiveBinding = new GetterValueBinding<bool>("photoMode", "orbitCameraActive", () => m_CameraUpdateSystem.activeCameraController is OrbitCameraController));
		AddBinding(new TriggerBinding<bool>("photoMode", "setOverlayHidden", SetOverlayHidden));
		AddBinding(new TriggerBinding("photoMode", "takeScreenshot", TakeScreenshot));
		AddBinding(new TriggerBinding("photoMode", "toggleOrbitCameraActive", ToggleOrbitCameraActive));
	}

	public void Activate(bool enabled)
	{
		if (enabled)
		{
			orbitMode = m_CameraUpdateSystem.activeCameraController == m_CameraUpdateSystem.orbitCameraController;
			if (m_CameraUpdateSystem.activeCameraController == m_CameraUpdateSystem.gamePlayController)
			{
				m_CameraUpdateSystem.cinematicCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
				m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.cinematicCameraController;
			}
			m_CameraUpdateSystem.orbitCameraController.mode = OrbitCameraController.Mode.PhotoMode;
			m_CameraUpdateSystem.orbitCameraController.collisionsEnabled = false;
			m_CameraUpdateSystem.cinematicCameraController.collisionsEnabled = false;
		}
		else
		{
			if (m_CameraUpdateSystem.activeCameraController != m_CameraUpdateSystem.orbitCameraController || m_CameraUpdateSystem.orbitCameraController.followedEntity == Entity.Null)
			{
				m_CameraUpdateSystem.gamePlayController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
				m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.gamePlayController;
			}
			m_CameraUpdateSystem.orbitCameraController.mode = OrbitCameraController.Mode.Follow;
			m_CameraUpdateSystem.orbitCameraController.collisionsEnabled = true;
			m_PhotoModeRenderSystem.DisableAllCameraProperties();
		}
		m_ToolBarrier.blocked = enabled;
		m_PhotoModeRenderSystem.Enable(enabled);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (m_OverlayHiddenBinding.value)
		{
			if (m_ToolSystem.activeTool != m_BulldozeTool)
			{
				m_RenderingSystem.hideOverlay = true;
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.FreeCameraDisable;
				m_ToolSystem.activeTool = m_DefaultToolSystem;
			}
			else
			{
				m_ToolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
				m_OverlayHiddenBinding.Update(newValue: false);
			}
		}
		if (m_TimeOfDayChanged)
		{
			m_TimeOfDayChanged = false;
			m_PlanetarySystem.Update();
		}
	}

	private void SetOverlayHidden(bool overlayHidden)
	{
		m_RenderingSystem.hideOverlay = overlayHidden;
		if (overlayHidden)
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.FreeCameraDisable;
			m_ToolSystem.activeTool = m_DefaultToolSystem;
		}
		else
		{
			m_ToolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
		}
		m_OverlayHiddenBinding.Update(overlayHidden);
	}

	private void BindTabNames(IJsonWriter writer)
	{
		writer.ArrayBegin(tabs.Count);
		foreach (Tab tab in tabs)
		{
			tab.Write(writer);
		}
		writer.ArrayEnd();
	}

	private void SetCinematicCameraVisible(bool visible)
	{
		m_CinematicCameraVisibleBinding.Update(visible);
	}

	private void SelectTab(string tabID)
	{
		Tab tab = tabs.Find((Tab tab2) => tab2.id == tabID);
		if (tab != null)
		{
			m_ActiveTabBinding.Update(tab.id);
			m_WidgetBindings.children = tab.items;
		}
	}

	private List<Tab> BuildProperties()
	{
		HashSet<string> handledGroupsCache = new HashSet<string>();
		OrderedDictionary<string, Tab> orderedDictionary = new OrderedDictionary<string, Tab>();
		foreach (KeyValuePair<string, PhotoModeProperty> photoModeProperty in m_PhotoModeRenderSystem.photoModeProperties)
		{
			if (!orderedDictionary.TryGetValue(photoModeProperty.Value.group, out var value))
			{
				value = new Tab
				{
					id = photoModeProperty.Value.group,
					icon = "Media/PhotoMode/" + photoModeProperty.Value.group + ".svg",
					items = new List<IWidget>()
				};
				orderedDictionary.Add(photoModeProperty.Value.group, value);
			}
			if (CheckMultiPropertyHandled(handledGroupsCache, photoModeProperty.Value))
			{
				value.items.Add(BuildControl(photoModeProperty.Value));
			}
		}
		return orderedDictionary.Values.ToList();
	}

	private void InjectPresets()
	{
		foreach (PhotoModeUIPreset preset in m_PhotoModeRenderSystem.presets)
		{
			InjectPreset(preset);
		}
	}

	private void InjectPreset(PhotoModeUIPreset preset)
	{
		Tab tab = tabs.Find((Tab tab2) => tab2.id == preset.injectionProperty.group);
		if (tab == null)
		{
			return;
		}
		DelegateAccessor<int> accessor = new DelegateAccessor<int>(delegate
		{
			int num2 = -1;
			bool flag = false;
			foreach (KeyValuePair<PhotoModeProperty, float[]> value in preset.descriptor.values)
			{
				if (flag && num2 == -1)
				{
					return num2;
				}
				num2 = -1;
				flag = true;
				for (int i = 0; i < value.Value.Length; i++)
				{
					if (value.Key.getValue() == value.Value[i])
					{
						if (num2 != -1 && num2 != i)
						{
							return -1;
						}
						num2 = i;
					}
				}
			}
			return num2;
		}, delegate(int value)
		{
			foreach (KeyValuePair<PhotoModeProperty, float[]> value2 in preset.descriptor.values)
			{
				if (value >= 0)
				{
					value2.Key.setValue(value2.Value[value]);
				}
			}
		});
		List<DropdownItem<int>> list = new List<DropdownItem<int>>();
		list.Add(new DropdownItem<int>
		{
			value = -1,
			displayName = "PhotoMode.SENSORTYPE[Custom]"
		});
		int num = 0;
		foreach (string item in preset.descriptor.optionsId)
		{
			list.Add(new DropdownItem<int>
			{
				value = num++,
				displayName = item
			});
		}
		int index = tab.items.FindIndex((IWidget x) => (x as NamedWidget)?.displayName.value == PhotoModeUtils.ExtractPropertyID(preset.injectionProperty));
		tab.items.Insert(index, BuildDropdownGroup(preset.id, list, accessor));
	}

	private static bool CheckMultiPropertyHandled(HashSet<string> handledGroupsCache, PhotoModeProperty property)
	{
		int num = property.id.IndexOf("/");
		if (num >= 0)
		{
			string item = property.id.Substring(0, num);
			if (handledGroupsCache.Contains(item))
			{
				return false;
			}
			handledGroupsCache.Add(item);
			return true;
		}
		return true;
	}

	private Group BuildGroupTitle(PhotoModeProperty property)
	{
		return new Group
		{
			displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
			tooltip = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", string.Empty),
			tooltipPos = Group.TooltipPosition.Title
		};
	}

	private Group BuildDropdownGroup(string groupName, List<DropdownItem<int>> items, DelegateAccessor<int> accessor)
	{
		Group obj = new Group();
		obj.displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + groupName + "]", groupName);
		obj.tooltip = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + groupName + "]", string.Empty);
		obj.children = new IWidget[1]
		{
			new DropdownField<int>
			{
				displayName = groupName + "Dropdown",
				accessor = accessor,
				items = items.ToArray()
			}
		};
		return obj;
	}

	private Group BuildEnumGroup(PhotoModeProperty property, bool multiPropertyComponent = false)
	{
		bool flag = property.isEnabled != null && property.setEnabled != null;
		List<IWidget> list = new List<IWidget>();
		AddCommonFields(list, property, multiPropertyComponent);
		list.Add(new EnumField
		{
			displayName = property.id + "Dropdown",
			disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false))),
			enumMembers = AutomaticSettings.GetEnumValues(property.enumType, "PhotoMode"),
			accessor = new DelegateAccessor<ulong>(() => (ulong)Mathf.RoundToInt(property.getValue()), delegate(ulong value)
			{
				property.setValue(value);
			})
		});
		if (!multiPropertyComponent && property.reset != null)
		{
			list.Add(new IconButton
			{
				icon = "Media/Glyphs/ArrowCircular.svg",
				tooltip = LocalizedString.Id("PhotoMode.RESET_PROPERTY_TOOLTIP"),
				action = property.reset
			});
		}
		return new Group
		{
			displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
			tooltip = ((!multiPropertyComponent) ? LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", string.Empty) : ((LocalizedString)null)),
			tooltipPos = Group.TooltipPosition.Title,
			children = list
		};
	}

	private Group BuildValueGroup(PhotoModeProperty property, bool multiPropertyComponent = false)
	{
		bool flag = property.setEnabled != null && property.isEnabled != null;
		List<IWidget> list = new List<IWidget>();
		if (property.fractionDigits > 0)
		{
			list.Add(new FloatInputField
			{
				displayName = property.id + " Value",
				dynamicMin = ((property.min != null) ? ((Func<double>)(() => property.min())) : null),
				dynamicMax = ((property.max != null) ? ((Func<double>)(() => property.max())) : null),
				fractionDigits = property.fractionDigits,
				disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false))),
				accessor = new DelegateAccessor<double>(() => property.getValue(), delegate(double value)
				{
					property.setValue((float)value);
				})
			});
		}
		else
		{
			list.Add(new IntInputField
			{
				displayName = property.id + " Value",
				dynamicMin = ((property.min != null) ? ((Func<int>)(() => Mathf.RoundToInt(property.min()))) : null),
				dynamicMax = ((property.max != null) ? ((Func<int>)(() => Mathf.RoundToInt(property.max()))) : null),
				disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false))),
				accessor = new DelegateAccessor<int>(() => Mathf.RoundToInt(property.getValue()), delegate(int value)
				{
					property.setValue(value);
				})
			});
		}
		AddCommonFields(list, property, multiPropertyComponent);
		if (property.fractionDigits > 0)
		{
			list.Add(new FloatSliderField
			{
				displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
				tooltip = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", string.Empty),
				dynamicMin = ((property.min != null) ? ((Func<double>)(() => property.min())) : ((Func<double>)(() => double.NegativeInfinity))),
				dynamicMax = ((property.max != null) ? ((Func<double>)(() => property.max())) : ((Func<double>)(() => double.PositiveInfinity))),
				disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false))),
				accessor = new DelegateAccessor<double>(() => property.getValue(), delegate(double value)
				{
					property.setValue((float)value);
				}),
				fractionDigits = property.fractionDigits
			});
		}
		else
		{
			list.Add(new IntSliderField
			{
				displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
				tooltip = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", string.Empty),
				dynamicMin = ((property.min != null) ? ((Func<int>)(() => Mathf.RoundToInt(property.min()))) : ((Func<int>)(() => Mathf.RoundToInt(float.NegativeInfinity)))),
				dynamicMax = ((property.max != null) ? ((Func<int>)(() => Mathf.RoundToInt(property.max()))) : ((Func<int>)(() => Mathf.RoundToInt(float.PositiveInfinity)))),
				disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false))),
				accessor = new DelegateAccessor<int>(() => Mathf.RoundToInt(property.getValue()), delegate(int value)
				{
					property.setValue(value);
				})
			});
		}
		if (!multiPropertyComponent && property.reset != null)
		{
			list.Add(new IconButton
			{
				icon = "Media/Glyphs/ArrowCircular.svg",
				tooltip = LocalizedString.Id("PhotoMode.RESET_PROPERTY_TOOLTIP"),
				action = property.reset
			});
		}
		return new Group
		{
			displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
			tooltip = ((!multiPropertyComponent) ? LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", string.Empty) : ((LocalizedString)null)),
			tooltipPos = Group.TooltipPosition.Title,
			children = list
		};
	}

	private void AddCommonFields(IList<IWidget> children, PhotoModeProperty property, bool multiPropertyComponent)
	{
		if (multiPropertyComponent)
		{
			return;
		}
		if (property.isEnabled != null && property.setEnabled != null)
		{
			children.Add(new ToggleField
			{
				displayName = property.id + "EnableToggle",
				tooltip = "PhotoMode.ENABLE_PROPERTY_TOOLTIP",
				accessor = new DelegateAccessor<bool>(property.isEnabled, property.setEnabled),
				disabled = () => property.isAvailable != null && !property.isAvailable(),
				tutorialTag = "UITagPrefab:PhotoModePropertyEnableCheckbox"
			});
		}
		children.Add(new IconButton
		{
			icon = "Media/PhotoMode/AddKeyframe.svg",
			tooltip = "PhotoMode.CAPTURE_PROPERTY_TOOLTIP",
			disabled = () => !m_CinematicCameraVisibleBinding.value || (property.isEnabled != null && !property.isEnabled()),
			action = delegate
			{
				m_CinematicCameraUISystem.ToggleModifier(property);
			},
			tutorialTag = "UITagPrefab:PhotoModePropertyKeyframeButton"
		});
	}

	private IWidget BuildColorGroup(PhotoModeProperty property, IDictionary<string, PhotoModeProperty> allProperties)
	{
		PhotoModeProperty[] source = PhotoModeUtils.ExtractMultiPropertyComponents(property, allProperties).ToArray();
		PhotoModeProperty r = source.FirstOrDefault((PhotoModeProperty c) => c.id.EndsWith("/r"));
		PhotoModeProperty g = source.FirstOrDefault((PhotoModeProperty c) => c.id.EndsWith("/g"));
		PhotoModeProperty b = source.FirstOrDefault((PhotoModeProperty c) => c.id.EndsWith("/b"));
		PhotoModeProperty a = source.FirstOrDefault((PhotoModeProperty c) => c.id.EndsWith("/a"));
		Func<Color> getter = () => new Color(r?.getValue() ?? 0f, g?.getValue() ?? 0f, b?.getValue() ?? 0f, a?.getValue() ?? 1f);
		Action<Color> setter = delegate(Color c)
		{
			r?.setValue(c.r);
			g?.setValue(c.g);
			b?.setValue(c.b);
			a?.setValue(c.a);
		};
		return new ColorField
		{
			accessor = new DelegateAccessor<Color>(getter, setter),
			disabled = () => property.isEnabled != null && !property.isEnabled(),
			showAlpha = (a != null),
			hdr = (property.max == null)
		};
	}

	private Group BuildCheckboxGroup(PhotoModeProperty property, bool multiPropertyComponent = false)
	{
		bool flag = property.setEnabled != null && property.isEnabled != null;
		List<IWidget> list = new List<IWidget>();
		AddCommonFields(list, property, multiPropertyComponent);
		list.Add(new ToggleField
		{
			displayName = property.id,
			accessor = new DelegateAccessor<bool>(() => Mathf.RoundToInt(property.getValue()) != 0, delegate(bool value)
			{
				property.setValue(value ? 1f : 0f);
			}),
			disabled = (flag ? ((Func<bool>)(() => !property.isEnabled())) : ((Func<bool>)(() => false)))
		});
		if (!multiPropertyComponent && property.reset != null)
		{
			list.Add(new IconButton
			{
				icon = "Media/Glyphs/ArrowCircular.svg",
				tooltip = LocalizedString.Id("PhotoMode.RESET_PROPERTY_TOOLTIP"),
				action = property.reset
			});
		}
		return new Group
		{
			displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + property.id + "]", property.id),
			tooltip = ((!multiPropertyComponent) ? LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + property.id + "]", property.id) : ((LocalizedString)null)),
			tooltipPos = Group.TooltipPosition.Title,
			children = list
		};
	}

	private Group BuildMultiPropertyGroup(PhotoModeProperty property, IDictionary<string, PhotoModeProperty> allProperties)
	{
		string text = property.id.Substring(0, property.id.IndexOf("/"));
		List<IWidget> list = new List<IWidget>();
		AddCommonFields(list, property, multiPropertyComponent: false);
		if (property.overrideControl == PhotoModeProperty.OverrideControl.ColorField)
		{
			list.Add(BuildColorGroup(property, allProperties));
			if (property.reset != null)
			{
				list.Add(new IconButton
				{
					icon = "Media/Glyphs/ArrowCircular.svg",
					tooltip = LocalizedString.Id("PhotoMode.RESET_PROPERTY_TOOLTIP"),
					action = property.reset
				});
			}
		}
		else
		{
			if (property.reset != null)
			{
				list.Add(new IconButton
				{
					icon = "Media/Glyphs/ArrowCircular.svg",
					tooltip = LocalizedString.Id("PhotoMode.RESET_PROPERTY_TOOLTIP"),
					action = property.reset
				});
			}
			foreach (PhotoModeProperty item in PhotoModeUtils.ExtractMultiPropertyComponents(property, allProperties))
			{
				list.Add(BuildControl(item, multiPropertyComponent: true));
			}
		}
		return new Group
		{
			displayName = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TITLE[" + text + "]", text),
			tooltip = LocalizedString.IdWithFallback("PhotoMode.PROPERTY_TOOLTIP[" + text + "]", string.Empty),
			tooltipPos = Group.TooltipPosition.Title,
			children = list
		};
	}

	private IWidget BuildControl(PhotoModeProperty property, bool multiPropertyComponent = false)
	{
		if (!multiPropertyComponent && property.id.IndexOf("/") >= 0)
		{
			return BuildMultiPropertyGroup(property, m_PhotoModeRenderSystem.photoModeProperties);
		}
		if (property.setValue == null && property.getValue == null)
		{
			return BuildGroupTitle(property);
		}
		if (property.overrideControl == PhotoModeProperty.OverrideControl.Checkbox)
		{
			return BuildCheckboxGroup(property, multiPropertyComponent);
		}
		if (property.enumType != null)
		{
			return BuildEnumGroup(property, multiPropertyComponent);
		}
		return BuildValueGroup(property, multiPropertyComponent);
	}

	private void TakeScreenshot()
	{
		PlatformManager.instance.UnlockAchievement(Game.Achievements.Achievements.Snapshot);
		GameManager.instance.StartCoroutine(CaptureScreenshot());
	}

	private void ToggleOrbitCameraActive()
	{
		if (m_CameraUpdateSystem.activeCameraController is OrbitCameraController)
		{
			orbitMode = false;
			m_CameraUpdateSystem.cinematicCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.cinematicCameraController;
		}
		else
		{
			orbitMode = true;
			m_CameraUpdateSystem.orbitCameraController.followedEntity = Entity.Null;
			m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
		}
		m_OrbitCameraActiveBinding.Update();
	}

	private static IEnumerator CaptureScreenshot()
	{
		UserInterface ui = GameManager.instance.userInterface;
		if (ui != null)
		{
			ui.view.enabled = false;
		}
		yield return new WaitForEndOfFrame();
		PlatformManager.instance.TakeScreenshot();
		if (ui != null)
		{
			ui.view.enabled = true;
		}
	}

	[Preserve]
	public PhotoModeUISystem()
	{
	}
}
