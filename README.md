# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

Following on with the [new interface](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/new_interface) function, I created a new implementation for a burst fire pattern. The idea comes from a couple of Discord users, Ragvard and Artyom. I merely put it in code. See this [Discord message](https://discord.com/channels/380929397445754890/1100537520057503776/1138638173438758933) for the high-level idea and this [message](https://discord.com/channels/380929397445754890/1100537520057503776/1143088045147566110) for how to encode the timings.

<video controls src="https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/assets/48565771/fa90d35f-6e96-48db-832b-48724fdd1a4e">
  <p>example of a burst fire pattern</p>
</video>

There were no surprises here, this was straightforward to implement, especially as it is a naive implementation that doesn't take into account other subsystems on the part modifying the act_count stat of the primary activation subsystem.
