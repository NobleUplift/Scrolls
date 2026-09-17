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

**There is no test project and no test runner.** The only automated verification is a render harness,
which has to be written each time it is needed; it is not committed. It works like this:

1. Compile a separate exe referencing the built `Scrolls.exe`:
   ```powershell
   & "C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\csc.exe" /target:exe /platform:x86 `
       /out:<scratch>\Harness.exe /r:<scratch>\Scrolls.exe <scratch>\Harness.cs
   ```
2. In `Main`, P/Invoke `AllocConsole` **before touching `System.Console`**. The process has no console
   when launched from a tool, and an allocated console is classic conhost, which *does* honour
   `Console.SetWindowSize` where Windows Terminal ignores it. This is what makes arbitrary sizes
   testable.
3. Drive `Objects.Field`, `Deck`, and `PlayerCommands` directly, then call `BasicBoard.Render`.
4. Read `Board.Screen`'s private static `back` buffer by reflection and write the frame to a **file**.
   Do not print it: stdout goes to the allocated console, not to the captured pipe.

Assert that every row is exactly `Screen.Width` wide. A row of the wrong width is the signature of the
shearing bug this renderer exists to fix, so that single check catches most layout regressions. Run it
at 80x25 (compact layout), something tall like 100x50 (full layout), and below the minimum to confirm
the "too small" notice.

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
Below `BasicBoard.MinWidth` x `MinHeight` (currently **67 x 23**) the renderer draws a "terminal too
small" notice instead. `MinWidth` is driven by a full eleven-card hand, which is wider than the
37-column board itself.

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
