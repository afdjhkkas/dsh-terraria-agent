DshTerrariaBridge.tmod is built from the checked-in C# source in
../DshTerrariaBridge/ with the local tModLoader 2026.8.3.0 build tool.

Protocol version: this artifact and the tools in ../index.js speak bridge protocol
v0.2.0 (see ../PROTOCOL.md). A build made before 0.2.0 is not compatible: it reports
an empty tile as type 0 instead of -1, has no scan `format`, no attack result
reporting, no `paused`/`hasFocus` fields, and no id validation on
give/place/spawn_npc. Rebuild (see ../README.zh-CN.md "构建 Mod") instead of mixing
an old .tmod with the new plugin.

Rebuild it with the target tModLoader installation before distributing a release
build for another game version.

Local install: close tModLoader and Terraria first (the game keeps this file open,
and the copy fails with a sharing violation while it runs). Then run the
repository's install-mod.ps1. It:

  - rejects a truncated or renamed artifact (checks the "TMOD" magic),
  - prints which tModLoader build packed the file,
  - keeps the build that is currently installed as
    DshTerrariaBridge.previous-installed.tmod next to this file (git-ignored),
  - copies the artifact into
    %USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\
    and prints the SHA-256 of what it installed.

Roll a bad build back with:

  ./install-mod.ps1 -Artifact artifacts\DshTerrariaBridge.previous-installed.tmod -NoBackup