using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Colossal;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;
using Colossal.UI;
using Game.Prefabs;
using Game.PSI;
using Game.Reflection;
using Game.SceneFlow;
using Game.UI.Editor;
using Game.UI.Editor.Widgets;
using Game.UI.Localization;
using Game.UI.Widgets;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

public class AssetUploadPanelUISystem : UISystemBase
{
	private enum State
	{
		Ready,
		Processing,
		Success,
		Failure,
		Disabled
	}

	private static readonly string kNotificationID = "AssetUpload";

	private static readonly LocalizedString kNone = LocalizedString.Id("Editor.NONE_VALUE");

	private static readonly string kFailureLabel = "Menu.ASSET_FAILURE";

	private static readonly string kSubmittingLabel = "Menu.ASSET_SUBMITTING";

	private static readonly string kCompleteLabel = "Menu.ASSET_COMPLETE";

	private static readonly string kSubmitLabel = "Common.SUBMIT";

	private static readonly string kNoInternetConnectionLabel = "Paradox.NO_INTERNET_CONNECTION";

	private static readonly string kNotLoggedInLabel = "Paradox.NOT_LOGGED_IN";

	private static readonly string kNoSocialProfile = "Paradox.NO_SOCIAL_PROFILE";

	private static readonly string kOpenProfilePage = "Paradox.OPEN_PROFILE_PAGE";

	private static readonly string kDLCListLabel = "Paradox.DLC_LIST_LABEL";

	private static readonly float kNotificationDelay = 4f;

	private NotificationUISystem m_NotificationUISystem;

	private AssetPickerAdapter m_PreviewPickerAdapter;

	private PdxAssetUploadHandle m_UploadHandle = new PdxAssetUploadHandle();

	private bool m_AllowManualFileCopy;

	private State m_State;

	private IWidget[] m_MainPanel;

	private LargeIconButton m_PreviewPickerButton;

	private ExternalLinkField m_ExternalLinkField;

	private IconButtonGroup m_Screenshots;

	private LayoutContainer m_PlatformResult;

	private ItemPickerPopup<int> m_ExistingModPopup;

	private PopupValueField<int> m_ExistingModField;

	private Button m_SubmitButton;

	[CanBeNull]
	private ListField m_AssetList;

	[CanBeNull]
	private PopupValueField<PrefabBase> m_AssetListPopup;

	private ListField m_TagsList;

	private PopupValueField<string> m_TagsListPopup;

	private LayoutContainer m_DLCInfo;

	private bool m_DLCInfoVisible;

	private IWidget[] m_PreviewPickerPanel;

	private Button m_SelectPreviewButton;

	public Action<IList<IWidget>> onChildrenChange;

	private IList<IWidget> m_Children;

	private bool nameError => string.IsNullOrEmpty(m_UploadHandle.modInfo.m_DisplayName);

	private bool shortDescirptionError => string.IsNullOrEmpty(m_UploadHandle.modInfo.m_ShortDescription);

	private bool longDescipriontError => string.IsNullOrEmpty(m_UploadHandle.modInfo.m_LongDescription);

	private bool forumLinkError => !AssetUploadUtils.ValidateForumLink(m_UploadHandle.modInfo.m_ForumLink);

	private bool externalLinkError => !AssetUploadUtils.ValidateExternalLinks(m_UploadHandle.modInfo.m_ExternalLinks);

	private bool changelogError
	{
		get
		{
			if (m_UploadHandle.updateExisting)
			{
				return string.IsNullOrWhiteSpace(m_UploadHandle.modInfo.m_Changelog);
			}
			return false;
		}
	}

	private bool versionError
	{
		get
		{
			if (m_UploadHandle.updateExisting)
			{
				return string.IsNullOrWhiteSpace(m_UploadHandle.modInfo.m_UserModVersion);
			}
			return false;
		}
	}

	private bool noInternetConnection => !PlatformManager.instance.hasConnectivity;

	private bool notLoggedIn => !m_UploadHandle.LoggedIn();

