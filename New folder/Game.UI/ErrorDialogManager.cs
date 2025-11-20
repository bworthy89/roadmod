using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Colossal;
using Colossal.IO;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Logging;
using Colossal.Logging.Diagnostics;
using Colossal.PSI.Common;
using Colossal.PSI.Environment;
using Colossal.UI.Binding;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.UI.Localization;
using Unity.Entities;
using UnityEngine;

namespace Game.UI;

public class ErrorDialogManager : IDisposable
{
	private struct SpamConfig
	{
		public double binSeconds;

		public double horizonSeconds;

		public double threshold;

		public int minOccupiedBins;

		public int minAdjacentBinsForBurst;

		public int minConsecutiveBinsForSingles;

		public double weightK;

		public double weightCap;

		public double weightBase;

		public double quietClearSeconds;
	}

	private float m_CachedSimulationSpeed;

	private ValueBinding<ErrorDialog> m_CurrentErrorDialogBinding;

	private readonly LinkedList<ErrorEntry> m_Queue = new LinkedList<ErrorEntry>();

	private readonly Dictionary<Fingerprint, FingerprintState> m_StateByKey = new Dictionary<Fingerprint, FingerprintState>();

	private bool m_Enabled = true;

	private readonly SpamConfig m_Spam = new SpamConfig
	{
		binSeconds = 1.0,
		horizonSeconds = 6.0,
		threshold = 3.15,
		minOccupiedBins = 2,
		minAdjacentBinsForBurst = 3,
		minConsecutiveBinsForSingles = 4,
		weightK = 3.0,
		weightCap = 3.0,
		weightBase = 0.22,
		quietClearSeconds = 5.0
	};

	public bool enabled
	{
		get
		{
			return m_Enabled;
		}
		set
		{
			if (m_Enabled != value)
			{
				m_Enabled = value;
				if (!m_Enabled)
				{
					Clear();
				}
			}
		}
	}

	private ErrorDialog currentError => m_Queue.First?.Value.error;

	public ErrorDialogManager()
	{
		UnityLogger.OnException += OnException;
		UnityLogger.OnWarnOrHigher += OnWarnOrHigher;
	}

	public void CreateBindings(AppBindings bindings)
	{
		bindings.AddBinding(m_CurrentErrorDialogBinding = new ValueBinding<ErrorDialog>("app", "currentError", currentError, ValueWriters.Nullable(new ValueWriter<ErrorDialog>())));
	}

	private Fingerprint CreateFingerprintKey(Exception exception, string message, string details, string identifier)
	{
		if (exception is CorruptedCokException || exception is CorruptedContentException)
		{
			message = null;
			details = null;
			identifier = null;
		}
		return new Fingerprint(exception?.GetType(), message, details, identifier);
	}

	public async Task RenameCorruptedPackagesAsync()
	{
		await Task.Run(delegate
		{
			LinkedListNode<ErrorEntry> first = m_Queue.First;
			if (first != null)
			{
				foreach (ErrorAsset asset in first.Value.error.assets)
				{
					RenameCorruptedPackage(asset);
				}
			}
		});
	}

	public void RenameCorruptedPackages()
	{
		LinkedListNode<ErrorEntry> first = m_Queue.First;
		if (first == null)
		{
			return;
		}
		foreach (ErrorAsset asset in first.Value.error.assets)
		{
			RenameCorruptedPackage(asset);
		}
	}

	private void RenameCorruptedPackage(ErrorAsset errorAsset)
	{
		try
		{
			string path = EnvPath.kUserDataPath + "/~CorruptedPackages/" + GetCorruptedFileName(errorAsset.path, errorAsset.dataSource);
			IOUtils.EnsureDirectory(path);
			path = IOUtils.EnsureUniquePath(path);
			using (Stream source = errorAsset.dataSource.GetReadStream(errorAsset.guid))
			{
				using FileStream destination = LongFile.OpenWrite(path);
				IOUtils.CopyStream(source, destination);
			}
			errorAsset.dataSource.DeleteEntry(errorAsset.guid);
		}
		catch (Exception exception)
		{
			LogManager.FileSystem.Error(exception);
		}
	}

	private void OnException(Exception exception, UnityEngine.Object context)
	{
		if (m_Enabled)
		{
			string message = exception?.Message ?? "Unknown Exception";
			string errorDetail = GetErrorDetail(exception, context);
			ErrorDialog errorDialog = new ErrorDialog
			{
				severity = ErrorDialog.Severity.Error,
				localizedTitle = GetTitle(exception),
				localizedMessage = GetMessage(exception, message),
				errorDetails = GetErrorDetailDisplay(exception, errorDetail),
				actions = GetActions(exception)
			};
			if (exception is IAssetException asset)
			{
				errorDialog.AddAsset(asset);
			}
			Fingerprint key = CreateFingerprintKey(exception, message, errorDetail, null);
			EnqueueOrUpdate(key, errorDialog, ShouldAggregateDetails(exception));
		}
	}

