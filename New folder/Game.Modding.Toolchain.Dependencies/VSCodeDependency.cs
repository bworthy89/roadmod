using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;

namespace Game.Modding.Toolchain.Dependencies;

public class VSCodeDependency : BaseIDEDependency
{
	public override string name => "VS Code";

	public override string icon => "Media/Toolchain/VSCode.svg";

	public override bool canBeInstalled => false;

	public override bool canBeUninstalled => false;

	public override string minVersion => "1.86";

	protected override async Task<string> GetIDEVersion(CancellationToken token)
	{
		string installedVersion = string.Empty;
		List<string> errorText = new List<string>();
		try
		{
			await Cli.Wrap("code").WithArguments("--version").WithStandardOutputPipe(PipeTarget.ToDelegate(delegate(string l)
			{
				if (string.IsNullOrEmpty(installedVersion))
				{
					installedVersion = l;
				}
			}))
				.WithStandardErrorPipe(PipeTarget.ToDelegate(delegate(string l)
				{
					errorText.Add(l);
				}))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync(token)
				.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Win32Exception ex)
		{
			if (ex.ErrorCode != -2147467259)
			{
				ToolchainDependencyManager.log.Error(ex, "Failed to get VSCode version");
			}
		}
		catch (Exception exception)
		{
			ToolchainDependencyManager.log.Error(exception, "Failed to get VSCode version");
		}
		if (errorText.Count > 0)
		{
			IToolchainDependency.log.Warn(string.Join('\n', errorText));
		}
		return installedVersion;
	}
}
