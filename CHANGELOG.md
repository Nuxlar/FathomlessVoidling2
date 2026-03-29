**1.0.0**

- 1.0 RELEASE (no major changes/additions for the forseeable future)
- Fixes potential softlocks if level up occurs right before Ward Wipe fires
- Barnacle director now gains credits whenever Gravity Bombs start firing
- Barnacle director credit multiplier increase (0.3 -> 0.6)
- Voidling Haunt's cooldown reduced (P1 40 -> 30 | P2/P3 30 -> 20)
- Tweaks Wandering Singularity projectile to only follow players
- Increases lowest speed of Wandering Singularity from 20% of initial speed to 50%
- Reduces max frog pets (10 -> 1)
- Reduces damage from Eye Blast mortars by 25%
- Removes lunar coin cost for petting the frog
- Removes Voidling Haunt's gravity bomb spawn increase after P2 
- Removes Portal Beam fire frequency increase in P3

**0.9.15**

- Makes Voidling and Joints immune to executes, void death, and ignore breaching fin's knockup
- Increases joint base and level HP (1250 -> 1500 | 350 -> 425)

**0.9.14**

- Improves arena lighting/visibility
- Increases force on Gravity Bombs/Projectiles (3000 -> 5000)
- Ward Wipe now forces gravity bombs and barnacle spawns
- Increases Barnacle director passive credit generation
- Wandering Singularity now lasts longer (20s -> 30s)
- Portal Beams now properly show the indicator of where the attack will land (vanilla bug apparently)
- Reworks joint mechanic
    - You will now need to damage each joint down to its threshold to advance the phase
    - Joints now have 2 thresholds, 75% HP and 50% HP
    - Barnacles no longer spawn on the joints on thresholds
    - After phase advance, joints are no longer immune and no longer heal
    - Joints now cleanse debuffs when they lose 10% HP (like mithrix's dash)
- Tweaks Eye Blast mortars
    - Mortar count and fire delay no longer change between phases
    - Wave count (amount of times it fires a barrage) has been reduced to 3 in P1 then increases to 5 in P2/P3

**0.9.13**

- Fixes joints not having adaptive armor
- Fixes joints not being "boss" enemies
- Fixes clients not teleporting after Ward Wipe
- Fixes Maze lasers not being accurate for clients
- Adds warning VFX for maze lasers
- Adds nullchecks/tweaks to prevent incompats
- Adds R2API to dependencies

**0.9.12**

- We're so back