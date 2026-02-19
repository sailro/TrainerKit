using System;
using System.IO;
using System.Linq;
using MelonLoader;
using TrainerKit.Features;
using TrainerKit.Properties;

[assembly: MelonInfo(typeof(TrainerKit.Context), "TrainerKit", "1.0.0", "TrainerKit")]
[assembly: MelonGame]

namespace TrainerKit;

public class Context : MelonMod
{
	public static string UserPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	public static string ConfigFile => Path.Combine(UserPath, "trainerkit.ini");

	internal static Lazy<Feature[]> Features => new(() => [.. FeatureFactory.GetAllFeatures().OrderBy(f => f.Name)]);

	public static string LastConsoleLog { get; set; } = string.Empty;
	public static void AddConsoleLog(string log)
	{
		LastConsoleLog = log;
	}

	public override void OnInitializeMelon()
	{
		FeatureFactory.RegisterAllFeatures();

		var commands = FeatureFactory.GetFeature<Commands>();
		if (commands == null)
			return;

		AddConsoleLog(Strings.FeatureRendererWelcome + $" ({commands.Key})");
	}

	public override void OnUpdate()
	{
		foreach (var feature in Features.Value)
			feature.DoUpdate();
	}

	public override void OnGUI()
	{
		foreach (var feature in Features.Value)
			feature.DoOnGUI();
	}
}
