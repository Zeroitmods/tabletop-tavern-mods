# Zero - Más oro, tope de intereses 30 (More Gold)

Mod para **Tabletop Tavern** (Steam 3337380). Autor: ZeroitMods. Versión 1.2.1.

## Español

### Qué hace

Sube el tope de interés del banco a 30 de oro por turno (de serie, 5). Es un mod JSON del juego: funciona también sin el núcleo si se copia a mano en la carpeta Mods. Viene **activado**.

**Textos y datos hechos con ayuda de IA generativa.** Los textos y los datos de este módulo se hicieron con ayuda de herramientas de IA generativa.

### Requisitos

- Ninguno si se instala a mano en la carpeta `Mods` del juego (es un mod JSON del juego).
- Con un gestor de mods: **Zero - Núcleo** (`Zero-Nucleo`), que lo copia a la carpeta `Mods`.

### Instalación con un gestor (Thunderstore / r2modman)

1. Instala este paquete (`Zero-Economia`); el gestor instala también sus dependencias.
2. Arranca el juego: el núcleo copia el módulo a la carpeta `Mods` del juego y aparece en el menú de mods.

### Instalación a mano (también Nexus)

1. Copia la carpeta `ZeroEconomia` (dentro de `plugins\` en el zip) a
   `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.
2. Actívala en el menú de mods del juego. BepInEx y el núcleo solo hacen falta si lo instalas con un gestor.

### Compatibilidad

- Compatible con los demás módulos de Zero; cada uno se activa por separado.
- Textos del mod en los idiomas del juego (español, inglés, alemán, francés, japonés, coreano, ruso, ucraniano y chino simplificado y tradicional); lo que no esté traducido sale en inglés.

### Problemas conocidos

- Hecho para el juego 1.11.1 (build 26359415), en el que se probó el núcleo 0.7.6. Una actualización del juego puede necesitar una versión nueva del núcleo y de los módulos.
- Sin soporte en cooperativo ni versus: con el núcleo instalado y este módulo activo, el núcleo no deja abrir ni unirse a partidas en línea (por defecto; se puede cambiar con `Multijugador.Bloquear` en la configuración del núcleo, pero entonces el desajuste es responsabilidad tuya), porque el otro jugador no tendría el mismo contenido, y lo avisa en pantalla. Instalado a mano sin el núcleo, nada bloquea las partidas en línea: no lo uses en cooperativo ni versus.
- Partidas modificadas: con el núcleo instalado y este módulo activo, el núcleo no envía tus partidas a las clasificaciones online ni deja salir la telemetría del juego (los datos de partida que el juego envía al estudio). Instalado a mano sin el núcleo, nada bloquea esos envíos.

### Cómo desactivarlo

Desactívalo en el menú de mods del juego y reinicia. Sin ningún módulo activo, el núcleo no cambia nada (juego de serie).
Si lo desinstalas con el gestor, el núcleo aparta su copia a `Mods\_ZeroRetirados\` la próxima vez que arranca el juego.

### Créditos

ZeroitMods. Tabletop Tavern es de Memori Studios.

## English

### What it does

Raises the bank's interest cap to 30 gold per turn (5 by default). It is a plain JSON game mod: it also works without the core if copied by hand into the Mods folder. It ships **enabled**.

**Texts and data made with the help of generative AI.** This module's texts and data were made with the help of generative AI tools.

### Requirements

- None if installed by hand into the game's `Mods` folder (it is a plain JSON game mod).
- With a mod manager: **Zero - Nucleo** (`Zero-Nucleo`), which copies it into the `Mods` folder.

### Installing with a mod manager (Thunderstore / r2modman)

1. Install this package (`Zero-Economia`); the manager also installs its dependencies.
2. Start the game: the core copies the module into the game's `Mods` folder and it shows up in the mod menu.

### Manual install (also Nexus)

1. Copy the `ZeroEconomia` folder (inside `plugins\` in the zip) into
   `%USERPROFILE%\AppData\LocalLow\MemoriStudios\TabletopTavern\Mods\`.
2. Enable it in the game's mod menu. BepInEx and the core are only needed if you install it with a mod manager.

### Compatibility

- Works with Zero's other modules; each one is enabled separately.
- Mod texts come in the game's languages (Spanish, English, German, French, Japanese, Korean, Russian, Ukrainian and Simplified and Traditional Chinese); anything not translated shows in English.

### Known issues

- Made for game version 1.11.1 (build 26359415), on which core 0.7.6 was tested. A game update may need a new version of the core and the modules.
- Co-op and versus are not supported: with the core installed and this module enabled, the core does not let you host or join online matches (by default; `Multijugador.Bloquear` in the core's settings can turn it off, but then any mismatch is on you), because the other player would not have the same content, and it says so on screen. Installed by hand without the core, nothing blocks online matches: don't use it in co-op or versus.
- Modded runs: with the core installed and this module enabled, the core does not submit your runs to the online leaderboards and blocks the game's telemetry (the run data the game sends to the studio). Installed by hand without the core, nothing blocks them.

### How to disable it

Disable it in the game's mod menu and restart. With no module enabled, the core changes nothing (vanilla game).
If you uninstall it with the mod manager, the core moves its copy to `Mods\_ZeroRetirados\` the next time the game starts.

### Credits

ZeroitMods. Tabletop Tavern is made by Memori Studios.