	private void OnWarnOrHigher(ILog log, Level level, string message, Exception exception, UnityEngine.Object context)
	{
		if (m_Enabled && (log == null || log.showsErrorsInUI) && level >= Level.Error)
		{
			string errorDetail = GetErrorDetail(exception, context);
			ErrorDialog errorDialog = new ErrorDialog
			{
				severity = ((!(level == Level.Warn)) ? ErrorDialog.Severity.Error : ErrorDialog.Severity.Warning),
				localizedTitle = GetTitle(exception),
				localizedMessage = GetMessage(exception, message),
				errorDetails = GetErrorDetailDisplay(exception, errorDetail),
				actions = GetActions(exception)
			};
			if (exception is IAssetException asset)
			{
				errorDialog.AddAsset(asset);
			}
			Fingerprint key = CreateFingerprintKey(exception, message, errorDetail, null);
			EnqueueOrUpdate(key, errorDialog, ShouldAggregateDetails(exception));
		}
	}

	private bool ShouldAggregateDetails(Exception exception)
	{
		if (!(exception is CorruptedCokException))
		{
			return exception is CorruptedContentException;
		}
		return true;
	}

	public void ShowError(ErrorDialog dialog, bool aggregateDetails = false)
	{
		if (m_Enabled)
		{
			HandlePause();
			string message = dialog.localizedMessage.ToJSONString();
			string errorDetails = dialog.errorDetails;
			if (dialog.actions == (ErrorDialog.ActionBits)0u)
			{
				dialog.actions = ErrorDialog.ActionBits.Continue;
			}
			Fingerprint key = CreateFingerprintKey(null, message, errorDetails, null);
			EnqueueOrUpdate(key, dialog, aggregateDetails);
		}
	}

	public void Dispose()
	{
		UnityLogger.OnException -= OnException;
		UnityLogger.OnWarnOrHigher -= OnWarnOrHigher;
	}

	private void UpdateBindings()
	{
		GameManager.instance.RunOnMainThread(delegate
		{
			if (m_CurrentErrorDialogBinding != null && m_CurrentErrorDialogBinding.attached)
			{
				if (m_CurrentErrorDialogBinding.value == currentError)
				{
					m_CurrentErrorDialogBinding.TriggerUpdate();
				}
				else
				{
					m_CurrentErrorDialogBinding.Update(currentError);
				}
			}
		});
	}

	private void HandlePause()
	{
		SimulationSystem simulationSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<SimulationSystem>();
		if (simulationSystem != null)
		{
			if (m_CachedSimulationSpeed == 0f)
			{
				m_CachedSimulationSpeed = simulationSystem.selectedSpeed;
			}
			simulationSystem.selectedSpeed = 0f;
		}
	}

	private void RestorePause()
	{
		if (m_Queue.Count == 0)
		{
			SimulationSystem simulationSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<SimulationSystem>();
			if (simulationSystem != null)
			{
				simulationSystem.selectedSpeed = m_CachedSimulationSpeed;
			}
		}
	}

	public void DismissCurrentError(int errorMuteCooldownSeconds = 0)
	{
		LinkedListNode<ErrorEntry> first = m_Queue.First;
		if (first != null)
		{
			if (m_StateByKey.TryGetValue(first.Value.key, out var value))
			{
				value.node = null;
				int num = SharedSettings.instance?.userInterface.errorMuteCooldownSeconds ?? 0;
				if (errorMuteCooldownSeconds == -1)
				{
					errorMuteCooldownSeconds = num;
				}
				value.cooldownUntilUtc = ((errorMuteCooldownSeconds == 0) ? ((DateTime?)null) : new DateTime?(DateTime.UtcNow.AddSeconds(errorMuteCooldownSeconds)));
				value.count = 0;
			}
			m_Queue.RemoveFirst();
			UpdateBindings();
		}
		RestorePause();
	}

	public void DismissAllErrors()
	{
		while (m_Queue.Count > 0)
		{
			DismissCurrentError();
		}
	}

	public void Clear()
	{
		m_Queue.Clear();
		m_StateByKey.Clear();
		m_CachedSimulationSpeed = 0f;
		UpdateBindings();
	}

	private LocalizedString GetTitle(Exception exception)
	{
		if (exception != null)
		{
			string name = exception.GetType().Name;
			return LocalizedString.IdWithFallback("Common.ERROR_TITLE[" + name + "]", name);
		}
		return null;
	}

