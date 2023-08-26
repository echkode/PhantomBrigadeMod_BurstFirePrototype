# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

This repo contains prototype research work on using the newly introduced event interface functions for modding attack actions. While the code can be compiled to create a mod, it's primary purpose is to document various attempts I made at implementing alternate firing patterns than the two built-in ones.

This README is broken up into different sections.

- A [summary](#synopsis) of what I looked at and what changes I recommend
- The [branches](#branches) in the repo
- Details about a new interface for the [subactions created event](#details on new interface for subactions created event)
- Details about a new interface for the [projectile created event](#details on new interface for projectile created event)
- About the [part event pattern](#partevents pattern)
- Registering [YAML tag mappings](#yaml tag mappings)

Each branch has a README with some notes about the changes introduced in that branch. The intention is to keep this document as concise as possible.

## Synopsis

I prototyped a number of different ways of using interface functions to mod the attack action. There are two main systems of interest, `EquipmentActionSystem` and `ScheduledAttackSystem`.

`EquipmentActionSystem` takes an action for a part with a primary activation subsystem, creates subactions with evenly spaced timings to represent the firing of individual rounds. This is where the built-in activation timing feature makes changes to the round timings.

`ScheduledAttackSystem` generates projectiles for every subaction created by `EquipmentActionSystem`. This is where the starting and target positions and velocities are calculated and also where properties such as damage and visuals are assigned to the projectile.

Additional hooks into these systems have recently been added to the code. I call these hooks "event interface functions". These hooks are used in various other places throughout the codebase as well. I'm concentrating on their use in conjuction with parts for this project.

There are three main categories of events for parts: PartEventsGeneral, PartEventsAction, PartEventsTargeted. `EquipmentActionSystem` invokes `PartEventsGeneral` on part activation and `ScheduledAttackSystem` invokes `PartEventsTargeted` when a projectile is created and fully configured. The interface functions that correspond to these PartEvents have fairly concise signatures that omit some information that makes modding the attack action much easier.

Here's what I recommend to make modding attack actions better.

- Add an interface function that's called when attack subactions are created
- Add an interface function that's called when a projectile is created
- Change the `PartEventsGeneral` event that's triggered in `EquipmentActionSystem` to a `PartEventsAction` event and move it outside the `act_count > 0` check so that parts with primary activation subsystems that do not have an activationCount will still trigger the "on_part_activation" event
- Replace the partEventsTargeted "on_wpn_fired" event with the new interface function called when a projectile is created

## Branches

I took a step by step approach to prototyping how to mod attack actions and the projectiles created from them. I created a branch for each step and these branches have the code changes I made, some example ConfigEdit/Override files to demonstrate the changes and some notes about the changes. Some of the notes have videos showcasing the change. The branches are listed below in the order that I did the work.

- [Using PartEventsGeneral in EquipmentActionSystem](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/existing_interface)
- [New interface for subactions created event](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/new_interface)
- [Using new interface for a second firing pattern](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/burstfire_impl)
- [New interface for projectile created event](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/burst_scatter)
- [Using PartEventsTargeted in ScheduledAttackSystem](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/tracer_rounds)
- [Using new interface for third firing pattern](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/strafing_fire)

## Details on new interface for subactions created event

Attack subactions are created in `EquipmentActionSystem` in response to an attack action for a part with a primary activation subsystem that has an activationCount > 0. When all the subactions have been created, a `PartEventsGeneral` event is triggered. This event interface function only receives the part that triggered the event.

I implemented an emulation of the activation timing feature using a `PartEventsGeneral` function. However, that required a fair amount of lookup which already had been done in `EquipmentActionSystem` and also some inside knowledge about how durations are modified for animation reasons.

Given what I learned using `PartEventsGeneral`, I created a new event interface function that receives sufficient information to make changing fire timings a relatively easy task. In the code, I named the function interface `ISubsystemFunctionFiring` and the event `OnPartEventFiring` but I think better names would be `ISubsystemFunctionSubactionsCreated' and `OnPartEventSubactionsCreated`. This new event should replace the `PartEventsGeneral` event that's triggered.

Here is the interface with the new names.
```
public interface ISubsystemFunctionSubactionsCreated
{
    void OnPartEventSubactionsCreated(
        EquipmentEntity subsystem,
        string context,
        ActionEntity action,
        float startTime,
        float duration,
        List<ActionEntity> subactions);
}
```

Another change that I think should be made in `EquipmentActionSystem` is to add a `PartEventsAction` event if the part has an primary activation subsystem regardless of the activationCount of that subsystem. The current "on_part_activation" event should be moved from `PartEventsGeneral` to `PartEventsAction`. The reason to use `PartEventsAction` is that its interface function receives the action which may be necessary to distinguish between two different activations of the part in a single turn.

## Details on new interface for projectile created event

Each attack subaction may create a projectile. This happens in `ScheduledAttackSystem`. There is a `PartEventsTargeted` event that's triggered when a projectile is created. This event interface function was sufficient to create an effect like tracer rounds but does not receive enough information to implement the scatter and target control used in the burst and strafing fire patterns.

I think the `PartEventsTargeted` event should be moved up into the `ProcessProjectiles` method after the call to `AttachTypeSpecificProjectileData()` and replaced with a new event interface function that takes more information. I've named this new event interface `ISubsystemFunctionProjectileCreated`. I used the wrong name for the function in the interface in my code. Here's what the new event interface should look like.
```
public interface ISubsystemFunctionProjectileCreated
{
    void OnPartEventProjectileCreated(
        EquipmentEntity subsystem,
        string context,
        ActionEntity action,
        ActionEntity subaction,
        float currentTime,
        CombatEntity owner,
        CombatEntity projectile,
        Vector3 firingPoint,
        Vector3 firingDirection,
        Vector3 targetPoint);
}
```
A note about the `firingPoint` and `firingDirection` parameters. These are the position and direction, respectively, of the muzzle of the weapon. They may be different than the position and facing assigned to the projectile.

## PartEvents pattern

I tried to follow the pattern of `PartEventsGeneral` where that class implements an extension method on `EquipmentEntity` which iterates through the subsystems in the part and calls a `TryActivation` method on `DataBlockSubsystemFunctionsGeneral`. However, I cannot change the layout of `DataContainerSubsystem` so I was not able to faithfully duplicate this pattern.

The reason I bring this up is because of the `context` parameter in both of the new event interface fuctions. The new event interface functions have different signatures which implies they should belong to different event categories. However, these are fairly narrow and specific events so they're likely going to be the only events in their category and so won't need a context to distinguish invokings. That implies these events shouldn't follow the same event pattern as the other part events and the invoking logic should be made simpler.

## YAML tag mappings

Event interface functions can appear in the serialized YAML configuration files and therefore will need YAML tag mappings to permit the YAML reader to instantiate them correctly. There are two steps to this.

- Add a new tag and its matching type to the `tagMappings` property of `UtilitiesYAML`
- Rebuilding the YAML deserializer

The first step is obvious but the second one is a little more subtle. The deserializer uses the traditional lazy init pattern and once it is initialized, it is kept in a static variable in `UtilitiesYAML`. The deserializer receives the tag mappings as part of its initialization process. If a mod earlier in the load sequence triggers the initialization of the deserializer -- for example, to load ConfigOverride files -- any mod that's loaded later will not be able to register its YAML tags with the deserializer.

I made a function named `RegisterFunctions` that receives an assembly as an argument and does both steps. This should be a public static function on `UtilitiesYAML` so that mods that need to register tag mappings have a simple way to do it.

One policy that I highly recommend is that any types being registered from third-party libraries like mods are registered with their full type names. Mods should be built with namespaces and using these namespaces will reduce the likelihood that two mods will clash over names.

An additional benefit is that anyone inspecting the configuration changes of a mod will be able to see which interface functions are supplied by the game (the ones without namespaces) and which are supplied by other mods. They may even be able to know precisely which mods if mod makers are good about choosing their namespaces.
