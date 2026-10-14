# Elixir System

## Purpose

Introduce a shared Elixir resource system for Card usage.

Elixir is used by both Tower Cards and Skill Cards.

For the current prototype:

- Tower Card costs are supported but set to `0`.
- Skill Cards use configurable non-zero costs.

## 1. Core Values

Use:

```text
Minimum Elixir: 0
Starting Elixir: 3
Maximum Elixir: 10
Regeneration Rate: 0.36 Elixir/second
```

Store runtime Elixir as a floating-point value.

Always clamp:

```text
0 <= CurrentElixir <= 10
```

Elixir must never become negative.

## 2. Regeneration

While gameplay is in the active Playing state:

```text
CurrentElixir += RegenRate * deltaTime
```

Clamp to Maximum Elixir.

Regeneration must stop during:

- Pause
- Game Over
- Other non-Playing states

Pause must not allow Elixir farming.

## 3. Shared Card Cost

Elixir cost belongs to the common Card abstraction:

```text
CardDefinition
    -> ElixirCost
```

Current prototype:

```text
Tower Cards:
ElixirCost = 0

Fireball:
configurable non-zero cost

Arrow Rain:
configurable non-zero cost
```

Do not hard-code cost handling separately for Tower and Skill Cards.

## 4. Spending Rules

Spend Elixir only after Card use has been successfully committed.

### Tower Card

Spend only when tower placement succeeds.

Failed placement:

- no Elixir spent
- no card cycle

### Skill Card

Spend only when the cast is valid and successfully committed.

Cancelled/rejected cast:

- no Elixir spent
- no card cycle

Expose a clean API such as:

```text
CanAfford(cost)
TrySpend(cost)
```

Exact naming may differ.

## 5. Insufficient Elixir

If `CurrentElixir < Card.ElixirCost`:

- Reject the action.
- Spend no Elixir.
- Do not cycle the card.
- Keep the card in Hand.
- Display:

`Not enough Elixir.`

The message should:

- Remain visible for approximately 1.5 seconds.
- Fade out automatically.
- Be reusable for both Tower and Skill Cards.

## 6. Elixir Bar UI

Add an Elixir Bar in the empty right-side area of the gameplay UI.

The bar represents:

```text
CurrentElixir / MaximumElixir
```

The fill should rise smoothly like liquid.

For the prototype:

- Use a smooth vertical fill animation.
- Advanced liquid shaders/waves/refraction are not required.
- Final VFX are not required.

Separate gameplay state from visual interpolation:

```text
CurrentElixir
    -> authoritative gameplay value

DisplayedElixirFill
    -> smoothly approaches CurrentElixir / MaximumElixir
```

Do not delay or alter logical Elixir values for animation.

Optional numeric display:

```text
3.0 / 10
```

## 7. Retry and Stage Start

Every fresh stage run, including Retry:

```text
CurrentElixir = 3
```

Do not carry Elixir over from the failed run.

## 8. Pause Integration

On Pause:

- Elixir value stays unchanged.
- Regeneration stops.

On Resume:

- Regeneration continues from the same value.

## 9. Architecture

Recommended separation:

```text
ElixirSystem
    -> current value
    -> regeneration
    -> affordability
    -> spending

ElixirBarUI
    -> presentation only

CardDefinition
    -> cost data

TowerPlacement / SkillCasting
    -> request validation/spending
```

Do not let Elixir UI own the resource state.

## 10. Scope Exclusions

Do not implement yet:

- Double-Elixir phases
- Bonus pickups
- Elixir-generation towers
- Dynamic regen modifiers
- Final liquid shader
- Persistent Elixir between stages

## 11. Verification

Verify at minimum:

1. Stage starts with 3 Elixir.
2. Regen is 0.36/sec.
3. Elixir never exceeds 10.
4. Elixir never drops below 0.
5. Pause stops regen.
6. Resume continues regen.
7. Game Over stops regen.
8. Tower Card cost is supported even when 0.
9. Skill Card cost is enforced.
10. Insufficient Elixir rejects usage.
11. Warning fades after about 1.5 seconds.
12. Successful use spends Elixir once.
13. Failed/cancelled use spends none.
14. Retry resets Elixir to 3.
15. Elixir Bar tracks state smoothly.

Report files changed, Elixir data flow, spending API, UI implementation, tests performed, and unverified behavior.

## Translation note

The English messages above translate the originally specified Korean UI messages. This document translation does not request a change to the runtime UI language. Decode the Unicode escapes below to recover the exact original text.

- English meaning: `Not enough Elixir.` Original required UI text (Unicode-escaped): `\uc5d8\ub9ad\uc11c\uac00 \ubd80\uc871\ud569\ub2c8\ub2e4`.
