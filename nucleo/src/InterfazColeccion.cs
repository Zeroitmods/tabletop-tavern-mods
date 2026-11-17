// InterfazColeccion.cs
// Interfaz de la Colección y de la Batalla personalizada con las facciones y unidades nuevas (prueba de Zero en el juego,
// 17-10-2026). Qué arregla:
//   1) COLECCIÓN (menú principal > Colección):
//      - Las razas nuevas 40/41 no salían en la lista de FACCIONES: el panel arma sus facciones recorriendo el enum Race
//        (CollectionPanel.BuildData), que no tiene 40/41. Un postfix de BuildData añade una facción por raza nueva con
//        unidades (y sus héroes, para la pestaña Héroes); BuildRail, que va justo después, les crea su fila en la lista.
//        Los logros de colección no cambian: EvaluateRaceCollection(40/41) sale sin hacer nada (no hay logro para esas
//        razas) y el resto solo suma al contador que se ve en pantalla. No se envía nada online.
//      - La rejilla de unidades de una facción está pensada para 2 filas de 8 (16 unidades) y con las sueltas pasa de 16
//        (La Legión de Hierro tiene 19): la tercera fila se cortaba. Si alguna facción pasa de 2 filas, la rejilla se mete
//        en una ventana con desplazamiento vertical (ScrollRect + RectMask2D) de 2 filas de alto que se baja con la rueda.
//        La altura de la ventana sigue al tamaño de casilla que calcula CollectionAutoGrid (postfix de su LateUpdate).
//      - Con 2 filas más en la lista de la izquierda, las filas pueden encoger un poco si falta sitio (escala de
//        interfaz 125 %), como ya hacían las de serie.
//   2) BATALLA PERSONALIZADA (panel «Generar unidades»):
//      - El selector de rombos y las cartas salen de listas de razas escritas a mano en CustomBattleUIManager.LoadUI. Un
//        postfix de LoadUI crea las cartas de las unidades de 40/41 y clona un rombo de serie por cada raza nueva (con
//        el nombre «40»/«41», que el propio juego sabe convertir en raza, y la gema teñida con el color de la facción).
//        Desde el 18-10 van dentro de las flechas (detrás del último rombo de raza de serie), la separación de la fila se
//        reduce para que quepan y las flechas pasan por ellos (SampleNavInputHelper.previous/next).
//        Un prefix de OnToggleChanged atiende esos rombos sin depender de Enum.Parse.
//      - La rejilla de cartas se salía por abajo con 18-19 unidades: si alguna raza pasa de las filas que necesita la
//        raza de serie más grande, la rejilla va en una ventana con desplazamiento de esa altura (rueda del ratón). Al
//        cambiar de raza, la ventana vuelve arriba.
// Todo falla en blando: si algo no está como se espera, se deshace lo hecho, se escribe en el log y la pantalla queda
// como la de serie. La primera vez que se monta cada pantalla se escribe en el log su jerarquía (para ajustar si hiciera
// falta). Interruptores: Interfaz.Coleccion e Interfaz.BatallaPersonalizada.
// Nota: el juego instalado es más nuevo que el código publicado (trae la Liga Olímpica «próximamente», el escudo de
// facción de la cabecera...); los nombres y firmas usados aquí son los del interop del juego instalado.
// Autor: ZeroitMods.
// InterfazColeccion.cs
// Collection and Custom Battle interface with the new factions and units (tested in game by Zero,
// 17-10-2026). What it fixes:
//   1) COLLECTION (main menu > Collection):
//      - The new races 40/41 did not show up in the FACTIONS list: the panel builds its factions by iterating the Race enum
//        (CollectionPanel.BuildData), which has no 40/41. A BuildData postfix adds one faction per new race with
//        units (and their heroes, for the Heroes tab); BuildRail, which runs right after, creates their row in the list.
//        Collection achievements do not change: EvaluateRaceCollection(40/41) exits without doing anything (there is no achievement for those
//        races) and the rest only adds to the counter shown on screen. Nothing is sent online.
//      - The faction unit grid is designed for 2 rows of 8 (16 units) and with the extra ones it goes past 16
//        (La Legión de Hierro has 19): the third row was cut off. If any faction goes past 2 rows, the grid is placed
//        inside a vertically scrolling window (ScrollRect + RectMask2D) 2 rows tall that scrolls with the mouse wheel.
//        The window height follows the cell size computed by CollectionAutoGrid (postfix of its LateUpdate).
//      - With 2 more rows in the list on the left, the rows may shrink a little if there is not enough room (UI
//        scale 125%), as the stock ones already did.
//   2) CUSTOM BATTLE ("Generar unidades" panel):
//      - The diamond selector and the cards come from hand-written race lists in CustomBattleUIManager.LoadUI. A
//        postfix of LoadUI creates the cards for the 40/41 units and clones a stock diamond for each new race (with
//        the name "40"/"41", which the game itself can convert into a race, and the gem tinted with the faction color).
//        Since 18-10 they sit inside the arrows (after the last stock race diamond), the row spacing is
//        reduced so they fit and the arrows go through them (SampleNavInputHelper.previous/next).
//        An OnToggleChanged prefix handles those diamonds without relying on Enum.Parse.
//      - The card grid overflowed at the bottom with 18-19 units: if any race exceeds the rows needed by the
//        largest stock race, the grid goes in a scrolling window of that height (mouse wheel). When
//        switching race, the window returns to the top.
// Everything fails softly: if something is not as expected, what was done is undone, it is written to the log and the screen stays
// as the stock one. The first time each screen is built, its hierarchy is written to the log (to adjust if
// needed). Switches: Interfaz.Coleccion and Interfaz.BatallaPersonalizada.
// Note: the installed game is newer than the published code (it includes the Olympic League "coming soon", the faction
// shield in the header...); the names and signatures used here are those of the installed game's interop.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using TJ.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: parches de la Colección y de la Batalla personalizada
    // Section: Collection and Custom Battle patches
    // -----------------------------------------------------------------------
    public static class InterfazColeccion
    {
        public const string NombreVentanaColeccion = "ZeroDesplazamientoColeccion";
        public const string NombreVentanaBatalla = "ZeroDesplazamientoBatalla";
        // Filas que enseña la rejilla de la Colección (CollectionAutoGrid: «dos filas a cualquier escala»)
        // Rows shown by the Collection grid (CollectionAutoGrid: "two rows at any scale")
        private const int FilasColeccion = 2;
        // Altura mínima de las filas de la lista de la izquierda cuando falta sitio (las de serie bajan hasta 42)
        // Minimum height of the left list rows when there is not enough room (stock ones go down to 42)
        private const float AlturaMinimaFilaLista = 36f;

        private static readonly HashSet<string> _avisos = new HashSet<string>();
        private static readonly HashSet<string> _volcados = new HashSet<string>();

        // Estado de la ventana de la Colección: id de la rejilla envuelta, unidades de la facción visible y última altura
        // Collection window state: id of the wrapped grid, units of the visible faction and last height
        private static int _idRejillaColeccion;
        private static int _activosColeccion;
        private static float _alturaColeccion = -1f;
        private static int _ultimaRazaColeccion = int.MinValue;
        private static bool _falloAutoGrid;

        /// <summary>
        /// Lista de parches para Plugin.cs: (nombre, tipo, método, argumentos, prefix, postfix, finalizer). Solo los activados.
        /// List of patches for Plugin.cs: (name, type, method, arguments, prefix, postfix, finalizer). Only the enabled ones.
        /// </summary>
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer)> Parches()
        {
            var l = new List<(string, Func<Type>, string, Func<Type[]>, string, string, string)>();
            if (Plugin.CfgInterfazColeccion != null && Plugin.CfgInterfazColeccion.Value)
            {
                Func<Type> tP = () => typeof(CollectionPanel);
                // BuildData: postfix de las facciones nuevas y, alrededor (prefix + finalizer), la lista de objetos ampliada
                // de Objetos.cs (Equipamiento con los objetos 7xx de los módulos activos)
                // BuildData: postfix for the new factions and, around it (prefix + finalizer), the extended item list
                // from Objetos.cs (Equipamiento with the 7xx items of the active modules)
                l.Add(("Colección: facciones y objetos nuevos en la lista (CollectionPanel.BuildData)", tP, nameof(CollectionPanel.BuildData), () => Type.EmptyTypes,
                    nameof(AntesDeBuildData), nameof(TrasBuildData), nameof(FinListaAmpliada)));
                l.Add(("Colección: lista de facciones que cabe (CollectionPanel.SetUp)", tP, nameof(CollectionPanel.SetUp), null, null, nameof(TrasSetUp), null));
                l.Add(("Colección: rejilla de unidades con desplazamiento (CollectionPanel.ShowFaction)", tP, nameof(CollectionPanel.ShowFaction), null, null, nameof(TrasShowFaction), null));
                l.Add(("Colección: historia de respaldo de los héroes nuevos (CollectionPanel.ShowHero)", tP, nameof(CollectionPanel.ShowHero), null, null, nameof(TrasShowHero), null));
                l.Add(("Colección: altura de la ventana de unidades (CollectionAutoGrid.LateUpdate)", () => typeof(CollectionAutoGrid), nameof(CollectionAutoGrid.LateUpdate), () => Type.EmptyTypes, null, nameof(TrasAutoGridLateUpdate), null));
                // Contador del botón Colección del menú principal: en el postfix, las unidades de las razas nuevas 40/41
                // (CampanaHeroes.SumarUnidadesAlContador). La lista de objetos ampliada alrededor del contador (prefix y
                // finalizer) va desde la revisión de la 0.7.2 con los parches de los objetos (Plugin.AplicarParchesObjetos):
                // así el total de objetos cuadra con los recogidos aunque Interfaz.Coleccion = false
                // Main menu Collection button counter: in the postfix, the units of the new races 40/41
                // (CampanaHeroes.SumarUnidadesAlContador). The extended item list around the counter (prefix and
                // finalizer) has been there since the 0.7.2 review with the item patches (Plugin.AplicarParchesObjetos):
                // so the total item count matches the collected ones even with Interfaz.Coleccion = false
                l.Add(("Colección: unidades nuevas en el contador del botón (MainMenu.RefreshCollectionButtonState)", () => typeof(TJ.MainMenu.MainMenu),
                    nameof(TJ.MainMenu.MainMenu.RefreshCollectionButtonState), () => Type.EmptyTypes, null, nameof(TrasRefreshCollectionButtonState), null));
            }
            if (Plugin.CfgInterfazBatalla != null && Plugin.CfgInterfazBatalla.Value)
            {
                Func<Type> tB = () => typeof(CustomBattleUIManager);
                l.Add(("Batalla personalizada: razas nuevas y desplazamiento (CustomBattleUIManager.LoadUI)", tB, nameof(CustomBattleUIManager.LoadUI), () => new[] { typeof(int) }, null, nameof(TrasLoadUI), null));
                l.Add(("Batalla personalizada: rombos de las razas nuevas (CustomBattleUIManager.OnToggleChanged)", tB, nameof(CustomBattleUIManager.OnToggleChanged), null, nameof(AntesDeOnToggleChanged), null, null));
                l.Add(("Batalla personalizada: rejilla arriba al cambiar de raza (CustomBattleUIManager.SelectRace)", tB, nameof(CustomBattleUIManager.SelectRace), null, null, nameof(TrasSelectRace), null));
            }
            return l;
        }

        // ---------------------------------------------------------------
        // Objetos nuevos en la Colección (API de Objetos.cs): mientras corre BuildData (o el contador del menú),
        // GearData.GetGearIDs devuelve también los objetos de los módulos activos. Entrar y Salir van siempre emparejados
        // (el finalizer corre aunque el método falle). EvaluateCollectionAchievements no se envuelve: el logro de objetos
        // sigue contando solo los de serie.
        // New gear in the Collection (Objetos.cs API): while BuildData runs (or the menu counter),
        // GearData.GetGearIDs also returns the items of the active modules. Entrar and Salir are always paired
        // (the finalizer runs even if the method fails). EvaluateCollectionAchievements is not wrapped: the item achievement
        // still counts only the stock ones.
        // ---------------------------------------------------------------
        public static void AntesDeBuildData()
        {
            AntesDeListaAmpliada();
            try { Objetos.AsegurarIconos("colección"); } catch (Exception e) { AvisoUnaVez("iconosColeccion", "Colección: iconos de objetos nuevos: " + e.Message); }
        }

        // Prefix: abre el contexto de lista ampliada (Objetos.cs)
        // Prefix: opens the extended list context (Objetos.cs)
        public static void AntesDeListaAmpliada()
        {
            try { Objetos.EntrarListaAmpliada(); } catch (Exception e) { AvisoUnaVez("entrarLista", "Colección: EntrarListaAmpliada: " + e.Message); }
        }

        // Postfix del contador del botón Colección: suma las unidades de las razas nuevas (CampanaHeroes.cs)
        // Collection button counter postfix: adds the units of the new races (CampanaHeroes.cs)
        public static void TrasRefreshCollectionButtonState(TJ.MainMenu.MainMenu __instance) => CampanaHeroes.SumarUnidadesAlContador(__instance);

        // Finalizer: cierra el contexto de lista ampliada y deja pasar la excepción del juego tal cual (o null)
        // Finalizer: closes the extended list context and lets the game's exception through as is (or null)
        public static Exception FinListaAmpliada(Exception __exception)
        {
            try { Objetos.SalirListaAmpliada(); } catch (Exception e) { AvisoUnaVez("salirLista", "Colección: SalirListaAmpliada: " + e.Message); }
            return __exception;
        }

        // Aviso una sola vez por clave y sesión (para fallos dentro de parches que se ejecutan a menudo)
        // Warns only once per key and session (for failures inside patches that run often)
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // Razas nuevas que salen en la interfaz: dadas de alta en esta sesión, con alguna unidad en su lista de raza y
        // (0.7.2, revisión) con datos de raza en el juego: la vista previa de la Colección lee GetRaceData(raza).RaceBasePrefab
        // al pasar el ratón por una unidad y, sin datos, daría un NullReferenceException en cada paso
        // New races that appear in the interface: registered in this session, with at least one unit in their race list and
        // (0.7.2, review) with race data in the game: the Collection preview reads GetRaceData(race).RaceBasePrefab
        // when hovering over a unit and, without data, it would throw a NullReferenceException on every step
        private static List<Race> RazasParaInterfaz(TabletopTavernData data)
        {
            var l = new List<Race>();
            if (!FaccionesNuevas.Activas || data == null) return l;
            foreach (Race r in FaccionesNuevas.RazasNuevas.OrderBy(x => (int)x))
            {
                if (!FaccionesNuevas.EsNueva(r)) continue;
                var u = data.GetUnitsOfRace(r);
                if (u == null || u.Length == 0) continue;
                bool conDatos;
                try { conDatos = data.GetRaceData(r) != null; } catch { conDatos = false; }
                if (conDatos) l.Add(r);
                else AvisoUnaVez("sinDatosRaza" + (int)r, $"Colección: la raza nueva {(int)r} no tiene datos de raza en el juego; no se enseña en la interfaz.");
            }
            return l;
        }

        // Héroes de una raza ordenados por id (para la pestaña Héroes de la Colección)
        // Heroes of a race sorted by id (for the Collection Heroes tab)
        private static Il2CppReferenceArray<Hero> HeroesDe(Race r)
        {
            var lista = new List<Hero>();
            try
            {
                var porRaza = HeroData.GetHeroesByRace(r);
                for (int i = 0; porRaza != null && i < porRaza.Count; i++) if (porRaza[i] != null) lista.Add(porRaza[i]);
            }
            catch (Exception e) { AvisoUnaVez("heroesraza", "Colección: GetHeroesByRace falló (" + e.Message + "); se buscan en HeroData.Heroes."); }
            if (lista.Count == 0)
            {
                var todos = HeroData.Heroes;
                for (int i = 0; todos != null && i < todos.Length; i++) if (todos[i] != null && todos[i].Race == r) lista.Add(todos[i]);
            }
            lista = lista.GroupBy(h => h.HeroID).Select(g => g.First()).OrderBy(h => h.HeroID).ToList();
            var arr = new Il2CppReferenceArray<Hero>(lista.Count);
            for (int i = 0; i < lista.Count; i++) arr[i] = lista[i];
            return arr;
        }

        // ===================================================================
        // COLECCIÓN
        // COLLECTION
        // ===================================================================

        // Postfix de CollectionPanel.BuildData: añade a _factions una facción por raza nueva (Units, Heroes) si no está ya
        // Postfix of CollectionPanel.BuildData: adds to _factions one faction per new race (Units, Heroes) if not already there
        public static void TrasBuildData(CollectionPanel __instance)
        {
            try
            {
                if (__instance == null || !FaccionesNuevas.Activas) return;
                var data = TabletopTavernData.InstanceIfExists;
                var actuales = __instance._factions;
                if (data == null || actuales == null) return;
                var ya = new HashSet<int>();
                for (int i = 0; i < actuales.Length; i++) if (actuales[i] != null) ya.Add((int)actuales[i].Race);
                var nuevas = new List<CollectionPanel.Faction>();
                var resumen = new List<string>();
                foreach (Race r in RazasParaInterfaz(data))
                {
                    if (ya.Contains((int)r)) continue;
                    var f = new CollectionPanel.Faction();
                    f.Race = r;
                    f.ComingSoon = false;
                    f.Units = data.GetUnitsOfRace(r);
                    f.Heroes = HeroesDe(r);
                    f.Built = false;
                    f.Pinned = -1;
                    f.HeroIndex = 0;
                    // El inicializador de la lista corre en el constructor del juego; por si acaso
                    // The list initializer runs in the game's constructor; just in case
                    if (f.Tiles == null) f.Tiles = new Il2CppGeneric.List<CollectionTile>();
                    nuevas.Add(f);
                    resumen.Add($"{(int)r}: {f.Units.Length} unidades, {f.Heroes.Length} héroes");
                }
                if (nuevas.Count == 0) return;
                var ampliado = new Il2CppReferenceArray<CollectionPanel.Faction>(actuales.Length + nuevas.Count);
                for (int i = 0; i < actuales.Length; i++) ampliado[i] = actuales[i];
                for (int i = 0; i < nuevas.Count; i++) ampliado[actuales.Length + i] = nuevas[i];
                __instance._factions = ampliado;
                if (_volcados.Add("coleccion-facciones"))
                    Plugin.Info($"Colección: facciones nuevas en la lista ({string.Join("; ", resumen)}); {ampliado.Length} facciones en total. No cuentan para los logros.");
            }
            catch (Exception e) { AvisoUnaVez("buildData", "Colección: no se pudieron añadir las facciones nuevas (la lista queda la de serie): " + e.Message); }
        }

        // Postfix de CollectionPanel.SetUp: comprueba que entraron las facciones nuevas y deja que las filas de la lista
        // encojan un poco si falta sitio (con 2 filas más, a escala de interfaz 125 % no cabrían a su altura mínima de serie)
        // Postfix of CollectionPanel.SetUp: checks that the new factions were added and lets the list rows
        // shrink a little if there is not enough room (with 2 more rows, at UI scale 125% they would not fit at their stock minimum height)
        public static void TrasSetUp(CollectionPanel __instance)
        {
            try
            {
                if (__instance == null) return;
                var facciones = __instance._factions;
                int nuevas = 0;
                for (int i = 0; facciones != null && i < facciones.Length; i++)
                    if (facciones[i] != null && FaccionesNuevas.EsNueva(facciones[i].Race)) nuevas++;
                int esperadas = FaccionesNuevas.Activas ? RazasParaInterfaz(TabletopTavernData.InstanceIfExists).Count : 0;
                if (nuevas < esperadas)
                    AvisoUnaVez("setupFacciones", $"Colección: solo {nuevas} de {esperadas} facciones nuevas en la lista (¿no enganchó el postfix de BuildData?).");
                if (nuevas == 0) return;
                var carril = __instance.railContainer;
                if (carril == null) return;
                int ajustadas = 0;
                for (int i = 0; i < carril.childCount; i++)
                {
                    var hijo = carril.GetChild(i);
                    if (hijo == null || hijo.GetComponent<CollectionRailRow>() == null) continue;
                    var le = hijo.GetComponent<LayoutElement>();
                    if (le == null || le.minHeight <= AlturaMinimaFilaLista) continue;
                    le.minHeight = AlturaMinimaFilaLista;
                    ajustadas++;
                }
                if (ajustadas > 0 && _volcados.Add("coleccion-carril"))
                    Plugin.Info($"Colección: {ajustadas} filas de la lista pueden encoger hasta {AlturaMinimaFilaLista:0} px si falta sitio.");
            }
            catch (Exception e) { AvisoUnaVez("setUp", "Colección: TrasSetUp: " + e.Message); }
        }

        // Postfix de CollectionPanel.ShowFaction: monta (una vez) la ventana con desplazamiento de la rejilla de unidades si
        // alguna facción pasa de 2 filas, ajusta su altura a la facción visible y la sube arriba al cambiar de facción
        // Postfix of CollectionPanel.ShowFaction: builds (once) the scrolling window for the unit grid if
        // any faction goes past 2 rows, adjusts its height to the visible faction and scrolls it to the top when switching faction
        public static void TrasShowFaction(CollectionPanel __instance)
        {
            try
            {
                if (__instance == null) return;
                var faccion = __instance._faction;
                var rejilla = __instance.rosterGrid == null ? null : __instance.rosterGrid.TryCast<RectTransform>();
                if (faccion == null) return;
                // Historia de las facciones y héroes nuevos (claves «40Lore», «41Lore»...): si falta el texto, uno de respaldo
                // Lore of the new factions and heroes (keys "40Lore", "41Lore"...): if the text is missing, a fallback one
                RellenarHistoria(__instance);
                if (rejilla == null) return;
                var glg = rejilla.GetComponent<GridLayoutGroup>();
                if (glg == null) { AvisoUnaVez("colSinGrid", "Colección: la rejilla de unidades no tiene GridLayoutGroup; se queda la de serie."); return; }

                RectTransform ventana = VentanaDe(rejilla, NombreVentanaColeccion);
                if (ventana == null)
                {
                    int columnas = ColumnasColeccion(rejilla, glg);
                    int maximo = 0;
                    var facciones = __instance._factions;
                    for (int i = 0; facciones != null && i < facciones.Length; i++)
                        if (facciones[i]?.Units != null) maximo = Math.Max(maximo, facciones[i].Units.Length);
                    if (!RejillaDesplazable.NecesitaDesplazamiento(maximo, columnas, FilasColeccion)) return;
                    VolcarUnaVez("coleccion", "Colección (antes de montar la ventana)", rejilla, 4, __instance.railContainer);
                    ventana = MontarVentana(rejilla, NombreVentanaColeccion, out var c);
                    if (ventana == null) return;
                    // rosterRoot es lo que el panel enciende y apaga por pestaña: si era la propia rejilla, pasa a ser la
                    // ventana (si no, la ventana se quedaría encendida en la pestaña Héroes y hundiría el escenario)
                    // rosterRoot is what the panel turns on and off per tab: if it was the grid itself, it becomes the
                    // window (otherwise the window would stay on in the Heroes tab and cover the stage)
                    try
                    {
                        var raiz = __instance.rosterRoot;
                        if (raiz != null && raiz.Pointer == rejilla.gameObject.Pointer)
                        {
                            bool activa = rejilla.gameObject.activeSelf;
                            __instance.rosterRoot = ventana.gameObject;
                            ventana.gameObject.SetActive(activa);
                            rejilla.gameObject.SetActive(true);
                        }
                    }
                    catch (Exception e) { Deshacer(c); AvisoUnaVez("rosterRoot", "Colección: no se pudo pasar rosterRoot a la ventana (" + e.Message + "); se queda la de serie."); return; }
                    _idRejillaColeccion = rejilla.gameObject.GetInstanceID();
                    _alturaColeccion = -1f;
                    Plugin.Info($"Colección: rejilla de unidades con desplazamiento vertical ({columnas} columnas, ventana de {FilasColeccion} filas; la facción más grande tiene {maximo} unidades).");
                }

                // Altura según la facción visible (1 o 2 filas) y arriba al cambiar de facción
                // Height according to the visible faction (1 or 2 rows) and top when switching faction
                _activosColeccion = faccion.Units == null ? 0 : faccion.Units.Length;
                AjustarVentanaColeccion(ventana, rejilla, glg, true);
                if ((int)faccion.Race != _ultimaRazaColeccion)
                {
                    _ultimaRazaColeccion = (int)faccion.Race;
                    var sr = ventana.GetComponent<ScrollRect>();
                    if (sr != null) sr.verticalNormalizedPosition = 1f;
                }
            }
            catch (Exception e) { AvisoUnaVez("showFaction", "Colección: TrasShowFaction: " + e.Message); }
        }

        // Postfix de CollectionPanel.ShowHero: al cambiar de héroe en la pestaña Héroes, historia de respaldo si falta
        // Postfix of CollectionPanel.ShowHero: when switching heroes in the Heroes tab, fallback lore if it is missing
        public static void TrasShowHero(CollectionPanel __instance)
        {
            try { if (__instance != null) RellenarHistoria(__instance); }
            catch (Exception e) { AvisoUnaVez("showHero", "Colección: TrasShowHero: " + e.Message); }
        }

        // Historia de las facciones nuevas (pestaña Historia: GetLoreString("<raza>Lore"), p. ej. «40Lore») y de sus héroes
        // (pestaña Héroes: GetLoreString(HeroPrefabName), «zeroHeroe901Lore»... desde el 03-11-2026). Si el juego no encuentra el texto (vacío o
        // devuelve la propia clave), se prueba la misma clave en los textos normales (GetText) y, si tampoco está, se pone
        // un texto de respaldo con el nombre de la facción y sus efectos de batalla y campaña. Solo facciones nuevas.
        // Lore of the new factions (Lore tab: GetLoreString("<raza>Lore"), e.g. «40Lore») and of their heroes
        // (Heroes tab: GetLoreString(HeroPrefabName), «zeroHeroe901Lore»... since 03-11-2026). If the game doesn't find the text (empty or
        // returns the key itself), the same key is tried in the normal texts (GetText) and, if it isn't there either,
        // a fallback text is used with the faction's name and its battle and campaign effects. New factions only.
        private static void RellenarHistoria(CollectionPanel p)
        {
            var f = p._faction;
            if (f == null || !FaccionesNuevas.EsNueva(f.Race)) return;
            var loc = Memori.Localization.LocalizationManager.Instance;
            if (loc == null) return;
            string raza = ((int)f.Race).ToString();
            if (p._tab == CollectionPanel.FactionTab.Lore && p.loreText != null && FaltaTexto(p.loreText.text, raza + "Lore"))
            {
                string texto = TextoSiExiste(loc, raza + "Lore");
                if (texto == null)
                {
                    string nombre = TextoSiExiste(loc, raza) ?? raza;
                    string pasiva = TextoSiExiste(loc, raza + "PassiveDescription");
                    string campana = TextoSiExiste(loc, raza + "BonusDescription");
                    texto = nombre + (pasiva != null ? "\n\n" + pasiva : "") + (campana != null ? "\n\n" + campana : "");
                }
                p.loreText.text = texto;
                AvisoUnaVez("lore" + raza, $"Colección: falta la historia de la facción {raza} (clave «{raza}Lore» en los textos de historia); se enseña un texto de respaldo.");
            }
            if (p._tab == CollectionPanel.FactionTab.Heroes && p.heroLoreText != null && f.Heroes != null && f.HeroIndex >= 0 && f.HeroIndex < f.Heroes.Length)
            {
                var heroe = f.Heroes[f.HeroIndex];
                string clave = heroe?.HeroPrefabName;
                if (heroe == null || string.IsNullOrEmpty(clave) || !FaltaTexto(p.heroLoreText.text, clave)) return;
                string texto = TextoSiExiste(loc, clave) ?? TextoSiExiste(loc, heroe.HeroDescription ?? "") ?? TextoSiExiste(loc, heroe.HeroName ?? "") ?? "";
                p.heroLoreText.text = texto;
                AvisoUnaVez("lorehero" + heroe.HeroID, $"Colección: falta la historia del héroe {heroe.HeroID} (clave «{clave}» en los textos de historia); se usa {(texto.Length > 0 ? "la de los textos normales" : "un texto vacío")}.");
            }
        }

        // ¿El texto está vacío o es solo la clave (lo que devuelve el juego si no la encuentra)? Se quitan antes las etiquetas
        // de formato: el panel pone la inicial dorada («<size=160%><color=...>4</color></size>0Lore»)
        // Is the text empty or just the key (what the game returns if it does not find it)? The formatting tags are removed first:
        // the panel adds the golden initial («<size=160%><color=...>4</color></size>0Lore»)
        private static bool FaltaTexto(string texto, string clave)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;
            string t = System.Text.RegularExpressions.Regex.Replace(texto, "<[^>]*>", "").Trim();
            return t.Length == 0 || t == clave;
        }

        // Texto normal de una clave (GetText), o null si no existe (vacío o la propia clave)
        // Normal text of a key (GetText), or null if it does not exist (empty or the key itself)
        private static string TextoSiExiste(Memori.Localization.LocalizationManager loc, string clave)
        {
            if (string.IsNullOrEmpty(clave)) return null;
            try
            {
                string t = loc.GetText(clave);
                return string.IsNullOrWhiteSpace(t) || t.Trim() == clave ? null : t;
            }
            catch { return null; }
        }

        // Postfix de CollectionAutoGrid.LateUpdate: si es la rejilla envuelta, la altura de la ventana sigue al tamaño de casilla
        // Postfix of CollectionAutoGrid.LateUpdate: if it is the wrapped grid, the window height follows the cell size
        public static void TrasAutoGridLateUpdate(CollectionAutoGrid __instance)
        {
            if (_falloAutoGrid || _idRejillaColeccion == 0 || __instance == null) return;
            try
            {
                if (__instance.gameObject.GetInstanceID() != _idRejillaColeccion) return;
                var rejilla = __instance.transform.TryCast<RectTransform>();
                var ventana = rejilla == null ? null : VentanaDe(rejilla, NombreVentanaColeccion);
                var glg = rejilla?.GetComponent<GridLayoutGroup>();
                if (ventana == null || glg == null) return;
                AjustarVentanaColeccion(ventana, rejilla, glg, false);
            }
            catch (Exception e)
            {
                _falloAutoGrid = true;
                Plugin.Aviso("Colección: el ajuste de altura de la ventana falló y se desactiva (" + e.Message + ").");
            }
        }

        // Columnas de la rejilla de la Colección: las de CollectionAutoGrid (8 de serie) o las del GridLayoutGroup
        // Columns of the Collection grid: those of CollectionAutoGrid (8 by default) or those of the GridLayoutGroup
        private static int ColumnasColeccion(RectTransform rejilla, GridLayoutGroup glg)
        {
            try
            {
                var auto = rejilla.GetComponent<CollectionAutoGrid>();
                if (auto != null && auto.columns > 0) return auto.columns;
            }
            catch { }
            return RejillaDesplazable.Columnas(glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount, glg.constraintCount,
                rejilla.rect.width, glg.padding.horizontal, glg.cellSize.x, glg.spacing.x);
        }

        // Altura de la ventana de la Colección = filas visibles de la facción actual (como mucho 2) con la casilla actual.
        // Solo se toca si cambia (cambiar el LayoutElement obliga a recalcular la maquetación)
        // Collection window height = visible rows of the current faction (at most 2) with the current cell size.
        // It is only touched if it changes (changing the LayoutElement forces a layout recalculation)
        private static void AjustarVentanaColeccion(RectTransform ventana, RectTransform rejilla, GridLayoutGroup glg, bool forzar)
        {
            int columnas = ColumnasColeccion(rejilla, glg);
            int filas = RejillaDesplazable.FilasVisibles(_activosColeccion, columnas, FilasColeccion);
            float h = RejillaDesplazable.AlturaFilas(filas, glg.cellSize.y, glg.spacing.y, glg.padding.top, glg.padding.bottom);
            if (!forzar && Math.Abs(h - _alturaColeccion) < 0.5f) return;
            _alturaColeccion = h;
            FijarAltura(ventana, h);
        }

        // ===================================================================
        // BATALLA PERSONALIZADA
        // CUSTOM BATTLE
        // ===================================================================

        // Postfix de CustomBattleUIManager.LoadUI: cartas y rombos de las razas nuevas, y desplazamiento de la rejilla.
        // En las batallas de campaña el panel está apagado y no se toca nada.
        // Postfix of CustomBattleUIManager.LoadUI: cards and diamonds for the new races, and grid scrolling.
        // In campaign battles the panel is off and nothing is touched.
        public static void TrasLoadUI(CustomBattleUIManager __instance)
        {
            try
            {
                if (__instance == null) return;
                var panel = __instance.customBattlePanel;
                if (panel == null || !panel.activeSelf) return;
                var data = TabletopTavernData.InstanceIfExists;
                if (data == null) return;
                VolcarUnaVez("batalla", "Batalla personalizada (antes de tocar nada)",
                    __instance.unitUIParent == null ? null : __instance.unitUIParent.TryCast<RectTransform>(), 6,
                    __instance.raceToggleGroup == null ? null : __instance.raceToggleGroup.transform);
                if (FaccionesNuevas.Activas)
                {
                    var razas = RazasParaInterfaz(data);
                    if (razas.Count > 0)
                    {
                        int cartas = AnadirCartas(__instance, data, razas);
                        int rombos = AnadirRombos(__instance, razas);
                        Plugin.Info($"Batalla personalizada: {cartas} cartas y {rombos} rombos de las facciones nuevas ({string.Join(", ", razas.Select(r => (int)r))}).");
                    }
                }
                MontarDesplazamientoBatalla(__instance, data);
            }
            catch (Exception e) { AvisoUnaVez("loadUI", "Batalla personalizada: TrasLoadUI: " + e); }
        }

        // Crea las cartas de las unidades de las razas nuevas (como hace LoadUI con las de serie) y las deja ocultas
        // Creates the unit cards for the new races (as LoadUI does with the default ones) and leaves them hidden
        private static int AnadirCartas(CustomBattleUIManager inst, TabletopTavernData data, List<Race> razas)
        {
            var prefab = inst.unitUIPrefab;
            var padre = inst.unitUIParent;
            var lista = inst.unitUIPrefabs;
            if (prefab == null || padre == null || lista == null) { AvisoUnaVez("cartasSin", "Batalla personalizada: falta el prefab, el padre o la lista de cartas; no se añaden cartas."); return 0; }
            var ya = new HashSet<int>();
            for (int i = 0; i < lista.Count; i++) if (lista[i] != null) ya.Add((int)lista[i].UnitName);
            // Al pulsar una carta, el juego pasa el cursor a modo «colocar escuadra» (SwitchCursorModeToSpawn)
            // When a card is clicked, the game switches the cursor to «place squad» mode (SwitchCursorModeToSpawn)
            var instancia = inst;
            var alPulsar = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() =>
            {
                try { instancia.SwitchCursorModeToSpawn(); }
                catch (Exception e) { AvisoUnaVez("cursor", "Batalla personalizada: SwitchCursorModeToSpawn: " + e.Message); }
            }));
            int creadas = 0;
            foreach (Race r in razas)
            {
                var unidades = data.GetUnitsOfRace(r);
                // Fondo de la carta: el de la raza de serie que presta la música (como SquadDisplayCardPresenter)
                // Card background: that of the default race that lends the music (like SquadDisplayCardPresenter)
                Race fondo = FaccionesNuevas.Prestada(r, x => x.Musica, Race.IronLegion);
                for (int i = 0; unidades != null && i < unidades.Length; i++)
                {
                    UnitName u = unidades[i];
                    if (!ya.Add((int)u)) continue;
                    GameObject go = null;
                    try
                    {
                        go = UnityEngine.Object.Instantiate(prefab.gameObject, padre, false);
                        var carta = go.GetComponent<CustomBattleSquadSpawnCard>();
                        if (carta == null) throw new InvalidOperationException("el clon no tiene CustomBattleSquadSpawnCard");
                        carta.SetUp(u, alPulsar);
                        var imagen = fondo == Race.RavenHost ? carta.ravenhostImage : carta.ironLegionImage;
                        if (imagen != null) imagen.enabled = true;
                        lista.Add(carta);
                        creadas++;
                    }
                    catch (Exception e)
                    {
                        if (go != null) UnityEngine.Object.Destroy(go);
                        AvisoUnaVez("carta" + (int)u, $"Batalla personalizada: no se pudo crear la carta de la unidad {(int)u}: {e.Message}");
                    }
                }
            }
            // Que solo se vean las de la raza elegida (LoadUI ya eligió la suya antes de este postfix)
            // So that only those of the chosen race are shown (LoadUI already chose its own before this postfix)
            if (creadas > 0) inst.SetUnitPrefabs(inst.selectedRace);
            return creadas;
        }

        // Clona un rombo de serie por cada raza nueva: nombre «40»/«41», gema con el color de la facción, mismo grupo.
        // Arreglo del 18-10 (prueba de Zero: solo se veían los 10 rombos de serie). La fila «H_Group» tiene ancho fijo
        // (3000 a escala 0,1) con HorizontalLayoutGroup centrado, separación 106 y las flechas a escala 5 (350 de ancho);
        // los clones se añadían al final, DETRÁS de la flecha derecha, y la fila pasaba de 3000 a 3758: quedaban fuera.
        // Ahora: van justo detrás del último rombo de raza de serie (antes de «Especial», como hace el juego con la Liga
        // Olímpica en AddOlympianLeagueToggle), la separación se reduce para que la fila quepa, entran en la cadena de las
        // flechas (SampleNavInputHelper) y la gema que se tiñe es la imagen «Icon Gem» (cada rombo trae la suya pintada).
        // Clones a default diamond for each new race: name «40»/«41», gem with the faction color, same group.
        // Fix from 18-10 (Zero's test: only the 10 default diamonds were visible). The «H_Group» row has a fixed width
        // (3000 at scale 0.1) with a centered HorizontalLayoutGroup, spacing 106 and the arrows at scale 5 (350 wide);
        // the clones were added at the end, BEHIND the right arrow, and the row went from 3000 to 3758: they ended up outside.
        // Now: they go right after the last default race diamond (before «Especial», as the game does with the Olympian
        // League in AddOlympianLeagueToggle), the spacing is reduced so the row fits, they enter the arrow chain
        // (SampleNavInputHelper) and the gem that gets tinted is the «Icon Gem» image (each diamond has its own painted one).
        private static int AnadirRombos(CustomBattleUIManager inst, List<Race> razas)
        {
            var grupo = inst.raceToggleGroup;
            if (grupo == null) return 0;
            var toggles = grupo.GetComponentsInChildren<Toggle>(true);
            if (toggles == null || toggles.Length == 0) return 0;
            // Plantilla: el último rombo de una raza de serie jugable (ni Special ni la Liga Olímpica «próximamente»)
            // Template: the last diamond of a playable default race (neither Special nor the «coming soon» Olympian League)
            Toggle plantilla = null;
            Race razaPlantilla = Race.IronLegion;
            var nombres = new HashSet<string>();
            for (int i = 0; i < toggles.Length; i++)
            {
                var t = toggles[i];
                if (t == null) continue;
                nombres.Add(t.name);
                if (Enum.TryParse(t.name, out Race r) && Enum.IsDefined(typeof(Race), r) && r != Race.Special && r != Race.OlympianLeague && t.gameObject.activeSelf)
                { plantilla = t; razaPlantilla = r; }
            }
            if (plantilla == null) { AvisoUnaVez("romboPlantilla", "Batalla personalizada: no hay ningún rombo de serie que clonar; las razas nuevas no salen en el selector."); return 0; }
            var padre = plantilla.transform.parent;
            bool conLayout = padre != null && padre.GetComponent<LayoutGroup>() != null;
            // Sin LayoutGroup: cada clon se coloca a la misma distancia que hay entre los dos últimos rombos
            // Without a LayoutGroup: each clone is placed at the same distance as between the last two diamonds
            var hermanos = new List<RectTransform>();
            for (int i = 0; i < toggles.Length; i++)
                if (toggles[i] != null && toggles[i].transform.parent != null && padre != null && toggles[i].transform.parent.Pointer == padre.Pointer && toggles[i].gameObject.activeSelf)
                    hermanos.Add(toggles[i].transform.TryCast<RectTransform>());
            hermanos = hermanos.Where(x => x != null).OrderBy(x => x.GetSiblingIndex()).ToList();
            Vector2 paso = hermanos.Count >= 2 ? hermanos[hermanos.Count - 1].anchoredPosition - hermanos[hermanos.Count - 2].anchoredPosition : Vector2.zero;
            Color colorPlantilla = ColorData.GetRaceDisplayColor(razaPlantilla);
            // Los clones van seguidos detrás de la plantilla (el último rombo de raza de serie)
            // The clones go consecutively after the template (the last default race diamond)
            int indicePlantilla = plantilla.transform.GetSiblingIndex();
            var nuevos = new List<Toggle>();

            int creados = 0, teñidos = 0;
            var instancia = inst;
            foreach (Race r in razas)
            {
                string nombre = ((int)r).ToString();
                if (nombres.Contains(nombre)) continue;
                GameObject clon = null;
                try
                {
                    clon = UnityEngine.Object.Instantiate(plantilla.gameObject, padre, false);
                    clon.name = nombre;
                    // Con LayoutGroup, el orden de hermanos es el orden en pantalla: dentro de las flechas, antes de «Especial»
                    // With a LayoutGroup, sibling order is the on-screen order: inside the arrows, before «Especial»
                    if (conLayout) clon.transform.SetSiblingIndex(indicePlantilla + 1 + creados);
                    var t = clon.GetComponent<Toggle>();
                    if (t == null) throw new InvalidOperationException("el clon no tiene Toggle");
                    t.SetIsOnWithoutNotify(false);
                    t.group = grupo;
                    t.interactable = true;
                    var cg = clon.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
                    if (!conLayout && hermanos.Count > 0)
                    {
                        var rt = clon.transform.TryCast<RectTransform>();
                        if (rt != null) rt.anchoredPosition = hermanos[hermanos.Count - 1].anchoredPosition + paso * (creados + 1);
                    }
                    // Gema con el color de la facción nueva (TeñirGema)
                    // Gem with the color of the new faction (TeñirGema)
                    teñidos += TeñirGema(clon, colorPlantilla, ColorData.GetRaceDisplayColor(r));
                    // Al encenderse, el mismo camino que los de serie (OnToggleChanged; el prefix atiende las razas nuevas)
                    // When switched on, the same path as the default ones (OnToggleChanged; the prefix handles the new races)
                    var este = t;
                    t.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<bool>>(new Action<bool>(encendido =>
                    {
                        try { if (encendido) instancia.OnToggleChanged(este); }
                        catch (Exception e) { AvisoUnaVez("romboClic", "Batalla personalizada: rombo de raza nueva: " + e.Message); }
                    })));
                    nuevos.Add(t);
                    creados++;
                }
                catch (Exception e)
                {
                    if (clon != null) UnityEngine.Object.Destroy(clon);
                    AvisoUnaVez("rombo" + (int)r, $"Batalla personalizada: no se pudo crear el rombo de la raza {(int)r}: {e.Message}");
                }
            }
            if (creados > 0 && teñidos == 0)
                AvisoUnaVez("romboColor", $"Batalla personalizada: la gema de los rombos nuevos no se pudo teñir (ni imagen «Icon Gem» ni imagen con el color de {razaPlantilla}); se ven como la de {razaPlantilla} y el nombre sale al elegirlos.");
            if (creados > 0) Plugin.Info($"Batalla personalizada: rombos nuevos clonados de {razaPlantilla} ({(conLayout ? "con LayoutGroup" : "colocados a mano")}, {teñidos} imágenes teñidas).");
            if (nuevos.Count == 0) return creados;

            // Flechas: que lleguen a los rombos nuevos
            // Arrows: so that they reach the new diamonds
            try { Plugin.Info("Batalla personalizada: flechas de los rombos: " + EnlazarFlechas(plantilla, nuevos) + "."); }
            catch (Exception e) { AvisoUnaVez("romboFlechas", "Batalla personalizada: no se pudo enganchar los rombos nuevos a las flechas (se eligen con el ratón): " + e.Message); }
            // Fila: que quepa (solo con LayoutGroup horizontal) y log de dónde queda cada rombo nuevo
            // Row: so that it fits (only with a horizontal LayoutGroup) and a log of where each new diamond ends up
            try
            {
                var fila = padre == null ? null : padre.TryCast<RectTransform>();
                string ajuste = conLayout && fila != null ? AjustarFila(fila) : "sin LayoutGroup (colocados a mano)";
                var partes = nuevos.Select(t => DescribirRect(t.transform.TryCast<RectTransform>(), t.name)).ToList();
                // Las flechas (primer y último hijo activo de la fila) sirven de referencia: los rombos tienen que quedar entre ellas
                // The arrows (first and last active child of the row) serve as a reference: the diamonds must end up between them
                if (fila != null && fila.childCount > 0)
                {
                    var hijos = Enumerable.Range(0, fila.childCount).Select(i => fila.GetChild(i)).Where(h => h != null && h.gameObject.activeSelf).ToList();
                    if (hijos.Count > 1)
                    {
                        partes.Add("izquierda " + DescribirRect(hijos[0].TryCast<RectTransform>(), hijos[0].name));
                        partes.Add("derecha " + DescribirRect(hijos[hijos.Count - 1].TryCast<RectTransform>(), hijos[hijos.Count - 1].name));
                    }
                }
                Plugin.Info($"Batalla personalizada: rombos nuevos colocados ({ajuste}): {string.Join("; ", partes)}.");
            }
            catch (Exception e) { AvisoUnaVez("romboFila", "Batalla personalizada: no se pudo ajustar o medir la fila de rombos: " + e.Message); }
            return creados;
        }

        // Tiñe la gema de un rombo clonado. El juego pinta cada rombo en su imagen «Icon Gem» (DrakosaurBrood #E1FF00,
        // Especial #00D2FF, DeepstoneHold #604434: colores puestos a mano en la escena, no los de ColorData), y para la
        // Liga Olímpica tiñe esa misma imagen del clon de «Especial». Si no hubiera imagen con ese nombre, se tiñen las que
        // tengan el color de interfaz de la raza de la plantilla (lo que se hacía antes). Devuelve las imágenes teñidas.
        // Tints the gem of a cloned diamond. The game paints each diamond in its «Icon Gem» image (DrakosaurBrood #E1FF00,
        // Especial #00D2FF, DeepstoneHold #604434: colors set by hand in the scene, not those of ColorData), and for the
        // Olympian League it tints that same image of the «Especial» clone. If there were no image with that name, those with
        // the template race's UI color are tinted (what was done before). Returns the tinted images.
        private static int TeñirGema(GameObject clon, Color colorPlantilla, Color propio)
        {
            var imagenes = clon.GetComponentsInChildren<Image>(true);
            int n = 0;
            for (int i = 0; imagenes != null && i < imagenes.Length; i++)
            {
                var img = imagenes[i];
                if (img == null || img.name != "Icon Gem") continue;
                img.color = new Color(propio.r, propio.g, propio.b, img.color.a);
                n++;
            }
            if (n > 0) return n;
            for (int i = 0; imagenes != null && i < imagenes.Length; i++)
            {
                var img = imagenes[i];
                if (img == null || !Parecido(img.color, colorPlantilla)) continue;
                img.color = new Color(propio.r, propio.g, propio.b, img.color.a);
                n++;
            }
            return n;
        }

        // Cadena de las flechas. El juego cambia de rombo con las flechas así: ChangeToggle → SampleNavInputHelper del rombo
        // activo → SelectNext/SelectPrevious → next/previous.isOn = true (referencias puestas en la escena). Los clones
        // heredan las de la plantilla, así que las flechas nunca llegaban a ellos. Queda: plantilla → 40 → 41 → el que
        // seguía a la plantilla («Especial»), y ese vuelve al último clon con la flecha izquierda. Devuelve la cadena.
        // Arrow chain. The game switches diamonds with the arrows like this: ChangeToggle → SampleNavInputHelper of the active
        // diamond → SelectNext/SelectPrevious → next/previous.isOn = true (references set in the scene). The clones
        // inherit those of the template, so the arrows never reached them. Result: template → 40 → 41 → the one that
        // followed the template («Especial»), and that one goes back to the last clone with the left arrow. Returns the chain.
        private static string EnlazarFlechas(Toggle plantilla, List<Toggle> nuevos)
        {
            var navPlantilla = plantilla.GetComponent<Synty.Interface.FantasyMenus.Samples.SampleNavInputHelper>();
            if (navPlantilla == null) return "la plantilla no tiene SampleNavInputHelper (las flechas no llegan a los rombos nuevos)";
            var siguiente = navPlantilla.next;
            var anterior = plantilla;
            var navAnterior = navPlantilla;
            var cadena = new List<string> { plantilla.name };
            foreach (var t in nuevos)
            {
                var nav = t.GetComponent<Synty.Interface.FantasyMenus.Samples.SampleNavInputHelper>();
                if (nav == null) continue;
                nav.previous = anterior;
                navAnterior.next = t;
                anterior = t;
                navAnterior = nav;
                cadena.Add(t.name);
            }
            navAnterior.next = siguiente;
            if (siguiente != null)
            {
                var navSiguiente = siguiente.GetComponent<Synty.Interface.FantasyMenus.Samples.SampleNavInputHelper>();
                if (navSiguiente != null && navSiguiente.previous != null && navSiguiente.previous.Pointer == plantilla.Pointer) navSiguiente.previous = anterior;
            }
            cadena.Add(siguiente == null ? "(nada)" : siguiente.name);
            return string.Join(" -> ", cadena) + " (y de vuelta con la izquierda)";
        }

        // Ajusta la separación del HorizontalLayoutGroup de la fila de rombos para que todo (flechas incluidas) quepa en su
        // ancho. Solo la reduce, nunca la agranda. Mide como el propio grupo: ancho del rect (el grupo no controla tamaños)
        // por la escala si el grupo la usa (las flechas van a escala 5). Devuelve un resumen para el log.
        // Adjusts the spacing of the diamond row's HorizontalLayoutGroup so that everything (arrows included) fits in its
        // width. It only reduces it, never increases it. Measures like the group itself: rect width (the group does not control sizes)
        // times the scale if the group uses it (the arrows are at scale 5). Returns a summary for the log.
        private static string AjustarFila(RectTransform fila)
        {
            var hlg = fila.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) return "la fila no tiene HorizontalLayoutGroup (no se ajusta)";
            LayoutRebuilder.ForceRebuildLayoutImmediate(fila);
            float suma = 0f;
            int n = 0;
            for (int i = 0; i < fila.childCount; i++)
            {
                var rt = fila.GetChild(i).TryCast<RectTransform>();
                if (rt == null || !rt.gameObject.activeSelf) continue;
                var le = rt.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;
                // Igual que el grupo: sin controlar tamaños usa sizeDelta.x (ignora los LayoutElement de los hijos)
                // Like the group: without controlling sizes it uses sizeDelta.x (ignores the children's LayoutElements)
                float w = hlg.childControlWidth ? LayoutUtility.GetPreferredWidth(rt) : rt.sizeDelta.x;
                // Si aún no tiene tamaño (su ContentSizeFitter no ha corrido), el que pide su LayoutElement
                // If it has no size yet (its ContentSizeFitter has not run), the one requested by its LayoutElement
                if (w <= 0.5f && le != null) w = Math.Max(le.preferredWidth, le.minWidth);
                if (hlg.childScaleWidth) w *= Math.Abs(rt.localScale.x);
                suma += Math.Max(0f, w);
                n++;
            }
            float disponible = fila.rect.width - hlg.padding.horizontal;
            float antes = hlg.spacing;
            if (n > 1 && suma + antes * (n - 1) > disponible + 0.5f)
            {
                hlg.spacing = Math.Max(0f, (disponible - suma) / (n - 1));
                LayoutRebuilder.ForceRebuildLayoutImmediate(fila);
            }
            float ocupado = suma + hlg.spacing * Math.Max(0, n - 1);
            return $"fila de {n} elementos, ancho {ocupado:0} de {disponible:0}, separación {antes:0.#} -> {hlg.spacing:0.#}" +
                   (ocupado > disponible + 0.5f ? " (NO cabe ni sin separación: los extremos se salen)" : "");
        }

        // Descripción de un rect para el log: activo, índice entre hermanos, posición local y rectángulo en pantalla
        // (píxeles; «FUERA» si se sale de la pantalla)
        // Description of a rect for the log: active, index among siblings, local position and on-screen rectangle
        // (pixels; «FUERA» if it goes off screen)
        private static string DescribirRect(RectTransform rt, string nombre)
        {
            if (rt == null) return $"«{nombre}» sin RectTransform";
            var esquinas = new Il2CppStructArray<Vector3>(4);
            rt.GetWorldCorners(esquinas);
            var canvas = rt.GetComponentInParent<Canvas>();
            var raiz = canvas == null ? null : canvas.rootCanvas;
            Camera cam = raiz != null && raiz.renderMode != RenderMode.ScreenSpaceOverlay ? raiz.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, esquinas[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, esquinas[2]);
            bool dentro = a.x >= -0.5f && a.y >= -0.5f && b.x <= Screen.width + 0.5f && b.y <= Screen.height + 0.5f;
            return $"«{nombre}» activo {rt.gameObject.activeInHierarchy}, índice {rt.GetSiblingIndex()}, pos ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0}), " +
                   $"pantalla ({a.x:0},{a.y:0})-({b.x:0},{b.y:0}) de {Screen.width}x{Screen.height} {(dentro ? "dentro" : "FUERA")}";
        }

        // Dos colores parecidos (RGB, sin alfa)
        // Two similar colors (RGB, without alpha)
        private static bool Parecido(Color a, Color b) =>
            Math.Abs(a.r - b.r) < 0.06f && Math.Abs(a.g - b.g) < 0.06f && Math.Abs(a.b - b.b) < 0.06f;

        // Prefix de CustomBattleUIManager.OnToggleChanged: los rombos de las razas nuevas («40», «41») se atienden aquí
        // (elegir raza, recordar el rombo y el clic) sin pasar por el Enum.Parse del juego
        // Prefix of CustomBattleUIManager.OnToggleChanged: the diamonds of the new races («40», «41») are handled here
        // (choosing race, remembering the diamond and the click) without going through the game's Enum.Parse
        public static bool AntesDeOnToggleChanged(CustomBattleUIManager __instance, Toggle __0)
        {
            try
            {
                if (__instance == null || __0 == null || !__0.isOn) return true;
                if (!int.TryParse(__0.name, out int n) || !FaccionesNuevas.EsNueva(n)) return true;
                var ultimo = __instance.lastActiveToggle;
                if (ultimo != null && ultimo.Pointer == __0.Pointer) return false;
                __instance.SelectRace((Race)n);
                __instance.lastActiveToggle = __0;
                try { Memori.Audio.IAudioRequester.Instance.PlaySFX("tiny-click", null); } catch { }
                return false;
            }
            catch (Exception e)
            {
                AvisoUnaVez("onToggle", "Batalla personalizada: AntesDeOnToggleChanged (sigue el del juego): " + e.Message);
                return true;
            }
        }

        // Postfix de CustomBattleUIManager.SelectRace: la ventana de cartas vuelve arriba al cambiar de raza
        // Postfix of CustomBattleUIManager.SelectRace: the card window goes back to the top when changing race
        public static void TrasSelectRace(CustomBattleUIManager __instance)
        {
            try
            {
                var rejilla = __instance?.unitUIParent == null ? null : __instance.unitUIParent.TryCast<RectTransform>();
                var ventana = rejilla == null ? null : VentanaDe(rejilla, NombreVentanaBatalla);
                var sr = ventana == null ? null : ventana.GetComponent<ScrollRect>();
                if (sr != null) sr.verticalNormalizedPosition = 1f;
            }
            catch (Exception e) { AvisoUnaVez("selectRace", "Batalla personalizada: TrasSelectRace: " + e.Message); }
        }

        // Desplazamiento de la rejilla de cartas: solo si alguna raza tiene más filas que la raza de serie más grande. La
        // ventana mide esas filas (lo que la pantalla de serie enseña entero) y empieza donde empezaba la rejilla.
        // Card grid scrolling: only if some race has more rows than the largest default race. The
        // window measures those rows (what the default screen shows in full) and starts where the grid started.
        private static void MontarDesplazamientoBatalla(CustomBattleUIManager inst, TabletopTavernData data)
        {
            var rejilla = inst.unitUIParent == null ? null : inst.unitUIParent.TryCast<RectTransform>();
            var cartas = inst.unitUIPrefabs;
            if (rejilla == null || cartas == null || VentanaDe(rejilla, NombreVentanaBatalla) != null) return;
            var glg = rejilla.GetComponent<GridLayoutGroup>();
            if (glg == null) { AvisoUnaVez("batSinGrid", "Batalla personalizada: la rejilla de cartas no tiene GridLayoutGroup; se queda sin desplazamiento."); return; }

            // Cartas por raza (de serie y nuevas) y, aparte, solo las de serie (id < 900) de cada raza
            // Cards per race (default and new) and, separately, only the default ones (id < 900) of each race
            var porRaza = new Dictionary<int, List<int>>();
            for (int i = 0; i < cartas.Count; i++)
            {
                var c = cartas[i];
                if (c == null) continue;
                int raza = (int)data.GetRaceFromUnitName(c.UnitName);
                if (!porRaza.TryGetValue(raza, out var l)) porRaza[raza] = l = new List<int>();
                l.Add((int)c.UnitName);
            }
            if (porRaza.Count == 0) return;
            // El panel se acaba de encender: se maqueta antes de medir el ancho (si no, podría medir 0 y salir 1 columna)
            // The panel has just been switched on: it is laid out before measuring the width (otherwise it could measure 0 and yield 1 column)
            try { LayoutRebuilder.ForceRebuildLayoutImmediate(rejilla.parent != null ? rejilla.parent.TryCast<RectTransform>() ?? rejilla : rejilla); } catch { }
            bool fijas = glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount && glg.constraintCount > 0;
            if (!fijas && rejilla.rect.width <= 1f)
            {
                AvisoUnaVez("batAncho", "Batalla personalizada: la rejilla de cartas aún no tiene ancho; no se monta el desplazamiento (se queda la de serie).");
                return;
            }
            int columnas = RejillaDesplazable.Columnas(glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount, glg.constraintCount,
                rejilla.rect.width, glg.padding.horizontal, glg.cellSize.x, glg.spacing.x);
            int maxSerie = RejillaDesplazable.MaximoDeSerie(porRaza.Values.Select(l => (IEnumerable<int>)l));
            int filasMax = Math.Max(1, RejillaDesplazable.FilasNecesarias(maxSerie, columnas));
            int maxActual = porRaza.Values.Max(l => l.Count);
            if (!fijas && columnas == 1)
            {
                AvisoUnaVez("batColumna", $"Batalla personalizada: sale 1 sola columna (ancho {rejilla.rect.width:0}); no se monta el desplazamiento por si la medida es mala.");
                return;
            }
            if (!RejillaDesplazable.NecesitaDesplazamiento(maxActual, columnas, filasMax))
            {
                if (_volcados.Add("batalla-nohace"))
                    Plugin.Info($"Batalla personalizada: la rejilla no necesita desplazamiento ({maxActual} cartas como mucho, {columnas} columnas, {filasMax} filas de serie).");
                return;
            }

            // Si la rejilla ya es el contenido de un ScrollRect del juego, basta con encenderlo en vertical
            // If the grid is already the content of a game ScrollRect, it is enough to enable it vertically
            ScrollRect existente = null;
            for (Transform t = rejilla.parent; t != null && existente == null; t = t.parent)
            {
                var sr = t.GetComponent<ScrollRect>();
                if (sr != null && sr.content != null && sr.content.Pointer == rejilla.Pointer) existente = sr;
            }
            float h = RejillaDesplazable.AlturaFilas(filasMax, glg.cellSize.y, glg.spacing.y, glg.padding.top, glg.padding.bottom);
            if (existente != null)
            {
                existente.enabled = true;
                existente.vertical = true;
                existente.movementType = ScrollRect.MovementType.Clamped;
                var vista = existente.viewport != null ? existente.viewport : existente.transform.TryCast<RectTransform>();
                if (vista != null && vista.GetComponent<RectMask2D>() == null && vista.GetComponent<Mask>() == null) vista.gameObject.AddComponent<RectMask2D>();
                var csf = rejilla.GetComponent<ContentSizeFitter>() ?? rejilla.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Plugin.Info($"Batalla personalizada: la rejilla ya estaba en un ScrollRect ({existente.name}); se activa el desplazamiento vertical.");
                return;
            }

            var ventana = MontarVentana(rejilla, NombreVentanaBatalla, out var cambios);
            if (ventana == null) return;
            try { FijarAltura(ventana, h); }
            catch (Exception e) { Deshacer(cambios); AvisoUnaVez("batAltura", "Batalla personalizada: no se pudo fijar la altura de la ventana (" + e.Message + "); se queda sin desplazamiento."); return; }
            Plugin.Info($"Batalla personalizada: rejilla de cartas con desplazamiento vertical ({columnas} columnas; ventana de {filasMax} filas = {h:0} px; la raza más grande tiene {maxActual} cartas).");
        }

        // ===================================================================
        // VENTANA CON DESPLAZAMIENTO (común a las dos pantallas)
        // SCROLLING WINDOW (shared by both screens)
        // ===================================================================

        // Todo lo creado o cambiado al montar una ventana, para deshacerlo si algo falla a medias
        // Everything created or changed when building a window, to undo it if something fails halfway
        private sealed class Cambios
        {
            public GameObject Ventana;
            public RectTransform Rejilla;
            public Transform Padre;
            public int Indice = -1;
            public Vector2 AnclaMin, AnclaMax, Pivote, Posicion, Tamano;
            public Vector3 Escala = Vector3.one;
            public ContentSizeFitter AjusteCreado;
            public ContentSizeFitter AjusteExistente;
            public ContentSizeFitter.FitMode ModoAnterior;
        }

        // Ventana que envuelve a la rejilla (su padre, si se llama así), o null
        // Window that wraps the grid (its parent, if it has that name), or null
        private static RectTransform VentanaDe(RectTransform rejilla, string nombre)
        {
            var p = rejilla.parent;
            if (p == null || p.name != nombre) return null;
            return p.TryCast<RectTransform>();
        }

        /// <summary>
        /// Mete la rejilla en una ventana nueva en su mismo sitio (mismo padre, posición entre hermanos, anclas y
        /// LayoutElement) con fondo transparente (recoge la rueda también en los huecos), RectMask2D y ScrollRect vertical.
        /// La rejilla queda anclada arriba y con la altura de su contenido (ContentSizeFitter). La altura de la ventana la
        /// pone después quien llama (FijarAltura). Si algo falla, se deshace y devuelve null.
        /// Puts the grid into a new window in the same place (same parent, sibling position, anchors and
        /// LayoutElement) with a transparent background (it also catches the wheel in the gaps), RectMask2D and a vertical ScrollRect.
        /// The grid stays anchored at the top and sized to its content (ContentSizeFitter). The window height is
        /// set afterwards by the caller (FijarAltura). If anything fails, it is undone and returns null.
        /// </summary>
        private static RectTransform MontarVentana(RectTransform rejilla, string nombre, out Cambios c)
        {
            c = new Cambios
            {
                Rejilla = rejilla, Padre = rejilla.parent, Indice = rejilla.GetSiblingIndex(),
                AnclaMin = rejilla.anchorMin, AnclaMax = rejilla.anchorMax, Pivote = rejilla.pivot,
                Posicion = rejilla.anchoredPosition, Tamano = rejilla.sizeDelta, Escala = rejilla.localScale,
            };
            try
            {
                if (c.Padre == null) throw new InvalidOperationException("la rejilla no tiene padre");
                var go = new GameObject(nombre);
                c.Ventana = go;
                go.layer = rejilla.gameObject.layer;
                var rt = go.AddComponent<RectTransform>();
                rt.SetParent(c.Padre, false);
                rt.SetSiblingIndex(c.Indice);
                rt.anchorMin = c.AnclaMin; rt.anchorMax = c.AnclaMax; rt.pivot = c.Pivote;
                rt.anchoredPosition = c.Posicion; rt.sizeDelta = c.Tamano;
                rt.localScale = rejilla.localScale;
                // Mismo comportamiento en la maquetación del padre que tenía la rejilla
                // Same behavior in the parent's layout as the grid had
                var leRejilla = rejilla.GetComponent<LayoutElement>();
                var le = go.AddComponent<LayoutElement>();
                if (leRejilla != null)
                {
                    le.ignoreLayout = leRejilla.ignoreLayout;
                    le.minWidth = leRejilla.minWidth; le.preferredWidth = leRejilla.preferredWidth; le.flexibleWidth = leRejilla.flexibleWidth;
                    le.layoutPriority = leRejilla.layoutPriority;
                }
                // Fondo transparente que recoge la rueda del ratón también entre casillas
                // Transparent background that catches the mouse wheel between cells too
                var fondo = go.AddComponent<Image>();
                fondo.color = new Color(0f, 0f, 0f, 0f);
                fondo.raycastTarget = true;
                go.AddComponent<RectMask2D>();
                var sr = go.AddComponent<ScrollRect>();
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.inertia = true;
                sr.scrollSensitivity = 30f;
                sr.viewport = rt;

                // La rejilla dentro, anclada arriba a todo el ancho y con la altura de su contenido
                // The grid inside, anchored at the top across the full width and sized to its content
                rejilla.SetParent(rt, false);
                // La escala ya la lleva la ventana (copiada de la rejilla): la rejilla queda a 1 para no aplicarla dos veces
                // The scale is already carried by the window (copied from the grid): the grid stays at 1 so it is not applied twice
                rejilla.localScale = Vector3.one;
                rejilla.anchorMin = new Vector2(0f, 1f);
                rejilla.anchorMax = new Vector2(1f, 1f);
                rejilla.pivot = new Vector2(0.5f, 1f);
                rejilla.anchoredPosition = Vector2.zero;
                rejilla.sizeDelta = new Vector2(0f, Math.Max(1f, c.Tamano.y));
                var csf = rejilla.GetComponent<ContentSizeFitter>();
                if (csf == null) { csf = rejilla.gameObject.AddComponent<ContentSizeFitter>(); c.AjusteCreado = csf; }
                else { c.AjusteExistente = csf; c.ModoAnterior = csf.verticalFit; }
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                sr.content = rejilla;
                sr.verticalNormalizedPosition = 1f;
                return rt;
            }
            catch (Exception e)
            {
                Deshacer(c);
                AvisoUnaVez("ventana" + nombre, $"{nombre}: no se pudo montar la ventana con desplazamiento ({e.Message}); se queda la rejilla de serie.");
                return null;
            }
        }

        /// <summary>
        /// Altura de la ventana: en un padre con LayoutGroup manda el LayoutElement (mínima y preferida = h, sin estirarse);
        /// sin LayoutGroup, se conserva el borde de arriba y se fija la altura con las anclas.
        /// Window height: in a parent with a LayoutGroup the LayoutElement rules (minimum and preferred = h, no stretching);
        /// without a LayoutGroup, the top edge is kept and the height is set with the anchors.
        /// </summary>
        private static void FijarAltura(RectTransform ventana, float h)
        {
            var le = ventana.GetComponent<LayoutElement>() ?? ventana.gameObject.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
            var padre = ventana.parent;
            bool conLayout = padre != null && padre.GetComponent<LayoutGroup>() != null && !le.ignoreLayout;
            if (!conLayout)
            {
                // Mismo borde de arriba: si estaba estirada en vertical, pasa a anclarse en su borde superior
                // Same top edge: if it was stretched vertically, it becomes anchored to its top edge
                float arriba = ventana.offsetMax.y;
                var amin = ventana.anchorMin; var amax = ventana.anchorMax;
                if (Math.Abs(amin.y - amax.y) > 0.0001f) ventana.anchorMin = new Vector2(amin.x, amax.y);
                ventana.offsetMin = new Vector2(ventana.offsetMin.x, arriba - h);
                ventana.offsetMax = new Vector2(ventana.offsetMax.x, arriba);
            }
            var padreRT = padre == null ? null : padre.TryCast<RectTransform>();
            if (padreRT != null) LayoutRebuilder.MarkLayoutForRebuild(padreRT);
        }

        // Deshace una ventana: la rejilla vuelve a su padre, sitio, anclas y tamaño; fuera el ContentSizeFitter puesto y la ventana
        // Undoes a window: the grid goes back to its parent, position, anchors and size; the ContentSizeFitter that was added and the window are removed
        private static void Deshacer(Cambios c)
        {
            if (c == null) return;
            try
            {
                if (c.AjusteCreado != null) UnityEngine.Object.Destroy(c.AjusteCreado);
                if (c.AjusteExistente != null) c.AjusteExistente.verticalFit = c.ModoAnterior;
                if (c.Rejilla != null && c.Padre != null)
                {
                    c.Rejilla.SetParent(c.Padre, false);
                    if (c.Indice >= 0) c.Rejilla.SetSiblingIndex(c.Indice);
                    c.Rejilla.anchorMin = c.AnclaMin; c.Rejilla.anchorMax = c.AnclaMax; c.Rejilla.pivot = c.Pivote;
                    c.Rejilla.anchoredPosition = c.Posicion; c.Rejilla.sizeDelta = c.Tamano;
                    c.Rejilla.localScale = c.Escala;
                }
                if (c.Ventana != null) UnityEngine.Object.Destroy(c.Ventana);
            }
            catch (Exception e) { Plugin.Error("Interfaz: error al deshacer la ventana con desplazamiento: " + e.Message); }
        }

        // ===================================================================
        // DIAGNÓSTICO: jerarquía de la pantalla en el log (una vez por sesión y pantalla)
        // DIAGNOSTICS: screen hierarchy in the log (once per session and screen)
        // ===================================================================

        // Escribe la cadena de padres de la rejilla (con sus componentes y tamaños) y el árbol de «otro» (rombos o lista)
        // Writes the grid's parent chain (with its components and sizes) and the tree of «otro» (diamonds or list)
        private static void VolcarUnaVez(string clave, string titulo, RectTransform rejilla, int padres, Transform otro)
        {
            if (!_volcados.Add("volcado-" + clave)) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append("Interfaz [jerarquía] ").Append(titulo).Append(":\n");
                if (rejilla != null)
                {
                    sb.Append("  rejilla: ").Append(Describir(rejilla)).Append('\n');
                    var glg = rejilla.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                        sb.Append($"    grid: celda {glg.cellSize.x:0}x{glg.cellSize.y:0}, espacio {glg.spacing.x:0}x{glg.spacing.y:0}, relleno {glg.padding.left}/{glg.padding.right}/{glg.padding.top}/{glg.padding.bottom}, restricción {glg.constraint} {glg.constraintCount}, hijos {rejilla.childCount}\n");
                    Transform t = rejilla.parent;
                    for (int i = 0; t != null && i < padres; i++, t = t.parent)
                        sb.Append("  padre ").Append(i + 1).Append(": ").Append(Describir(t)).Append('\n');
                }
                if (otro != null) Arbol(sb, otro, 0, 3, new int[] { 0 }, 60);
                Plugin.Info(sb.ToString().TrimEnd());
            }
            catch (Exception e) { AvisoUnaVez("volcado" + clave, "Interfaz: no se pudo escribir la jerarquía de " + titulo + ": " + e.Message); }
        }

        // Árbol de un objeto hasta cierta profundidad (como mucho «max» líneas)
        // Tree of an object down to a given depth (at most «max» lines)
        private static void Arbol(StringBuilder sb, Transform t, int nivel, int maxNivel, int[] lineas, int max)
        {
            if (t == null || lineas[0] >= max) return;
            lineas[0]++;
            sb.Append("  ").Append(new string(' ', nivel * 2)).Append("- ").Append(Describir(t)).Append('\n');
            if (nivel >= maxNivel) return;
            for (int i = 0; i < t.childCount; i++) Arbol(sb, t.GetChild(i), nivel + 1, maxNivel, lineas, max);
        }

        // Nombre, si está activo, componentes, tamaño y anclas, y el sprite/color de su imagen si la tiene
        // Name, whether it is active, components, size and anchors, and the sprite/color of its image if it has one
        private static string Describir(Transform t)
        {
            var sb = new StringBuilder();
            sb.Append('«').Append(t.name).Append('»').Append(t.gameObject.activeSelf ? "" : " [apagado]");
            try
            {
                var comps = t.GetComponents<Component>();
                var tipos = new List<string>();
                for (int i = 0; comps != null && i < comps.Length; i++)
                    if (comps[i] != null) tipos.Add(comps[i].GetIl2CppType().Name);
                sb.Append(" (").Append(string.Join(", ", tipos.Where(x => x != "Transform" && x != "RectTransform" && x != "CanvasRenderer"))).Append(')');
            }
            catch { }
            var rt = t.TryCast<RectTransform>();
            if (rt != null)
                sb.Append($" {rt.rect.width:0}x{rt.rect.height:0} anclas ({rt.anchorMin.x:0.##},{rt.anchorMin.y:0.##})-({rt.anchorMax.x:0.##},{rt.anchorMax.y:0.##}) pivote ({rt.pivot.x:0.##},{rt.pivot.y:0.##}) pos ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})");
            var img = t.GetComponent<Image>();
            if (img != null)
                sb.Append($" img «{(img.sprite != null ? img.sprite.name : "-")}» color #{ColorUtility.ToHtmlStringRGBA(img.color)}");
            return sb.ToString();
        }
    }
}
