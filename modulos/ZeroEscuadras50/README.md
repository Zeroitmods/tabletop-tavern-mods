# Zero - Escuadras más grandes x50% (Bigger Squads x50%)

Mod para **Tabletop Tavern** (Steam 3337380). Autor: ZeroitMods. Versión 1.0.1.

## Español

### Qué hace

Todas las escuadras de 2 o más soldados llevan un 50 % más de soldados (redondeo normal), con la munición escalada igual, en los dos bandos. Viene **activado**.

**Textos y datos hechos con ayuda de IA generativa.** Los textos y los datos de este módulo se hicieron con ayuda de herramientas de IA generativa.

### Requisitos

- **Zero - Núcleo** (`Zero-Nucleo`, plugin `ZeroNucleo.dll`, versión 0.7.8 o más nueva).
- **BepInEx 6 IL2CPP** (be.788 o compatible) instalado en el juego.

### Instalación con un gestor (Thunderstore / r2modman)

1. Instala este paquete (`Zero-Escuadras_50`); el gestor instala también sus dependencias.
2. Arranca el juego: el núcleo copia el módulo a la carpeta `Mods` del juego y aparece en el menú de mods.

### Instalación a mano (también Nexus)

1. Instala BepInEx 6 IL2CPP en la carpeta del juego si no lo tienes.
2. Copia `ZeroNucleo.dll` (paquete Zero - Núcleo) a `BepInEx\plugins\` del juego.
3. Copia la carpeta `ZeroEscuadras50` (dentro de `plugins\` en el zip) a
   `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.

### Compatibilidad

- Compatible con los demás módulos de Zero; cada uno se activa por separado.
- Textos del mod en los idiomas del juego (español, inglés, alemán, francés, japonés, coreano, ruso, ucraniano y chino simplificado y tradicional); lo que no esté traducido sale en inglés.
- Si hay varios módulos de escuadras activos (+50 %, +100 %, +150 %), manda el mayor: nunca se multiplican.
- Los cambios de soldados afectan a las escuadras que se recluten o generen después; las ya guardadas conservan su tamaño.

### Problemas conocidos

- Hecho para el juego 1.11.1 (build 26359415), en el que se probó el núcleo 0.7.6. Una actualización del juego puede necesitar una versión nueva del núcleo y de los módulos.
- Sin soporte en cooperativo ni versus: con cualquier módulo de Zero activo (también este), el núcleo no deja abrir ni unirse a partidas en línea (por defecto; se puede cambiar con `Multijugador.Bloquear` en la configuración del núcleo, pero entonces el desajuste es responsabilidad tuya), porque el otro jugador no tendría el mismo contenido, y lo avisa en pantalla.
- Partidas modificadas: con cualquier módulo de Zero activo (también este), el núcleo no envía tus partidas a las clasificaciones online ni deja salir la telemetría del juego (los datos de partida que el juego envía al estudio).

### Cómo desactivarlo

Desactívalo en el menú de mods del juego y reinicia. Sin ningún módulo activo, el núcleo no cambia nada (juego de serie).
Si lo desinstalas con el gestor, el núcleo aparta su copia a `Mods\_ZeroRetirados\` la próxima vez que arranca el juego.

### Créditos

ZeroitMods. Tabletop Tavern es de Memori Studios.

## English

### What it does

Every squad with 2 or more soldiers gets 50% more soldiers (normal rounding), with ammunition scaled the same, for both sides. It ships **enabled**.

**Texts and data made with the help of generative AI.** This module's texts and data were made with the help of generative AI tools.

### Requirements

- **Zero - Nucleo** (`Zero-Nucleo`, plugin `ZeroNucleo.dll`, version 0.7.8 or newer).
- **BepInEx 6 IL2CPP** (be.788 or compatible) installed in the game.

### Installing with a mod manager (Thunderstore / r2modman)

1. Install this package (`Zero-Escuadras_50`); the manager also installs its dependencies.
2. Start the game: the core copies the module into the game's `Mods` folder and it shows up in the mod menu.

### Manual install (also Nexus)

1. Install BepInEx 6 IL2CPP into the game folder if you don't have it.
2. Copy `ZeroNucleo.dll` (Zero - Nucleo package) into the game's `BepInEx\plugins\`.
3. Copy the `ZeroEscuadras50` folder (inside `plugins\` in the zip) into
   `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.

### Compatibility

- Works with Zero's other modules; each one is enabled separately.
- Mod texts come in the game's languages (Spanish, English, German, French, Japanese, Korean, Russian, Ukrainian and Simplified and Traditional Chinese); anything not translated shows in English.
- If several squad modules are enabled (+50%, +100%, +150%), the biggest one wins: they never stack.
- Soldier changes apply to squads recruited or generated afterwards; saved squads keep their size.

### Known issues

- Made for game version 1.11.1 (build 26359415), on which core 0.7.6 was tested. A game update may need a new version of the core and the modules.
- Co-op and versus are not supported: with any of Zero's modules enabled (this one included), the core does not let you host or join online matches (by default; `Multijugador.Bloquear` in the core's settings can turn it off, but then any mismatch is on you), because the other player would not have the same content, and it says so on screen.
- Modded runs: with any of Zero's modules enabled (this one included), the core does not submit your runs to the online leaderboards and blocks the game's telemetry (the run data the game sends to the studio).

### How to disable it

Disable it in the game's mod menu and restart. With no module enabled, the core changes nothing (vanilla game).
If you uninstall it with the mod manager, the core moves its copy to `Mods\_ZeroRetirados\` the next time the game starts.

### Credits

ZeroitMods. Tabletop Tavern is made by Memori Studios.
