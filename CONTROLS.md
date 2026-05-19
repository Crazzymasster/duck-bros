# Duck Bros Control System

## Sprint / Momentum System

Instead of holding a button, **sprinting is now momentum-based**:

1. **Press Left Shift** to activate sprint (drains stamina while active)
2. **Keep the momentum** - you maintain speed naturally without holding the button
3. **Press Left Shift again** to stop sprint early and preserve remaining stamina
4. **Stamina recharges** automatically when not sprinting

This gives you much better control over momentum for **attack combos and positioning**.

### Sprint Mechanics

- **Max Sprint Duration**: 2.5 seconds (configurable)
- **Stamina Drain**: Depletes while sprinting
- **Stamina Recharge**: Recovers while standing/moving normally
- **Speed Boost**: 50% faster movement while sprinting (configurable)
- **Grounded Only**: Sprint stops automatically if you jump or leave the ground

### How This Helps Combat

- Build momentum with sprint, then **release to have stable speed** for attacks
- Time your sprint to **have max velocity** for dash attacks
- Press sprint again to **stop early** if you need to adjust positioning
- No more holding sprint through entire attack sequences!

The combat system now uses **categorized attacks** inspired by Super Smash Bros for better control precision.

### Light Attacks (X Key)
Fast attacks with low knockback. Combos well but deals less damage.

- **Neutral** (no direction) → **Jab** - Quick neutral strike
- **Forward** (A or D + X) → **Forward Tilt** - Directional quick attack
- **Up** (W + X) → **Up Tilt** - Anti-air attack
- **Down** (S + X) → **Down Tilt** - Low profile attack
- **Dash Attack** (Sprint + X + direction) → Velocity-based dash attack - Use sprint to build momentum!

### Heavy Attacks (C Key)
Powerful attacks with high knockback. Slower startup but great for finishing.

- **Forward** (A or D + C) → **Forward Smash** - Strong directional attack
- **Up** (W + C) → **Up Smash** - Vertical power attack
- **Down** (S + C) → **Down Smash** - Ground pound effect

### Aerial Attacks
Both Light (X) and Heavy (C) perform aerial moves when airborne.

- **Neutral** (no direction) → **Neutral Air**
- **Forward** (forward direction + X/C) → **Forward Air**
- **Back** (backward direction + X/C) → **Back Air**
- **Up** (W + X/C) → **Up Air**
- **Down** (S + X/C) → **Down Air**

## Default Key Bindings

| Input | Action |
|-------|--------|
| **X** | Light Attack (Jabs & Tilts) |
| **C** | Heavy Attack (Smashes) |
| **V** | Special (Reserved for future) |
| **Left Shift** | Sprint / Toggle Momentum |
| **W** | Up Direction |
| **A** | Left Direction |
| **S** | Down Direction |
| **D** | Right Direction |
| **Space** | Jump |

## How to Change Keys

1. In Unity Editor, select the Player character
2. In the Inspector, find the **PlayerAttacks** component
3. Modify these fields:
   - **Light Attack Key** - Default: X
   - **Heavy Attack Key** - Default: C
   - **Special Attack Key** - Default: V

## Tips for Better Control

1. **Light attacks** are fast, use them to interrupt opponent combos or string together quick sequences
2. **Heavy attacks** take longer to start but deal massive knockback - use them to finish enemies
3. **Aerials** change based on your facing direction, giving you 5 different air options
4. **Sprint Management**:
   - Press sprint once and let momentum carry you (don't hold!)
   - Press again early to stop sprint and conserve stamina
   - Time your press-and-release for precise positioning
5. **Dash Attacks** work best when:
   - You have active sprint momentum
   - You press X + direction while moving fast
   - Great for aggressive rushdown gameplay
6. Practice the directional inputs and sprint timing for consistent combos

## Future Additions

- **Special Moves**: Assign unique character abilities (V key is reserved)
- **Grab System**: Could add separate grab button
- **Power Meter**: Could add charge mechanics to heavy attacks

## Input Simplification Philosophy

The game uses **velocity-based dash attacks** instead of complex timing windows. This means:
- ❌ Old way: Hold movement + Hold sprint + Press attack button (3 keys!)
- ✅ New way: Move and build speed, then press attack (2 keys!)

This makes the game **more accessible** and **easier to control** while keeping it skill-based (you still need proper spacing and timing).
