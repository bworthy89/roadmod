using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game.Settings;
using Game.UI.Localization;

namespace Game.UI;

public class ErrorDialog : IJsonWritable
{
	[Flags]
	public enum ActionBits : uint
	{
		Continue = 1u,
		Ignore = 2u,
		Mute = 0x100u,
		SaveAndContinue = 0x200u,
		SaveAndQuit = 0x400u,
		Quit = 0x20000u,
		Rename = 0x40000u
	}

	private static class ActionLayout
	{
		public const ActionBits Group0Mask = (ActionBits)255u;

		public const ActionBits MuteMask = ActionBits.Mute;

		public const ActionBits Group1Mask = (ActionBits)130560u;

		public const ActionBits Group2Mask = (ActionBits)33423360u;

		public const ActionBits Group3Mask = (ActionBits)4261412864u;

		public static bool HasAny(ActionBits flags, ActionBits mask)
		{
			return (flags & mask) != 0;
		}

		public static ActionBits NormalizeExclusive(ActionBits flags, ActionBits mask, ActionBits defaultBit = (ActionBits)0u)
		{
			ActionBits actionBits = flags & mask;
			if (actionBits == (ActionBits)0u)
			{
				return (flags & ~mask) | defaultBit;
			}
			uint num = (uint)(actionBits & (~actionBits + 1));
			return (ActionBits)((uint)(flags & ~mask) | num);
		}

		public static ActionBits NormalizeOptional(ActionBits flags, ActionBits mask)
		{
			ActionBits actionBits = flags & mask;
			if (actionBits == (ActionBits)0u)
			{
				return flags & ~mask;
			}
			uint num = (uint)(actionBits & (~actionBits + 1));
			return (ActionBits)((uint)(flags & ~mask) | num);
		}

		private static IEnumerable<ActionBits> EnumerateGroup(ActionBits flags, ActionBits mask)
		{
			uint sel = (uint)(flags & mask);
			while (sel != 0)
			{
				uint lowest = sel & (~sel + 1);
				yield return (ActionBits)lowest;
				sel &= ~lowest;
			}
		}

		private static ActionBits Prepare(ActionBits flags)
		{
			if ((SharedSettings.instance?.userInterface.errorMuteCooldownSeconds ?? 0) <= 0)
			{
				flags = (ActionBits)((uint)flags & 0xFFFFFEFFu);
			}
			flags = NormalizeExclusive(flags, (ActionBits)255u, ActionBits.Continue);
			flags = NormalizeOptional(flags, (ActionBits)130560u);
			flags = NormalizeExclusive(flags, (ActionBits)33423360u, ActionBits.Quit);
			flags = NormalizeOptional(flags, (ActionBits)4261412864u);
			return flags;
		}

		public static int CountOrdered(ActionBits flags)
		{
			int num = 0;
			flags = Prepare(flags);
			foreach (ActionBits item in EnumerateGroup(flags, (ActionBits)255u))
			{
				if (BitToEntry(item) != null)
				{
					num++;
				}
			}
			foreach (ActionBits item2 in EnumerateGroup(flags, ActionBits.Mute))
			{
				if (BitToEntry(item2) != null)
				{
					num++;
				}
			}
			foreach (ActionBits item3 in EnumerateGroup(flags, (ActionBits)130560u))
			{
				if (BitToEntry(item3) != null)
				{
					num++;
				}
			}
			foreach (ActionBits item4 in EnumerateGroup(flags, (ActionBits)33423360u))
			{
				if (BitToEntry(item4) != null)
				{
					num++;
				}
			}
			foreach (ActionBits item5 in EnumerateGroup(flags, (ActionBits)4261412864u))
			{
				if (BitToEntry(item5) != null)
				{
					num++;
				}
			}
			return num;
		}

		public static IEnumerable<ActionEntry> EnumerateOrdered(ActionBits flags)
		{
			flags = Prepare(flags);
			foreach (ActionBits item in EnumerateGroup(flags, (ActionBits)255u))
			{
				ActionEntry actionEntry = BitToEntry(item);
				if (actionEntry != null)
				{
					yield return actionEntry;
				}
			}
			foreach (ActionBits item2 in EnumerateGroup(flags, ActionBits.Mute))
			{
				ActionEntry actionEntry2 = BitToEntry(item2);
				if (actionEntry2 != null)
				{
					yield return actionEntry2;
				}
			}
			foreach (ActionBits item3 in EnumerateGroup(flags, (ActionBits)130560u))
			{
				ActionEntry actionEntry3 = BitToEntry(item3);
				if (actionEntry3 != null)
				{
					yield return actionEntry3;
				}
			}
			foreach (ActionBits item4 in EnumerateGroup(flags, (ActionBits)33423360u))
			{
				ActionEntry actionEntry4 = BitToEntry(item4);
				if (actionEntry4 != null)
				{
					yield return actionEntry4;
				}
			}
			foreach (ActionBits item5 in EnumerateGroup(flags, (ActionBits)4261412864u))
			{
				ActionEntry actionEntry5 = BitToEntry(item5);
				if (actionEntry5 != null)
				{
					yield return actionEntry5;
				}
			}
		}

