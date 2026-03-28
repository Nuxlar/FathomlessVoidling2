# Fathomless Voidling

Releasing a short beta to catch any bugs/incompats before doing a full 1.0 release.

**CONFIGS WILL BE ADDED WITH 1.0, USE THE FORM BELOW TO ASK FOR SPECIFIC CONFIGS, ALSO INTENDED AS A STAGE 6 BOSS FIGHT SO THE BASE HP WILL REFLECT THAT**

### Feedback form! [Link](https://forms.gle/ACmqSCYwPHCnDvv48)

Spawns a void locus portal on stage 5 and go to the locus as an alt moon, currently no void fog during void pillars but there are cauldrons. 

## Known Issues

- (Multiplayer) Joint break VFX doesn't show up for clients

## Bug Reports

Report bugs using the GitHub link above or in the **RoR2 Modding Discord**. Please include a detailed description and a log file. For multiplayer bugs, provide logs from **both the host and a client**.

**SUBMIT A LOG WITH ALL BUG REPORTS PLEASE**

### How to get a log

If you are using **r2modman** or **Thunderstore Mod Manager**:
You can copy your log by going to the `Settings` screen, selecting the `Debugging` tab.  Click the `Copy LogOutput.log file contents to clipboard` and use `Ctrl+V` to paste it

NOTE: If it says `Log file does not exist` then you should double check in the `Locations tab` that your `Risk of Rain 2` and `Steam` folder paths are correct!

If you are using **Gale Mod Manager**
You can copy your log by either pressing the `File` tab then pressing either `Open game log` and use `Ctrl+A` then `Ctrl+C` and use `Ctrl+V` to paste it or `Open profile folder` then go to `BepInEx` and select `LogOutput.log` and use `Ctrl+V` to paste it

## Credits

[DTEE](https://thunderstore.io/package/DTEE/) for the icon art (I added effects to the base art)
[viliger](https://thunderstore.io/package/viliger/) for the void locus cauldron and portal code

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
