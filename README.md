# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

There is a `PartEventsTargeted` event triggered in `ScheduledAttackSystem` after a projectile has been created and configured. The event interface function has sufficient information passed to it to easily implement tracer rounds. For the tracer round effect, every nth projectile has a color override applied to its asset. Other effects like swapping out the asset completely or changing the damge types and so forth can easily be applied.

<video controls src="">
  <p>lighting up a dark scene with tracer rounds</p>
</video>

The reason I don't think that `PartEventsTargeted` is a good fit for the "on_wpn_fired" event is that there is a lot of extra information that goes into creating a projectile that's not passed on to the interface function. Scatter control as I implemented in the [burst scatter](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/burst_scatter) branch leverages that extra info and is something that can't be done with this event interface function.