		private static ActionEntry BitToEntry(ActionBits bit)
		{
			return bit switch
			{
				ActionBits.Continue => Actions.Continue, 
				ActionBits.Ignore => Actions.Ignore, 
				ActionBits.SaveAndContinue => Actions.SaveAndContinue, 
				ActionBits.SaveAndQuit => Actions.SaveAndQuit, 
				ActionBits.Quit => Actions.Quit, 
				ActionBits.Rename => Actions.Rename, 
				ActionBits.Mute => Actions.Mute, 
				_ => null, 
			};
		}
	}

	public static class Actions
	{
		public const string kContinue = "Continue";

		public const string kSaveAndContinue = "SaveAndContinue";

		public const string kSaveAndQuit = "SaveAndQuit";

		public const string kQuit = "Quit";

		public const string kIgnore = "Ignore";

		public const string kRename = "Rename";

		public const string kMute = "Mute";

		public static readonly ActionEntry Continue = new ActionEntry("Continue");

		public static readonly ActionEntry SaveAndContinue = new ActionEntry("SaveAndContinue");

		public static readonly ActionEntry SaveAndQuit = new ActionEntry("SaveAndQuit");

		public static readonly ActionEntry Quit = new ActionEntry("Quit");

		public static readonly ActionEntry Ignore = new ActionEntry("Ignore");

		public static readonly ActionEntry Rename = new ActionEntry("Rename");

		private static int s_lastCooldown;

		private static ActionEntry s_lastMuteEntry;

		public static ActionEntry Mute
		{
			get
			{
				int num = SharedSettings.instance?.userInterface.errorMuteCooldownSeconds ?? 0;
				if (num <= 0)
				{
					return null;
				}
				if (s_lastMuteEntry != null && s_lastCooldown == num)
				{
					return s_lastMuteEntry;
				}
				Dictionary<string, ILocElement> args = new Dictionary<string, ILocElement> { 
				{
					"TIME",
					LocalizedString.Value(num.ToString())
				} };
				s_lastMuteEntry = new ActionEntry("Mute", args);
				s_lastCooldown = num;
				return s_lastMuteEntry;
			}
		}
	}

	public sealed class ActionEntry : IJsonWritable
	{
		public readonly LocalizedString localizedString;

		public readonly string name;

		public ActionEntry(string name)
			: this(name, LocalizedString.Id("Common.ERROR_ACTION[" + name + "]"))
		{
		}

		public ActionEntry(string name, LocalizedString localizedString)
		{
			this.name = name;
			this.localizedString = localizedString;
		}

		public ActionEntry(string name, IReadOnlyDictionary<string, ILocElement> args)
			: this(name, new LocalizedString("Common.ERROR_ACTION[" + name + "]", null, args))
		{
		}

		public static implicit operator ActionEntry(string name)
		{
			return new ActionEntry(name);
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("name");
			writer.Write(name);
			writer.PropertyName("localizedString");
			writer.Write(localizedString);
			writer.TypeEnd();
		}
	}

	public enum Severity
	{
		Warning,
		Error
	}

	public Severity severity = Severity.Error;

	public ActionBits actions = ActionBits.Continue | ActionBits.Quit;

	public LocalizedString localizedTitle;

	public LocalizedString localizedMessage;

	[CanBeNull]
	public string errorDetails;

	public int count;

	public string fingerprint;

	private HashSet<ErrorAsset> m_Assets;

	public IReadOnlyCollection<ErrorAsset> assets => m_Assets;

	public void AddAsset(IAssetException asset)
	{
		if (m_Assets == null)
		{
			m_Assets = new HashSet<ErrorAsset>();
		}
		m_Assets.Add(new ErrorAsset(asset));
	}

	public void Merge(ErrorDialog incoming, bool aggregateDetails)
	{
		severity = incoming.severity;
		localizedTitle = incoming.localizedTitle;
		localizedMessage = incoming.localizedMessage;
		errorDetails = (aggregateDetails ? (errorDetails + "\n" + incoming.errorDetails) : incoming.errorDetails);
		if (incoming.m_Assets != null)
		{
			if (m_Assets == null)
			{
				m_Assets = new HashSet<ErrorAsset>();
			}
			m_Assets.UnionWith(incoming.m_Assets);
		}
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("severity");
		writer.Write((int)severity);
		writer.PropertyName("actions");
		writer.ArrayBegin(ActionLayout.CountOrdered(actions));
		foreach (ActionEntry item in ActionLayout.EnumerateOrdered(actions))
		{
			writer.Write(item);
		}
		writer.ArrayEnd();
		writer.PropertyName("localizedTitle");
		writer.Write(localizedTitle);
		writer.PropertyName("localizedMessage");
		writer.Write(localizedMessage);
		writer.PropertyName("errorDetails");
		writer.Write(errorDetails);
		writer.PropertyName("count");
		writer.Write(count);
		writer.PropertyName("fingerprint");
		writer.Write(fingerprint);
		writer.TypeEnd();
	}
}
