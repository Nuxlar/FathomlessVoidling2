# Fathomless Voidling

MAZE ATTACK INCONSISTENT IN MULTIPLAYER.

BASE STATS BALANCED AROUND A STAGE 6 BOSS, IF LOOPING INCREASE THE HP IN THE CONFIG.

Reworked Voidling fight that uses the unused large voidling model. Spawns a void locus portal on stage 5, locus acts as an alternative moon. No void fog during void pillars by default, configurable. Locus has moon cauldrons. Risk of Options support for in-game configs

## Bug Reports

Report bugs using the GitHub link above or in the **RoR2 Modding Discord**. Please include a detailed description and a log file. For multiplayer bugs, provide logs from **both the host and a client**.

## Credits

- [DTEE](https://thunderstore.io/package/DTEE/) for the icon art (I added effects to the base art)
- [viliger](https://thunderstore.io/package/viliger/) for the void locus cauldron and stage 5 locus portal code

## Attack Details and Phase Flow

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
- **Primary:** Mortar Blast
- **Secondary:** Portal Beams
- **Utility:** none
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs
- Gravity Barnacle director active after first joint reaches 75% HP

### Phase 2

All joints reach 75% HP, triggering a ward wipe. Ward wipe forces gravity bombs and barnacle spawns. Phase 2 music begins.

**Voidling:**
- **Primary:** Mortar Blast
- **Secondary:** Portal Beams
- **Utility:** Maze I
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs
- Gravity Barnacle director still active

### Phase 3

All joints reach 50% HP, triggering a ward wipe. Ward wipe forces gravity bombs and barnacle spawns.

**Voidling:**
- **Primary:** Mortar Blast
- **Secondary:** Portal Beams
- **Utility:** Maze II (one laser targets a player)
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs (forced active during Maze)
- Gravity Barnacle director still active
</details>
