#define UNITY_ASSERTIONS
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Colossal;
using Colossal.Animations;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Collectors;
using Colossal.AssetPipeline.Diagnostic;
using Colossal.AssetPipeline.Importers;
using Colossal.AssetPipeline.Native;
using Colossal.AssetPipeline.PostProcessors;
using Colossal.Collections.Generic;
using Colossal.Core.Utils;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Json;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Colossal.Reflection;
using Game.Prefabs;
using Game.Rendering;
using Game.Rendering.Debug;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;

namespace Game.AssetPipeline;

public static class AssetImportPipeline
{
	private class ThemesConfig
	{
		public string[] themePrefixes;
	}

	private class Progress
	{
		public class ScopedThreadDescriptionObject : IDisposable
		{
			private Progress owner;

			public ScopedThreadDescriptionObject(Progress owner, string description)
			{
				this.owner = owner;
				owner.SetThreadDescription(description);
			}

			public void Dispose()
			{
				owner.SetThreadDescription(null);
			}
		}

		private string title;

		private string description;

		private string threadDescription;

		private float value;

		private Func<string, string, float, bool> progressCallback;

		private Thread mainThread;

		public bool shouldCancel { get; private set; }

		public Progress()
		{
			mainThread = Thread.CurrentThread;
		}

		public void SetHandler(Func<string, string, float, bool> progressCallback)
		{
			this.progressCallback = progressCallback;
		}

		public void Reset(Func<string, string, float, bool> progressCallback)
		{
			this.progressCallback = progressCallback;
			shouldCancel = false;
		}

		public void Set(string title, string description, float value)
		{
			this.title = title;
			this.description = description;
			Interlocked.Exchange(ref this.value, value);
			if (mainThread == Thread.CurrentThread)
			{
				Update();
			}
		}

		public ScopedThreadDescriptionObject ScopedThreadDescription(string description)
		{
			return new ScopedThreadDescriptionObject(this, description);
		}

		public void SetThreadDescription(string description)
		{
			threadDescription = description;
			if (mainThread == Thread.CurrentThread)
			{
				Update();
			}
		}

		public void Set(string title, string description)
		{
			this.title = title;
			this.description = description;
			if (mainThread == Thread.CurrentThread)
			{
				Update();
			}
		}

		public void Update()
		{
			string text = threadDescription ?? description;
			if (shouldCancel)
			{
				text += " (Canceled)";
			}
			if (progressCallback != null && progressCallback(title, text, value))
			{
				shouldCancel = true;
			}
		}
	}

	private static readonly ILog log;

	private static readonly string[] kSettings;

	public static bool useParallelImport;

	public static ILocalAssetDatabase targetDatabase;

	private static readonly ProfilerMarker s_ImportPath;

	private static readonly ProfilerMarker s_ProfPostImport;

	private static readonly ProfilerMarker s_ProfImportModels;

	private static readonly ProfilerMarker s_ProfImportTextures;

	private static readonly ProfilerMarker s_ProfCreateGeomsSurfaces;

	private static readonly ProfilerMarker s_ProfImportAssetGroup;

	private static readonly Progress s_Progress;

	private static MainThreadDispatcher s_MainThreadDispatcher;

	private static int parallelCount;

	private static int total;

	private static int importsCount;

	public static Action<string, Texture> OnDebugTexture;

	private static Material s_BackgroundMaterial;

	private const float kBoundSize = 0.1f;

	private const float kHalfBoundSize = 0.05f;

	public static Material backgroundMaterial
	{
		get
		{
			if (s_BackgroundMaterial == null)
			{
				s_BackgroundMaterial = new Material(Shader.Find("HDRP/Unlit"));
				s_BackgroundMaterial.color = Color.white;
			}
			return s_BackgroundMaterial;
		}
	}

	static AssetImportPipeline()
	{
		log = LogManager.GetLogger("AssetPipeline");
		kSettings = new string[4] { "AssetCatalog.json", "CharacterDescriptor.json", "settings.json", "config.json" };
		useParallelImport = true;
		s_ImportPath = new ProfilerMarker("AssetImportPipeline.ImportPath");
		s_ProfPostImport = new ProfilerMarker("AssetImportPipeline.PostImportMainThread");
		s_ProfImportModels = new ProfilerMarker("AssetImportPipeline.ImportModels");
		s_ProfImportTextures = new ProfilerMarker("AssetImportPipeline.ImportTextures");
		s_ProfCreateGeomsSurfaces = new ProfilerMarker("AssetImportPipeline.CreateGeometriesAndSurfaces");
		s_ProfImportAssetGroup = new ProfilerMarker("AssetImportPipeline.ImportAssetGroup");
		s_Progress = new Progress();
		parallelCount = 0;
		total = 0;
		importsCount = 0;
		ImporterCache.GetSupportedExtensions();
	}

	private static string GetNameWithoutGUID(string str)
	{
		return str.Substring(0, str.LastIndexOf("_"));
	}

	private static async Task ExecuteMainThreadQueue(Task importTask, Report report)
	{
		using (report.AddImportStep("Process main thread task queue"))
		{
			while ((!importTask.IsCompleted || s_MainThreadDispatcher.hasPendingTasks) && !s_Progress.shouldCancel)
			{
				s_Progress.Update();
				if (s_MainThreadDispatcher.hasPendingTasks)
				{
					s_Progress.SetThreadDescription($"Executing {s_MainThreadDispatcher.pendingTasksCount} tasks");
					s_MainThreadDispatcher.ProcessTasks();
				}
				await Task.Yield();
			}
		}
		await importTask;
	}

	private static string MakeRelativePath(string path, string rootPath)
	{
		if (path.IndexOf(rootPath) == 0)
		{
			return path.Substring(rootPath.Length + 1);
		}
		throw new FormatException(path + " is not relative to " + rootPath);
	}

	public static void SetReportCallback(Func<string, string, float, bool> progressCallback)
	{
		s_Progress.Reset(progressCallback);
	}

	private static void AddSupportedThemes(string projectRootPath)
	{
		string path = projectRootPath + "/themes.json";
		if (!LongFile.Exists(path))
		{
			return;
		}
		Variant variant = JSON.Load(LongFile.ReadAllText(path).Trim());
		if (variant != null)
		{
			ThemesConfig themesConfig = JSON.MakeInto<ThemesConfig>(variant);
			if (themesConfig.themePrefixes != null)
			{
				AssetUtils.AddSupportedThemes(themesConfig.themePrefixes);
				log.InfoFormat("Theme prefixes added: {0}", string.Join(',', themesConfig.themePrefixes));
			}
		}
	}

	public static async Task ImportPath(string projectRootPath, IEnumerable<string> relativePaths, ImportMode importMode, bool convertToVT, Func<string, string, float, bool> progressCallback = null, IPrefabFactory prefabFactory = null)
	{
		if (targetDatabase == null)
		{
			throw new Exception("targetDatabase must be set");
		}
		if (s_MainThreadDispatcher == null)
		{
			s_MainThreadDispatcher = new MainThreadDispatcher();
		}
		using (s_ImportPath.Auto())
		{
			Report report = new Report();
			int failures = 0;
			using (PerformanceCounter perf = PerformanceCounter.Start(delegate(TimeSpan t)
			{
				log.Info(string.Format("Completed {0} asset groups import in {1:F3}s. Errors {2}. {3}", total, t.TotalSeconds, failures, s_Progress.shouldCancel ? "(Canceled)" : ""));
			}))
			{
				using (Report.ImportStep report2 = report.AddImportStep("Cache importers & post processors"))
				{
					ImporterCache.CacheSupportedExtensions(report2);
					PostProcessorCache.CachePostProcessors(report2);
				}
				SetReportCallback(progressCallback);
				AddSupportedThemes(projectRootPath);
				SourceAssetCollector assetCollector;
				using (report.AddImportStep("Collect source assets"))
				{
					s_Progress.Set("Importing assets", "Collecting files...", 0f);
					assetCollector = new SourceAssetCollector(projectRootPath, relativePaths, (string x) => AssetCatalogSettingsExtensions.kCatalogSettings.Any((string s) => s.Contains(x)));
				}
				ParallelOptions opts = new ParallelOptions
				{
					MaxDegreeOfParallelism = ((!useParallelImport) ? 1 : Environment.ProcessorCount)
				};
				HashSet<SurfaceAsset> VTMaterials = new HashSet<SurfaceAsset>();
				parallelCount = 0;
				total = 0;
				importsCount = assetCollector.count;
				await ExecuteMainThreadQueue(Task.Run(() => Parallel.ForEach(assetCollector, opts, delegate(SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset> asset, ParallelLoopState state, long index)
				{
					string relativeRootPath = MakeRelativePath(asset.rootPath, projectRootPath);
					Report.Asset assetReport = report.AddAsset(asset.name);
					Interlocked.Increment(ref parallelCount);
					s_Progress.Set($"Importing {parallelCount} assets (group {total + 1}/{importsCount})", "Importing textures and meshes for " + asset.name, (float)total / (float)importsCount);
					if (s_Progress.shouldCancel)
					{
						state.Stop();
					}
					if (ImportAssetGroup(projectRootPath, relativeRootPath, asset, out var assetCount, out var postImportOperations, report, assetReport))
					{
						s_MainThreadDispatcher.Dispatch(delegate
						{
							postImportOperations?.Invoke(relativeRootPath, importMode, report, VTMaterials, prefabFactory);
						});
					}
					else
					{
						Interlocked.Increment(ref failures);
					}
					Interlocked.Add(ref parallelCount, -assetCount);
					Interlocked.Increment(ref total);
					s_Progress.Set($"Importing {parallelCount} assets (group {total}/{importsCount})", "Completed textures and meshes for " + asset.name, (float)total / (float)importsCount);
				})), report);
				if (convertToVT)
				{
					using Report.ImportStep report3 = report.AddImportStep("Convert materials to VT");
					ProcessSurfacesForVT(VTMaterials, null, (importMode & ImportMode.Forced) == ImportMode.Forced, report3);
				}
				s_Progress.Set("Completed", "", 1f);
				report.totalTime = perf.result;
			}
			report.Log(log);
			s_MainThreadDispatcher = null;
		}
	}

	private static Colossal.AssetPipeline.Settings ImportSettings(string projectRootPath, SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset> assetGroup, (Report.ImportStep step, Report.Asset asset) report)
	{
		using Report.ImportStep importStep = report.step.AddImportStep("Settings import");
		bool flag = false;
		Colossal.AssetPipeline.Settings settings = Colossal.AssetPipeline.Settings.GetDefault(assetGroup.name);
		string[] array = kSettings;
		foreach (string configFile in array)
		{
			SourceAssetCollector.Asset asset = assetGroup.SingleOrDefault((SourceAssetCollector.Asset a) => a.name == configFile);
			if (string.IsNullOrEmpty(asset.name) || !(ImporterCache.GetImporter(Path.GetExtension(asset.path)) is SettingsImporter settingsImporter))
			{
				continue;
			}
			Report.FileReport report2 = report.asset.AddFile(asset);
			if (AssetCatalogSettingsExtensions.kCatalogSettings.Contains(asset.name))
			{
				Colossal.AssetPipeline.Settings settings2 = AssetCatalogSettingsExtensions.GetDefault();
				settingsImporter.TryImport(projectRootPath, asset.path, mergeWithAncestry: false, expand: false, ref settings2, report2);
				string name = asset.name;
				if (!(name == "CharacterDescriptor.json"))
				{
					if (!(name == "AssetCatalog.json"))
					{
						throw new Exception("Catalog settings " + asset.name + " is missing a serialization mapping.");
					}
					settings.animationExportData = settings2.animationExportData;
					settings.deformableAssets = settings2.deformableAssets;
					settings.materials = settings2.materials;
					settings.overlayAssets = settings2.overlayAssets;
					settings.propAssets = settings2.propAssets;
					settings.shapeAssets = settings2.shapeAssets;
					settings.templateAssets = settings2.templateAssets;
					settings.textures = settings2.textures;
				}
				else
				{
					settings.groups = settings2.groups;
				}
			}
			else
			{
				bool flag2 = settingsImporter.TryImport(projectRootPath, asset.path, mergeWithAncestry: true, expand: true, ref settings, report2);
				if (asset.name == "settings.json")
				{
					flag = flag2;
				}
			}
		}
		if (settings.IsAssetCatalogImport())
		{
			settings.RedirectCatalogIndices(projectRootPath, assetGroup, report);
		}
		else if (settings.useProceduralAnimation)
		{
			settings.createProceduralAnimationForPrefab.Add(settings.mainAsset);
		}
		if (!flag)
		{
			SettingsImporter.Expand(ref settings, importStep);
			importStep.AddMessage("Using default settings: " + settings.ToJSONString());
		}
		return settings;
	}

	private static string ResolveRelativePath(string projectRootPath, string target, string to)
	{
		if (target.StartsWith('/'))
		{
			return Path.GetFullPath(Path.Combine(projectRootPath, target.Substring(1)));
		}
		return Path.GetFullPath(Path.Combine(to, target));
	}

	private static SourceAssetCollector.AssetGroup<IAsset> CreateAssetGroupFromSettings(string projectRootPath, ref Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset> assetGroup, Report.Asset assetReport)
	{
		HashSet<IAsset> hashSet = new HashSet<IAsset>(assetGroup.count);
		foreach (SourceAssetCollector.Asset item in assetGroup)
		{
			if (settings.ignoreSuffixes == null || !Path.GetFileNameWithoutExtension(item.name).EndsWithAny(settings.ignoreSuffixes))
			{
				IAsset asset = IAsset.Create(settings, Path.GetFileNameWithoutExtension(item.name), item);
				if (asset != null)
				{
					hashSet.Add(asset);
				}
			}
		}
		if (settings.IsAssetCatalogImport())
		{
			foreach (string catalogAsset in AssetCatalogSettingsExtensions.GetCatalogAssets(ref settings, assetGroup.rootPath, useParallelImport, assetReport))
			{
				SourceAssetCollector.Asset sourceAsset = new SourceAssetCollector.Asset(catalogAsset, projectRootPath);
				IAsset asset2 = IAsset.Create(settings, Path.GetFileNameWithoutExtension(sourceAsset.name), sourceAsset);
				if (asset2 != null)
				{
					hashSet.Add(asset2);
				}
			}
		}
		foreach (KeyValuePair<string, string> item2 in settings.UsedShaderAssets(assetGroup, assetReport))
		{
			string path = ResolveRelativePath(projectRootPath, item2.Value, assetGroup.rootPath);
			if (!LongFile.Exists(path))
			{
				string fileName = Path.GetFileName(path);
				path = EnvPath.kContentPath + "/Game/.ModdingToolchain/shared_assets_fallback/" + fileName;
				log.InfoFormat("Using fallback {0}", fileName);
			}
			if (LongFile.Exists(path))
			{
				IAsset asset3 = IAsset.Create(sourceAsset: new SourceAssetCollector.Asset(path, projectRootPath), settings: settings, name: Path.GetFileNameWithoutExtension(item2.Key));
				if (asset3 != null)
				{
					hashSet.Add(asset3);
				}
			}
		}
		return new SourceAssetCollector.AssetGroup<IAsset>(assetGroup.rootPath, hashSet);
	}

