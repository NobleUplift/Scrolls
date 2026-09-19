# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Scrolls` is an unfinished console-based trading card game prototype: a .NET Framework 3.5 C# console
app (VS2008 project, upgraded to VS2010 in 2011) that renders a two-player card battlefield with
box-drawing characters and reads card definitions from a SQL Server Compact 3.5 database. Original
work ran to about 2012; the renderer was rewritten in 2026. There is a git repository whose history
was reconstructed to match the original development, but no README, no tests, and no lint config.

## Build and run

### Prerequisite: SQL Server Compact 3.5 SP2

The csproj references `System.Data.SqlServerCe, Version=3.5.0.0` with **no `HintPath`**, so it
resolves only from the GAC. Without SQL CE 3.5 SP2 installed, the build fails with `CS0234` on the
`using System.Data.SqlServerCe;` at `Objects.cs:12` and `CS0246` on the `SqlCeConnection` field, and
nothing in the project compiles.

`SSCERuntime-ENU.exe` sits in the repo root on this machine, but `.gitignore:119` excludes it by name,
so a clone will not have it. Keep a copy somewhere alongside the repo; the download is no longer
straightforward to find.

### Building

```powershell
& "C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" Scrolls.sln /p:Configuration=Debug /p:Platform=x86
```

Visual Studio Community 2022 and the .NET SDK are also installed, but the in-box .NET Framework
MSBuild is the shortest path. Build **x86**, not `AnyCPU`: the project sets `PlatformTarget` x86 under
the x86 configurations, and SQL CE loads bitness-specific native DLLs (`sqlce*35.dll`), so an AnyCPU
build on 64-bit Windows can load a provider it cannot then initialise.

One warning remains and is harmless: MSB3245 for the `Microsoft.SqlServerCe.Client` reference, whose
`HintPath` points at a Visual Studio 9.0 directory that no longer exists. Nothing in the project uses
that assembly.

Redirect build output with `/p:OutputPath=<scratchpad> /p:BaseIntermediateOutputPath=<scratchpad>`
when you only want to check compilation, so the committed 2010/2012/2014 binaries in `bin\` are left
alone.

Running the app needs a real interactive console. It no longer calls `Console.SetWindowSize`, but it
does use `Console.KeyAvailable` and cursor positioning, which are meaningless under redirected output.
Ask the user to run it.

### Testing

**There is no test project.** The automated verification is a render harness, `tools\RenderHarness.cs`,
run by `tools\run-harness.ps1`, which builds into a scratch directory so the committed binaries under
`Scrolls\bin\` are never touched. **Run it after any change to the renderer or the rules**, and extend
it rather than leaving it behind: it covers deck loading, drawing, placing, the hand cap, deck
exhaustion and row widths at six terminal sizes, and it will not compile at all if a member it uses
has been removed.

A throwaway harness for one feature is still worth writing, and the recipe is the same. It works like
this:

1. Compile a separate exe referencing the built `Scrolls.exe`:
   ```powershell
   & "C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\csc.exe" /target:exe /platform:x86 `
       /out:<scratch>\Harness.exe /r:<scratch>\Scrolls.exe <scratch>\Harness.cs
   ```
2. In `Main`, **before touching `System.Console`**, P/Invoke `FreeConsole`, then `AllocConsole`, then
   rebind the standard handles:

   ```csharp
   IntPtr h = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE,
                         FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
   SetStdHandle(STD_OUTPUT_HANDLE, h);   // and STD_ERROR_HANDLE; CONIN$ for STD_INPUT_HANDLE
   ```

   **`AllocConsole` on its own is not enough**, and it fails silently. When the parent process hands
   the harness pipes, `AllocConsole` leaves the standard handles pointing at them, so
   `Console.WindowWidth` throws `IOException: The handle is invalid` and `Screen.GetConsoleSize`
   quietly falls back to its hardcoded 80x25 for *every* size asked for. The harness then reports
   passes for layouts it never actually rendered. An allocated console is classic conhost, which
   honours `Console.SetWindowSize` where Windows Terminal ignores it; that is what makes arbitrary
   sizes testable, but only once the handles point at it.
3. Resize in the right order. The window can never exceed the buffer, so shrink the window first and
   grow the buffer first: `SetWindowSize(min(w, cur), min(h, cur))`, then `SetBufferSize(w, h)`, then
   `SetWindowSize(w, h)`. Then call `Screen.EnsureSize()`.
