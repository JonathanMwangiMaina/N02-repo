# BLACKOUT CLAUSE — Weapon Stats & Balance

## Balance Philosophy

- **TTK target:** ~1.5–2.5s vs equal-HP target at optimal range. Fights are decided by positioning and cooldown management, not raw stat checks.
- **Crit/headshot rule:** 2x multiplier on precision hitscan (Reveille, Wraith's sidearm), 1.5x on general-purpose hitscan (Vex, Suture's sidearm). No headshot multiplier on splash/AoE/beam weapons (Marrow, Breach, Cinder, Bulwark) — prevents double-dipping burst damage with crits.
- **No unbeatable matchup:** every kit has exactly one hard counter and one soft counter (see matrix below).
- **Ultimate charge is mixed-source:** damage dealt/taken plus role-specific actions (healing, building, cloaking, planting), so support and utility kits aren't starved for ult charge in low-kill rounds.
- **Ammo economy:** no passive regen. Resupply lockers are placed at fixed positions on every map (ties into map callouts in the maps doc).

---

## Base Stats

| Character | Role | HP | Move Speed |
|---|---|---|---|
| Vex | Recon | 100 | 130% |
| Marrow | Assault | 175 | 100% |
| Cinder | Incendiary | 150 | 100% |
| Breach | Demolitions | 150 | 95% |
| Bulwark | Heavy | 300 | 70% |
| Forge | Engineer | 125 | 100% |
| Suture | Medic | 125 | 105% |
| Reveille | Marksman | 125 | 95% |
| Wraith | Infiltrator | 100 | 110% (125% cloaked) |

---

## Weapon Loadouts

### VEX — Twin Auto-Pistols
- Damage: 8/shot (alternating guns), effective RPM 700
- Clip: 24 (12 per gun) · Reload: 1.8s
- Falloff starts at 15m, floor at 40% damage past 30m
- **Ability — Blink Dash:** 6s cooldown, 8m teleport dash. Cooldown instantly refunds on a kill within 3s of landing.
- **Ultimate — Adrenal Surge:** charges on 150 damage dealt. +30% move speed, +25% fire rate for 6s.

### MARROW — Shoulder Rocket Launcher
- Damage: 90 direct / 40 splash (3m radius)
- Clip: 4 · Reload: 2.5s per rocket (3.2s full reload)
- Rocket-jump self-damage: 30% of direct hit value
- **Ability — Impact Jump:** consumes 1 rocket, 50% reduced self-damage vs. enemy-targeted rockets.
- **Ultimate — Barrage:** charges on 200 damage dealt. Fires 3 rockets in 1.5s, no reload between shots.

### CINDER — Plasma Thrower
- DPS: 120 within 4m, falls to 0 past 8m
- Fuel tank: 100, drains 20/s while firing, refuels over 3s idle
- Afterburn: 6 dmg/s for 4s, stacks up to 3x
- **Ability — Flare Wall:** 12s cooldown. Wall lasts 5s, slows projectiles 70%, deals 15 dmg/s to anyone crossing.
- **Ultimate — Incinerate:** charges on 30 afterburn ticks landed. 6m AoE, 150 damage over 3s, reduces enemy accuracy in the cloud.

### BREACH — Grenade Launcher + Sticky Charges
- Grenade: 60 direct / 45 splash · Clip: 4 · Reload: 2.8s · 3s fuse arc
- Sticky charge: 100 damage each, max 6 planted, 1.5s arm time, manual detonation
- **Ability — Breach Jump:** self-launch off a planted sticky, 40% reduced self-damage.
- **Ultimate — Collapse:** charges on 250 sticky damage dealt. Detonates all planted charges instantly (ignores arm timer), +50% damage.

### BULWARK — Rotary Cannon (powered exo-frame)
- Damage: 10/bullet, RPM 900 after 0.8s spin-up
- Belt: 200 rounds, overheats instead of reloading (4s cooldown)
- Falloff starts at 10m · Move speed -50% while firing
- **Ability — Kinetic Shield:** 14s cooldown. Absorbs 300 damage or 5s duration, frontal 90° arc only.
- **Ultimate — Overdrive:** charges on 250 damage dealt or received. +50% fire rate, +20% damage, movement locked, 6s duration.

