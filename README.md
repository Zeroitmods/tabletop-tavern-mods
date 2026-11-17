# Tabletop Tavern - ZeroitMods

Código fuente de los mods de ZeroitMods para **Tabletop Tavern** (Memori Studios, Steam 3337380).
Source code of ZeroitMods' mods for **Tabletop Tavern** (Memori Studios, Steam 3337380).

[Español](#español) · [English](#english)

---

## Español

### Qué es

Este repositorio tiene el código de **Zero - Núcleo** (`ZeroNucleo.dll`), un plugin de BepInEx 6 IL2CPP, y los datos de los módulos que funcionan con él. El núcleo por sí solo no cambia nada: lee los módulos activos en el menú **Mods** del juego y aplica lo que declara cada uno en su `zero_modulo.json`. Sin ningún módulo activo, el juego queda de serie.

Además, mientras haya algún módulo activo, el núcleo:

- guarda aparte lo que el juego de serie no conoce (unidades, objetos, héroes y hechizos nuevos), para que las partidas carguen también sin los mods;
- no envía las partidas a las clasificaciones en línea ni deja salir la telemetría del juego;
- filtra los logros de Steam para que solo cuente el contenido de serie;
- bloquea el cooperativo y el versus, porque el otro jugador no tendría el mismo contenido.

Los comentarios del código están en español, con su traducción al inglés justo debajo. Los mensajes del registro (log) y de la configuración están en español.

### Mods incluidos

| Mod | Carpeta | Versión | Página |
|---|---|---|---|
| Zero - Núcleo (Core) | `nucleo/` | 0.7.9 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/12) |
| Zero - Escuadras más grandes x50% / x100% / x150% (Bigger Squads) | `modulos/ZeroEscuadras50`, `ZeroEscuadras100`, `ZeroEscuadras150` | 1.0.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/13) |
| Zero - Munición x2 / x3 / x5 (Ammo) | `modulos/ZeroMunicion2`, `ZeroMunicion`, `ZeroMunicion5` | 1.0.0 / 1.0.1 / 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/14) |
| Zero - Mejores estadísticas a distancia (Better Ranged Stats) | `modulos/ZeroStatsDistancia` | 1.0.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/15) |
| Zero - Indesmoralizables (Unbreakable) | `modulos/ZeroImperterritos` | 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/16) |
| Zero - Más iconos (More Banner Icons) | `modulos/ZeroHeraldica` | 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/17) |
| Zero - Más oro, tope de intereses 30 (More Gold) | `modulos/ZeroEconomia` | 1.2.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/18) |

Cada módulo lleva su propio `README.md` con lo que hace, cómo se instala y sus problemas conocidos.

Otros mods de ZeroitMods para el juego (las facciones nuevas y «Más unidades») no están en este repositorio, porque llevan modelos 3D, sonidos y datos del juego que no se pueden repartir aquí. El núcleo sí contiene el código que les da soporte, porque es el mismo plugin.

### Requisitos

- **Tabletop Tavern** para Windows, versión **1.11.1** (build 26359415). Una actualización del juego puede necesitar una versión nueva del núcleo.
- **BepInEx 6.0.0-be.788 IL2CPP (win-x64)**: [descarga oficial](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip). Los paquetes de BepInEx 5 (Mono) no cargan este plugin.
- Para compilar: **.NET SDK 8** o más nuevo (el plugin se compila para `net6.0` y las pruebas para `net8.0`).

### Cómo compilar

1. Instala BepInEx be.788 en la carpeta del juego y arranca el juego una vez: BepInEx crea `BepInEx\interop`, con los ensamblados del juego contra los que se compila. Esos ensamblados no se reparten en este repositorio.
2. Compila indicando la carpeta del juego:

   ```
   dotnet build nucleo/ZeroNucleo.csproj -c Release -p:GameDir="D:\Games\Steam\steamapps\common\TabletopTavern"
   ```

   También puedes poner la carpeta en la variable de entorno `TABLETOP_TAVERN_DIR`. Si no das ninguna, se usa la ruta de Steam por defecto (`C:\Program Files (x86)\Steam\steamapps\common\TabletopTavern`).
3. La DLL queda en `nucleo/bin/Release/net6.0/ZeroNucleo.dll`. Si añades `-p:Instalar=true`, se copia además a `BepInEx\plugins` del juego (con el juego cerrado).

Pruebas fuera del juego (no usan tipos del juego):

```
dotnet run --project nucleo/pruebas/PruebasSaneado.csproj -c Release -- <carpeta con una copia de campaignSaveData.json y playerSaveData.json> [carpeta de módulos]
```