4. Drive `Objects.Field`, `Deck`, `Commands.Turn`, `PlayerCommands` and `Board.Selection` directly,
   then call `BasicBoard.Render`.

   **`Turn.Begin` first, or nothing is legal.** `CanPlace`, `CanAttackFrom` and `CanMoveFrom` all
   refuse while `Turn.Running` is false, so a harness that seeds a board and never begins a turn sees
   every rule return false and reads that as a pass. `new Field(0, 0)` between tests is a clean board,
   and `Turn.Begin` resets the turn state, so the two together isolate one case from the next.

   The `Scroll` constructor is public, so synthetic cards (`new Scroll(id, line, ...)` with an
   attack string of `"1:Hit:100"`) test the rules without touching the database at all. Use `Deck`
   only when the point is the loading itself.
5. Read `Board.Screen`'s private static `back` buffer by reflection and write the frame to a **file**.
   Do not print it: stdout goes to the allocated console, not to the captured pipe.

Assert that every row is exactly `Screen.Width` wide. A row of the wrong width is the signature of the
shearing bug this renderer exists to fix, so that single check catches most layout regressions. Run it
at 80x25 (compact layout), something tall like 100x50 (full layout), and below the minimum to confirm
the "too small" notice. **Treat a size the console refused to give you as a failure, not a skip** —
that is exactly how the silent 80x25 fallback hides behind a green run.

Drive the cursor through `Selection.Move` followed by `Selection.Revalidate`, which is the pair
`Program.CursorKey` performs per keystroke. Calling `Confirm` without ever calling `Move` exercises
none of the navigation, and that is where the bugs are.

## Architecture

Five namespaces, and every file redundantly `using`s all five:

- `Scrolls` (`Program.cs`) - entry point and the input loop
- `Objects` (`Objects.cs`) - `Field` (global game state), `Deck` (database loader), `Scroll` (a card)
- `Board` (`Screen.cs`, `Board.cs`, `Selection.cs`) - `Screen` (the frame buffer), `BasicBoard`
  (layout and drawing), `MessageLog` (scrollback), `Selection` (the modal cursor)
- `Commands` (`Commands.cs`, `Turn.cs`) - `SystemCommands` (automated sequences), `PlayerCommands`
  (verbs) and `Turn` (the phase machine)
- `ArtificialIntelligence` - an empty placeholder class

`Board` spans three files: `Screen.cs` knows about characters and the terminal, `Board.cs` knows about
cards and layout, and `Selection.cs` knows where the cursor is but no game rules. Nothing outside
`Screen.cs` may call `Console.Write`, or the buffers fall out of step with what the terminal is
actually showing.

`Selection` calls into `Commands` for the rules and `Commands` calls into `Board` for `MessageLog`,
so the two namespaces reference each other. That is legal within one assembly and every file already
`using`s all five namespaces, so it needs no plumbing.

The project file lists every source explicitly (`<Compile Include="..." />`); there is no globbing, so
a new `.cs` file that is not added there simply will not be compiled.

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
Below `BasicBoard.MinWidth` x `MinHeight` (currently **67 x 23**) the renderer draws a "terminal too
small" notice instead. `MinWidth` is driven by a full eleven-card hand, which is wider than the
37-column board itself.

`BasicBoard` records the screen rectangle of every slot and hand position as it draws
(`SlotRect`, `HandRect`, `BattlefieldRect`), and `DrawSelection` reads them back to mark whatever the
cursor is on. Those rects are the only place the layout arithmetic lives; a second copy of it would
drift the moment a glyph string changed.

**Single rules are the board, double rules mean "selected".** The grid used to divide its own slots
with `║` and `═`, which left nothing heavier to mark a cursor with, so every internal separator in
`DrawField` and `DrawMiddle` was demoted to `│ ─ ┬ ┼ ┴ ├ ┤`. A marked field cell gets `║` down its
sides and `═` along its own top and bottom edges, joined to the surrounding grid with `╥` and `╨`; a
marked hand card gets `╓ ╖ ╙ ╜` around it. Adding any permanent double line to the board takes that
vocabulary back again.

The compact hand is a bare row of abbreviations with no border to promote, so a selection there is
marked with `║` in the single column of clearance either side. That is why the two-line hand this
feature seemed to need never happened, and why `MinHeight` is still 23.

Two things can be marked at once, so they are told apart by colour rather than weight: the live
cursor is `Yellow` and the choice already confirmed behind it is `DarkYellow`. `Screen.Recolour`
exists for this and is still unused; `DrawSelection` overwrites characters instead, because the mark
changes the glyphs and not only their colour.

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

#### Inspecting the databases

No extra tooling is needed once SQL CE is installed; the provider in the GAC is enough:

```powershell
Copy-Item Scrolls\BoosterPacks.sdf $scratch\   # work on a COPY, see warning below
Add-Type -AssemblyName "System.Data.SqlServerCe, Version=3.5.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91"
$c = New-Object System.Data.SqlServerCe.SqlCeConnection("Data Source=$scratch\BoosterPacks.sdf")
$c.Open()
$da = New-Object System.Data.SqlServerCe.SqlCeDataAdapter("SELECT * FROM Aztec", $c)
$t = New-Object System.Data.DataTable; $da.Fill($t) | Out-Null
$t | Format-Table -AutoSize
```

