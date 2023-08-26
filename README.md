# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

Here's a lateral strafing fire pattern that leverages the same interface function that I made for the [burst scatter branch](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/burst_scatter). This is a bit simpler because it doesn't change the timing of the rounds.

<video controls src="https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/assets/48565771/0f7ea8ae-ff27-431f-b6c9-7d934d7dc89a">
  <p>laterally strafing a wall</p>
</video>

I'm more positive about this new interface function now that I've found a second use for it. I'm thinking that the "on_wpn_fired" event should be moved out of `PartEventsTargeted` and put in a new event class with an expanded interface function.

I think the interface should have the function signature I developed in `ISubsystemFunctionProjectileCreated.cs`.
```
void OnPartProjectileCreated(
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
```
The call to the event should be moved out of `AttachTypeSpecificProjectileData()` and back into `ProcessProjectiles()` after the call to `AttachTypeSpecificProjectileData`.
