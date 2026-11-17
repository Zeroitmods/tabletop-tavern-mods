// AutoPruebaNuevas.cs
// Autoprueba de lo nuevo de la 0.7.2 que se puede comprobar sin abrir ninguna pantalla ni tocar partidas (parte de la
// clase AutoPrueba; resultado con la etiqueta [autoprueba], como el resto):
//  27) Parches de la interfaz y de los logros puestos: Colección (CollectionPanel.BuildData, MainMenu.RefreshCollection-
//      ButtonState), Batalla personalizada (CustomBattleUIManager.LoadUI), armería (WarbandGearArmory.Build) y logros
//      (PlayerAchievements.Unlock, antes SteamAchievements.Unlock; SaveDataHandler.EvaluateRaceCollection), según sus interruptores. Se mira la lista de
//      parches de Harmony de cada método; no se llama a ninguno (Unlock concedería un logro de verdad).
//  28) Lista de gear ampliada: fuera de contexto GearData.GetGearIDs da solo los de serie; dentro (lo que abren la
//      armería y la Colección) da además los objetos de los módulos activos; suspendida (lo que hace el logro de objetos)
//      vuelve a dar solo los de serie, y al reanudar todo queda como estaba.
//  29) Logros de colección: listas de las 8 razas de serie con sus unidades de serie (no vacías) y cuántas sueltas 9xx
//      llevan, que es lo que el filtro deja fuera.
//  30) Pantalla de héroes: los héroes nuevos están en la lista de jugables (HeroData._heroesSorted) y las casillas que
//      hacen falta para todos (CasillasHeroes) superan las 18 de serie, que es cuando la pantalla se amplía.
//  31) Mapa de campaña: PlayerToken.heroPrefabs existe en el interop como Il2CppReferenceArray<GameObject> escribible,
//      MapSceneManager.playerToken es un PlayerToken, el prefix de CompleteLoad está puesto (LoadHero está inlineado ahí)
//      y la lógica de ampliar da 901 figuras con la del prestado del héroe 901. No carga el mapa.
//  32) Juego 1.11.1 (01-11-2026): el hechizo emblema de cada héroe nuevo está en el registro de hechizos del juego
//      (TJ.Spells.SpellRegistry); si el núcleo tuvo que sustituirlo, se dice cuál pedía el manifiesto y, desde la 0.7.4,
//      sale además una línea de AVISO (no es un fallo de la carga, pero hay que mirarlo).
//  33) 0.7.4: hechizos propios (1901-1904) registrados en el juego, con su base, maná, valor, radio y equipo; dueño según
//      el juego (SpellLoadout.TryGetSignatureHero, tiene que ser su héroe nuevo), en la ranura 1 de su héroe y en el
//      grimorio (SpellLoadout.GetGrimoireSpells). 03-11-2026: TryGetSignatureHero se llama a mano (DuenoDelHechizo), no por
//      el envoltorio del interop, que desbordaba la pila y cerraba el juego al arrancar (notas internas (no publicadas)).
//  36) 0.7.6: módulo «Impertérritos» activo: parches de la moral del jugador puestos y cuentas de la sesión.
//  37) 0.7.7: estandarte: parches puestos y dónde se aplica el icono elegido (vista previa y banderas del jugador).
//  35) 03-11-2026: las cartas propias 40/41 llevan su raza y su banda está en los dos órdenes de bandas del juego
//      (grimorio de la preparación y menú de hechizos de la batalla). Va dentro del bloque del 33.
//  34) 0.7.4: parches nuevos puestos según su interruptor: Grand Grimoire (BattleAchievements.Evaluate y
//      SpellCastRecord.Remaining), bloqueo de multijugador (con módulos aplicados: QuickBattleQueue.Start, las salas de
//      VersusLobby, OnJoinRequested, VersusRejoin.Resume y StartLink; y si se puede crear la tarea terminada que corta las
//      salas) y munición de las híbridas (GetHeroStatBonus de 5 argumentos), con los contadores de la sesión. Desde la
//      ronda 2 de la revisión, los tipos nuevos de la 1.11.1 se buscan por nombre.
// Autor: ZeroitMods.
// AutoPruebaNuevas.cs
// Self-test of what is new in 0.7.2 that can be checked without opening any screen or touching runs (part of the
// AutoPrueba class; result with the [autoprueba] tag, like the rest):
//  27) UI and achievement patches applied: Collection (CollectionPanel.BuildData, MainMenu.RefreshCollection-
//      ButtonState), Custom Battle (CustomBattleUIManager.LoadUI), armory (WarbandGearArmory.Build) and achievements
//      (PlayerAchievements.Unlock, formerly SteamAchievements.Unlock; SaveDataHandler.EvaluateRaceCollection), according to their switches. The list of
//      Harmony patches of each method is inspected; none is called (Unlock would grant a real achievement).
//  28) Extended gear list: out of context GearData.GetGearIDs returns only the stock ones; inside (what the armory
//      and the Collection open) it also returns the items of the active modules; suspended (what the items achievement does)
//      it returns only the stock ones again, and on resume everything is as it was.
//  29) Collection achievements: lists of the 8 stock races with their stock units (non-empty) and how many loose 9xx
//      units they carry, which is what the filter leaves out.
//  30) Heroes screen: the new heroes are in the playable list (HeroData._heroesSorted) and the slots
//      needed for all of them (CasillasHeroes) exceed the stock 18, which is when the screen is expanded.
//  31) Campaign map: PlayerToken.heroPrefabs exists in the interop as a writable Il2CppReferenceArray<GameObject>,
//      MapSceneManager.playerToken is a PlayerToken, the CompleteLoad prefix is applied (LoadHero is inlined there)
//      and the expansion logic yields 901 figures with that of the borrowed hero of hero 901. It does not load the map.
//  32) Game 1.11.1 (01-11-2026): the signature spell of each new hero is in the game's spell registry
//      (TJ.Spells.SpellRegistry); if the core had to replace it, it says which one the manifest requested and, since 0.7.4,
//      a WARNING line is also emitted (it is not a load failure, but it needs a look).
//  33) 0.7.4: custom spells (1901-1904) registered in the game, with their base, mana, value, radius and gear; owner according to
//      the game (SpellLoadout.TryGetSignatureHero, must be its new hero), in slot 1 of its hero and in the
//      grimoire (SpellLoadout.GetGrimoireSpells). 03-11-2026: TryGetSignatureHero is called manually (DuenoDelHechizo), not through
//      the interop wrapper, which overflowed the stack and closed the game at startup (internal notes (unpublished)).
//  36) 0.7.6: module «Impertérritos» active: player morale patches applied and session counts.
//  37) 0.7.7: banner: patches applied and where the chosen icon is applied (preview and player flags).
//  35) 03-11-2026: custom cards 40/41 carry their race and their band is in both band orders of the game
//      (preparation grimoire and battle spell menu). It is inside the block of 33.
//  34) 0.7.4: new patches applied according to their switch: Grand Grimoire (BattleAchievements.Evaluate and
//      SpellCastRecord.Remaining), multiplayer lock (with modules applied: QuickBattleQueue.Start, the rooms of
//      VersusLobby, OnJoinRequested, VersusRejoin.Resume and StartLink; and whether the completed task that cuts the
//      rooms can be created) and hybrid ammo (5-argument GetHeroStatBonus), with the session counters. Since
//      round 2 of the review, the new types of 1.11.1 are looked up by name.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TJ;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: autoprueba de la 0.7.2 (interfaz, armería, logros, pantalla de héroes)
    // Section: 0.7.2 self-test (UI, armory, achievements, heroes screen)
    // -----------------------------------------------------------------------
    public static partial class AutoPrueba
    {
        // ¿Tiene el método algún parche de una instancia de Harmony de este plugin? (sin llamarlo)
        // Does the method have any patch from a Harmony instance of this plugin? (without calling it)
        private static bool TieneParche(Type tipo, string metodo, Type[] args = null)
        {
            var m = args == null ? AccessTools.Method(tipo, metodo) : AccessTools.Method(tipo, metodo, args);
            if (m == null) return false;
            var info = Harmony.GetPatchInfo(m);
            if (info == null) return false;
            return info.Owners.Any(o => o != null && o.StartsWith(Plugin.GUID, StringComparison.Ordinal));
        }

        // 03-11-2026 (crash al arrancar con la autoprueba; notas internas (no publicadas)): dueño de un hechizo de firma según el juego,
        // SpellLoadout.TryGetSignatureHero(Spell, out Hero), llamado a mano y NO por el envoltorio del interop. Hero es un struct
        // de il2cpp con referencias (no blittable) y el envoltorio generado pasa como «out Hero» la dirección de un IntPtr local
        // de 8 bytes; el juego escribe ahí el struct entero (decenas de bytes), desborda la pila y machaca los registros que
        // guarda el llamador: en el paso 33 el HashSet «grimorio» acababa apuntando a basura y el juego se cerraba con una
        // AccessViolation en HashSet<int>.Contains (no se puede capturar). Aquí el juego escribe dentro de una caja Hero de su
        // tamaño real (il2cpp_object_new, a ceros), como hace il2cpp con un out de struct que va a un campo.
        // Regla: ningún método del juego con ref/out de un struct no blittable se llama por su envoltorio (prueba 28 del
        // proyecto de pruebas, PruebasInterop.cs, lo vigila en la DLL compilada).
        // 03-11-2026 (crash on startup with the self-test; internal notes (unpublished)): owner of a signature spell according to the game,
        // SpellLoadout.TryGetSignatureHero(Spell, out Hero), called by hand and NOT through the interop wrapper. Hero is an il2cpp struct
        // with references (not blittable) and the generated wrapper passes as «out Hero» the address of a local 8-byte IntPtr;
        // the game writes the whole struct there (dozens of bytes), overflows the stack and clobbers the registers that
        // the caller saves: at step 33 the «grimorio» HashSet ended up pointing to garbage and the game closed with an
        // AccessViolation in HashSet<int>.Contains (it cannot be caught). Here the game writes into a Hero box of its
        // real size (il2cpp_object_new, zeroed), as il2cpp does with a struct out that goes to a field.
        // Rule: no game method with ref/out of a non-blittable struct is called through its wrapper (test 28 of the
        // test project, PruebasInterop.cs, watches this in the compiled DLL).
        private static unsafe bool DuenoDelHechizo(TJ.Spells.Spell hechizo, out int heroe)
        {
            heroe = -1;
            // Puntero del método que ya resolvió el interop para ESTA firma (Spell, out Hero): buscarlo por nombre podría dar
            // otra sobrecarga con argumentos de otro tamaño si el juego la añade (revisión externa del 03-11, hallazgo 1)
            // Pointer of the method that the interop already resolved for THIS signature (Spell, out Hero): looking it up by name could return
            // another overload with arguments of a different size if the game adds it (external review of 03-11, finding 1)
            var campo = typeof(TJ.Spells.SpellLoadout).GetField("NativeMethodInfoPtr_TryGetSignatureHero_Public_Static_Boolean_Spell_byref_Hero_0",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            IntPtr m = campo == null ? IntPtr.Zero : (IntPtr)campo.GetValue(null);
            if (m == IntPtr.Zero) throw new MissingMethodException("TJ.Spells.SpellLoadout", "TryGetSignatureHero(Spell, out Hero)");
            // Caja del struct con su tamaño real: el «out Hero» del juego escribe en sus datos (desempaquetados)
            // Box of the struct with its real size: the game's «out Hero» writes into its data (unboxed)
            var caja = new Hero();
            var valor = hechizo;   // el enum con su tipo exacto (sin suponer que es de int)
                                   // the enum with its exact type (without assuming it is an int)
            void** args = stackalloc void*[2];
            args[0] = &valor;
            args[1] = (void*)IL2CPP.il2cpp_object_unbox(caja.Pointer);
            IntPtr excepcion = IntPtr.Zero;
            IntPtr res = IL2CPP.il2cpp_runtime_invoke(m, IntPtr.Zero, args, ref excepcion);
            // Si el juego lanza, se relanza aquí con su mensaje (lo recoge el try/catch del paso 33)
            // If the game throws, it is rethrown here with its message (caught by the try/catch in step 33)
            Il2CppException.RaiseExceptionIfNecessary(excepcion);
            // El bool de vuelta llega en caja; el héroe se lee de la caja ya rellena (HeroID por su campo)
            // The returned bool arrives boxed; the hero is read from the already filled box (HeroID by its field)
            bool encontrado = res != IntPtr.Zero && *(bool*)IL2CPP.il2cpp_object_unbox(res);
            if (encontrado) heroe = caja.HeroID;
            GC.KeepAlive(caja);
            return encontrado;
        }

        private static void PruebasVersion072(TabletopTavernData data, ref int ok, ref int total)
        {
            // --- 27) Parches de la interfaz, de la armería y de los logros ---
            // --- 27) Patches for the interface, the armory and the achievements ---
            try
            {
                var faltan = new List<string>();
                void Pedir(bool activo, string nombre, Type t, string metodo, Type[] args = null)
                {
                    if (activo && !TieneParche(t, metodo, args)) faltan.Add(nombre);
                }
                bool contenido = Plugin.Operativo;
                Pedir(contenido && Plugin.CfgInterfazColeccion.Value, "CollectionPanel.BuildData", typeof(TJ.MainMenu.CollectionPanel), nameof(TJ.MainMenu.CollectionPanel.BuildData), Type.EmptyTypes);
                Pedir(contenido && Plugin.CfgInterfazColeccion.Value, "MainMenu.RefreshCollectionButtonState", typeof(TJ.MainMenu.MainMenu), nameof(TJ.MainMenu.MainMenu.RefreshCollectionButtonState), Type.EmptyTypes);
                Pedir(contenido && Plugin.CfgInterfazBatalla.Value, "CustomBattleUIManager.LoadUI", typeof(CustomBattleUIManager), nameof(CustomBattleUIManager.LoadUI), new[] { typeof(int) });
                Pedir(Objetos.Operativo, "WarbandGearArmory.Build", typeof(TJ.MainMenu.WarbandGearArmory), nameof(TJ.MainMenu.WarbandGearArmory.Build), Type.EmptyTypes);
                Pedir(Objetos.Operativo, "GearData.GetGearIDs", typeof(GearData), nameof(GearData.GetGearIDs), Type.EmptyTypes);
                // Juego 1.11.1: SteamAchievements.Unlock pasó a TJ.Achievements.PlayerAchievements.Unlock
                // Game 1.11.1: SteamAchievements.Unlock became TJ.Achievements.PlayerAchievements.Unlock
                Pedir(Plugin.CfgLogrosFiltrar.Value, "PlayerAchievements.Unlock", typeof(TJ.Achievements.PlayerAchievements), nameof(TJ.Achievements.PlayerAchievements.Unlock), new[] { typeof(TJ.Achievements.AchievementId) });
                Pedir(Plugin.CfgLogrosFiltrar.Value, "SaveDataHandler.EvaluateRaceCollection", typeof(Memori.SaveData.SaveDataHandler), nameof(Memori.SaveData.SaveDataHandler.EvaluateRaceCollection), new[] { typeof(Race) });
                Linea(faltan.Count == 0, "Parches de la 0.7.2 (interfaz, armería y logros)",
                    faltan.Count == 0 ? "todos los que piden sus interruptores están puestos" : "sin parche: " + string.Join(", ", faltan), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Parches de la 0.7.2 (interfaz, armería y logros)", e.ToString(), ref ok, ref total); }

            // --- 28) Lista de gear ampliada (armería y Colección) y suspendida (logro de objetos) ---
            // --- 28) Extended gear list (armory and Collection) and suspended (new gear achievement) ---
            if (Objetos.Operativo && Objetos.ActivosEnCache && Objetos.Ids.Count > 0)
            {
                try
                {
                    List<int> Leer()
                    {
                        var a = GearData.GetGearIDs();
                        var l = new List<int>();
                        for (int i = 0; a != null && i < a.Length; i++) l.Add((int)a[i]);
                        return l;
                    }
                    var fuera = Leer();
                    Objetos.EntrarListaAmpliada();
                    List<int> dentro, suspendida;
                    try
                    {
                        dentro = Leer();
                        int antes = ArmeriaObjetos.Suspender();
                        try { suspendida = Leer(); }
                        finally { ArmeriaObjetos.Reanudar(antes); }
                    }
                    finally { Objetos.SalirListaAmpliada(); }
                    var despues = Leer();
                    var activos = Objetos.Ids.ToList();
                    bool bien = fuera.All(id => id < 700) && activos.All(dentro.Contains) && dentro.Count == fuera.Count + activos.Count
                                && suspendida.SequenceEqual(fuera) && despues.SequenceEqual(fuera) && !ArmeriaObjetos.Ampliando;
                    Linea(bien, "Lista de gear ampliada",
                        $"fuera de contexto {fuera.Count} (máx. {(fuera.Count > 0 ? fuera.Max() : 0)}), dentro {dentro.Count} ({activos.Count} nuevos), suspendida {suspendida.Count}, después {despues.Count}", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Lista de gear ampliada", e.ToString(), ref ok, ref total); }
            }

            // --- 29) Listas de las razas de serie para los logros de colección ---
            // --- 29) Lists of the stock races for the collection achievements ---
            try
            {
                var logros = Memori.SaveData.SaveDataHandler.RaceCollectionAchievements;
                var partes = new List<string>();
                bool bien = logros != null && logros.Count > 0;
                int conSueltas = 0;
                foreach (Race r in Enum.GetValues(typeof(Race)))
                {
                    if (logros == null || !logros.ContainsKey(r)) continue;
                    var u = data.GetUnitsOfRace(r);
                    var ids = new List<int>();
                    for (int i = 0; u != null && i < u.Length; i++) ids.Add((int)u[i]);
                    int deSerie = ids.Count(LogicaLogros.EsUnidadDeSerie);
                    int nuevas = ids.Count - deSerie;
                    if (nuevas > 0) conSueltas++;
                    if (deSerie == 0) bien = false;
                    partes.Add($"{r} {deSerie}+{nuevas}");
                }
                bool sueltasEsperadas = Nucleo.Actual.Manifiesto.Unidades.Any(x => x.Activa && x.Reclutamiento == Manifiesto.ReclutaPorRaza
                                                                                && int.TryParse(x.Raza, out _) == false);
                Linea(bien && (!sueltasEsperadas || conSueltas > 0), "Listas de raza para los logros de colección",
                    $"de serie + sueltas que el filtro deja fuera: {string.Join(", ", partes)}; {conSueltas} razas con sueltas" +
                    (Plugin.CfgLogrosFiltrar.Value ? "" : " (Logros.Filtrar = false: no se filtran)"), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Listas de raza para los logros de colección", e.ToString(), ref ok, ref total); }

            // --- 30) Pantalla de héroes: héroes nuevos en la lista y casillas necesarias ---
            // --- 30) Heroes screen: new heroes in the list and required slots ---
            if (FaccionesNuevas.Activas)
            {
                try
                {
                    var lista = HeroData._heroesSorted;
                    var ids = new List<int>();
                    for (int i = 0; lista != null && i < lista.Count; i++) ids.Add(lista[i].HeroID);
                    var nuevos = FaccionesNuevas.Heroes.Keys.OrderBy(x => x).ToList();
                    int proximamente = PantallaHeroes.ContarProximamente();
                    int reserva = proximamente >= 0 ? CasillasHeroes.CasillasMisterio : 0;
                    int necesarias = CasillasHeroes.Necesarias(ids.Count, proximamente, reserva);
                    // 0.7.2 (revisión): casillas de serie reales (las que vio la pantalla al abrirse o, si aún no se ha
                    // abierto, las del juego instalado); antes se comparaba con 18, de la pantalla antigua. Si hacen falta más
                    // de las de serie, la pantalla ampliada tiene que estar activada
                    // 0.7.2 (review): real stock slots (the ones the screen saw when it opened or, if it has not been
                    // opened yet, those of the installed game); before, it was compared against 18, from the old screen. If more
                    // than the stock ones are needed, the extended screen must be enabled
                    int deSerie = PantallaHeroes.CasillasDeSerie > 0 ? PantallaHeroes.CasillasDeSerie : CasillasHeroes.CasillasDeSerieJuego;
                    string fuente = PantallaHeroes.CasillasDeSerie > 0 ? "vistas al abrir la pantalla" : "las del juego instalado; la pantalla aún no se ha abierto";
                    bool hayQueAmpliar = necesarias > deSerie;
                    bool bien = nuevos.Count > 0 && nuevos.All(ids.Contains) && ids.Distinct().Count() == ids.Count && (!hayQueAmpliar || Plugin.CfgPantallaHeroes.Value);
                    Linea(bien, "Pantalla de héroes (lista y casillas)",
                        $"{ids.Count} jugables con los nuevos [{string.Join(",", nuevos)}], {Math.Max(0, proximamente)} próximamente; hacen falta {necesarias} casillas " +
                        $"({CasillasHeroes.Tarjetas(necesarias)} tarjetas; la de serie tiene {deSerie}, {fuente}): {(hayQueAmpliar ? "hay que ampliar" : "caben sin ampliar")}. " +
                        $"Pantalla ampliada {(Plugin.CfgPantallaHeroes.Value ? "activada" : "desactivada")}", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Pantalla de héroes (lista y casillas)", e.ToString(), ref ok, ref total); }
            }

            // --- 31) Mapa de campaña: arreglo de la figura con LoadHero inlineado (sin cargar el mapa) ---
            // --- 31) Campaign map: fix for the figure with LoadHero inlined (without loading the map) ---
            try
            {
                // Campos del interop que usa CampanaHeroes.AsegurarFiguraEnMapa y parche puesto en CompleteLoad
                // Interop fields used by CampanaHeroes.AsegurarFiguraEnMapa and patch applied to CompleteLoad
                var pFiguras = typeof(TJ.Map.PlayerToken).GetProperty("heroPrefabs");
                var pFicha = typeof(TJ.Map.MapSceneManager).GetProperty("playerToken");
                bool tipoFiguras = pFiguras != null && pFiguras.CanWrite &&
                                   pFiguras.PropertyType == typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.GameObject>);
                bool tipoFicha = pFicha != null && pFicha.PropertyType == typeof(TJ.Map.PlayerToken);
                bool parche = TieneParche(typeof(TJ.Map.MapSceneManager), nameof(TJ.Map.MapSceneManager.CompleteLoad), Type.EmptyTypes);
                // Lógica pura con el caso de la prueba de Zero (héroe 901, 16 figuras de serie)
                // Pure logic with the test case from Zero (hero 901, 16 default figures)
                bool logica = LogicaCampanaHeroes.LongitudNecesaria(901, 16) == 901 &&
                              LogicaCampanaHeroes.OrigenFigura(901, 16, i => true, CampanaHeroes.Prestado, HeroData.DefaultHeroID) ==
                              LogicaCampanaHeroes.IndiceFigura(901, 16, CampanaHeroes.Prestado, HeroData.DefaultHeroID);   // misma cadena prestado -> Edric -> primera
                                                                                                                           // same chain borrowed -> Edric -> first
                Linea(tipoFiguras && tipoFicha && parche && logica, "Mapa de campaña (figura con LoadHero inlineado)",
                    $"PlayerToken.heroPrefabs {(pFiguras == null ? "no existe" : pFiguras.PropertyType.Name + (pFiguras.CanWrite ? " (escribible)" : " (solo lectura)"))}, " +
                    $"MapSceneManager.playerToken {(pFicha == null ? "no existe" : pFicha.PropertyType.Name)}, prefix de CompleteLoad {(parche ? "puesto" : "NO puesto")}, " +
                    $"Yarrick -> {LogicaCampanaHeroes.LongitudNecesaria(901, 16)} figuras con la del héroe {CampanaHeroes.Prestado(901)}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Mapa de campaña (figura con LoadHero inlineado)", e.ToString(), ref ok, ref total); }

            // --- 32) Juego 1.11.1: el hechizo emblema de cada héroe nuevo está registrado (si no, su carta sale vacía) ---
            // --- 32) Game 1.11.1: the signature spell of each new hero is registered (otherwise, their card comes out empty) ---
            if (FaccionesNuevas.Activas)
            {
                try
                {
                    var partes = new List<string>();
                    var sustituidos = new List<string>();
                    bool bien = FaccionesNuevas.Heroes.Count > 0;
                    foreach (var h in FaccionesNuevas.Heroes.Values.OrderBy(x => x.Id))
                    {
                        var hechizo = HeroData.GetHeroByID(h.Id).SignatureSpell;
                        var estado = HeroesNuevos.EstadoEnRegistro(hechizo.ToString());
                        bool pedido = string.Equals(h.Hechizo, hechizo.ToString(), StringComparison.Ordinal);
                        bien &= estado == LogicaHechizos.Estado.Registrado;
                        partes.Add($"{h.Id} {hechizo} ({(estado == LogicaHechizos.Estado.Registrado ? "registrado" : estado == LogicaHechizos.Estado.NoRegistrado ? "NO registrado" : "sin poder comprobar")}" +
                                   $"{(pedido ? "" : $", sustituye a {h.Hechizo}")})");
                        if (!pedido) sustituidos.Add($"{h.Id} lleva {hechizo} en vez de {h.Hechizo}");
                    }
                    Linea(bien, "Hechizos de los héroes nuevos registrados", string.Join("; ", partes), ref ok, ref total);
                    // 0.7.4: un héroe que cayó a un sustituto (plan B, prestado o RallyTheBanners) no es un fallo de la carga,
                    // pero sí algo que mirar: su hechizo propio u oficial no se pudo usar
                    // 0.7.4: a hero that fell back to a substitute (plan B, borrowed or RallyTheBanners) is not a load failure,
                    // but it is something to look at: their own or official spell could not be used
                    if (sustituidos.Count > 0)
                        Plugin.Aviso($"[autoprueba] Hechizos de los héroes nuevos AVISO: {string.Join("; ", sustituidos)} (mira los avisos de «Hechizo propio» y «Héroe nuevo» más arriba).");
                }
                catch (Exception e) { Linea(false, "Hechizos de los héroes nuevos registrados", e.ToString(), ref ok, ref total); }
            }

            // --- 33) 0.7.4: hechizos propios en el registro del juego, en su héroe y en el grimorio ---
            // --- 33) 0.7.4: custom spells in the game registry, on their hero and in the grimoire ---
            if (Hechizos.Definidos.Count > 0)
            {
                try
                {
                    var partes = new List<string>();
                    bool bien = true;
                    var grimorio = new HashSet<int>();
                    var lista = TJ.Spells.SpellLoadout.GetGrimoireSpells();
                    for (int i = 0; lista != null && i < lista.Count; i++) grimorio.Add((int)lista[i]);
                    foreach (var h in Hechizos.Definidos.Values.OrderBy(x => x.Id))
                    {
                        var d = Hechizos.Datos(h.Id);
                        bool registrado = Hechizos.Registrado(h.Id) && d != null;
                        // Dueño según el juego (el primer héroe con ese hechizo de firma): tiene que ser el nuestro
                        // 03-11-2026: a mano (DuenoDelHechizo); por el envoltorio del interop desbordaba la pila
                        // Owner according to the game (the first hero with that signature spell): it must be ours
                        // 03-11-2026: by hand (DuenoDelHechizo); going through the interop wrapper overflowed the stack
                        bool conDueno = DuenoDelHechizo((TJ.Spells.Spell)h.Id, out int dueno);
                        bool suyo = conDueno && dueno == h.Heroe;
                        bool enGrimorio = grimorio.Contains(h.Id);
                        bool enHeroe = (int)HeroData.GetHeroByID(h.Heroe).SignatureSpell == h.Id;
                        bien &= registrado && suyo && enGrimorio && enHeroe;
                        partes.Add(registrado
                            ? $"{h.Id} «{h.NombreEs}» de {h.Base} (raza {d.Race}, maná {d.SpellManaCost}, valor {d.SpellModifierValue}, radio {d.SpellRadius:0.#}, " +
                              $"equipo {d.TargetTeam}; dueño {(conDueno ? dueno.ToString() : "ninguno")}, {(enHeroe ? "en su héroe" : "NO en su héroe")}, " +
                              $"{(enGrimorio ? "en el grimorio" : "NO en el grimorio")})"
                            : $"{h.Id} «{h.NombreEs}» NO registrado");
                    }
                    Linea(bien, "Hechizos propios registrados y en su héroe",
                          $"{string.Join("; ", partes)}; inyecciones en la sesión {Hechizos.Inyecciones}", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Hechizos propios registrados y en su héroe", e.ToString(), ref ok, ref total); }

                // --- 35) 03-11-2026: bandas propias «facción A» (40) e «facción B» (41) en el grimorio de la
                //     preparación (SpellLoadout.GrimoireGroupOrder) y en el menú de hechizos de la batalla (SpellBrowseMenu.GROUP_ORDER)
                // --- 35) 03-11-2026: custom bands «facción A» (40) and «facción B» (41) in the grimoire of the
                //     setup (SpellLoadout.GrimoireGroupOrder) and in the battle spell menu (SpellBrowseMenu.GROUP_ORDER)
                try
                {
                    var conBanda = Hechizos.Definidos.Values.Where(h => int.TryParse(h.Raza, out int r) && LogicaHechizos.EsRazaBandaPropia(r)).ToList();
                    if (conBanda.Count > 0)
                    {
                        var grim = Hechizos.OrdenGrimorio();
                        var bat = Hechizos.OrdenBatalla();
                        var partes = new List<string>();
                        bool bien = true;
                        foreach (var h in conBanda.OrderBy(x => x.Id))
                        {
                            int r = int.Parse(h.Raza);
                            var d = Hechizos.Datos(h.Id);
                            bool raza = d != null && (int)d.Race == r;
                            bool b1 = grim.Contains(r), b2 = bat.Contains(r);
                            bien &= raza && b1 && b2;
                            partes.Add($"{h.Id} raza {(d == null ? "?" : ((int)d.Race).ToString())} (pide {r}), banda en el grimorio {(b1 ? "sí" : "NO")}, en la batalla {(b2 ? "sí" : "NO")}");
                        }
                        Linea(bien, "Bandas propias del grimorio (Astra Militarum e Imperio Galáctico)",
                              $"{string.Join("; ", partes)}; orden del grimorio {string.Join(",", grim)}", ref ok, ref total);
                    }
                }
                catch (Exception e) { Linea(false, "Bandas propias del grimorio (Astra Militarum e Imperio Galáctico)", e.ToString(), ref ok, ref total); }
            }

            // --- 34) 0.7.4: parches nuevos puestos (Grand Grimoire, multijugador y munición de las híbridas). 0.7.5: y los de la
            //     revisión total (recuento de logros por partida, reenvío, InviteFriend y líneas de comandos) ---
            // --- 34) 0.7.4: new patches applied (Grand Grimoire, multiplayer and hybrid ammo). 0.7.5: and those of the
            //     full review (achievement count per run, resend, InviteFriend and command lines) ---
            try
            {
                var faltan = new List<string>();
                // Ronda 2 de la revisión: los tipos nuevos de la 1.11.1 se buscan por nombre (sin typeof): si el juego los mueve,
                // esta línea sale como FALLO con «falta el tipo» en vez de tumbar el JIT de toda la autoprueba
                // Review round 2: the new 1.11.1 types are looked up by name (no typeof): if the game moves them,
                // this line comes out as FAIL with "missing type" instead of bringing down the JIT of the whole self-test
                bool Pedir(string tipo, string metodo, Type[] args = null)
                {
                    var t = Plugin.TipoPorNombre(tipo);
                    if (t == null) { faltan.Add($"{tipo} (falta el tipo)"); return false; }
                    if (TieneParche(t, metodo, args)) return true;
                    // 0.7.5 (hallazgo 37): un parche omitido a mano se distingue de uno que no engancha
                    // 0.7.5 (finding 37): a manually skipped patch is distinguished from one that does not hook
                    faltan.Add(Plugin.MetodosOmitidos.Contains(metodo) ? $"{t.Name}.{metodo} (OMITIDO por Diagnostico.OmitirParches)" : $"{t.Name}.{metodo}");
                    return false;
                }
                if (Plugin.CfgLogrosFiltrar == null || Plugin.CfgLogrosFiltrar.Value)
                {
                    Pedir("TJ.Achievements.BattleAchievements, TabletopTavern.Core", "Evaluate");
                    Pedir("TJ.Spells.SpellCastRecord, TabletopTavern.Core", "Remaining");
                    // 0.7.5: recuento por partida del historial y reenvío de los ya ganados (logros de ejército)
                    // 0.7.5: per-run count of the history and resend of those already earned (army achievements)
                    Pedir("TJ.Achievements.AchievementRules, TabletopTavern.Core", "FinalBattleArmy");
                    Pedir("TJ.Achievements.AchievementRules, TabletopTavern.Core", "ForFinalArmy");
                    Pedir("TJ.Achievements.AchievementSync, TabletopTavern.Core", "SendEarned");
                }
                // Multijugador: las entradas y la última red (todas en la misma instancia de Harmony, que siempre se aplica)
                // Multiplayer: the entries and the last network (all in the same Harmony instance, which is always applied)
                string multijugador = "sin módulos aplicados (no bloquea)";
                if (Nucleo.PartidaModificada && (Plugin.CfgBloquearMultijugador == null || Plugin.CfgBloquearMultijugador.Value))
                {
                    const string lobby = "TJ.Versus.Net.VersusLobby, TabletopTavern.Core";
                    Pedir("TJ.Versus.Net.QuickBattleQueue, TabletopTavern.Core", "Start", new[] { typeof(bool) });
                    Pedir(lobby, "HostCustomAsync"); Pedir(lobby, "HostCoopAsync"); Pedir(lobby, "JoinAsync");
                    Pedir(lobby, "OnJoinRequested"); Pedir(lobby, "StartLink");
                    Pedir("TJ.Versus.Net.VersusRejoin, TabletopTavern.Core", "Resume");
                    // 0.7.5: HostCoopAsync va copiado dentro de InviteFriend en la 1.11.1 (su parche no salta): el cooperativo como
                    // anfitrión se corta en CoopSession.InviteFriend. Y las dos rutas de línea de comandos
                    // 0.7.5: HostCoopAsync is copied inside InviteFriend in 1.11.1 (its patch does not trigger): co-op as
                    // host is cut off in CoopSession.InviteFriend. And the two command-line routes
                    Pedir("TJ.Versus.Net.CoopSession, TabletopTavern.Core", "InviteFriend", Type.EmptyTypes);
                    Pedir("TJ.Versus.Net.QuickBattleQueue, TabletopTavern.Core", "BootFromCommandLine", Type.EmptyTypes);
                    Pedir("TJ.Versus.Net.VersusNet, TabletopTavern.Core", "BootFromCommandLine", Type.EmptyTypes);
                    // ¿Se puede crear la tarea terminada con la que se cortan las salas? (si no, solo queda StartLink)
                    // Can the completed task used to cut off the rooms be created? (if not, only StartLink remains)
                    string tarea;
                    try { tarea = Saves.TareaTerminada() != null ? "tarea terminada: sí" : "tarea terminada: NO (solo StartLink)"; }
                    catch (Exception e) { tarea = $"tarea terminada: NO ({e.GetType().Name}; solo StartLink)"; }
                    // 0.7.5: VersusRejoin.Resume (que el menú llama siempre) ya no suma: al arrancar debe salir 0
                    // 0.7.5: VersusRejoin.Resume (which the menu always calls) no longer adds up: it must yield 0 at startup
                    multijugador = $"bloqueado (cooperativo como anfitrión en InviteFriend y StartLink); {tarea}; intentos bloqueados en la sesión {Saves.BloqueosOnline}";
                }
                bool hibridas = EjercitoReforzado.Operativo && EjercitoReforzado.Municion?.Tipos?.Contains("Hybrid") == true && EjercitoReforzado.MultiplicadorMunicion > 1;
                if (hibridas && !TieneParche(typeof(TJ.HeroBonusManager), nameof(TJ.HeroBonusManager.GetHeroStatBonus),
                        new[] { typeof(UnitStat), typeof(UnitName), typeof(int), typeof(float), typeof(Race) })) faltan.Add("HeroBonusManager.GetHeroStatBonus (5)");
                Linea(faltan.Count == 0, "Parches de la 0.7.4 (Grand Grimoire, multijugador, munición de las híbridas)",
                      faltan.Count == 0
                          ? $"puestos; multijugador {multijugador}; munición de las híbridas {(hibridas ? $"x{EjercitoReforzado.MultiplicadorMunicion} (añadida {EjercitoReforzado.MunicionHibridasAnadida}, ya del juego {EjercitoReforzado.MunicionHibridasDelJuego} en la sesión)" : "sin módulo")}"
                          : "faltan " + string.Join(", ", faltan), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Parches de la 0.7.4 (Grand Grimoire, multijugador, munición de las híbridas)", e.ToString(), ref ok, ref total); }

            // --- 36) 0.7.6: módulo «Impertérritos» (moral del jugador). Solo si está activo: sus tres parches puestos (batalla,
            //     batalla nueva y resolución automática) y las cuentas de la sesión (al arrancar, 0: aún no hubo batalla) ---
            // --- 36) 0.7.6: module "Impertérritos" (player morale). Only if active: its three patches applied (battle,
            //     new battle and auto-resolve) and the session counts (at startup, 0: no battle yet) ---
            if (MoralJugador.Activo)
            {
                try
                {
                    var faltan = new List<string>();
                    if (!TieneParche(typeof(EntityWatcher), "Update", Type.EmptyTypes)) faltan.Add("EntityWatcher.Update");
                    if (!TieneParche(typeof(EntityWatcher), "SetUp", Type.EmptyTypes)) faltan.Add("EntityWatcher.SetUp");
                    var tAuto = Plugin.TipoPorNombre("TJ.Engagement.AutoResolveSimulation, TabletopTavern.Core");
                    if (tAuto == null) faltan.Add("AutoResolveSimulation (falta el tipo)");
                    else if (!TieneParche(tAuto, "UpdateMorale")) faltan.Add("AutoResolveSimulation.UpdateMorale");
                    var ef = MoralJugador.Efecto;
                    Linea(faltan.Count == 0, "Moral del jugador (Impertérritos)",
                          faltan.Count == 0
                              ? $"parches puestos; sin huida {ef.SinHuida}, sin flaquear {ef.SinVacilar}; escuadras ajustadas en la sesión {MoralJugador.EscuadrasSesion}, pasos de resolución automática {MoralJugador.PasosAutoSesion}"
                              : "faltan " + string.Join(", ", faltan), ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Moral del jugador (Impertérritos)", e.ToString(), ref ok, ref total); }
            }

            // --- 37) 0.7.7: estandarte (Estandarte.cs). Con facciones nuevas o algún módulo con iconos: parches puestos y DÓNDE
            //     se aplica el icono elegido (vista previa y banderas del jugador en batalla; el mapa no pinta el estandarte:
            //     xref de notas internas (no publicadas)) ---
            // --- 37) 0.7.7: banner (Estandarte.cs). With new factions or any module with icons: patches applied and WHERE
            //     the chosen icon is applied (preview and player flags in battle; the map does not draw the banner:
            //     xref of internal notes (unpublished)) ---
            if (FaccionesNuevas.Activas || Estandarte.HayIconos)
            {
                try
                {
                    var faltan = new List<string>();
                    if (!TieneParche(typeof(TJ.MainMenu.WarbandBannerPanel), "EnsureBuilt", Type.EmptyTypes)) faltan.Add("WarbandBannerPanel.EnsureBuilt");
                    if (Estandarte.HayIconos)
                    {
                        if (!TieneParche(typeof(TJ.MainMenu.WarbandBannerPanel), "Refresh", Type.EmptyTypes)) faltan.Add("WarbandBannerPanel.Refresh");
                        if (!TieneParche(typeof(TJ.SquadFlagGameObject), "ApplyTeamColors", Type.EmptyTypes)) faltan.Add("SquadFlagGameObject.ApplyTeamColors");
                    }
                    var tex = Estandarte.TexturaElegida();
                    string donde = Estandarte.HayIconos
                        ? "el icono elegido se aplica en la vista previa de la pestaña Estandarte (WarbandBannerPanel.Refresh) y en las banderas de las escuadras del jugador en batalla (SquadFlagGameObject.ApplyTeamColors, squadId > 0); el mapa de campaña no pinta el estandarte"
                        : "sin módulos con iconos: solo los colores de facción nuevos";
                    Linea(faltan.Count == 0, "Estandarte (colores de facción e iconos)",
                          faltan.Count == 0
                              ? $"parches puestos; {Estandarte.IconosModulos} iconos de módulos; elegido «{(tex == null ? "del héroe (de serie)" : tex.name)}»; {donde}; en la sesión: vista previa {Estandarte.VistasSesion}, banderas {Estandarte.BanderasSesion}"
                              : "faltan " + string.Join(", ", faltan), ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Estandarte (colores de facción e iconos)", e.ToString(), ref ok, ref total); }
            }
        }
    }
}
