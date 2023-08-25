# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

One of the more unusual aspects of the burst fire suggestion from Discord user Ragvard was to control the scatter on the rounds in a burst. There is a PartEventsTargeted event named "on_wpn_fired" in `ScheduledAttackSystem` that looked promising. However, there were some data in `ProcessProjectiles()` that I needed so I made my own subsystem function and event that I call from that function.

Much like the other interface function and event that I prototyped in the [new_interface branch](), I had to fake it with custom properties. It's supposed to follow the pattern of `PartEventsGeneral`.

Here's what it looks like in comparison to the stock scatter handling. With stock scatter, each bullet regardless of which burst it is in has a slightly different heading. With the burst scatter function, you can see that the trailing bullets in a burst follow the lead bullet but the lead bullets of each burst have different headings.

<video controls src="">
  <p>standard scatter affects all rounds in the activation</p>
</video>

<video controls src="">
  <p>burst scatter makes the rounds in a burst parallel the lead round</p>
</video>

I'm on the fence about this new interface function. While does let you get in a fiddle with the scatter and other details of the projectile at a really low level, I'm not sure what else it would be good for besides scatter and there might be a better way to handle scatter than this.
