# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Scrolls` is an unfinished console-based trading card game prototype: a .NET Framework 3.5 C# console
app (VS2008 project, upgraded to VS2010 in 2011) that renders a two-player card battlefield with
box-drawing characters and reads card definitions from a SQL Server Compact 3.5 database. Original
work ran to about 2012; the renderer was rewritten in 2026. There is a git repository whose history
was reconstructed to match the original development, but no README, no tests, and no lint config.

## Build and run

Visual Studio Community 2022 and the .NET SDK are installed, but the in-box .NET Framework MSBuild
builds this project fine and is the shortest path:

```powershell
& "C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" Scrolls.sln /p:Configuration=Debug /p:Platform=x86
```

**The build succeeds.** SQL Server Compact 3.5 SP2 is installed, which puts `System.Data.SqlServerCe`
3.5.0.0 in the GAC, the exact version the project reference asks for. One warning remains: MSB3245 for
the `Microsoft.SqlServerCe.Client` reference, whose `HintPath` points at a Visual Studio 9.0 directory
that no longer exists. Nothing in the project uses that assembly and the warning is harmless.

Redirect build output with `/p:OutputPath=<scratchpad> /p:BaseIntermediateOutputPath=<scratchpad>`
when you only want to check compilation, so the committed 2010/2012/2014 binaries in `bin\` are left
alone.

Running the app needs a real interactive console. It no longer calls `Console.SetWindowSize`, but it
does use `Console.KeyAvailable` and cursor positioning, which are meaningless under redirected output.
Ask the user to run it.

To verify rendering without an interactive console, compile a harness that references the built
`Scrolls.exe`, calls `AllocConsole`, sizes the console with `Console.SetWindowSize`, calls
`BasicBoard.Render`, and reads `Board.Screen`'s private `back` buffer by reflection, writing the frame
to a file. An allocated console is classic conhost, which honours `SetWindowSize`, so arbitrary sizes
can be exercised this way.

## Architecture

Five namespaces, and every file redundantly `using`s all five:

- `Scrolls` (`Program.cs`) - entry point and the input loop
- `Objects` (`Objects.cs`) - `Field` (global game state), `Deck` (database loader), `Scroll` (a card)
- `Board` (`Screen.cs`, `Board.cs`) - `Screen` (the frame buffer), `BasicBoard` (layout and drawing),
  `MessageLog` (scrollback)
- `Commands` (`Commands.cs`) - `SystemCommands` (automated sequences) and `PlayerCommands` (verbs)
- `ArtificialIntelligence` - an empty placeholder class

`Board` is the one namespace spanning two files: `Screen.cs` knows about characters and the terminal,
`Board.cs` knows about cards and layout. Nothing outside `Screen.cs` may call `Console.Write`, or the
buffers fall out of step with what the terminal is actually showing.

### Global state lives on `Objects.Field`

Every field of the `Field` struct is `static`. The constructor exists only for its side effects: it
allocates and seeds the static arrays. `Program.Main` does `new Field(40, 40)` and discards the value.
Nothing else instantiates it, so `Field` is effectively a singleton with a constructor-shaped
initializer. Its arrays share an index convention documented in the source:

- `playerLines[player, line, slot]` - 2 players x 3 lines x 6 slots of `Scroll`
- `scrollsIn[player, category]` - `short` counts, category 0 = deck, 1 = hand, 2 = void
- `scrollLists[player, category]` - `ArrayList` stacks of actual `Scroll` objects, same categories
- `battlefields[player]` - one `Scroll` each

### `scrollLists` is authoritative; `scrollsIn` is derived

`scrollLists` holds the actual `Scroll` objects and is the truth. `scrollsIn` is a cache of the three
counts, recomputed from the lists by `PlayerCommands.Sync(player)`. **Any code that moves a `Scroll`
between piles must call `Sync` afterwards** rather than adjusting a count by hand. The two used to be
maintained separately and drifted apart immediately, which is why the board once showed cards being
drawn while no object moved.

### The board renderer

`BasicBoard.Render(input)` composes one frame into `Screen`'s back buffer and flushes it; `Flush`
rewrites only the cells that changed. There is no `Console.Clear` anywhere.

Widths derive from `SlotInterior` and `Slots`, not from literals. The old renderer hand-tuned every
row against an 82-column console and its field rows and hand rows disagreed by one column, which
sheared the grid. If you change a glyph string, the arithmetic still holds as long as it is built
from those constants.

The layout has two forms, chosen by available height: a full form with three rows per card
(abbreviation, type, endurance) and a compact form with one. `Screen.Put` silently drops writes
outside the buffer, so a layout that overruns a small terminal clips instead of shearing or throwing.
Below `BasicBoard.MinWidth` x `MinHeight` the renderer draws a "terminal too small" notice instead;
`MinWidth` is driven by a full hand, which is wider than the board itself.

`BasicBoard` records the screen rectangle of every slot and hand position as it draws
(`SlotRect`, `HandRect`, `BattlefieldRect`). Nothing selects cards yet, but combined with
`Screen.Recolour` this is how a cursor would highlight a slot without duplicating the layout maths.

### Card data comes from SQL CE, not from the typed DataSet

`BoosterPacks.sdf` holds card definitions in a table named `Aztec`. `Deck.Make` runs a raw
`SELECT * FROM Aztec` through `SqlCeDataReader` and constructs `Scroll` objects. Columns:
`id, line, nameAbb, typeAbb, name, types, attacks, endurance, armor, accuracy, intelligence,
resistence, weakness, effect`. `types` and `attacks` are comma-delimited strings split on read.
Note the column is spelled `resistence`, and the C# field matches; do not "fix" one without the other.

`BoosterPacksDataSet.xsd` and its generated `.Designer.cs` define a typed DataSet with **no tables**;
only the connection is configured there. It is dead weight the data-access code does not use.

Both `.sdf` files are `Content` with `CopyToOutputDirectory=PreserveNewest`, and the connection string
uses `|DataDirectory|`, so the database the app reads is the copy in the output folder, not the one in
the source tree. Editing `Scrolls\BoosterPacks.sdf` only takes effect after a build copies it.

### `Deck`

`Deck(owner, deck, capacity)` reads the `Aztec` table once into a lookup, then builds
`scrollLists[owner, 0]` from the `{cardId, quantity}` manifest up to `capacity`, and shuffles it.
Each copy of a card is a distinct `Scroll` via `Scroll.Copy`, because copies take damage
independently. Ids not present in the database are reported to `MessageLog` and skipped.

`Dispose` is implemented explicitly (`void IDisposable.Dispose()`), so it is reachable only through a
`using` block or an interface cast. The finalizer also closes the connection but reports nothing,
since it may run after the screen is gone.

The `effect` column is NULL on every row, so `Deck.Text` maps NULL to `""`; calling `ToString` on the
raw value is not safe.

### Two `line` numbering schemes coexist

`Field`'s comment defines line index 0 = Front, 1 = Forward, 2 = Equipment. `Scroll.line` (loaded from
the database) uses 1 = Front, 2 = Back, 3 = Either, 4 = Equipment, and `Scroll.ToString` branches on
`line < 3` to decide between the Entity and Equipment layout. `Scroll.FieldLine()` converts the
database numbering to a `playerLines` row index, resolving Either to the front row. Use it rather
than indexing with `Scroll.line` directly.

### Command loop

`Program.InputCommand` polls `Console.KeyAvailable` and assembles input a key at a time rather than
calling `Console.ReadLine`, which would echo and move the cursor against a renderer that owns the
screen. Polling also lets the board re-lay out when the window is resized mid-game. There is one read,
one dispatch, and one redraw per iteration.

`PlayerCommands.allCommands` and `helpText` are the single source of truth for verbs; `runCommand`
parses a line into verb plus arguments and dispatches. Adding a verb means adding it to both tables
and to the `switch`. The acting player is threaded through `runCommand` but there is no turn order
yet, so everything runs as player 1.

Command output goes to `MessageLog`, never to `Console.WriteLine`, otherwise it is overwritten by the
next frame.

## Files that are not part of the build

- `Scrolls\Sandbox.txt` - scratch fragments of the earlier hardcoded board renderer. It references
  `Field` members that no longer exist (`player1frontLine1`, `player2scrollsInHand`, ...) and is not
  valid against the current code.

The `Backup\` and `Scrolls\2010-07-19 Code\` snapshot directories have been deleted and were never
committed, so they are not recoverable from git. The older one-class-per-deck design they contained
does survive in history as `Scrolls/Decks.cs`, added in `fa5c874` and removed by `1353043` when the
parameterized `Deck` class replaced it.

## Conventions in this codebase

- Source files are UTF-8 **with BOM**, and `Board.cs` depends on it: the box-drawing characters are
  non-ASCII, and the C# compiler needs the BOM to read them as UTF-8. Preserve it when editing.
- Files are indented with tabs, though the later 2012-era edits in `Board.cs` and `Program.cs` mixed
  in spaces. Match the surrounding block.
- Numeric game state uses `short` deliberately (counts, indices, loop counters), with explicit
  `(short)` casts on arithmetic.
- Doc comments use the `/** ... */` Javadoc style with `@param` / `@return`, not C# XML doc comments.
- Some files end with a `CHANGELOG` comment block of dated one-line entries. If you change a file that
  has one, add an entry in the same format.
