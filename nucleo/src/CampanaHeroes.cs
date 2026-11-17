// CampanaHeroes.cs
// Los héroes nuevos (901, 902, 903, 904) en los sitios del juego que no son su pantalla
// (revisión del 17-10-2026, notas internas (no publicadas)):
//   1) MAPA DE CAMPAÑA (ALTA): PlayerToken.LoadHero enciende heroPrefabs[heroID - 1] (Map/Campaign/PlayerToken.cs:21),
//      un array con una figura por héroe de serie. Con 901-904 se sale del array dentro de MapSceneManager.CompleteLoad
//      (async void): la carga se corta antes de los objetos, del nodo, de la intro y de la capa elegida, y el mapa queda
//      bloqueado. Un prefix enciende la figura del héroe prestado del manifiesto (901->1, 902->2, 903->10, 904->12) o,
//      si no cabe, la de Edric. Un finalizer se traga cualquier otra excepción de LoadHero con un héroe nuevo (el juego
//      instalado es más nuevo que el código publicado: si el cuerpo cambió, la carga sigue aunque falte la figura).
//      Es el único sitio del código publicado que indexa un array por id de héroe (revisados también los «id - 1» y
//      «id + 1» de HeroBonusManager, SaveDataHandler y GameOverPanel: son búsquedas que caen en Edric, no arrays).
//      OJO (prueba de Zero del 17-10 con héroe 901): el prefix y el finalizer de LoadHero NO SALTAN NUNCA. IL2CPP inlineó
//      LoadHero dentro de MapSceneManager.CompleteLoad (GameAssembly.dll: CompleteLoad 0x18400B9B0 lleva el cuerpo de
//      LoadHero copiado en 0x18400BBF1-0x18400BC8C, con su comprobación de longitud y su SetActive, y no llama nunca a
//      LoadHero 0x184012CA0), así que el desvío de LoadHero no se ejecuta. Se dejan puestos (no hacen daño y valdrían si
//      una versión del juego deja de inlinearlo). EL ARREGLO QUE FUNCIONA es el prefix de CompleteLoad (AntesDeCompleteLoad,
//      que sí se llama de verdad desde MapGenerator.<LoadMap>d__35.MoveNext, call en 0x184003E61): antes de que corra el
//      código de LoadHero, AsegurarFiguraEnMapa amplía heroPrefabs hasta el id del héroe y pone en cada hueco nuevo la
//      figura de su héroe prestado (o la de Edric), así heroPrefabs[heroID - 1] existe y no es nulo.
//   2) RETRATO Y FIGURA SIN ENTRADA (MEDIA): TabletopTavernData.LoadHeroSpriteAsync (TabletopTavernData.cs:360-376) no
//      comprueba nulos. Con un héroe nuevo cuyo módulo está apagado (historial de partidas de la Colección) daba
//      NullReferenceException. Un prefix cambia el id pedido por el del héroe prestado (o Edric) si el pedido no tiene
//      entrada de assets. Lo mismo en LoadHeroPrefabAsync (que devolvía null y dejaba la figura vacía).
//   3) CONTADOR DEL BOTÓN COLECCIÓN (MEDIA): MainMenu.RefreshCollectionButtonState (MainMenu.cs:617-657) solo recorre las
//      8 razas de serie. SumarUnidadesAlContador (llamado desde el postfix de InterfazColeccion, que ya envuelve ese método
//      para los objetos) suma las unidades de 40/41 y enciende el punto de «nuevo» si hay alguna recogida sin ver.
// Los parches 1 y 2 van en una instancia de Harmony propia (Plugin.cs) que se aplica con cualquier módulo instalado,
// aunque las facciones estén apagadas o la fábrica no enganche: es justo cuando un save puede traer un 9xx sin su módulo.
// Todo falla en blando: si algo no está como se espera, se escribe una vez en el log y sigue el juego.
// Autor: ZeroitMods.
// CampanaHeroes.cs
// The new heroes (901, 902, 903, 904) in the parts of the game that are not their screen
// (review of 17-10-2026, internal notes (unpublished)):
//   1) CAMPAIGN MAP (HIGH): PlayerToken.LoadHero enables heroPrefabs[heroID - 1] (Map/Campaign/PlayerToken.cs:21),
//      an array with one figure per base-game hero. With 901-904 it goes out of the array inside MapSceneManager.CompleteLoad
//      (async void): the load is cut off before the objects, the node, the intro and the chosen layer, and the map ends up
//      blocked. A prefix enables the figure of the borrowed hero from the manifest (901->1, 902->2, 903->10, 904->12) or,
//      if that doesn't fit, Edric's. A finalizer swallows any other exception from LoadHero with a new hero (the installed
//      game is newer than the published code: if the body changed, the load continues even if the figure is missing).
//      It is the only place in the published code that indexes an array by hero id (the «id - 1» and
//      «id + 1» in HeroBonusManager, SaveDataHandler and GameOverPanel were also reviewed: they are lookups that fall back to Edric, not arrays).
//      NOTE (Zero's test of 17-10 with hero 901): the LoadHero prefix and finalizer NEVER FIRE. IL2CPP inlined
//      LoadHero inside MapSceneManager.CompleteLoad (GameAssembly.dll: CompleteLoad 0x18400B9B0 carries a copy of the LoadHero body
//      at 0x18400BBF1-0x18400BC8C, with its length check and its SetActive, and never calls
//      LoadHero 0x184012CA0), so the LoadHero detour is never executed. They are left in place (they do no harm and would work if
//      a game version stops inlining it). THE FIX THAT WORKS is the CompleteLoad prefix (AntesDeCompleteLoad,
//      which is actually called from MapGenerator.<LoadMap>d__35.MoveNext, call at 0x184003E61): before the
//      LoadHero code runs, AsegurarFiguraEnMapa extends heroPrefabs up to the hero id and puts in each new slot the
//      figure of its borrowed hero (or Edric's), so heroPrefabs[heroID - 1] exists and is not null.
//   2) PORTRAIT AND FIGURE WITHOUT ENTRY (MEDIUM): TabletopTavernData.LoadHeroSpriteAsync (TabletopTavernData.cs:360-376) does not
//      check for nulls. With a new hero whose module is turned off (run history of the Collection) it threw
//      NullReferenceException. A prefix replaces the requested id with that of the borrowed hero (or Edric) if the requested one has no
//      assets entry. The same in LoadHeroPrefabAsync (which returned null and left the figure empty).
//   3) COLLECTION BUTTON COUNTER (MEDIUM): MainMenu.RefreshCollectionButtonState (MainMenu.cs:617-657) only iterates over the
//      8 base-game races. SumarUnidadesAlContador (called from the InterfazColeccion postfix, which already wraps that method
//      for the gear) adds up the units of 40/41 and lights the «new» dot if there is any collected one not yet seen.
// Patches 1 and 2 go in their own Harmony instance (Plugin.cs) that is applied with any module installed,
// even if the factions are turned off or the factory does not hook: that is exactly when a save may contain a 9xx without its module.
// Everything fails softly: if something is not as expected, it is written once to the log and the game continues.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using TJ;
using TJ.Map;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: parches de los héroes nuevos en campaña, retratos y contador
    // Section: patches for the new heroes in the campaign, portraits and counter
    // -----------------------------------------------------------------------
    public static class CampanaHeroes
    {
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        // Aviso una sola vez por clave y sesión (estos parches pueden correr a menudo)
        // Warn only once per key and session (these patches may run often)
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // Información una sola vez por clave y sesión
        // Information only once per key and session
        private static void InfoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Info(texto);
        }

        /// <summary>
        /// Lista de parches para Plugin.cs: (nombre, tipo, método, argumentos, prefix, postfix, finalizer). Se aplican en la
        /// instancia de Harmony propia de este fichero.
        /// List of patches for Plugin.cs: (name, type, method, arguments, prefix, postfix, finalizer). They are applied on this
        /// file's own Harmony instance.
        /// </summary>
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer)> Parches()
        {
            Func<Type> tData = () => typeof(TabletopTavernData);
            return new List<(string, Func<Type>, string, Func<Type[]>, string, string, string)>
            {
                // nameof comprueba al compilar que los métodos existen en el interop del juego instalado
                // El arreglo operativo del mapa: LoadHero está inlineado en CompleteLoad (ver la cabecera)
                // nameof checks at compile time that the methods exist in the installed game's interop
                // The operational map fix: LoadHero is inlined into CompleteLoad (see the header)
                ("Héroes nuevos: figura del mapa de campaña (MapSceneManager.CompleteLoad, LoadHero inlineado)", () => typeof(MapSceneManager), nameof(MapSceneManager.CompleteLoad), () => Type.EmptyTypes,
                    nameof(AntesDeCompleteLoad), null, null),
                // Inerte con el juego actual (LoadHero inlineado); se deja por si una versión deja de inlinearlo
                // Inert with the current game (LoadHero inlined); left in case a version stops inlining it
                ("Héroes nuevos: figura del mapa de campaña (PlayerToken.LoadHero)", () => typeof(PlayerToken), nameof(PlayerToken.LoadHero), () => Type.EmptyTypes,
                    nameof(AntesDeLoadHero), null, nameof(FinLoadHero)),
                ("Héroes nuevos: retrato sin entrada de assets (LoadHeroSpriteAsync)", tData, nameof(TabletopTavernData.LoadHeroSpriteAsync), () => new[] { typeof(int) },
                    nameof(AntesDeCargarAssetHeroe), null, null),
                ("Héroes nuevos: figura sin entrada de assets (LoadHeroPrefabAsync)", tData, nameof(TabletopTavernData.LoadHeroPrefabAsync), () => new[] { typeof(int), typeof(bool) },
                    nameof(AntesDeCargarAssetHeroe), null, null),
            };
        }

        // ---------------------------------------------------------------
        // Héroe prestado de un héroe nuevo
        // Borrowed hero of a new hero
        // ---------------------------------------------------------------
        /// <summary>
        /// Héroe de serie que presta figura y retrato a un héroe nuevo: el del manifiesto de su módulo si está activo; si no,
        /// el de la tabla de módulos instalados (Saves.TablaHeroes, también los apagados). -1 si no es un héroe nuevo conocido.
        /// Stock hero that lends its figure and portrait to a new hero: the one from its module's manifest if active; otherwise,
        /// the one from the table of installed modules (Saves.TablaHeroes, including disabled ones). -1 if it is not a known new hero.
        /// </summary>
        public static int Prestado(int heroID)
        {
            try
            {
                if (FaccionesNuevas.Heroes.TryGetValue(heroID, out var h) && h.PrestadoDe > 0) return h.PrestadoDe;
                var tabla = Saves.TablaHeroes;
                if (tabla != null && tabla.EsNuevo(heroID)) return tabla.PrestadoDe(heroID);
            }
            catch { }
            return -1;
        }

        // ¿Es un héroe nuevo (conocido por los módulos o, aunque no se conozca, del rango 9xx de los módulos de Zero)?
        // Is it a new hero (known by the modules or, even if unknown, in the 9xx range of Zero's modules)?
        private static bool EsHeroeNuevo(int heroID) => Prestado(heroID) > 0 || (heroID >= 900 && heroID <= 999);

        // ---------------------------------------------------------------
        // 1) Mapa de campaña: figura del héroe en PlayerToken
        // 1) Campaign map: hero figure in PlayerToken
        // ---------------------------------------------------------------
        // Héroe activo como lo lee PlayerToken.LoadHero (-1 si no hay gestor)
        // Active hero as read by PlayerToken.LoadHero (-1 if there is no manager)
        private static int HeroeActivo()
        {
            var g = HeroBonusManager.InstanceIfExists;
            return g == null ? -1 : g.ActiveHeroID;
        }

        /// <summary>
        /// Prefix de PlayerToken.LoadHero: si el héroe activo cabe en heroPrefabs, sigue el juego (true). Si no cabe (héroe
        /// nuevo), enciende la figura de su héroe prestado (o la de Edric) y se salta el original (false).
        /// Prefix of PlayerToken.LoadHero: if the active hero fits in heroPrefabs, the game proceeds (true). If it does not fit (new
        /// hero), it enables the figure of its borrowed hero (or Edric's) and skips the original (false).
        /// </summary>
        public static bool AntesDeLoadHero(PlayerToken __instance)
        {
            bool fuera = false;
            try
            {
                if (__instance == null) return true;
                var figuras = __instance.heroPrefabs;
                if (figuras == null || figuras.Length == 0) return true;
                int heroe = HeroeActivo();
                fuera = LogicaCampanaHeroes.FueraDeRango(heroe, figuras.Length);
                if (!fuera) return true;                                    // héroe de serie: el juego, tal cual
                                                                            // stock hero: the game, as is
                int indice = LogicaCampanaHeroes.IndiceFigura(heroe, figuras.Length, Prestado, HeroData.DefaultHeroID);
                GameObject figura = indice >= 0 ? figuras[indice] : null;
                if (figura != null) figura.SetActive(true);
                InfoUnaVez("figura" + heroe, $"Mapa de campaña: el héroe {heroe} no tiene figura propia ({figuras.Length} figuras de serie); " +
                           (figura != null ? $"se usa la del héroe {indice + 1}." : "no hay figura de respaldo, el mapa sigue sin ficha."));
                return false;
            }
            catch (Exception e)
            {
                AvisoUnaVez("loadhero", "Mapa de campaña: AntesDeLoadHero: " + e.Message);
                // Si ya se sabe que el original se saldría del array, mejor sin figura que con la carga del mapa cortada
                // If we already know the original would overflow the array, better no figure than a cut-off map load
                return !fuera;
            }
        }

        /// <summary>
        /// Finalizer de PlayerToken.LoadHero: con un héroe nuevo activo, cualquier excepción del original se escribe en el log
        /// y se descarta, para que MapSceneManager.CompleteLoad siga (objetos, nodo, intro y capa). Con un héroe de serie,
        /// la excepción sigue su curso como en el juego de serie.
        /// Finalizer of PlayerToken.LoadHero: with a new hero active, any exception from the original is written to the log
        /// and discarded, so that MapSceneManager.CompleteLoad continues (gear, node, intro and layer). With a stock hero,
        /// the exception takes its course as in the stock game.
        /// </summary>
        public static Exception FinLoadHero(Exception __exception)
        {
            if (__exception == null) return null;
            try
            {
                int heroe = HeroeActivo();
                if (!EsHeroeNuevo(heroe)) return __exception;
                AvisoUnaVez("finloadhero", $"Mapa de campaña: PlayerToken.LoadHero falló con el héroe nuevo {heroe} ({__exception.GetType().Name}: {__exception.Message}); se sigue sin figura para no cortar la carga del mapa.");
                return null;
            }
            catch { return __exception; }
        }

        // ---------------------------------------------------------------
        // 1b) Mapa de campaña: arreglo que no depende de desviar LoadHero
        // 1b) Campaign map: fix that does not depend on diverting LoadHero
        // ---------------------------------------------------------------
        /// <summary>
        /// Prefix de MapSceneManager.CompleteLoad (siempre sigue el original): antes del código inlineado de LoadHero, deja
        /// heroPrefabs con sitio y figura para el héroe activo. Nunca lanza.
        /// Prefix of MapSceneManager.CompleteLoad (always lets the original run): before the inlined LoadHero code, it leaves
        /// heroPrefabs with room and a figure for the active hero. Never throws.
        /// </summary>
        public static void AntesDeCompleteLoad(MapSceneManager __instance)
        {
            try
            {
                if (__instance == null) return;
                AsegurarFiguraEnMapa(__instance.playerToken, HeroeActivoParaMapa());
            }
            catch (Exception e) { AvisoUnaVez("completeload", "Mapa de campaña: AntesDeCompleteLoad: " + e.Message); }
        }

        // Héroe que leerá el código de LoadHero: el del gestor de bonos; si aún no existe, el guardado (mismo valor que
        // copia HeroBonusManager al iniciarse). -1 si no se sabe.
        // Hero that the LoadHero code will read: the one from the bonus manager; if it does not exist yet, the saved one (same value that
        // HeroBonusManager copies on startup). -1 if unknown.
        private static int HeroeActivoParaMapa()
        {
            int heroe = -1;
            try { heroe = HeroeActivo(); } catch { }
            if (heroe != -1) return heroe;
            try { return Memori.SaveData.SaveDataHandler.GetActiveHeroID(); } catch { return -1; }
        }

        /// <summary>
        /// Deja <c>token.heroPrefabs[heroe - 1]</c> listo para el código de LoadHero (inlineado en CompleteLoad): si el héroe
        /// no cabe, amplía el array hasta su id y rellena cada hueco nuevo con la figura del héroe prestado de ese id (o la de
        /// Edric, o la primera que haya); si cabe pero su hueco está vacío, lo rellena igual. Con un héroe de serie que cabe
        /// no toca nada. <paramref name="figuraPropia"/> (opcional) es la figura que se pone en el hueco del héroe en vez de la
        /// prestada (para cuando haya figura propia del héroe en el mapa). Se puede llamar las veces que haga falta. Devuelve
        /// true si el hueco del héroe queda con figura. Nunca lanza.
        /// Leaves <c>token.heroPrefabs[heroe - 1]</c> ready for the LoadHero code (inlined in CompleteLoad): if the hero
        /// does not fit, it extends the array up to its id and fills each new gap with the figure of the borrowed hero of that id (or
        /// Edric's, or the first one available); if it fits but its slot is empty, it fills it the same way. With a stock hero that fits
        /// it does nothing. <paramref name="figuraPropia"/> (optional) is the figure placed in the hero's slot instead of the
        /// borrowed one (for when the hero has its own figure on the map). It can be called as many times as needed. Returns
        /// true if the hero's slot ends up with a figure. Never throws.
        /// </summary>
        public static bool AsegurarFiguraEnMapa(PlayerToken token, int heroe, GameObject figuraPropia = null)
        {
            try
            {
                if (token == null) return false;
                var figuras = token.heroPrefabs;
                int longitud = figuras == null ? 0 : figuras.Length;
                int necesaria = LogicaCampanaHeroes.LongitudNecesaria(heroe, longitud);
                if (necesaria < 0)
                {
                    AvisoUnaVez("figuraId" + heroe, $"Mapa de campaña: el héroe activo {heroe} no tiene arreglo posible en las figuras del mapa (id fuera de 1-{LogicaCampanaHeroes.TopeHeroeMapa}).");
                    return false;
                }
                int hueco = (heroe == -1 ? 1 : heroe) - 1;
                if (necesaria == longitud && figuraPropia == null && figuras[hueco] != null) return true;   // ya está bien
                                                                                                            // it's fine

                // Figuras de origen: las del array tal como está ahora (antes de ampliarlo)
                // Source figures: those in the array as it is now (before extending it)
                bool Llena(int i) => figuras != null && i >= 0 && i < longitud && figuras[i] != null;
                GameObject Origen(int id)
                {
                    int o = LogicaCampanaHeroes.OrigenFigura(id, longitud, Llena, Prestado, HeroData.DefaultHeroID);
                    return o >= 0 ? figuras[o] : null;
                }

                var destino = figuras;
                if (necesaria > longitud)
                {
                    destino = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject>(necesaria);
                    for (int i = 0; i < longitud; i++) destino[i] = figuras[i];
                    // Cada hueco nuevo con figura (la del prestado de ese id): nunca queda un nulo que LoadHero pudiera encender
                    // Each new gap with a figure (that of the borrowed hero of that id): a null that LoadHero could enable never remains
                    for (int i = longitud; i < necesaria; i++) destino[i] = Origen(i + 1);
                }
                if (figuraPropia != null) destino[hueco] = figuraPropia;
                else if (destino[hueco] == null) destino[hueco] = Origen(hueco + 1);

                if (!ReferenceEquals(destino, figuras)) token.heroPrefabs = destino;
                bool bien = destino[hueco] != null;
                InfoUnaVez("figuraMapa" + heroe, $"Mapa de campaña: figuras del mapa {longitud} -> {necesaria}; el héroe {heroe} usa " +
                           (!bien ? "ninguna (no hay figuras de serie; el juego fallará como antes)." :
                            figuraPropia != null ? "su figura propia." : $"la figura «{destino[hueco].name}» (prestada)."));
                return bien;
            }
            catch (Exception e)
            {
                AvisoUnaVez("asegurarFigura", "Mapa de campaña: AsegurarFiguraEnMapa: " + e.Message);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // 2) Retrato y figura de un héroe sin entrada de assets
        // 2) Portrait and figure of a hero without an assets entry
        // ---------------------------------------------------------------
        /// <summary>
        /// Prefix de LoadHeroSpriteAsync(int) y LoadHeroPrefabAsync(int, bool): si el héroe pedido no tiene entrada en los
        /// assets de héroe (héroe nuevo con su módulo apagado), pide la de su héroe prestado o, si tampoco, la de Edric.
        /// Con entrada no cambia nada (y el prefix del retrato propio de HeroesNuevos sigue igual). Siempre sigue el original.
        /// Prefix of LoadHeroSpriteAsync(int) and LoadHeroPrefabAsync(int, bool): if the requested hero has no entry in the
        /// hero assets (new hero with its module disabled), it requests that of its borrowed hero or, failing that, Edric's.
        /// With an entry nothing changes (and the prefix of HeroesNuevos's own portrait stays the same). Always lets the original run.
        /// </summary>
        public static void AntesDeCargarAssetHeroe(TabletopTavernData __instance, ref int __0)
        {
            try
            {
                var assets = __instance == null ? null : __instance._heroAssetsData;
                if (assets == null || assets.Heroes == null || assets.Heroes.Count == 0) return;
                int pedido = __0;
                // Héroe nuevo activo: su retrato propio lo da HeroesNuevos y su entrada la pone su alta; no se toca nunca
                // Active new hero: its own portrait is provided by HeroesNuevos and its entry is set by its registration; it is never touched
                if (FaccionesNuevas.EsHeroeNuevo(pedido)) return;
                int usar = LogicaCampanaHeroes.HeroeConAssets(pedido, id => assets.GetByID(id) != null, Prestado, HeroData.DefaultHeroID);
                if (usar == pedido) return;
                __0 = usar;
                InfoUnaVez("asset" + pedido, $"Assets de héroe: el héroe {pedido} no tiene entrada (¿su módulo está apagado?); se usan el retrato y la figura del héroe {usar}.");
            }
            catch (Exception e) { AvisoUnaVez("asset", "Assets de héroe: AntesDeCargarAssetHeroe: " + e.Message); }
        }

        // ---------------------------------------------------------------
        // 3) Contador del botón Colección del menú principal
        // 3) Counter of the Collection button in the main menu
        // ---------------------------------------------------------------
        /// <summary>
        /// Suma al contador del botón Colección las unidades de las razas nuevas (recogidas/total) y enciende el punto de
        /// «nuevo» si alguna está recogida y sin ver. Se llama desde el postfix de MainMenu.RefreshCollectionButtonState de
        /// InterfazColeccion (justo después de que el juego escriba su contador). Solo enciende el punto, nunca lo apaga.
        /// Adds to the Collection button counter the units of the new races (collected/total) and turns on the "new" dot
        /// if any is collected and unseen. Called from the postfix of MainMenu.RefreshCollectionButtonState in
        /// InterfazColeccion (right after the game writes its counter). It only turns the dot on, never off.
        /// </summary>
        public static void SumarUnidadesAlContador(TJ.MainMenu.MainMenu menu)
        {
            try
            {
                if (menu == null || !FaccionesNuevas.Activas) return;
                var data = TabletopTavernData.InstanceIfExists;
                var etiqueta = menu.collectionTotalText;
                if (data == null || etiqueta == null) return;

                // Unidades que el juego ya cuenta en sus razas de serie: no se suman dos veces (p. ej. sueltas compartidas)
                // Units the game already counts in its stock races: they are not added twice (e.g. shared loose ones)
                var deSerie = new HashSet<int>();
                foreach (Race r in Enum.GetValues(typeof(Race)))
                {
                    try
                    {
                        var u = data.GetUnitsOfRace(r);
                        for (int i = 0; u != null && i < u.Length; i++) deSerie.Add((int)u[i]);
                    }
                    catch { }                                           // raza sin lista (p. ej. Special): se salta
                                                                        // race without a list (e.g. Special): skipped
                }

                var recogidas = Memori.SaveData.SaveDataHandler.GetTroopsIDsCollected();
                var vistas = Memori.SaveData.SaveDataHandler.GetTroopsIDsAcknowledged();
                int masHechas = 0, masTotal = 0;
                bool sinVer = false;
                var contadas = new HashSet<int>();
                foreach (Race r in FaccionesNuevas.RazasNuevas.OrderBy(x => (int)x))
                {
                    if (!FaccionesNuevas.EsNueva(r)) continue;
                    var unidades = data.GetUnitsOfRace(r);
                    for (int i = 0; unidades != null && i < unidades.Length; i++)
                    {
                        UnitName u = unidades[i];
                        if (deSerie.Contains((int)u) || !contadas.Add((int)u)) continue;
                        masTotal++;
                        if (recogidas != null && recogidas.Contains(u))
                        {
                            masHechas++;
                            if (vistas == null || !vistas.Contains(u)) sinVer = true;
                        }
                    }
                }
                if (masTotal == 0) return;
                if (!LogicaCampanaHeroes.SumarAContador(etiqueta.text, masHechas, masTotal, out int hechas, out int total))
                {
                    AvisoUnaVez("contadorForma", $"Contador de la Colección: el texto «{etiqueta.text}» no tiene la forma hechas/total; no se suman las unidades de las razas nuevas.");
                    return;
                }
                menu.SetCompletionCounter(etiqueta, hechas, total);     // mismo método del juego: texto y color de completado
                                                                        // same game method: completion text and color
                if (sinVer && menu.collectionUnacknowledgedIndicator != null) menu.collectionUnacknowledgedIndicator.SetActive(true);
                InfoUnaVez("contadorInfo", $"Contador de la Colección: +{masHechas}/{masTotal} de las razas nuevas (queda {hechas}/{total}).");
            }
            catch (Exception e) { AvisoUnaVez("contador", "Contador de la Colección (se queda el del juego): " + e.Message); }
        }
    }
}
