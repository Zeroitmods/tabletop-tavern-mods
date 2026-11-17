# Changelog

Versiones publicadas de cada paquete, con las notas en español y en inglés.
Published versions of each package, with notes in Spanish and English.

## Zero - Núcleo (Core)

### 0.7.10 (17-11-2026)

- Adaptado a la versión 1.11.12 del juego. Con ella, el juego se quedaba colgado al arrancar (también con la 0.7.8): el aviso en pantalla de «no se puede jugar en línea con mods» salía antes de que el juego terminara de cargar. Ahora, en el arranque, ese aviso solo va al log. También se adapta a las reglas nuevas de las razas de los actos de la campaña (sin esto, el sorteo de razas de los actos infinitos con las facciones nuevas fallaba).
- Updated for game version 1.11.12. With it, the game froze at startup (also with 0.7.8): the on-screen "online play is disabled with mods" notice was shown before the game had finished loading. At startup that notice now only goes to the log. Also updated for the new campaign act race rules (without it, the endless act race draw failed with the new factions).

### 0.7.9 (17-11-2026)

- Los modelos 3D de las unidades nuevas usan la animación de su unidad base del juego. Los módulos ya no la traen: el núcleo la toma de tu instalación del juego al arrancar, la comprueba (SHA-256) y la deja junto al modelo. Si falta o una actualización del juego la cambia, esa unidad usa el modelo de su unidad base y el log lo dice. Los módulos Más unidades y los de las dos facciones nuevas 1.1.1 necesitan esta versión.
- The new units' 3D models use the animation of their base unit from the game. The modules no longer ship it: the core takes it from your own game install at startup, checks it (SHA-256) and puts it next to the model. If it is missing or a game update changes it, that unit uses its base unit's model and the log says so. The More Units and the two new factions' 1.1.1 modules need this version.

### 0.7.8 (03-11-2026)

- Estandarte: los títulos «Iconos» e «Icono del estandarte» y los nombres de los colores e iconos cambian al idioma nuevo al cambiarlo en Ajustes, sin reiniciar.
- Banner: the «Icons» and «Banner icon» titles and the names of the colours and icons switch to the new language when you change it in Settings, without restarting.

### 0.7.7 (03-11-2026)

- Estandarte: los colores de facción de las facciones A y B salen en la fila «Colores de facción» de la pestaña «Estandarte», que ahora se desplaza con la rueda del ratón. Iconos para el estandarte que traen los módulos (Heráldica y las dos facciones nuevas), con un selector en esa pestaña: el elegido se guarda en el registro del núcleo (nunca en el save), se ve en la vista previa y en las banderas de las escuadras del jugador en batalla (el mapa de campaña no pinta el estandarte). Hecha para el juego 1.11.1 (la 0.7.6 está probada en él).
- Banner: the two new factions' colours show in the "Faction colours" row of the "Banner" tab, which now scrolls with the mouse wheel. Banner icons provided by the modules (Heraldry and the two new factions), with a picker in that tab: the chosen one is stored in the core's registry (never in the save) and shows in the preview and on the player's squad flags in battle (the campaign map does not draw the banner). Made for game version 1.11.1 (0.7.6 is tested on it).

### 0.7.6 (03-11-2026)

