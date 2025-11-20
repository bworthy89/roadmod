using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Game.Modding.Toolchain.Dependencies;

public class VisualStudioDependency : BaseIDEDependency
{
	public override string name => "Visual Studio";

	public override string icon => "Media/Toolchain/VisualStudio.svg";

	public override bool canBeInstalled => false;

	public override bool canBeUninstalled => false;

	public static string vsWhere => ToolchainDependencyManager.kGameToolingPath + "/vswhere.exe";

	public override string minVersion => "17.8";

	protected override Task<string> GetIDEVersion(CancellationToken token)
	{
		try
		{
			return GetIDEVersion(token, "-prerelease -format json");
		}
		catch
		{
			try
			{
				return GetIDEVersion(token, "-prerelease -format json");
			}
			catch (Exception exception)
			{
				ToolchainDependencyManager.log.Error(exception, "Failed to get Visual Studio version");
				return Task.FromResult(string.Empty);
			}
		}
	}

	private static async Task<string> GetIDEVersion(CancellationToken token, string arguments)
	{
		VsWhereResult vsWhereResult = await QueryVsWhere(token, arguments);
		return vsWhereResult.entries.Any() ? vsWhereResult.entries[0].catalog.buildVersion : string.Empty;
	}

	private static async Task<VsWhereResult> QueryVsWhere(CancellationToken token, string arguments)
	{
		StringBuilder vsWhereResult = new StringBuilder();
		List<string> errorText = new List<string>();
		vsWhereResult.AppendLine("{ \"entries\": ");
		await Cli.Wrap(vsWhere).WithArguments("-prerelease -latest -format json").WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
		{
			vsWhereResult.Append(l);
		}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				errorText.Add(l);
			}))
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync(token)
			.ConfigureAwait(continueOnCapturedContext: false);
		vsWhereResult.AppendLine("}");
		if (errorText.Count > 0)
		{
			IToolchainDependency.log.Warn(string.Join('\n', errorText));
		}
		return VsWhereResult.FromJson(vsWhereResult.ToString());
	}
}
