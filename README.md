# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

This is an example of using a `ISubsystemFunctionGeneral` in conjunction with `PartEventsGeneral` and the `PartEventsGeneral.OnPartActivation` context. The implementation function is named `ExponentialRoundTiming` and it emulates the built-in activation timing feature which changes the firing timing of rounds.

I set up a debug scenario in which one mech is equipped with a machine gun that's been modified to have an activation timing block and another mech which is equipped with a similar machine gun that has an `ISubsystemFunctionGeneral` implementation instead to change the round timing in exactly the same way as the activation timing block. The two mechs have nearly identical firing patterns.

<video controls src="">
  <p>comparing prototype function implementation to built-in activation timing</p>
</video>

One issue with the `ISubsystemFunctionGeneral` interface is that no information other than the part and the context is passed as arguments. This makes the implementation a lot longer than it needs to be as the implementation has to do a number of lookups to get at the subaction `ActionEntity` objects for the individual rounds to adjust their timings. In addition, the duration of an action is actually reduced by some padding for animation and the implementation has to be aware of that for the new timings to work properly. It would be better if the calling function, `EquipmentActionSystem.Execute()`, passed all the necessary data to the implementation.

There is an additional function named `RegisterFunctions` that should be a public static method on the `UtilitiesYAML` class. This function updates the YAML tag mapping so that the implementation function can be mapped to a YAML tag. The `RegisterFunctions` function lifts code from `UtilitiesYAML.LoadTagMappings()` that deals with `TypeHinted` interfaces and some code from `UtilitiesYAML.SetupReader()` to rebuild the deserializer so that it has the new tag mappings.

One important addition I made is that the YAML tag for the implementation function includes the namespace of the implementation class. The YAML tag for the implementation function in this mod would appear as `!EchKode.PBMods.BurstFire.ExponentialRoundTiming` in config edits/overrides. This will permit different mods to use similarly named functions without clashing. It also separates out built-in functions from third-party ones and can help to identify which mods are supplying which functions.

Be aware that a mod cannot use the YAML tags it registers in any of its ConfigOverrides. The ConfigOverride YAML files are parsed before the library is loaded so the new tag mappings are not in place. This is not an issue for ConfigEdits as they're deferred until the databases are loaded. I've included some sample ConfigEdits/Overrides as part of this demonstration but they should be placed in a second mod that is loaded after the library mod.

One nice feature about this way of extending Phantom Brigade is that you don't need Harmony to inject code. The function interfaces are loaded through standard .NET idioms and that lowers the bar for prospective code modders.
