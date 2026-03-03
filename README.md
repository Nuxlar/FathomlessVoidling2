# Fathomless Voidling

How to make a raid boss less monotonous:
    - Environment hazards in the arena
    - Additional "skills" or attacks that aren't tied to the main body

Exploring hazards:
    - Void fog, though might just have this integrated with the main attacks
    - "mines" that dont do damage but maybe a version of the gravity bump from the original kit
    - convert the phase transition safe pillars into a damaging "projectile"

Voidling attacks should be large scale, cinematic, typical raid boss stuff. The joints should be mixing things up.

Voidling Skills
    - Primary: Eye Blast (removes tracking from normal projectiles, increases size, blast radius, and adds oscillation)
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

Add skilldrivers to the joints and have them do some attacks, the "auto" or "hazard" ones. So while voidling is doing the big, grandiose attacks, the joints are adding to the chaos. 

## Phase Flow

### Phase 1

Voidling spawns in at the center, shield up for the main body

Main Body Attacks
Primary: Mortar Blast (fires a volley of "mortars" that rain down)
Secondary: Portal Beams (fires beams from portals)
Utility: Laser Sweep (fires big laser and sweeps a part of the arena)
Special: Singularity (creates a giant black hole at the center of the arena)

Joint Attacks (random intervals? or make actual skilldrivers?):
- Random intervals would be easiest but I think actual skilldrivers would be good here, maybe a collider where if a player enters a certain area they can attack?
- No skilldrivers, but maybe just trigger one of the random attacks if a player enters an area around the leg
Attack1: Stomp (stomps down if enemy is within range)
Attack2: Portal bombs

Probably just an attack controller like the P4 controller from umbral or an invisible "voidling haunt"
Psychic Attacks:
Either A: random portal bombs that if hit, do the gravity bump
Or B: random portal bombs that spawn gravity mines after attacking
why not both?
So we got voidling haunt spawning gravity portal bombs, I'd say random on/off intervals so it's not constant
Maybe also random spawns of void squids (these tether down enemies within an area)
the stomps from the legs


Joints:
HP thresholds: 75% 50% 25% go immune and spawn barnacles on the leg, immunity dissipated once barnacles are killed

### Phase 2

Joint break, 1 leg retracted, other joints heal

### Phase 3

Joint break, 2 legs retracted, other joints heal

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

It's charging up the final stand to wipe out everyone, 
Portal Beams firing automatically


Known Issues:
- Joint respawn not networked


What would 1.0 entail?
- The complete move/attack set
- Some arena tweaks to hit the joints better
- MP compat
- Testing with drones or many allies
