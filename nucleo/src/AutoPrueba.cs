// AutoPrueba.cs
// Comprobación automática al arrancar (una vez por sesión, en el menú, sin tocar partidas):
//   1) Reclutamiento: pide al juego una oferta de ciudad de Gruntkin (una facción que no es la de Eisenmann) con
//      la inyección forzada y comprueba que la facción A aparece y que el resultado se puede modificar.
//   2) Saves: serializa con JsonUtility una escuadra de guardias y comprueba que el texto sale como Eisenmann y sin
//      la 900; después lo vuelve a leer con FromJson (no genérico y genérico) y comprueba que vuelve a ser 900.
//   4) Modelo propio (F2): carga completa de unit_visuals\_900 con el lector del juego, sin crear entidades.
//   3) Blob de stats: llama a LoadAndInjectData (lo mismo que hace el juego al cargar una batalla) y comprueba que
//      los rellenos temporales se retiran y la 900 sigue en el catálogo.
//   5) Sonido láser (F3, 18-10-2026): los WAV se han convertido en AudioClip con las funciones internas de Unity
//      (Construct_Internal + CreateUserSound + SetData, leído de vuelta con GetData), GetBattlefieldAudio devuelve los
//      clips propios para la 900 (láser facción A), se informa del disparo de cada unidad de energía y se mide el nivel del
//      disparo de Eisenmann (pico/RMS) para igualar el volumen. Si la capa 1 no funcionó, se informa del motivo y de
//      lo que queda (prestado o base) y no cuenta como fallo.
//   6) Unidades 9xx (0.4.0): todas las del manifiesto en el catálogo, las activas en la lista de su facción y las
//      demás (la 900 y las solo-datos) fuera de toda lista; assets verificados.
//   7) Una suelta concreta (944 Ogros de Asedio): base Oni, raza Legión de Hierro, stats y rasgos del mod, visual,
//      icono y prefab de reclutamiento.
//   8) Ejército reforzado en una unidad de serie (PeasantBowmen) y en la 900: soldados y munición de base frente a
//      los del catálogo.
//   9) Reglas del jugador (0.7.1): solo para el héroe activo (forzado en la prueba: Morvayne y un héroe nuevo 9xx),
//      ninguna para otro héroe ni sin héroe (la IA), y ninguna con un señor de la guerra enemigo del mismo héroe.
//  10) Ida y vuelta del saneado con una unidad 9xx (944 se guarda como Oni), en memoria.
//  11) Aviso si el mod antiguo ZeroMunicionAlcance sigue activo.
// 0.5.0 (F2 y F3), con las facciones nuevas activas:
//  12) RaceData de 40 y 41 (GetRaceData no nulo, con su raza y sus colores) y color de interfaz propio.
//  13) Unidades de 40/41 en la lista de su raza, en los 4 rangos (también la 900 en la 40 con la F3).
//  14) Generación de razas con las nuevas: actos 2 y 3, actos infinitos, anfitriones de la Marcha, ciudades y general
//      rival de esa raza (llamando a los métodos del juego con muchas semillas, sin partida).
//  15) Bonus de los héroes 901-904 (hero_bonus_rules.json del mod) con el evaluador de reglas del juego.
//  16) Saneado de la raza de una ciudad 40 ida y vuelta con JsonUtility (registro solo en memoria).
//  17) F3: la 900 en la lista de la facción A y fuera de las de serie; ya no se inyecta en ofertas.
//  18) Retratos propios cargados y LoadHeroSpriteAsync(901) devuelve el propio.
//  19) Héroes 901-904 tras una recarga de datos forzada (ReloadData), 2 por raza nueva, con sus datos.
//  20) (0.5.1) Razas jugables: 40 y 41 jugables; quitando la 40 de las jugables, no sale en el acto 2, ni en ciudades,
//      ni en la Marcha, y la 900 vuelve a las ofertas; al devolverla, todo vuelve.
//  21) (0.5.1) La raza de una ciudad 40 se restaura al leer aunque no haya unidades 9xx registradas (simulado).
//  La pantalla de héroes no se puede comprobar sin abrirla: se informa (Diagnostico.ProbarPantallaHeroes la abre).
// 0.6.0, objetos nuevos (gear) 700-727, si están activos:
//  22) Los 22 objetos registrados tras una recarga de datos forzada: GetGear devuelve su nombre interno, rareza y número;
//      los de serie (Arming Swords) y None siguen igual.
//  23) Iconos: los 22 PNG cargados como Sprite y en la caché del juego (SpriteData.GetSprite los devuelve).
//  24) Efecto de un objeto de cada grupo (700 de la facción A, 712 de la facción B, 721 de Gruntkin) con el evaluador del juego en una
//      unidad de prueba (en memoria): el bonus aparece al llevarlo y desaparece al quitarlo.
//  25) Sorteo (§5): simulación de muchas tiradas con la proporción afín (25 %), general (5 %), cofre de 3 cartas,
//      peso doble del objeto de comandante, objetos excluidos y afinidad por ejército (3 escuadras).
//  26) Saneado de saves de un objeto nuevo ida y vuelta (botín de ciudad con JsonUtility y lista Gear con choque de
//      respaldo), con un registro solo en memoria.
// 0.7.2 (AutoPruebaNuevas.cs): parches de la interfaz, la armería y los logros puestos; lista de gear ampliada y
// suspendida; listas de raza para los logros de colección; héroes nuevos en la lista y casillas de la pantalla de héroes.
// 0.7.0, núcleo con módulos (cada prueba de arriba solo se hace si su módulo está aplicado: la 900 con la facción A,
// la 944 con las unidades sueltas, las facciones y los objetos con el suyo; sin módulos de contenido se comprueba que el
// catálogo queda de serie; escuadras y bonus del jugador con los valores que declaran los módulos, o de serie):
//  M1) Detección: módulos encontrados, activos, sincronizados y los desactivados por defecto apuntados en modlist.json.
//  M2) Combinaciones en memoria (sin tocar el juego) con las declaraciones reales: ninguno, cada módulo solo, las tres
//      escuadras juntas (manda la mayor y se avisa), facciones con y sin sueltas, la 900 solo con la facción A y cada
//      objeto solo con su módulo.
//  M3) Combinaciones aplicadas al juego (se reconstruye el catálogo con ReloadData y al final se vuelve al real): ninguno
//      (catálogo de serie, sin escalado ni reglas), cada módulo de contenido solo, cada módulo de escuadras solo y los
//      tres juntos, munición sola, stats solos. Los de contenido solo se simulan si el juego los cargó en esta sesión.
// El resultado sale en el log con la etiqueta [autoprueba] («... OK» o «... FALLO»). Se desactiva con
// Diagnostico.AutoPrueba = false. Deja en el registro dos GUID de prueba fijos (no corresponden a ninguna escuadra
// real, son inofensivos). No toca ningún fichero de save.
// 0.4.1: las pruebas de saves usan un registro SOLO EN MEMORIA (ruta null), así no se apunta ningún GUID inventado en
// el registro real; mientras duran, el saneado usa ese registro y al terminar se devuelve el real.
// Revisión del 18-10 (A-1): el vigilante de saves corre en otro hilo y leía Saves.Registro. Ahora la autoprueba entera va
// con el vigilante en pausa (Saves.Pausa, que además espera a que acabe la vuelta en curso) y el vigilante nunca sanea con
// un registro solo en memoria. Si la vuelta en curso no acaba a tiempo, la autoprueba no se hace en esta sesión.
// AutoPrueba.cs
// Automatic check at startup (once per session, in the menu, without touching runs):
//   1) Recruitment: asks the game for a city offer from Gruntkin (a faction other than Eisenmann's) with
//      forced injection and checks that faction A appears and that the result can be modified.
//   2) Saves: serializes a squad of guards with JsonUtility and checks that the text comes out as Eisenmann and without
//      the 900; then reads it back with FromJson (non-generic and generic) and checks that it is 900 again.
//   4) Custom model (F2): full load of unit_visuals\_900 with the game's reader, without creating entities.
//   3) Stats blob: calls LoadAndInjectData (the same thing the game does when loading a battle) and checks that
//      the temporary fillers are removed and the 900 is still in the catalog.
//   5) Laser sound (F3, 18-10-2026): the WAVs have been converted into AudioClip with Unity's internal functions
//      (Construct_Internal + CreateUserSound + SetData, read back with GetData), GetBattlefieldAudio returns the
//      custom clips for the 900 (faction A laser), the shot of each energy unit is reported and the level of the
//      Eisenmann shot is measured (peak/RMS) to match the volume. If layer 1 did not work, the reason is reported along with
//      what remains (borrowed or base) and it does not count as a failure.
//   6) 9xx units (0.4.0): all those in the manifest are in the catalog, the active ones in their faction's list and the
//      others (the 900 and the data-only ones) outside every list; assets verified.
//   7) One specific loose unit (944 Siege Ogres): Oni base, Iron Legion race, mod stats and traits, visual,
//      icon and recruitment prefab.
//   8) Reinforced army on a stock unit (PeasantBowmen) and on the 900: base soldiers and ammo versus
//      those in the catalog.
//   9) Player rules (0.7.1): only for the active hero (forced in the test: Morvayne and a new 9xx hero),
//      none for another hero or without a hero (the AI), and none with an enemy warlord of the same hero.
//  10) Round trip of the sanitizing with a 9xx unit (944 is saved as Oni), in memory.
//  11) Warning if the old mod ZeroMunicionAlcance is still active.
// 0.5.0 (F2 and F3), with the new factions active:
//  12) RaceData of 40 and 41 (GetRaceData non-null, with its race and its colors) and custom interface color.
//  13) Units of 40/41 in the list of their race, in all 4 ranks (also the 900 in 40 with F3).
//  14) Race generation with the new ones: acts 2 and 3, infinite acts, March hosts, cities and rival general
//      of that race (calling the game's methods with many seeds, without a run).
//  15) Bonuses of heroes 901-904 (the mod's hero_bonus_rules.json) with the game's rule evaluator.
//  16) Sanitizing of the race of a city 40 round trip with JsonUtility (registry in memory only).
//  17) F3: the 900 in the faction A list and outside the stock ones; it is no longer injected into offers.
//  18) Custom portraits loaded and LoadHeroSpriteAsync(901) returns the custom one.
//  19) Heroes 901-904 after a forced data reload (ReloadData), 2 per new race, with their data.
//  20) (0.5.1) Playable races: 40 and 41 playable; removing 40 from the playable ones, it does not appear in act 2, nor in cities,
//      nor in the March, and the 900 returns to the offers; when it is restored, everything returns.
//  21) (0.5.1) The race of a city 40 is restored on reading even if no 9xx units are registered (simulated).
//  The heroes screen cannot be checked without opening it: it is reported (Diagnostico.ProbarPantallaHeroes opens it).
// 0.6.0, new gear 700-727, if active:
//  22) The 22 items registered after a forced data reload: GetGear returns their internal name, rarity and number;
//      the stock ones (Arming Swords) and None stay the same.
//  23) Icons: the 22 PNGs loaded as Sprite and in the game's cache (SpriteData.GetSprite returns them).
//  24) Effect of one item from each group (700 from faction A, 712 from faction B, 721 from Gruntkin) with the game's evaluator on a
//      test unit (in memory): the bonus appears when it is equipped and disappears when it is removed.
//  25) Drop roll (§5): simulation of many rolls with the affinity ratio (25%), general (5%), 3-card chest,
//      double weight for the commander item, excluded items and affinity per army (3 squads).
//  26) Sanitizing of saves of a new item round trip (city loot with JsonUtility and Gear list with backup
//      conflict), with a registry in memory only.
// 0.7.2 (AutoPruebaNuevas.cs): patches for the interface, the armory and the achievements applied; extended and
// suspended gear list; race lists for the Collection achievements; new heroes in the list and slots of the heroes screen.
// 0.7.0, core with modules (each test above is only run if its module is applied: the 900 with faction A,
// the 944 with the loose units, the factions and items with their own; without content modules it is checked that the
// catalog stays stock; squads and player bonuses with the values declared by the modules, or stock):
//  M1) Detection: modules found, active, synchronized and those disabled by default noted in modlist.json.
//  M2) In-memory combinations (without touching the game) with the real declarations: none, each module alone, the three
//      squads together (the largest wins and a warning is shown), factions with and without loose units, the 900 only with faction A and each
//      item only with its module.
//  M3) Combinations applied to the game (the catalog is rebuilt with ReloadData and at the end it returns to the real one): none
//      (stock catalog, no scaling or rules), each content module alone, each squad module alone and all
//      three together, ammo alone, stats alone. Content-only ones are simulated if the game loaded them in this session.
// The result appears in the log with the tag [autoprueba] («... OK» or «... FALLO»). It is disabled with
// Diagnostico.AutoPrueba = false. It leaves two fixed test GUIDs in the registry (they do not match any real
// squad, they are harmless). It does not touch any save file.
// 0.4.1: the save tests use an IN-MEMORY-ONLY registry (null path), so no made-up GUID is recorded in
// the real registry; while they last, the sanitizing uses that registry and when finished the real one is restored.
// Review of 18-10 (A-1): the save watcher runs on another thread and read Saves.Registro. Now the whole self-test runs
// with the watcher paused (Saves.Pausa, which also waits for the current cycle to finish) and the watcher never sanitizes with
// an in-memory-only registry. If the current cycle does not finish in time, the self-test is not run in this session.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime;
using Memori.SaveData;
using TabletopTavern.GpuAnim;
using TJ;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: autoprueba de arranque
    // Section: startup self-test
    // -----------------------------------------------------------------------
    public static partial class AutoPrueba
    {
        private const string GuidPrueba = "00000000-0000-0000-0000-00000000a900";
        private const string GuidPrueba944 = "00000000-0000-0000-0000-00000000a944";
        private static bool _hecha;
        internal static bool ForzarOferta;   // lo lee Reclutamiento.Inyectar durante la autoprueba
                                             // read by Reclutamiento.Inyectar during the self-test

        /// <summary>Ejecuta las comprobaciones una sola vez (si la configuración lo permite).</summary>
        /// Runs the checks only once (if the configuration allows it).
        public static void EjecutarUnaVez(TabletopTavernData data)
        {
            if (_hecha || Plugin.CfgAutoPrueba == null || !Plugin.CfgAutoPrueba.Value) return;
            if (data == null || data.SquadStatsDictionary == null || data.SquadStatsDictionary.Count == 0) return;
            // Con módulos de contenido se espera a que sus unidades estén en el catálogo (primera recarga completa)
            // With content modules, wait until their units are in the catalog (first full reload)
            if (UnidadGuardia.Todas.Count > 0 && !UnidadGuardia.AlgunaRegistrada()) return;
            _hecha = true;
            // Revisión del 18-10 (A-1): vigilante de saves en pausa durante TODA la autoprueba (varias pruebas cambian
            // Saves.Registro por uno temporal). Se espera a que acabe su vuelta en curso; si no acaba a tiempo, la autoprueba
            // no se hace (mejor sin autoprueba que arriesgar un GUID real en el registro temporal)
            // Review of 18-10 (A-1): save watcher paused during the ENTIRE self-test (several tests swap
            // Saves.Registro for a temporary one). It waits for its current cycle to finish; if it doesn't finish in time, the self-test
            // is skipped (better no self-test than risking a real GUID in the temporary registry)
            try
            {
                if (!Saves.Pausa.Pausar(5000))
                {
                    Plugin.Aviso("[autoprueba] No se hace en esta sesión: el vigilante de saves sigue a mitad de una vuelta tras 5 s.");
                    DiagnosticoPantalla.FinAutoprueba = UnityEngine.Time.realtimeSinceStartup;   // el diagnóstico de pantalla no espera más
                                                                                                 // the on-screen diagnostics no longer wait
                    return;
                }
                int ok = 0, total = 0;
                Plugin.Info("[autoprueba] Módulos aplicados: " + Nucleo.Describir(Nucleo.Actual));

                // --- M1 y M2) Detección y combinaciones en memoria (no tocan el juego) ---
                // --- M1 and M2) In-memory detection and combinations (they don't touch the game) ---
                PruebasModulosMemoria(ref ok, ref total);

                // Pruebas de la facción A (900): solo con el módulo facción A aplicado
                // Tests for faction A (900): only with the faction A module applied
                if (UnidadGuardia.RegistradaEnCatalogo()) PruebasGuardia(data, ref ok, ref total);
                else Plugin.Info("[autoprueba] Guardia Imperial (900): no está en el catálogo (módulo Astra Militarum no aplicado); sus pruebas no se hacen.");

                PruebasFabrica(data, ref ok, ref total);

                // --- 27-30) Lo nuevo de la 0.7.2 (interfaz, armería, logros y pantalla de héroes; AutoPruebaNuevas.cs) ---
                // --- 27-30) What's new in 0.7.2 (interface, armory, achievements and heroes screen; AutoPruebaNuevas.cs) ---
                PruebasVersion072(data, ref ok, ref total);

                // --- M3) Combinaciones aplicadas al juego (al final: reconstruyen el catálogo y vuelven al real) ---
                // --- M3) Combinations applied to the game (at the end: they rebuild the catalog and return to the real one) ---
                PruebasModulosJuego(data, ref ok, ref total);

                Plugin.Info($"[autoprueba] Resultado: {ok}/{total} comprobaciones correctas" + (ok == total ? " (todo OK)." : " (hay FALLOS: ver arriba)."));
                DiagnosticoPantalla.FinAutoprueba = UnityEngine.Time.realtimeSinceStartup;
            }
            finally { Saves.Pausa.Reanudar(); }
        }

        // Pruebas 1-5 de la facción A (900), cada una aislada en su try/catch
        // Tests 1-5 for faction A (900), each isolated in its own try/catch
        private static void PruebasGuardia(TabletopTavernData data, ref int ok, ref int total)
        {
            // --- 1) Oferta de ciudad de otra facción ---
            // --- 1) City offer from another faction ---
            total++;
            try
            {
                ForzarOferta = true;
                var oferta = data.GetSquadsToRecruitForRaceTown(Race.Gruntkin, 3, 12345, TownSize.City);
                bool hay = false;
                for (int i = 0; oferta != null && i < oferta.Length; i++) if (oferta[i] == UnidadGuardia.Guardia) hay = true;
                if (FaccionesNuevas.GuardiaEnAstra)
                {
                    // F3: la 900 es de la facción A; la oferta de otra facción ya no la lleva aunque se fuerce
                    // F3: 900 belongs to faction A; the offer from another faction no longer includes it even if forced
                    if (!hay) { ok++; Plugin.Info("[autoprueba] Reclutamiento OK (F3): la oferta forzada de una ciudad Gruntkin ya NO incluye la Guardia Imperial (solo Astra Militarum)."); }
                    else Plugin.Error("[autoprueba] Reclutamiento FALLO (F3): la oferta de una ciudad Gruntkin sigue incluyendo la 900.");
                }
                else if (hay) { ok++; Plugin.Info("[autoprueba] Reclutamiento OK: la oferta de una ciudad Gruntkin incluye la Guardia Imperial (el postfix se ejecuta)."); }
                else Plugin.Error("[autoprueba] Reclutamiento FALLO: la oferta forzada NO incluye la 900 (¿método inlineado o rango no permitido?).");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Reclutamiento FALLO: " + e.Message); }
            finally { ForzarOferta = false; }

            // --- 2) Ida y vuelta de un save con una escuadra de guardias (registro solo en memoria) ---
            // --- 2) Save round trip with a squad of guardsmen (registry in memory only) ---
            total++;
            var registroReal = Saves.Registro;
            Saves.Registro = new RegistroGuardias(null);
            try
            {
                var escuadra = new SquadToLoad(UnidadGuardia.Guardia, 0, -1, false, 1f);
                escuadra.UniqueID = GuidPrueba;
                string texto = UnityEngine.JsonUtility.ToJson(escuadra, true);
                bool salida = texto.Contains("\"UnitName\": " + UnidadGuardia.OrdinalBase) && SaneadoSaves.ContarResiduos(texto, Saves.Tabla) == 0
                              && Saves.Registro.ContieneGuid(GuidPrueba);
                Plugin.Info($"[autoprueba] Texto guardado: {texto.Replace("\n", " ").Replace("    ", "")}");

                var leido1 = UnityEngine.JsonUtility.FromJson(texto, Il2CppType.Of<SquadToLoad>());
                UnitName vuelta1 = new SquadToLoad(leido1.Pointer).UnitName;
                UnitName vuelta2 = UnityEngine.JsonUtility.FromJson<SquadToLoad>(texto).UnitName;
                Plugin.Info($"[autoprueba] Escritura sin 900: {salida}. Lectura FromJson(string,Type): {vuelta1}. Lectura FromJson<T> (la de los saves): {vuelta2}.");
                if (salida && vuelta1 == UnidadGuardia.Guardia && vuelta2 == UnidadGuardia.Guardia) { ok++; Plugin.Info("[autoprueba] Saves OK: el disco no ve la 900 y al leer vuelve a ser Guardia Imperial."); }
                else Plugin.Error("[autoprueba] Saves FALLO: la ida y vuelta NO es correcta (ver líneas anteriores).");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Saves FALLO: " + e); }
            finally { Saves.Registro = registroReal; }

            // --- 2b) Assets que lee la interfaz (carta de reclutamiento, bandera de batalla, sonidos) ---
            // --- 2b) Assets read by the interface (recruitment card, battle banner, sounds) ---
            total++;
            try
            {
                var dic = data.SquadAssetsDictionary;
                string g = AssetsGuardia.Describir(AssetsGuardia.LeerEntrada(dic, UnidadGuardia.Guardia));
                string b = AssetsGuardia.Describir(AssetsGuardia.LeerEntrada(dic, UnidadGuardia.Base));
                Plugin.Info($"[autoprueba] Assets 900: {g}");
                Plugin.Info($"[autoprueba] Assets Eisenmann: {b}");
                // Lo mismo que hacen RecruitCard.SetUp, SquadFlagGameObject y EntityWatcher con la unidad
                // The same thing RecruitCard.SetUp, SquadFlagGameObject and EntityWatcher do with the unit
                var iconoTipo = data.GetSquadTypeIcon(UnidadGuardia.Guardia);
                var bandera = data.GetSquadTypeFlagSprite(UnidadGuardia.Guardia);
                var icono = data.GetUnitIcon(UnidadGuardia.Guardia);
                var raza = data.GetRaceFromUnitName(UnidadGuardia.Guardia);
                string clavePrefab = data.GetRecruitmentPrefabKey(UnidadGuardia.Guardia);
                var grito = data.GetRandomBarkSFX(UnidadGuardia.Guardia);
                float ruido = data.GetSquadNoise(UnidadGuardia.Guardia);
                var voces = AssetsGuardia.LeerEntrada(dic, UnidadGuardia.Guardia).voiceSFX;   // la entrada tal cual la lee SquadFlagGameObject en batalla
                                                                                              // the entry exactly as SquadFlagGameObject reads it in battle
                bool okA = AssetsGuardia.AssetsValidos && iconoTipo != null && bandera != null && icono != null
                           && !string.IsNullOrEmpty(clavePrefab) && grito != null && voces != null;
                Plugin.Info($"[autoprueba] Getters 900: icono de tipo {(iconoTipo == null ? "null" : iconoTipo.name)}, bandera {(bandera == null ? "null" : bandera.name)}, " +
                            $"icono {(icono == null ? "null" : icono.name)}, raza {raza}, prefab {clavePrefab}, grito {(grito == null ? "null" : grito.name)}, ruido {ruido}, voces {(voces == null ? "null" : voces.name)}");
                if (okA) { ok++; Plugin.Info("[autoprueba] Assets OK: la carta de reclutamiento y la bandera de batalla tienen todo lo que piden."); }
                else Plugin.Error("[autoprueba] Assets FALLO: falta algo (ver líneas anteriores).");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Assets FALLO: " + e); }

            // --- 3) Blob de stats: se construye como al empezar una batalla y se comprueba que los rellenos se retiran ---
            // --- 3) Stats blob: built as when a battle starts, and checks that the fillers are removed ---
            total++;
            try
            {
                if (Unity.Entities.World.DefaultGameObjectInjectionWorld == null)
                {
                    total--;
                    Plugin.Info("[autoprueba] Blob: el mundo ECS aún no existe; se comprobará al empezar la primera batalla.");
                }
                else
                {
                    int antes = data.SquadStatsDictionary.Count;
                    data.LoadAndInjectData();
                    int despues = data.SquadStatsDictionary.Count;
                    bool sigue = data.SquadStatsDictionary.ContainsKey(UnidadGuardia.Guardia);
                    if (despues == antes && sigue) { ok++; Plugin.Info($"[autoprueba] Blob OK: construido con la 900 y diccionario de vuelta a {despues} unidades."); }
                    else Plugin.Error($"[autoprueba] Blob FALLO: el diccionario tenía {antes} unidades y ahora {despues} (900 presente: {sigue}).");
                    // (sin la 900, la prueba del blob con las demás unidades nuevas va en PruebasFabrica)
                    // (without 900, the blob test with the other new units is in PruebasFabrica)
                }
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Blob FALLO: " + e); }

            // --- 4) Modelo propio (F2): se lee la carpeta entera con el mismo lector que usa el juego en batalla
            //        (TJBakeVisualLoader.Load + GpuAnimVisualImport.From), sin crear entidades, y se libera ---
            // --- 4) Custom model (F2): the whole folder is read with the same reader the game uses in battle
            //        (TJBakeVisualLoader.Load + GpuAnimVisualImport.From), without creating entities, and then released ---
            if (UnitVisualOverrides.TryGet(UnidadGuardia.Guardia.ToString(), out string carpetaModelo))
            {
                total++;
                var propios = new Il2CppSystem.Collections.Generic.List<UnityEngine.Object>();
                try
                {
                    var visual = MemoriStudios.TJBake.TJBakeVisualLoader.Load(carpetaModelo, propios, false, null);
                    var datos = GpuAnimVisualImport.From(visual);
                    int bow = 0, sword = 0, adjuntos = 0;
                    for (int v = 0; v < visual.Variants.Count; v++)
                        for (int a = 0; a < visual.Variants[v].Attachments.Count; a++)
                        {
                            adjuntos++;
                            var rol = visual.Variants[v].Attachments[a].Role;
                            if (rol == MemoriStudios.TJBake.TJBakePropRole.Bow) bow++;
                            else if (rol == MemoriStudios.TJBake.TJBakePropRole.Sword) sword++;
                        }
                    int nv = visual.Variants.Count;   // 1 a 3 variantes; cada una con un bow y un sword (regla de Eisenmann)
                                                      // 1 to 3 variants; each with a bow and a sword (Eisenmann's rule)
                    bool okM = datos != null && nv >= 1 && nv <= 3 && visual.Slots.Count == 15 && bow == nv && sword == nv;
                    string resumen = $"{visual.Variants.Count} variantes, {visual.Slots.Count} slots, {visual.BoneCount} huesos, " +
                                     $"{visual.FrameCount} frames, {adjuntos} adjuntos ({bow} bow, {sword} sword), {propios.Count} objetos de Unity";
                    if (okM) { ok++; Plugin.Info($"[autoprueba] Modelo propio OK: el lector del juego carga {carpetaModelo}: {resumen}."); }
                    else Plugin.Error($"[autoprueba] Modelo propio FALLO: cargado pero no cuadra ({resumen}).");
                }
                catch (Exception e) { Plugin.Error("[autoprueba] Modelo propio FALLO: el lector del juego lo rechaza: " + e.Message); }
                finally
                {
                    // libera mallas, texturas y materiales creados por la prueba
                    // releases meshes, textures and materials created by the test
                    for (int i = 0; i < propios.Count; i++)
                        if (propios[i] != null) UnityEngine.Object.Destroy(propios[i]);
                }
            }

            // --- 5) Sonido láser (F3): clips creados desde WAV con las funciones internas de Unity, lectura de vuelta
            //        con GetData y postfix de GetBattlefieldAudio activo; además se mide el nivel del disparo de Eisenmann ---
            //        Va aislada: la medida de nivel (solo informativa) y la comprobación tienen cada una su try/catch, y
            //        un fallo aquí solo suma un fallo a esta prueba (las anteriores ya están contadas).
            // Si la capa 1 (WAV) no funcionó (funciones internas sin resolver, centinela de una carga anterior que cerró el
            // juego, WAV ausentes...) no es un fallo de la prueba: se informa del motivo y quedan las capas 2 (prestado)
            // y 3 (base). En el menú los clips prestados casi nunca están cargados; eso tampoco cuenta.
            // --- 5) Laser sound (F3): clips created from WAV with Unity's internal functions, read back
            //        with GetData and the GetBattlefieldAudio postfix active; the level of Eisenmann's shot is also measured ---
            //        It is isolated: the level measurement (informational only) and the check each have their own try/catch, and
            //        a failure here only adds one failure to this test (the previous ones are already counted).
            // If layer 1 (WAV) didn't work (unresolved internal functions, a sentinel from a previous load that crashed the
            // game, missing WAVs...) it is not a test failure: the reason is reported and layers 2 (borrowed)
            // and 3 (base) remain. In the menu the borrowed clips are almost never loaded; that doesn't count either.
            if (Plugin.CfgSonidoPropio != null && Plugin.CfgSonidoPropio.Value && AudioGuardia.NumDisparos == 0)
                Plugin.Aviso($"[autoprueba] Audio sin láser propio desde WAV (no cuenta): {AudioGuardia.MotivoSinPropios ?? "sin clips"}. " +
                             $"En batalla se usará un clip prestado del juego si está cargado (ahora en memoria: {AudioGuardia.ResumenPrestados()}) o el sonido base.");
            else if (Plugin.CfgSonidoPropio != null && Plugin.CfgSonidoPropio.Value)
            {
                total++;
                try
                {
                    // Nivel de los disparos de Eisenmann (referencia para igualar volumen) y de los propios (informativo)
                    // Level of Eisenmann's shots (reference to match volume) and of our own (informational)
                    var fps = AssetsGuardia.LeerEntrada(data.SquadAssetsDictionary, UnidadGuardia.Base)?.fireProjectileSFX;
                    var clipsBase = fps == null ? null : fps.fireProjectileSFX;
                    if (clipsBase == null) Plugin.Info("[autoprueba] Audio Eisenmann: sin FireProjectileSFX legible (nivel sin medir).");
                    for (int i = 0; clipsBase != null && i < clipsBase.Length && i < 6; i++)
                        Plugin.Info($"[autoprueba] Audio Eisenmann disparo {i + 1}: {AudioGuardia.Medir(clipsBase[i])}");
                    foreach (var t in LogicaSonidos.TiposConWav)
                        for (int i = 0; i < AudioGuardia.Propios(t).Count; i++)
                            Plugin.Info($"[autoprueba] Audio propio [{LogicaSonidos.Nombre(t)}] disparo {i + 1}: {AudioGuardia.Medir(AudioGuardia.Propios(t)[i])}");
                }
                catch (Exception e) { Plugin.Aviso("[autoprueba] Audio: no se pudo medir el nivel (no cuenta como fallo): " + e.Message); }

                try
                {
                    // Muestras escritas de verdad: se leen 32 muestras del primer clip y deben no ser todas cero
                    // Samples actually written: 32 samples are read from the first clip and they must not all be zero
                    bool datosOk = false;
                    var c = AudioGuardia.Disparos[0];
                    var buf = new float[32];
                    if (c != null && AudioGuardia.LeerMuestras(c, buf, c.samples / 4))
                        for (int i = 0; i < 32; i++) if (buf[i] != 0f) { datosOk = true; break; }
                    // El postfix responde: la 900 debe dar un clip láser propio al disparar y (si está activado) al impactar
                    // The postfix responds: 900 must give its own laser clip when firing and (if enabled) on impact
                    var disparo = data.GetBattlefieldAudio(UnidadGuardia.Guardia, Memori.Audio.SFXEntityType.FireProjectile);
                    var impacto = data.GetBattlefieldAudio(UnidadGuardia.Guardia, Memori.Audio.SFXEntityType.ProjectileHit);
                    bool okDisparo = disparo != null && disparo.name.StartsWith(AudioGuardia.PrefijoDisparo);
                    bool quiereImpacto = Plugin.CfgImpactoPropio.Value && AudioGuardia.NumImpactos > 0;
                    bool okImpacto = !quiereImpacto || (impacto != null && impacto.name.StartsWith(AudioGuardia.PrefijoImpacto));
                    // Informativo: el disparo de cada unidad nueva con sonido de energía (qué clip le toca ahora)
                    // Informational: the shot of each new unit with energy sound (which clip it gets now)
                    foreach (var u in UnidadGuardia.Todas)
                    {
                        var tipo = AudioGuardia.TipoDe(u.Id);
                        if (tipo == TipoSonido.Base) continue;
                        // Cada consulta con su try: las unidades cuerpo a cuerpo (infantería de choque, andador ligero) pueden no tener sonido de disparo
                        // y el método del juego podría lanzar; eso no debe tapar el veredicto de abajo
                        // Each query in its own try: melee units (shock infantry, light walker) may have no firing sound
                        // and the game's method could throw; that must not mask the verdict below
                        try
                        {
                            var d = data.GetBattlefieldAudio(u.Id, Memori.Audio.SFXEntityType.FireProjectile);
                            Plugin.Info($"[autoprueba] Audio {u.Numero} [{LogicaSonidos.Nombre(tipo)}]: disparo = {(d == null ? "null" : d.name)}");
                        }
                        catch (Exception) { Plugin.Info($"[autoprueba] Audio {u.Numero} [{LogicaSonidos.Nombre(tipo)}]: sin sonido de disparo (unidad cuerpo a cuerpo)"); }
                    }
                    Plugin.Info($"[autoprueba] Audio: carpetas {AudioGuardia.CarpetaUsada ?? "(ninguna)"}, láser {AudioGuardia.NumPropios(TipoSonido.Laser)}, " +
                                $"bláster {AudioGuardia.NumPropios(TipoSonido.Blaster)}, pesado {AudioGuardia.NumPropios(TipoSonido.Pesado)}, " +
                                $"{AudioGuardia.NumImpactos} impactos, muestras legibles {datosOk}, disparo de la 900 = {(disparo == null ? "null" : disparo.name)}, " +
                                $"impacto de la 900 = {(impacto == null ? "null" : impacto.name)}");
                    if (datosOk && okDisparo && okImpacto) { ok++; Plugin.Info("[autoprueba] Audio OK: las armas de energía usan el láser propio desde WAV."); }
                    else Plugin.Error("[autoprueba] Audio FALLO: hay clips propios pero la 900 no los usa (ver líneas anteriores).");
                }
                catch (Exception e) { Plugin.Error("[autoprueba] Audio FALLO: " + e); }
            }

        }

        // Pruebas 6-26 (fábrica, efectos, facciones y objetos), según los módulos aplicados
        // Tests 6-26 (factory, effects, factions and gear), depending on the modules applied
        private static void PruebasFabrica(TabletopTavernData data, ref int ok, ref int total)
        {
            // ===================================================================
            // Pruebas de la 0.4.0: fábrica de unidades, escuadras, bonus del jugador y saves con unidades 9xx
            // 0.4.0 tests: unit factory, squads, player bonus and saves with 9xx units
            // ===================================================================

            // --- 6a) Sin módulos de contenido: ninguna unidad 9xx en el catálogo ni en las listas (juego de serie) ---
            // --- 6a) Without content modules: no 9xx unit in the catalog or in the lists (vanilla game) ---
            if (UnidadGuardia.Todas.Count == 0)
            {
                try
                {
                    int nuevas = 0, enListas = 0;
                    foreach (UnitName k in data.SquadStatsDictionary.Keys) if ((int)k >= 900 && (int)k <= 999) nuevas++;
                    foreach (Race r in Enum.GetValues(typeof(Race)))
                        if (data.UnitsOfRaceDictionary.ContainsKey(r))
                            for (int i = 0; i < data.UnitsOfRaceDictionary[r].Count; i++) if ((int)data.UnitsOfRaceDictionary[r][i] >= 900) enListas++;
                    bool razas = data.GetRaceData((Race)40) == null && data.GetRaceData((Race)41) == null;
                    Linea(nuevas == 0 && enListas == 0 && razas && !Plugin.Operativo, "Catálogo de serie (sin módulos de contenido)",
                        $"unidades 900-999 en el catálogo {nuevas} (esperado 0), en listas de raza {enListas} (esperado 0), RaceData 40/41 nulos {razas}, " +
                        $"fábrica sin parches {!Plugin.Operativo}", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Catálogo de serie (sin módulos de contenido)", e.ToString(), ref ok, ref total); }
            }

            // --- 6b) Blob de stats sin la 900 (con la 900 va en las pruebas de la Guardia) ---
            // --- 6b) Stats blob without 900 (with 900 it is in the Guard tests) ---
            if (UnidadGuardia.AlgunaRegistrada() && !UnidadGuardia.RegistradaEnCatalogo() && Unity.Entities.World.DefaultGameObjectInjectionWorld != null)
            {
                try
                {
                    int antes = data.SquadStatsDictionary.Count;
                    data.LoadAndInjectData();
                    int despues = data.SquadStatsDictionary.Count;
                    Linea(despues == antes && UnidadGuardia.AlgunaRegistrada(), "Blob", $"construido con las unidades nuevas de los módulos y diccionario de vuelta a {despues} unidades", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Blob", e.ToString(), ref ok, ref total); }
            }

            // --- 6) Unidades 9xx registradas: en el catálogo (datos) y en las listas de raza (activas) ---
            // --- 6) Registered 9xx units: in the catalog (data) and in the race lists (active) ---
            if (UnidadGuardia.Todas.Count > 0) total++;
            if (UnidadGuardia.Todas.Count > 0)
            try
            {
                var stats = data.SquadStatsDictionary;
                var listas = data.UnitsOfRaceDictionary;
                int enCatalogo = 0, enSuLista = 0, debenLista = 0, fueraBien = 0, debenFuera = 0, activas = 0;
                foreach (var u in UnidadGuardia.Todas)
                {
                    if (stats.ContainsKey(u.Id)) enCatalogo++;
                    if (u.Datos.Activa) activas++;
                    // ¿En qué listas de raza está?
                    // Which race lists is it in?
                    int listasDondeEsta = 0;
                    bool enLaSuya = false;
                    foreach (Race r in System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Concat(Enum.GetValues(typeof(Race)).Cast<Race>(), FaccionesNuevas.RazasNuevas)))
                    {
                        if (!listas.ContainsKey(r) || !listas[r].Contains(u.Id)) continue;
                        listasDondeEsta++;
                        if (u.Raza.HasValue && r == u.Raza.Value) enLaSuya = true;
                    }
                    if (u.EnLista) { debenLista++; if (enLaSuya && listasDondeEsta == 1) enSuLista++; }
                    else { debenFuera++; if (listasDondeEsta == 0) fueraBien++; }
                }
                int totalManifiesto = UnidadGuardia.Todas.Count;
                string resumen = $"en el catálogo {enCatalogo}/{totalManifiesto}, activas {activas}, en la lista de su facción {enSuLista}/{debenLista}, " +
                                 $"fuera de toda lista (900 y solo-datos) {fueraBien}/{debenFuera}, assets verificados {AssetsGuardia.NumeroValidas}/{totalManifiesto}";
                if (enCatalogo == totalManifiesto && enSuLista == debenLista && fueraBien == debenFuera && AssetsGuardia.NumeroValidas == totalManifiesto)
                { ok++; Plugin.Info($"[autoprueba] Unidades 9xx OK: {resumen}."); }
                else Plugin.Error($"[autoprueba] Unidades 9xx FALLO: {resumen}.");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Unidades 9xx FALLO: " + e); }

            // --- 7) Una suelta concreta: 944 Ogros de Asedio (Legión de Hierro, clon del Oni de Sakura); módulo de sueltas ---
            // --- 7) A specific standalone: 944 Siege Ogres (Iron Legion, clone of Sakura's Oni); standalone module ---
            bool hay944 = UnidadGuardia.Registrada(944);
            if (!hay944) Plugin.Info("[autoprueba] Suelta 944: no está en el catálogo (módulo de unidades sueltas no aplicado); no se comprueba.");
            if (hay944) total++;
            if (hay944)
            try
            {
                var id = (UnitName)944;
                var u = UnidadGuardia.Datos(id);
                var s = data.SquadStatsDictionary[id];
                var a = AssetsGuardia.LeerEntrada(data.SquadAssetsDictionary, id);
                bool enLista = data.UnitsOfRaceDictionary.ContainsKey(Race.IronLegion) && data.UnitsOfRaceDictionary[Race.IronLegion].Contains(id);
                UnitVisualOverrides.TryGetBuiltIn("944", out string carpeta);
                bool tieneBase = EjercitoReforzado.ValoresBase(id, out int sold0, out int mun0);
                var icono = data.GetUnitIcon(id);
                string prefab = data.GetRecruitmentPrefabKey(id);
                // Stats esperados: los del unit_overrides.json del módulo que trae la 944 (no números fijos: el equilibrio
                // los cambia, p. ej. el 18-10 los soldados de base pasaron de 8 a 10). Los soldados, con el multiplicador.
                // Expected stats: those from the unit_overrides.json of the module that brings 944 (not fixed numbers: balancing
                // changes them, e.g. on 18-10 the base soldiers went from 8 to 10). The soldiers, with the multiplier.
                var ov = Modulos.LeerOverrideUnidad(u?.Datos?.Carpeta, 944);
                string V(string campo) => ov != null && ov.TryGetValue(campo, out var t) ? t : null;
                bool Entero(string campo, int real) => int.TryParse(V(campo), out int e) && e == real;
                bool Bandera(string campo, bool real) => bool.TryParse(V(campo), out bool e) && e == real;
                bool Texto(string campo, string real) => string.Equals(V(campo), real, StringComparison.OrdinalIgnoreCase);
                bool cadenciaOk = float.TryParse(V("attackCooldown"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float cad)
                                  && Math.Abs(s.attackCooldown - cad) < 0.01f;
                int soldEsperados = int.TryParse(V("baseUnitCount"), out int soldMod) ? EjercitoReforzado.EnCampo(soldMod) : -1;
                // Soldados: en el catálogo, la base del módulo con el multiplicador de escuadras; y, si se escaló, la base
                // guardada antes de escalar tiene que ser la del módulo
                // Soldiers: in the catalog, the module's base with the squad multiplier; and, if scaled, the base
                // saved before scaling must be the module's
                bool statsOk = ov != null && s.baseUnitCount == soldEsperados && (!tieneBase || sold0 == soldMod) && Entero("hitPointsPerUnit", s.HitPointsPerUnit)
                               && Entero("meleeAttack", s.MeleeAttack) && Entero("meleeDefense", s.MeleeDefense)
                               && Entero("weaponStrength", s.WeaponStrength) && Entero("armor", s.Armor) && cadenciaOk
                               && Entero("explosionDamage", s.ExplosionDamage) && Bandera("armorPiercing", s.SquadAttributes.ArmorPiercing)
                               && Bandera("terrifying", s.SquadAttributes.Terrifying) && Bandera("antiInfantry", s.SquadAttributes.AntiInfantry)
                               && Texto("rarityTier", s.RarityTier.ToString()) && Texto("unitSize", s.unitSize.ToString());
                string esperado = ov == null ? "sin entrada 944 en el unit_overrides.json del módulo" :
                    $"módulo: soldados {V("baseUnitCount")} (en campo {soldEsperados}), vida {V("hitPointsPerUnit")}, ataque {V("meleeAttack")}/{V("meleeDefense")}, " +
                    $"fuerza {V("weaponStrength")}, armadura {V("armor")}, cadencia {V("attackCooldown")}, explosión {V("explosionDamage")}, " +
                    $"perforante {V("armorPiercing")}, aterrador {V("terrifying")}, antiinfantería {V("antiInfantry")}, rareza {V("rarityTier")}, tamaño {V("unitSize")}";
                string detalle = $"base {u?.Base}, raza {a?.race}, en lista IronLegion {enLista}, soldados {s.baseUnitCount} (base {(tieneBase ? sold0 : s.baseUnitCount)}), " +
                                 $"vida {s.HitPointsPerUnit}, ataque {s.MeleeAttack}/{s.MeleeDefense}, fuerza {s.WeaponStrength}, armadura {s.Armor}, " +
                                 $"cadencia {s.attackCooldown}, explosión {s.ExplosionDamage}, perforante {s.SquadAttributes.ArmorPiercing}, " +
                                 $"aterrador {s.SquadAttributes.Terrifying}, antiinfantería {s.SquadAttributes.AntiInfantry}, rareza {s.RarityTier}, " +
                                 $"tipo {s.unitType}/{s.unitSize}, visual {carpeta}, icono {(icono == null ? "null" : icono.name)}, prefab {prefab}; esperado del {esperado}";
                bool okS = u != null && u.Base == UnitName.Oni && a != null && a.race == Race.IronLegion && enLista && statsOk
                           && !string.IsNullOrEmpty(carpeta) && carpeta.Replace('\\', '/').EndsWith("/Oni") && icono != null && !string.IsNullOrEmpty(prefab);
                if (okS) { ok++; Plugin.Info($"[autoprueba] Suelta 944 OK: {detalle}."); }
                else Plugin.Error($"[autoprueba] Suelta 944 FALLO: {detalle}.");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Suelta 944 FALLO: " + e); }

            // --- 8) Efecto global en una unidad de serie (PeasantBowmen, 48 soldados y 750 de munición de serie) y en la 900 ---
            // --- 8) Global effect on a vanilla unit (PeasantBowmen, 48 soldiers and 750 vanilla ammo) and on 900 ---
            total++;
            try
            {
                var pb = UnitName.PeasantBowmen;
                var sp = data.SquadStatsDictionary[pb];
                if (EjercitoReforzado.Factor <= 1.0)
                {
                    // Sin módulo de escuadras: nada escalado (PeasantBowmen con sus soldados de serie)
                    // Without squads module: nothing scaled (PeasantBowmen with their vanilla soldiers)
                    bool okS = EjercitoReforzado.Escaladas == 0 && !EjercitoReforzado.ValoresBase(pb, out _, out _);
                    string detalle = $"sin módulo de escuadras: {EjercitoReforzado.Escaladas} unidades escaladas (esperado 0), PeasantBowmen {sp.baseUnitCount} soldados y {sp.Ammunition} de munición (de serie)";
                    if (okS) { ok++; Plugin.Info($"[autoprueba] Escuadras de serie OK: {detalle}."); }
                    else Plugin.Error($"[autoprueba] Escuadras de serie FALLO: {detalle}.");
                }
                else
                {
                    bool hayP = EjercitoReforzado.ValoresBase(pb, out int bS, out int bM);
                    int espP = EjercitoReforzado.EnCampo(bS), espPM = bS > 0 ? (int)(bM * (double)espP / bS + 0.5) : 0;
                    string detalle = $"x{EjercitoReforzado.Factor:0.##}: PeasantBowmen soldados {bS} -> {sp.baseUnitCount} (esperado {espP}), munición {bM} -> {sp.Ammunition} (esperada {espPM})";
                    bool okE = hayP && sp.baseUnitCount == espP && sp.Ammunition == espPM;
                    if (UnidadGuardia.RegistradaEnCatalogo())
                    {
                        var sg = data.SquadStatsDictionary[UnidadGuardia.Guardia];
                        bool hayG = EjercitoReforzado.ValoresBase(UnidadGuardia.Guardia, out int gS, out int gM);
                        int espG = EjercitoReforzado.EnCampo(20), espGM = (int)(1100 * (double)espG / 20 + 0.5);
                        detalle += $"; 900 soldados {gS} -> {sg.baseUnitCount} (esperado {espG}), munición {gM} -> {sg.Ammunition} (esperada {espGM})";
                        okE &= hayG && gS == 20 && gM == 1100 && sg.baseUnitCount == espG && sg.Ammunition == espGM;
                    }
                    detalle += $"; {EjercitoReforzado.Escaladas} unidades escaladas";
                    if (okE) { ok++; Plugin.Info($"[autoprueba] Escuadras (soldados y munición) OK: {detalle}."); }
                    else Plugin.Error($"[autoprueba] Escuadras (soldados y munición) FALLO: {detalle}.");
                }
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Escuadras (soldados y munición) FALLO: " + e); }

            // --- 9) Reglas del jugador según los módulos de munición y stats a distancia (0.7.1: solo para el héroe activo;
            //        ninguna para otro héroe, sin héroe ni con un señor de la guerra del mismo héroe) ---
            // --- 9) Player rules according to the ammo and ranged stats modules (0.7.1: only for the active hero;
            //        none for another hero, without a hero or with a warlord of the same hero) ---
            total++;
            try
            {
                // Se usa el evaluador que leen los sistemas ECS (HeroBonusRuleEvaluator): no pide textos al
                // LocalizationManager (pedirlos en el arranque duplicaba la carga de los textos del juego). En el menú no hay
                // héroe activo: se fuerza uno con EjercitoReforzado.ForzarParaPrueba (recarga las reglas con el juego).
                // The evaluator read by the ECS systems is used (HeroBonusRuleEvaluator): it does not request texts from
                // LocalizationManager (requesting them at startup duplicated the loading of the game's texts). In the menu there is no
                // active hero: one is forced with EjercitoReforzado.ForzarParaPrueba (reloads the rules with the game).
                var cat = data.SquadStatsDictionary;
                var arquero = cat[UnitName.PeasantBowmen];
                var canon = cat[UnitName.KaiserCannon];
                const int activo = 9;      // Morvayne: sin reglas propias de alcance, precisión, daño ni munición
                                           // Morvayne: no own rules for range, accuracy, damage or ammo
                const int otro = 1;        // Edric: sin regla propia de alcance para los arqueros
                                           // Edric: no own range rule for archers
                float Bono(UnitStat st, UnitName u, int heroe, SquadStats s, float actual) =>
                    HeroBonusRuleEvaluator.SumHeroStatBonus(st, u, heroe, s, Race.Gruntkin, actual);
                float espAlc = EjercitoReforzado.ExtraAlcance, espPre = EjercitoReforzado.ExtraPrecision, espDan = EjercitoReforzado.ExtraDano;
                float espMun = 40f * (EjercitoReforzado.MultiplicadorMunicion - 1);

                // a) Héroe activo 9 sin señor de la guerra: él recibe todo; Edric y «sin héroe» (la IA) nada
                // a) Active hero 9 without warlord: he gets everything; Edric and «no hero» (the AI) get nothing
                EjercitoReforzado.ForzarParaPrueba(activo, 0);
                int reglasA = EjercitoReforzado.ReglasInyectadas, esperadasA = EjercitoReforzado.ReglasEsperadas;
                float alc = Bono(UnitStat.Range, UnitName.PeasantBowmen, activo, arquero, 75f);
                float pre = Bono(UnitStat.Accuracy, UnitName.PeasantBowmen, activo, arquero, 50f);
                float dan = Bono(UnitStat.MissileStrength, UnitName.PeasantBowmen, activo, arquero, 10f);
                float mun = Bono(UnitStat.Ammunition, UnitName.KaiserCannon, activo, canon, 40f);
                float alcCanon = Bono(UnitStat.Range, UnitName.KaiserCannon, activo, canon, 125f);
                float alcOtro = Bono(UnitStat.Range, UnitName.PeasantBowmen, otro, arquero, 75f);
                float alcIa = Bono(UnitStat.Range, UnitName.PeasantBowmen, -1, arquero, 75f);
                bool okA = reglasA == esperadasA && Math.Abs(alc - espAlc) < 0.01f && Math.Abs(pre - espPre) < 0.01f && Math.Abs(dan - espDan) < 0.01f
                           && Math.Abs(mun - espMun) < 0.01f && Math.Abs(alcCanon) < 0.01f && Math.Abs(alcOtro) < 0.01f && Math.Abs(alcIa) < 0.01f;

                // b) Héroe nuevo de un módulo de facción (901-904, id por encima de 200) como héroe activo
                // b) New hero from a faction module (901-904, id above 200) as active hero
                bool okN = true; string detalleN = "sin héroes nuevos (no se comprueba)";
                if (FaccionesNuevas.Heroes.Count > 0)
                {
                    int nuevo = FaccionesNuevas.Heroes.Keys.Min();
                    EjercitoReforzado.ForzarParaPrueba(nuevo, 0);
                    float preN = Bono(UnitStat.Accuracy, UnitName.PeasantBowmen, nuevo, arquero, 50f);
                    float preActivoAntes = Bono(UnitStat.Accuracy, UnitName.PeasantBowmen, activo, arquero, 50f);
                    okN = Math.Abs(preN - espPre) < 0.01f && Math.Abs(preActivoAntes) < 0.01f;
                    detalleN = $"con el héroe {nuevo} activo: precisión +{preN} (esperado +{espPre}) y el héroe {activo} +{preActivoAntes} (esperado 0)";
                }

                // c) Señor de la guerra enemigo con el mismo héroe: no se pone ninguna (si no, el enemigo también las tendría)
                // c) Enemy warlord with the same hero: none is applied (otherwise the enemy would have them too)
                EjercitoReforzado.ForzarParaPrueba(activo, activo);
                int reglasC = EjercitoReforzado.ReglasInyectadas;
                float alcC = Bono(UnitStat.Range, UnitName.PeasantBowmen, activo, arquero, 75f);
                bool okC = reglasC == 0 && Math.Abs(alcC) < 0.01f;

                string detalle = $"munición x{EjercitoReforzado.MultiplicadorMunicion}, distancia +{espAlc}/+{espPre}/+{espDan}; con el héroe {activo} activo: {reglasA}/{esperadasA} reglas, " +
                                 $"arquero alcance +{alc}, precisión +{pre}, daño +{dan} (esperados +{espAlc}/+{espPre}/+{espDan}), cañón munición +{mun} sobre 40 (esperado +{espMun}) y alcance +{alcCanon} (esperado 0); " +
                                 $"Edric (no activo) alcance +{alcOtro} y sin héroe (IA) +{alcIa} (esperados 0); {detalleN}; " +
                                 $"señor de la guerra del mismo héroe: {reglasC} reglas, alcance +{alcC} (esperados 0)";
                bool okR = okA && okN && okC;
                if (okR) { ok++; Plugin.Info($"[autoprueba] Reglas del jugador OK: {detalle}."); }
                else Plugin.Error($"[autoprueba] Reglas del jugador FALLO: {detalle}.");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Reglas del jugador FALLO: " + e); }
            finally
            {
                // Vuelve al estado real del juego (en el menú: sin héroe, sin reglas del jugador)
                // Returns to the real game state (in the menu: no hero, no player rules)
                try { EjercitoReforzado.ForzarParaPrueba(null); } catch (Exception e) { Plugin.Error("[autoprueba] No se pudo devolver las reglas del jugador al estado real: " + e.Message); }
            }

            // --- 10) Ida y vuelta de un save con una unidad 9xx (944 -> se guarda como Oni), registro solo en memoria ---
            // --- 10) Save round trip with a 9xx unit (944 -> saved as Oni), registry in memory only ---
            if (hay944) total++;
            var registroReal10 = Saves.Registro;
            if (hay944) Saves.Registro = new RegistroGuardias(null);
            if (hay944)
            try
            {
                var id = (UnitName)944;
                var escuadra = new SquadToLoad(id, 0, -1, false, 1f);
                escuadra.UniqueID = GuidPrueba944;
                string texto = UnityEngine.JsonUtility.ToJson(escuadra, true);
                int oni = (int)UnitName.Oni;
                bool salida = texto.Contains("\"UnitName\": " + oni + ",") && SaneadoSaves.ContarResiduos(texto, Saves.Tabla) == 0
                              && Saves.Registro.IdDeGuid(GuidPrueba944) == 944;
                UnitName v1 = new SquadToLoad(UnityEngine.JsonUtility.FromJson(texto, Il2CppType.Of<SquadToLoad>()).Pointer).UnitName;
                UnitName v2 = UnityEngine.JsonUtility.FromJson<SquadToLoad>(texto).UnitName;
                string detalle = $"en disco {texto.Replace("\n", " ").Replace("    ", "")}; sin 9xx y GUID apuntado: {salida}; lectura FromJson(string,Type): {(int)v1}, FromJson<T>: {(int)v2}";
                if (salida && v1 == id && v2 == id) { ok++; Plugin.Info($"[autoprueba] Saves 9xx OK: {detalle}."); }
                else Plugin.Error($"[autoprueba] Saves 9xx FALLO: {detalle}.");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Saves 9xx FALLO: " + e); }
            finally { Saves.Registro = registroReal10; }

            // --- 11) El mod antiguo ZeroMunicionAlcance no debe seguir activo (escalaría dos veces) ---
            // --- 11) The old ZeroMunicionAlcance mod must not remain active (it would scale twice) ---
            total++;
            try
            {
                if (!EjercitoReforzado.ModViejoActivo()) { ok++; Plugin.Info("[autoprueba] Mod viejo OK: ZeroMunicionAlcance no está activo; el efecto global se aplica solo una vez."); }
                else Plugin.Error("[autoprueba] Mod viejo FALLO: ZeroMunicionAlcance sigue activo; el efecto global está parado para no escalar dos veces.");
            }
            catch (Exception e) { Plugin.Error("[autoprueba] Mod viejo FALLO: " + e); }

            // ===================================================================
            // Pruebas de la 0.5.0: facciones nuevas (F2) y facción A en la facción A (F3)
            // 0.5.0 tests: new factions (F2) and faction A in faction A (F3)
            // ===================================================================
            if (!FaccionesNuevas.Activas)
                Plugin.Info("[autoprueba] Facciones nuevas: ningún módulo de facción aplicado; no se comprueban.");
            else
                PruebasFacciones(data, ref ok, ref total);

            // ===================================================================
            // Pruebas de la 0.6.0: objetos nuevos (gear)
            // 0.6.0 tests: new gear
            // ===================================================================
            if (!Objetos.Operativo || !Objetos.ActivosEnCache)
                Plugin.Info($"[autoprueba] Objetos nuevos: no activos en esta sesión (operativos {Objetos.Operativo}, {Objetos.Numero} en los módulos aplicados); no se comprueban.");
            else
                PruebasObjetos(data, ref ok, ref total);
        }
        // Comprueba una condición y escribe la línea OK/FALLO (suma a los contadores)
        // Checks a condition and writes the OK/FAIL line (adds to the counters)
        private static void Linea(bool bien, string nombre, string detalle, ref int ok, ref int total)
        {
            total++;
            if (bien) { ok++; Plugin.Info($"[autoprueba] {nombre} OK: {detalle}."); }
            else Plugin.Error($"[autoprueba] {nombre} FALLO: {detalle}.");
        }

        // Pruebas 12-19 de la F2/F3 (cada una aislada en su try/catch)
        // F2/F3 tests 12-19 (each isolated in its own try/catch)
        private static void PruebasFacciones(TabletopTavernData data, ref int ok, ref int total)
        {
            var nuevas = FaccionesNuevas.RazasNuevas.ToList();

            // --- 12) RaceData de las razas nuevas (riesgo ALTO: GetRaceData nulo cierra el juego) ---
            // --- 12) RaceData of the new races (HIGH risk: null GetRaceData crashes the game) ---
            try
            {
                var partes = new List<string>();
                bool bien = nuevas.Count == FaccionesNuevas.Razas.Count && nuevas.Count > 0;
                foreach (Race r in nuevas)
                {
                    var d = data.GetRaceData(r);
                    var c = UnityEngine.Color.clear;
                    bool esta = d != null && (int)d.Race == (int)r;
                    if (d != null) c = d.PrimaryColor;
                    var colorUi = ColorData.GetRaceDisplayColor(r);
                    var gris = ColorData.GetRaceDisplayColor(Race.Special);
                    bool colorPropio = colorUi != gris;
                    bien &= esta && colorPropio && d.RaceBasePrefab != null;
                    partes.Add($"{(int)r}: {(d == null ? "null" : d.name)}, raza {(d == null ? -1 : (int)d.Race)}, color {c.r:0.00}/{c.g:0.00}/{c.b:0.00}, " +
                               $"peana {(d?.RaceBasePrefab == null ? "null" : d.RaceBasePrefab.name)}, región {(d?.MapRegion == null ? "null" : d.MapRegion.name)}, color de interfaz propio {colorPropio}");
                }
                Linea(bien, "RaceData de las razas nuevas", string.Join("; ", partes), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "RaceData de las razas nuevas", e.ToString(), ref ok, ref total); }

            // --- 13) Unidades de las razas nuevas en la lista de su raza, en los 4 rangos ---
            // --- 13) Units of the new races in their race's list, in all 4 ranks ---
            try
            {
                var partes = new List<string>();
                bool bien = true;
                foreach (Race r in nuevas)
                {
                    var ids = new List<int>();
                    var rangos = new SortedSet<int>();
                    if (data.UnitsOfRaceDictionary.ContainsKey(r))
                    {
                        var l = data.UnitsOfRaceDictionary[r];
                        for (int i = 0; i < l.Count; i++) { ids.Add((int)l[i]); rangos.Add(data.GetUnitTierFromUnitName(l[i])); }
                    }
                    int esperadas = UnidadGuardia.Todas.Count(u => u.EnLista && u.Raza == r);
                    bool todos = rangos.SetEquals(new[] { 1, 2, 3, 4 }) && ids.Count == esperadas && esperadas > 0;
                    bien &= todos;
                    partes.Add($"raza {(int)r}: {ids.Count}/{esperadas} unidades [{string.Join(",", ids)}], rangos {string.Join(",", rangos)}");
                }
                Linea(bien, "Unidades de las razas nuevas", string.Join("; ", partes), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Unidades de las razas nuevas", e.ToString(), ref ok, ref total); }

            // --- 14) Generación de razas: actos de campaña, infinitos, Marcha, ciudades y general rival ---
            // --- 14) Race generation: campaign acts, endless, March, towns and rival general ---
            try
            {
                const int Semillas = 300;
                int acto2 = 0, acto3 = 0, infinito = 0, ciudades = 0, propias = 0;
                int semillaActo2 = -1;
                for (int s = 0; s < Semillas; s++)
                {
                    Race a2 = data.GenerateRaceForMap(2, s, Race.Gruntkin);
                    if ((int)a2 == 40) { acto2++; if (semillaActo2 < 0) semillaActo2 = s; }
                    if ((int)data.GenerateRaceForMap(3, s, Race.Gruntkin) == 41) acto3++;
                    if (FaccionesNuevas.EsNueva(data.GenerateRaceForMap(6, s, Race.Gruntkin))) infinito++;
                    // Juego 1.11.1: GenerateTownRace(nodo, semilla de la campaña, acto); revisión: nodo y semilla varían a la vez
                    // (300 semillas de campaña distintas), no solo el nodo con la semilla 0
                    // Game 1.11.1: GenerateTownRace(node, campaign seed, act); review: node and seed vary at the same time
                    // (300 different campaign seeds), not just the node with seed 0
                    if (FaccionesNuevas.EsNueva(CampaignSaveManager.GenerateTownRace(s % 30, s * 7919 + 13, 1))) ciudades++;
                    // Jugando con la facción A, el acto 2 nunca debe dar su propia raza
                    // When playing as faction A, act 2 must never give its own race
                    if ((int)data.GenerateRaceForMap(2, s, (Race)40) == 40) propias++;
                }
                int anfitriones = FaccionesNuevas.AnfitrionesNuevos();
                bool hay40 = FaccionesNuevas.Razas.ContainsKey(40), hay41 = FaccionesNuevas.Razas.ContainsKey(41);
                string general = hay40 ? "(sin semilla con Astra Militarum en el acto 2)" : "(sin Astra Militarum: no se comprueba)";
                bool generalOk = !hay40;
                if (hay40 && semillaActo2 >= 0)
                {
                    var rival = data.GetEnemyHeroForCampaign(1, 2, semillaActo2, false);
                    generalOk = (rival.HeroID == 901 || rival.HeroID == 902) && (int)rival.Race == 40;
                    general = $"con Edric, acto 2, semilla {semillaActo2}: héroe rival {rival.HeroID} de la raza {(int)rival.Race}";
                }
                int senorId = -1;
                bool senorOk = !hay41;
                if (hay41) { var senor = data.GetHeroLeadingRace((Race)41, 7); senorId = senor.HeroID; senorOk = senorId == 903 || senorId == 904; }
                // Sin la raza de un acto, ese acto no debe darla nunca (y con ella, alguna vez)
                // Without an act's race, that act must never give it (and with it, sometimes)
                bool bien = (hay40 ? acto2 > 0 : acto2 == 0) && (hay41 ? acto3 > 0 : acto3 == 0) && infinito > 0 && ciudades > 0
                            && propias == 0 && anfitriones == nuevas.Count && generalOk && senorOk;
                Linea(bien, "Generación de razas nuevas",
                    $"de {Semillas} semillas: acto 2 con Astra Militarum {acto2}, acto 3 con Imperio Galáctico {acto3}, acto 6 (infinito) con una nueva {infinito}, " +
                    $"ciudades nuevas {ciudades} ({ciudades * 100 / Semillas} %), acto 2 con la propia raza 40 {propias} (esperado 0); anfitriones de la Marcha nuevos {anfitriones}; " +
                    $"general rival {general}; señor de la guerra del Imperio: héroe {(hay41 ? senorId.ToString() : "(sin Imperio: no se comprueba)")}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Generación de razas nuevas", e.ToString(), ref ok, ref total); }

            // --- 15) Bonus de los héroes nuevos (hero_bonus_rules.json del mod oficial) ---
            // --- 15) Bonuses of the new heroes (hero_bonus_rules.json from the official mod) ---
            try
            {
                var cat = data.SquadStatsDictionary;
                var partes = new List<string>();
                bool bien = true;
                if (FaccionesNuevas.Razas.ContainsKey(40))
                {
                    float y = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.MeleeAttack, (UnitName)901, 901, cat[(UnitName)901], Race.Gruntkin, 29f);
                    // precisión de base de los Tiradores leída del catálogo cargado (no un literal: cambia con el equilibrio)
                    // base accuracy of the Shooters read from the loaded catalog (not a literal: it changes with balance)
                    float c = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.Accuracy, (UnitName)905, 902, cat[(UnitName)905], Race.Gruntkin, (float)cat[(UnitName)905].attackAccuracy);
                    // héroe 902: +6 propio y, si héroe 902 es el héroe de las reglas del jugador (0.7.1: solo el héroe activo; en el menú
                    // ninguno), la precisión del módulo de stats a distancia (si está aplicado y los Tiradores son Ranged)
                    // hero 902: +6 own and, if hero 902 is the hero of the player's rules (0.7.1: only the active hero; in the menu
                    // none), the accuracy of the ranged stats module (if applied and the Shooters are Ranged)
                    float esperadoC = 6f + (EjercitoReforzado.HeroesUsados.Contains(902) && EjercitoReforzado.Distancia != null && EjercitoReforzado.Distancia.Tipos.Contains(cat[(UnitName)905].unitType.ToString()) ? EjercitoReforzado.ExtraPrecision : 0f);
                    bien &= Math.Abs(y - 14f) < 0.01f && Math.Abs(c - esperadoC) < 0.01f;
                    partes.Add($"Yarrick, Krieg contra Gruntkin: ataque +{y} (esperado +14 = +4 comunes +10 Ojo Torvo); Creed, Tiradores Veteranos: precisión +{c} (esperado +{esperadoC})");
                }
                if (FaccionesNuevas.Razas.ContainsKey(41))
                {
                    float v = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.MeleeAttack, (UnitName)932, 903, cat[(UnitName)932], Race.Gruntkin, 49f);
                    float th = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.Armor, (UnitName)930, 904, cat[(UnitName)930], Race.Gruntkin, 120f);
                    bool firme = HeroBonusManager.UnitHasAttribute((UnitName)921, 903, UnitAttribute.Stalwart);
                    bien &= Math.Abs(v - 8f) < 0.01f && Math.Abs(th - 15f) < 0.01f && firme;
                    partes.Add($"Vader, Inquisidores: ataque +{v} (esperado +8); Thrawn, AT-ST: armadura +{th} (esperado +15); Vader, Soldados de Asalto firmes: {firme}");
                }
                Linea(bien && partes.Count > 0, "Bonus de los héroes nuevos", string.Join("; ", partes), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Bonus de los héroes nuevos", e.ToString(), ref ok, ref total); }

            // --- 16) Saneado de la raza de una ciudad 40 (ida y vuelta con JsonUtility, registro solo en memoria) ---
            // --- 16) Sanitizing of a town 40 race (round trip with JsonUtility, in-memory registry only) ---
            var registroReal = Saves.Registro;
            Saves.Registro = new RegistroGuardias(null);
            try
            {
                int rz = FaccionesNuevas.Razas.Keys.Min();
                var ciudad = new TownSaveData();
                ciudad.townName = "zeroCiudadPrueba";
                ciudad.townRace = (Race)rz;
                string texto = UnityEngine.JsonUtility.ToJson(ciudad, true);
                int sustituta = (int)FaccionesNuevas.RazaSerie(FaccionesNuevas.Razas[rz].RazaSustituta, Race.IronLegion);
                bool salida = texto.Contains("\"townRace\": " + sustituta) && SaneadoSaves.ContarResiduosRazas(texto, Saves.TablaRazas) == 0
                              && Saves.Registro.RazaDe("ciudad", out int apuntada, out _) && apuntada == rz;
                var vuelta = UnityEngine.JsonUtility.FromJson<TownSaveData>(texto);
                bool bien = salida && vuelta != null && (int)vuelta.townRace == rz;
                Linea(bien, "Saneado de la raza de ciudad",
                    $"en disco {texto.Replace("\n", " ").Replace("    ", "")}; raza {rz} guardada como {sustituta} y apuntada: {salida}; al leer: {(vuelta == null ? -1 : (int)vuelta.townRace)}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Saneado de la raza de ciudad", e.ToString(), ref ok, ref total); }
            finally { Saves.Registro = registroReal; }

            // --- 17) F3: la facción A (900) en la facción A ---
            // --- 17) F3: faction A (900) in faction A ---
            try
            {
                if (!FaccionesNuevas.GuardiaEnAstra)
                    Plugin.Info("[autoprueba] F3: desactivada (Facciones.GuardiaEnAstra = false); la 900 sigue en las ofertas del jugador.");
                else
                {
                    var listas = data.UnitsOfRaceDictionary;
                    bool enAstra = listas.ContainsKey((Race)40) && listas[(Race)40].Contains(UnidadGuardia.Guardia);
                    var deSerie = new List<int>();
                    foreach (Race r in Enum.GetValues(typeof(Race)))
                        if (listas.ContainsKey(r) && listas[r].Contains(UnidadGuardia.Guardia)) deSerie.Add((int)r);
                    var raza = data.GetRaceFromUnitName(UnidadGuardia.Guardia);
                    var s = data.SquadStatsDictionary[UnidadGuardia.Guardia];
                    bool bien = enAstra && deSerie.Count == 0 && (int)raza == 40 && !UnidadGuardia.GuardiaEnOfertas && AssetsGuardia.Valida(UnidadGuardia.Guardia)
                                && s.BaseRange == 100 && s.attackAccuracy == 80 && s.MissileStrength == 15
                                && s.SquadAttributes.ArmorPiercing && !s.SquadAttributes.AntiInfantry;
                    Linea(bien, "F3 Guardia Imperial en Astra Militarum",
                        $"en la lista de la raza 40 {enAstra}, en listas de serie [{string.Join(",", deSerie)}], raza {(int)raza}, ofertas del jugador {UnidadGuardia.GuardiaEnOfertas}, " +
                        $"balance aprobado por Zero el 17-10-2026 (alcance {s.BaseRange}, esperado 100; antiinfantería {s.SquadAttributes.AntiInfantry}, esperado False; " +
                        $"perforante {s.SquadAttributes.ArmorPiercing}; precisión {s.attackAccuracy}; daño {s.MissileStrength})", ref ok, ref total);
                }
            }
            catch (Exception e) { Linea(false, "F3 Guardia Imperial en Astra Militarum", e.ToString(), ref ok, ref total); }

            // --- 18) Retratos propios ---
            // --- 18) Custom portraits ---
            try
            {
                int cargados = HeroesNuevos.CargarRetratos();
                int hp = FaccionesNuevas.Heroes.Keys.Min();
                var tarea = data.LoadHeroSpriteAsync(hp);
                bool terminada = tarea != null && tarea.IsCompleted;
                var sprite = terminada ? tarea.Result : null;
                string nombre = sprite == null ? "null" : sprite.name;
                bool bien = cargados == FaccionesNuevas.Heroes.Count && nombre == "zeroRetrato" + hp;
                Linea(bien, "Retratos de los héroes nuevos",
                    $"{cargados}/{FaccionesNuevas.Heroes.Count} PNG cargados; LoadHeroSpriteAsync({hp}) terminada {terminada}, sprite {nombre} " +
                    $"({(sprite == null ? "-" : $"{sprite.rect.width}x{sprite.rect.height}")}), vía {HeroesNuevos.ViaRetrato ?? "(ninguna)"}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Retratos de los héroes nuevos", e.ToString(), ref ok, ref total); }

            // --- 19) Héroes tras una recarga de datos forzada (HeroData se vacía y se recarga en cada recarga) ---
            // --- 19) Heroes after a forced data reload (HeroData is emptied and reloaded on every reload) ---
            try
            {
                int altasAntes = HeroesNuevos.AltasSesion;
                data.ReloadData();
                var partes = new List<string>();
                bool bien = HeroesNuevos.AltasSesion > altasAntes;
                foreach (var h in FaccionesNuevas.Heroes.Values.OrderBy(x => x.Id))
                {
                    var hero = HeroData.GetHeroByID(h.Id);
                    bool esta = hero.HeroID == h.Id && (int)hero.Race == h.Raza && (int)hero.SignatureUnit == h.Emblema && hero.StartingGold > 0
                                && hero.HeroName == "zeroHeroe" + h.Id && data._heroAssetsData?.GetByID(h.Id) != null;
                    bien &= esta;
                    partes.Add($"{h.Id} (raza {(int)hero.Race}, emblema {(int)hero.SignatureUnit}, hechizo {hero.SignatureSpell}, oro {hero.StartingGold}, desbloqueo {hero.UnlockCondition}{(esta ? "" : ", MAL")})");
                }
                var porRaza = new List<string>();
                foreach (Race r in nuevas)
                {
                    int n = HeroData.GetHeroesByRace(r).Count;
                    bien &= n == 2;
                    porRaza.Add($"raza {(int)r}: {n}");
                }
                // Tras la recarga todo lo demás debe seguir: RaceData, listas y la 900
                // After the reload everything else must remain: RaceData, lists and the 900
                foreach (Race r in nuevas) bien &= data.GetRaceData(r) != null;
                if (FaccionesNuevas.Razas.ContainsKey(40)) bien &= UnidadGuardia.RegistradaEnCatalogo();
                Linea(bien, "Héroes tras recarga forzada",
                    $"altas en la sesión {altasAntes} -> {HeroesNuevos.AltasSesion}; {string.Join("; ", partes)}; héroes por raza: {string.Join(", ", porRaza)}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Héroes tras recarga forzada", e.ToString(), ref ok, ref total); }

            // --- 20) Razas jugables y sorteos condicionados (0.5.1): la primera raza aplicada se simula «no jugable» ---
            // --- 20) Playable races and conditioned draws (0.5.1): the first applied race is simulated as "not playable" ---
            try
            {
                Race r0 = nuevas.OrderBy(x => (int)x).First();
                Race? otra = nuevas.Where(x => x != r0).Select(x => (Race?)x).FirstOrDefault();
                // 0.7.6: el primer acto en el que sale (actosCampana; si no hay lista, actoCampana)
                // 0.7.6: the first act in which it appears (actosCampana; if there is no list, actoCampana)
                var m0 = FaccionesNuevas.Razas[(int)r0];
                int acto0 = Enumerable.Range(1, 3).FirstOrDefault(a => LogicaCampanaHeroes.SaleEnActo(a, m0.ActoCampana, m0.ActosCampana));
                if (acto0 == 0) acto0 = Math.Clamp(m0.ActoCampana, 1, 3);
                bool todasJug = nuevas.All(FaccionesNuevas.Jugable);
                bool f3Antes = FaccionesNuevas.GuardiaEnAstra;
                int actoSin = 0, ciudadesSin = 0, ciudadesOtra = 0;
                bool marchaSin = false, f3Sin = f3Antes;
                FaccionesNuevas.ForzarJugable(r0, false);
                try
                {
                    for (int s = 0; s < 300; s++)
                    {
                        if (data.GenerateRaceForMap(acto0, s, Race.Gruntkin) == r0) actoSin++;
                        Race c = CampaignSaveManager.GenerateTownRace(s % 30, s * 7919 + 13, 1);   // juego 1.11.1: (nodo, semilla, acto), varias semillas
                                                                                                   // game 1.11.1: (node, seed, act), several seeds
                        if (c == r0) ciudadesSin++;
                        if (otra.HasValue && c == otra.Value) ciudadesOtra++;
                    }
                    FaccionesNuevas.AsegurarAnfitrionesMarcha("autoprueba");
                    var a = MarchRules.HostRaces;
                    marchaSin = true;
                    for (int i = 0; a != null && i < a.Length; i++) if (a[i] == r0) marchaSin = false;
                    f3Sin = FaccionesNuevas.GuardiaEnAstra;
                }
                finally
                {
                    FaccionesNuevas.ForzarJugable(r0, true);
                    FaccionesNuevas.AsegurarAnfitrionesMarcha("autoprueba");
                }
                bool vuelve = FaccionesNuevas.AnfitrionesNuevos() == nuevas.Count && FaccionesNuevas.GuardiaEnAstra == f3Antes;
                // F3: si la raza simulada es la 40, la 900 deja de estar en la facción A; si no, no cambia
                // F3: if the simulated race is 40, 900 is no longer in faction A; otherwise it does not change
                bool f3Bien = (int)r0 == 40 ? !f3Sin : f3Sin == f3Antes;
                bool bien = todasJug && actoSin == 0 && ciudadesSin == 0 && (!otra.HasValue || ciudadesOtra > 0) && marchaSin && f3Bien && vuelve;
                Linea(bien, "Razas jugables",
                    $"jugables {string.Join(",", nuevas.Select(x => $"{(int)x}={FaccionesNuevas.Jugable(x)}"))}; con la {(int)r0} no jugable (simulado): acto {acto0} con la {(int)r0} {actoSin} (esperado 0), " +
                    $"ciudades {(int)r0} {ciudadesSin} (esperado 0){(otra.HasValue ? $" y {(int)otra.Value} {ciudadesOtra}" : "")}, Marcha sin la {(int)r0} {marchaSin}, F3 en vigor {f3Sin}; " +
                    $"al devolverla: anfitriones nuevos {FaccionesNuevas.AnfitrionesNuevos()}, F3 en vigor {FaccionesNuevas.GuardiaEnAstra}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Razas jugables", e.ToString(), ref ok, ref total); }

            // --- 21) Restauración de la raza de ciudad sin unidades 9xx registradas (0.5.1, simulado) ---
            // --- 21) Restoring the town race without registered 9xx units (0.5.1, simulated) ---
            var registroReal21 = Saves.Registro;
            Saves.Registro = new RegistroGuardias(null);
            Saves.SimularSinUnidades = true;
            try
            {
                int rz = FaccionesNuevas.Razas.Keys.Max();
                var ciudad = new TownSaveData();
                ciudad.townName = "zeroCiudadSin9xx";
                ciudad.townRace = (Race)rz;
                string texto = UnityEngine.JsonUtility.ToJson(ciudad, true);
                var vuelta = UnityEngine.JsonUtility.FromJson<TownSaveData>(texto);
                // Prueba de que la simulación quita las unidades: una escuadra de una unidad nueva de esa raza guardada se
                // queda como su base al leer
                // Proof that the simulation removes the units: a saved squad of a new unit of that race
                // becomes its base unit when read
                var u = UnidadGuardia.Todas.First(x => UnidadGuardia.Registrada(x.Numero) && x.Raza.HasValue && (int)x.Raza.Value == rz);
                var escuadra = new SquadToLoad(u.Id, 0, -1, false, 1f);
                escuadra.UniqueID = "00000000-0000-0000-0000-0000000a9441";
                string tEsc = UnityEngine.JsonUtility.ToJson(escuadra, true);
                UnitName leida = UnityEngine.JsonUtility.FromJson<SquadToLoad>(tEsc).UnitName;
                bool bien = vuelta != null && (int)vuelta.townRace == rz && leida == u.Base;
                Linea(bien, "Razas sin unidades 9xx",
                    $"ciudad {rz} en disco {(texto.Contains("\"townRace\": " + rz) ? $"con {rz} (MAL)" : "con su sustituta")}, al leer {(vuelta == null ? -1 : (int)vuelta.townRace)} " +
                    $"(esperado {rz}); escuadra {u.Numero} leída como {leida} (esperado {u.Base}: las unidades no se restauran en la simulación)", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Razas sin unidades 9xx", e.ToString(), ref ok, ref total); }
            finally { Saves.SimularSinUnidades = false; Saves.Registro = registroReal21; }

            // La pantalla de héroes solo existe al abrir la nueva partida: aquí no se puede comprobar
            // The heroes screen only exists when opening the new run: it cannot be checked here
            Plugin.Info($"[autoprueba] Pantalla de héroes (no cuenta): {PantallaHeroes.Estado}. Se amplía al abrirla" +
                        (Plugin.CfgProbarPantalla.Value ? "; Diagnostico.ProbarPantallaHeroes la abrirá en unos segundos." : " (prueba manual o Diagnostico.ProbarPantallaHeroes = true)."));
        }
            // -------------------------------------------------------------------
        // Pruebas 22-26 de los objetos nuevos (0.6.0), cada una aislada en su try/catch
        // Tests 22-26 of the new gear (0.6.0), each one isolated in its own try/catch
        // -------------------------------------------------------------------
        private static void PruebasObjetos(TabletopTavernData data, ref int ok, ref int total)
        {
            var ids = Objetos.Ids;

            // --- 22) Objetos registrados tras una recarga de datos forzada ---
            // --- 22) Gear registered after a forced data reload ---
            try
            {
                // La prueba 19 ya recarga con las facciones activas; si no, se recarga aquí
                // Test 19 already reloads with the factions active; if not, it reloads here
                if (!FaccionesNuevas.Activas) data.ReloadData();
                var mal = new List<string>();
                var overrides = GearData.ModifierOverrides;
                foreach (int id in ids)
                {
                    var g = GearData.GetGear((GearID)id);
                    var d = Objetos.Datos(id);
                    int esperado = overrides != null && overrides.ContainsKey((GearID)id) ? overrides[(GearID)id] : d.Valor;
                    if (g == null || g.GearName != Objetos.NombreInterno(id) || g.GearRarity != Objetos.RarezaDe(id) || g.GearModifierValue != esperado || g.BanFromShop)
                        mal.Add($"{id}={(g == null ? "null" : $"{g.GearName}/{g.GearRarity}/{g.GearModifierValue}")}");
                }
                var espadas = GearData.GetGear(GearID.ArmingSwords);
                var ninguno = GearData.GetGear(GearID.None);
                bool serieBien = espadas.GearName == "Arming Swords" && espadas.GearRarity == GearRarity.Common
                                 && (overrides != null && overrides.ContainsKey(GearID.ArmingSwords) || espadas.GearModifierValue == 6) && ninguno.GearName == "None";
                int primero = ids.Count > 0 ? ids[0] : 700;
                var g700 = GearData.GetGear((GearID)primero);
                int esperados = Nucleo.Actual.Manifiesto.Objetos.Count;
                bool bien = ids.Count == esperados && ids.Count > 0 && mal.Count == 0 && serieBien && Objetos.Desactivados.Count == 0;
                Linea(bien, "Objetos registrados",
                    $"{ids.Count}/{esperados} objetos de los módulos aplicados ({string.Join(",", ids)}) tras recarga forzada; mal: [{string.Join("; ", mal)}]; {primero} = {g700.GearName}/{g700.GearRarity}/{g700.GearModifierValue}; " +
                    $"de serie: Arming Swords {espadas.GearName}/{espadas.GearModifierValue}, None {ninguno.GearName}; desactivados {Objetos.Desactivados.Count}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Objetos registrados", e.ToString(), ref ok, ref total); }

            // --- 23) Iconos cargados y en la caché del juego ---
            // --- 23) Icons loaded and in the game's cache ---
            try
            {
                int presentes = Objetos.AsegurarIconos("autoprueba");
                var sinIcono = new List<int>();
                foreach (int id in ids)
                {
                    var sp = SpriteData.GetSprite(Objetos.NombreInterno(id));
                    if (sp == null || sp.name != Objetos.NombreInterno(id)) sinIcono.Add(id);
                }
                var s700 = SpriteData.GetSprite(Objetos.NombreInterno(ids.Count > 0 ? ids[0] : 700));
                var serie = SpriteData.GetSprite("Arming Swords");
                bool bien = presentes == ids.Count && sinIcono.Count == 0;
                Linea(bien, "Iconos de objetos",
                    $"{presentes}/{ids.Count} en la caché del juego; sin icono [{string.Join(",", sinIcono)}]; {(ids.Count > 0 ? ids[0] : 700)} = {(s700 == null ? "null" : $"{s700.name} {s700.rect.width}x{s700.rect.height}")}; " +
                    $"icono de serie Arming Swords {(serie == null ? "null" : $"{serie.rect.width}x{serie.rect.height}")}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Iconos de objetos", e.ToString(), ref ok, ref total); }

            // --- 24) Efecto de un objeto de cada grupo, aplicado y retirado (reglas en memoria) ---
            // --- 24) Effect of one gear item from each group, applied and removed (in-memory rules) ---
            try
            {
                const int heroe = 1;   // Edric; el efecto vale con cualquier héroe
                                       // Edric; the effect works with any hero
                var dic = data.SquadStatsDictionary;
                // Unidades de prueba: infantería común que no sea goblin, una híbrida y un goblin
                // Test units: regular infantry that is not a goblin, a hybrid and a goblin
                UnitName comun = (UnitName)(-1), hibrida = (UnitName)(-1), goblin = UnitName.GoblinRabble;
                var claves = new List<UnitName>();
                foreach (UnitName k in dic.Keys) claves.Add(k);
                foreach (UnitName k in claves.OrderBy(x => (int)x))
                {
                    SquadStats s = dic[k];
                    if ((int)k >= 900) continue;
                    if ((int)comun < 0 && s.RarityTier == UnitRarity.Common && s.unitSize == UnitSize.Infantry && !TabletopTavernConstants.IsAGoblinUnit(k)) comun = k;
                    if ((int)hibrida < 0 && s.unitType == UnitType.Hybrid) hibrida = k;
                }
                if ((int)comun < 0 || (int)hibrida < 0 || !dic.ContainsKey(goblin)) throw new InvalidOperationException($"sin unidades de prueba (común {comun}, híbrida {hibrida}, goblin {dic.ContainsKey(goblin)})");
                SquadStats sc = dic[comun], sh = dic[hibrida], sg = dic[goblin];
                float Bono(UnitStat st, UnitName u, SquadStats s, float actual) => HeroBonusRuleEvaluator.SumHeroStatBonus(st, u, heroe, s, Race.Special, actual);
                bool Perforante(UnitName u) => HeroBonusManager.UnitHasAttribute(u, heroe, UnitAttribute.ArmorPiercing);
                bool h700 = ids.Contains(700), h712 = ids.Contains(712), h721 = ids.Contains(721);
                // Sin objetos (estado base, con los bonus del jugador de los módulos si están aplicados)
                // No gear (base state, with the player bonuses from the modules if applied)
                Objetos.ForzarParaPrueba(heroe, new int[0]);
                float lid0 = Bono(UnitStat.Leadership, comun, sc, sc.Leadership), dc0 = Bono(UnitStat.MeleeDefense, comun, sc, sc.MeleeDefense);
                float dd0 = Bono(UnitStat.MissileStrength, hibrida, sh, sh.MissileStrength), ac0 = Bono(UnitStat.MeleeAttack, goblin, sg, sg.MeleeAttack);
                bool ap0 = Perforante(hibrida), apBase = sh.SquadAttributes.ArmorPiercing;
                int reglas0 = HeroBonusManager._statRules.Count;
                var partes = new List<string>();
                bool bien = h700 || h712 || h721;
                // 700 Estandarte del Regimiento (facción A): infantería común +10 liderazgo y +4 defensa
                // 700 Regiment Banner (faction A): common infantry +10 leadership and +4 defense
                if (h700)
                {
                    Objetos.ForzarParaPrueba(heroe, new[] { 700 });
                    float lid1 = Bono(UnitStat.Leadership, comun, sc, sc.Leadership), dc1 = Bono(UnitStat.MeleeDefense, comun, sc, sc.MeleeDefense);
                    int reglas700 = Objetos.ReglasAplicadas;
                    bien &= Math.Abs(lid1 - lid0 - 10f) < 0.01f && Math.Abs(dc1 - dc0 - 4f) < 0.01f && reglas700 == 2;
                    partes.Add($"700 en {comun}: liderazgo +{lid0} -> +{lid1} (esperado +10 más), defensa +{dc0} -> +{dc1} (esperado +4 más), {reglas700} reglas");
                }
                // 712 Detonadores Térmicos (facción B): híbridas +20 % de daño a distancia y perforante
                // 712 Thermal Detonators (faction B): hybrids +20% ranged damage and piercing
                if (h712)
                {
                    Objetos.ForzarParaPrueba(heroe, new[] { 712 });
                    float dd1 = Bono(UnitStat.MissileStrength, hibrida, sh, sh.MissileStrength);
                    bool ap1 = Perforante(hibrida);
                    float ddEsperado = 0.2f * sh.MissileStrength;
                    bien &= Math.Abs(dd1 - dd0 - ddEsperado) < 0.01f && (apBase || (!ap0 && ap1));
                    partes.Add($"712 en {hibrida}: daño a distancia +{dd0} -> +{dd1} (esperado +{ddEsperado} más), perforante {ap0} -> {ap1}{(apBase ? " (ya lo era de serie)" : "")}");
                }
                // 721 Grog de Hongo (unidades sueltas): goblins +4 ataque cuerpo a cuerpo
                // 721 Mushroom Grog (standalone units): goblins +4 melee attack
                if (h721)
                {
                    Objetos.ForzarParaPrueba(heroe, new[] { 721 });
                    float ac1 = Bono(UnitStat.MeleeAttack, goblin, sg, sg.MeleeAttack);
                    bien &= Math.Abs(ac1 - ac0 - 4f) < 0.01f;
                    partes.Add($"721 en {goblin}: ataque +{ac0} -> +{ac1} (esperado +4 más)");
                }
                // Se quitan: todo vuelve al estado base
                // They are removed: everything returns to the base state
                Objetos.ForzarParaPrueba(heroe, new int[0]);
                float lid2 = Bono(UnitStat.Leadership, comun, sc, sc.Leadership), dd2 = Bono(UnitStat.MissileStrength, hibrida, sh, sh.MissileStrength);
                float ac2 = Bono(UnitStat.MeleeAttack, goblin, sg, sg.MeleeAttack);
                bool ap2 = Perforante(hibrida);
                int reglas2 = HeroBonusManager._statRules.Count;
                bien &= Math.Abs(lid2 - lid0) < 0.01f && Math.Abs(dd2 - dd0) < 0.01f && Math.Abs(ac2 - ac0) < 0.01f && (apBase || !ap2) && reglas2 == reglas0;
                Linea(bien, "Efecto de objetos",
                    $"{string.Join("; ", partes)}; al quitarlos: liderazgo +{lid2}, daño +{dd2}, ataque +{ac2}, perforante {ap2}; reglas de stats {reglas0} -> {reglas2}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Efecto de objetos", e.ToString(), ref ok, ref total); }
            finally
            {
                // Vuelve al estado real del juego (en el menú: sin objetos)
                // Returns to the real game state (in the menu: no gear)
                try { Objetos.ForzarParaPrueba(-1, null); } catch (Exception e) { Plugin.Error("[autoprueba] No se pudo devolver las reglas al estado real: " + e.Message); }
            }

            // --- 25) Sorteo según la partida (simulación pura con los objetos de los módulos aplicados) ---
            // --- 25) Draw according to the run (pure simulation with the gear of the applied modules) ---
            try
            {
                var objetos = Objetos.ParaSorteo;
                // Grupo de afinidad de prueba: el del primer objeto (facción A con la facción A, starwars con el Imperio o una raza)
                // Test affinity group: that of the first item (faction A with faction A, starwars with the Empire or a race)
                string grupo = objetos.OrderBy(o => o.Id).First().Afinidad;
                var conjunto = new HashSet<int>(objetos.Where(o => o.Afinidad == grupo).Select(o => o.Id));
                var nuevos = new HashSet<int>(objetos.Select(o => o.Id));
                const int N = 20000;
                int afin = 0, otros = 0, sinAfin = 0;
                var nada = new HashSet<int>();
                for (int i = 0; i < N; i++)
                {
                    var c = new List<int> { (int)GearID.ArmingSwords };
                    SorteoObjetos.Sustituir(c, objetos, new HashSet<string> { grupo }, nada, 1, i * 31 + 7, 0.25, 0.05);
                    if (conjunto.Contains(c[0])) afin++; else if (nuevos.Contains(c[0])) otros++;
                    var c2 = new List<int> { (int)GearID.ArmingSwords };
                    SorteoObjetos.Sustituir(c2, objetos, new HashSet<string>(), nada, 1, i * 31 + 7, 0.25, 0.05);
                    if (nuevos.Contains(c2[0])) sinAfin++;
                }
                double pAfin = afin / (double)N, pOtros = otros / (double)N, pSin = sinAfin / (double)N;
                // Cofre de 3 cartas con afinidad
                // Chest of 3 cards with affinity
                int cofres = 5000, conAfin = 0, repetidos = 0;
                for (int i = 0; i < cofres; i++)
                {
                    var c = new List<int> { (int)GearID.ArmingSwords, (int)GearID.Longbows, (int)GearID.Glaives };
                    SorteoObjetos.Sustituir(c, objetos, new HashSet<string> { grupo }, nada, 1, 1000003 * i + 11, 0.25, 0.05);
                    if (c.Any(conjunto.Contains)) conAfin++;
                    if (c.Distinct().Count() != c.Count) repetidos++;
                }
                double pCofre = conAfin / (double)cofres;
                // Esperados: el 25 % afín más la parte afín del 5 % general
                // Expected: the 25% affine plus the affine part of the general 5%
                double frAfin = conjunto.Count / (double)objetos.Count;
                double espAfin = 0.25 + 0.75 * 0.05 * frAfin, espOtros = 0.75 * 0.05 * (1 - frAfin), espCofre = 1 - Math.Pow(1 - espAfin, 3);
                // Comandante (solo con el Imperio): con héroe 904 la Directiva (715) pesa el doble entre los de la facción B
                // Commander (only with the Empire): with hero 904 the Directive (715) weighs double among those of faction B
                string comandante = "(sin el Imperio Galáctico: no se comprueba)";
                bool comandanteOk = true;
                var starwars = new HashSet<int>(objetos.Where(o => o.Afinidad == SorteoObjetos.GrupoStarWars).Select(o => o.Id));
                if (starwars.Contains(715))
                {
                    int sw = 0, d715 = 0;
                    for (int i = 0; i < N; i++)
                    {
                        var c = new List<int> { (int)GearID.ArmingSwords };
                        SorteoObjetos.Sustituir(c, objetos, new HashSet<string> { SorteoObjetos.GrupoStarWars }, nada, 904, i * 17 + 3, 1.0, 0.0);
                        if (starwars.Contains(c[0])) { sw++; if (c[0] == 715) d715++; }
                    }
                    double p715 = sw == 0 ? 0 : d715 / (double)sw, esp715 = 2.0 / (starwars.Count + 1);
                    comandanteOk = Math.Abs(p715 - esp715) < 0.025;
                    comandante = $"con Thrawn la Directiva es {p715:P1} de los de Star Wars (esperado {esp715:P1})";
                }
                // Excluidos (tenidos o vendidos): con todos los del grupo excluidos salvo el primero, solo puede salir ese
                // Excluded (owned or sold): with all of the group excluded except the first, only that one can appear
                int primeroGrupo = conjunto.Min();
                var excl = new HashSet<int>(conjunto.Where(x => x != primeroGrupo));
                int soloPrimero = 0, afinEx = 0;
                for (int i = 0; i < 2000; i++)
                {
                    var c = new List<int> { (int)GearID.ArmingSwords };
                    SorteoObjetos.Sustituir(c, objetos, new HashSet<string> { grupo }, excl, 1, i, 1.0, 0.0);
                    if (conjunto.Contains(c[0])) { afinEx++; if (c[0] == primeroGrupo) soloPrimero++; }
                }
                // Afinidad por ejército (función pura): 3 unidades (900) -> facción A; 2 escuadras de Gruntkin -> no
                // Affinity per army (pure function): 3 units (900) -> faction A; 2 Gruntkin squads -> no
                var grupos = SorteoObjetos.GruposDelEjercito(new[] { 900, 900, 900, 30, 31 }, u => u >= 900 ? 0 : 1, r => ((Race)r).ToString());
                bool bien = Math.Abs(pAfin - espAfin) < 0.012 && Math.Abs(pOtros - espOtros) < 0.006 && Math.Abs(pSin - 0.05) < 0.006
                            && Math.Abs(pCofre - espCofre) < 0.025 && repetidos == 0 && comandanteOk
                            && afinEx == 2000 && soloPrimero == 2000 && grupos.Contains(SorteoObjetos.Grupo40k) && !grupos.Contains("Gruntkin");
                Linea(bien, "Sorteo de objetos",
                    $"{N} cartas con afinidad {grupo}: afín {pAfin:P1} (esperado {espAfin:P1}: 25 % + la parte afín del 5 %), otro nuevo {pOtros:P2} (esperado {espOtros:P2}); sin afinidad: nuevo {pSin:P2} (esperado 5 %); " +
                    $"cofre de 3 con al menos un afín {pCofre:P1} (esperado {espCofre:P1}), cofres con cartas repetidas {repetidos}; {comandante}; " +
                    $"excluidos respetados {soloPrimero}/{afinEx}; ejército con 3 escuadras 900 -> [{string.Join(",", grupos)}]", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Sorteo de objetos", e.ToString(), ref ok, ref total); }

            // --- 26) Saneado de saves de un objeto nuevo, ida y vuelta (registro solo en memoria) ---
            // --- 26) Sanitizing of saves with new gear, round trip (in-memory registry only) ---
            var registroReal = Saves.Registro;
            Saves.Registro = new RegistroGuardias(null);
            try
            {
                // a) Botín de una ciudad con dos objetos nuevos, con JsonUtility (los enganches reales de escritura y lectura)
                // a) Loot of a town with two new gear items, with JsonUtility (the real write and read hooks)
                int oa = ids[0], ob = ids[ids.Count - 1];   // dos objetos de los módulos aplicados (iguales si solo hay uno)
                                                            // two items from the applied modules (the same if there is only one)
                var ciudad = new TownSaveData();
                ciudad.townName = "zeroCiudadObjetos";
                ciudad.townLootGearIDs = new Il2CppSystem.Collections.Generic.List<GearID>();
                ciudad.townLootGearIDs.Add((GearID)oa);
                ciudad.townLootGearIDs.Add(GearID.ArmingSwords);
                ciudad.townLootGearIDs.Add((GearID)ob);
                string texto = UnityEngine.JsonUtility.ToJson(ciudad, true);
                bool salida = SaneadoObjetos.ContarResiduos(texto, Saves.TablaObjetos) == 0
                              && Saves.Registro.ObjetosPosicion("townLootGearIDs").Count == 2;
                var vuelta = UnityEngine.JsonUtility.FromJson<TownSaveData>(texto);
                var lv = vuelta?.townLootGearIDs;
                string leidos = lv == null ? "null" : string.Join(",", Enumerable.Range(0, lv.Count).Select(i => ((int)lv[i]).ToString()));
                bool vuelve = lv != null && lv.Count == 3 && (int)lv[0] == oa && lv[1] == GearID.ArmingSwords && (int)lv[2] == ob;
                // b) Lista Gear con choque de respaldo (el objeto se guarda como su respaldo, que ya se lleva; formato compacto
                //    como el que vuelve a escribir el saneado)
                // b) Gear list with fallback clash (the item is saved as its fallback, which is already worn; compact format
                //    like the one the sanitizing writes again)
                int qep = (int)Enum.Parse<GearID>(Objetos.Datos(oa).Respaldo);
                string gear = "{\"Gear\": [" + qep + "," + oa + "], \"SoldGear\": [" + ob + "], \"snapShot\": false}";
                string disco = SaneadoObjetos.ParaDisco(gear, Saves.TablaObjetos, Saves.Registro, true);
                string deVuelta = SaneadoObjetos.DesdeDisco(disco, Saves.TablaObjetos, Saves.Registro);
                bool choque = SaneadoObjetos.ContarResiduos(disco, Saves.TablaObjetos) == 0 && !disco.Contains("[" + qep + "," + qep + "]") && deVuelta == gear;
                // c) Sin registro (como sin plugin) el save se queda con objetos de serie válidos
                // c) Without a registry (as without the plugin) the save keeps valid default gear
                bool sinPlugin = SaneadoObjetos.DesdeDisco(disco, Saves.TablaObjetos, new RegistroGuardias(null)) == disco;
                bool bien = salida && vuelve && choque && sinPlugin;
                Linea(bien, "Saneado de objetos",
                    $"botín de ciudad en disco: {Regex.Match(texto, "\"townLootGearIDs\"[^\\]]*\\]").Value.Replace("\n", " ").Replace("  ", "")} " +
                    $"(sin objetos nuevos y 2 apuntados: {salida}); al leer [{leidos}] (esperado {oa},{(int)GearID.ArmingSwords},{ob}); " +
                    $"Gear con choque: {disco} -> vuelta exacta {deVuelta == gear}; sin registro se queda con objetos de serie {sinPlugin}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Saneado de objetos", e.ToString(), ref ok, ref total); }
            finally { Saves.Registro = registroReal; }
        }
}
}
