# Fathomless Voidling

TODOs:
    - Tweaks to Master and Voidling mission controllers to account for the defined flow
    - Tweak VoidlingHaunt to increase intensity after P1
    - Tweak VoidlingHaunt to force gravity bombs during Maze in P3
    - See how difficult it would be for the Ward Wipe teleport
    - Set up Ward Wipe (phase transition attack)
    - Handle Voidling death properly
    - Think of what to do for Phase 4 (or just ship without a P4 based on workload)
    - NETWORKING STUFF (check after testing)
        - Redo VoidRain to use the entitystate rather than random components, will probably end up with bad networking
        - Redo Wandering Singularity to use the entitystate rather than random components, will probably end up with bad networking

What would 1.0 entail?
    - The complete move/attack set for voidling and voidling haunt
    - Properly ending the fight instead of manually ending the run
    - (MAYBE) Phase transition to alternate donuts
    - MP compat

## Attack Details and Phase Flow

Voidling Skills
    - Primary: Eye Blast (fires a volley of "mortars" that rain down, they have slight tracking)
    - Secondary: Portal Beams (spawns portals that fire out predictive laser beams)
    - Utility: Maze (laser pizza)
    - Special: Wandering Singularity (spawns a black hole that slowly follows enemies)

Voidling Haunt Attacks:
    - Gravity Bombs: spawns bombs across the arena, if hit, you'll get launched into a random direction
    - Gravity Barnacles: has a combat director for spawning special barnacles, they shoot gravity bullets that have the same effect as the bombs

Joints:
    - 75% HP threshold event: go immune and spawn barnacles on the leg, immunity dissipated once barnacles are killed

### Phase 1

Voidling spawns in at the center, shield up for the main body

Voidling attacks:
    - Primary: Mortar Blast I
    - Secondary: Portal Beams I
    - Utility: NONE
    - Special: Wandering Singularity (active after 80% HP)

Voidling Haunt:
    - Intermittent Gravity Bombs
    - Gravity Barnacle director active after 80% HP

### Phase 2

Joint break, 1 leg retracted, other joints heal

Voidling attacks:
    - Primary: Mortar Blast II (increase missile count 6 -> 8)
    - Secondary: Portal Beams II (increase beam spawn frequency)
    - Utility: Maze I (active after 80% HP)
    - Special: Wandering Singularity 

Voidling Haunt:
    - Intermittent Gravity Bombs (increased bomb quantity)
    - Gravity Barnacle director still active

### Phase 3

Joint break, 2 legs retracted, other joints heal

Voidling attacks:
    - Primary: Mortar Blast III (increase attack speed)
    - Secondary: Portal Beams III (increase beam spawn frequency)
    - Utility: Maze II (full line randomness)
    - Special: Wandering Singularity

Voidling Haunt:
    - Intermittent Gravity Bombs (forced active during Maze)
    - Gravity Barnacle director still active

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

<img width="1083" height="353" alt="Image" src="https://github.com/user-attachments/assets/cae570a0-e254-4ab1-8e6b-e5f3fedd16bc" />
