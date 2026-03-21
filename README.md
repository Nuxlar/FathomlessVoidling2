# Fathomless Voidling

## TODOs
- Add "void moon" path

Releasing a short beta to catch any bugs/incompats before doing a full 1.0 release.

**CONFIGS WILL BE ADDED WITH 1.0, USE THE FORM BELOW TO ASK FOR SPECIFIC CONFIGS**

**INTENDED AS A STAGE 6 BOSS FIGHT**

### Feedback form! [Link](https://forms.gle/ACmqSCYwPHCnDvv48)

## Bug Reports

Report bugs using the GitHub link above or in the **RoR2 Modding Discord**. Please include a detailed description and a log file. For multiplayer bugs, provide logs from **both the host and a client**.

**SUBMIT A LOG WITH ALL BUG REPORTS PLEASE**

### How to get a log

If you are using **r2modman** or **Thunderstore Mod Manager**:
You can copy your log by going to the `Settings` screen, selecting the `Debugging` tab.  Click the `Copy LogOutput.log file contents to clipboard` and use `Ctrl+V` to paste it

NOTE: If it says `Log file does not exist` then you should double check in the `Locations tab` that your `Risk of Rain 2` and `Steam` folder paths are correct!

If you are using **Gale Mod Manager**
You can copy your log by either pressing the `File` tab then pressing either `Open game log` and use `Ctrl+A` then `Ctrl+C` and use `Ctrl+V` to paste it or `Open profile folder` then go to `BepInEx` and select `LogOutput.log` and use `Ctrl+V` to paste it

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

- **75% HP threshold:** go immune and spawn barnacles on the leg, immunity lifts once barnacles are killed
</details>

<details>
<summary>Phase Flow</summary>

### Phase 1

Voidling spawns at the center with the main body shielded.

**Voidling:**
- **Primary:** Mortar Blast I
- **Secondary:** Portal Beams I
- **Utility:** none
- **Special:** Wandering Singularity (active after 80% HP)

**Voidling Haunt:**
- Intermittent Gravity Bombs
- Gravity Barnacle director active after 80% HP

### Phase 2

Joint break, 1 leg retracted, other joints heal.

**Voidling:**
- **Primary:** Mortar Blast II (missile count 6 -> 8)
- **Secondary:** Portal Beams II (increased beam frequency)
- **Utility:** Maze I (active after 80% HP)
- **Special:** Wandering Singularity

**Voidling Haunt:**
- Intermittent Gravity Bombs (increased bomb quantity)
- Gravity Barnacle director still active

### Phase 3

Joint break, 2 legs retracted, other joints heal.

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
