# TrainerKit

[![Sponsor](https://img.shields.io/badge/sponsor-%E2%9D%A4-lightgrey?logo=github&style=flat-square)](https://github.com/sponsors/sailro)

TrainerKit is an extract of the Feature-system developed for [EscapeFromTarkov-Trainer](https://github.com/sailro/EscapeFromTarkov-Trainer).

TrainerKit simplifies the development of internal Unity trainers by handling:
- UI rendering of features (everything is done automatically through reflection). Support for `Color`, `Float`, `Int`, `String`, `Bool`, `KeyCode` types.
- Load/save of settings.
- Runtime execution of Features.
- Injection into target process.

## Example with Unity's FPS Microgame

Starting with TrainerKit codebase, simply write the following code:

```csharp
using TrainerKit.Features;
using TrainerKit.Properties;
using Unity.FPS.Game;
	
namespace TrainerKit.DemoFPS;

internal class GodMode : ToggleFeature
{
	public override string Name => Strings.FeatureGodModeName;
	public override string Description => Strings.FeatureGodModeDescription;

	protected override void UpdateWhenEnabled()
	{
		var health = FindFirstObjectByType<Health>();
		health?.Invincible = true;
	}

	protected override void UpdateWhenDisabled()
	{
		var health = FindFirstObjectByType<Health>();
		health?.Invincible = false;
	}
}
```

and

```csharp
using TrainerKit.Configuration;
using TrainerKit.Features;
using TrainerKit.Properties;
using Unity.FPS.Game;
using UnityEngine;

namespace TrainerKit.DemoFPS;

internal class UnlimitedAmmo : ToggleFeature
{
	public override string Name => Strings.FeatureUnlimitedAmmoName;
	public override string Description => Strings.FeatureUnlimitedAmmoDescription;

	[ConfigurationProperty] public int DemoInt { get; set; } = 999;
	[ConfigurationProperty] public Color DemoColor { get; set; } = Color.red;
	[ConfigurationProperty] public string DemoText { get; set; } = "Text";
	[ConfigurationProperty] public KeyCode DemoKey { get; set; } = KeyCode.Alpha0;
	[ConfigurationProperty] public float DemoFloat { get; set; } = 3.14f;

	protected override void UpdateWhenEnabled()
	{
		var weapon = FindFirstObjectByType<WeaponController>();
		weapon?.MaxAmmo = int.MaxValue;
	}

	protected override void UpdateWhenDisabled()
	{
		var weapon = FindFirstObjectByType<WeaponController>();
		weapon?.MaxAmmo = 8;
	}
}
```

and the system will automatically generate the following window after injecting into the target process (use `RightAlt` by default to display the menu):

![TrainerKit Demo](./media/trainerkit.png)

Note that the system displays names like `!! [Id] !!` because it couldn't find the corresponding entries in the resource file (for example `PropertyDemoInt` for `DemoInt`):

![Resources](./media/resources.png)


The following configuration file can be automatically generated (and reloaded on next execution):

```ini
; Be careful when updating this file :)
; For keys, use https://docs.unity3d.com/ScriptReference/KeyCode.html
; Colors are stored as an array of 'RGBA' floats

TrainerKit.DemoFPS.GodMode.Enabled=true
TrainerKit.DemoFPS.GodMode.Key="None"

TrainerKit.DemoFPS.UnlimitedAmmo.Enabled=true
TrainerKit.DemoFPS.UnlimitedAmmo.Key="None"
TrainerKit.DemoFPS.UnlimitedAmmo.DemoColor=[1.0,0.0,0.0,1.0]
TrainerKit.DemoFPS.UnlimitedAmmo.DemoFloat=3.14
TrainerKit.DemoFPS.UnlimitedAmmo.DemoInt=999
TrainerKit.DemoFPS.UnlimitedAmmo.DemoKey="Alpha0"
TrainerKit.DemoFPS.UnlimitedAmmo.DemoText="Text"

TrainerKit.Features.Commands.Key="RightAlt"
TrainerKit.Features.Commands.ConsoleColor=[1.0,1.0,1.0,1.0]
TrainerKit.Features.Commands.X=462.0
TrainerKit.Features.Commands.Y=10.0
```


## How to Use

1- Starting from the source code, add the necessary references to the game you want to mod. You need at least:

```text
UnityEngine
UnityEngine.CoreModule
UnityEngine.IMGUIModule
UnityEngine.InputLegacyModule
UnityEngine.TextRenderingModule
```

2- Write your Features, starting from:
```
HoldFeature
ToggleFeature
TriggerFeature
```

Decorate your own properties with `ConfigurationPropertyAttribute`. This will expose your property to rendering and persistence. You can finetune the behavior by using dedicated settings:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationPropertyAttribute : Attribute
{
	// Skip completely (Rendering+Config), useful when overriding properties
	public bool Skip { get; set; } = false;

	// Dislay order
	public int Order { get; set; } = int.MaxValue;

    // Link a resource id for generating comment in the ini file
	public string CommentResourceId { get; set; } = string.Empty;
	
	// Enable/Disable rendering
	public bool Browsable { get; set; } = true;
}
```

3- Compile. If you need extra dependencies, you can either add them in the `Managed` folder of the target game, or il-merge everything into one unique assembly (we already use `ILRepack` to add a compatible `Newtonsoft.Json` assembly).

4- Run the executable, it will handle the injection into the target process (see `Program.cs` to customize the target):

```csharp
public static void Main()
{
	try
	{
		var location = Assembly.GetEntryAssembly()!.Location;
		var bytes = File.ReadAllBytes(location);

		const string processName = "TODO";
		using var injector = new Injector(processName);
		var intptr = injector.Inject(bytes, typeof(Context).Namespace, nameof(Context), nameof(Context.Load));

		Console.WriteLine(
			Strings.InjectorSuccess,
			Path.GetFileName(location),
			processName,
			injector.Is64Bit ? $"0x{intptr.ToInt64():X16}" : $"0x{intptr.ToInt32):X8}");
	}
	catch (Exception e)
	{
		Console.WriteLine(Strings.InjectorFailure, e.Message);
	}
}
```
Here is the output, but you can still use your preferred Mono injector.

```
Injected TrainerKit.exe into process FPS: 0x000002B0E9099380
```

Indeed if we have a look at the `TrainerKit.Context.Load` method, we can see the registration logic:

```csharp
public static void Load()
{
	var go = new GameObject(nameof(Context));
	UnityEngine.Object.DontDestroyOnLoad(go);
	FeatureFactory.RegisterAllFeatures(go);

	var commands = FeatureFactory.GetFeature<Commands>();
	if (commands == null)
		return;

	AddConsoleLog(Strings.FeatureRendererWelcome + $" ({commands.Key})");
}
```

If you hit any issue while injecting, make sure you are not missing any dependencies.

## Trainers using TrainerKit

- [EscapeFromTarkov-Trainer](https://github.com/sailro/EscapeFromTarkov-Trainer) for [Escape From Tarkov](https://www.escapefromtarkov.com/) game. *Before switching to IL2CPP*.
  - No `ILRepacking`: specific `BepInEx` injector, and `NewtonSoft.Json` types are directly provided by the game assemblies. Compiled for netfx `4.7.1`
  - Using Unity `2022.3.43f1`.

- [IronCast-Trainer](https://github.com/sailro/Ironcast-Trainer) for [IronCast](http://store.steampowered.com/app/327670/) game.
  - No `ILRepacking`: standalone `SharpMonoInjector`, and `NewtonSoft.Json` types are directly provided by the game assemblies.
  - Using Unity `4.6.1f1`, so compiled for netfx `3.5`, requiring minor code adaptations.

- [YAZDHD-Trainer]() for  [Yet Another Zombie Defense HD](https://store.steampowered.com/app/674750/Yet_Another_Zombie_Defense_HD) game.
  - `ILRepacking` to provide `SharpMonoInjector` as a built-in injector, and `NewtonSoft.Json` types internalized. Compiled for netfx `4.8.1`.
  - Using Unity `2019.4.13f1`.

- The demo Unity's FPS Microgame used here.
  - `ILRepacking` to provide `SharpMonoInjector` as a built-in injector, and `NewtonSoft.Json` types internalized. Compiled for netfx `4.8.1`.
  - Using Unity `2022.3.62f3`.
