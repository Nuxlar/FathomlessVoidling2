# Fathomless Voidling

Also need to think for haunt, how to mix up the attacks, maybe changing intensity or intervals as the fight goes on, for now though, I think it's fine, need to work on the actual attacks

Voidling Skills
    - Primary: Eye Blast (larger missiles, much weaker tracking, fired upwards in a volley then rain down after pausing for a second)
    - Secondary: Portal Beams (really liked this from the initial draft)
    - Utility: Void Laser
    - Special: One of the singularities ig
    - Per phase changes
        - So not sure if I wanna make new abilities for the phases or just add more hazards/joint skills instead while buffing each ability based on the phase
        - PortalBeam is pretty simple, increase the quantity and frequency values per phase
        - EyeBlast could either add more missiles or more "waves"

Skill ideas to mess around with
    - Void Laser: Multiple possibilties here
        - Predictively point in a certain direction
        - The normal spinny one to sweep the donut arena (force players to get up on the parkour bits)
        - Spawns a portal and blasts a part of the arena (kinda like this one, block off a bit of the arena)
        - Laser sweeps a part of the arena, sees where player is for general direction, then sweeps that area with the laser
    - Abyssal Vision: Spawns a disembodied "eye" that fires a shotgun blast
    - Singularity: Can just be an enlarged version of the vanilla one, since there's more stuff going on, could be fun.
    - Asteroid Barrage: Spawns "orbiting" asteroids that are launched (one by one or simultaneously) or fall from the sky

Voidling Haunt is going to be an invisible body that adds some chaos to the fight so Voidling doesn't have the same issues as Solus Wing
Thinking of adding some kind of mechanics as well, like another way to mix things up so Haunt/Voidling aren't just cycling the same things
I think for Haunt specifically, maybe a counter for how many hits it lands, land X hits and goes into an "enrage"

Voidling Haunt Attacks:
- Gravity Bombs: spawns bombs across the arena, if hit, you'll get launched into a random direction
- Gravity Barnacles: has a combat director for spawning special barnacles, they shoot gravity bullets that have the same effect as the bombs

Joints:
Do I wanna add stomping to the legs?
HP thresholds: 75% 50% 25% go immune and spawn barnacles on the leg, immunity dissipated once barnacles are killed?

## Phase Flow

### Phase 1

Voidling spawns in at the center, shield up for the main body

Voidling attacks:
    - Primary: Mortar Blast I (fires a volley of "mortars" that rain down)
    - Secondary: Portal Beams I (fires beams from portals)
    - Utility: Maze I (fires 1 large lasers into the arena)
    - Special: UNDEFINED

Voidling Haunt attacks:
    - Gravity Bombs


### Phase 2

Joint break, 1 leg retracted, other joints heal

Voidling attacks:
    - Primary: Mortar Blast II (increase missile count 6 -> 8)
    - Secondary: Portal Beams II (increase beam spawn frequency)
    - Utility: Maze II (2 lasers)
    - Special: Singularity (creates a giant black hole at the center of the arena)

Voidling Haunt:
    - Gravity Bombs
    - Activate Barnacle Director


### Phase 3

Joint break, 2 legs retracted, other joints heal

Voidling attacks:
    - Primary: Mortar Blast III (increase attack speed)
    - Secondary: Portal Beams III (increase beam spawn frequency)
    - Utility: Maze III (more waves)
    - Special: UNDEFINED


### Phase 4

Final joint doesn't fully break, Voidling's final stand begins...

shield is down
(i dont want it to be just an umbral p4 meme, so maybe throw some stuff in there for the player to do, some objectives)
Deep void signals from locus? Few of these pop up and they protect you from the fog, could make it a charging meme but eh

Simulacrum crab "wakes up" with a small safe zone, wandering around
Disembodied eyes spawn to stop the crab, destroy them
If the crab is "asleep" when voidling dies, everyone is taken with it

OR 

Do something to prevent yourself from being taken with Voidling when it dies

Known Issues:
- Joint respawn not networked

What would 1.0 entail?
- The complete move/attack set
- Some arena tweaks to hit the joints better
- MP compat
- Testing with drones or many allies

ISSUES:
- Joint barnacle threshold kinda working, spawns an elite barnacle sometimes and doesn't stick to the joint if it does spawn
- Test threshold in multiplayer
- New problem, spawns are working but the joint body is still technically at the bottom of the fucking arena, so gotta figure out how to get the spawns to stick to the actual mf, kms

TODOs:
- Maze I, II, III
- Singularity (force haunt bombs during state)
- Joint threshold event