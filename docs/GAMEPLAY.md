# Gameplay

The turn structure of Scrolls. This is the rules reference; `NOMENCLATURE.md` is the vocabulary
reference and `CLAUDE.md` describes how the rules below are wired into the code.

## Turns and rounds

A **turn** is one player taking all six phases in order. A **round** is both players having taken a
turn. Player 1 takes the first turn of the game, so a round runs player 1 then player 2, and the round
number advances when the turn returns to the player who started the game.

Until this existed there was no turn, phase, priority or pass: every command ran as player 1 and a
scroll could attack as often as it liked.

## The phases of a turn

### 1. Draw Phase

The player draws 1 card. The player cannot draw at any other time except for card effects.

### 2. Interlude Phase

A phase that allows card effects to resolve, mainly counters.

### 3. Rallying Phase

Players are at base camp. Players can play 1 Entity and 1 Equipment card and change the state of
1 entity.

The three allowances are independent: playing an Entity does not consume the Equipment allowance, and
neither consumes the state change.

### 4. Skirmish Phase

Every entity that a player controls can attack once unless its effects state otherwise. This phase can
be skipped.

"Unless its effects state otherwise" is the hook for per-card exceptions in both directions: an entity
that may attack twice, and an entity that may not attack at all.

### 5. Regroup Phase / Retreat Phase

Skipped if Skirmish Phase is skipped.

A **Regroup Phase** if the player succeeded in killing 1 or more entities on the other side of the
field. Players can then play 1 Equipment Line card.

A **Retreat Phase** does not allow players to play 1 Equipment Line card.

In both Regroup and Retreat Phases, a player may change the position of 1 entity based on that
entity's rules (left or right, front or rear).

No entity reaches the field outside the Rallying Phase: the Regroup reward is an Equipment card, not
another creature. Its allowance is its own, not the Rallying Phase's Equipment carried forward, so a
player who equipped at base camp may equip again on a successful skirmish.

So the phase after Skirmish takes one of three forms:

| What happened in Skirmish | Which phase follows | Play 1 Equipment | Reposition 1 entity |
|---|---|---|---|
| At least one enemy entity destroyed | Regroup | Yes | Yes |
| Attacked, destroyed nothing | Retreat | No | Yes |
| No attack made at all | Neither; the turn goes straight to the End Phase | No | No |

### 6. End Phase

Turn passes to opponent.

## State and position

The Rallying Phase's "change the state of 1 entity" and the Regroup or Retreat Phase's "change the
position of 1 entity" are the same operation, and each phase grants its own separate allowance for it.

An entity's **state** is where it stands: which of the six slots it occupies along its line, and
whether it is on the Front Line or the Rear Line.

A state change moves an entity one step:

- left or right, to the neighbouring slot on the same line, or
- front to rear, or rear to front, in the **same column**.

It is legal only when all of the following hold:

- the destination is empty. An entity cannot move to the rear line if a card is already in the
  corresponding rear slot;
- the card itself may occupy the destination line. An entity cannot move to the rear line if it cannot
  be placed in the rear line according to the card itself. A card whose field position is
  "Front Line/Rear Line" may move between the two; one printed for a single line may not leave it;
- the entity is not Equipment. The Equipment Line does not take part: it is never a target, never
  attacks, and does not reposition.

Two entities never trade places. Swapping is a card effect, which is what Mounted Cannon's `Cannoneer`
describes: "Swap the Mounted Cannon with the Being in front of it."

## Vocabulary already printed on the cards

These phase names are not new. The card pool in `aztec_tcg_cards.csv` and `prehistoric_tcg_cards.csv`
already refers to them, which is the evidence that the structure above is the original design rather
than a later invention:

| Card | Text | What it shows |
|---|---|---|
| Mounted Cannon `[A]` | "You may not change the position of Mounted Cannon in the Regroup Phase." | The Regroup Phase, and a per-card denial of its reposition |
| Master Seal `[A]` | "Armor replenishes for every monster every interlude phase (after the draw phase)." | The Interlude Phase, and its position immediately after the Draw Phase |
| Engineering `[A]` | "Every draw stage, the equipped Entity recovers 50 Life and 50 Armor." | The Draw Phase, under the name draw stage |
| Great Castle `[A]` | "Only one Front Line and one Rear Line Entity can attack per turn." | A per-card narrowing of the Skirmish allowance |
| Embryo `[A]` | "Every turn Embryo's HP increases by 10 ... Only one discard is needed per turn" | Per-turn effects, which need a turn boundary to reset against |
| Return to Battle `[P]` | "If you have a face-down Resurrection card and it is your War Phase" | **War Phase**, which reads as an older name for the Skirmish Phase |
| Blinding Blizzard `[P]` | "void the attack and end their War Phase" | The same name, and a card that ends a phase early |

The Prehistoric pool says War Phase where the Aztec pool and the rules above say Skirmish Phase.
Nothing in either pool uses both names, so they are treated here as the same phase under two names,
with Skirmish as the current one.

## What this version does not model

Stated plainly so the gaps are not mistaken for bugs:

- **No card effects.** The Interlude Phase has nothing to resolve, so it passes by itself. It exists
  in the sequence because counters resolve there once effects are implemented, and because Master Seal
  already prints a trigger for it.
- **No hidden information.** One console shows both hands, so hot seat play relies on the players not
  looking. There is no face-down state, which several Prehistoric cards assume.
- **No win condition.** Running a deck out logs a message and nothing more. The Element Grounds slot
  is still never populated.
- **No per-card overrides.** Every allowance above is the same for every card. The card text quoted in
  the table above describes exceptions that are recorded but not implemented.
- **Attacks target entities only.** There is no direct attack on a player, so "If this monster attacks
  your opponent directly" has nothing to resolve against.

## Open rules questions

Recorded rather than silently decided.

1. **Does the player taking the first turn draw on it?** Many trading card games have the player going
   first skip their first draw, to offset the advantage of acting first. Nothing dictated says so here,
   so it is implemented as a normal draw: every turn including the first begins with one.
2. **Is the Rallying Phase state change limited to one step, as the Regroup reposition is?** The
   Rallying allowance is described as changing an entity's state and the Regroup allowance as changing
   its position, with the one step wording attached to the latter. Both are implemented through the
   same one step rule. If the Rallying change was meant to be unrestricted travel to any legal empty
   cell, it is one condition to relax.
3. **Can the Skirmish Phase be declared skipped after attacking?** Implemented as no: a phase in which
   an attack was made is not skipped, so a Regroup or Retreat always follows it.