### FORGE — Light SMG + Constructor Tool
- SMG: 6/shot, RPM 600, clip 30, reload 2s (personal defense only)
- Sentry turret: 150 HP, 15 dmg/shot, RPM 300, 20m range, 4s build time (interruptible), 1 active max
- Repair tool: 40 HP/s to structures
- **Ability — Deploy Barrier:** 15s cooldown, 200 HP wall.
- **Ultimate — Fortify:** charges on 200 structure damage absorbed. All active structures get +100% HP and damage for 8s.

### SUTURE — Nanite Injector + Stim Sidearm
- Injector beam: 60 HP/s to target (line of sight required), 30 HP/s self-heal
- Sidearm: 7 dmg/shot, RPM 300, clip 8, reload 1.6s
- **Ability — Overcharge Stim:** 10s cooldown. +50% damage resistance for an ally for 4s, followed by -20% healing received for 3s (crash penalty).
- **Ultimate — Mass Revive Pulse:** charges on 300 total healing done. 8m radius, instantly restores all critical (<20% HP) allies to 50% HP.

### REVEILLE — Charge Rifle
- Uncharged: 40 damage, fires every 0.6s
- Fully charged (1.5s hold): 150 damage
- Clip: 5 · Reload: 2.2s
- **Ability — Recon Dart:** 15s cooldown. Throwable, reveals enemies through walls in a 10m radius for 4s.
- **Ultimate — Railshot:** charges on 3 fully-charged hits landed. Next shot penetrates up to 2 targets/cover, guaranteed 200 damage.

### WRAITH — Silenced Sidearm + EMP Blade
- Sidearm: 22 dmg/shot, RPM 300, clip 12, reload 1.6s, no falloff
- EMP blade: 35 dmg normal, 150 dmg execute (target <50% HP or unaware from behind)
- **Ability — Active Camo:** 12s cooldown, cloak breaks on firing, +15% speed while cloaked, holographic decoy on separate 20s cooldown (draws aggro for 6s).
- **Ultimate — Ghost Protocol:** charges on 2 successful executes. Next melee hit within the window is a guaranteed execute regardless of target HP, +20% speed, silent footsteps, 5s duration.

---

## Counter Matrix

| Character | Hard Counter | Soft Counter | Why |
|---|---|---|---|
| Vex | Bulwark | Cinder | Shield eats dash damage and the cannon punishes low HP; afterburn ignores mobility since it ticks after Vex has already moved on. |
| Marrow | Wraith | Forge | Wraith closes the gap and executes inside the rocket's long reload window; sentries punish predictable arcs. |
| Cinder | Reveille | Suture | Charge rifle kills before the plasma thrower's short range becomes relevant; healing outpaces afterburn ticks. |
| Breach | Vex | Bulwark | Slow arcing projectiles struggle to hit erratic movement; Bulwark's HP pool outlasts a sticky burst. |
| Bulwark | Breach | Wraith | Stickies bypass the shield's frontal arc; an execute ignores raw HP entirely. |
| Forge | Cinder | Marrow | Fire melts sentry HP fast without needing precise aim; splash damage clears turret placements efficiently. |
| Suture | Wraith | Reveille | Low HP and an exposed healing position get punished by both a fast execute and a one-shot from range. |
| Reveille | Wraith | Vex | Camo closes the distance a charge rifle needs; Vex's erratic movement denies clean charge-shot tracking. |
| Wraith | Forge | Bulwark | Automated turret detection beats a kit that relies on human reaction time; a tank's HP pool survives a failed execute attempt. |

---

## Quick-Reference Cooldowns

| Character | Ability CD | Ultimate Charge Condition |
|---|---|---|
| Vex | 6s | 150 damage dealt |
| Marrow | ammo-cost, no CD | 200 damage dealt |
| Cinder | 12s | 30 afterburn ticks |
| Breach | ammo-cost, no CD | 250 sticky damage |
| Bulwark | 14s | 250 dmg dealt/received |
| Forge | 15s (barrier) | 200 structure dmg absorbed |
| Suture | 10s | 300 total healing |
| Reveille | 15s | 3 fully-charged hits |
| Wraith | 12s (camo) / 20s (decoy) | 2 executes |
