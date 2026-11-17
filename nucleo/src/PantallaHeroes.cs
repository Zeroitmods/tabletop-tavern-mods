// PantallaHeroes.cs
// Fase F2 (decisión de Zero, diseño «Decisiones de Zero» y §6.3): la pantalla de héroes de la nueva partida tiene un
// número fijo de casillas generado en el editor (RunSetupBuilder) y descarta en silencio los héroes que sobran.
// Juego instalado (actualización del 17-10-2026, leído del código nativo; el código fuente público es anterior): 20
// casillas = 10 tarjetas; OpenPanel pasa los héroes jugables (16 de serie + 901-904) seguidos de los 2 «próximamente»
// (olímpicos, raza aún no incluida) y LoadRoster enseña min(casillas - 2, héroes): las 2 últimas casillas y la última
// tarjeta son SIEMPRE la facción misteriosa «??????????». Sin ampliar salían 18 (Astra sí, Imperio y olímpicos no).
// Cuentas en CasillasHeroes.cs: hacen falta jugables + próximamente + 2 = 24 casillas = 12 tarjetas (2 clones).
// Qué hace el plugin, en un prefix de PlayPanel.OpenPanel (la primera vez que hacen falta más casillas):
//   1) Clona en tiempo de ejecución la última tarjeta NORMAL (la penúltima: la última es la misteriosa) tantas veces
//      como falten y pone los clones justo antes de la misteriosa, que sigue la última (donde la busca el juego).
//   2) Amplía los campos privados tiles, factionNames y factionPips en ese mismo orden (localizados en el clon por la
//      misma ruta de hijos que en la tarjeta original).
//   3) Mete la rejilla de facciones en un contenedor con desplazamiento vertical (ScrollRect + RectMask2D) de la altura
//      que ocupaban las 8 tarjetas de serie a tamaño completo; lo que no cabe se ve bajando con la rueda del ratón.
//      (Con más de 8 tarjetas el propio juego las compacta en LoadRoster: FitNineFactions.)
// Orden resultante: 8 facciones de serie, facción A, facción B, olímpicos «próximamente» y misteriosa.
// La ampliación se hace en un prefix de PlayPanel.OpenPanel, antes de que el panel lea las casillas (las guarda para
// elegir la del último héroe usado). Ampliar más tarde (en LoadRoster) dejaría al panel con el array viejo, así que si
// ese parche no engancha no se amplía (0.5.1).
// Un postfix de LoadRoster ajusta los títulos de facción que no caben en su tarjeta (en español «FUERTE DE
// PIEDRAPROFUNDA» se montaba sobre el de al lado): menos espacio entre letras y, si no basta, letra algo más pequeña.
// Al abrir con un héroe nuevo como último usado, la ventana baja sola hasta su tarjeta.
// Si el juego no trae la facción misteriosa ni los «próximamente» (versión anterior), se amplía como antes: sin reserva.
// Si algo falla, deshace todo (borra los clones, devuelve la rejilla a su sitio y los campos a sus valores) y la pantalla
// queda como la de serie: los héroes nuevos no se pueden elegir, pero siguen existiendo como enemigos. (El panel de
// colección recorre el enum de razas y no enseña las facciones nuevas, con o sin esta pantalla.)
// Marcado «sin probar» en el diseño: se comprueba con Diagnostico.ProbarPantallaHeroes (DiagnosticoPantalla.cs).
// PantallaHeroes.cs
// Phase F2 (Zero's decision, design «Decisiones de Zero» and §6.3): the hero screen of the new run has a
// fixed number of slots generated in the editor (RunSetupBuilder) and silently discards any surplus heroes.
// Installed game (update of 17-10-2026, read from the native code; the public source code is older): 20
// slots = 10 cards; OpenPanel passes the playable heroes (16 stock + 901-904) followed by the 2 «coming soon»
// (Olympians, race not yet included) and LoadRoster shows min(slots - 2, heroes): the last 2 slots and the last
// card are ALWAYS the mystery faction «??????????». Without expanding, 18 were shown (Astra yes, Empire and Olympians no).
// Calculations in CasillasHeroes.cs: playable + coming soon + 2 = 24 slots = 12 cards (2 clones) are needed.
// What the plugin does, in a prefix of PlayPanel.OpenPanel (the first time more slots are needed):
//   1) Clones at runtime the last NORMAL card (the second to last: the last one is the mystery one) as many times
//      as needed and places the clones right before the mystery one, which stays last (where the game looks for it).
//   2) Expands the private fields tiles, factionNames and factionPips in that same order (located in the clone by the
//      same child path as in the original card).
//   3) Puts the faction grid inside a vertically scrolling container (ScrollRect + RectMask2D) with the height that the
//      8 stock cards occupied at full size; whatever does not fit is seen by scrolling with the mouse wheel.
//      (With more than 8 cards the game itself compacts them in LoadRoster: FitNineFactions.)
// Resulting order: 8 stock factions, faction A, faction B, Olympians «coming soon» and mystery.
// The expansion is done in a prefix of PlayPanel.OpenPanel, before the panel reads the slots (it stores them in order to
// pick the one for the last hero used). Expanding later (in LoadRoster) would leave the panel with the old array, so if
// that patch does not hook, no expansion is done (0.5.1).
// A postfix of LoadRoster adjusts the faction titles that do not fit in their card (in Spanish «FUERTE DE
// PIEDRAPROFUNDA» overlapped the one next to it): less letter spacing and, if that is not enough, a somewhat smaller font.
// When opening with a new hero as the last one used, the window scrolls down by itself to its card.
// If the game has neither the mystery faction nor the «coming soon» ones (earlier version), it expands as before: with no reserve.
// If anything fails, it undoes everything (deletes the clones, puts the grid back in place and the fields back to their values) and the screen
// ends up like the stock one: the new heroes cannot be chosen, but they still exist as enemies. (The Collection panel
// iterates over the race enum and does not show the new factions, with or without this screen.)
// Marked «untested» in the design: it is checked with Diagnostico.ProbarPantallaHeroes (DiagnosticoPantalla.cs).

