# Nomenclature

## Renamed terms

The game's word versus the generic trading-card-game word.

| Generic term | This game's word | Evidence |
|---|---|---|
| Card | **Scroll** | `Scroll` class, `scrollsIn`, `scrollLists`, `scrollsInDeck` / `scrollsInHand` / `scrollsInVoid`. "Card" appears only in comments and in the deck-manifest identifier `cardId`, never as a type or field name |
| Discard pile / graveyard | **Void** | `Field.scrollsIn[player, 2]`, `player1scrollsInVoid` |
| Play area | **Field** | `Objects.Field` |
| Creature / unit | **Entity** | `Scroll.ToString()` prints the header `Entity` when `line < 3`, otherwise `Equipment` |
| Set / expansion | **Booster Pack** | `BoosterPacks.sdf`, table `Index` with column `pack` |
| HP / toughness | **Endurance** | database column |
| Deck, Hand | unchanged | |

`Scrolls\Objects.cs:185` carries a dated design note listing candidate renames for the core noun:

> `Alternative names: Bind, Cell, Unit (2012/01/28 02:29)`

## Zones

- **Deck**, **Hand**, and **Void** are the three categories indexed in `scrollsIn` and `scrollLists`.
- **Battlefield** is a single `Scroll` per player (`Field.battlefields[player]`), drawn as the
  isolated box on each end of the board's middle band.
- **Lines** are the rows of six slots. The naming drifted between versions:

| Source | Names |
|---|---|
| `Field` doc comment (current) | Front Line (0), **Rear Line** (1), Equipment Line (2) |
| `2010-07-19 Code\Objects.cs` | `frontLine`, `forwardLine`, **`rearLine`** |
| `Scroll.line` values in the database | 1 = Front Line, 2 = **Back Line**, 3 = Either, 4 = Equipment |

**Resolved: row 1 is the Rear Line.** The name moved. In 2010 `rearLine` was the *third* row, which
later became the Equipment Line, leaving row 1 briefly called the "Forward Line". Row 1 is now the
Rear Line, which matches the card data.

The database numbering is deliberately left alone: `Scroll.line` still stores 1 = Front, 2 = Back,
3 = Either, 4 = Equipment, and `Scroll.FieldLine()` converts it to a `playerLines` row index. Two
numbering schemes still exist, but there is now exactly one conversion between them rather than none.

## Scroll attributes