	private LocalizedString GetMessage(Exception exception, string message)
	{
		string text = exception?.GetType().Name ?? "UnknownException";
		return LocalizedString.IdWithFallback("Common.ERROR_MESSAGE[" + text + "]", (message ?? string.Empty).Replace("\\", "\\\\"));
	}

	private static string GetCorruptedFileName(string path, IDataSourceProvider dataSource)
	{
		string text = "~Corrupted_";
		if (dataSource.isRemoteStorageSource)
		{
			text += dataSource.remoteStorageSourceName;
		}
		return text + Path.GetFileName(path);
	}

	private static string GetErrorDetailDisplay(Exception exception, string details)
	{
		if (exception is CorruptedCokException ex)
		{
			return ToLocalUri(ex.uri) + " ⇨ " + GetCorruptedFileName(ex.path, ex.dataSource);
		}
		if (exception is CorruptedContentException ex2)
		{
			return ex2.name;
		}
		return details;
	}

	private static string ToLocalUri(string uri)
	{
		string[] array = uri.Substring("assetdb://".Length).Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
		return array[0] + "://" + array[1];
	}

	private static string GetErrorDetail(Exception exception, UnityEngine.Object context)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (context != null)
		{
			string text = $"{context.name} ({context.GetType()})";
			string text2 = context.ToString();
			stringBuilder.AppendFormat("With object {0}", text).AppendLine();
			if (text != text2)
			{
				stringBuilder.AppendFormat("Additional info: {0}", text2).AppendLine();
			}
			stringBuilder.AppendLine();
		}
		if (exception != null)
		{
			StackTraceHelper.ExtractStackTraceFromException(exception, stringBuilder);
		}
		return StackTraceHelper.ExtractStackTrace(3, null, stringBuilder);
	}

	private static ErrorDialog.ActionBits GetActions(Exception exception)
	{
		if (GameManager.instance == null)
		{
			return ErrorDialog.ActionBits.Continue | ErrorDialog.ActionBits.Quit;
		}
		if (exception is CorruptedCokException)
		{
			return ErrorDialog.ActionBits.Ignore | ErrorDialog.ActionBits.Rename;
		}
		if (Platform.PlayStation.IsPlatformSet(Application.platform))
		{
			if (!GameManager.instance.gameMode.IsGameOrEditor())
			{
				return ErrorDialog.ActionBits.Continue | ErrorDialog.ActionBits.Quit;
			}
			return ErrorDialog.ActionBits.Continue | ErrorDialog.ActionBits.SaveAndContinue | ErrorDialog.ActionBits.Quit;
		}
		if (!GameManager.instance.gameMode.IsGameOrEditor())
		{
			return ErrorDialog.ActionBits.Continue | ErrorDialog.ActionBits.Quit;
		}
		return ErrorDialog.ActionBits.Continue | ErrorDialog.ActionBits.SaveAndQuit | ErrorDialog.ActionBits.Quit;
	}

	private static long UtcNowMs(DateTime nowUtc)
	{
		return nowUtc.Ticks / 10000;
	}

	private double BinWeight(int count)
	{
		if (count <= 0)
		{
			return 0.0;
		}
		double num = m_Spam.weightBase + Math.Log(1.0 + (double)count / m_Spam.weightK, 2.0);
		if (m_Spam.weightCap > 0.0)
		{
			num = Math.Min(num, m_Spam.weightCap);
		}
		return num;
	}

	public void Update()
	{
		LinkedListNode<ErrorEntry> first = m_Queue.First;
		if (first != null && m_StateByKey.TryGetValue(first.Value.key, out var value))
		{
			ErrorDialog error = first.Value.error;
			DateTime utcNow = DateTime.UtcNow;
			if ((0u | (RefreshSpamQuietClear(value, utcNow) ? 1u : 0u) | (UpdateMuteBit(value, error, utcNow) ? 1u : 0u)) != 0)
			{
				UpdateBindings();
			}
		}
	}

	private bool RefreshSpamQuietClear(FingerprintState state, DateTime nowUtc)
	{
		if (state.isSpam && state.lastSeenUtc != default(DateTime) && (nowUtc - state.lastSeenUtc).TotalSeconds >= m_Spam.quietClearSeconds)
		{
			state.isSpam = false;
			state.timestampsMs?.Clear();
			return true;
		}
		return false;
	}

	private bool UpdateMuteBit(FingerprintState state, ErrorDialog dialog, DateTime nowUtc)
	{
		bool num = (SharedSettings.instance?.userInterface.errorMuteCooldownSeconds ?? 0) > 0;
		bool flag = state.cooldownUntilUtc.HasValue && nowUtc < state.cooldownUntilUtc.Value;
		bool flag2 = num && state.isSpam && !flag;
		bool flag3 = (dialog.actions & ErrorDialog.ActionBits.Mute) != 0;
		if (flag2 == flag3)
		{
			return false;
		}
		dialog.actions = (flag2 ? (dialog.actions | ErrorDialog.ActionBits.Mute) : ((ErrorDialog.ActionBits)((uint)dialog.actions & 0xFFFFFEFFu)));
		return true;
	}

	private void RecordOccurrenceAndUpdateSpam(FingerprintState state, DateTime nowUtc)
	{
		if (state.isSpam && state.lastSeenUtc != default(DateTime) && (nowUtc - state.lastSeenUtc).TotalSeconds >= m_Spam.quietClearSeconds)
		{
			state.isSpam = false;
			state.timestampsMs?.Clear();
		}
		long num = UtcNowMs(nowUtc);
		long num2 = (long)(m_Spam.horizonSeconds * 1000.0);
		if (state.timestampsMs == null)
		{
			state.timestampsMs = new List<long>(32);
		}
		state.timestampsMs.Add(num);
		long num3 = num - num2;
		List<long> timestampsMs = state.timestampsMs;
		int i;
		for (i = 0; i < timestampsMs.Count && timestampsMs[i] < num3; i++)
		{
		}
		if (i > 0)
		{
			timestampsMs.RemoveRange(0, i);
		}
		int num4 = Math.Max(1, (int)Math.Ceiling(m_Spam.horizonSeconds / m_Spam.binSeconds));
		int[] array = new int[num4];
		double num5 = m_Spam.binSeconds * 1000.0;
		for (int num6 = timestampsMs.Count - 1; num6 >= 0; num6--)
		{
			long num7 = timestampsMs[num6];
			if (num7 < num3)
			{
				break;
			}
			int num8 = (int)Math.Floor((double)(num - num7) / num5);
			if (num8 >= 0 && num8 < num4)
			{
				array[num8]++;
			}
		}
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		double num12 = 0.0;
		for (int j = 0; j < num4; j++)
		{
			if (array[j] > 0)
			{
				num11++;
				num12 += BinWeight(array[j]);
				num9++;
				if (num9 > num10)
				{
					num10 = num9;
				}
			}
			else
			{
				num9 = 0;
			}
		}
		int num13 = Math.Min(m_Spam.minAdjacentBinsForBurst, num4);
		int num14 = Math.Min(m_Spam.minConsecutiveBinsForSingles, num4);
		bool num15 = num10 >= num13 && num11 >= m_Spam.minOccupiedBins && num12 >= m_Spam.threshold;
		bool flag = num10 >= num14;
		bool flag2 = num15 || flag;
		if (!state.isSpam && flag2)
		{
			state.isSpam = true;
		}
		state.lastSeenUtc = nowUtc;
	}

	private void EnqueueOrUpdate(Fingerprint key, ErrorDialog incoming, bool aggregateDetails)
	{
		DateTime utcNow = DateTime.UtcNow;
		if (!m_StateByKey.TryGetValue(key, out var value))
		{
			value = new FingerprintState();
			m_StateByKey[key] = value;
		}
		RecordOccurrenceAndUpdateSpam(value, utcNow);
		if (value.cooldownUntilUtc.HasValue && utcNow < value.cooldownUntilUtc.Value)
		{
			if (value.count < int.MaxValue)
			{
				value.count++;
			}
			return;
		}
		HandlePause();
		if (value.count < int.MaxValue)
		{
			value.count++;
		}
		if (value.node != null)
		{
			ErrorDialog error = value.node.Value.error;
			error.Merge(incoming, aggregateDetails);
			error.count = value.count;
			UpdateMuteBit(value, error, utcNow);
			if (m_Queue.First != value.node)
			{
				m_Queue.Remove(value.node);
				m_Queue.AddFirst(value.node);
			}
			UpdateBindings();
			return;
		}
		if (value.cooldownUntilUtc.HasValue && utcNow >= value.cooldownUntilUtc.Value)
		{
			value.cooldownUntilUtc = null;
			value.count = 0;
		}
		value.count = 1;
		incoming.count = value.count;
		incoming.fingerprint = key.ToString();
		if (incoming.actions == (ErrorDialog.ActionBits)0u)
		{
			incoming.actions = ErrorDialog.ActionBits.Continue;
		}
		UpdateMuteBit(value, incoming, utcNow);
		ErrorEntry value2 = new ErrorEntry(key, incoming);
		LinkedListNode<ErrorEntry> node = m_Queue.AddFirst(value2);
		value.node = node;
		UpdateBindings();
	}
}