using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TJ;
using TJ.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: ampliación de la pantalla de héroes
    // Section: hero screen expansion
    // -----------------------------------------------------------------------
    public static class PantallaHeroes
    {
        public const string NombreDesplazamiento = "ZeroDesplazamientoFacciones";
        public const string PrefijoTarjeta = "Faction Zero ";

        /// <summary>Estado de la última ampliación: «sin abrir», «ampliada (...)», «de serie (...)» tras un fallo.</summary>
        /// Status of the last expansion: «sin abrir», «ampliada (...)», «de serie (...)» after a failure.
        public static string Estado { get; private set; } = "sin abrir todavía";
        public static int CasillasAnadidas { get; private set; }
        /// <summary>Casillas de serie que tenía la pantalla la primera vez que se abrió en la sesión (-1 = aún sin abrir).</summary>
        /// Stock slots the screen had the first time it was opened in the session (-1 = not opened yet).
        public static int CasillasDeSerie { get; private set; } = -1;
        // Vistas ya revisadas, por id de instancia de Unity (el puntero del envoltorio puede reutilizarse)
        // Views already reviewed, by Unity instance id (the wrapper pointer may be reused)
        private static readonly HashSet<int> _ampliadas = new HashSet<int>();
        // Componente de recorte de la última ampliación (para bajar la ventana al abrir con un héroe nuevo)
        // Clipping component from the latest expansion (to scroll the window down when opening with a new hero)
        private static RecorteTarjetas _recorte;

        // Prefix de PlayPanel.OpenPanel: amplía ANTES de que el panel lea las casillas (las guarda en una variable y con
        // ella elige la casilla del último héroe usado; con el array viejo, un héroe nuevo se saldría del array)
        // PlayPanel.OpenPanel prefix: expands BEFORE the panel reads the tiles (it stores them in a variable and uses
        // it to pick the tile of the last hero used; with the old array, a new hero would fall outside the array)
        public static void AntesDeOpenPanel(PlayPanel __instance)
        {
            try
            {
                if (!Plugin.Operativo || !FaccionesNuevas.Activas || !Plugin.CfgPantallaHeroes.Value || __instance == null) return;
                var lista = HeroData._heroesSorted;
                if (lista != null)
                {
                    InformarRecuentos(__instance.commanderView, lista);
                    Revisar(__instance.commanderView, lista.Count, ContarProximamente());
                }
                // Si el héroe con el que se abre es nuevo, su tarjeta está abajo: la ventana baja sola al abrir
                // If the hero it opens with is new, its card is at the bottom: the window scrolls down by itself on open
                if (_recorte != null)
                {
                    int ultimo = Memori.SaveData.SaveDataHandler.LoadPlayerSaveData().lastHeroID;
                    _recorte.Destino = FaccionesNuevas.EsHeroeNuevo(ultimo) ? 0f : 1f;
                }
            }
            catch (Exception e) { Plugin.Error("AntesDeOpenPanel: " + e); }
        }

        // Amplía la vista si los héroes (jugables + próximamente + la reserva de la facción misteriosa) no caben en las
        // casillas. «proximamente» = -1 si el juego no tiene esa API (versión anterior: sin reserva). La vista se apunta
        // como hecha solo si la ampliación salió bien (0.6.1); si falla (y se deshace), se reintenta en la siguiente
        // apertura, como mucho 2 veces por vista
        // Expands the view if the heroes (playable + coming soon + the mystery faction's reserve) don't fit in the
        // tiles. "proximamente" = -1 if the game doesn't have that API (older version: no reserve). The view is marked
        // as done only if the expansion went well (0.6.1); if it fails (and is rolled back), it is retried on the next
        // opening, at most 2 times per view
        private static void Revisar(CommanderScreenView v, int jugables, int proximamente)
        {
            if (v == null) return;
            var tiles = v.tiles;
            int id = v.GetInstanceID();
            int reserva = proximamente >= 0 ? CasillasHeroes.CasillasMisterio : 0;
            int necesarias = CasillasHeroes.Necesarias(jugables, proximamente, reserva);
            if (tiles == null || necesarias <= tiles.Length || _ampliadas.Contains(id)) return;
            _fallidas.TryGetValue(id, out int intentos);
            if (intentos >= 2) return;
            if (Ampliar(v, jugables, Math.Max(0, proximamente), reserva)) _ampliadas.Add(id);
            else _fallidas[id] = intentos + 1;
        }
        private static readonly Dictionary<int, int> _fallidas = new Dictionary<int, int>();

        // Diagnóstico (una vez por sesión): recuentos que ve la pantalla al abrirse. El juego pasa a LoadRoster los jugables
        // (HeroData.Heroes = _heroesSorted) seguidos de los «próximamente»; el aviso del juego «N heroes but M roster
        // tiles» cuenta los dos juntos. Si hubiera ids repetidos en la lista de jugables, se dice aquí
        // Diagnostics (once per session): counts the screen sees when it opens. The game passes the playable heroes to LoadRoster
        // (HeroData.Heroes = _heroesSorted) followed by the "coming soon" ones; the game's warning "N heroes but M roster
        // tiles" counts both together. If there were repeated ids in the playable list, it is reported here
        private static void InformarRecuentos(CommanderScreenView v, Il2CppSystem.Collections.Generic.List<Hero> lista)
        {
            if (_recuentosInformados) return;
            _recuentosInformados = true;
            try
            {
                var ids = new List<int>();
                for (int i = 0; i < lista.Count; i++) ids.Add(lista[i].HeroID);
                var repetidos = new List<int>();
                var vistos = new HashSet<int>();
                foreach (int id in ids) if (!vistos.Add(id) && !repetidos.Contains(id)) repetidos.Add(id);
                string prox = "sin API";
                try { prox = LeerIdsProximamente(); } catch (Exception e) { prox = "sin API (" + e.GetType().Name + ")"; }
                int casillas = v?.tiles?.Length ?? -1;
                // Antes de ampliar: son las casillas de serie del juego instalado (las usa la autoprueba)
                // Before expanding: these are the stock tiles of the installed game (used by the self-test)
                if (casillas > 0) CasillasDeSerie = casillas;
                Plugin.Info($"Pantalla de héroes al abrir: {ids.Count} jugables [{string.Join(", ", ids)}]; próximamente {prox}; " +
                            $"{casillas} casillas; repetidos: {(repetidos.Count == 0 ? "ninguno" : string.Join(", ", repetidos))}.");
            }
            catch (Exception e) { Plugin.Aviso("Pantalla de héroes: no se pudieron contar los héroes al abrir (" + e.Message + ")."); }
        }
        private static bool _recuentosInformados;

        // Ids de los «próximamente» (aparte y sin inline por si el juego no tiene la API)
        // Ids of the "coming soon" heroes (separate and non-inlined in case the game doesn't have the API)
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string LeerIdsProximamente()
        {
            var l = HeroData.ComingSoonHeroes;
            if (l == null) return "0";
            var ids = new List<int>();
            for (int i = 0; i < l.Length; i++) ids.Add(l[i].HeroID);
            return $"{ids.Count} [{string.Join(", ", ids)}]";
        }

        // Héroes «próximamente» que el juego añade al final de la pantalla (HeroData.ComingSoonHeroes, la misma lista que
        // usa PlayPanel.OpenPanel). -1 si el juego no la tiene (versión anterior): así no se reserva la tarjeta misteriosa
        // "Coming soon" heroes that the game adds at the end of the screen (HeroData.ComingSoonHeroes, the same list that
        // PlayPanel.OpenPanel uses). -1 if the game doesn't have it (older version): so the mystery card is not reserved
        internal static int ContarProximamente()
        {
            try { return LeerProximamente(); }
            catch (Exception e)
            {
                if (!_avisoProximamente) { _avisoProximamente = true; Plugin.Aviso("Pantalla de héroes: sin héroes «próximamente» en este juego (" + e.GetType().Name + "); se amplía sin reservar la tarjeta misteriosa."); }
                return -1;
            }
        }
        private static bool _avisoProximamente;

        // Aparte y sin inline: si el miembro no existe en el juego, el fallo salta aquí y lo recoge ContarProximamente
        // Separate and non-inlined: if the member doesn't exist in the game, the failure is raised here and ContarProximamente catches it
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static int LeerProximamente()
        {
            var lista = HeroData.ComingSoonHeroes;
            return lista == null ? 0 : lista.Length;
        }

        // Postfix de CommanderScreenView.LoadRoster: cada título de facción debe caber en el hueco que le deja su fila (ancho
        // de la fila menos el punto de color, el espacio entre ellos y el relleno). Antes de medir se vuelve al espacio entre
        // letras y al tamaño de letra originales del título (por si el texto cambió desde la última vez); después, primero
        // menos espacio entre letras (hasta 2) y, si no basta, letra más pequeña (hasta 10). Solo se toca el que no cabe y
        // se avisa del que siga sin caber (0.6.1: validaciones y medida real del hueco).
        // CommanderScreenView.LoadRoster postfix: each faction title must fit in the gap left by its row (row width
        // minus the colored dot, the spacing between them and the padding). Before measuring, it reverts to the original letter spacing
        // and font size of the title (in case the text changed since last time); then, first
        // less letter spacing (up to 2) and, if that's not enough, a smaller font (up to 10). Only the one that doesn't fit is touched
        // and a warning is issued for any that still doesn't fit (0.6.1: validations and real measurement of the gap).
        public static void TrasLoadRoster(CommanderScreenView __instance)
        {
            try
            {
                if (!Plugin.Operativo || __instance == null) return;
                var nombres = __instance.factionNames;
                if (nombres == null || nombres.Length == 0) return;
                int ajustados = 0;
                var sinCaber = new List<string>();
                for (int i = 0; i < nombres.Length; i++)
                {
                    var n = nombres[i];
                    if (n == null || !n.gameObject.activeInHierarchy || string.IsNullOrEmpty(n.text)) continue;
                    float ancho = HuecoTitulo(n);
                    if (ancho <= 0f) continue;
                    // Valores originales del título (la primera vez que se ve)
                    // Original title values (the first time it is seen)
                    int idT = n.GetInstanceID();
                    if (!_originales.TryGetValue(idT, out var orig)) { orig = (n.characterSpacing, n.fontSize); _originales[idT] = orig; }
                    n.characterSpacing = orig.Item1;
                    n.fontSize = orig.Item2;
                    if (n.GetPreferredValues(n.text).x <= ancho) continue;
                    while (n.characterSpacing > 2f && n.GetPreferredValues(n.text).x > ancho) n.characterSpacing = Math.Max(2f, n.characterSpacing - 1f);
                    while (n.fontSize > 10f && n.GetPreferredValues(n.text).x > ancho) n.fontSize = Math.Max(10f, n.fontSize - 0.5f);
                    if (n.GetPreferredValues(n.text).x > ancho) sinCaber.Add(n.text);
                    else ajustados++;
                }
                if ((ajustados > 0 || sinCaber.Count > 0) && _titulosInformados.Add(__instance.GetInstanceID()))
                    Plugin.Info($"Pantalla de héroes: {ajustados} títulos de facción ajustados para que quepan" +
                                (sinCaber.Count > 0 ? $"; siguen sin caber: {string.Join(", ", sinCaber)}" : "") + ".");
            }
            catch (Exception e) { Plugin.Aviso("TrasLoadRoster (títulos de facción): " + e.Message); }
        }
        private static readonly HashSet<int> _titulosInformados = new HashSet<int>();
        private static readonly Dictionary<int, (float, float)> _originales = new Dictionary<int, (float, float)>();

        // Ancho disponible para un título: su fila («Heading Faction», HorizontalLayoutGroup) menos el relleno, los demás
        // hijos activos (el punto de color) y el espacio entre hijos. Si la fila aún no tiene tamaño, el ancho de la celda
        // de la rejilla menos 17 px (punto de 9 + hueco de 8). 0 = no se puede medir.
        // Available width for a title: its row ("Heading Faction", HorizontalLayoutGroup) minus the padding, the other active
        // children (the colored dot) and the spacing between children. If the row has no size yet, the grid cell
        // width minus 17 px (9 dot + 8 gap). 0 = cannot be measured.
        private static float HuecoTitulo(TMP_Text n)
        {
            var fila = n.transform.parent == null ? null : n.transform.parent.GetComponent<RectTransform>();
            if (fila == null) return 0f;
            var h = fila.GetComponent<HorizontalLayoutGroup>();
            if (fila.rect.width > 1f && h != null)
            {
                float otros = h.padding.left + h.padding.right;
                int activos = 0;
                for (int i = 0; i < fila.childCount; i++)
                {
                    var hijo = fila.GetChild(i);
                    if (hijo == null || !hijo.gameObject.activeSelf) continue;
                    activos++;
                    if (hijo.Pointer == n.transform.Pointer) continue;
                    var rt = hijo.TryCast<RectTransform>();
                    if (rt != null) otros += Math.Max(LayoutUtility.GetPreferredWidth(rt), rt.rect.width);
                }
                otros += h.spacing * Math.Max(0, activos - 1);
                return fila.rect.width - otros;
            }
            var g = fila.parent?.parent?.GetComponent<GridLayoutGroup>();
            return g == null ? 0f : g.cellSize.x - 17f;
        }

        // Todo lo creado o cambiado, para poder deshacerlo si algo falla a medias
        // Everything created or changed, so it can be undone if something fails halfway
        private sealed class Cambios
        {
            public readonly List<GameObject> Creados = new List<GameObject>();
            public Il2CppReferenceArray<HeroRosterTile> Tiles;
            public Il2CppReferenceArray<TMP_Text> Nombres;
            public Il2CppReferenceArray<Image> Puntos;
            public RectTransform Rejilla;
            public Transform PadreRejilla;
            public int IndiceRejilla = -1;
            public Vector2 AnclaMin, AnclaMax, Pivote, Posicion, Tamano;
            public ContentSizeFitter Ajuste;
        }

        // Clona tarjetas, amplía los campos y monta el desplazamiento; si falla, deshace
        // jugables = héroes de HeroData.Heroes; proximamente = HeroData.ComingSoonHeroes; reserva = casillas de la facción
        // misteriosa al final (2) o 0 en un juego sin ella
        // Clones cards, expands the fields and sets up scrolling; if it fails, rolls back
        // playable = heroes from HeroData.Heroes; proximamente = HeroData.ComingSoonHeroes; reserve = mystery faction tiles
        // at the end (2) or 0 in a game without it
        private static bool Ampliar(CommanderScreenView v, int jugables, int proximamente, int reserva)
        {
            var c = new Cambios { Tiles = v.tiles, Nombres = v.factionNames, Puntos = v.factionPips };
            try
            {
                int originales = c.Nombres.Length;
                bool misterio = reserva > 0;
                int nuevas = CasillasHeroes.TarjetasNuevas(jugables, proximamente, reserva, originales);
                int tarjetas = originales + nuevas;
                int u = CasillasHeroes.Plantilla(originales, misterio);
                if (nuevas <= 0 || u < 0 || c.Tiles.Length != originales * 2 || c.Puntos.Length != originales)
                    throw new InvalidOperationException($"estructura inesperada: {c.Tiles.Length} casillas, {originales} títulos, {c.Puntos.Length} puntos, {jugables} héroes jugables y {proximamente} próximamente");

                // Tarjeta modelo: la última normal: título -> «Heading Faction» -> tarjeta -> rejilla «Factions»
                // Template card: the last normal one: title -> "Heading Faction" -> card -> "Factions" grid
                Transform titulo = c.Nombres[u].transform;
                Transform tarjeta = titulo.parent != null ? titulo.parent.parent : null;
                Transform rejilla = tarjeta != null ? tarjeta.parent : null;
                if (tarjeta == null || rejilla == null) throw new InvalidOperationException("la tarjeta de facción no tiene la jerarquía esperada");
                // La misteriosa (última) debe estar en la misma rejilla: los clones van justo antes que ella
                // The mystery one (last) must be in the same grid: clones go right before it
                Transform tarjetaMisterio = null;
                if (misterio)
                {
                    var tm = c.Nombres[originales - 1].transform;
                    tarjetaMisterio = tm.parent != null ? tm.parent.parent : null;
                    if (tarjetaMisterio == null || tarjetaMisterio.parent == null || tarjetaMisterio.parent.Pointer != rejilla.Pointer)
                        throw new InvalidOperationException("la tarjeta misteriosa no está en la rejilla de facciones");
                }
                var rutaT1 = Camino(tarjeta, c.Tiles[u * 2].transform);
                var rutaT2 = Camino(tarjeta, c.Tiles[u * 2 + 1].transform);
                var rutaN = Camino(tarjeta, titulo);
                var rutaP = Camino(tarjeta, c.Puntos[u].transform);
                if (rutaT1 == null || rutaT2 == null || rutaN == null || rutaP == null)
                    throw new InvalidOperationException("las casillas, el título o el punto no cuelgan de la tarjeta de facción");

                var rejillaRT = rejilla.GetComponent<RectTransform>();
                // Altura visible: la de las tarjetas de los héroes de serie (de 2 en 2) a tamaño completo, lo que cabe en
                // el panel; las demás se ven bajando
                // Visible height: that of the stock heroes' cards (2 at a time) at full size, what fits in
                // the panel; the rest are seen by scrolling down
                int deSerie = Math.Max(1, (jugables - FaccionesNuevas.Heroes.Count + 1) / 2);
                float alturaAntes = AlturaRejilla(rejillaRT, Math.Min(deSerie, originales));

                // 1) Clones de la tarjeta modelo, colocados antes de la misteriosa (o al final si no hay)
                // 1) Clones of the template card, placed before the mystery one (or at the end if there is none)
                var clones = new List<(HeroRosterTile t1, HeroRosterTile t2, TMP_Text n, Image p)>();
                for (int k = 0; k < nuevas; k++)
                {
                    var clon = UnityEngine.Object.Instantiate(tarjeta.gameObject, rejilla, false);
                    c.Creados.Add(clon);
                    clon.name = PrefijoTarjeta + (originales + k + 1);
                    Transform t = clon.transform;
                    if (tarjetaMisterio != null) t.SetSiblingIndex(tarjetaMisterio.GetSiblingIndex());
                    var t1 = Seguir(t, rutaT1)?.GetComponent<HeroRosterTile>();
                    var t2 = Seguir(t, rutaT2)?.GetComponent<HeroRosterTile>();
                    var n = Seguir(t, rutaN)?.GetComponent<TMP_Text>();
                    var p = Seguir(t, rutaP)?.GetComponent<Image>();
                    if (t1 == null || t2 == null || n == null || p == null) throw new InvalidOperationException("el clon de la tarjeta no tiene sus casillas, título o punto");
                    // El clon copia el estado privado del original: que se suscriba a su cambio de héroe al cargarse
                    // The clone copies the original's private state: so that it subscribes to its hero change on load
                    t1.listening = false; t2.listening = false;
                    t1.gameObject.name = "Hero Zero " + (k * 2 + 1);
                    t2.gameObject.name = "Hero Zero " + (k * 2 + 2);
                    clones.Add((t1, t2, n, p));
                }

                // Arrays nuevos en el orden de la rejilla: originales normales, clones y la misteriosa al final
                // New arrays in grid order: normal originals, clones and the mystery one at the end
                var orden = CasillasHeroes.OrdenTarjetas(originales, nuevas, misterio);
                var tilesNuevas = new Il2CppReferenceArray<HeroRosterTile>(tarjetas * 2);
                var nombresNuevos = new Il2CppReferenceArray<TMP_Text>(tarjetas);
                var puntosNuevos = new Il2CppReferenceArray<Image>(tarjetas);
                for (int j = 0; j < orden.Length; j++)
                {
                    int o = orden[j];
                    if (o >= 0)
                    {
                        tilesNuevas[j * 2] = c.Tiles[o * 2];
                        tilesNuevas[j * 2 + 1] = c.Tiles[o * 2 + 1];
                        nombresNuevos[j] = c.Nombres[o];
                        puntosNuevos[j] = c.Puntos[o];
                    }
                    else
                    {
                        var cl = clones[-o - 1];
                        tilesNuevas[j * 2] = cl.t1;
                        tilesNuevas[j * 2 + 1] = cl.t2;
                        nombresNuevos[j] = cl.n;
                        puntosNuevos[j] = cl.p;
                    }
                }

                // 2) Campos privados de la vista
                // 2) Private fields of the view
                v.tiles = tilesNuevas;
                v.factionNames = nombresNuevos;
                v.factionPips = puntosNuevos;

                // 3) Desplazamiento vertical alrededor de la rejilla
                // 3) Vertical scrolling around the grid
                float alturaDespues = AlturaRejilla(rejillaRT, tarjetas);
                MontarDesplazamiento(c, rejillaRT, alturaAntes, alturaDespues);

                CasillasAnadidas = nuevas * 2;
                Estado = $"ampliada: {nuevas} tarjetas y {nuevas * 2} casillas nuevas ({tilesNuevas.Length} en total para {jugables} héroes jugables, " +
                         $"{proximamente} próximamente y {reserva} de la facción misteriosa), rejilla de {alturaAntes:0} a {alturaDespues:0} px con desplazamiento";
                Plugin.Info("Pantalla de héroes " + Estado + ".");
                return true;
            }
            catch (Exception e)
            {
                Deshacer(v, c);
                Estado = "de serie (la ampliación falló y se deshizo: " + e.Message + ")";
                Plugin.Error("Pantalla de héroes: " + Estado + ". Los héroes nuevos no se pueden elegir en esta pantalla, pero siguen en el juego.");
                return false;
            }
        }

        // Altura que ocupa la rejilla con N tarjetas (GridLayoutGroup de 2 columnas); si no hay rejilla, la actual
        // Height the grid takes up with N cards (GridLayoutGroup with 2 columns); if there is no grid, the current one
        private static float AlturaRejilla(RectTransform rejilla, int tarjetas)
        {
            var g = rejilla.GetComponent<GridLayoutGroup>();
            if (g == null) return rejilla.rect.height;
            int columnas = g.constraint == GridLayoutGroup.Constraint.FixedColumnCount && g.constraintCount > 0 ? g.constraintCount : 2;
            int filas = (tarjetas + columnas - 1) / columnas;
            return g.padding.top + g.padding.bottom + filas * g.cellSize.y + Math.Max(0, filas - 1) * g.spacing.y;
        }

        // Mete la rejilla en un contenedor con ScrollRect: mínimo la altura de antes, preferida la de todas las tarjetas
        // Puts the grid in a container with a ScrollRect: minimum the previous height, preferred that of all the cards
        private static void MontarDesplazamiento(Cambios c, RectTransform rejilla, float minimo, float preferido)
        {
            Transform panel = rejilla.parent;
            c.Rejilla = rejilla; c.PadreRejilla = panel; c.IndiceRejilla = rejilla.GetSiblingIndex();
            c.AnclaMin = rejilla.anchorMin; c.AnclaMax = rejilla.anchorMax; c.Pivote = rejilla.pivot;
            c.Posicion = rejilla.anchoredPosition; c.Tamano = rejilla.sizeDelta;

            var go = new GameObject(NombreDesplazamiento);
            c.Creados.Add(go);
            go.layer = rejilla.gameObject.layer;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.SetSiblingIndex(c.IndiceRejilla);
            // Ventana fija con la altura de la pantalla de serie: lo que no cabe se ve bajando con la rueda
            // Fixed window with the stock screen's height: what doesn't fit is seen by scrolling with the wheel
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = minimo;
            le.preferredHeight = minimo;
            le.flexibleHeight = 0f;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, minimo);
            // Fondo transparente que recoge la rueda del ratón también en los huecos entre tarjetas
            // Transparent background that captures the mouse wheel also in the gaps between cards
            var fondo = go.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0f);
            fondo.raycastTarget = true;
            go.AddComponent<RectMask2D>();
            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f;
            sr.viewport = rt;

            // Oculta las tarjetas que se salen de la ventana (algunos marcos de casilla no los recorta RectMask2D)
            // Hides the cards that fall outside the window (RectMask2D doesn't clip some tile frames)
            try
            {
                var recorte = go.AddComponent<RecorteTarjetas>();
                recorte.Ventana = rt;
                recorte.Rejilla = rejilla;
                recorte.Desplazamiento = sr;
                _recorte = recorte;
            }
            catch (Exception e) { Plugin.Aviso("Pantalla de héroes: sin ocultado de tarjetas fuera de la ventana (" + e.Message + ")."); }

            rejilla.SetParent(rt, false);
            rejilla.anchorMin = new Vector2(0f, 1f);
            rejilla.anchorMax = new Vector2(1f, 1f);
            rejilla.pivot = new Vector2(0.5f, 1f);
            rejilla.anchoredPosition = Vector2.zero;
            rejilla.sizeDelta = new Vector2(0f, preferido);
            c.Ajuste = rejilla.gameObject.AddComponent<ContentSizeFitter>();
            c.Ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            c.Ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sr.content = rejilla;
            var panelRT = panel.GetComponent<RectTransform>();
            if (panelRT != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);
        }

        // Deshace lo hecho (en orden inverso): rejilla a su sitio, campos originales y clones fuera
        // Undoes what was done (in reverse order): grid back in place, original fields restored and clones removed
        private static void Deshacer(CommanderScreenView v, Cambios c)
        {
            try
            {
                if (c.Ajuste != null) UnityEngine.Object.Destroy(c.Ajuste);
                if (c.Rejilla != null && c.PadreRejilla != null)
                {
                    c.Rejilla.SetParent(c.PadreRejilla, false);
                    if (c.IndiceRejilla >= 0) c.Rejilla.SetSiblingIndex(c.IndiceRejilla);
                    c.Rejilla.anchorMin = c.AnclaMin; c.Rejilla.anchorMax = c.AnclaMax; c.Rejilla.pivot = c.Pivote;
                    c.Rejilla.anchoredPosition = c.Posicion; c.Rejilla.sizeDelta = c.Tamano;
                }
                if (c.Tiles != null) v.tiles = c.Tiles;
                if (c.Nombres != null) v.factionNames = c.Nombres;
                if (c.Puntos != null) v.factionPips = c.Puntos;
                foreach (var go in c.Creados) if (go != null) UnityEngine.Object.Destroy(go);
            }
            catch (Exception e) { Plugin.Error("Pantalla de héroes: error al deshacer la ampliación: " + e.Message); }
        }

        /// <summary>Registra el componente de recorte en IL2CPP (al cargar el plugin, una vez).</summary>
        /// Registers the clipping component in IL2CPP (when the plugin loads, once).
        public static void RegistrarTipos()
        {
            if (!Il2CppInterop.Runtime.Injection.ClassInjector.IsTypeRegisteredInIl2Cpp<RecorteTarjetas>())
                Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<RecorteTarjetas>();
        }

        // Ruta de índices de hijo desde «raiz» hasta «t» (null si t no cuelga de raiz)
        // Path of child indices from "raiz" to "t" (null if t doesn't hang from raiz)
        private static List<int> Camino(Transform raiz, Transform t)
        {
            var ruta = new List<int>();
            while (t != null && t.Pointer != raiz.Pointer)
            {
                ruta.Insert(0, t.GetSiblingIndex());
                t = t.parent;
            }
            return t == null ? null : ruta;
        }

        // Sigue una ruta de índices de hijo desde «raiz» (null si no existe)
        // Follows a path of child indices from "raiz" (null if it doesn't exist)
        private static Transform Seguir(Transform raiz, List<int> ruta)
        {
            Transform t = raiz;
            foreach (int i in ruta)
            {
                if (t == null || i < 0 || i >= t.childCount) return null;
                t = t.GetChild(i);
            }
            return t;
        }
    }

    // -----------------------------------------------------------------------
    // Sección: componente que oculta las tarjetas fuera de la ventana de desplazamiento
    // Section: component that hides the cards outside the scrolling window
    // -----------------------------------------------------------------------
    /// <summary>
    /// En cada fotograma, las tarjetas de facción que no caben enteras en la ventana se ocultan (CanvasGroup a 0, sin
    /// clics). Hace falta porque algunos marcos de casilla del juego no los recorta RectMask2D y asomaban por encima del
    /// título al bajar.
    /// Every frame, the faction cards that don't fit entirely in the window are hidden (CanvasGroup at 0, no
    /// clicks). Needed because some of the game's tile frames aren't clipped by RectMask2D and poked out above the
    /// title when scrolling down.
    /// </summary>
    public class RecorteTarjetas : MonoBehaviour
    {
        public RecorteTarjetas(IntPtr ptr) : base(ptr) { }

        public RectTransform Ventana;
        public RectTransform Rejilla;
        public ScrollRect Desplazamiento;
        // Posición a la que llevar la ventana en cuanto el contenido tenga su altura (1 = arriba, 0 = abajo; -1 = nada)
        // Position to move the window to once the content has its height (1 = top, 0 = bottom; -1 = nothing)
        public float Destino = -1f;
        private readonly Il2CppStructArray<Vector3> _v = new Il2CppStructArray<Vector3>(4);
        private readonly Il2CppStructArray<Vector3> _c = new Il2CppStructArray<Vector3>(4);
        private bool _fallo;

        private void LateUpdate()
        {
            if (_fallo || Ventana == null || Rejilla == null) return;
            try
            {
                // Bajar o subir al abrir (cuando el contenido ya mide más que la ventana)
                // Scroll down or up on open (when the content is already taller than the window)
                if (Destino >= 0f && Desplazamiento != null && Rejilla.rect.height > Ventana.rect.height + 1f)
                {
                    Desplazamiento.verticalNormalizedPosition = Destino;
                    Destino = -1f;
                }
                Ventana.GetWorldCorners(_v);
                float arriba = _v[1].y, abajo = _v[0].y;
                for (int i = 0; i < Rejilla.childCount; i++)
                {
                    var hijo = Rejilla.GetChild(i).TryCast<RectTransform>();
                    if (hijo == null || !hijo.gameObject.activeSelf) continue;
                    hijo.GetWorldCorners(_c);
                    float tolerancia = (_c[1].y - _c[0].y) * 0.05f;
                    bool dentro = _c[1].y <= arriba + tolerancia && _c[0].y >= abajo - tolerancia;
                    var grupo = hijo.GetComponent<CanvasGroup>();
                    if (grupo == null) grupo = hijo.gameObject.AddComponent<CanvasGroup>();
                    float alfa = dentro ? 1f : 0f;
                    if (grupo.alpha != alfa) grupo.alpha = alfa;
                    grupo.blocksRaycasts = dentro;
                    grupo.interactable = dentro;
                }
            }
            catch (Exception e)
            {
                _fallo = true;
                Plugin.Aviso("Pantalla de héroes: el ocultado de tarjetas falló y se desactiva (" + e.Message + ").");
            }
        }
    }
}