**Always query a copy, never the file in the source tree.** Opening a 3.5 `.sdf` with a newer engine
can silently upgrade its format, and these two files are the only originals.

`INFORMATION_SCHEMA.COLUMNS`, `.INDEXES`, `.KEY_COLUMN_USAGE`, `.TABLE_CONSTRAINTS`, and
`.PROVIDER_TYPES` are all available, which is enough to reconstruct full DDL if the data ever needs
exporting. Do not reach for third-party tools such as `ExportSQLCE.exe` for this.

`NOMENCLATURE.md` is the reference for game vocabulary and the full contents of both tables. It
carries two corrections against its own earlier claims, because it was first compiled by scanning raw
database pages before the provider was available.

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

`Field`'s comment defines line index 0 = Front, 1 = Rear, 2 = Equipment. `Scroll.line` (loaded from
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
and to the `switch`. The status row is no longer a third copy: it is built by
`PlayerCommands.StatusText` from the phase that is running. The acting player threaded through
`runCommand` is `Turn.Active`.

### The selection cursor

`Board.Selection` is a modal cursor in seven stages: `None` while the player is typing, then
`Hand` → `Place` for playing a scroll, `Attacker` → `Target` for attacking, and
`Mover` → `Destination` for repositioning. Bare `place`, `attack` and `move` at the prompt open the
three chains; `place <hand> <line> <slot>` still works typed, two arguments keep the original
behaviour of taking the line from the card, and `move <line> <slot> <line> <slot>` is the typed
reposition.

While a stage is running it owns the keyboard. `Program.CursorKey` handles it and the text prompt is
inert, so Enter and Escape mean one thing at a time. Arrow keys reach the input loop with a
`KeyChar` of `'\0'`, which `Char.IsControl` calls a control character, so they must be read off
`key.Key` and not the typing branch.

**The cursor only ever stands on a position the command would accept.** `Selection.LegalCells`
filters every cell through `PlayerCommands.CanPlace` / `CanAttackFrom` / `CanTarget`, so Enter never
has to refuse and a stage with no legal position is never entered at all. The rules live in
`PlayerCommands` and are asked rather than copied, so the cursor and a typed command cannot disagree.
`Selection.Revalidate` runs after every keystroke because playing a scroll shortens the hand and
destroying one empties a cell; it relocates the cursor **only** when the position it held has gone,
or moving would drag it back to the first card each time.

Up and down are not line indices. `DrawField` draws player 1's lines in reverse so both front lines
meet in the middle, so `ScreenUpStep` flips the direction depending on whose field the cursor is
standing on, which for the `Target` stage is the other player's.

### Turns, rounds and phases

`docs/GAMEPLAY.md` is the rules reference; `Commands.Turn` is the implementation. A turn is one
player's six phases (`Phase.Draw`, `Interlude`, `Rallying`, `Skirmish`, `Regroup`, `End`) and a round
is both players having taken one. The two players share one console: `Program` dispatches every verb
as `Turn.Active`, so the turn passing is the whole of the hot seat handover.

`Advance` is `Step` then `Settle`, and `Settle` calls `Enter` on each phase until one of them has
something to wait for. **It cannot cycle because `Enter` returns false for the Rallying Phase
whatever the board holds**, so any lap through the six phases stops there. Draw draws, Interlude has
nothing to resolve while no card carries an effect, and stepping off `End` is what passes the turn,
which is why there is no `PassTurn` call inside `Enter`.

`Regroup` and `Retreat` are one enum value, told apart by `Turn.IsRegroup`, which is simply whether
the turn destroyed anything. Only a Regroup allows an Equipment to be played, on an allowance of its
own rather than the Rallying Phase's carried forward; both allow a reposition. **No entity reaches
the field outside the Rallying Phase.**

**The phase gates live inside the existing predicates, not at the call sites.** `CanPlace` asks
`Turn.CanPlay` and `CanAttackFrom` asks `Turn.CanAttackNow` and the scroll's own `attacked` flag.
`Selection.LegalCells` already filters every cell through those two, so spending an allowance removes
the action from the cursor and refuses the typed command in the same instant, and neither can be
changed without the other. Adding a new restriction means adding it to the predicate, never to a verb.

`Scroll.attacked` is per instance and deliberately **not** carried by `Scroll.Copy`: copies are minted
by `Deck.Make` before play, and `Turn` clears the flag for the active player as their turn begins.