	private bool anyError
	{
		get
		{
			if (!nameError && !shortDescirptionError && !longDescipriontError && !forumLinkError && !externalLinkError && !changelogError && !versionError && !noInternetConnection)
			{
				return notLoggedIn;
			}
			return true;
		}
	}

	private bool disableSubmit
	{
		get
		{
			if (m_State != State.Processing && m_State != State.Success && m_State != State.Disabled)
			{
				return anyError;
			}
			return true;
		}
	}

	public IList<IWidget> children => m_Children;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NotificationUISystem = base.World.GetOrCreateSystemManaged<NotificationUISystem>();
		m_ExistingModPopup = new ItemPickerPopup<int>(hasFooter: false);
		IWidget[] array = new IWidget[1];
		Scrollable scrollable = new Scrollable();
		IWidget[] array2 = new IWidget[16];
		LargeIconButton obj = new LargeIconButton
		{
			action = delegate
			{
				OpenPreviewPickerPanel(SetPreview, m_UploadHandle.preview?.id ?? default(Identifier));
			}
		};
		LargeIconButton largeIconButton = obj;
		m_PreviewPickerButton = obj;
		array2[0] = largeIconButton;
		Group obj2 = new Group
		{
			displayName = kDLCListLabel
		};
		Group obj3 = obj2;
		IWidget[] array3 = new IWidget[1];
		Column obj4 = new Column
		{
			flex = new FlexLayout(1f, 0f, -1),
			children = new List<IWidget>()
		};
		LayoutContainer layoutContainer = obj4;
		m_DLCInfo = obj4;
		array3[0] = layoutContainer;
		obj3.children = array3;
		obj2.hidden = () => !m_DLCInfoVisible;
		array2[1] = obj2;
		array2[2] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_NAME",
			errorMessage = "Menu.ASSET_ERROR_NAME",
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_DisplayName, delegate(string value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_DisplayName = value;
				m_UploadHandle.modInfo = modInfo;
			}),
			error = () => nameError,
			maxLength = 60
		};
		array2[3] = new ToggleField
		{
			displayName = "Menu.ASSET_UPDATE_EXISTING",
			accessor = new DelegateAccessor<bool>(() => m_UploadHandle.updateExisting, delegate(bool value)
			{
				m_UploadHandle.updateExisting = value;
			})
		};
		PopupValueField<int> obj5 = new PopupValueField<int>
		{
			displayName = "Menu.ASSET_EXISTING",
			accessor = new DelegateAccessor<int>(() => m_UploadHandle.modInfo.m_PublishedID, delegate(int value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_PublishedID = value;
				m_UploadHandle.modInfo = modInfo;
				SetInfoFromExisting();
			}),
			hidden = () => !m_UploadHandle.updateExisting || m_UploadHandle.authorMods.Count == 0,
			popup = m_ExistingModPopup
		};
		PopupValueField<int> popupValueField = obj5;
		m_ExistingModField = obj5;
		array2[4] = popupValueField;
		array2[5] = new IntInputField
		{
			displayName = "Menu.ASSET_EXISTING_ID",
			hidden = () => !m_UploadHandle.updateExisting || m_UploadHandle.authorMods.Count > 0,
			min = 0,
			accessor = new DelegateAccessor<int>(() => m_UploadHandle.modInfo.m_PublishedID, delegate(int value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_PublishedID = value;
				m_UploadHandle.modInfo = modInfo;
			})
		};
		array2[6] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_VERSION",
			errorMessage = "Menu.ASSET_ERROR_VERSION",
			error = () => versionError,
			hidden = () => !m_UploadHandle.updateExisting,
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_UserModVersion, delegate(string value)
			{
				if (value.All((char c) => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))
				{
					IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
					modInfo.m_UserModVersion = value;
					m_UploadHandle.modInfo = modInfo;
				}
			}),
			maxLength = 20
		};
		array2[7] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_CHANGELOG",
			errorMessage = "Menu.ASSET_ERROR_EMPTY_CHANGELOG",
			error = () => changelogError,
			multiline = StringInputField.kDefaultMultilines,
			hidden = () => !m_UploadHandle.updateExisting,
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_Changelog, delegate(string value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_Changelog = value;
				m_UploadHandle.modInfo = modInfo;
			}),
			maxLength = 20000
		};
		array2[8] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_SHORT_DESCRIPTION",
			errorMessage = "Menu.ASSET_ERROR_DESCRIPTION",
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_ShortDescription, delegate(string value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_ShortDescription = value;
				m_UploadHandle.modInfo = modInfo;
			}),
			error = () => shortDescirptionError,
			maxLength = 200
		};
		array2[9] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_LONG_DESCRIPTION",
			errorMessage = "Menu.ASSET_ERROR_DESCRIPTION",
			multiline = StringInputField.kDefaultMultilines,
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_LongDescription, delegate(string value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_LongDescription = value;
				m_UploadHandle.modInfo = modInfo;
			}),
			error = () => longDescipriontError,
			maxLength = 20000
		};
		array2[10] = new StringInputFieldWithError
		{
			displayName = "Menu.ASSET_FORUM_LINK_LABEL",
			errorMessage = "Menu.ASSET_ERROR_LINK",
			accessor = new DelegateAccessor<string>(() => m_UploadHandle.modInfo.m_ForumLink, delegate(string value)
			{
				IModsUploadSupport.ModInfo modInfo = m_UploadHandle.modInfo;
				modInfo.m_ForumLink = value;
				m_UploadHandle.modInfo = modInfo;
			}),
			error = () => forumLinkError
		};
		array2[11] = new Group
		{
			displayName = "Menu.ASSET_EXTERNAL_LINKS",
			children = new IWidget[1] { m_ExternalLinkField = new ExternalLinkField() }
		};
		array2[12] = new Group
		{
			displayName = "Menu.ASSET_PREVIEW_SCREENSHOTS",
			children = new IWidget[1] { m_Screenshots = new IconButtonGroup() }
		};
		EditorSection editorSection = new EditorSection
		{
			displayName = "Paradox.ADDITIONAL_TAGS"
		};
		IWidget[] obj6 = new IWidget[2]
		{
			m_TagsList = new ListField(),
			null
		};
		PopupValueField<string> obj7 = new PopupValueField<string>
		{
			displayName = "Paradox.ADD_TAG",
			popup = new ItemPickerPopup<string>(hasFooter: false, hasImages: false),
			accessor = new DelegateAccessor<string>(() => string.Empty, OnAddTag),
			disabled = () => m_UploadHandle.tagCount >= ModTags.kMaxTags
		};
		PopupValueField<string> popupValueField2 = obj7;
		m_TagsListPopup = obj7;
		obj6[1] = popupValueField2;
		editorSection.children = obj6;
		array2[13] = editorSection;
		Row row = new Row
		{
			flex = new FlexLayout(1f, 0f, -1)
		};
		row.children = new IWidget[2]
		{
			m_PlatformResult = new Column
			{
				flex = new FlexLayout(1f, 1f, -1)
			},
			new ProgressIndicator
			{
				state = delegate
				{
					if (m_State == State.Processing)
					{
						return ProgressIndicator.State.Loading;
					}
					return (m_State != State.Failure && m_State != State.Disabled) ? ProgressIndicator.State.Success : ProgressIndicator.State.Failure;
				},
				hidden = () => m_State == State.Ready
			}
		};
		array2[14] = row;
		array2[15] = (m_SubmitButton = new Button
		{
			displayName = kSubmitLabel,
			action = delegate
			{
				Submit();
			},
			disabled = () => disableSubmit
		});
		scrollable.children = array2;
		array[0] = scrollable;
		m_MainPanel = array;
		if (m_AssetList != null)
		{
			ListField assetList = m_AssetList;
			assetList.onItemRemoved = (Action<int>)Delegate.Combine(assetList.onItemRemoved, new Action<int>(OnRemoveAdditionalAsset));
		}
		ListField tagsList = m_TagsList;
		tagsList.onItemRemoved = (Action<int>)Delegate.Combine(tagsList.onItemRemoved, new Action<int>(OnRemoveTag));
		m_PreviewPickerAdapter = new AssetPickerAdapter(GetPreviews(), 4);
		IWidget[] array4 = new IWidget[1];
		Column column = new Column
		{
			flex = FlexLayout.Fill
		};
		IWidget[] obj8 = new IWidget[2]
		{
			new ItemPicker<AssetItem>
			{
				adapter = m_PreviewPickerAdapter
			},
			null
		};
		obj8[1] = new ButtonRow
		{
			children = new Button[2]
			{
				m_SelectPreviewButton = new Button
				{
					displayName = "Common.SELECT"
				},
				new Button
				{
					displayName = "Common.CANCEL",
					action = delegate
					{
						SetChildren(m_MainPanel);
					}
				}
			}
		};
		column.children = obj8;
		array4[0] = column;
		m_PreviewPickerPanel = array4;
		SetChildren(m_MainPanel);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		RefreshSubmitButtonLabel();
	}

	private void RefreshSubmitButtonLabel()
	{
		string submitButtonLabel = GetSubmitButtonLabel();
		if (m_SubmitButton.displayName.id != submitButtonLabel)
		{
			m_SubmitButton.displayName = submitButtonLabel;
			m_SubmitButton.SetPropertiesChanged();
		}
	}

	private void ReportError(IModsUploadSupport.ModOperationResult result)
	{
		m_State = State.Failure;
		List<IWidget> list = new List<IWidget>
		{
			new ErrorLabel
			{
				visible = true,
				displayName = kFailureLabel
			}
		};
		foreach (string line in result.m_Error.GetLines())
		{
			list.Add(new ErrorLabel
			{
				visible = true,
				displayName = line
			});
		}
		m_PlatformResult.children = list;
		m_NotificationUISystem.RemoveNotification(kNotificationID);
		m_NotificationUISystem.RemoveNotification(kNotificationID, kNotificationDelay, kFailureLabel, result.m_Error.m_Raw, null, ProgressState.Failed);
	}

	private void ReportSuccess()
	{
		m_State = State.Success;
		List<IWidget> list = new List<IWidget>();
		list.Add(new Label
		{
			displayName = kCompleteLabel
		});
		string id = m_UploadHandle.modInfo.m_PublishedID.ToString();
		LocalizedString localizedString = new LocalizedString("Menu.ASSET_UPLOAD_ID", id, new Dictionary<string, ILocElement> { 
		{
			"ID",
			LocalizedString.Value(id)
		} });
		list.Add(new Label
		{
			displayName = localizedString
		});
		list.Add(new Button
		{
			displayName = "Menu.ASSET_COPY_ID",
			action = delegate
			{
				GUIUtility.systemCopyBuffer = id;
			}
		});
		m_PlatformResult.children = list;
		RefreshSubmitButtonLabel();
		m_NotificationUISystem.RemoveNotification(kNotificationID);
		m_NotificationUISystem.RemoveNotification(kNotificationID, kNotificationDelay, kCompleteLabel, localizedString, null, ProgressState.Complete, null, delegate
		{
			GUIUtility.systemCopyBuffer = id;
		});
	}

	private void ReportLocalDataNotFound()
	{
		m_PlatformResult.children = new IWidget[1]
		{
			new Label
			{
				displayName = "Paradox.EXISTING_PREVIEWS_ERROR"
			}
		};
	}

	private void ClearState()
	{
		m_State = State.Ready;
		m_PlatformResult.children = Array.Empty<IWidget>();
		RefreshSubmitButtonLabel();
	}

	private void ReportSubmitting()
	{
		m_State = State.Processing;
		m_PlatformResult.children = new IWidget[1]
		{
			new Label
			{
				displayName = kSubmittingLabel
			}
		};
		m_NotificationUISystem.AddOrUpdateNotification(kNotificationID, kSubmittingLabel, m_UploadHandle.modInfo.m_DisplayName, null, ProgressState.Indeterminate);
	}

	private void ReportSyncing()
	{
		m_State = State.Processing;
		m_PlatformResult.children = new IWidget[1]
		{
			new Label
			{
				displayName = "Paradox.RETRIEVING_DATA"
			}
		};
	}

	private void ReportNoSocial()
	{
		m_State = State.Disabled;
		m_PlatformResult.children = new IWidget[2]
		{
			new Label
			{
				displayName = kNoSocialProfile
			},
			new Button
			{
				displayName = kOpenProfilePage,
				action = delegate
				{
					PdxAssetUploadHandle uploadHandle = m_UploadHandle;
					uploadHandle.onSocialProfileSynced = (Action)Delegate.Combine(uploadHandle.onSocialProfileSynced, new Action(RefreshSocialProfileStatus));
					m_UploadHandle.ShowModsUIProfilePage();
				}
			}
		};
	}

	private string GetSubmitButtonLabel()
	{
		if (m_State == State.Success)
		{
			return kCompleteLabel;
		}
		if (noInternetConnection)
		{
			return kNoInternetConnectionLabel;
		}
		if (notLoggedIn)
		{
			return kNotLoggedInLabel;
		}
		return kSubmitLabel;
	}

	private void SetChildren(IWidget[] newChildren)
	{
		if (newChildren != m_Children)
		{
			m_Children = newChildren;
			onChildrenChange?.Invoke(m_Children);
		}
	}

	public async Task Show(AssetData mainAsset, bool allowManualFileCopy = false)
	{
		await Show(new PdxAssetUploadHandle(mainAsset), allowManualFileCopy);
	}

	public async Task Show(PdxAssetUploadHandle assetUploadHandle, bool allowManualFileCopy = false)
	{
		try
		{
			m_AllowManualFileCopy = false;
			m_UploadHandle = assetUploadHandle;
			SetChildren(m_MainPanel);
			RefreshExternalLinks();
			RefreshScreenshots();
			RefreshPreview();
			RefreshAssetList();
			RefreshDLCList();
			ClearState();
			await SyncPlatformData();
		}
		catch (Exception exception)
		{
			UISystemBase.log.Error(exception);
		}
	}

	private async Task SyncPlatformData()
	{
		ReportSyncing();
		await m_UploadHandle.SyncPlatformData();
		GameManager.instance.RunOnMainThread(delegate
		{
			ClearState();
			RefreshSocialProfileStatus();
			RefreshAuthorMods();
			RefreshTags();
			RefreshDLCList();
		});
	}

	public bool Close()
	{
		if (children == m_PreviewPickerPanel)
		{
			SetChildren(m_MainPanel);
			return false;
		}
		if (m_ExistingModField.expanded)
		{
			m_ExistingModField.expanded = false;
			return false;
		}
		if (m_AssetListPopup != null && m_AssetListPopup.expanded)
		{
			m_AssetListPopup.expanded = false;
			return false;
		}
		return true;
	}

	private void RefreshSocialProfileStatus()
	{
		if (string.IsNullOrEmpty(m_UploadHandle.socialProfile.m_Name))
		{
			ReportNoSocial();
		}
		else if (m_State == State.Disabled)
		{
			ClearState();
		}
	}

	private async Task Submit()
	{
		try
		{
			ReportSubmitting();
			await BeginSubmit();
		}
		catch (Exception exception)
		{
			UISystemBase.log.Error(exception);
		}
	}

	private async Task BeginSubmit()
	{
		IModsUploadSupport.ModOperationResult result = await m_UploadHandle.BeginSubmit();
		if (result.m_Success)
		{
			if (m_AllowManualFileCopy)
			{
				GameManager.instance.RunOnMainThread(delegate
				{
					GUIUtility.systemCopyBuffer = m_UploadHandle.GetAbsoluteContentPath();
					GameManager.instance.userInterface.paradoxBindings.PushDialog(new ParadoxBindings.MultiOptionDialog("Ready to upload", "A work-in-progress folder has been created in " + m_UploadHandle.GetAbsoluteContentPath() + " where you may now copy any additional files you wish to share. Press Submit once you're ready to finalize the upload.", new ParadoxBindings.MultiOptionDialog.Option
					{
						m_Id = "Submit",
						m_OnSelect = delegate
						{
							FinalizeSubmit();
						}
					}, new ParadoxBindings.MultiOptionDialog.Option
					{
						m_Id = "Cancel",
						m_OnSelect = Cancel
					}));
				});
			}
			else
			{
				await FinalizeSubmit();
			}
		}
		else
		{
			GameManager.instance.RunOnMainThread(delegate
			{
				ReportError(result);
			});
		}
	}

	private async Task FinalizeSubmit()
	{
		try
		{
			IModsUploadSupport.ModOperationResult result = await m_UploadHandle.FinalizeSubmit();
			GameManager.instance.RunOnMainThread(delegate
			{
				if (!result.m_Success)
				{
					ReportError(result);
				}
				else
				{
					ReportSuccess();
				}
			});
		}
		catch (Exception exception)
		{
			UISystemBase.log.Error(exception);
		}
	}

	private void Cancel()
	{
		m_NotificationUISystem.RemoveNotification(kNotificationID);
		m_UploadHandle.Cleanup();
		ClearState();
	}

	private void RefreshExternalLinks()
	{
		m_ExternalLinkField.links = m_UploadHandle.modInfo.m_ExternalLinks;
		m_ExternalLinkField.SetPropertiesChanged();
	}

	private void RefreshScreenshots()
	{
		List<IconButton> list = new List<IconButton>(m_UploadHandle.screenshots.Count + 1);
		for (int i = 0; i < m_UploadHandle.screenshots.Count; i++)
		{
			int index = i;
			list.Add(new IconButton
			{
				icon = AssetUploadUtils.GetImageURI(m_UploadHandle.screenshots[index]),
				action = delegate
				{
					RemoveScreenshot(index);
				}
			});
		}
		list.Add(new IconButton
		{
			icon = "Media/Glyphs/Plus.svg",
			action = delegate
			{
				OpenPreviewPickerPanel(AddScreenshot, default(Colossal.Hash128), excludeScreenshots: true);
			}
		});
		m_Screenshots.children = list.ToArray();
	}

	private void RefreshPreview()
	{
		m_PreviewPickerButton.icon = AssetUploadUtils.GetImageURI(m_UploadHandle.preview);
		m_PreviewPickerButton.SetPropertiesChanged();
	}

	private void RefreshAssetList()
	{
		if (m_AssetList == null)
		{
			return;
		}
		List<ListField.Item> list = new List<ListField.Item>();
		foreach (AssetData asset in m_UploadHandle.assets)
		{
			list.Add(GetItem(asset, removable: false, asset == m_UploadHandle.mainAsset));
		}
		foreach (AssetData additionalAsset in m_UploadHandle.additionalAssets)
		{
			list.Add(GetItem(additionalAsset, removable: true, additionalAsset == m_UploadHandle.mainAsset));
		}
		m_AssetList.m_Items = list;
		m_AssetList.SetPropertiesChanged();
	}

	private void RefreshDLCList()
	{
		m_DLCInfoVisible = false;
		m_DLCInfo.children.Clear();
		string[] dLCDependencies = m_UploadHandle.modInfo.m_DLCDependencies;
		foreach (string internalName in dLCDependencies)
		{
			if (TryMatchDLC(internalName, out var dlc))
			{
				m_DLCInfo.children.Add(new DLCInfoField
				{
					displayName = dlc.m_DisplayName,
					type = dlc.m_Type,
					image = dlc.m_ImageURI
				});
				m_DLCInfoVisible = true;
			}
		}
		m_DLCInfo.SetChildrenChanged();
	}

	private bool TryMatchDLC(string internalName, out IModsUploadSupport.DLCTag dlc)
	{
		IModsUploadSupport.DLCTag[] availableDLCs = m_UploadHandle.availableDLCs;
		for (int i = 0; i < availableDLCs.Length; i++)
		{
			IModsUploadSupport.DLCTag dLCTag = availableDLCs[i];
			if (dLCTag.m_InternalName == internalName)
			{
				dlc = dLCTag;
				return true;
			}
		}
		dlc = default(IModsUploadSupport.DLCTag);
		return false;
	}

	private static ListField.Item GetItem(AssetData asset, bool removable, bool mainAsset)
	{
		ListField.Item result = new ListField.Item
		{
			m_Label = GetLabel(asset),
			m_Removable = removable,
			m_Data = asset
		};
		if (asset is PrefabAsset prefabAsset)
		{
			HashSet<AssetData> hashSet = new HashSet<AssetData>();
			AssetUploadUtils.CollectPrefabAssetDependencies(prefabAsset, hashSet, mainAsset);
			result.m_SubItems = hashSet.Where((AssetData dep) => dep != asset && dep is PrefabAsset).Select(GetLabel).ToArray();
		}
		return result;
	}

	private static string GetLabel(AssetData asset)
	{
		SourceMeta meta = asset.GetMeta();
		if (meta.platformID > 0)
		{
			return $"{asset.name} ({meta.platformID})";
		}
		if (meta.packaged)
		{
			return asset.name + " (" + meta.packageName + ")";
		}
		return asset.name;
	}

	private void OnAddPrefab(PrefabBase prefab)
	{
		m_AssetListPopup.expanded = false;
		if (!(prefab == null))
		{
			m_UploadHandle.AddAdditionalAsset(prefab.asset);
			RefreshAssetList();
			RefreshDLCList();
		}
	}

	private void OnRemoveAdditionalAsset(int index)
	{
		if (m_AssetList.m_Items[index].m_Data is AssetData asset)
		{
			m_UploadHandle.RemoveAdditionalAsset(asset);
			RefreshAssetList();
			RefreshDLCList();
		}
	}

	private bool ShouldShowInPrefabPicker(PrefabBase prefab)
	{
		if (prefab.asset != null && prefab.asset.database == AssetDatabase.user)
		{
			return !m_UploadHandle.cachedDependencies.Contains(prefab.asset);
		}
		return false;
	}

	private void RefreshTags()
	{
		List<ListField.Item> list = new List<ListField.Item>();
		foreach (string tag in m_UploadHandle.tags)
		{
			list.Add(new ListField.Item
			{
				m_Label = tag,
				m_Removable = false,
				m_Data = tag
			});
		}
		foreach (string additionalTag in m_UploadHandle.additionalTags)
		{
			list.Add(new ListField.Item
			{
				m_Label = additionalTag,
				m_Removable = true,
				m_Data = additionalTag
			});
		}
		m_TagsList.m_Items = list;
		m_TagsList.SetPropertiesChanged();
		List<ItemPickerPopup<string>.Item> list2 = new List<ItemPickerPopup<string>.Item>();
		IModsUploadSupport.ModTag[] availableTags = m_UploadHandle.availableTags;
		for (int i = 0; i < availableTags.Length; i++)
		{
			IModsUploadSupport.ModTag modTag = availableTags[i];
			if (!m_UploadHandle.tags.Contains(modTag.m_Id) && !m_UploadHandle.additionalTags.Contains(modTag.m_Id))
			{
				list2.Add(new ItemPickerPopup<string>.Item
				{
					m_Value = modTag.m_Id,
					displayName = modTag.m_DisplayName
				});
			}
		}
		((ItemPickerPopup<string>)m_TagsListPopup.popup).SetItems(list2);
	}

	private void OnAddTag(string tag)
	{
		m_TagsListPopup.expanded = false;
		if (tag != null)
		{
			m_UploadHandle.AddAdditionalTag(tag);
		}
		RefreshTags();
	}

	private void OnRemoveTag(int index)
	{
		if (m_TagsList.m_Items[index].m_Data is string tag)
		{
			m_UploadHandle.RemoveAdditionalTag(tag);
		}
		RefreshTags();
	}

	private void OpenPreviewPickerPanel(Action<Colossal.Hash128> callback, Colossal.Hash128 defaultSelection = default(Colossal.Hash128), bool excludeScreenshots = false)
	{
		m_PreviewPickerAdapter.SetItems(GetPreviews(excludeScreenshots));
		m_PreviewPickerAdapter.SelectItemByGuid(defaultSelection);
		SetChildren(m_PreviewPickerPanel);
		m_SelectPreviewButton.action = delegate
		{
			ClosePreviewPickerPanel(callback);
		};
	}

	private void ClosePreviewPickerPanel(Action<Colossal.Hash128> callback)
	{
		SetChildren(m_MainPanel);
		callback?.Invoke(m_PreviewPickerAdapter.selectedItem.guid);
	}

	private void SetPreview(Colossal.Hash128 guid)
	{
		if (AssetDatabase.global.TryGetAsset(guid, out AssetData asset))
		{
			m_UploadHandle.SetPreview(asset);
		}
		RefreshPreview();
	}

	private void AddScreenshot(Colossal.Hash128 guid)
	{
		if (AssetDatabase.global.TryGetAsset(guid, out AssetData asset))
		{
			m_UploadHandle.AddScreenshot(asset);
		}
		RefreshScreenshots();
	}

	private void RemoveScreenshot(int index)
	{
		AssetData asset = m_UploadHandle.screenshots[index];
		m_UploadHandle.RemoveScreenshot(asset);
		RefreshScreenshots();
	}

	private IEnumerable<AssetItem> GetPreviews(bool excludeScreenshots = false)
	{
		HashSet<IAssetData> screenshots = (excludeScreenshots ? new HashSet<IAssetData>(m_UploadHandle.screenshots) : null);
		foreach (AssetData originalPreview in m_UploadHandle.originalPreviews)
		{
			if (!excludeScreenshots || !screenshots.Contains(originalPreview))
			{
				yield return new AssetItem
				{
					guid = originalPreview.id,
					image = AssetUploadUtils.GetImageURI(originalPreview)
				};
			}
		}
		foreach (ImageAsset asset in AssetDatabase.global.GetAssets(SearchFilter<ImageAsset>.ByCondition((ImageAsset a) => a.GetMeta().subPath?.StartsWith(ScreenUtility.kScreenshotDirectory) ?? false)))
		{
			if (!excludeScreenshots || !screenshots.Contains(asset))
			{
				yield return new AssetItem
				{
					guid = asset.id,
					fileName = asset.name,
					displayName = asset.name,
					image = asset.ToUri()
				};
			}
		}
	}

	private void RefreshAuthorMods()
	{
		m_ExistingModPopup.SetItems(GetExistingMods());
	}

	private IEnumerable<ItemPickerPopup<int>.Item> GetExistingMods()
	{
		yield return new ItemPickerPopup<int>.Item
		{
			m_Value = -1,
			displayName = kNone
		};
		foreach (IModsUploadSupport.ModInfo authorMod in m_UploadHandle.authorMods)
		{
			ItemPickerPopup<int>.Item item = new ItemPickerPopup<int>.Item
			{
				m_Value = authorMod.m_PublishedID,
				displayName = LocalizedString.Value(authorMod.m_DisplayName)
			};
			string[] array = new string[2];
			int publishedID = authorMod.m_PublishedID;
			array[0] = publishedID.ToString();
			array[1] = authorMod.m_DisplayName;
			item.m_SearchTerms = array;
			yield return item;
		}
	}

	private async void SetInfoFromExisting()
	{
		m_State = State.Processing;
		IModsUploadSupport.ModInfo info = await m_UploadHandle.GetExistingInfo();
		var (localDataFound, localData) = await m_UploadHandle.GetLocalData(info.m_PublishedID);
		GameManager.instance.RunOnMainThread(delegate
		{
			m_State = State.Ready;
			m_UploadHandle.modInfo = info;
			RefreshExternalLinks();
			if (localDataFound)
			{
				ClearState();
				m_UploadHandle.SetPreviewsFromExisting(localData);
				RefreshPreview();
				RefreshScreenshots();
			}
			else
			{
				ReportLocalDataNotFound();
			}
		});
	}

	[Preserve]
	public AssetUploadPanelUISystem()
	{
	}
}
