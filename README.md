# Fathomless Voidling

Spawns a void locus portal on stage 5 and go to the locus as an alt moon, currently no void fog during void pillars but there are cauldrons. 

TODO
- Add configs for everything
- Add music switch on phase change

Configs
void pillar count
specific ability tweaks (idk if all this is in there already) such as cooldown, damage, activation speed, etc.
Body stats (health, damage, attack speed, etc) as well as individual attack numbers (cooldown, damage, hopefully even projectile count?). Also configuration for the Locus changes.

## Bug Reports

Report bugs using the GitHub link above or in the **RoR2 Modding Discord**. Please include a detailed description and a log file. For multiplayer bugs, provide logs from **both the host and a client**.

## Credits

- [DTEE](https://thunderstore.io/package/DTEE/) for the icon art (I added effects to the base art)
- [viliger](https://thunderstore.io/package/viliger/) for the void locus cauldron and stage 5 locus portal code

## Attack Details and Phase Flow

***SPOILERS!** Use for reference when reporting bugs*

<details>
<summary>Expand</summary>

<details>
<summary>Attack Details</summary>

### Voidling Skills

- **Eye Blast:** fires a volley of mortars that rain down with slight tracking
- **Portal Beams:** spawns portals that fire predictive laser beams
- **Maze:** laser pizza
- **Wandering Singularity:** spawns a black hole that slowly follows enemies

***Maze and Singularity can interrupt the Eye Blast and Portal Beams***

### Voidling Haunt Attacks

- **Gravity Bombs:** spawns bombs across the arena, if hit you get launched in a random direction
- **Gravity Barnacles:** combat director spawns special barnacles that shoot gravity bullets with the same effect as the bombs

***Gravity attacks go through block but only deal 1 damage***

### Joints

- **P1/P2 threshold (75%/50% HP):** damage all joints to their threshold to trigger the ward wipe and advance the phase, joints become immune at the threshold
- **10% HP intervals:** joints cleanse their debuffs each time they lose 10% HP
- **Phase 3:** joints become immune at 1 HP, bring all joints to 1 HP to end the fight
</details>

<details>
<summary>Phase Flow</summary>

### Phase 1

Voidling spawns at the center with the main body shielded.

**Voidling:**
- **Primary:** Mortar Blast I
- **Secondary:** Portal Beams I
- **Utility:** none
- **Special:** Wandering Singularity (active after first joint reaches 75% HP)

**Voidling Haunt:**
- Intermittent Gravity Bombs
- Gravity Barnacle director active after first joint reaches 75% HP

### Phase 2

All joints reach 75% HP, triggering a ward wipe. Ward wipe forces gravity bombs and barnacle spawns.

**Voidling:**
- **Primary:** Mortar Blast II (wave count 3 -> 5)
- **Secondary:** Portal Beams II (increased beam frequency)
- **Utility:** Maze I
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs (increased bomb quantity)
- Gravity Barnacle director still active

### Phase 3

All joints reach 50% HP, triggering a ward wipe. Ward wipe forces gravity bombs and barnacle spawns.

**Voidling:**
- **Primary:** Mortar Blast III (increased attack speed)
- **Secondary:** Portal Beams III (increased beam frequency)
- **Utility:** Maze II (full line randomness)
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs (forced active during Maze)
- Gravity Barnacle director still active
</details>
</details>