- Arreglos de la primera prueba en el juego 1.11.1. El juego ya no se cierra al arrancar con la autoprueba activada. En la Colección, la vista 3D de los comandantes nuevos enseña su propio modelo (antes salía la figura del héroe prestado). Los hechizos propios de los comandantes van en su propia banda del grimorio, una por facción, con sus colores. Las historias largas de los comandantes se leen bajo su modelo (clave nueva). Las facciones las facciones A y B pueden salir como ejército enemigo en los tres actos de la campaña, con el mismo peso que cada facción del juego (antes, una en el acto 2 y otra en el 3). Nuevo efecto de moral para el módulo «Impertérritos».
- Fixes from the first test on game version 1.11.1. The game no longer closes at startup with the self-test enabled. In the Collection, the 3D view of the new commanders shows their own model (it used to show the borrowed hero's figure). The commanders' own spells get their own grimoire band, one per faction, in their colours. The commanders' long stories show under their model (new key). The facción A and faction B factions can appear as enemy armies in all three campaign acts, weighted like each of the game's factions (before, one in act 2 and the other in act 3). New morale effect for the Undaunted module.

### 0.7.5 (02-11-2026)

- Revisión completa. Las partidas diaria y cooperativa ya no borran lo que el núcleo guarda aparte de la campaña principal (comandantes, objetos, ofertas, ciudades y hechizos nuevos): cada partida tiene lo suyo. Los saves tampoco llevan ya ids nuevos en las listas que trajo el juego 1.11.1 (oferta tras la batalla, mercenarios, ejército inicial del cooperativo), en el historial de partidas (comandante, raza y señor de la guerra de cada batalla), en las estadísticas por héroe ni en el aspecto elegido; al cargar con los módulos activos vuelve todo. La copia de seguridad de los saves cubre también la partida diaria y la cooperativa, y lleva el registro aparte del núcleo (que no viaja con Steam Cloud). Logros: los ocho de composición del ejército ya no se bloquean para siempre por haber jugado una partida con módulos: desde el historial se recuenta partida a partida y solo cuentan las de serie, y los ya ganados se reenvían a Steam. Multijugador: el cooperativo se corta al invitar a un amigo y el menú principal ya no avisa de un bloqueo en cada arranque. Un módulo activo en el menú aunque el núcleo lo rechace también cuenta como partida modificada. La opción de diagnóstico para omitir parches ya no puede quitar los bloqueos en línea ni el filtro de logros. Al bloquear el juego en línea (cola, sala propia, unirse, invitar), el núcleo lo avisa en pantalla con el aviso del juego, en español o en inglés; el idioma de los textos de respaldo sale del idioma elegido en el juego. Autor visible: ZeroitMods. Otros arreglos menores.
- Full review. Daily and co-op runs no longer wipe what the core stores separately for the main campaign (new commanders, gear, offers, towns and spells): each run keeps its own. Saves no longer keep new ids in the lists added by game version 1.11.1 (post-battle offer, mercenaries, co-op starting army), in the run history (commander, race and warlord of each battle), in the per-hero stats or in the picked looks; loading with the modules enabled brings it all back. The save backup also covers daily and co-op runs and includes the core's separate registry (it does not travel with Steam Cloud). Achievements: the eight army-composition ones are no longer blocked forever after playing a run with modules: from the run history they are recounted run by run and only vanilla runs count, and those already earned are re-sent to Steam. Multiplayer: co-op is stopped when inviting a friend and the main menu no longer reports a block on every start. A module enabled in the menu but rejected by the core also counts as a modded run. The diagnostic option to skip patches can no longer remove the online blocks or the achievement filter. When it blocks online play (queue, own lobby, joining, inviting), the core now says so on screen with the game's notice, in Spanish or English; the language of the fallback texts comes from the language picked in the game. Visible author: ZeroitMods. Other minor fixes.

### 0.7.4 (02-11-2026)

- Hechizos propios para los comandantes nuevos (fase 1, solo datos): uno por cada comandante de las facciones nuevas. Cada uno es una copia de un hechizo del juego con sus propios valores, nombre y texto, y el icono del hechizo en que se basa; el núcleo lo vuelve a poner cada vez que el juego recarga su lista de hechizos. Como los del juego, ganar una partida con su comandante lo desbloquea en el grimorio para los demás héroes. Si un hechizo propio no se puede crear, su comandante lleva un hechizo oficial de respaldo y el log lo dice. Los saves no guardan nunca los hechizos propios (el núcleo los apunta aparte y los devuelve al cargar si su módulo está activo), así que una partida carga igual sin el núcleo y el historial no enseña textos rotos. El logro Grand Grimoire cuenta solo los hechizos de serie. Sin soporte en cooperativo ni versus: con módulos de Zero activos no se pueden abrir partidas en línea (Multijugador.Bloquear en la configuración). Munición x3 también para las unidades híbridas (el juego no les da bonus de munición por su cuenta). El sorteo de objetos nuevos respeta la rareza del juego (una carta común solo se cambia por un objeto nuevo común, y así con las demás) y no toca la última carta de un tesoro de tres, la que encaja con tu ejército.
- Own spells for the new commanders (phase 1, data only): one for each commander of the new factions. Each one is a copy of a game spell with its own values, name and text, and the icon of the spell it is based on; the core puts it back every time the game reloads its spell list. Like the game's own, winning a run with its commander unlocks it in the grimoire for the other heroes. If an own spell cannot be created, its commander takes an official fallback spell and the log says so. Saves never store the own spells (the core records them separately and brings them back on load if their module is enabled), so a run still loads without the core and the run history shows no broken text. The Grand Grimoire achievement counts only vanilla spells. Not supported in co-op or versus: with Zero's modules enabled, online matches cannot be started (Multijugador.Bloquear in the config). Ammo x3 also for hybrid units (the game gives them no ammunition bonus of its own). New gear in the draws follows the game's rarity (a common card is only swapped for a common new item, and so on) and the last card of a three-card treasure, the one that suits your army, is left alone.

### 0.7.3 (01-11-2026)

- Adaptado al juego 1.11.1: el juego movió de sitio varias piezas internas (telemetría, logros de Steam, audio) y la 0.7.2 ya no cargaba. Ahora, si una actualización del juego vuelve a mover algo, solo se desactiva esa parte y el resto del núcleo sigue funcionando. Los logros de ejército entero que el juego revisa con el historial de partidas no se conceden si ese historial tiene partidas jugadas con contenido nuevo. El objeto del juego que sirve de respaldo al estandarte regimental y al cuerno del fiordo se llama ahora War Drums. Si un comandante nuevo pide un hechizo que el juego no tiene registrado, lo dice en el log y lleva el de su comandante prestado (o, si tampoco está registrado, «Alza los estandartes», el común que tiene cualquier héroe) en vez de quedarse sin hechizo. Con los iconos de unidad de 512x768 del juego 1.11.1, el núcleo ya no crea su propio retrato: lo carga el juego.
- Updated for game version 1.11.1: the game moved several internal pieces (telemetry, Steam achievements, audio) and 0.7.2 no longer loaded. If a game update moves something again, only that part is switched off and the rest of the core keeps working. Whole-army achievements that the game checks against the run history are not granted if that history has runs played with new content. The vanilla item used as fallback for the Regimental Standard and the Fjord Horn is now called War Drums. If a new commander asks for a spell the game has not registered, the log says so and the commander takes the spell of the commander it borrows from (or, if that one is not registered either, Rally The Banners, the common spell every hero has) instead of having none. With the 512x768 unit icons of game version 1.11.1, the core no longer builds its own portrait: the game loads it.

### 0.7.2 (18-10-2026)

- Pantalla de héroes ampliada con todas las facciones (también las de los módulos, que antes no cabían) y ejército inicial de 3 unidades para los comandantes nuevos; el último héroe elegido se guarda como su héroe de serie prestado; los objetos nuevos salen en la armería del equipo inicial (y el equipo recordado se guarda sin ellos); la Colección enseña las facciones nuevas con sus unidades, comandantes y objetos, con desplazamiento si una facción no cabe, y su contador del menú las cuenta; la batalla personalizada tiene las facciones nuevas y desplazamiento; el mapa de campaña ya no se bloquea con un comandante nuevo; los retratos y figuras de un comandante cuyo módulo está apagado usan los de su héroe prestado; los logros de Steam de serie cuentan solo unidades y héroes de serie (Logros.Filtrar); las reglas de los objetos admiten la condición UnitName (el Grog de hongos vale también para los acuchilladores nocturnos); la Tesorería imperial de los comandantes del Imperio duplica solo los primeros 5 de oro de interés de cada turno (antes, todo el interés); con cualquier módulo de Zero activo tampoco sale la telemetría del juego. El README lo cuenta en «Qué hace» y añade una sección de problemas conocidos.
- Hero screen extended with every faction (those from the modules, which did not fit, included) and a 3-unit starting army for the new commanders; the last picked hero is saved as its borrowed vanilla hero; the new gear shows up in the starting gear armory (and the remembered pick is saved without it); the Collection shows the new factions with their units, commanders and gear, scrolling when a faction does not fit, and its main menu counter includes them; custom battles have the new factions and scrolling; the campaign map no longer locks up with a new commander; portraits and figures of a commander whose module is disabled use their borrowed hero's; vanilla Steam achievements count only vanilla units and heroes (Logros.Filtrar); gear rules accept the UnitName condition (Mushroom Grog also works for the Night Stabbers); the Empire commanders' Imperial Treasury doubles only the first 5 gold of interest each turn (it used to double all of it); with any of Zero's modules enabled the game's telemetry is blocked too. The README covers it under "What it does" and adds a known issues section.

### 0.7.1 (17-10-2026)

- Revisión: con cualquier módulo de Zero activo (también solo de efectos o Economía) las partidas no se envían a las clasificaciones online; munición x3 y stats a distancia solo para el héroe del jugador (un señor de la guerra enemigo ya no los recibe, y si usa tu mismo héroe ese enfrentamiento va sin ellos); los héroes nuevos se guardan como su héroe de serie prestado (la partida carga sin su módulo y vuelve al activarlo); las marcas de objetos de módulos apagados ya no pasan de una partida a otra; con varios módulos de stats a distancia manda el mayor; la autoprueba viene desactivada; la carpeta de datos sale de Unity; la DLL ya no lleva rutas locales. Sincronización con el gestor fichero a fichero: lo que el paquete ya no trae se aparta a Mods\_ZeroRetirados y lo editado a mano se respalda allí antes de actualizar; al cambiar de perfil no se duplican las copias retiradas; modlist.json se lee igual de estricto que el juego.
- Review: with any of Zero's modules enabled (also effect-only modules or Economy) runs are not submitted to the online leaderboards; ammo x3 and ranged stats apply only to the player's hero (an enemy warlord no longer gets them, and if it uses your same hero that fight goes without them); new heroes are saved as their borrowed vanilla hero (the run loads without their module and comes back when it is enabled again); gear marks from disabled modules no longer carry over to another run; with several ranged-stats modules the biggest one wins; the self-test ships disabled; the data folder comes from Unity; the DLL no longer contains local paths. Mod manager sync works file by file: files the package no longer ships are moved to Mods\_ZeroRetirados and local edits are backed up there before updating; switching profiles no longer duplicates retired copies; modlist.json is read as strictly as the game.

### 0.7.0 (17-10-2026)

- Primera versión: el plugin monolítico anterior se separa en núcleo y módulos.
- First version: the earlier monolithic plugin is split into core and modules.

## Zero - Escuadras más grandes x50% (Bigger Squads x50%)

### 1.0.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods. Pide el núcleo 0.7.7.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods. Requires core 0.7.7.

### 1.0.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.

## Zero - Escuadras más grandes x100% (Bigger Squads x100%)

### 1.0.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods. Pide el núcleo 0.7.7.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods. Requires core 0.7.7.

### 1.0.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.

## Zero - Escuadras más grandes x150% (Bigger Squads x150%)

### 1.0.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods. Pide el núcleo 0.7.7.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods. Requires core 0.7.7.

### 1.0.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.

## Zero - Munición x2 (Ammo x2)

### 1.0.0 (03-11-2026)

- Primera versión: el doble de munición para tu ejército; variante de «Munición x3» (necesita el núcleo 0.7.7).
- First version: double ammunition for your army; a variant of "Ammo x3" (requires core 0.7.7).

## Zero - Munición x3 (Ammo x3)

### 1.0.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Con el núcleo 0.7.1, la munición extra es solo para tu ejército: el señor de la guerra enemigo ya no la recibe. Con el núcleo 0.7.4, el triple de munición llega también a las unidades híbridas (el juego no les da bonus de munición por su cuenta: lo pone el núcleo); pide el núcleo 0.7.7. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. With core 0.7.1, the extra ammo is only for your army: the enemy warlord no longer gets it. With core 0.7.4, hybrid units get triple ammunition too (the game gives them no ammunition bonus of its own: the core adds it); requires core 0.7.7. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods.

### 1.0.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.

## Zero - Munición x5 (Ammo x5)

### 1.0.0 (03-11-2026)

- Primera versión: cinco veces la munición para tu ejército; variante de «Munición x3» (necesita el núcleo 0.7.7).
- First version: five times the ammunition for your army; a variant of "Ammo x3" (requires core 0.7.7).

## Zero - Mejores estadísticas a distancia (Better Ranged Stats)

### 1.0.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Con el núcleo 0.7.1, los bonus a distancia son solo para tu ejército: el señor de la guerra enemigo ya no los recibe. Para el juego 1.11.1, en español los bonus de alcance y de precisión se llaman «Vista de águila» y «Manos firmes» (los nombres de antes coincidían con textos del juego); pide el núcleo 0.7.7. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. With core 0.7.1, the ranged bonuses are only for your army: the enemy warlord no longer gets them. For game version 1.11.1, the Spanish names of the range and accuracy bonuses change (the old ones matched game texts); the English names stay the same; requires core 0.7.7. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods.

### 1.0.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.

## Zero - Indesmoralizables (Unbreakable)

### 1.0.0 (03-11-2026)

- Primera versión: las escuadras del jugador no flaquean ni huyen por la moral, en batalla y en la resolución automática (necesita el núcleo 0.7.7).
- First version: the player's squads never waver or rout from morale, in battle and in auto-resolve (requires core 0.7.7).

## Zero - Más iconos (More Banner Icons)

### 1.0.0 (03-11-2026)

- Primera versión: 34 iconos para el estandarte y el selector de iconos en la pestaña «Estandarte» (necesita el núcleo 0.7.7).
- First version: 34 banner icons and the icon picker in the Banner tab (requires core 0.7.7).

## Zero - Más oro, tope de intereses 30 (More Gold)

### 1.2.1 (03-11-2026)

- README revisado: clasificaciones online (no se envían partidas con módulos de Zero activos) y compatibilidad. Además, el README avisa de que no hay soporte en cooperativo ni versus con los módulos de Zero y pone al día la compatibilidad (adaptado al juego 1.11.1). Trae el texto del aviso en pantalla cuando el núcleo bloquea el juego en línea (traducido a los idiomas del juego), y el README dice en qué idiomas van los textos y qué se hizo con IA generativa. Autor visible: ZeroitMods.
- README reviewed: online leaderboards (runs with Zero's modules enabled are not submitted) and compatibility. The README also notes that co-op and versus are not supported with Zero's modules and brings compatibility up to date (adapted to game version 1.11.1). It ships the text of the on-screen notice shown when the core blocks online play (translated into the game's languages), and the README states which languages the texts come in and what was made with generative AI. Visible author: ZeroitMods.

### 1.2.0 (17-10-2026)

- Primera versión como módulo independiente del núcleo Zero - Núcleo.
- First version as a standalone module of the Zero - Nucleo core.