`id`, `line`, `nameAbb`, `typeAbb`, `name`, `stance`, `types[]`, `attacks[]`, `endurance`, `armor`,
`accuracy`, `intelligence`, `resistence` (spelled that way in both the column and the C# field),
`weakness`, `effect` (Equipment only), plus two marked "Hidden": `creator` and `date`.

`stance` is declared and initialized but never populated, and its line in `ToString` is commented
out. It is vocabulary without an implementation.

## Card data

Table `Aztec` in `BoosterPacks.sdf`. Four scrolls, all with `creator` = `PatPeter`.

| id | nameAbb | typeAbb | name | types | resistence | weakness | attacks | line |
|---|---|---|---|---|---|---|---|---|
| 10000000 | AZGD | AZGD | Aztec God | Aztec, God | Light, Warrior | Darkness, God | `1:Solar Blast:300`, `2:Aztec Unity` | 1 |
| 10000001 | AZWR | AZWR | Aztec Warrior | Aztec, Warrior | Light, Warrior | Darkness | `1:Spirit Slash:250`, `2:Sacrifice for Our God` | 1 |
| 10000002 | MTCN | ARTY | Mounted Cannon | Artillery, Warrior | Artillery | Warrior | `1:Cannon Blast:250`, `2:Cannoneer` | 2 |
| 10000003 | ELFC | CAST | Element Force | Cast | None | None | effect: `Cast` | 4 |

- **Attack string format**: comma-separated `<slot>:<Attack Name>:<power>`.
- **Correction.** This document previously said "each table holds two revisions of every row". That
  was an artefact of reading raw pages. Queried properly, `BoosterPacks.Aztec` holds **4 rows** and
  `Decks.Aztec` holds **1**. The extra copies are superseded row versions sitting in unreclaimed free
  pages, not live data. The observation that the older version omits the `:power` suffix still stands
  as evidence that power was added later; it just comes from deleted rows rather than live ones.
- **Type vocabulary**: Aztec, God, Warrior, Artillery, Cast, Light, Darkness (written `Dark` in
  `Decks.sdf`), None. Resistances and weaknesses are expressed with these same type names, so there
  is no separate element system.
- **Attack names**: Solar Blast, Aztec Unity, Spirit Slash, Sacrifice for Our God, Cannon Blast,
  Cannoneer.
- `Decks.sdf` gives Aztec God a `typeAbb` of `AGOD` while `BoosterPacks.sdf` gives it `AZGD`. The two
  databases disagree.
- The older Aztec God row in `BoosterPacks.sdf` has the placeholder word `penis` in its `effect`
  field. It is leftover test data in the superseded revision.

## Deck and faction names

- **Aztec** is the table name in both databases and was a class in `2010-07-19 Code\Decks.cs`.
- **Celtic** was the second deck class there, left empty. Decks and sets are named after cultures.
- `Decks.sdf`'s `Aztec` table adds one deck-building column, **quantity**.
- **Correction.** **Deck**, **Scroll**, **Information**, and **Creation** were listed here as columns.
  They are **index names**, not columns: multi-column indexes on `Decks.Aztec` that group the card
  schema into four conceptual blocks. `BoosterPacks.sdf` has no equivalent, only a primary key and a
  unique constraint on `id`. This is the author's own partitioning of a card, so it is worth
  preserving in any future schema:

  | Index | Columns | Reads as |
  |---|---|---|
  | `Deck` | id, quantity | deck membership |
  | `Scroll` | line, nameAbb, typeAbb | identity and placement |
  | `Information` | name, types, attacks, endurance, armor, accuracy, intelligence, resistence, weakness, effect | the card itself |
  | `Creation` | creator, date | provenance |
- Card ids encode the set. The Aztec cards are `1000000x`, and `Program.cs` builds a second deck from
  `2000000x` ids that have no rows in the database yet.

## Player commands

`draw`, `place`, `attack`, `help`, and `quit`, all listed in `PlayerCommands.allCommands`.
`SystemCommands.DrawHand` is the opening draw.

`place` and `attack` with no arguments open a **cursor**, which is the normal way to use them: pick a
scroll, then pick where it goes, with the arrow keys. `place <hand position> <line> <slot>` does the
same thing typed, where the line is `front`, `rear`, `equip` (also `back`, `equipment`, `mage`) or a
number. `place <hand position> <slot>` keeps the older two-argument behaviour of taking the line from
the scroll itself via `Scroll.FieldLine()`, so Element Force (database line 4) still goes to the
Equipment Line automatically.

- **Select**, **cursor**, **stage**, and **target** are the new vocabulary. The five stages are
  `None`, `Hand`, `Place`, `Attacker`, `Target` (`Board.Selection`).
- **Shield**: a scroll on the Rear Line is shielded by the Front Line scroll **in its own column**,
  and by nothing else. An empty front slot exposes whatever is behind it.
- The Equipment Line is neither a target nor an attacker.
- **Destroyed** is what happens at zero endurance; the scroll goes to the **Void**, which until now
  was allocated and never written to.

## Fixed quantities

Deck size 40, opening hand 6, 6 slots per line, 3 lines per player, 2 players, hand cap 11.

The hand cap of 11 used to overflow the renderer, whose margin arithmetic only went up to 10 and
produced a negative margin at 11. The rewritten renderer derives the hand width from the same
constants as everything else and sets `BasicBoard.MinWidth` from a full hand, so 11 scrolls fit.

## Absent

No occurrence anywhere of: arsenal, graveyard, discard, mana, summon, library, exile, banish, token,
trap, relic, turn, phase, round, match, life, or health. There is no vocabulary yet for turn
structure or for a player's life total.

**Update.** Attacking, damage, targeting, destruction and selection were all listed here as absent
and now exist; see "Player commands" above. What is still absent, and is the next thing missing:

- **Turn structure.** `Program.currentPlayer` is still 0 and never changes, so every command runs as
  player 1 and a scroll may attack as often as it likes. There is no turn, phase, priority or pass.
- **A win condition.** Nothing ends the game but `quit`. `Field.battlefields[player]` is a single
  `Scroll` per player that is still never populated, and is the obvious candidate for the thing a
  player loses by having destroyed.
- **The rest of the stat block.** `accuracy`, `intelligence`, `resistence`, `weakness` and `types`
  are loaded and displayed but take no part in combat; damage is power less armor and nothing else.
  `resistence` and `weakness` hold comma-separated type lists and are still never split.
- **`stance`** and **`effect`** remain vocabulary without an implementation.