The Rallying Phase's "state change" and the Regroup or Retreat Phase's "position change" are one
operation, `PlayerCommands.Move`, with a separate allowance in each phase. A step is one slot sideways
or front to rear in the same column, into an empty cell on a line `PlaceLines` allows. `Movable` is
split out from `CanMoveFrom` so that the two can both consult `CanMoveTo` without calling each other
in a circle.

`PlayerCommands.StatusText` builds the status row from the live phase. This replaced the literal
`BasicBoard.status` assigned in `Program.Main`, which was a third hand-maintained copy of the verb
list; `allCommands`, `helpText` and the `switch` in `runCommand` are the remaining two, and still need
updating together.

### Placement and combat rules

`PlaceLines(scroll)` gives the rows a scroll may occupy, which is what `Scroll.FieldLine()` could not
express: it has to return one row, so it collapses "Either" (`line == 3`) to the front. Nothing in
the shipped `Aztec` table is line 3, so no card offers a real front-versus-rear choice yet, though
`aztec_tcg_cards.csv` gives Aztec God a `FieldPosition` of `Front Line/Rear Line`.

`CanTarget` is a per-column shield: a rear scroll is blocked only by the front scroll in **its own
column**, so an empty front slot exposes whatever sits behind it even when the rest of the front line
is full. The equipment line is never a target and never attacks.

`Attack` takes the attack's power less the defender's armor off its endurance, and a defender at zero
or less goes to the void. **A vacated cell takes `new Scroll()`, never `null`** — the `Field`
constructor seeds all 36 cells with placeholder scrolls whose `id` is 0, and that is what the
occupancy tests read. Endurance is mutated in place, which is safe because every scroll on the field
is a distinct object from `Scroll.Copy`.

`Scroll.AttackName` / `AttackPower` / `PrimaryAttack` parse the `<slot>:<Name>:<power>` attack
strings, which the database has always carried and nothing read until now. They parse on demand and
**never write back into `attacks`**: `Copy` hands that array to every duplicate of a card by
reference, so caching into it would alter every other copy in play. The second attack of every
shipped card has no `:power` suffix, and an empty column arrives as a one-element array holding `""`.

Command output goes to `MessageLog`, never to `Console.WriteLine`, otherwise it is overwritten by the
next frame.

## Deleted history

Three things `NOMENCLATURE.md` and older notes refer to are no longer in the working tree, with
different consequences:

- `Scrolls\Sandbox.txt` - scratch fragments of the earlier hardcoded board renderer, referencing
  `Field` members that no longer exist (`player1frontLine1`, `player2scrollsInHand`, ...). **Recoverable**:
  committed in `d5ec8be`, so `git show d5ec8be:Scrolls/Sandbox.txt` still has it.
- `Backup\` and `Scrolls\2010-07-19 Code\` - the VS2008 snapshot and the pre-rewrite snapshot.
  **Not recoverable**: never committed. The older one-class-per-deck design they held does survive
  separately as `Scrolls/Decks.cs`, added in `fa5c874` and removed by `1353043` when the parameterized
  `Deck` class replaced it.

Documentation that cites `2010-07-19 Code\` as a source (notably `NOMENCLATURE.md`) is therefore
citing something that can no longer be re-checked.

## Conventions in this codebase

- Source files are UTF-8 **with BOM**, and `Board.cs` depends on it: the box-drawing characters are
  non-ASCII, and the C# compiler needs the BOM to read them as UTF-8. Preserve it when editing.
- **Every `.cs` file uses CRLF line endings.** There is no `.gitattributes` and `core.autocrlf` is
  `false`, so git stores bytes verbatim: a file written with bare LF differs from its committed form
  on *every* line, which turns its diff into a whole-file replacement and hides the real change.

  Tools that rewrite a file wholesale tend to emit LF and strip the BOM. After any such rewrite,
  restore both before building or committing:

  ```powershell
  $enc = New-Object System.Text.UTF8Encoding($true)   # $true = emit BOM
  $t = [System.IO.File]::ReadAllText($path)
  $t = $t.Replace("`r`n","`n").Replace("`n","`r`n")   # normalise first, or CRLF becomes CR CR LF
  [System.IO.File]::WriteAllText($path, $t, $enc)
  ```

  To check the whole tree at a glance, count bytes: any `0x0A` not preceded by `0x0D` is a bare LF.
  Targeted `Edit` calls preserve encoding and endings; only full-file writes need this.
- Files are indented with tabs, though the later 2012-era edits in `Board.cs` and `Program.cs` mixed
  in spaces. Match the surrounding block.
- Numeric game state uses `short` deliberately (counts, indices, loop counters), with explicit
  `(short)` casts on arithmetic.
- Doc comments use the `/** ... */` Javadoc style with `@param` / `@return`, not C# XML doc comments.
- Some files end with a `CHANGELOG` comment block of dated one-line entries. If you change a file that
  has one, add an entry in the same format.
