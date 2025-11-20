using System.Collections.Generic;
using System.Linq;
using Game.UI.Localization;

namespace Game.Modding.Toolchain.Dependencies;

public class IDEDependency : CombinedDependency
{
	private BaseIDEDependency[] ides = new BaseIDEDependency[3]
	{
		new VisualStudioDependency(),
		new RiderDependency(),
		new VSCodeDependency()
	};

	public override IEnumerable<IToolchainDependency> dependencies => ides;

	public override CombineType type => CombineType.OR;

	protected override bool isAsync => true;

	public override bool canBeInstalled => false;

	public override bool canBeUninstalled => false;

	public override LocalizedString GetLocalizedState(bool includeProgress)
	{
		switch (base.state.m_State)
		{
		case DependencyState.Installed:
		{
			Dictionary<string, ILocElement> dictionary = new Dictionary<string, ILocElement> { 
			{
				"STATE",
				LocalizedString.Id("Options.STATE_TOOLCHAIN[Detected]")
			} };
			BaseIDEDependency[] array = ides;
			foreach (BaseIDEDependency baseIDEDependency in array)
			{
				if (baseIDEDependency.isMinVersion)
				{
					dictionary.Add($"Item{dictionary.Count}", new LocalizedString(null, "{NAME}", new Dictionary<string, ILocElement> { { "NAME", baseIDEDependency.localizedName } }));
				}
			}
			return new LocalizedString(null, "{STATE} (" + string.Join(", ", from k in dictionary.Keys.Skip(1)
				select "{" + k + "}") + ")", dictionary);
		}
		case DependencyState.NotInstalled:
			return LocalizedString.Id("Options.STATE_TOOLCHAIN[NotDetected]");
		default:
			return base.GetLocalizedState(includeProgress);
		}
	}
}
