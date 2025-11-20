using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Colossal.Annotations;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Colossal.UI.Binding;
using PDX.SDK.Contracts.Enums;
using PDX.SDK.Contracts.Service.Mods.Models;
using UnityEngine;

namespace Game.UI.Menu;

public class ParadoxBindings : CompositeBinding
{
	public abstract class ParadoxDialog : IJsonWritable
	{
		public virtual void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.TypeEnd();
		}
	}

	public class LoginFormData : IJsonReadable
	{
		public string email;

		public string password;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("email");
			reader.Read(out email);
			reader.ReadProperty("password");
			reader.Read(out password);
			reader.ReadMapEnd();
		}
	}

	public class RegistrationFormData : IJsonReadable
	{
		public string email;

		public string password;

		public string country;

		public string dateOfBirth;

		public bool marketingPermission;

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("email");
			reader.Read(out email);
			reader.ReadProperty("password");
			reader.Read(out password);
			reader.ReadProperty("country");
			reader.Read(out country);
			reader.ReadProperty("dateOfBirth");
			reader.Read(out dateOfBirth);
			reader.ReadProperty("marketingPermission");
			reader.Read(out marketingPermission);
			reader.ReadMapEnd();
		}
	}

	public abstract class MessageDialog : ParadoxDialog
	{
		[CanBeNull]
		public readonly string icon;

		[CanBeNull]
		public readonly string titleId;

		[NotNull]
		public readonly string messageId;

		[CanBeNull]
		public readonly Dictionary<string, string> messageArgs;

		protected MessageDialog(string icon, string titleId, string messageId, Dictionary<string, string> messageArgs)
		{
			this.icon = icon;
			this.titleId = titleId;
			this.messageId = messageId;
			this.messageArgs = messageArgs;
		}

		public override void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.PropertyName("titleId");
			writer.Write(titleId);
			writer.PropertyName("messageId");
			writer.Write(messageId);
			writer.PropertyName("messageArgs");
			writer.Write((IReadOnlyDictionary<string, string>)messageArgs);
			writer.TypeEnd();
		}
	}

	public class LoginDialog : ParadoxDialog
	{
	}

	public class RegistrationDialog : ParadoxDialog
	{
	}

	public class AccountLinkDialog : MessageDialog
	{
		public AccountLinkDialog(string icon, string messageId)
			: base(icon, "Paradox.ACCOUNT_LINK_PROMPT_TITLE", messageId, null)
		{
		}
	}

	public class AccountLinkOverwriteDialog : MessageDialog
	{
		public AccountLinkOverwriteDialog(string icon, string messageId)
			: base(icon, "Paradox.ACCOUNT_LINK_OVERWRITE_TITLE", messageId, null)
		{
		}
	}

	public class LegalDocumentDialog : ParadoxDialog
	{
		[NotNull]
		public readonly LegalDocument document;

		public readonly bool agreementRequired;

		public readonly string confirmLabel;

		public LegalDocumentDialog(LegalDocument document, bool agreementRequired = true)
		{
			this.document = document;
			this.agreementRequired = agreementRequired;
			confirmLabel = document.confirmLabel;
		}

		public override void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("text");
			writer.Write(document.content);
			writer.PropertyName("agreementRequired");
			writer.Write(agreementRequired);
			writer.PropertyName("confirmLabel");
			writer.Write(confirmLabel);
			writer.TypeEnd();
		}
	}

	public class ConfirmationDialog : MessageDialog
	{
		public ConfirmationDialog(string icon, string titleId, string messageId, Dictionary<string, string> messageArgs)
			: base(icon, titleId, messageId, messageArgs)
		{
		}
	}

	public class ErrorDialog : ParadoxDialog
	{
		[CanBeNull]
		public readonly string messageId;

		[CanBeNull]
		public readonly string message;

		public ErrorDialog(string messageId, string message)
		{
			this.messageId = messageId;
			this.message = message;
		}

		public override void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("messageId");
			writer.Write(messageId);
			writer.PropertyName("message");
			writer.Write(message);
			writer.TypeEnd();
		}
	}

	public class MultiOptionDialog : ParadoxDialog
	{
		public struct Option : IJsonWritable
		{
			public string m_Id;

			public Action m_OnSelect;

			public void Write(IJsonWriter writer)
			{
				writer.TypeBegin(GetType().Name);
				writer.PropertyName("id");
				writer.Write(m_Id);
				writer.TypeEnd();
			}
		}

		public string m_TitleId;

		public string m_MessageId;

		public Option[] m_Options;

		public MultiOptionDialog(string titleId, string messageId, params Option[] options)
		{
			m_TitleId = titleId;
			m_MessageId = messageId;
			m_Options = options;
		}

		public override void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("titleId");
			writer.Write(m_TitleId);
			writer.PropertyName("messageId");
			writer.Write(m_MessageId);
			writer.PropertyName("options");
			writer.Write((IList<Option>)m_Options);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "paradox";

	private readonly ValueBinding<bool> m_RequestActiveBinding;

	private readonly ValueBinding<bool> m_LoggedInBinding;

	private readonly ValueBinding<AccountLinkProvider> m_AccountLinkProviderBinding;

	private readonly ValueBinding<int> m_AccountLinkStateBinding;

	private readonly ValueBinding<string> m_UserNameBinding;

	private readonly ValueBinding<string> m_EmailBinding;

	private readonly ValueBinding<string> m_AvatarBinding;

	private readonly ValueBinding<bool> m_IsThirdPartyOffline;

	private readonly ValueBinding<bool> m_HasInternetConnection;

	private readonly ValueBinding<bool> m_IsPDXSDKEnabled;

	private readonly StackBinding<ParadoxDialog> m_ActiveDialogsBinding;

	private PdxSdkPlatform m_PdxPlatform;

	private static readonly string kTermsOfUse = "TERMS_OF_USE";

	private static readonly string kPrivacyPolicy = "PRIVACY_POLICY";

	public ParadoxBindings()
	{
		AddBinding(m_RequestActiveBinding = new ValueBinding<bool>("paradox", "requestActive", initialValue: false));
		AddBinding(m_LoggedInBinding = new ValueBinding<bool>("paradox", "loggedIn", initialValue: false));
		AddBinding(m_AccountLinkProviderBinding = new ValueBinding<AccountLinkProvider>("paradox", "accountLinkProvider", AccountLinkProvider.Unknown, new EnumNameWriter<AccountLinkProvider>()));
		AddBinding(m_AccountLinkStateBinding = new ValueBinding<int>("paradox", "accountLinkState", 0));
		AddBinding(m_UserNameBinding = new ValueBinding<string>("paradox", "userName", null, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_EmailBinding = new ValueBinding<string>("paradox", "email", null, ValueWriters.Nullable(new StringWriter())));
		AddBinding(m_AvatarBinding = new ValueBinding<string>("paradox", "avatar", null, ValueWriters.Nullable(new StringWriter())));
		AddBinding(new TriggerBinding("paradox", "linkAccount", LinkAccount));
		AddBinding(new TriggerBinding("paradox", "unlinkAccount", UnlinkAccount));
		AddBinding(new TriggerBinding("paradox", "logout", Logout));
		AddBinding(m_ActiveDialogsBinding = new StackBinding<ParadoxDialog>("paradox", "activeDialogs", new ValueWriter<ParadoxDialog>()));
		AddBinding(new TriggerBinding("paradox", "closeActiveDialog", CloseActiveDialog));
		AddBinding(new TriggerBinding("paradox", "showLoginForm", ShowLoginForm));
		AddBinding(new TriggerBinding<string>("paradox", "submitPasswordReset", SubmitPasswordReset));
		AddBinding(new TriggerBinding<LoginFormData>("paradox", "submitLoginForm", SubmitLoginForm, new ValueReader<LoginFormData>()));
		AddBinding(m_IsThirdPartyOffline = new ValueBinding<bool>("paradox", "isThirdPartyOffline", PlatformManager.instance.isOfflineOnly));
		AddBinding(m_HasInternetConnection = new ValueBinding<bool>("paradox", "hasInternetConnection", PlatformManager.instance.hasConnectivity));
		AddBinding(new GetterValueBinding<List<string>>("paradox", "countryCodes", GetCountryCodes, new ListWriter<string>(new StringWriter())));
		AddBinding(new TriggerBinding("paradox", "showRegistrationForm", ShowRegistrationForm));
		AddBinding(new TriggerBinding<string>("paradox", "showLink", ShowLink));
		AddBinding(new TriggerBinding<RegistrationFormData>("paradox", "submitRegistrationForm", SubmitRegistrationForm, new ValueReader<RegistrationFormData>()));
		AddBinding(new TriggerBinding("paradox", "confirmAccountLink", ConfirmAccountLink));
		AddBinding(new TriggerBinding("paradox", "confirmAccountLinkOverwrite", ConfirmAccountLinkOverwrite));
		AddBinding(new TriggerBinding("paradox", "markLegalDocumentAsViewed", MarkLegalDocumentAsViewed));
		AddBinding(new TriggerBinding("paradox", "showTermsOfUse", ShowTermsOfUse));
		AddBinding(new TriggerBinding("paradox", "showPrivacyPolicy", ShowPrivacyPolicy));
		AddBinding(new TriggerBinding<int>("paradox", "onOptionSelected", OnOptionSelected));
		AddBinding(m_IsPDXSDKEnabled = new ValueBinding<bool>("paradox", "pdxSDKEnabled", initialValue: false));
		m_PdxPlatform = PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk");
		PlatformManager.instance.onPlatformRegistered += delegate(IPlatformServiceIntegration psi)
		{
			if (psi is PdxSdkPlatform pdxPlatform)
			{
				m_PdxPlatform = pdxPlatform;
				m_PdxPlatform.onLoggedIn += OnUserLoggedIn;
				m_PdxPlatform.onLoggedOut += OnUserLoggedOut;
				m_PdxPlatform.onAccountLinkChanged += OnAccountLinkChanged;
				m_PdxPlatform.onLegalDocumentStatusChanged += OnLegalDocumentStatusChanged;
				m_PdxPlatform.onStatusChanged += OnStatusChanged;
			}
		};
		OnInternetConnectionStatusChanged(PlatformManager.instance.hasConnectivity);
		PlatformManager.instance.onConnectivityStatusChanged += OnInternetConnectionStatusChanged;
		OnThirdPartyOfflineStatusChanged(PlatformManager.instance.isOfflineOnly);
		PlatformManager.instance.onThirdPartyOfflineChanged += OnThirdPartyOfflineStatusChanged;
	}

	public void OnPSModsUIOpened(Action onContinue)
	{
		m_ActiveDialogsBinding.Push(new MultiOptionDialog("Menu.PDX_MODS", "Paradox.PS_MODS_DISCLAIMER", new MultiOptionDialog.Option
		{
			m_Id = "Common.OK",
			m_OnSelect = onContinue
		}));
	}

	public void OnPSModsUIClosed(Action onKeepMods, Action onDisableMods, Action onBack)
	{
		m_ActiveDialogsBinding.Push(new MultiOptionDialog("Menu.PDX_MODS", "Paradox.PS_MODS_EXIT_DISCLAIMER", new MultiOptionDialog.Option
		{
			m_Id = "Paradox.PS_MODS_EXIT_KEEP_MODS",
			m_OnSelect = onKeepMods
		}, new MultiOptionDialog.Option
		{
			m_Id = "Paradox.PS_MODS_EXIT_DISABLE_MODS",
			m_OnSelect = onDisableMods
		}, new MultiOptionDialog.Option
		{
			m_Id = "Paradox.PS_MODS_EXIT_GO_BACK",
			m_OnSelect = onBack
		}));
	}

	public void PushDialog(ParadoxDialog dialog)
	{
		m_ActiveDialogsBinding.Push(dialog);
	}

	private void OnOptionSelected(int index)
	{
		if (m_ActiveDialogsBinding.Peek() is MultiOptionDialog multiOptionDialog)
		{
			m_ActiveDialogsBinding.Pop();
			multiOptionDialog.m_Options[index].m_OnSelect?.Invoke();
		}
	}

	private void OnAccountLinkChanged(AccountLinkState state, AccountLinkProvider provider)
	{
		m_AccountLinkProviderBinding.Update(provider);
		m_AccountLinkStateBinding.Update((int)state);
	}

	private async void Logout()
	{
		PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.Logout());
		if (requestReport != null)
		{
			m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
		}
	}

	private List<string> GetCountryCodes()
	{
		List<string> list = new List<string>(Enum.GetNames(typeof(Country)));
		list.Remove(Country.Undefined.ToString());
		return list;
	}

	private void OnInternetConnectionStatusChanged(bool connected)
	{
		CompositeBinding.log.Debug("$ParadoxBinding.OnInternetConnectionStatusChanged {connected}");
		m_HasInternetConnection.Update(connected);
	}

	private void OnThirdPartyOfflineStatusChanged(bool isOffline)
	{
		m_IsThirdPartyOffline.Update(isOffline);
	}

	private void OnStatusChanged(IPlatformServiceIntegration psi)
	{
		if (psi == m_PdxPlatform)
		{
			m_IsPDXSDKEnabled.Update(m_PdxPlatform.isInitialized);
			m_AccountLinkProviderBinding.Update(m_PdxPlatform.accountLinkProvider);
			m_AccountLinkStateBinding.Update((int)m_PdxPlatform.accountLinkState);
		}
	}

	private async void OnUserLoggedIn(string firstName, string lastName, string email, AccountLinkState accountLinkState, bool firstTime)
	{
		m_LoggedInBinding.Update(newValue: true);
		m_AccountLinkStateBinding.Update((int)accountLinkState);
		m_EmailBinding.Update(email);
		ModCreator modCreator = await m_PdxPlatform.GetCreatorProfile();
		if (modCreator != null)
		{
			m_UserNameBinding.Update(modCreator.Username);
			m_AvatarBinding.Update(modCreator.Avatar.Url);
		}
	}

	private void OnUserLoggedOut(string id)
	{
		m_UserNameBinding.Update(null);
		m_EmailBinding.Update(null);
		m_AvatarBinding.Update(null);
		m_LoggedInBinding.Update(newValue: false);
	}

	private void OnLegalDocumentStatusChanged(LegalDocument doc, int remainingCount)
	{
		if (m_ActiveDialogsBinding.Peek() is LegalDocumentDialog)
		{
			m_ActiveDialogsBinding.Pop();
		}
		if (doc != null)
		{
			m_ActiveDialogsBinding.Push(new LegalDocumentDialog(doc));
		}
	}

	private void CloseActiveDialog()
	{
		if (!m_RequestActiveBinding.value && !(m_ActiveDialogsBinding.Peek() is LegalDocumentDialog { agreementRequired: not false }))
		{
			m_ActiveDialogsBinding.Pop();
			if (m_ActiveDialogsBinding.count == 0)
			{
				PlatformManager.instance.EnableSharing();
			}
		}
	}

	public void ShowLoginForm()
	{
		if (Connectivity.hasConnectivity)
		{
			PlatformManager.instance.DisableSharing();
			m_ActiveDialogsBinding.ClearAndPush(new LoginDialog());
		}
		else
		{
			m_ActiveDialogsBinding.Push(new ErrorDialog("Failed to connect", "Please check your internet connection"));
		}
	}

	private async void SubmitLoginForm(LoginFormData data)
	{
		if (m_RequestActiveBinding.value)
		{
			return;
		}
		PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.Login(data.email, data.password, CancellationToken.None));
		if (requestReport == null)
		{
			m_ActiveDialogsBinding.Clear();
			if (m_PdxPlatform.accountLinkProvider != AccountLinkProvider.Unknown && m_PdxPlatform.accountLinkState == AccountLinkState.Unlinked)
			{
				LinkAccount();
			}
		}
		else
		{
			m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
		}
	}

	private async void SubmitPasswordReset(string email)
	{
		if (!m_RequestActiveBinding.value)
		{
			PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.ResetPassword(email));
			if (requestReport == null)
			{
				m_ActiveDialogsBinding.Push(new ConfirmationDialog(null, null, "Paradox.PASSWORD_RESET_CONFIRMATION_TEXT", new Dictionary<string, string> { { "EMAIL", email } }));
			}
			else
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
			}
		}
	}

	private void ShowRegistrationForm()
	{
		m_ActiveDialogsBinding.ClearAndPush(new RegistrationDialog());
	}

	private async void ShowLink(string link)
	{
		if (link == kTermsOfUse)
		{
			(LegalDocument, PdxSdkPlatform.RequestReport) tuple = await RunForegroundRequest(m_PdxPlatform.ShowTermsOfUse());
			if (tuple.Item1 != null)
			{
				m_ActiveDialogsBinding.Push(new LegalDocumentDialog(tuple.Item1, agreementRequired: false));
			}
			else if (tuple.Item2 != null)
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(tuple.Item2.messageId, tuple.Item2.message));
			}
		}
		else if (link == kPrivacyPolicy)
		{
			(LegalDocument, PdxSdkPlatform.RequestReport) tuple2 = await RunForegroundRequest(m_PdxPlatform.ShowPrivacyPolicy());
			if (tuple2.Item1 != null)
			{
				m_ActiveDialogsBinding.Push(new LegalDocumentDialog(tuple2.Item1, agreementRequired: false));
			}
			else if (tuple2.Item2 != null)
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(tuple2.Item2.messageId, tuple2.Item2.message));
			}
		}
		else
		{
			Application.OpenURL(link);
		}
	}

	private async void SubmitRegistrationForm(RegistrationFormData data)
	{
		if (m_RequestActiveBinding.value)
		{
			return;
		}
		if (Enum.TryParse<Country>(data.country, out var result) && DateTime.TryParseExact(data.dateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
		{
			PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.CreateParadoxAccount(data.email, data.password, Language.en, result, result2, data.marketingPermission));
			if (requestReport == null)
			{
				m_ActiveDialogsBinding.Clear();
				if (m_PdxPlatform.accountLinkProvider != AccountLinkProvider.Unknown && m_PdxPlatform.accountLinkState == AccountLinkState.Unlinked)
				{
					LinkAccount();
				}
				m_ActiveDialogsBinding.Push(new ConfirmationDialog(null, "Paradox.REGISTRATION_CONFIRMATION_TITLE", "Paradox.REGISTRATION_CONFIRMATION_TEXT", null));
			}
			else
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
			}
		}
		else
		{
			m_ActiveDialogsBinding.Push(new ErrorDialog(null, "Internal error: Invalid Country Code string or Invalid date string"));
		}
	}

	private async void ConfirmAccountLink()
	{
		if (m_RequestActiveBinding.value)
		{
			return;
		}
		if (m_PdxPlatform.AccountLinkMismatch == AccountLinkMismatch.None)
		{
			PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.LinkAccount());
			if (requestReport == null)
			{
				m_AccountLinkStateBinding.Update(2);
				m_ActiveDialogsBinding.ClearAndPush(new ConfirmationDialog(GetAccountLinkProviderIcon(), "Paradox.ACCOUNT_LINK_PROMPT_TITLE", $"Paradox.ACCOUNT_LINK_CONFIRMATION_TEXT[{m_PdxPlatform.accountLinkProvider:G}]", null));
			}
			else
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
			}
		}
		else
		{
			string messageId = m_PdxPlatform.AccountLinkMismatch switch
			{
				AccountLinkMismatch.Paradox => $"Paradox.PDX_ACCOUNT_LINK_OVERWRITE_PROMPT_TEXT[{m_PdxPlatform.accountLinkProvider:G}]", 
				AccountLinkMismatch.ThirdParty => $"Paradox.PLATFORM_ACCOUNT_LINK_OVERWRITE_PROMPT_TEXT[{m_PdxPlatform.accountLinkProvider:G}]", 
				AccountLinkMismatch.Both => $"Paradox.PDX_PLATFORM_ACCOUNT_LINK_OVERWRITE_PROMPT_TEXT[{m_PdxPlatform.accountLinkProvider:G}]", 
				_ => null, 
			};
			m_ActiveDialogsBinding.Push(new AccountLinkOverwriteDialog(GetAccountLinkProviderIcon(), messageId));
		}
	}

	private async void ConfirmAccountLinkOverwrite()
	{
		if (!m_RequestActiveBinding.value)
		{
			PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.OverwriteAccountLinks());
			if (requestReport == null)
			{
				m_AccountLinkStateBinding.Update(2);
				m_ActiveDialogsBinding.ClearAndPush(new ConfirmationDialog(GetAccountLinkProviderIcon(), "Paradox.ACCOUNT_LINK_PROMPT_TITLE", $"Paradox.ACCOUNT_LINK_CONFIRMATION_TEXT[{m_PdxPlatform.accountLinkProvider:G}]", null));
			}
			else
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
			}
		}
	}

	private void LinkAccount()
	{
		m_ActiveDialogsBinding.Push(new AccountLinkDialog(GetAccountLinkProviderIcon(), $"Paradox.ACCOUNT_LINK_PROMPT_TEXT[{m_PdxPlatform.accountLinkProvider:G}]"));
	}

	private async void UnlinkAccount()
	{
		if (!m_RequestActiveBinding.value)
		{
			PdxSdkPlatform.RequestReport requestReport = await RunForegroundRequest(m_PdxPlatform.UnlinkThirdPartyAccount());
			if (requestReport == null)
			{
				m_AccountLinkStateBinding.Update(1);
			}
			else
			{
				m_ActiveDialogsBinding.Push(new ErrorDialog(requestReport.messageId, requestReport.message));
			}
		}
	}

	private void ShowTermsOfUse()
	{
		ShowLink(kTermsOfUse);
	}

	private void ShowPrivacyPolicy()
	{
		ShowLink(kPrivacyPolicy);
	}

	private string GetAccountLinkProviderIcon()
	{
		return $"Media/Menu/Platforms/{m_PdxPlatform.accountLinkProvider:G}.svg";
	}

	private async void MarkLegalDocumentAsViewed()
	{
		if (m_ActiveDialogsBinding.Peek() is LegalDocumentDialog { document: var document })
		{
			await RunForegroundRequest(m_PdxPlatform.MarkLegalDocumentAsViewed(document));
		}
	}

	private async Task<T> RunForegroundRequest<T>(Task<T> task)
	{
		m_RequestActiveBinding.Update(newValue: true);
		try
		{
			return await task;
		}
		finally
		{
			m_RequestActiveBinding.Update(newValue: false);
		}
	}
}