	private static void ImportModels(Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, (Report.ImportStep step, Report.Asset asset) report)
	{
		using (s_ProfImportModels.Auto())
		{
			Report.ImportStep modelsReport = report.step.AddImportStep("Import Models");
			try
			{
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = ((!useParallelImport) ? 1 : Environment.ProcessorCount)
				};
				int failures = 0;
				int total = 0;
				using (PerformanceCounter.Start(delegate(TimeSpan t)
				{
					if (total == 0)
					{
						log.Info($"No models processed. All models in this group were already loaded or none were found. {t.TotalSeconds:F3}");
					}
					else
					{
						log.Info($"Completed {total} models import in {t.TotalSeconds:F3}s. Errors {failures}.");
					}
				}))
				{
					Parallel.ForEach(assetGroup.FilterBy<ModelAsset>(), parallelOptions, delegate(ModelAsset asset, ParallelLoopState state, long index)
					{
						if (asset.instance != null)
						{
							return;
						}
						Report.FileReport fileReport = report.asset.AddFile(asset);
						using (s_Progress.ScopedThreadDescription("Importing " + asset.fileName))
						{
							if (s_Progress.shouldCancel)
							{
								state.Stop();
							}
							try
							{
								ISettings importSettings = settings.GetImportSettings(asset.fileName, asset.importer, fileReport);
								using (modelsReport.AddImportStep("Asset import"))
								{
									if (asset.importer.Import<ModelImporter.ModelList>(importSettings, asset.path, fileReport, out asset.instance))
									{
										asset.instance.sourceAsset = asset;
										Interlocked.Increment(ref total);
										if (asset.instance.isValid)
										{
											foreach (IModelPostProcessor modelPostProcessor in PostProcessorCache.GetModelPostProcessors())
											{
												if (settings.GetPostProcessSettings(asset.fileName, modelPostProcessor, fileReport, out var settings2))
												{
													Context context = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings2, settings);
													if (modelPostProcessor.ShouldExecute(context, asset))
													{
														using (modelsReport.AddImportStep("Execute " + PPUtils.GetPostProcessorString(modelPostProcessor.GetType()) + " Post Processors"))
														{
															modelPostProcessor.Execute(context, asset, fileReport);
														}
													}
												}
											}
											return;
										}
									}
								}
							}
							catch (Exception ex)
							{
								Interlocked.Increment(ref failures);
								log.Error(ex, "Error importing " + asset.name + ". Skipped...");
								fileReport.AddError($"Error: {ex}");
							}
						}
					});
				}
			}
			finally
			{
				if (modelsReport != null)
				{
					((IDisposable)modelsReport).Dispose();
				}
			}
		}
	}

	private static bool MakeRestPose(Colossal.AssetPipeline.Settings settings, ModelAsset modelAsset, int shapeCount, string[] boneNames, bool useBakedAnimationData, (Report.ImportStep step, Report.Asset asset) report, out int[] boneHierarchy, out Animation.ElementRaw[] elements)
	{
		int num = boneNames.Length;
		elements = new Animation.ElementRaw[num * shapeCount];
		boneHierarchy = new int[num];
		if (!useBakedAnimationData)
		{
			ModelImporter.Model.BoneInfo[] array = modelAsset.instance.SelectMany((ModelImporter.Model model) => model.bones).ToArray();
			if (boneNames.Length != array.Length)
			{
				throw new Exception($"Cannot make rest pose for {modelAsset.name}: model has {array.Length} while catalog file expects {boneNames.Length}");
			}
			int i;
			for (i = 0; i < boneNames.Length; i++)
			{
				ModelImporter.Model.BoneInfo boneInfo = array.Single((ModelImporter.Model.BoneInfo b) => b.name == boneNames[i]);
				boneHierarchy[i] = boneInfo.parentIndex;
				elements[i] = new Animation.ElementRaw
				{
					position = boneInfo.localPosition,
					rotation = new float4(boneInfo.localRotation.x, boneInfo.localRotation.y, boneInfo.localRotation.z, boneInfo.localRotation.w)
				};
			}
			return true;
		}
		Report.FileReport report2 = report.asset.AddFile(modelAsset);
		ISettings importSettings = settings.GetImportSettings(modelAsset.fileName, modelAsset.importer, report2);
		if (!modelAsset.importer.ImportAnimationData<ModelImporter.AnimationData>(importSettings, modelAsset.path, report2, out var instance) || instance.nodeCount == 0)
		{
			return false;
		}
		if (boneNames.Length != instance.nodes.Length)
		{
			throw new Exception($"Cannot make rest pose for {modelAsset.assetName}: model has {instance.nodes.Length} while catalog file expects {boneNames.Length}");
		}
		boneHierarchy = instance.boneParentsHierarchy;
		for (int num2 = 0; num2 < num; num2++)
		{
			NativeArray<ModelImporter.AnimationData.AnimationNodeSample> nativeArray = instance.nodes[num2].samples.Reinterpret<ModelImporter.AnimationData.AnimationNodeSample>(1);
			for (int num3 = 0; num3 < shapeCount; num3++)
			{
				elements[num2 * shapeCount + num3] = new Animation.ElementRaw
				{
					position = nativeArray[num3].pos,
					rotation = new float4(nativeArray[num3].rot.x, nativeArray[num3].rot.y, nativeArray[num3].rot.z, nativeArray[num3].rot.w)
				};
			}
		}
		instance.Dispose();
		return true;
	}

	private static void FixAnimationLastFrame(List<Animation.ElementRaw> animationElements, int activeBoneCount, int activeShapeCount, int frameCount)
	{
		int num = activeBoneCount * activeShapeCount * (frameCount + 1);
		if (frameCount > 1 && num == animationElements.Count + activeBoneCount * activeShapeCount)
		{
			animationElements.AddRange(animationElements.Take(activeBoneCount * activeShapeCount).ToArray());
		}
	}

	private static void CreateAnimationElements(List<Animation.ElementRaw> elements, AnimationTargetData animationTargetData, int activeShapeIndex, int activeShapeCount, int frameCount, int[] activeBoneIndices, bool[] activeBonesPerShape)
	{
		int num = activeBoneIndices.Length;
		for (int i = 0; i < frameCount; i++)
		{
			int num2 = i * num * activeShapeCount;
			int num3 = 0;
			for (int j = 0; j < num; j++)
			{
				int num4 = activeBoneIndices[j];
				Animation.ElementRaw value;
				if (activeBonesPerShape[num4])
				{
					value = animationTargetData.elements[num3 * frameCount + i];
					num3++;
				}
				else
				{
					value = Animation.ElementRaw.Identity;
				}
				elements[num2 + j * activeShapeCount + activeShapeIndex] = value;
			}
		}
	}

	private static Animation.ElementRaw[] CreateAnimationElements(List<AnimationTargetData> animationTargetData, int activeShapeCount, int frameCount, int[] activeBoneIndices, List<bool[]> activeBonesPerShape)
	{
		int num = activeBoneIndices.Length;
		List<Animation.ElementRaw> list = Enumerable.Repeat(Animation.ElementRaw.Identity, activeShapeCount * frameCount * num).ToList();
		for (int i = 0; i < activeShapeCount; i++)
		{
			CreateAnimationElements(list, animationTargetData[i], i, activeShapeCount, frameCount, activeBoneIndices, activeBonesPerShape[i]);
		}
		FixAnimationLastFrame(list, activeBoneIndices.Length, activeShapeCount, frameCount);
		return list.ToArray();
	}

	private static (List<(Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash)>, Dictionary<string, int[]>) ImportAnimations(ref Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, string[] modelNames, int templateIndex, (Report.ImportStep step, Report.Asset asset) report)
	{
		using (s_ProfImportModels.Auto())
		{
			using Report.ImportStep item = report.step.AddImportStep("Import Animations");
			List<(Animation, int[], string, int, Colossal.Hash128)> list = new List<(Animation, int[], string, int, Colossal.Hash128)>(settings.animationExportData.animations.Count((AnimationExportAsset a) => a.templateIndex == templateIndex) + 1);
			List<(Animation, int[], string, int, Colossal.Hash128)> list2 = new List<(Animation, int[], string, int, Colossal.Hash128)>();
			ModelExportAsset templateAsset = settings.templateAssets[templateIndex];
			ModelAsset modelAsset = assetGroup.Find((ModelAsset a) => a.path.Contains(templateAsset.path));
			if (modelAsset == null)
			{
				return (null, null);
			}
			int num = (from s in settings.shapeAssets
				where s.templateIndex == templateIndex
				select s.assetName).Count() + 1;
			string[] boneNames = settings.animationExportData.templatesBones[templateIndex].boneNames;
			Animation animation = new Animation
			{
				name = templateAsset.assetName + "_RestPose",
				type = Colossal.Animations.AnimationType.RestPose,
				layer = Colossal.Animations.AnimationLayer.All,
				shapeIndices = Enumerable.Range(0, num).ToArray(),
				frameCount = 1
			};
			for (int num2 = 0; num2 < num; num2++)
			{
				animation.shapeIndices[num2] = num2;
			}
			if (!MakeRestPose(settings, modelAsset, num, boneNames, useBakedAnimationData: true, (step: item, asset: report.asset), out var boneHierarchy, out var elements))
			{
				return (null, null);
			}
			animation.SetElements(elements);
			animation.boneIndices = Enumerable.Range(0, boneHierarchy.Length).ToArray();
			list.Add((animation, boneHierarchy, "RestPose", -1, HashUtils.GetHash(animation, relativeRootPath)));
			Dictionary<string, int[]> dictionary = new Dictionary<string, int[]>();
			foreach (AnimationExportAsset animation5 in settings.animationExportData.animations)
			{
				if (animation5.templateIndex != templateIndex)
				{
					continue;
				}
				string path = Path.Combine(assetGroup.rootPath, templateAsset.assetName, "Animation", animation5.name, animation5.animationBinFile);
				animation5.ReadBinFile(path);
				AnimationTargetData[] array = animation5.shapeAnimationData.OrderBy((AnimationTargetData a) => a.targetIndex).ToArray();
				int num3 = animation5.shapeAnimationData.Count + 1;
				int[] array2 = new int[num3];
				List<bool[]> list3 = new List<bool[]>(num3);
				HashSet<int> hashSet = new HashSet<int>();
				array2[0] = 0;
				list3.Add(Enumerable.Repeat(element: false, boneHierarchy.Length).ToArray());
				int[] boneIndices = animation5.templateAnimationData.boneIndices;
				foreach (int num5 in boneIndices)
				{
					list3[0][num5] = true;
					hashSet.Add(num5);
				}
				for (int num6 = 0; num6 < array.Length; num6++)
				{
					AnimationTargetData animationTargetData = array[num6];
					array2[num6 + 1] = settings.shapeAssets[animationTargetData.targetIndex].shapeIndex + 1;
					list3.Add(Enumerable.Repeat(element: false, boneHierarchy.Length).ToArray());
					boneIndices = animationTargetData.boneIndices;
					foreach (int num7 in boneIndices)
					{
						list3[num6 + 1][num7] = true;
						hashSet.Add(num7);
					}
				}
				int[] array3 = (from y in hashSet.ToArray()
					orderby y
					select y).ToArray();
				List<AnimationTargetData> list4 = new List<AnimationTargetData>();
				list4.Add(animation5.templateAnimationData);
				list4.AddRange(array);
				Animation animation2 = new Animation
				{
					name = templateAsset.assetName + "_" + animation5.name,
					layer = animation5.GetAnimatedLayer(),
					type = Colossal.Animations.AnimationType.Additive,
					frameRate = animation5.fps,
					frameCount = animation5.frameCount,
					boneIndices = array3,
					shapeIndices = array2
				};
				Animation.ElementRaw[] source = CreateAnimationElements(list4, num3, animation5.frameCount, array3, list3);
				animation2.SetElements(source.ToArray());
				int item2 = -1;
				if (animation5.propAnimationData.Count > 0)
				{
					AnimationTargetData animationTargetData2 = animation5.propAnimationData.First();
					ModelExportAsset propAsset = settings.propAssets[animationTargetData2.targetIndex];
					item2 = modelNames.IndexOf(propAsset.assetName + "_Prop");
					if (animationTargetData2.boneIndices.Length != 0)
					{
						if (!dictionary.TryGetValue(propAsset.assetName, out var value))
						{
							Animation animation3 = new Animation
							{
								name = "RestPose#" + propAsset.assetName,
								layer = Colossal.Animations.AnimationLayer.PropLayer,
								type = Colossal.Animations.AnimationType.RestPose,
								frameRate = animation5.fps,
								frameCount = 1,
								shapeIndices = new int[1]
							};
							ModelAsset modelAsset2 = assetGroup.Find((ModelAsset a) => a.path.Contains(propAsset.path));
							AnimationExportBoneData animationExportBoneData = settings.animationExportData.propBones[animation5.propAnimationData.First().targetIndex];
							if (!MakeRestPose(settings, modelAsset2, animation3.shapeIndices.Length, animationExportBoneData.boneNames, useBakedAnimationData: false, (step: item, asset: report.asset), out value, out var elements2))
							{
								throw new Exception("Unable to make rest pose for asset " + modelAsset.name);
							}
							animation3.SetElements(elements2);
							animation3.boneIndices = Enumerable.Range(0, value.Length).ToArray();
							list2.Add((animation3, value, "RestPose", -1, HashUtils.GetHash(animation3, relativeRootPath)));
							dictionary.Add(propAsset.assetName, value);
						}
						bool[] array4 = Enumerable.Repeat(element: false, value.Length).ToArray();
						boneIndices = animationTargetData2.boneIndices;
						foreach (int num8 in boneIndices)
						{
							array4[num8] = true;
						}
						Animation animation4 = new Animation
						{
							name = animation2.name + "#" + propAsset.assetName,
							layer = Colossal.Animations.AnimationLayer.PropLayer,
							type = Colossal.Animations.AnimationType.Additive,
							frameRate = animation5.fps,
							frameCount = animation5.frameCount,
							boneIndices = animationTargetData2.boneIndices,
							shapeIndices = new int[1]
						};
						List<Animation.ElementRaw> list5 = new Animation.ElementRaw[animation4.frameCount * animation4.boneIndices.Length * animation4.shapeIndices.Length].ToList();
						CreateAnimationElements(list5, animationTargetData2, 0, animation4.shapeIndices.Length, animation5.frameCount, animationTargetData2.boneIndices, array4);
						FixAnimationLastFrame(list5, animation4.boneIndices.Length, animation4.shapeIndices.Length, animation5.frameCount);
						animation4.SetElements(list5.ToArray());
						list2.Add((animation4, value, animation5.name, templateIndex, HashUtils.GetHash(animation4, relativeRootPath)));
						if (settings.createProceduralAnimationForPrefab.Contains(propAsset.assetName))
						{
							ref Dictionary<string, Colossal.AssetPipeline.Settings.AnimationDefinition> animations = ref settings.animations;
							if (animations == null)
							{
								animations = new Dictionary<string, Colossal.AssetPipeline.Settings.AnimationDefinition>();
							}
							settings.animations.Add(templateAsset.assetName + "_" + animation5.name + "#" + propAsset.assetName, default(Colossal.AssetPipeline.Settings.AnimationDefinition));
						}
					}
				}
				list.Add((animation2, boneHierarchy, animation5.name, item2, HashUtils.GetHash(animation2, relativeRootPath)));
			}
			list = (from a in list
				orderby a.data.layer, a.data.type, a.shortName
				select a).ToList();
			list.AddRange((from a in list2
				orderby a.targetRenderGroup, a.data.type, a.shortName
				select a).ToList());
			return (list, dictionary);
		}
	}

	private static TTo CastStruct<TFrom, TTo>(TFrom s) where TFrom : struct where TTo : struct
	{
		TFrom from = s;
		return UnsafeUtility.As<TFrom, TTo>(ref from);
	}

	private static CharacterStyle CreateStylePrefab((string name, IReadOnlyList<(Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash)> animations, int boneCount, int shapeCount) style, string sourcePath, Dictionary<Colossal.Hash128, AnimationAsset> overrideSafeGuard, List<RenderPrefab> prefabs, IPrefabFactory prefabFactory = null)
	{
		CharacterStyle characterStyle = CreatePrefab<CharacterStyle>("Style", sourcePath, style.name, 0, prefabFactory);
		int num = 0;
		Dictionary<Colossal.Hash128, CharacterStyle.AnimationInfo> dictionary = new Dictionary<Colossal.Hash128, CharacterStyle.AnimationInfo>();
		if (characterStyle.m_Animations != null)
		{
			CharacterStyle.AnimationInfo[] animations = characterStyle.m_Animations;
			foreach (CharacterStyle.AnimationInfo animationInfo in animations)
			{
				dictionary[animationInfo.animationAsset.guid] = animationInfo;
			}
		}
		characterStyle.m_ShapeCount = style.shapeCount;
		characterStyle.m_BoneCount = style.boneCount;
		characterStyle.m_Animations = new CharacterStyle.AnimationInfo[style.animations?.Count ?? 0];
		Dictionary<int, Animation> dictionary2 = new Dictionary<int, Animation>();
		foreach (var item in style.animations)
		{
			if (item.data.type == Colossal.Animations.AnimationType.RestPose)
			{
				(dictionary2[(item.data.layer == Colossal.Animations.AnimationLayer.PropLayer) ? item.targetRenderGroup : (-1)], _, _, _, _) = item;
			}
		}
		foreach (var item2 in style.animations)
		{
			using (s_Progress.ScopedThreadDescription("Processing animation " + item2.data.name))
			{
				Assert.AreEqual(item2.boneHierarchy.Length, style.boneCount);
				bool flag = false;
				if (!overrideSafeGuard.TryGetValue(item2.hash, out var value))
				{
					Colossal.Animations.AnimationClip animationClip = new Colossal.Animations.AnimationClip
					{
						m_BoneHierarchy = item2.boneHierarchy,
						m_Animation = item2.data
					};
					using AnimationAsset animationAsset = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{animationClip.name}_{HashUtils.GetHash(animationClip, sourcePath)}"), animationClip);
					animationAsset.Save();
					value = animationAsset;
					overrideSafeGuard.Add(item2.hash, animationAsset);
					flag = true;
				}
				if (dictionary.TryGetValue(value.id, out var value2))
				{
					characterStyle.m_Animations[num] = value2;
				}
				else
				{
					characterStyle.m_Animations[num] = new CharacterStyle.AnimationInfo();
					characterStyle.m_Animations[num].animationAsset = value;
				}
				characterStyle.m_Animations[num].name = item2.shortName;
				characterStyle.m_Animations[num].type = item2.data.type;
				characterStyle.m_Animations[num].layer = item2.data.layer;
				characterStyle.m_Animations[num].frameCount = item2.data.frameCount;
				characterStyle.m_Animations[num].frameRate = item2.data.frameRate;
				if (item2.targetRenderGroup != -1 && (characterStyle.m_Animations[num].target == null || characterStyle.m_Animations[num].target.geometryAsset.id != prefabs[item2.targetRenderGroup].geometryAsset.id))
				{
					characterStyle.m_Animations[num].target = prefabs[item2.targetRenderGroup];
				}
				if (flag)
				{
					if (item2.data.type != Colossal.Animations.AnimationType.RestPose && (item2.data.layer == Colossal.Animations.AnimationLayer.BodyLayer || item2.data.layer == Colossal.Animations.AnimationLayer.PropLayer))
					{
						int key2 = ((item2.data.layer == Colossal.Animations.AnimationLayer.PropLayer) ? item2.targetRenderGroup : (-1));
						characterStyle.CalculateRootMotion(item2.boneHierarchy, item2.data, dictionary2[key2], num);
					}
					else
					{
						characterStyle.m_Animations[num].rootMotionBone = -1;
						characterStyle.m_Animations[num].rootMotion = null;
					}
				}
				num++;
			}
		}
		return characterStyle;
	}

	private static void CreatePropPrefab((string name, List<(Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash)> animations, int boneCount) style, Dictionary<int, GenderMask> templateToMaskMap, string sourcePath, Dictionary<Colossal.Hash128, AnimationAsset> overrideSafeGuard, List<RenderPrefab> prefabs, IPrefabFactory prefabFactory = null)
	{
		ActivityPropPrefab activityPropPrefab = CreatePrefab<ActivityPropPrefab>(string.Empty, sourcePath, style.name, 0, prefabFactory);
		int num = 0;
		Dictionary<Colossal.Hash128, ActivityPropPrefab.AnimationInfo> dictionary = new Dictionary<Colossal.Hash128, ActivityPropPrefab.AnimationInfo>();
		if (activityPropPrefab.m_Animations != null)
		{
			ActivityPropPrefab.AnimationInfo[] animations = activityPropPrefab.m_Animations;
			foreach (ActivityPropPrefab.AnimationInfo animationInfo in animations)
			{
				dictionary[animationInfo.animationAsset.guid] = animationInfo;
			}
		}
		activityPropPrefab.m_BoneCount = style.boneCount;
		activityPropPrefab.m_Animations = new ActivityPropPrefab.AnimationInfo[style.animations.Count];
		RenderPrefab renderPrefab = prefabs.SingleOrDefault((RenderPrefab p) => p.name == style.name + "_Prop Mesh");
		if (renderPrefab == null)
		{
			return;
		}
		activityPropPrefab.m_Meshes = new ObjectMeshInfo[1];
		activityPropPrefab.m_Meshes[0] = new ObjectMeshInfo
		{
			m_Mesh = renderPrefab,
			m_Rotation = new quaternion(0f, 0f, 0f, 1f)
		};
		Animation item = style.animations.Single(((Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash) a) => a.data.type == Colossal.Animations.AnimationType.RestPose).data;
		foreach (var item2 in style.animations)
		{
			using (s_Progress.ScopedThreadDescription("Processing animation " + item2.data.name))
			{
				Assert.AreEqual(item2.boneHierarchy.Length, style.boneCount);
				bool flag = false;
				if (!overrideSafeGuard.TryGetValue(item2.hash, out var value))
				{
					Colossal.Animations.AnimationClip animationClip = new Colossal.Animations.AnimationClip
					{
						m_BoneHierarchy = item2.boneHierarchy,
						m_Animation = item2.data
					};
					using AnimationAsset animationAsset = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{animationClip.name}_{HashUtils.GetHash(animationClip, sourcePath)}"), animationClip);
					animationAsset.Save();
					value = animationAsset;
					overrideSafeGuard.Add(item2.hash, animationAsset);
					flag = true;
				}
				if (dictionary.TryGetValue(value.id, out var value2))
				{
					activityPropPrefab.m_Animations[num] = value2;
				}
				else
				{
					activityPropPrefab.m_Animations[num] = new ActivityPropPrefab.AnimationInfo();
					activityPropPrefab.m_Animations[num].animationAsset = value;
				}
				activityPropPrefab.m_Animations[num].name = item2.shortName;
				activityPropPrefab.m_Animations[num].type = item2.data.type;
				activityPropPrefab.m_Animations[num].gender = templateToMaskMap.GetValueOrDefault(item2.targetRenderGroup, GenderMask.Any);
				activityPropPrefab.m_Animations[num].frameCount = item2.data.frameCount;
				activityPropPrefab.m_Animations[num].frameRate = item2.data.frameRate;
				if (flag)
				{
					if (item2.data.type != Colossal.Animations.AnimationType.RestPose)
					{
						activityPropPrefab.CalculateRootMotion(item2.boneHierarchy, item2.data, item, num);
					}
					else
					{
						activityPropPrefab.m_Animations[num].rootMotionBone = -1;
						activityPropPrefab.m_Animations[num].rootMotion = null;
					}
				}
				num++;
			}
		}
	}

	private static void ImportTextures(Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, (Report.ImportStep step, Report.Asset asset) report)
	{
		ImportTextures(settings, relativeRootPath, assetGroup, null, report);
	}

	private static void ImportTextures(Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, Func<Colossal.AssetPipeline.TextureAsset, bool> predicate, (Report.ImportStep step, Report.Asset asset) report)
	{
		using (s_ProfImportTextures.Auto())
		{
			Report.ImportStep texturesReport = report.step.AddImportStep("Import Textures");
			try
			{
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = ((!useParallelImport) ? 1 : Environment.ProcessorCount)
				};
				int failures = 0;
				int totalTif = 0;
				int totalPng = 0;
				int total = 0;
				int totalNpot = 0;
				int totalNon8Bpp = 0;
				long totalFileSize = 0L;
				long totalWidth = 0L;
				long totalHeight = 0L;
				using (PerformanceCounter.Start(delegate(TimeSpan t)
				{
					double num = (double)totalFileSize * 1.0 / (double)total;
					double num2 = (double)totalWidth * 1.0 / (double)total;
					double num3 = (double)totalHeight * 1.0 / (double)total;
					if (total == 0)
					{
						log.Info($"No textures processed. All textures in this group were already loaded. {t.TotalSeconds:F3}");
					}
					else
					{
						log.Info($"Completed {total} textures import in {t.TotalSeconds:F3}s. Errors {failures}, png {totalPng}, tif {totalTif}, NPOT {totalNpot}; 16bpp {totalNon8Bpp}. Total size {FormatUtils.FormatBytes(totalFileSize)}, avg size {FormatUtils.FormatBytes((long)num)}, {num2:F0}x{num3:F0}");
					}
				}))
				{
					Parallel.ForEach(assetGroup.FilterBy(predicate), parallelOptions, delegate(Colossal.AssetPipeline.TextureAsset asset, ParallelLoopState state, long index)
					{
						if (asset.instance != null)
						{
							return;
						}
						Report.FileReport report2 = report.asset.AddFile(asset);
						using (s_Progress.ScopedThreadDescription("Importing " + asset.fileName))
						{
							if (s_Progress.shouldCancel)
							{
								state.Stop();
							}
							try
							{
								ISettings importSettings = settings.GetImportSettings(asset.fileName, asset.importer, report2);
								using (texturesReport.AddImportStep("Asset import"))
								{
									asset.instance = asset.importer.Import(importSettings, asset.path, report2);
								}
								asset.instance.sourceAsset = asset;
								Interlocked.Increment(ref total);
								if (asset.instance != null)
								{
									foreach (ITexturePostProcessor texturePostProcessor in PostProcessorCache.GetTexturePostProcessors())
									{
										if (settings.GetPostProcessSettings(asset.fileName, texturePostProcessor, report2, out var settings2))
										{
											Context context = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings2, settings);
											if (texturePostProcessor.ShouldExecute(context, asset))
											{
												using (texturesReport.AddImportStep("Execute " + PPUtils.GetPostProcessorString(texturePostProcessor.GetType()) + " Post Processors"))
												{
													texturePostProcessor.Execute(context, asset, report2);
												}
											}
										}
									}
									Interlocked.Add(ref totalFileSize, asset.instance.fileDataLength);
									Interlocked.Add(ref totalWidth, asset.instance.info.width);
									Interlocked.Add(ref totalHeight, asset.instance.info.height);
									if (!Mathf.IsPowerOfTwo(asset.instance.info.width) || !Mathf.IsPowerOfTwo(asset.instance.info.height))
									{
										Interlocked.Increment(ref totalNpot);
									}
									if (asset.instance.info.bpp != 8)
									{
										Interlocked.Increment(ref totalNon8Bpp);
									}
									if (asset.instance.info.fileFormat == NativeTextures.ImageFileFormat.PNG)
									{
										Interlocked.Increment(ref totalPng);
									}
									if (asset.instance.info.fileFormat == NativeTextures.ImageFileFormat.TIFF)
									{
										Interlocked.Increment(ref totalTif);
									}
								}
							}
							catch (Exception exception)
							{
								Interlocked.Increment(ref failures);
								log.Error(exception, "Error importing " + asset.name + ". Skipped...");
							}
						}
					});
				}
			}
			finally
			{
				if (texturesReport != null)
				{
					((IDisposable)texturesReport).Dispose();
				}
			}
		}
	}

	private static void CreateGeometriesAndSurfaces(Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, out Action<string, ImportMode, Report, HashSet<SurfaceAsset>, IPrefabFactory> postImportOperations, (Report parent, Report.Asset asset) report)
	{
		using (s_ProfCreateGeomsSurfaces.Auto())
		{
			using Report.ImportStep importStep = report.parent.AddImportStep("Create Geometry and Surfaces");
			Dictionary<ModelAsset, List<List<(ModelImporter.Model, Surface)>>> dictionary = new Dictionary<ModelAsset, List<List<(ModelImporter.Model, Surface)>>>();
			foreach (ModelAsset item2 in assetGroup.FilterBy<ModelAsset>())
			{
				int lod = item2.lod;
				if (!dictionary.TryGetValue(item2, out var value))
				{
					value = new List<List<(ModelImporter.Model, Surface)>>();
					dictionary.Add(item2, value);
				}
				while (value.Count <= lod)
				{
					value.Add(new List<(ModelImporter.Model, Surface)>());
				}
				List<(ModelImporter.Model, Surface)> list = value[lod];
				for (int i = 0; i < item2.instance.Count; i++)
				{
					ModelImporter.Model model = item2.instance[i];
					Surface surface = new Surface(model.name);
					Report.AssetData item = report.parent.AddAssetData(surface.name, typeof(Surface));
					foreach (Colossal.AssetPipeline.TextureAsset item3 in assetGroup.FilterBy<Colossal.AssetPipeline.TextureAsset>())
					{
						string bakingTextureProperty = Constants.Material.GetBakingTextureProperty(item3.suffix);
						if (bakingTextureProperty != null && !surface.HasBakingTexture(bakingTextureProperty) && item3.material == item2.material)
						{
							surface.AddBakingTexture(bakingTextureProperty, item3.instance);
						}
						bakingTextureProperty = Constants.Material.GetShaderProperty(item3.suffix, report.asset);
						if (bakingTextureProperty != null && (!surface.HasProperty(bakingTextureProperty) || (item3.assetName == item2.assetName && item3.module == item2.module)) && item3.material == item2.material && item3.lod == item2.lod)
						{
							surface.AddProperty(bakingTextureProperty, item3.instance);
							report.parent.AddAssetData(Path.GetFileNameWithoutExtension(item3.fileName), typeof(TextureImporter.Texture)).AddFile(item3);
						}
					}
					if (i == 0 && item2.name == settings.mainAsset)
					{
						list.Insert(0, (model, surface));
					}
					else
					{
						list.Add((model, surface));
					}
					Report.FileReport fileReport = report.parent.GetFileReport(item2);
					foreach (IModelSurfacePostProcessor modelSurfacePostProcessor in PostProcessorCache.GetModelSurfacePostProcessors())
					{
						string text = item2.name;
						if (item2.instance.Count > 1)
						{
							text += $"#{i}";
						}
						if (!settings.GetPostProcessSettings(text, modelSurfacePostProcessor, fileReport, out var settings2))
						{
							continue;
						}
						Context context = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings2, settings);
						if (modelSurfacePostProcessor.ShouldExecute(context, item2, i, surface))
						{
							using (importStep.AddImportStep("Execute " + PPUtils.GetPostProcessorString(modelSurfacePostProcessor.GetType()) + " Post Processors"))
							{
								modelSurfacePostProcessor.Execute(context, item2, i, surface, (parent: report.parent, asset: report.asset, model: fileReport, surface: item));
							}
						}
					}
				}
			}
			List<List<Colossal.AssetPipeline.LOD>> assets = new List<List<Colossal.AssetPipeline.LOD>>(dictionary.Count);
			if (dictionary.Count > 0)
			{
				foreach (List<List<(ModelImporter.Model, Surface)>> value2 in dictionary.Values)
				{
					List<Colossal.AssetPipeline.LOD> list2 = new List<Colossal.AssetPipeline.LOD>();
					for (int j = 0; j < value2.Count; j++)
					{
						List<(ModelImporter.Model, Surface)> list3 = value2[j];
						if (list3.Count == 0)
						{
							continue;
						}
						Geometry geometry = new Geometry(list3.Select<(ModelImporter.Model, Surface), ModelImporter.Model>(((ModelImporter.Model model, Surface surface) t) => t.model).ToArray());
						Surface[] array = list3.Select<(ModelImporter.Model, Surface), Surface>(((ModelImporter.Model model, Surface surface) t) => t.surface).ToArray();
						if (list2.Count > j)
						{
							list2[j] = new Colossal.AssetPipeline.LOD(geometry, array, j);
							report.asset.AddWarning($"LOD {j} already exist and was replaced by last imported.");
						}
						else
						{
							list2.Add(new Colossal.AssetPipeline.LOD(geometry, array, j));
						}
						Report.AssetData assetData = report.parent.AddAssetData(geometry.name, typeof(Geometry));
						assetData.AddFiles(list3.Select<(ModelImporter.Model, Surface), IAsset>(((ModelImporter.Model model, Surface surface) t) => t.model.sourceAsset));
						Report.AssetData[] array2 = new Report.AssetData[array.Length];
						int num = 0;
						Surface[] array3 = array;
						foreach (Surface surface2 in array3)
						{
							array2[num] = report.parent.AddAssetData(surface2.name, typeof(Surface));
							array2[num++].AddFiles(surface2.textures.Values.Select((TextureImporter.ITexture t) => t.sourceAsset));
						}
						foreach (IGeometryPostProcessor geometryPostProcessor in PostProcessorCache.GetGeometryPostProcessors())
						{
							if (!settings.GetPostProcessSettings(geometry.name, geometryPostProcessor, report.asset, out var settings3))
							{
								continue;
							}
							Context context2 = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings3, settings);
							if (geometryPostProcessor.ShouldExecute(context2, list2))
							{
								using (importStep.AddImportStep("Execute " + PPUtils.GetPostProcessorString(geometryPostProcessor.GetType()) + " Post Processors"))
								{
									geometryPostProcessor.Execute(context2, list2, (parent: report.parent, asset: report.asset, geometry: assetData, surface: array2));
								}
							}
						}
					}
					assets.Add(list2);
				}
			}
			else
			{
				List<Colossal.AssetPipeline.LOD> list4 = new List<Colossal.AssetPipeline.LOD>();
				Dictionary<string, List<Colossal.AssetPipeline.TextureAsset>> dictionary2 = (from t in assetGroup.FilterBy<Colossal.AssetPipeline.TextureAsset>()
					group t by t.material).ToDictionary((IGrouping<string, Colossal.AssetPipeline.TextureAsset> g) => g.Key, (IGrouping<string, Colossal.AssetPipeline.TextureAsset> g) => g.ToList());
				List<Surface> list5 = new List<Surface>();
				foreach (KeyValuePair<string, List<Colossal.AssetPipeline.TextureAsset>> item4 in dictionary2)
				{
					string text2 = assetGroup.name;
					if (!string.IsNullOrEmpty(item4.Key))
					{
						text2 += item4.Key;
					}
					Surface surface3 = new Surface(text2);
					Report.AssetData assetData2 = report.parent.AddAssetData(surface3.name, typeof(Surface));
					foreach (Colossal.AssetPipeline.TextureAsset item5 in item4.Value)
					{
						string bakingTextureProperty2 = Constants.Material.GetBakingTextureProperty(item5.suffix);
						if (bakingTextureProperty2 != null && !surface3.HasBakingTexture(bakingTextureProperty2))
						{
							surface3.AddBakingTexture(bakingTextureProperty2, item5.instance);
						}
						bakingTextureProperty2 = Constants.Material.GetShaderProperty(item5.suffix, report.asset);
						if (bakingTextureProperty2 != null && !surface3.HasProperty(bakingTextureProperty2) && item5.material == item4.Key)
						{
							surface3.AddProperty(bakingTextureProperty2, item5.instance);
							report.parent.AddAssetData(Path.GetFileNameWithoutExtension(item5.fileName), typeof(TextureImporter.Texture)).AddFile(item5);
						}
					}
					foreach (IModelSurfacePostProcessor modelSurfacePostProcessor2 in PostProcessorCache.GetModelSurfacePostProcessors())
					{
						if (!settings.GetPostProcessSettings(text2, modelSurfacePostProcessor2, assetData2, out var settings4))
						{
							continue;
						}
						Context context3 = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings4, settings);
						if (modelSurfacePostProcessor2.ShouldExecute(context3, null, 0, surface3))
						{
							using (importStep.AddImportStep("Execute " + PPUtils.GetPostProcessorString(modelSurfacePostProcessor2.GetType()) + " Post Processors"))
							{
								modelSurfacePostProcessor2.Execute(context3, null, 0, surface3, (parent: report.parent, asset: report.asset, model: null, surface: assetData2));
							}
						}
					}
					list5.Add(surface3);
				}
				list4.Add(new Colossal.AssetPipeline.LOD(null, list5.ToArray(), 0));
				assets.Add(list4);
			}
			postImportOperations = delegate(string sourcePath, ImportMode importMode, Report report2, HashSet<SurfaceAsset> VTMaterials, IPrefabFactory prefabFactory)
			{
				CreateRenderPrefabs(settings, sourcePath, assets, importMode, report2, VTMaterials, prefabFactory);
				DisposeLODs(assets);
			};
		}
	}

	private static Surface BuildAssetSurface(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<IAsset> assetGroup, ModelExportAsset asset, int meshIndex, ref int firstResolution, Report.Asset report)
	{
		if (meshIndex >= asset.skinnedRenderObjects.Count)
		{
			return null;
		}
		SkinnedRenderObject skinnedRenderObject = asset.skinnedRenderObjects[meshIndex];
		string text = AdjustNamingConvention(skinnedRenderObject.meshName);
		if (asset.skinnedRenderObjects.Count > 1)
		{
			text += $"#{meshIndex}";
		}
		Surface surface = new Surface(text);
		MaterialExport materialExport = settings.materials[skinnedRenderObject.materialExportIndex.First()];
		surface.template = Constants.Material.Shader.GetCharacterShader(materialExport.name, materialExport.shader);
		string assetFolder = assetGroup.rootPath + "/" + asset.path + "/";
		foreach (Colossal.AssetPipeline.TextureAsset item in from a in assetGroup.FilterBy<Colossal.AssetPipeline.TextureAsset>()
			where a.path.StartsWith(assetFolder) && a.name.EndsWithAny(Constants.Material.kEmissiveMapIDs.Union(Constants.Material.kEmissiveMaps).ToArray())
			select a)
		{
			string text2 = item.name.Split("_").Last();
			string bakingTextureProperty = Constants.Material.GetBakingTextureProperty("_" + text2);
			if (bakingTextureProperty != null && !surface.HasBakingTexture(bakingTextureProperty))
			{
				surface.AddBakingTexture(bakingTextureProperty, item.instance);
			}
		}
		foreach (TextureExport textureExport in materialExport.textureExports)
		{
			string text3 = Constants.Material.GetDidimoShaderProperty(textureExport.key, report);
			string path = settings.textures[textureExport.textureVariationIndices.First()].path;
			if (string.IsNullOrEmpty(text3))
			{
				report.AddMessage("Key '" + textureExport.key + "' is not mapped to a shader property. Ignoring " + path);
				continue;
			}
			if (textureExport.slot.Equals("ALBEDO"))
			{
				text3 = "_BaseColorMap";
			}
			AddTextureToSurface(assetGroup, skinnedRenderObject.meshName, text3, assetGroup.rootPath + "/" + path, ref firstResolution, ref surface);
		}
		if (!surface.HasProperty("_ControlMask"))
		{
			AddTextureToSurface(assetGroup, skinnedRenderObject.meshName, "_ControlMask", assetGroup.rootPath + "/" + asset.path + "/" + asset.assetName + "_ControlMask.png", ref firstResolution, ref surface);
		}
		return surface;
	}

	private static void ExtendTemplateSurface(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<IAsset> assetGroup, int assetIndex, int meshIndex, int firstResolution, ref Surface surface, Report.Asset report)
	{
		ModelExportAsset asset = settings.templateAssets[assetIndex];
		SkinnedRenderObject renderObject = asset.skinnedRenderObjects[meshIndex];
		MaterialExport materialExport = settings.materials[renderObject.materialExportIndex.First()];
		if (renderObject.meshName.EndsWithAny(Constants.Model.kModelsWithVariation))
		{
			surface.RemoveProperty("_BaseColorMap");
			string text = assetGroup.rootPath + "/" + settings.textures[materialExport.textureExports.Single((TextureExport t) => t.slot == "ALBEDO").textureVariationIndices.First()].path;
			int[] array = settings.shapeAssets.Where((ShapeExportAsset sa) => settings.templateAssets[sa.templateIndex].assetName == asset.assetName).ToArray().Select(delegate(ShapeExportAsset sa)
			{
				List<int> materialExportIndex = sa.skinnedRenderObjects.First((SkinnedRenderObject sro) => sro.meshExportIndex == renderObject.meshExportIndex).materialExportIndex;
				return (!materialExportIndex.Any()) ? (-1) : materialExportIndex.First();
			})
				.ToArray();
			string[] array2 = new string[array.Length + 1];
			array2[0] = text;
			for (int num = 0; num < array.Length; num++)
			{
				int num2 = array[num];
				if (num2 != -1 && settings.materials[num2].textureExports.Exists((TextureExport te) => te.slot == "ALBEDO"))
				{
					array2[num + 1] = assetGroup.rootPath + "/" + settings.textures[settings.materials[num2].textureExports.First((TextureExport te) => te.slot == "ALBEDO").textureVariationIndices.First()].path;
				}
				else
				{
					array2[num + 1] = text;
				}
			}
			AddTextureToSurface(assetGroup, renderObject.meshName, "_BaseMap2", array2, forceArray: false, ref firstResolution, ref surface);
		}
		if (renderObject.meshName.EndsWithAny(Constants.Model.kModelsWithOverlay))
		{
			AddTextureToSurface(assetGroup, renderObject.meshName, "_OverlayAtlas", assetGroup.rootPath + "/" + settings.textures.Single((TextureInfoExport t) => t.path.EndsWith("BaseMapBin0.png")).path, ref firstResolution, ref surface);
			OverlayMaterialConnection[] source = settings.overlayAssets.Where((OverlayExportAsset o) => o.templateIndex == assetIndex).SelectMany((OverlayExportAsset o) => o.overlayMaterialConnections).ToArray();
			int num3 = source.Select((OverlayMaterialConnection mc) => mc.materialIndex).Distinct().Count((int i) => i < renderObject.materialExportIndex.First());
			surface.AddProperty("_OverlayAtlas_IndexOffset", -source.Count() * num3);
		}
	}

	private static Surface BuildCatalogAssetSurface(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<IAsset> assetGroup, ModelAsset model, int meshIndex, Dictionary<int, List<(string assetGuid, string targetGuid, string, int meshIndex, string path, int[])>> cullMasks, Report.Asset report)
	{
		List<ModelExportAsset> typeAssets = settings.templateAssets;
		int assetIndex = typeAssets.FindIndex((ModelExportAsset a) => model.path.EndsWith(a.LODData.First().fbxPath));
		int num = assetIndex;
		if (assetIndex == -1)
		{
			typeAssets = settings.deformableAssets;
			assetIndex = typeAssets.FindIndex((ModelExportAsset a) => model.path.EndsWith(a.path + "/" + a.assetName + ".FBX", StringComparison.InvariantCultureIgnoreCase));
			if (assetIndex != -1)
			{
				num = typeAssets[assetIndex].templateIndex;
			}
		}
		if (assetIndex == -1)
		{
			typeAssets = settings.propAssets;
			assetIndex = typeAssets.FindIndex((ModelExportAsset a) => model.path.EndsWith(a.path + "/" + a.assetName + ".FBX", StringComparison.InvariantCultureIgnoreCase));
		}
		if (assetIndex == -1)
		{
			return null;
		}
		int firstResolution = 0;
		Surface surface = BuildAssetSurface(settings, assetGroup, typeAssets[assetIndex], meshIndex, ref firstResolution, report);
		if (cullMasks.TryGetValue(num, out List<(string, string, string, int, string, int[])> value))
		{
			int num2 = value.FindIndex(((string assetGuid, string targetGuid, string, int meshIndex, string path, int[]) m) => m.assetGuid == typeAssets[assetIndex].assetGuid && m.meshIndex == meshIndex);
			int num3 = value.FindLastIndex(((string assetGuid, string targetGuid, string, int meshIndex, string path, int[]) m) => m.assetGuid == typeAssets[assetIndex].assetGuid && m.meshIndex == meshIndex);
			if (num2 != -1 && num3 != -1)
			{
				AddTextureToSurface(assetGroup, settings.templateAssets[num].assetName, "_AlphaMask", value.Select<(string, string, string, int, string, int[]), string>(((string assetGuid, string targetGuid, string, int meshIndex, string path, int[]) m) => m.path).ToArray(), forceArray: true, ref firstResolution, ref surface);
				surface.AddProperty("_AlphaMask_IndexRange", new Vector4(num2, num3 - num2 + 1));
			}
		}
		if (typeAssets[assetIndex].path.EndsWith("Template"))
		{
			ExtendTemplateSurface(settings, assetGroup, assetIndex, meshIndex, firstResolution, ref surface, report);
		}
		return surface;
	}

	private static void AddTextureToSurface(SourceAssetCollector.AssetGroup<IAsset> assetGroup, string meshName, string property, IReadOnlyList<string> texturePaths, bool forceArray, ref int firstResolution, ref Surface surface)
	{
		if (texturePaths.Count() == 1 && !forceArray)
		{
			TextureImporter.Texture texture = assetGroup.Find((Colossal.AssetPipeline.TextureAsset t) => t.path == texturePaths.First())?.instance;
			if (texture != null)
			{
				firstResolution = texture.width;
				surface.AddProperty(property, texture);
			}
			else if (property == "_MaskMap" && firstResolution != 0)
			{
				int resolution = firstResolution;
				TextureImporter.Texture texture2 = assetGroup.Find((Colossal.AssetPipeline.TextureAsset x) => x.fileName == string.Format("Identity{0}{1}.png", resolution, "_MaskMap"))?.instance;
				if (texture2 != null)
				{
					surface.AddProperty(property, texture2);
				}
			}
			else if (property == "_ControlMask" && firstResolution != 0)
			{
				int resolution2 = firstResolution;
				TextureImporter.Texture texture3 = assetGroup.Find((Colossal.AssetPipeline.TextureAsset x) => x.fileName == string.Format("White{0}{1}.png", resolution2, "_ControlMask"))?.instance;
				if (texture3 != null)
				{
					surface.AddProperty(property, texture3);
				}
			}
		}
		else
		{
			if (!(texturePaths.Count() > 1 || forceArray))
			{
				return;
			}
			TextureImporter.TextureArray textureArray = new TextureImporter.TextureArray(meshName + property);
			for (int num = 0; num < texturePaths.Count; num++)
			{
				int i1 = num;
				TextureImporter.Texture instance = assetGroup.Find((Colossal.AssetPipeline.TextureAsset t) => t.path == texturePaths[i1]).instance;
				if (!textureArray.AddSlice(instance))
				{
					log.WarnFormat("Texture {0} does not match the texture array resolution {1}x{2}. Skipped!", texturePaths[i1], textureArray.width, textureArray.height);
				}
			}
			surface.AddProperty(property, textureArray);
		}
	}

	private static void AddTextureToSurface(SourceAssetCollector.AssetGroup<IAsset> assetGroup, string meshName, string property, string texturePath, ref int firstResolution, ref Surface surface)
	{
		AddTextureToSurface(assetGroup, meshName, property, new List<string> { texturePath }, forceArray: false, ref firstResolution, ref surface);
	}

	private static List<(string, string, string, int, string, int[])> GetAssetCullInformations(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<IAsset> assetGroup, ModelExportAsset asset)
	{
		List<(string, string, string, int, string, int[])> list = new List<(string, string, string, int, string, int[])>();
		for (int i = 0; i < asset.skinnedRenderObjects.Count; i++)
		{
			foreach (CullingInformation cullingInformation in asset.skinnedRenderObjects[i].cullingInformation)
			{
				string texturePath = assetGroup.rootPath + "/" + settings.textures[cullingInformation.textureIndex].path;
				ModelExportAsset modelExportAsset = settings.deformableAssets.Single((ModelExportAsset d) => d.assetGuid == cullingInformation.assetGuid);
				if (!assetGroup.All((IAsset a) => a.path != texturePath))
				{
					list.Add((asset.assetGuid, cullingInformation.assetGuid, asset.skinnedRenderObjects[i].meshName + "_" + modelExportAsset.assetName, i, assetGroup.rootPath + "/" + settings.textures[cullingInformation.textureIndex].path, cullingInformation.vertices.ToArray()));
				}
			}
		}
		return list;
	}

	private static Dictionary<int, List<(string, string, string, int, string, int[])>> GetCharacterCullInformation(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.AssetGroup<IAsset> assetGroup)
	{
		Dictionary<int, List<(string, string, string, int, string, int[])>> dictionary = new Dictionary<int, List<(string, string, string, int, string, int[])>>();
		List<(string, string, string, int, string, int[])> value;
		for (int i = 0; i < settings.templateAssets.Count; i++)
		{
			ModelExportAsset asset = settings.templateAssets[i];
			List<(string, string, string, int, string, int[])> assetCullInformations = GetAssetCullInformations(settings, assetGroup, asset);
			if (assetCullInformations.Any())
			{
				if (!dictionary.TryGetValue(i, out value))
				{
					dictionary.Add(i, new List<(string, string, string, int, string, int[])>());
				}
				dictionary[i].AddRange(assetCullInformations);
			}
		}
		for (int j = 0; j < settings.deformableAssets.Count; j++)
		{
			ModelExportAsset asset2 = settings.deformableAssets[j];
			List<(string, string, string, int, string, int[])> assetCullInformations2 = GetAssetCullInformations(settings, assetGroup, asset2);
			if (assetCullInformations2.Any())
			{
				if (!dictionary.TryGetValue(asset2.templateIndex, out value))
				{
					dictionary.Add(asset2.templateIndex, new List<(string, string, string, int, string, int[])>());
				}
				dictionary[asset2.templateIndex].AddRange(assetCullInformations2);
			}
		}
		return dictionary;
	}

	private static bool TryDisposeTexture(TextureImporter.Texture texture, List<TextureImporter.ITexture> surfaceTextures)
	{
		foreach (TextureImporter.ITexture surfaceTexture in surfaceTextures)
		{
			if (surfaceTexture is TextureImporter.Texture texture2 && texture2.path == texture.path)
			{
				return false;
			}
			if (surfaceTexture is TextureImporter.TextureArray textureArray && textureArray.paths.Any((string p) => p == texture.path))
			{
				return false;
			}
		}
		texture.Dispose();
		return true;
	}

	private static void CreateGeometriesAndSurfacesForCatalogAssets(Colossal.AssetPipeline.Settings settings, string relativeRootPath, SourceAssetCollector.AssetGroup<IAsset> assetGroup, out Action<string, ImportMode, Report, HashSet<SurfaceAsset>, IPrefabFactory> postImportOperations, (Report parent, Report.Asset asset) report)
	{
		using (s_ProfCreateGeomsSurfaces.Auto())
		{
			Report.ImportStep geometryReport = report.parent.AddImportStep("Create Geometry and Surfaces");
			try
			{
				List<(string name, IReadOnlyList<(int templateIndex, string styleName, CharacterGroup.Meta meta, IReadOnlyList<int> meshes)> characters)> groups = new List<(string, IReadOnlyList<(int, string, CharacterGroup.Meta, IReadOnlyList<int>)>)>();
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = ((!useParallelImport) ? 1 : Environment.ProcessorCount)
				};
				Dictionary<int, List<(string assetGuid, string targetGuid, string name, int meshIndex, string texturePath, int[] vertices)>> characterCulls = GetCharacterCullInformation(settings, assetGroup);
				Dictionary<string, int> first = new Dictionary<string, int>();
				foreach (KeyValuePair<int, List<(string, string, string, int, string, int[])>> kvp in characterCulls)
				{
					first = first.Concat(kvp.Value.Select<(string, string, string, int, string, int[]), string>(((string assetGuid, string targetGuid, string name, int meshIndex, string texturePath, int[] vertices) v) => v.assetGuid).Distinct().ToDictionary((string v) => v, (string v) => kvp.Key)).ToDictionary((KeyValuePair<string, int> vp) => vp.Key, (KeyValuePair<string, int> vp) => vp.Value);
				}
				ModelAsset[] source = assetGroup.FilterBy<ModelAsset>().ToArray();
				ConcurrentDictionary<string, (List<Colossal.AssetPipeline.LOD> lods, int templateIndex, int deformableIndex, int propIndex)> allConcurrentModels = new ConcurrentDictionary<string, (List<Colossal.AssetPipeline.LOD>, int, int, int)>();
				ConcurrentBag<TextureImporter.ITexture> concurrentUsedTextureAssets = new ConcurrentBag<TextureImporter.ITexture>();
				Parallel.ForEach(source, parallelOptions, delegate(ModelAsset modelFile)
				{
					try
					{
						int num7 = settings.templateAssets.FindIndex((ModelExportAsset t) => modelFile.path.Contains("/" + t.path));
						int num8 = settings.deformableAssets.FindIndex((ModelExportAsset d) => modelFile.path.Contains(d.path ?? ""));
						int num9 = settings.propAssets.FindIndex((ModelExportAsset d) => modelFile.path.Contains(d.path ?? ""));
						ModelImporter.Model[] models = modelFile.instance.models;
						ModelImporter.Model[] array4 = new ModelImporter.Model[models.Length];
						Surface[] array5 = new Surface[models.Length];
						int num10 = 0;
						for (int num11 = 0; num11 < modelFile.instance.models.Length; num11++)
						{
							ModelImporter.Model model = modelFile.instance.models[num11];
							Surface surface = BuildCatalogAssetSurface(settings, assetGroup, modelFile, num11, characterCulls, report.asset);
							concurrentUsedTextureAssets.AddRange(surface.textures.Values);
							concurrentUsedTextureAssets.AddRange(surface.bakingTextures.Values);
							Report.AssetData item7 = report.parent.AddAssetData(surface.name, typeof(Surface));
							Report.FileReport fileReport = report.parent.GetFileReport(modelFile);
							foreach (IModelSurfacePostProcessor modelSurfacePostProcessor in PostProcessorCache.GetModelSurfacePostProcessors())
							{
								string name = AdjustNamingConvention(model.name);
								if (settings.GetPostProcessSettings(name, modelSurfacePostProcessor, fileReport, out var settings2))
								{
									Context context = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings2, settings);
									if (modelSurfacePostProcessor.ShouldExecute(context, modelFile, num11, surface))
									{
										using (geometryReport.AddImportStep("Execute " + PPUtils.GetPostProcessorString(modelSurfacePostProcessor.GetType()) + " Post Processors"))
										{
											modelSurfacePostProcessor.Execute(context, modelFile, num11, surface, (parent: report.parent, asset: report.asset, model: fileReport, surface: item7));
										}
									}
								}
							}
							array5[num10] = surface;
							array4[num10] = model;
							num10++;
						}
						List<Colossal.AssetPipeline.LOD> list4 = new List<Colossal.AssetPipeline.LOD>();
						Geometry geometry = new Geometry(array4.ToArray());
						list4.Add(new Colossal.AssetPipeline.LOD(geometry, array5, 0));
						Report.AssetData assetData = report.parent.AddAssetData(geometry.name, typeof(Geometry));
						assetData.AddFiles(array4.Select((ModelImporter.Model t) => t.sourceAsset));
						Report.AssetData[] array6 = new Report.AssetData[array5.Length];
						int num12 = 0;
						Surface[] array7 = array5;
						foreach (Surface surface2 in array7)
						{
							array6[num12] = report.parent.AddAssetData(surface2.name, typeof(Surface));
							array6[num12++].AddFiles(surface2.textures.Values.Select((TextureImporter.ITexture t) => t.sourceAsset));
						}
						foreach (IGeometryPostProcessor geometryPostProcessor in PostProcessorCache.GetGeometryPostProcessors())
						{
							try
							{
								if (settings.GetPostProcessSettings(geometry.name, geometryPostProcessor, report.asset, out var settings3))
								{
									Context context2 = new Context(s_MainThreadDispatcher, relativeRootPath, OnDebugTexture, settings3, settings);
									if (geometryPostProcessor.ShouldExecute(context2, list4))
									{
										using (geometryReport.AddImportStep("Execute " + PPUtils.GetPostProcessorString(geometryPostProcessor.GetType()) + " Post Processors"))
										{
											geometryPostProcessor.Execute(context2, list4, (parent: report.parent, asset: report.asset, geometry: assetData, surface: array6));
										}
									}
								}
							}
							catch (Exception exception)
							{
								log.Error(exception, "Exception occured with " + geometry.name);
								throw;
							}
						}
						string text = modelFile.name;
						if (num7 != -1)
						{
							text = ((modelFile.instance.Count > 1) ? settings.templateAssets[num7].skinnedRenderObjects.First().meshName : settings.templateAssets[num7].assetName);
						}
						else if (num8 != -1)
						{
							text = ((modelFile.instance.Count > 1) ? settings.deformableAssets[num8].skinnedRenderObjects.First().meshName : settings.deformableAssets[num8].assetName);
						}
						else if (num9 != -1)
						{
							text += "_Prop";
						}
						allConcurrentModels.TryAdd(text, (list4, num7, num8, num9));
					}
					catch (Exception exception2)
					{
						log.Error(exception2, "Exception occured with " + modelFile.name);
					}
				});
				List<TextureImporter.ITexture> surfaceTextures = concurrentUsedTextureAssets.ToList();
				TextureImporter.Texture[] array = (from a in assetGroup.FilterBy<Colossal.AssetPipeline.TextureAsset>()
					select a.instance).ToArray();
				for (int num = 0; num < array.Length; num++)
				{
					TryDisposeTexture(array[num], surfaceTextures);
				}
				Dictionary<string, (List<Colossal.AssetPipeline.LOD> lods, int templateIndex, int deformableIndex, int propIndex)> allModels = allConcurrentModels.OrderBy((KeyValuePair<string, (List<Colossal.AssetPipeline.LOD> lods, int templateIndex, int deformableIndex, int propIndex)> m) => m.Key).ToDictionary((KeyValuePair<string, (List<Colossal.AssetPipeline.LOD> lods, int templateIndex, int deformableIndex, int propIndex)> m) => m.Key, (KeyValuePair<string, (List<Colossal.AssetPipeline.LOD> lods, int templateIndex, int deformableIndex, int propIndex)> m) => m.Value);
				List<(string, IReadOnlyList<(Animation, int[], string, int, Colossal.Hash128)>, int, int)> characterStyles = new List<(string, IReadOnlyList<(Animation, int[], string, int, Colossal.Hash128)>, int, int)>();
				List<(string name, List<(Animation, int[], string, int, Colossal.Hash128)> animations, int boneCount)> propStyles = new List<(string, List<(Animation, int[], string, int, Colossal.Hash128)>, int)>();
				HashSet<string> hashSet = new HashSet<string>();
				int templateIndex;
				for (templateIndex = 0; templateIndex < settings.templateAssets.Count; templateIndex++)
				{
					int num2 = settings.shapeAssets.Count((ShapeExportAsset s) => s.templateIndex == templateIndex);
					int item = settings.animationExportData.templatesBones[templateIndex].boneNames.Length;
					var (list, dictionary) = ImportAnimations(ref settings, relativeRootPath, assetGroup, allModels.Keys.ToArray(), templateIndex, (step: geometryReport, asset: report.asset));
					characterStyles.Add((settings.templateAssets[templateIndex].assetName, list?.Where<(Animation, int[], string, int, Colossal.Hash128)>(((Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash) a) => a.data.layer != Colossal.Animations.AnimationLayer.PropLayer).ToArray(), item, num2 + 1));
					if (dictionary == null)
					{
						continue;
					}
					foreach (KeyValuePair<string, int[]> styleProp in dictionary)
					{
						IEnumerable<(Animation, int[], string, int, Colossal.Hash128)> enumerable = list?.Where<(Animation, int[], string, int, Colossal.Hash128)>(((Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash) a) => a.data.layer == Colossal.Animations.AnimationLayer.PropLayer && a.data.name.EndsWith("#" + styleProp.Key));
						if (!hashSet.Contains(styleProp.Key))
						{
							propStyles.Add((styleProp.Key, new List<(Animation, int[], string, int, Colossal.Hash128)>(), styleProp.Value.Length));
							hashSet.Add(styleProp.Key);
						}
						else
						{
							enumerable = enumerable?.Where<(Animation, int[], string, int, Colossal.Hash128)>(((Animation data, int[] boneHierarchy, string shortName, int targetRenderGroup, Colossal.Hash128 hash) a) => a.data.type != Colossal.Animations.AnimationType.RestPose);
						}
						if (list != null)
						{
							int index = propStyles.FindIndex(((string name, List<(Animation, int[], string, int, Colossal.Hash128)> animations, int boneCount) s) => s.name == styleProp.Key);
							propStyles[index].animations.AddRange(enumerable);
						}
					}
				}
				List<(string name, string suffix, Rect sourceRegion, Rect targetRegion, int index, int materialIndex, int templateIndex)> characterOverlays = new List<(string, string, Rect, Rect, int, int, int)>();
				Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
				foreach (OverlayExportAsset overlay in settings.overlayAssets)
				{
					if (settings.groups.All((GroupExportInfo g) => g.characters.Count((CharacterExportInfo c) => c.blendInstance.templateAssetIndex == overlay.templateIndex) == 0))
					{
						continue;
					}
					foreach (OverlayMaterialConnection overlayMaterialConnection in overlay.overlayMaterialConnections)
					{
						int value;
						int num3 = (dictionary2.TryGetValue(overlay.templateIndex, out value) ? value : 0);
						int overlayOffset = settings.GetOverlayOffset(overlay.templateIndex, overlayMaterialConnection.materialIndex);
						string item2 = string.Empty;
						if (overlay.overlayMaterialConnections.Count > 1)
						{
							item2 = settings.materials[overlayMaterialConnection.materialIndex].name.Split("_")[1];
						}
						Rect item3 = new Rect(overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().sourceRect.xMin, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().sourceRect.yMin, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().sourceRect.width, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().sourceRect.height);
						Rect item4 = new Rect(overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().targetRect.xMin, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().targetRect.yMin, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().targetRect.width, overlayMaterialConnection.overlayAtlasInformations.First().variantRegions.First().targetRect.height);
						value = (dictionary2[overlay.templateIndex] = value + 1);
						characterOverlays.Add((overlay.assetName, item2, item3, item4, overlayOffset + num3, overlayMaterialConnection.materialIndex, overlay.templateIndex));
					}
				}
				foreach (GroupExportInfo group in settings.groups)
				{
					List<(int, string, CharacterGroup.Meta, IReadOnlyList<int>)> list2 = new List<(int, string, CharacterGroup.Meta, IReadOnlyList<int>)>();
					foreach (CharacterExportInfo character in group.characters)
					{
						List<int> list3 = new List<int>();
						ModelExportAsset modelExportAsset = settings.templateAssets[character.blendInstance.templateAssetIndex];
						int item5 = allModels.Keys.IndexOf(modelExportAsset.skinnedRenderObjects.First().meshName);
						list3.Add(item5);
						foreach (DeformableVariationExportAsset deformableAssetsIndex in character.blendInstance.deformableAssetsIndices)
						{
							item5 = allModels.Keys.IndexOf(settings.deformableAssets[deformableAssetsIndex.deformableAssetIndex].assetName);
							list3.Add(item5);
						}
						IndexWeight[] indexWeights = Enumerable.Repeat(default(IndexWeight), 8).ToArray();
						if (characterCulls.TryGetValue(character.blendInstance.templateAssetIndex, out List<(string, string, string, int, string, int[])> value2))
						{
							int insertionIndex = 0;
							List<(string, string)> assetGuids = value2.Select<(string, string, string, int, string, int[]), (string, string)>(((string assetGuid, string targetGuid, string name, int meshIndex, string texturePath, int[] vertices) m) => (assetGuid: m.assetGuid, targetGuid: m.targetGuid)).ToList();
							settings.GetRenderObjectsCullWeights(modelExportAsset.assetGuid, character.blendInstance.deformableAssetsIndices, assetGuids, ref insertionIndex, ref indexWeights);
							foreach (DeformableVariationExportAsset deformableAssetsIndex2 in character.blendInstance.deformableAssetsIndices)
							{
								settings.GetRenderObjectsCullWeights(settings.deformableAssets[deformableAssetsIndex2.deformableAssetIndex].assetGuid, character.blendInstance.deformableAssetsIndices, assetGuids, ref insertionIndex, ref indexWeights);
							}
						}
						IndexWeight[] array2 = Enumerable.Repeat(default(IndexWeight), 8).ToArray();
						int num5 = 0;
						foreach (OverlayVariationExportAsset overlayAssetsIndex in character.overlayAssetsIndices)
						{
							(string, string, Rect, Rect, int, int, int)[] array3 = characterOverlays.Where(((string name, string suffix, Rect sourceRegion, Rect targetRegion, int index, int materialIndex, int templateIndex) o) => o.templateIndex == character.blendInstance.templateAssetIndex && o.name == overlayAssetsIndex.name).ToArray();
							for (int num6 = 0; num6 < array3.Length; num6++)
							{
								array2[num5] = new IndexWeight
								{
									index = array3[num6].Item5,
									weight = 1f
								};
								num5++;
							}
						}
						CharacterGroup.Meta item6 = new CharacterGroup.Meta
						{
							shapeWeights = CastStruct<IndexWeight8, CharacterGroup.IndexWeight8>(settings.GetShapeIndices(character.blendInstance.templateAssetIndex, character.blendInstance.shapeWeights)),
							textureWeights = CastStruct<IndexWeight8, CharacterGroup.IndexWeight8>(settings.GetShapeIndices(character.blendInstance.templateAssetIndex, character.blendInstance.textureWeights)),
							overlayWeights = CastStruct<IndexWeight8, CharacterGroup.IndexWeight8>(new IndexWeight8(array2)),
							maskWeights = CastStruct<IndexWeight8, CharacterGroup.IndexWeight8>(new IndexWeight8(indexWeights))
						};
						list2.Add((character.blendInstance.templateAssetIndex, modelExportAsset.assetName, item6, list3));
					}
					groups.Add((group.characters[0].group, list2));
				}
				postImportOperations = delegate(string sourcePath, ImportMode importMode, Report report2, HashSet<SurfaceAsset> VTMaterials, IPrefabFactory prefabFactory)
				{
					AssetDatabase.global.UnloadAllAssets();
					List<RenderPrefab> list4 = new List<RenderPrefab>();
					foreach (KeyValuePair<string, (List<Colossal.AssetPipeline.LOD>, int, int, int)> model in allModels)
					{
						(RenderPrefab, Report.Prefab) tuple2 = CreateRenderPrefab(settings, sourcePath, model.Key, model.Value.Item1, importMode, report2, VTMaterials, prefabFactory)[0];
						CharacterProperties characterProperties = tuple2.Item1.AddOrGetComponent<CharacterProperties>();
						if (model.Value.Item2 != -1)
						{
							List<(string, string, Rect, Rect, int, int, int)> list5 = characterOverlays.Where(((string name, string suffix, Rect sourceRegion, Rect targetRegion, int index, int materialIndex, int templateIndex) a) => a.templateIndex == model.Value.Item2).ToList();
							characterProperties.m_Overlays = new CharacterOverlay[list5.Count];
							for (int num7 = 0; num7 < list5.Count; num7++)
							{
								(string, string, Rect, Rect, int, int, int) tuple3 = list5[num7];
								CharacterOverlay characterOverlay = CreatePrefab<CharacterOverlay>("Overlay", sourcePath, tuple3.Item1 + (string.IsNullOrEmpty(tuple3.Item2) ? string.Empty : ("_" + tuple3.Item2)), 0, prefabFactory);
								characterOverlay.m_Index = tuple3.Item5;
								characterOverlay.m_sourceRegion = tuple3.Item3;
								characterOverlay.m_targetRegion = tuple3.Item4;
								characterProperties.m_Overlays[num7] = characterOverlay;
							}
							if (list5.Any())
							{
								RenderPrefab[] lodMeshes = tuple2.Item1.GetComponent<LodProperties>().m_LodMeshes;
								for (int num8 = 0; num8 < lodMeshes.Length; num8++)
								{
									lodMeshes[num8].AddOrGetComponent<CharacterProperties>().m_Overlays = characterProperties.m_Overlays;
								}
							}
						}
						else if (model.Value.Item3 != -1)
						{
							characterProperties.m_Overlays = Array.Empty<CharacterOverlay>();
						}
						else if (model.Value.Item4 != -1 && settings.animationExportData.propBones.Any((AnimationExportBoneData p) => p.targetIndex == model.Value.Item4))
						{
							characterProperties.m_AnimatedPropName = settings.propAssets[model.Value.Item4].assetName;
						}
						list4.Add(tuple2.Item1);
					}
					foreach (KeyValuePair<string, (List<Colossal.AssetPipeline.LOD>, int, int, int)> item8 in allModels)
					{
						DisposeLODs(item8.Value.Item1);
					}
					Dictionary<Colossal.Hash128, AnimationAsset> overrideSafeGuard = new Dictionary<Colossal.Hash128, AnimationAsset>();
					Dictionary<int, GenderMask> dictionary3 = new Dictionary<int, GenderMask>();
					foreach (var item9 in groups)
					{
						CharacterGroup characterGroup = CreatePrefab<CharacterGroup>("Group", sourcePath, item9.name, 0, prefabFactory);
						characterGroup.m_Characters = new CharacterGroup.Character[item9.characters.Count];
						for (int num9 = 0; num9 < item9.characters.Count; num9++)
						{
							(int templateIndex, string styleName, CharacterGroup.Meta meta, IReadOnlyList<int> meshes) characterRenderGroups = item9.characters[num9];
							CharacterGroup.Character character2 = new CharacterGroup.Character();
							int count = characterRenderGroups.meshes.Count;
							character2.m_MeshPrefabs = new RenderPrefab[count];
							character2.m_Style = CreateStylePrefab(characterStyles.Single(((string, IReadOnlyList<(Animation, int[], string, int, Colossal.Hash128)>, int, int) s) => s.Item1 == characterRenderGroups.styleName), sourcePath, overrideSafeGuard, list4, prefabFactory);
							character2.m_Meta = characterRenderGroups.meta;
							for (int num10 = 0; num10 < count; num10++)
							{
								character2.m_MeshPrefabs[num10] = list4[characterRenderGroups.meshes[num10]];
							}
							characterGroup.m_Characters[num9] = character2;
							dictionary3.TryAdd(item9.characters[num9].templateIndex, character2.m_Style.m_Gender);
						}
					}
					foreach (var propStyle in propStyles)
					{
						if (settings.animations == null || !settings.animations.Any((KeyValuePair<string, Colossal.AssetPipeline.Settings.AnimationDefinition> a) => Regex.IsMatch(a.Key, ".*#" + propStyle.name)))
						{
							CreatePropPrefab(propStyle, dictionary3, sourcePath, overrideSafeGuard, list4, prefabFactory);
						}
					}
				};
			}
			finally
			{
				if (geometryReport != null)
				{
					((IDisposable)geometryReport).Dispose();
				}
			}
		}
	}

	private static bool ImportAssetGroup(string projectRootPath, string relativeRootPath, SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset> assetGroup, out int assetCount, out Action<string, ImportMode, Report, HashSet<SurfaceAsset>, IPrefabFactory> postImportOperations, Report report, Report.Asset assetReport)
	{
		postImportOperations = null;
		using (s_ProfImportAssetGroup.Auto())
		{
			try
			{
				log.Info("Start processing " + assetGroup);
				using (Report.ImportStep item = report.AddImportStep("Import asset group"))
				{
					Colossal.AssetPipeline.Settings settings = ImportSettings(projectRootPath, assetGroup, (step: item, asset: assetReport));
					SourceAssetCollector.AssetGroup<IAsset> assetGroup2 = CreateAssetGroupFromSettings(projectRootPath, ref settings, assetGroup, assetReport);
					assetCount = assetGroup2.count;
					Interlocked.Add(ref parallelCount, assetCount - 1);
					s_Progress.Set($"Importing {parallelCount} assets (group {total + 1}/{importsCount})", "Importing textures and meshes for " + assetGroup.name, (float)total / (float)importsCount);
					foreach (IAsset item2 in assetGroup2)
					{
						log.Verbose($"   {item2}");
					}
					ImportTextures(settings, relativeRootPath, assetGroup2, (step: item, asset: assetReport));
					ImportModels(settings, relativeRootPath, assetGroup2, (step: item, asset: assetReport));
					if (!settings.IsAssetCatalogImport())
					{
						CreateGeometriesAndSurfaces(settings, relativeRootPath, assetGroup2, out postImportOperations, (parent: report, asset: assetReport));
					}
					else
					{
						CreateGeometriesAndSurfacesForCatalogAssets(settings, relativeRootPath, assetGroup2, out postImportOperations, (parent: report, asset: assetReport));
					}
				}
				return true;
			}
			catch (Exception exception)
			{
				log.ErrorFormat(exception, "Error processing {0}.. Skipped!", assetGroup.ToString());
				assetCount = 1;
				return false;
			}
		}
	}

	private static bool IsLODsValid(IReadOnlyList<List<Colossal.AssetPipeline.LOD>> assets)
	{
		if (assets == null || assets.Count == 0)
		{
			return false;
		}
		foreach (List<Colossal.AssetPipeline.LOD> asset in assets)
		{
			foreach (Colossal.AssetPipeline.LOD item in asset)
			{
				if (item.geometry == null && (item.surfaces == null || item.surfaces.Length == 0))
				{
					return false;
				}
				if (item.geometry != null && !item.geometry.isValid)
				{
					return false;
				}
				if (item.surfaces != null && (item.surfaces.Length == 0 || item.surfaces.Any((Surface surface) => !surface.isValid)))
				{
					return false;
				}
			}
		}
		return true;
	}

	private static void DisposeLODs(IReadOnlyList<IReadOnlyList<Colossal.AssetPipeline.LOD>> assets)
	{
		foreach (IReadOnlyList<Colossal.AssetPipeline.LOD> asset in assets)
		{
			DisposeLODs(asset);
		}
	}

	private static void DisposeLODs(IReadOnlyList<Colossal.AssetPipeline.LOD> assets)
	{
		foreach (Colossal.AssetPipeline.LOD asset in assets)
		{
			asset.Dispose();
		}
	}

	private static IReadOnlyList<(RenderPrefab prefab, Report.Prefab report)> CreateRenderPrefab(Colossal.AssetPipeline.Settings settings, string sourcePath, IReadOnlyList<Colossal.AssetPipeline.LOD> asset, ImportMode importMode, Report report, HashSet<SurfaceAsset> VTMaterials, IPrefabFactory prefabFactory = null)
	{
		return CreateRenderPrefab(settings, sourcePath, string.Empty, asset, importMode, report, VTMaterials, prefabFactory);
	}

	private static IReadOnlyList<(RenderPrefab prefab, Report.Prefab report)> CreateRenderPrefab(Colossal.AssetPipeline.Settings settings, string sourcePath, string assetName, IReadOnlyList<Colossal.AssetPipeline.LOD> asset, ImportMode importMode, Report report, HashSet<SurfaceAsset> VTMaterials, IPrefabFactory prefabFactory = null)
	{
		List<(RenderPrefab, Report.Prefab)> list = new List<(RenderPrefab, Report.Prefab)>(asset.Count);
		try
		{
			foreach (Colossal.AssetPipeline.LOD item in asset)
			{
				string text = (string.IsNullOrEmpty(assetName) ? item.name : (assetName + ((item.level > 0) ? $"_LOD{item.level}" : string.Empty)));
				RenderPrefab renderPrefab = CreateRenderPrefab(sourcePath, text, item.level, prefabFactory);
				Report.Prefab prefab = report.AddPrefab(renderPrefab.name);
				list.Add((renderPrefab, prefab));
				if (importMode.Has(ImportMode.Geometry))
				{
					Geometry geometry = item.geometry;
					if (geometry != null)
					{
						using (GeometryAsset geometryAsset = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{text}_{HashUtils.GetHash(item, sourcePath)}"), geometry))
						{
							geometryAsset.Save();
							report.AddInfoToAsset(text, typeof(Geometry), geometryAsset);
							renderPrefab.geometryAsset = geometryAsset;
							renderPrefab.bounds = geometry.CalcBounds();
							renderPrefab.surfaceArea = geometry.CalcSurfaceArea();
							renderPrefab.indexCount = geometry.CalcTotalIndices();
							renderPrefab.vertexCount = geometry.CalcTotalVertices();
							renderPrefab.meshCount = geometry.models.Length;
						}
						ModelImporter.Model model = geometry.FirstOrDefault();
						if (model.animationClip != null)
						{
							settings.useProceduralAnimation = true;
							settings.createProceduralAnimationForPrefab.Add(item.name);
							if (settings.animations == null)
							{
								Colossal.Hash128 hash = HashUtils.GetHash(model.animationClip, sourcePath);
								using (AnimationAsset animationAsset = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{text}_{hash}"), model.animationClip))
								{
									animationAsset.Save();
									report.AddInfoToAsset(text, typeof(Colossal.Animations.AnimationClip), animationAsset);
								}
								settings.animations = new Dictionary<string, Colossal.AssetPipeline.Settings.AnimationDefinition> { 
								{
									$"{hash}",
									default(Colossal.AssetPipeline.Settings.AnimationDefinition)
								} };
							}
							else
							{
								foreach (KeyValuePair<string, Colossal.AssetPipeline.Settings.AnimationDefinition> animation2 in settings.animations)
								{
									string key = animation2.Key;
									string text2 = text + "_" + key;
									Colossal.Animations.AnimationClip animation = SliceAnimation(text2, model.animationClip, animation2.Value);
									using AnimationAsset animationAsset2 = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{text2}_{HashUtils.GetHash(animation, sourcePath)}"), animation);
									animationAsset2.Save();
									report.AddInfoToAsset(key, typeof(Colossal.Animations.AnimationClip), animationAsset2);
								}
							}
						}
					}
					else if (item.surfaces != null)
					{
						renderPrefab.meshCount = item.surfaces.Length;
					}
				}
				if (importMode.Has(ImportMode.Textures))
				{
					Surface[] surfaces = item.surfaces;
					if (surfaces != null)
					{
						SurfaceAsset[] array = new SurfaceAsset[surfaces.Length];
						for (int i = 0; i < surfaces.Length; i++)
						{
							Surface surface = surfaces[i];
							try
							{
								targetDatabase.onAssetDatabaseChanged.Subscribe<Colossal.IO.AssetDatabase.TextureAsset>(OnTextureAdded);
								using SurfaceAsset surfaceAsset = targetDatabase.AddAsset(AssetDataPath.Create("StreamingData~", $"{surface.name}_{HashUtils.GetHash(surface, sourcePath)}"), surface);
								surfaceAsset.Save();
								report.AddInfoToAsset(surface.name, typeof(Surface), surfaceAsset);
								VTMaterials.Add(surfaceAsset);
								array[i] = surfaceAsset;
							}
							finally
							{
								targetDatabase.onAssetDatabaseChanged.Unsubscribe(OnTextureAdded);
							}
							if (surface.isImpostor)
							{
								renderPrefab.isImpostor = true;
							}
						}
						renderPrefab.surfaceAssets = array;
					}
				}
				SetupComponents(settings, renderPrefab, item, prefab);
			}
			SetupLODs(settings, list);
		}
		catch (Exception exception)
		{
			log.Error(exception, "Error occured with " + asset[0].name);
		}
		return list;
		void OnTextureAdded(AssetChangedEventArgs args)
		{
			Colossal.IO.AssetDatabase.TextureAsset textureAsset = (Colossal.IO.AssetDatabase.TextureAsset)args.asset;
			report.AddInfoToAsset(GetNameWithoutGUID(textureAsset.name), typeof(TextureImporter.Texture), textureAsset);
		}
	}

	private static Colossal.Animations.AnimationClip SliceAnimation(string clipName, Colossal.Animations.AnimationClip source, Colossal.AssetPipeline.Settings.AnimationDefinition definition)
	{
		Animation animation = new Animation
		{
			name = clipName,
			type = source.m_Animation.type,
			layer = source.m_Animation.layer,
			shapeIndices = source.m_Animation.shapeIndices,
			frameCount = definition.endFrame - definition.startFrame,
			frameRate = source.m_Animation.frameRate
		};
		Dictionary<int, List<Animation.ElementRaw>> dictionary = new Dictionary<int, List<Animation.ElementRaw>>();
		for (int i = 0; i < source.m_Animation.boneIndices.Length; i++)
		{
			bool flag = false;
			List<Animation.ElementRaw> list = new List<Animation.ElementRaw>();
			for (int j = definition.startFrame; j < definition.endFrame; j++)
			{
				Animation.ElementRaw boneSample = source.m_Animation.GetBoneSample(i, 0, j);
				if (list.Count > 0)
				{
					flag |= !boneSample.position.Equals(list.Last().position) || !boneSample.rotation.Equals(list.Last().rotation);
				}
				list.Add(boneSample);
			}
			if (flag)
			{
				dictionary.Add(i, list);
			}
		}
		animation.boneIndices = dictionary.Keys.ToArray();
		Animation.ElementRaw[] array = new Animation.ElementRaw[animation.boneIndices.Length * (definition.endFrame - definition.startFrame)];
		for (int k = 0; k < definition.endFrame - definition.startFrame; k++)
		{
			for (int l = 0; l < animation.boneIndices.Length; l++)
			{
				array[k * animation.boneIndices.Length + l] = dictionary[dictionary.Keys.ToArray()[l]][k];
			}
		}
		animation.SetElements(array);
		return new Colossal.Animations.AnimationClip
		{
			m_BoneHierarchy = source.m_BoneHierarchy,
			m_Animation = animation
		};
	}

	private static RenderPrefab CreateRenderPrefab(string sourcePath, string name, int lodLevel, IPrefabFactory prefabFactory = null)
	{
		return CreatePrefab<RenderPrefab>("Mesh", sourcePath, name, lodLevel, prefabFactory);
	}

	private static T CreatePrefab<T>(string suffix, string sourcePath, string name, int lodLevel, IPrefabFactory prefabFactory = null) where T : PrefabBase
	{
		string name2 = name + ((!string.IsNullOrEmpty(suffix)) ? (" " + suffix) : string.Empty);
		T val = ((prefabFactory != null) ? prefabFactory.CreatePrefab<T>(sourcePath, name2, lodLevel) : null);
		if (val == null)
		{
			val = ScriptableObject.CreateInstance<T>();
			val.name = name2;
		}
		return val;
	}

	private static void CreateRenderPrefabs(Colossal.AssetPipeline.Settings settings, string sourcePath, IReadOnlyList<List<Colossal.AssetPipeline.LOD>> assets, ImportMode importMode, Report report, HashSet<SurfaceAsset> VTMaterials, IPrefabFactory prefabFactory = null)
	{
		using (s_ProfPostImport.Auto())
		{
			if (!IsLODsValid(assets))
			{
				log.DebugFormat("Result for {0} is not valid and will not be serialized", sourcePath);
				return;
			}
			string name = assets[0][0].name;
			try
			{
				using (report.AddImportStep("Perform main thread tasks (Assetdatabase serialization + Prefabs upgrade)"))
				{
					foreach (List<Colossal.AssetPipeline.LOD> asset in assets)
					{
						CreateRenderPrefab(settings, sourcePath, asset, importMode, report, VTMaterials, prefabFactory);
					}
				}
			}
			catch (Exception ex)
			{
				log.Error(ex, "Error post-importing " + name + ".");
				report.AddError("Error post-importing " + name + ": " + ex.Message + ".");
			}
		}
	}

	private static void SetupLODs(Colossal.AssetPipeline.Settings settings, IReadOnlyList<(RenderPrefab prefab, Report.Prefab report)> meshPrefabs)
	{
		if (!settings.setupLODs || meshPrefabs.Count <= 1)
		{
			return;
		}
		RenderPrefab item = meshPrefabs[0].prefab;
		ProceduralAnimationProperties component = item.GetComponent<ProceduralAnimationProperties>();
		ContentPrerequisite component2 = item.GetComponent<ContentPrerequisite>();
		LodProperties lodProperties = item.AddOrGetComponent<LodProperties>();
		meshPrefabs[0].report.AddComponent(lodProperties.ToString());
		lodProperties.m_LodMeshes = new RenderPrefab[meshPrefabs.Count - 1];
		for (int i = 1; i < meshPrefabs.Count; i++)
		{
			if (component != null)
			{
				ProceduralAnimationProperties proceduralAnimationProperties = meshPrefabs[i].prefab.AddComponentFrom(component);
				proceduralAnimationProperties.m_Animations = null;
				meshPrefabs[i].report.AddComponent(proceduralAnimationProperties.ToString());
			}
			if (component2 != null)
			{
				ContentPrerequisite contentPrerequisite = meshPrefabs[i].prefab.AddComponentFrom(component2);
				meshPrefabs[i].report.AddComponent(contentPrerequisite.ToString());
			}
			lodProperties.m_LodMeshes[i - 1] = meshPrefabs[i].prefab;
		}
	}

	private static void SetupComponents(Colossal.AssetPipeline.Settings settings, RenderPrefab meshPrefab, Colossal.AssetPipeline.LOD lod, Report.Prefab report)
	{
		if (lod.level == 0)
		{
			SetupEmissiveComponent(settings, meshPrefab, lod, report);
			if (settings.useProceduralAnimation)
			{
				SetupProceduralAnimationComponent(settings, meshPrefab, lod, report);
			}
		}
	}

	private static void SetupEmissiveComponent(Colossal.AssetPipeline.Settings settings, RenderPrefab meshPrefab, Colossal.AssetPipeline.LOD lod, Report.Prefab report)
	{
		List<EmissiveProperties.MultiLightMapping> multiLightProps = new List<EmissiveProperties.MultiLightMapping>();
		List<EmissiveProperties.SingleLightMapping> list = new List<EmissiveProperties.SingleLightMapping>();
		int num = 0;
		Surface[] surfaces = lod.surfaces;
		foreach (Surface surface in surfaces)
		{
			if (surface.emissiveLayers.Count == 0)
			{
				if (surface.HasProperty("_EmissiveColorMap"))
				{
					list.Add(new EmissiveProperties.SingleLightMapping
					{
						purpose = (surface.name.Contains("Neon") ? EmissiveProperties.Purpose.NeonSign : EmissiveProperties.Purpose.DecorativeLight),
						intensity = 5f,
						materialId = num++
					});
				}
				continue;
			}
			foreach (Surface.EmissiveLayer emissiveLayer in surface.emissiveLayers)
			{
				multiLightProps.Add(new EmissiveProperties.MultiLightMapping
				{
					intensity = emissiveLayer.intensity,
					luminance = emissiveLayer.luminance,
					color = emissiveLayer.color,
					layerId = emissiveLayer.layerId,
					purpose = EmissiveProperties.Purpose.None,
					colorOff = Color.black,
					animationIndex = -1,
					responseTime = 0f
				});
			}
		}
		if (list.Count > 0)
		{
			if (!meshPrefab.TryGet<EmissiveProperties>(out var component))
			{
				component = meshPrefab.AddComponent<EmissiveProperties>();
				component.m_SingleLights = list;
				report.AddComponent(component.ToString()).AddMessage($"Missing EmissiveProperties. {list.Count} single lights found. Please set up correctly...");
				log.WarnFormat(meshPrefab, "Mesh prefab {1} was missing EmissiveProperties. {0} single lights found. Please set up correctly...", list.Count, meshPrefab.name);
			}
			else if (component.m_SingleLights.Count != list.Count)
			{
				report.AddComponent(component.ToString()).AddMessage($"EmissiveProperties already added but the asset contains a different lightCount than set. Expected: {list.Count} Found: {component.m_SingleLights.Count}. Please set up correctly...");
				log.WarnFormat(meshPrefab, "Mesh prefab {2} has an EmissiveProperties but the asset contains a different lightCount than set. Expected: {0} Found: {1}. Please set up correctly...", list.Count, component.m_SingleLights.Count, meshPrefab.name);
			}
		}
		if (multiLightProps.Count <= 0)
		{
			return;
		}
		if (!meshPrefab.TryGet<EmissiveProperties>(out var component2))
		{
			component2 = meshPrefab.AddComponent<EmissiveProperties>();
			component2.m_MultiLights = multiLightProps;
			report.AddComponent(component2.ToString()).AddMessage($"Missing EmissiveProperties. {multiLightProps.Count} light layers found. Please set up correctly...");
			log.WarnFormat(meshPrefab, "Mesh prefab {1} was missing EmissiveProperties. {0} light layers found. Please set up correctly...", multiLightProps.Count, meshPrefab.name);
			return;
		}
		if (component2.m_MultiLights.Count != multiLightProps.Count)
		{
			report.AddComponent(component2.ToString()).AddWarning($"EmissiveProperties already added but the asset contains a different light layer count than set. Expected: {list.Count} Found: {component2.m_MultiLights.Count}. Please set up correctly...");
			log.WarnFormat(meshPrefab, "Mesh prefab {2} has an EmissiveProperties but the asset contains a different light layer count than set. Expected: {0} Found: {1}. Please set up correctly...", list.Count, component2.m_MultiLights.Count, meshPrefab.name);
		}
		int i2;
		int i;
		for (i2 = 0; i2 < multiLightProps.Count; i2 = i)
		{
			EmissiveProperties.MultiLightMapping multiLightMapping = component2.m_MultiLights.Find((EmissiveProperties.MultiLightMapping x) => x.layerId == multiLightProps[i2].layerId);
			if (multiLightMapping != null)
			{
				multiLightProps[i2].purpose = multiLightMapping.purpose;
				multiLightProps[i2].color = multiLightMapping.color;
				multiLightProps[i2].colorOff = multiLightMapping.colorOff;
				multiLightProps[i2].animationIndex = multiLightMapping.animationIndex;
				multiLightProps[i2].responseTime = multiLightMapping.responseTime;
			}
			i = i2 + 1;
		}
		component2.m_MultiLights = multiLightProps;
	}

	private static bool GetSkinningInfo(ModelImporter.Model model, out ModelImporter.Model.BoneInfo[] bones, Report.Prefab report)
	{
		bones = model.GetBones();
		if (model.HasAttribute(VertexAttribute.BlendIndices))
		{
			if (model.rootBoneIndex == -1)
			{
				report.AddWarning(model.name + " is missing root bone");
				return false;
			}
			if (model.bones == null)
			{
				report.AddWarning(model.name + " is missing bind poses");
				return false;
			}
			if (!model.HasAttribute(VertexAttribute.BlendWeight) && model.GetAttributeData(VertexAttribute.BlendIndices).dimension != 1)
			{
				report.AddWarning(model.name + " has BlendIndices but no BlendWeight. Assuming rigid skinning..");
				return false;
			}
			return true;
		}
		if (model.HasAttribute(VertexAttribute.BlendWeight))
		{
			report.AddWarning(model.name + " has BlendWeight but is missing BlendIndices");
		}
		return false;
	}

	private static string GetUniqueString(string input, int currentIndex, ProceduralAnimationProperties.BoneInfo[] array)
	{
		int num = 0;
		for (int i = 0; i < currentIndex; i++)
		{
			if (array[i].name.StartsWith(input))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return input;
		}
		return $"{input} {num}";
	}

	private static void SetupProceduralAnimationComponent(Colossal.AssetPipeline.Settings settings, RenderPrefab meshPrefab, Colossal.AssetPipeline.LOD lod, Report.Prefab report)
	{
		if (lod.geometry == null || !GetSkinningInfo(lod.geometry.models[0], out var bones, report))
		{
			return;
		}
		if (!meshPrefab.TryGet<ProceduralAnimationProperties>(out var component))
		{
			if (!settings.createProceduralAnimationForPrefab.Contains(lod.name))
			{
				return;
			}
			log.WarnFormat(meshPrefab, "Mesh prefab {0} was missing ProceduralAnimationProperties. Please set up correctly...", meshPrefab.name);
			component = meshPrefab.AddComponent<ProceduralAnimationProperties>();
			report.AddComponent(component.ToString());
		}
		ProceduralAnimationProperties.BoneInfo[] bones2 = new ProceduralAnimationProperties.BoneInfo[bones.Length];
		int i;
		int num;
		for (i = 0; i < bones.Length; i = num)
		{
			bones2[i] = new ProceduralAnimationProperties.BoneInfo
			{
				name = GetUniqueString(bones[i].name, i, bones2),
				position = bones[i].localPosition,
				rotation = bones[i].localRotation,
				scale = bones[i].localScale,
				parentId = bones[i].parentIndex,
				bindPose = bones[i].bindPose
			};
			if (component.m_Bones != null)
			{
				ProceduralAnimationProperties.BoneInfo boneInfo = Array.Find(component.m_Bones, (ProceduralAnimationProperties.BoneInfo x) => x.name == bones2[i].name);
				if (boneInfo != null)
				{
					bones2[i].m_Acceleration = boneInfo.m_Acceleration;
					bones2[i].m_Speed = boneInfo.m_Speed;
					bones2[i].m_ConnectionID = boneInfo.m_ConnectionID;
					bones2[i].m_SourceID = boneInfo.m_SourceID;
					bones2[i].m_Type = boneInfo.m_Type;
				}
			}
			num = i + 1;
		}
		component.m_Bones = bones2;
		if (settings.animations != null)
		{
			Dictionary<Colossal.Hash128, ProceduralAnimationProperties.AnimationInfo> dictionary = new Dictionary<Colossal.Hash128, ProceduralAnimationProperties.AnimationInfo>();
			if (component.m_Animations != null)
			{
				ProceduralAnimationProperties.AnimationInfo[] animations = component.m_Animations;
				foreach (ProceduralAnimationProperties.AnimationInfo animationInfo in animations)
				{
					dictionary[animationInfo.animationAsset.guid] = animationInfo;
				}
			}
			bool flag = false;
			string meshName = meshPrefab.name.Split(" ").FirstOrDefault();
			if (meshPrefab.TryGet<CharacterProperties>(out var component2) && !string.IsNullOrEmpty(component2.m_AnimatedPropName))
			{
				meshName = "#" + component2.m_AnimatedPropName;
				flag = true;
			}
			List<AnimationAsset> list = targetDatabase.GetAssets(new SearchFilter<AnimationAsset>
			{
				str = meshName
			}).ToList();
			List<ProceduralAnimationProperties.AnimationInfo> list2 = new List<ProceduralAnimationProperties.AnimationInfo>();
			int num2 = 0;
			List<int> list3 = new List<int>();
			foreach (AnimationAsset animationAsset in list)
			{
				foreach (KeyValuePair<string, Colossal.AssetPipeline.Settings.AnimationDefinition> item in settings.animations.Where((KeyValuePair<string, Colossal.AssetPipeline.Settings.AnimationDefinition> a) => Regex.IsMatch(animationAsset.name, "(" + meshName + "_" + a.Key + "|.*" + a.Key + "_.*)")))
				{
					if (dictionary.TryGetValue(animationAsset.id, out var value))
					{
						list2.Add(value);
					}
					else
					{
						list2.Add(new ProceduralAnimationProperties.AnimationInfo
						{
							animationAsset = animationAsset,
							layer = Game.Prefabs.AnimationLayer.PlaybackLayer0
						});
					}
					Colossal.Animations.AnimationClip animationClip = animationAsset.Load();
					list3.AddRange(animationClip.m_Animation.boneIndices);
					if (!flag)
					{
						list2[num2].name = meshName + "_" + item.Key;
					}
					else
					{
						list2[num2].name = item.Key;
					}
					list2[num2].frameCount = animationClip.m_Animation.frameCount;
					list2[num2].frameRate = animationClip.m_Animation.frameRate;
					num2++;
				}
			}
			list3 = list3.Distinct().ToList();
			for (int num3 = 0; num3 < component.m_Bones.Length; num3++)
			{
				if (list3.Contains(num3))
				{
					if (component.m_Bones[num3].m_Type < BoneType.PlaybackLayer0 && component.m_Bones[num3].m_Type != BoneType.None)
					{
						report.AddWarning("Bone '" + component.m_Bones[num3].name + "' is involved in playback animation but has already a configured bone type. Check the data");
					}
					else
					{
						component.m_Bones[num3].m_Type = BoneType.PlaybackLayer0;
					}
				}
			}
			component.m_Animations = list2.ToArray();
		}
		if (!meshPrefab.prefab.TryGet<LodProperties>(out var component3))
		{
			return;
		}
		RenderPrefab[] lodMeshes = component3.m_LodMeshes;
		for (num = 0; num < lodMeshes.Length; num++)
		{
			if (lodMeshes[num].prefab.TryGetExactly<ProceduralAnimationProperties>(out var component4))
			{
				component4.m_Bones = bones2;
			}
		}
	}

	private static Variant ToJsonSchema(object obj)
	{
		return ToJsonSchema(obj.GetType());
	}

	private static Variant ToJsonSchema(Type type, Variant previous = null)
	{
		Variant variant = previous ?? new ProxyObject();
		string text = ToJsonType(type);
		variant["type"] = JSON.Load(text);
		ProxyObject properties = new ProxyObject();
		type.ForEachField(EncodeOptions.None, delegate(FieldInfo fieldInfo, bool typeHint)
		{
			ProxyObject value = (ProxyObject)JSON.Load("{ " + ToJsonSchema(fieldInfo) + " }");
			properties[fieldInfo.Name] = value;
		});
		if (typeof(ISettings).IsAssignableFrom(type))
		{
			properties["@type"] = JSON.Load("{ \"$ref\": \"#/definitions/settingsType\" }");
			ProxyArray proxyArray = new ProxyArray();
			proxyArray.Add(new ProxyString("@type"));
			variant["required"] = proxyArray;
		}
		variant["properties"] = properties;
		variant["additionalProperties"] = new ProxyBoolean(value: false);
		if (properties.Count <= 0 && !(text != "object"))
		{
			return null;
		}
		return variant;
	}

	private static string ToJsonType(Type type, bool nullable = false)
	{
		if (type == typeof(string))
		{
			if (!nullable)
			{
				return "\"string\"";
			}
			return "[ \"string\", \"null\" ]";
		}
		if (type.IsEnum)
		{
			return "\"string\"";
		}
		if (type == typeof(uint))
		{
			return "\"nonNegativeInteger\"";
		}
		if (type == typeof(int))
		{
			return "\"integer\"";
		}
		if (type == typeof(float) || type == typeof(double))
		{
			return "\"number\"";
		}
		if (type == typeof(bool))
		{
			return "\"boolean\"";
		}
		if (type.IsArray || typeof(IList).IsAssignableFrom(type))
		{
			return "\"array\"";
		}
		return "\"object\"";
	}

	private static string GetSettings()
	{
		string[] array = (from x in (from settings in (from ext in ImporterCache.GetSupportedExtensions()
					select ImporterCache.GetImporter(ext.Key).GetDefaultSettings()).Concat(from x in PostProcessorCache.GetTexturePostProcessors()
					select x.GetDefaultSettings()).Concat(from x in PostProcessorCache.GetModelPostProcessors()
					select x.GetDefaultSettings()).Concat(from x in PostProcessorCache.GetModelSurfacePostProcessors()
					select x.GetDefaultSettings())
					.Concat(from x in PostProcessorCache.GetGeometryPostProcessors()
						select x.GetDefaultSettings())
				where settings != null
				group settings by settings.GetType() into @group
				select @group.First()).Select(GetIfThen)
			where x != null
			select x).ToArray();
		string text = string.Empty;
		for (int num = 0; num < array.Length; num++)
		{
			if (num > 0)
			{
				text += ", \"else\": {";
			}
			text += array[num];
		}
		text += ", \"else\": { \"additionalProperties\": false";
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			text += "}";
		}
		return text;
		static string GetIfThen(ISettings settings)
		{
			Variant variant = ToJsonSchema(settings);
			if (variant == null)
			{
				return null;
			}
			string text2 = "[ { \"const\": \"" + settings.GetType().FullName + "\" }, { \"const\": \"" + settings.GetType().TypeName() + "\" } ]";
			return "\"if\": { \"properties\": { \"@type\": { \"oneOf\": " + text2 + " } } }, \"then\": " + variant.ToJSONString();
		}
	}

	private static Variant GetDefinitions()
	{
		ProxyObject proxyObject = new ProxyObject();
		IEnumerable<string> source = (from ext in ImporterCache.GetSupportedExtensions()
			select ImporterCache.GetImporter(ext.Key).GetType()).Distinct().SelectMany((Type t) => new string[2]
		{
			t.FullName,
			t.TypeName()
		});
		proxyObject["importers"] = JSON.Load("{ \"type\": \"string\", \"enum\": " + source.ToArray().ToJSONString() + " }");
		IEnumerable<string> source2 = (from x in (from x in (from x in (from x in (from ext in ImporterCache.GetSupportedExtensions()
							select ImporterCache.GetImporter(ext.Key).GetDefaultSettings()?.GetType()).Distinct().Concat(from x in PostProcessorCache.GetTexturePostProcessors()
							select x.GetDefaultSettings()?.GetType())
						where x != null
						select x).Concat(from x in PostProcessorCache.GetModelPostProcessors()
						select x.GetDefaultSettings()?.GetType())
					where x != null
					select x).Concat(from x in PostProcessorCache.GetModelSurfacePostProcessors()
					select x.GetDefaultSettings()?.GetType())
				where x != null
				select x).Concat(from x in PostProcessorCache.GetGeometryPostProcessors()
				select x.GetDefaultSettings()?.GetType())
			where x != null
			select x).SelectMany((Type t) => new string[2]
		{
			t.FullName,
			t.TypeName()
		});
		proxyObject["settingsType"] = JSON.Load("{ \"type\": \"string\", \"enum\": " + source2.ToArray().ToJSONString() + " }");
		return proxyObject;
	}

	private static string ToJsonSchema(FieldInfo fieldInfo)
	{
		string name = fieldInfo.Name;
		Type fieldType = fieldInfo.FieldType;
		bool nullable = name == "materialTemplate";
		string text = "\"type\": " + ToJsonType(fieldType, nullable);
		if (fieldType.IsEnum)
		{
			return text + ", \"enum\": " + Enum.GetNames(fieldType).ToJSONString();
		}
		if (fieldType.IsArray)
		{
			return text + ", \"items\": " + ToJsonSchema(fieldType.GetElementType()).ToJSONString();
		}
		if (typeof(IList).IsAssignableFrom(fieldType))
		{
			return text + ", \"items\": " + ToJsonSchema(fieldType.GetGenericArguments()[0]).ToJSONString();
		}
		return name switch
		{
			"importerTypeHints" => text + ", \"patternProperties\": { \"^\\\\.[A-Za-z0-9]+$\": { \"$ref\": \"#/definitions/importers\" } }, \"additionalProperties\": false", 
			"sharedAssets" => text + ", \"patternProperties\": { \"^[A-Za-z0-9_*{}]+(\\\\.[A-Za-z0-9]+)?(/_[A-Za-z0-9]+)?$\": { \"type\": \"string\", \"format\": \"uri-reference\" } }, \"additionalProperties\": false", 
			"importSettings" => text + ", \"additionalProperties\": { " + GetSettings() + " }", 
			_ => text, 
		};
	}

	private static void AddSimplifiedProperties(Variant schema)
	{
		schema["^LOD(\\d+|\\*)?(_[A-Za-z0-9*]+)?$"] = ToJsonSchema(typeof(LODPostProcessor.PostProcessSettings.LODLevelSettings));
		schema["^Surface(_[A-Za-z0-9*]+)?$"] = ToJsonSchema(typeof(SurfacePostProcessor.PostProcessSettings));
	}

	public static void GenerateJSONSchema()
	{
		PostProcessorCache.CachePostProcessors();
		ImporterCache.CacheSupportedExtensions();
		Variant variant = new ProxyObject();
		variant["$schema"] = new ProxyString("http://json-schema.org/draft-07/schema#");
		variant["definitions"] = GetDefinitions();
		ToJsonSchema(typeof(Colossal.AssetPipeline.Settings), variant);
		Variant schema = (variant["patternProperties"] = new ProxyObject());
		AddSimplifiedProperties(schema);
		variant["additionalProperties"] = new ProxyBoolean(value: false);
		string message = (GUIUtility.systemCopyBuffer = variant.ToJSON());
		log.Info(message);
	}

	private static string AdjustNamingConvention(string input)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (char c in input)
		{
			if (flag)
			{
				stringBuilder.Append(char.ToUpper(c));
				flag = false;
			}
			else
			{
				stringBuilder.Append(c);
			}
			if (c == '_')
			{
				flag = true;
			}
		}
		return stringBuilder.ToString();
	}

	private static bool IsArtRootPath(string rootPathName, string path, out string artProjectPath, out string artProjectRelativePath)
	{
		if (rootPathName == null)
		{
			throw new IOException("rootPath can not be null");
		}
		if (path == null)
		{
			throw new IOException("import path can not be null");
		}
		if (path == rootPathName)
		{
			throw new IOException("rootPath can not be the same as import path");
		}
		int num = path.IndexOf(rootPathName, StringComparison.Ordinal);
		bool flag = num != -1;
		artProjectRelativePath = (flag ? path.Substring(num + rootPathName.Length).Replace('\\', '/').TrimStart('/') : null);
		artProjectPath = (flag ? path.Substring(0, num + rootPathName.Length).Replace('\\', '/').TrimEnd('/') : null);
		return flag;
	}

	public static bool IsArtRootPath(string rootPathName, string[] paths, out string artProjectPath, out List<string> artProjectRelativePaths)
	{
		artProjectPath = null;
		artProjectRelativePaths = new List<string>(paths.Length);
		foreach (string text in paths)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (!IsArtRootPath(rootPathName, text, out var artProjectPath2, out var artProjectRelativePath))
				{
					return false;
				}
				if (artProjectPath != null && artProjectPath2 != artProjectPath)
				{
					throw new Exception("Root project path does not match. Previous: " + artProjectPath + " Current: " + artProjectPath2);
				}
				artProjectPath = artProjectPath2;
				artProjectRelativePaths.Add(artProjectRelativePath);
			}
		}
		return true;
	}

	public static IEnumerable<ISettingable> GetImportChainFor(Colossal.AssetPipeline.Settings settings, SourceAssetCollector.Asset asset, ReportBase report)
	{
		List<ISettingable> list = new List<ISettingable>();
		if (ImporterCache.GetImporter<IAssetImporter>(asset.path, out var importer, settings.importerTypeHints))
		{
			list.Add(importer);
		}
		string assetName;
		try
		{
			AssetUtils.ParseName(Path.GetFileNameWithoutExtension(asset.name), out var _, out assetName, out var _, out var _, out var _, out var _, out var _, out var _);
		}
		catch (FormatException)
		{
			log.WarnFormat("Invalid filename: {0}", Path.GetFileNameWithoutExtension(asset.name));
			assetName = Path.GetFileName(asset.name);
		}
		if (importer is TextureImporter)
		{
			foreach (ITexturePostProcessor texturePostProcessor in PostProcessorCache.GetTexturePostProcessors())
			{
				if (settings.GetPostProcessSettings(asset.name, texturePostProcessor, report, out var _))
				{
					list.Add(texturePostProcessor);
				}
			}
		}
		if (importer is ModelImporter)
		{
			foreach (IModelPostProcessor modelPostProcessor in PostProcessorCache.GetModelPostProcessors())
			{
				if (settings.GetPostProcessSettings(asset.name, modelPostProcessor, report, out var _))
				{
					list.Add(modelPostProcessor);
				}
			}
			foreach (IModelSurfacePostProcessor modelSurfacePostProcessor in PostProcessorCache.GetModelSurfacePostProcessors())
			{
				if (settings.GetPostProcessSettings(assetName, modelSurfacePostProcessor, report, out var _))
				{
					list.Add(modelSurfacePostProcessor);
				}
			}
			foreach (IGeometryPostProcessor geometryPostProcessor in PostProcessorCache.GetGeometryPostProcessors())
			{
				if (settings.GetPostProcessSettings(assetName, geometryPostProcessor, report, out var _))
				{
					list.Add(geometryPostProcessor);
				}
			}
		}
		return list;
	}

	public static IDictionary<SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset>, Colossal.AssetPipeline.Settings> CollectDataToImport(string projectRootPath, string[] assetPaths, Report report)
	{
		OrderedDictionary<SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset>, Colossal.AssetPipeline.Settings> orderedDictionary = new OrderedDictionary<SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset>, Colossal.AssetPipeline.Settings>();
		if (IsArtRootPath(projectRootPath, assetPaths, out var artProjectPath, out var artProjectRelativePaths))
		{
			using (Report.ImportStep item = report.AddImportStep("Collect asset group"))
			{
				foreach (SourceAssetCollector.AssetGroup<SourceAssetCollector.Asset> item2 in new SourceAssetCollector(artProjectPath, artProjectRelativePaths))
				{
					Report.Asset asset = report.AddAsset(item2.name);
					Colossal.AssetPipeline.Settings value = ImportSettings(projectRootPath, item2, (step: item, asset: asset));
					foreach (SourceAssetCollector.Asset item3 in item2)
					{
						if (value.ignoreSuffixes != null && Path.GetFileNameWithoutExtension(item3.name).EndsWithAny(value.ignoreSuffixes))
						{
							item2.RemoveFile(item3);
						}
					}
					foreach (KeyValuePair<string, string> item4 in value.UsedShaderAssets(item2, asset))
					{
						string path = ResolveRelativePath(projectRootPath, item4.Value, item2.rootPath);
						if (LongFile.Exists(path))
						{
							SourceAssetCollector.Asset file = new SourceAssetCollector.Asset(path, projectRootPath);
							item2.AddFile(file);
						}
					}
					orderedDictionary.Add(item2, value);
				}
				return orderedDictionary;
			}
		}
		throw new Exception("Invalid " + artProjectPath);
	}

	private static void CreateTitle(Transform parent, string text, Vector3 textPosOffset, Color bgColor, Color txtColor, int txtSize, float txtPadding)
	{
		GameObject gameObject = new GameObject("Title");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		TextMeshPro textMeshPro = gameObject.AddComponent<TextMeshPro>();
		textMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
		textMeshPro.text = text;
		textMeshPro.fontSize = txtSize;
		textMeshPro.color = txtColor;
		textMeshPro.alignment = TextAlignmentOptions.Center;
		textMeshPro.enableWordWrapping = false;
		Vector2 preferredValues = textMeshPro.GetPreferredValues();
		Vector3 vector = new Vector3(preferredValues.x + txtPadding, preferredValues.y + txtPadding, 0.1f);
		gameObject.transform.localPosition = textPosOffset + new Vector3((0f - preferredValues.x) / 2f, 0f, 0f);
		CreateBackground(gameObject.transform, "TextBg", Vector3.zero, new Vector3(vector.x / 10f, 1f, vector.y / 10f));
		backgroundMaterial.color = bgColor;
	}

	private static void CreateBounds(Transform parent)
	{
		Transform transform = parent.Find("Title");
		Transform transform2 = transform.Find("TextBg");
		CreateBackground(transform, "BoundsTop", Vector3.zero, new Vector3(1f, 1f, 0.1f));
		CreateBackground(transform, "BoundsBottom", Vector3.zero, new Vector3(1f, 1f, 0.1f));
		CreateBackground(transform, "BoundsRight", Vector3.zero, new Vector3(0.1f, 1f, 1f));
		CreateBackground(transform, "BoundsLeft", new Vector3((transform2.localScale.x * 0.5f + 0.05f) * 10f, 0f, 0f), new Vector3(0.1f, 1f, 1f));
	}

	private static void AdjustZBounds(Transform parent, float z)
	{
		Transform transform = parent.Find("Title");
		Transform transform2 = transform.Find("BoundsTop");
		Transform transform3 = transform.Find("BoundsBottom");
		Transform transform4 = transform.Find("BoundsRight");
		Transform transform5 = transform.Find("BoundsLeft");
		Vector3 localPosition = transform2.localPosition;
		localPosition.y = z;
		transform2.localPosition = localPosition;
		Vector3 localPosition2 = transform3.localPosition;
		localPosition2.y = 0f - z;
		transform3.localPosition = localPosition2;
		Vector3 localScale = transform5.localScale;
		localScale.z = z * 2f / 10f;
		transform5.localScale = localScale;
		Vector3 localScale2 = transform4.localScale;
		localScale2.z = z * 2f / 10f;
		transform4.localScale = localScale2;
	}

	private static void AdjustXBounds(Transform parent, float x)
	{
		Transform transform = parent.Find("Title");
		Transform transform2 = transform.Find("BoundsTop");
		Transform transform3 = transform.Find("BoundsBottom");
		Transform transform4 = transform.Find("BoundsRight");
		Vector3 localPosition = transform.Find("BoundsLeft").transform.localPosition;
		Vector3 localPosition2 = localPosition;
		localPosition2.x += x;
		transform4.transform.localPosition = localPosition2;
		float x2 = (localPosition.x + localPosition2.x) * 0.5f;
		Vector3 localPosition3 = transform2.localPosition;
		localPosition3.x = x2;
		transform2.localPosition = localPosition3;
		Vector3 localPosition4 = transform3.localPosition;
		localPosition4.x = x2;
		transform3.localPosition = localPosition4;
		float x3 = (localPosition2.x - localPosition.x) / 10f + 0.1f;
		Vector3 localScale = transform2.localScale;
		localScale.x = x3;
		transform2.localScale = localScale;
		Vector3 localScale2 = transform3.localScale;
		localScale2.x = x3;
		transform3.localScale = localScale2;
	}

	private static void CreateBackground(Transform parent, string name, Vector3 position, Vector3 size)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
		gameObject.transform.localScale = size;
		gameObject.transform.localPosition = new Vector3(position.x, position.y, position.z + 0.05f);
		gameObject.GetComponent<Renderer>().sharedMaterial = backgroundMaterial;
	}

	public static void InstantiateRenderPrefabs<T>(IEnumerable<(T prefab, string sourcePath)> prefabs, bool smartInstantiate, bool ignoreLODs) where T : PrefabBase
	{
		if (smartInstantiate)
		{
			List<(RenderPrefab, string)> list = (from tuple in prefabs
				where tuple.prefab is RenderPrefab && (!ignoreLODs || !tuple.prefab.name.Contains("_LOD"))
				select (tuple.prefab as RenderPrefab, GetParent(tuple.sourcePath)) into x
				orderby x.Item1.name, x.Item2
				select x).ToList();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			List<float> list2 = new List<float>();
			int num = 0;
			foreach (var item in list)
			{
				Bounds bounds = RenderingUtils.ToBounds(item.Item1.bounds);
				if (!dictionary.TryGetValue(item.Item2, out var value))
				{
					list2.Add(bounds.extents.z * 1.5f);
					dictionary.Add(item.Item2, num);
					num++;
				}
				else
				{
					list2[value] = Mathf.Max(list2[value], bounds.extents.z * 1.5f);
				}
			}
			float num2 = 5f;
			float num3 = 0f;
			Dictionary<string, GameObject> dictionary2 = new Dictionary<string, GameObject>(dictionary.Count);
			List<float> list3 = Enumerable.Repeat(num2, dictionary.Count).ToList();
			{
				foreach (var item2 in list)
				{
					int index = dictionary[item2.Item2];
					if (!dictionary2.TryGetValue(item2.Item2, out var value2))
					{
						string fileName = Path.GetFileName(item2.Item2);
						value2 = GameObject.Find(fileName);
						if (value2 == null)
						{
							value2 = new GameObject(fileName);
							CreateTitle(value2.transform, fileName, new Vector3(0f, 0.1f, 0f), new Color32(1, 174, 240, byte.MaxValue), Color.white, 48, 0.1f);
							CreateBounds(value2.transform);
						}
						num3 += list2[index];
						value2.transform.position = new Vector3(0f, 0f, num3);
						num3 += list2[index] + 10f;
						AdjustZBounds(value2.transform, list2[index] + 5f);
						dictionary2.Add(item2.Item2, value2);
					}
					if (GameObject.Find(value2.name + "/" + item2.Item1.name) == null)
					{
						GameObject gameObject = new GameObject(item2.Item1.name);
						gameObject.transform.parent = value2.transform;
						gameObject.AddComponent<RenderPrefabRenderer>().m_Prefab = item2.Item1;
						Bounds bounds2 = RenderingUtils.ToBounds(item2.Item1.bounds);
						list3[index] += bounds2.extents.x * 1.5f;
						gameObject.transform.localPosition = new Vector3(list3[index], 0f, 0f);
						list3[index] += bounds2.extents.x * 1.5f + num2;
						AdjustXBounds(value2.transform, list3[index]);
					}
				}
				return;
			}
		}
		int num4 = 0;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		foreach (var prefab in prefabs)
		{
			if ((ignoreLODs && prefab.prefab.name.Contains("_LOD")) || !(prefab.prefab is RenderPrefab renderPrefab))
			{
				continue;
			}
			GameObject gameObject2 = GameObject.Find(renderPrefab.name);
			if (gameObject2 == null)
			{
				gameObject2 = new GameObject(renderPrefab.name);
				gameObject2.AddComponent<RenderPrefabRenderer>().m_Prefab = renderPrefab;
				Bounds bounds3 = RenderingUtils.ToBounds(renderPrefab.bounds);
				num6 += bounds3.extents.x * 1.5f;
				gameObject2.transform.position = new Vector3(num6, 0f, num7);
				num6 += bounds3.extents.x * 1.5f;
				num5 = Mathf.Max(num5, bounds3.extents.z * 3f);
				num4++;
				if (num4 % 10 == 0)
				{
					num7 += num5;
					num5 = 0f;
					num6 = 0f;
				}
			}
			else
			{
				RenderPrefabRenderer component = gameObject2.GetComponent<RenderPrefabRenderer>();
				if (component != null)
				{
					component.m_Prefab = renderPrefab;
				}
			}
		}
		static string GetParent(string path)
		{
			int num8 = path.LastIndexOf('/');
			if (num8 < 0)
			{
				return path;
			}
			return path.Substring(0, num8);
		}
	}

	public static Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> GetTextureReferenceCount(IEnumerable<SurfaceAsset> surfaces, out int surfaceCount)
	{
		Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> dictionary = new Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>>();
		surfaceCount = 0;
		foreach (SurfaceAsset surface in surfaces)
		{
			using (surface)
			{
				surface.LoadProperties(useVT: false);
				foreach (KeyValuePair<string, Colossal.IO.AssetDatabase.TextureAsset> texture in surface.textures)
				{
					if (!dictionary.TryGetValue(texture.Value, out var value))
					{
						value = new List<SurfaceAsset>();
						dictionary.Add(texture.Value, value);
					}
					value.Add(surface);
				}
				surfaceCount++;
			}
		}
		return dictionary;
	}

	private static void ReportTextureReferenceStats(Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> textureReferencesMap, Report.ImportStep report)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Colossal.IO.AssetDatabase.TextureAsset key in textureReferencesMap.Keys)
		{
			if (textureReferencesMap[key].Count == 1)
			{
				num++;
			}
			else if (textureReferencesMap[key].Count == 2)
			{
				num2++;
			}
			else
			{
				num3++;
			}
		}
		report.AddMessage($"Singles: {num}");
		report.AddMessage($"Doubles: {num2}");
		report.AddMessage($"Multiple: {num3}");
	}

	private static int TestTextureSizesUniformity(SurfaceAsset asset, int tileSize, MaterialLibrary.MaterialDescription description)
	{
		int num = description.m_Stacks.Length;
		int[] array = new int[num];
		int[] array2 = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = -1;
		}
		foreach (KeyValuePair<string, Colossal.IO.AssetDatabase.TextureAsset> texture in asset.textures)
		{
			int stackConfigIndex = description.GetStackConfigIndex(texture.Key);
			if (stackConfigIndex != -1)
			{
				Colossal.IO.AssetDatabase.TextureAsset value = texture.Value;
				value.LoadData(0);
				if (value.width < tileSize)
				{
					return -1;
				}
				if (value.height < tileSize)
				{
					return -2;
				}
				if (array[stackConfigIndex] == -1)
				{
					array[stackConfigIndex] = value.width;
					array2[stackConfigIndex] = value.height;
				}
				else if (array[stackConfigIndex] != value.width || array2[stackConfigIndex] != value.height)
				{
					return -4;
				}
			}
		}
		return 0;
	}

	public static void ProcessSurfacesForVT(IEnumerable<SurfaceAsset> surfacesToConvert, IEnumerable<SurfaceAsset> surfaces, bool force, Report.ImportStep report)
	{
		int midMipsCount = 3;
		int tileSize = 512;
		int mipBias = 20;
		ConvertSurfacesToVT(surfacesToConvert, surfaces, writeVTSettings: false, tileSize, midMipsCount, mipBias, force, report);
		BuildMidMipsCache(surfaces, tileSize, midMipsCount, AssetDatabase.game);
		HideVTSourceTextures(surfacesToConvert);
		ResaveCache(report);
	}

	public static void ConvertSurfacesToVT(IEnumerable<SurfaceAsset> surfacesToConvert, IEnumerable<SurfaceAsset> allSurfaces, bool force, Report.ImportStep report)
	{
		int midMipsCount = 3;
		int tileSize = 512;
		int mipBias = 20;
		ConvertSurfacesToVT(surfacesToConvert, allSurfaces, writeVTSettings: false, tileSize, midMipsCount, mipBias, force, report);
	}

	public static void ConvertSurfacesToVT(IEnumerable<SurfaceAsset> surfacesToConvert, IEnumerable<SurfaceAsset> allSurfaces, bool writeVTSettings, int tileSize, int midMipsCount, int mipBias, bool force, Report.ImportStep report)
	{
		s_Progress.Set("VT post process - Converting surfaces", "Collecting references...", 0f);
		int surfaceCount;
		Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> textureReferenceCount = GetTextureReferenceCount(allSurfaces, out surfaceCount);
		ReportTextureReferenceStats(textureReferenceCount, report);
		MaterialLibrary materialLibrary = AssetDatabase.global.resources.materialLibrary;
		VirtualTexturingConfig virtualTexturingConfig = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");
		int num = 0;
		foreach (SurfaceAsset item in surfacesToConvert)
		{
			s_Progress.Set("VT post process - Converting surfaces", "Processing " + item.name, (float)num++ / (float)surfaceCount);
			try
			{
				bool flag = item.IsVTMaterialFromHeader();
				if (!force && flag)
				{
					continue;
				}
				item.LoadProperties(useVT: false);
				MaterialLibrary.MaterialDescription materialDescription = materialLibrary.GetMaterialDescription(item.materialTemplateHash);
				if (materialDescription != null)
				{
					if (materialDescription.m_SupportsVT)
					{
						switch (TestTextureSizesUniformity(item, tileSize, materialDescription))
						{
						case 0:
						{
							if (!materialDescription.m_SupportsVT)
							{
								break;
							}
							int mipBias2 = (materialDescription.hasMipBiasOverride ? materialDescription.m_MipBiasOverride : mipBias);
							if (item.Save(mipBias2, force: true, saveTextures: true, vt: true, virtualTexturingConfig, textureReferenceCount, tileSize, midMipsCount))
							{
								log.InfoFormat("File {0} has been converted to VT", item);
							}
							goto end_IL_0089;
						}
						case -1:
							log.WarnFormat("File {0} cannot use VT because at least one of its textures width is smaller than the tileSize {1}", item, tileSize);
							break;
						case -2:
							log.WarnFormat("File {0} cannot use VT because at least one of its textures height is smaller than the tileSize {1}", item, tileSize);
							break;
						case -3:
							log.WarnFormat("File {0} cannot use VT because at least one texture uses a wrap mode that is not Clamp", item);
							break;
						case -4:
							log.WarnFormat("File {0} cannot use VT because its texture sizes is not uniform", item);
							break;
						case -5:
							log.WarnFormat("File {0} cannot use VT because some of its textures are null", item);
							break;
						}
					}
					else
					{
						log.WarnFormat("File {0} cannot use VT because its template {2} (Shader:{3}) from material hash {1} is not set to support VT", item, item.materialTemplateHash, materialDescription.m_Material.name, materialDescription.m_Material.shader.name);
					}
				}
				else
				{
					log.WarnFormat("File {0} cannot use VT because its material hash {1} is not mapped or not found", item, item.materialTemplateHash);
				}
				if (flag)
				{
					item.Save(0, force: true, saveTextures: false);
					log.InfoFormat("File {0} has been unconverted from VT", item);
				}
				end_IL_0089:;
			}
			catch (Exception exception)
			{
				log.ErrorFormat(exception, "Error occured with {0}", item);
				throw;
			}
			finally
			{
				item.Unload();
			}
		}
		if (writeVTSettings)
		{
			using (VTSettingsAsset vTSettingsAsset = AssetDatabase.game.AddAsset<VTSettingsAsset>(AssetDataPath.Create(EnvPath.kVTSubPath, "VT")))
			{
				vTSettingsAsset.Save(mipBias, tileSize, midMipsCount);
			}
		}
	}

	private static void ResaveCache(Report.ImportStep report)
	{
		s_Progress.Set("VT post process", "Resaving asset cache", 100f);
		report.AddMessage(AssetDatabase.global.ResaveCache().Result);
	}

	public static void HideVTSourceTextures(IEnumerable<SurfaceAsset> surfaces)
	{
		s_Progress.Set("VT post process - Hiding converted textures", "", 0f);
		Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> dictionary = new Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>>();
		Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> dictionary2 = new Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>>();
		foreach (SurfaceAsset surface in surfaces)
		{
			surface.LoadProperties(useVT: true);
			if (surface.isVTMaterial)
			{
				foreach (KeyValuePair<string, Colossal.IO.AssetDatabase.TextureAsset> texture in surface.textures)
				{
					if (surface.IsHandledByVirtualTexturing(texture))
					{
						AddReferenceTo(dictionary, texture.Value, surface);
					}
					else
					{
						AddReferenceTo(dictionary2, texture.Value, surface);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<string, Colossal.IO.AssetDatabase.TextureAsset> texture2 in surface.textures)
				{
					AddReferenceTo(dictionary2, texture2.Value, surface);
				}
			}
			surface.Unload();
		}
		List<Colossal.IO.AssetDatabase.TextureAsset> list = AssetDatabase.global.GetAssets(default(SearchFilter<Colossal.IO.AssetDatabase.TextureAsset>)).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Colossal.IO.AssetDatabase.TextureAsset textureAsset = list[i];
			s_Progress.Set("VT post process - Hiding", "Processing " + textureAsset.name, (float)i / (float)dictionary.Count);
			if (dictionary.ContainsKey(textureAsset))
			{
				if (dictionary2.ContainsKey(textureAsset))
				{
					log.WarnFormat("Texture {0} is referenced {1} times by VT materials and {2} times by non VT materials. It will be duplicated on disk.", textureAsset, dictionary[textureAsset].Count, dictionary2[textureAsset].Count);
					log.InfoFormat("Detail for {0}:\nvt: {1}\nnon vt: {2}", textureAsset, string.Join(", ", dictionary[textureAsset]), string.Join(", ", dictionary2[textureAsset]));
				}
				else
				{
					log.InfoFormat($"Hiding {textureAsset}");
				}
			}
		}
		static void AddReferenceTo(Dictionary<Colossal.IO.AssetDatabase.TextureAsset, List<SurfaceAsset>> references, Colossal.IO.AssetDatabase.TextureAsset texture, SurfaceAsset surface)
		{
			if (!references.TryGetValue(texture, out var value))
			{
				value = new List<SurfaceAsset>();
				references.Add(texture, value);
			}
			value.Add(surface);
		}
	}

	public static void BuildMidMipsCache(IEnumerable<SurfaceAsset> surfaces, int tileSize, int midMipsCount, ILocalAssetDatabase database)
	{
		s_Progress.Set("VT post process - Rebuilding mip cache", "", 0f);
		if (midMipsCount < 0)
		{
			throw new Exception("Nb mid mip levels can't be negative");
		}
		VirtualTexturingConfig virtualTexturingConfig = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");
		int nbConfigStacks = virtualTexturingConfig.stackDatas.Length;
		MaterialLibrary materialLibrary = AssetDatabase.global.resources.materialLibrary;
		AtlasMaterialsGrouper atlasMaterialsGrouper = new AtlasMaterialsGrouper(nbConfigStacks, tileSize, midMipsCount);
		List<SurfaceAsset> list = surfaces.ToList();
		Dictionary<Colossal.Hash128, NativeArray<byte>> dictionary = new Dictionary<Colossal.Hash128, NativeArray<byte>>();
		Dictionary<Colossal.Hash128, Colossal.Hash128[]>[] array = new Dictionary<Colossal.Hash128, Colossal.Hash128[]>[2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Dictionary<Colossal.Hash128, Colossal.Hash128[]>();
		}
		for (int j = 0; j < list.Count; j++)
		{
			using SurfaceAsset surfaceAsset = list[j];
			s_Progress.Set("VT post process - Rebuilding mip cache", "Processing " + surfaceAsset.name, (float)j / (float)list.Count);
			surfaceAsset.LoadProperties(useVT: true);
			if (!surfaceAsset.isVTMaterial)
			{
				continue;
			}
			MaterialLibrary.MaterialDescription materialDescription = materialLibrary.GetMaterialDescription(surfaceAsset.materialTemplateHash);
			long[] textureHash = new long[materialDescription.m_Stacks.Length];
			for (int k = 0; k < materialDescription.m_Stacks.Length; k++)
			{
				Colossal.Hash128[] value = new Colossal.Hash128[4];
				array[k][surfaceAsset.id] = value;
				surfaceAsset.AddMidMipTexturesDataToDictionary(k, midMipsCount, tileSize, materialDescription, dictionary);
			}
			int multiStackLayersMask = surfaceAsset.ComputeVTLayersMask(materialDescription, array, textureHash);
			for (int l = 0; l < surfaceAsset.stackCount; l++)
			{
				AtlassedSize unbiasedStackTextureSize = surfaceAsset.GetUnbiasedStackTextureSize(l);
				if (unbiasedStackTextureSize.x >= 0)
				{
					atlasMaterialsGrouper.Add(l, unbiasedStackTextureSize, multiStackLayersMask, surfaceAsset, textureHash, materialDescription.m_MipBiasOverride);
				}
			}
		}
		atlasMaterialsGrouper.ResolveDuplicates(array, 2);
		atlasMaterialsGrouper.GroupEntries(virtualTexturingConfig, dictionary, array);
		string assetName = AtlasMaterialsGrouper.GetAssetName(tileSize, midMipsCount);
		using (BinaryWriter bw = new BinaryWriter(database.AddAsset<MidMipCacheAsset>(AssetDataPath.Create("StreamingData~", assetName)).GetWriteStream()))
		{
			atlasMaterialsGrouper.Write(bw);
		}
		foreach (NativeArray<byte> value2 in dictionary.Values)
		{
			value2.Dispose();
		}
		atlasMaterialsGrouper.Dispose();
	}

	public static async Task ApplyVTMipBias(IAssetDatabase database, int mipBias, int tileSize, int midMipCount, string folder)
	{
		if (s_MainThreadDispatcher == null)
		{
			s_MainThreadDispatcher = new MainThreadDispatcher();
		}
		if (mipBias < 0)
		{
			throw new Exception("Mip bias cannot be smaller than zero in that context!");
		}
		bool flag = true;
		string text = null;
		if (database is ILocalAssetDatabase { dataSource: FileSystemDataSource dataSource })
		{
			text = dataSource.rootPath + "/StreamingData~";
			if (database != AssetDatabase.game)
			{
				flag = false;
			}
		}
		if (text == null)
		{
			throw new ArgumentException("Master VT file path is null.");
		}
		ILocalAssetDatabase vtMipXDatabase = AssetDatabase.GetTransient(0L, text + "/." + folder);
		VirtualTexturingConfig virtualTexturingConfig = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");
		ParallelOptions opts = new ParallelOptions
		{
			MaxDegreeOfParallelism = ((!useParallelImport) ? 1 : Environment.ProcessorCount)
		};
		int total = 0;
		Task importTask = Task.Run(delegate
		{
			List<VTTextureAsset> texture2DPreProcessedAssets = database.GetAssets(default(SearchFilter<VTTextureAsset>)).ToList();
			List<SurfaceAsset> list = database.GetAssets(default(SearchFilter<SurfaceAsset>)).ToList();
			int assetsToProcess = texture2DPreProcessedAssets.Count + list.Count;
			for (int i = 0; i < list.Count; i++)
			{
				try
				{
					using (SurfaceAsset surfaceAsset = list[i])
					{
						s_Progress.Set("VT post process - Apply Surface MipBias", "Applying Mip Bias to " + surfaceAsset.name, (float)total / (float)assetsToProcess);
						log.InfoFormat("Processing {0} ({1}/{2})", surfaceAsset, total + 1, assetsToProcess);
						if (!s_Progress.shouldCancel)
						{
							surfaceAsset.LoadProperties(useVT: false);
							if (surfaceAsset.isVTMaterial && surfaceAsset.hasVTSurfaceAsset)
							{
								surfaceAsset.UpdateMipBias(vtMipXDatabase, mipBias, virtualTexturingConfig, tileSize, midMipCount);
							}
							goto IL_0127;
						}
					}
					goto end_IL_0067;
					IL_0127:
					Interlocked.Increment(ref total);
					continue;
					end_IL_0067:;
				}
				catch (Exception exception)
				{
					log.ErrorFormat(exception, "Error with {0}", list[i]);
					continue;
				}
				break;
			}
			Parallel.ForEach(texture2DPreProcessedAssets, opts, delegate(VTTextureAsset texture2DPreProcessedAsset, ParallelLoopState state, long index)
			{
				try
				{
					s_Progress.Set("VT post process - Apply Texture MipBias", "Applying Mip Bias to " + texture2DPreProcessedAsset.name, (float)total / (float)assetsToProcess);
					log.InfoFormat("Processing {0} ({1}/{2})", texture2DPreProcessedAsset, total + 1, assetsToProcess);
					if (s_Progress.shouldCancel)
					{
						state.Stop();
					}
					texture2DPreProcessedAsset.LoadHeader();
					using (Colossal.IO.AssetDatabase.TextureAsset textureAsset = texture2DPreProcessedAsset.textureAsset)
					{
						textureAsset.LoadData(0);
						if (textureAsset.width < tileSize || textureAsset.height < tileSize)
						{
							log.ErrorFormat("That texture [{0}] dimension is too small to be supported by the VT system textureSize: {1}x{2} VT tileSize: {3}", textureAsset.name, textureAsset.width, textureAsset.height, tileSize);
						}
						using VTTextureAsset vTTextureAsset = vtMipXDatabase.AddAsset<VTTextureAsset>(texture2DPreProcessedAsset.name, texture2DPreProcessedAsset.id);
						vTTextureAsset.Save(mipBias, textureAsset, tileSize, midMipCount, virtualTexturingConfig);
					}
					Interlocked.Increment(ref total);
				}
				catch (Exception exception2)
				{
					log.ErrorFormat(exception2, "Error with {0}", texture2DPreProcessedAssets);
				}
			});
		});
		if (flag)
		{
			using VTSettingsAsset vTSettingsAsset = vtMipXDatabase.AddAsset<VTSettingsAsset>("VT");
			vTSettingsAsset.Save(mipBias, tileSize, midMipCount);
		}
		Report report = new Report();
		await ExecuteMainThreadQueue(importTask, report);
	}
}