Usa una **copia** de tus partidas (están en `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\`). Algunas pruebas necesitan cosas que no están en este repositorio y fallarán: los WAV de las facciones, una copia de partidas antigua concreta y el juego instalado (la prueba del interop lo busca en la ruta de Steam por defecto; la de los ficheros del juego, también en la carpeta de la variable de entorno `TABLETOP_TAVERN_JUEGO`). Las demás deben pasar todas, y algunas dependen de que tu partida tenga al menos 3 escuadras.

### Cómo instalar

Lo más fácil es bajar los paquetes de Nexus Mods (tabla de arriba). A mano:

1. Instala BepInEx be.788 en la carpeta del juego y arranca el juego una vez.
2. Copia `ZeroNucleo.dll` a `<carpeta del juego>\BepInEx\plugins\`.
3. Copia la carpeta de cada módulo que quieras (por ejemplo `modulos/ZeroEconomia`) a `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.
4. Actívalos o desactívalos en el menú **Mods** del juego y reinicia el juego.

### Aviso de IA

Este proyecto se ha hecho con ayuda de herramientas de IA generativa (código, textos, traducciones e iconos).

### Créditos

- ZeroitMods.
- Tabletop Tavern es de Memori Studios. Mod no oficial, sin relación con el estudio.
- BepInEx, HarmonyX e Il2CppInterop, del equipo de BepInEx.
- El núcleo da soporte a dos facciones hechas por fans que no están en este repositorio. Algunos identificadores y textos del juego dentro del código nombran sus unidades y personajes; esos nombres son marcas de sus dueños, y este proyecto no tiene relación con ellos. En los comentarios se llaman «facción A», «facción B» y «héroes 901 a 904».

### Licencia

[MIT](LICENSE): puedes usar, cambiar y repartir el código y los datos de este repositorio, también en tus propios mods, siempre que mantengas el aviso de copyright («Copyright (c) 2026 ZeroitMods») y el texto de la licencia.

---

## English

### What it is

This repository holds the code of **Zero - Core** (`ZeroNucleo.dll`), a BepInEx 6 IL2CPP plugin, and the data of the modules that run on it. The core changes nothing on its own: it reads the modules enabled in the game's **Mods** menu and applies what each one declares in its `zero_modulo.json`. With no module enabled, the game stays vanilla.

While any module is enabled, the core also:

- stores separately whatever the base game does not know (new units, gear, heroes and spells), so runs still load without the mods;
- does not submit runs to the online leaderboards and blocks the game's telemetry;
- filters Steam achievements so that only vanilla content counts;
- blocks co-op and versus, because the other player would not have the same content.

Code comments are in Spanish, with their English translation right below. Log and configuration messages are in Spanish.

### Included mods

| Mod | Folder | Version | Page |
|---|---|---|---|
| Zero - Core (Núcleo) | `nucleo/` | 0.7.9 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/12) |
| Zero - Bigger Squads x50% / x100% / x150% | `modulos/ZeroEscuadras50`, `ZeroEscuadras100`, `ZeroEscuadras150` | 1.0.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/13) |
| Zero - Ammo x2 / x3 / x5 | `modulos/ZeroMunicion2`, `ZeroMunicion`, `ZeroMunicion5` | 1.0.0 / 1.0.1 / 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/14) |
| Zero - Better Ranged Stats | `modulos/ZeroStatsDistancia` | 1.0.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/15) |
| Zero - Unbreakable | `modulos/ZeroImperterritos` | 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/16) |
| Zero - More Banner Icons | `modulos/ZeroHeraldica` | 1.0.0 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/17) |
| Zero - More Gold (interest cap 30) | `modulos/ZeroEconomia` | 1.2.1 | [Nexus Mods](https://www.nexusmods.com/tabletoptavern/mods/18) |

Each module has its own `README.md` with what it does, how to install it and its known issues.

ZeroitMods' other mods for the game (the new factions and "More Units") are not in this repository, because they ship 3D models, sounds and game data that cannot be shared here. The core does contain the code that supports them, because it is the same plugin.

### Requirements

- **Tabletop Tavern** for Windows, version **1.11.1** (build 26359415). A game update may need a new version of the core.
- **BepInEx 6.0.0-be.788 IL2CPP (win-x64)**: [official download](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip). BepInEx 5 (Mono) packs do not load this plugin.
- To build: **.NET SDK 8** or newer (the plugin targets `net6.0` and the tests `net8.0`).

### How to build

1. Install BepInEx be.788 into the game folder and start the game once: BepInEx creates `BepInEx\interop`, with the game assemblies the plugin is built against. Those assemblies are not shared in this repository.
2. Build, pointing to the game folder:

   ```
   dotnet build nucleo/ZeroNucleo.csproj -c Release -p:GameDir="D:\Games\Steam\steamapps\common\TabletopTavern"
   ```

   You can also set the folder in the `TABLETOP_TAVERN_DIR` environment variable. If you give none, the default Steam path is used (`C:\Program Files (x86)\Steam\steamapps\common\TabletopTavern`).
3. The DLL ends up in `nucleo/bin/Release/net6.0/ZeroNucleo.dll`. If you add `-p:Instalar=true`, it is also copied into the game's `BepInEx\plugins` (with the game closed).

Tests outside the game (they use no game types):

```
dotnet run --project nucleo/pruebas/PruebasSaneado.csproj -c Release -- <folder with a copy of campaignSaveData.json and playerSaveData.json> [modules folder]
```

Use a **copy** of your saves (they live in `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\`). Some tests need things that are not in this repository and will fail: the factions' WAV files, one specific old save backup and the installed game (the interop test looks for it at the default Steam path; the game files test also looks in the folder set in the `TABLETOP_TAVERN_JUEGO` environment variable). All the others should pass, and some of them need your run to have at least 3 squads.

### How to install

The easiest way is to download the packages from Nexus Mods (table above). By hand:

1. Install BepInEx be.788 into the game folder and start the game once.
2. Copy `ZeroNucleo.dll` into `<game folder>\BepInEx\plugins\`.
3. Copy the folder of each module you want (for example `modulos/ZeroEconomia`) into `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.
4. Enable or disable them in the game's **Mods** menu and restart the game.

### AI notice

This project was made with the help of generative AI tools (code, texts, translations and icons).

### Credits

- ZeroitMods.
- Tabletop Tavern belongs to Memori Studios. Unofficial mod, not affiliated with the studio.
- BepInEx, HarmonyX and Il2CppInterop, by the BepInEx team.
- The core supports two fan-made factions that are not in this repository. Some identifiers and in-game texts inside the code name their units and characters; those names are trademarks of their owners, and this project is not affiliated with them. Comments call them "faction A", "faction B" and "heroes 901 to 904".

### License

[MIT](LICENSE): you may use, change and share the code and data in this repository, also in your own mods, as long as you keep the copyright notice ("Copyright (c) 2026 ZeroitMods") and the license text.
