// AutoPruebaModulos.cs
// Pruebas de arranque de los módulos (0.7.0), parte de AutoPrueba (resultado con la etiqueta [autoprueba]):
//   M1) Detección: módulos encontrados en Mods, activos, sincronizados desde BepInEx\plugins y los que vienen
//       desactivados por defecto apuntados en modlist.json; ningún módulo rechazado.
//   M2) Combinaciones en memoria con las declaraciones reales (sin tocar el juego): ninguno, cada módulo solo, las tres
//       escuadras juntas (manda la mayor, con aviso), las 8 combinaciones de los tres módulos de contenido (la 900 solo
//       con la facción A; cada objeto solo con su módulo; facciones con y sin sueltas) y rutas de recursos resueltas.
//   M3) Combinaciones aplicadas al juego: se suma un conjunto, se reconstruye el catálogo (ReloadData) y se comprueba
//       catálogo 9xx, la 900, razas, héroes, objetos, escalado de escuadras y reglas del jugador; al final se vuelve a los
//       módulos reales y se comprueba que todo queda como al principio. Los módulos de contenido solo se simulan si el
//       juego los cargó en esta sesión (si no, sus stats y textos no estarían en el catálogo).
// Autor: ZeroitMods.
// AutoPruebaModulos.cs
// Module startup tests (0.7.0), part of AutoPrueba (result with the [autoprueba] tag):
//   M1) Detection: modules found in Mods, active, synced from BepInEx\plugins and those that come
//       disabled by default listed in modlist.json; no module rejected.
//   M2) In-memory combinations with the real declarations (without touching the game): none, each module alone, all three
//       squads together (the largest wins, with a warning), the 8 combinations of the three content modules (900 only
//       with faction A; each item only with its module; factions with and without loose ones) and resolved resource paths.
//   M3) Combinations applied to the game: a set is added, the catalog is rebuilt (ReloadData) and the following are checked:
//       9xx catalog, 900, races, heroes, items, squad scaling and player rules; at the end it goes back to the real
//       modules and it is checked that everything is as it was at the start. Content modules are only simulated if the
//       game loaded them in this session (otherwise their stats and texts would not be in the catalog).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TJ;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: pruebas de los módulos
    // Section: module tests
    // -----------------------------------------------------------------------
    public static partial class AutoPrueba
    {
        // Ids de los módulos que genera generar_mod_facciones.py
        // Module ids produced by generar_mod_facciones.py
        internal const string MMunicion = "ZeroMunicion", MEsc50 = "ZeroEscuadras50", MEsc100 = "ZeroEscuadras100", MEsc150 = "ZeroEscuadras150",
                              MStats = "ZeroStatsDistancia", MSueltas = "ZeroUnidadesSueltas", MAstra = "ZeroAstraMilitarum",
                              MImperio = "ZeroImperioGalactico", MEconomia = "ZeroEconomia",
                              MImperterritos = "ZeroImperterritos";   // 0.7.6 (03-11-2026): moral del jugador
                                                                      // 0.7.6 (03-11-2026): player morale

        // Rangos de ids que trae cada módulo de contenido (unidades y objetos)
        // Id ranges that each content module brings (units and gear)
        private static readonly Dictionary<string, (int uDesde, int uHasta, int oDesde, int oHasta, int raza)> Contenido =
            new Dictionary<string, (int, int, int, int, int)>(StringComparer.OrdinalIgnoreCase)
            {
                [MSueltas] = (941, 972, 720, 727, 0),
                [MAstra] = (900, 913, 700, 706, 40),
                [MImperio] = (921, 934, 710, 716, 41),
            };

        // Todos los módulos encontrados marcados como activos si están en «ids» (para las combinaciones en memoria)
        // All modules found marked as active if they are in «ids» (for the in-memory combinations)
        private static ResultadoModulos Combinar(params string[] ids)
        {
            var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
            var lista = Nucleo.Encontrados.Select(m => new ModuloEncontrado
            {
                Declaracion = m.Declaracion, Carpeta = m.Carpeta, NombreCarpeta = m.NombreCarpeta, Activo = set.Contains(m.Id),
            }).ToList();
            return Modulos.Resolver(lista, Plugin.VERSION);
        }

        private static bool Instalado(string id) => Nucleo.Encontrados.Any(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

        // ¿El contenido de un resultado es exactamente el de estos módulos de contenido? (unidades, objetos, razas, héroes)
        // Is the content of a result exactly that of these content modules? (units, gear, races, heroes)
        private static bool ContenidoExacto(ResultadoModulos r, IEnumerable<string> modulos, out string detalle)
        {
            var mods = modulos.Where(Contenido.ContainsKey).ToList();
            var uEsp = new SortedSet<int>(mods.SelectMany(m => Enumerable.Range(Contenido[m].uDesde, Contenido[m].uHasta - Contenido[m].uDesde + 1)));
            var oEsp = new SortedSet<int>(mods.SelectMany(m => Enumerable.Range(Contenido[m].oDesde, Contenido[m].oHasta - Contenido[m].oDesde + 1)));
            var rEsp = new SortedSet<int>(mods.Select(m => Contenido[m].raza).Where(x => x > 0));
            var hEsp = new SortedSet<int>(rEsp.SelectMany(x => x == 40 ? new[] { 901, 902 } : new[] { 903, 904 }));
            var u = new SortedSet<int>(r.Manifiesto.Unidades.Select(x => x.Id));
            var o = new SortedSet<int>(r.Manifiesto.Objetos.Select(x => x.Id));
            var ra = new SortedSet<int>(r.Manifiesto.Razas.Select(x => x.Id));
            var h = new SortedSet<int>(r.Manifiesto.Heroes.Select(x => x.Id));
            detalle = $"{u.Count} unidades{(u.Contains(900) ? " (con la 900)" : "")}, objetos [{Rango(o)}], razas [{string.Join(",", ra)}], héroes [{string.Join(",", h)}]";
            return u.SetEquals(uEsp) && o.SetEquals(oEsp) && ra.SetEquals(rEsp) && h.SetEquals(hEsp);
        }

        // Lista de ids compacta (700-706,710-716)
        // Compact id list (700-706,710-716)
        private static string Rango(IEnumerable<int> ids)
        {
            var l = ids.OrderBy(x => x).ToList();
            var partes = new List<string>();
            for (int i = 0; i < l.Count;)
            {
                int j = i;
                while (j + 1 < l.Count && l[j + 1] == l[j] + 1) j++;
                partes.Add(i == j ? l[i].ToString() : $"{l[i]}-{l[j]}");
                i = j + 1;
            }
            return string.Join(",", partes);
        }

        // ---------------------------------------------------------------
        // M1 y M2: detección y combinaciones en memoria
        // M1 and M2: detection and in-memory combinations
        // ---------------------------------------------------------------
        private static void PruebasModulosMemoria(ref int ok, ref int total)
        {
            // --- M1) Detección ---
            // --- M1) Detection ---
            try
            {
                var lista = OrdenMods.LeerLista(Nucleo.CarpetaMods, out _) ?? new List<OrdenMods.Entrada>();
                var porDefectoNo = Nucleo.Encontrados.Where(m => !m.Declaracion.ActivoPorDefecto).ToList();
                var sinApuntar = porDefectoNo.Where(m => !lista.Any(e => string.Equals(e.Folder, m.NombreCarpeta, StringComparison.OrdinalIgnoreCase))).Select(m => m.Id).ToList();
                string estados = string.Join(", ", porDefectoNo.Select(m => $"{m.Id} {(m.Activo ? "activo" : "desactivado")}"));
                bool bien = Nucleo.Encontrados.Count > 0 && sinApuntar.Count == 0 && Nucleo.Real.Rechazados.Count == 0;
                Linea(bien, "Módulos detectados",
                    $"{Nucleo.Encontrados.Count} en Mods ({string.Join(", ", Nucleo.Encontrados.Select(m => m.Id))}); activos [{string.Join(", ", Nucleo.Encontrados.Where(m => m.Activo).Select(m => m.Id))}]; " +
                    $"desactivados por defecto apuntados en modlist.json: {(porDefectoNo.Count == 0 ? "(ninguno instalado)" : estados)}{(sinApuntar.Count > 0 ? $"; SIN APUNTAR: {string.Join(", ", sinApuntar)}" : "")}; " +
                    $"rechazados {Nucleo.Real.Rechazados.Count}; sincronización: {Nucleo.Sincronizacion}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Módulos detectados", e.ToString(), ref ok, ref total); }

            // --- M2a) Ninguno: nada que aplicar ---
            // --- M2a) None: nothing to apply ---
            try
            {
                var r = Combinar();
                bool bien = r.Aplicados.Count == 0 && !r.HayContenido && r.FactorEscuadras == 1.0 && r.Municion == null && r.Distancia == null && r.Moral == null;
                Linea(bien, "Combinación en memoria «ninguno»", $"aplicados {r.Aplicados.Count}, contenido {r.HayContenido}, escuadras x{r.FactorEscuadras}, munición {(r.Municion == null ? "no" : "sí")}, distancia {(r.Distancia == null ? "no" : "sí")}, moral {(r.Moral == null ? "de serie" : "sí")}", ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Combinación en memoria «ninguno»", e.ToString(), ref ok, ref total); }

            // --- M2b) Cada módulo solo ---
            // --- M2b) Each module alone ---
            try
            {
                var partes = new List<string>();
                bool bien = true;
                foreach (var m in Nucleo.Encontrados.OrderBy(x => x.Id))
                {
                    var r = Combinar(m.Id);
                    bool b = r.Aplicados.Count == 1;
                    string d;
                    switch (m.Id)
                    {
                        case MMunicion: b &= r.Municion?.Multiplicador == 3 && r.Distancia == null && r.FactorEscuadras == 1.0 && !r.HayContenido; d = $"munición x{r.Municion?.Multiplicador}"; break;
                        case MEsc50: b &= Math.Abs(r.FactorEscuadras - 1.5) < 1e-9 && r.MinimoSoldados == 2 && r.Municion == null && !r.HayContenido; d = $"escuadras x{r.FactorEscuadras}"; break;
                        case MEsc100: b &= Math.Abs(r.FactorEscuadras - 2.0) < 1e-9 && r.MinimoSoldados == 2 && !r.HayContenido; d = $"escuadras x{r.FactorEscuadras}"; break;
                        case MEsc150: b &= Math.Abs(r.FactorEscuadras - 2.5) < 1e-9 && r.MinimoSoldados == 2 && !r.HayContenido; d = $"escuadras x{r.FactorEscuadras}"; break;
                        case MStats:
                            b &= r.Distancia != null && r.Distancia.Alcance == 10 && r.Distancia.Precision == 10 && r.Distancia.Dano == 3 && r.Distancia.Tipos.SequenceEqual(new[] { "Ranged" })
                                 && r.Municion == null && r.FactorEscuadras == 1.0 && !r.HayContenido;
                            d = $"distancia +{r.Distancia?.Alcance}/+{r.Distancia?.Precision}/+{r.Distancia?.Dano}"; break;
                        case MEconomia: b &= !r.HayContenido && r.FactorEscuadras == 1.0 && r.Municion == null && r.Distancia == null && r.Moral == null; d = "sin contenido ni efectos (solo JSON del juego)"; break;
                        // 0.7.6: «Impertérritos», solo la moral del jugador (sin huida y sin flaquear)
                        // 0.7.6: «Impertérritos», player morale only (no fleeing and no wavering)
                        case MImperterritos:
                            b &= r.Moral != null && r.Moral.SinHuida && r.Moral.SinVacilar && r.Municion == null && r.Distancia == null && r.FactorEscuadras == 1.0 && !r.HayContenido;
                            d = r.Moral == null ? "moral de serie (NO)" : $"moral sin huida {r.Moral.SinHuida}, sin flaquear {r.Moral.SinVacilar}"; break;
                        default:
                            if (Contenido.ContainsKey(m.Id)) { b &= ContenidoExacto(r, new[] { m.Id }, out d) && r.FactorEscuadras == 1.0 && r.Municion == null; }
                            else d = "(módulo no conocido por la autoprueba)";
                            break;
                    }
                    bien &= b;
                    partes.Add($"{m.Id}: {d}{(b ? "" : " MAL")}");
                }
                Linea(bien && Nucleo.Encontrados.Count > 0, "Combinaciones en memoria, cada módulo solo", string.Join("; ", partes), ref ok, ref total);
            }
            catch (Exception e) { Linea(false, "Combinaciones en memoria, cada módulo solo", e.ToString(), ref ok, ref total); }

            // --- M2c) Las tres escuadras juntas: manda la mayor y se avisa ---
            // --- M2c) The three squads together: the largest wins and a warning is given ---
            if (Instalado(MEsc50) && Instalado(MEsc100) && Instalado(MEsc150))
            {
                try
                {
                    var r = Combinar(MEsc50, MEsc100, MEsc150);
                    var r2 = Combinar(MEsc150, MEsc50);
                    bool aviso = r.Avisos.Any(a => a.Contains("manda el mayor"));
                    bool bien = Math.Abs(r.FactorEscuadras - 2.5) < 1e-9 && r.ModuloEscuadras == MEsc150 && aviso && Math.Abs(r2.FactorEscuadras - 2.5) < 1e-9
                                && Modulos.SoldadosEnCampo(48, r.FactorEscuadras) == 120 && Modulos.SoldadosEnCampo(1, r.FactorEscuadras) == 1;
                    Linea(bien, "Escuadras juntas en memoria",
                        $"+50 %, +100 % y +150 % activos: x{r.FactorEscuadras} de {r.ModuloEscuadras} (esperado x2,5, sin multiplicarse); aviso «manda el mayor» {aviso}; " +
                        $"en otro orden x{r2.FactorEscuadras}; 48 soldados -> {Modulos.SoldadosEnCampo(48, r.FactorEscuadras)}, 1 soldado -> 1", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Escuadras juntas en memoria", e.ToString(), ref ok, ref total); }
            }

            // --- M2d) Las 8 combinaciones de los módulos de contenido (la 900 solo con Astra; cada objeto con el suyo) ---
            // --- M2d) The 8 combinations of the content modules (the 900 only with Astra; each item with its own) ---
            var deContenido = new[] { MSueltas, MAstra, MImperio }.Where(Instalado).ToArray();
            if (deContenido.Length > 0)
            {
                try
                {
                    var partes = new List<string>();
                    bool bien = true;
                    for (int mascara = 0; mascara < (1 << deContenido.Length); mascara++)
                    {
                        var ids = deContenido.Where((m, i) => (mascara & (1 << i)) != 0).ToArray();
                        var r = Combinar(ids);
                        bool b = ContenidoExacto(r, ids, out string d);
                        bool con900 = r.Manifiesto.Unidades.Any(u => u.Id == 900);
                        b &= con900 == ids.Contains(MAstra, StringComparer.OrdinalIgnoreCase);
                        // Rutas de recursos de cada entrada dentro de la carpeta de su módulo y existentes
                        // Resource paths of each entry inside its module's folder and existing
                        foreach (var h in r.Manifiesto.Heroes) b &= Path.IsPathRooted(h.Retrato) && File.Exists(h.Retrato);
                        foreach (var o in r.Manifiesto.Objetos) b &= Path.IsPathRooted(o.Icono) && File.Exists(o.Icono);
                        foreach (var u in r.Manifiesto.Unidades) b &= !string.IsNullOrEmpty(u.Carpeta) && string.Equals(Nucleo.Encontrados.First(m => m.Id == u.Modulo).Carpeta, u.Carpeta, StringComparison.OrdinalIgnoreCase);
                        bien &= b;
                        partes.Add($"[{(ids.Length == 0 ? "ninguno" : string.Join("+", ids.Select(x => x.Replace("Zero", ""))))}] {d}{(b ? "" : " MAL")}");
                    }
                    Linea(bien, $"Combinaciones de contenido en memoria ({1 << deContenido.Length})", string.Join("; ", partes), ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Combinaciones de contenido en memoria", e.ToString(), ref ok, ref total); }
            }
        }

        // ---------------------------------------------------------------
        // M3: combinaciones aplicadas al juego (catálogo reconstruido) y vuelta a los módulos reales
        // M3: combinations applied to the game (catalog rebuilt) and return to the real modules
        // ---------------------------------------------------------------
        private static void PruebasModulosJuego(TabletopTavernData data, ref int ok, ref int total)
        {
            // Estado real de partida, para comprobar la vuelta
            // Actual run state, to verify the return
            int nuevasReal = ContarNuevas(data);
            int catalogoReal = data.SquadStatsDictionary.Count;
            double factorReal = EjercitoReforzado.Factor;
            int reglasReal = EjercitoReforzado.ReglasInyectadas;
            int objetosReal = Objetos.Numero;

            // Escenarios: los de contenido solo si el juego los cargó (activos); los de efectos aunque estén desactivados
            // Scenarios: content ones only if the game loaded them (active); effects ones even if they are disabled
            var escenarios = new List<(string nombre, string[] ids)> { ("ninguno", new string[0]) };
            foreach (string m in new[] { MSueltas, MAstra, MImperio }) escenarios.Add((m + " solo", new[] { m }));
            escenarios.Add(("facciones sin sueltas", new[] { MAstra, MImperio }));
            foreach (string m in new[] { MEsc50, MEsc100, MEsc150, MMunicion, MStats }) escenarios.Add((m + " solo", new[] { m }));
            escenarios.Add(("tres escuadras juntas", new[] { MEsc50, MEsc100, MEsc150 }));

            int baseArqueros = -1, baseMunicion = -1;
            // 0.7.1: las reglas del jugador son solo para el héroe activo y en el menú no hay ninguno: durante las simulaciones
            // se fija Morvayne (9, sin reglas propias) como héroe activo; cada simulación recarga el catálogo y con él las reglas
            // 0.7.1: player rules apply only to the active hero and there is none in the menu: during simulations
            // Morvayne (9, with no rules of his own) is set as the active hero; each simulation reloads the catalog and with it the rules
            EjercitoReforzado.FijarForzado(9);
            try
            {
                foreach (var (nombre, ids) in escenarios)
                {
                    if (ids.Any(id => !Instalado(id))) continue;
                    try
                    {
                        var r = Nucleo.Simular(data, ids, out var omitidos);
                        if (omitidos.Count > 0)
                        {
                            Plugin.Info($"[autoprueba] Simulación «{nombre}» (no cuenta): {string.Join(", ", omitidos)} " +
                                        (omitidos.Count == 1 ? "no está activo" : "no están activos") + " en esta sesión; no se simula.");
                            continue;
                        }
                        ComprobarEscenario(data, nombre, ids, r, ref baseArqueros, ref baseMunicion, ref ok, ref total);
                    }
                    catch (Exception e) { Linea(false, $"Simulación «{nombre}»", e.ToString(), ref ok, ref total); }
                }
            }
            finally
            {
                // --- Vuelta a los módulos reales (sin héroe forzado: la recarga deja las reglas del estado real) ---
                // --- Return to the real modules (no forced hero: the reload leaves the rules of the real state) ---
                EjercitoReforzado.FijarForzado(null);
                try
                {
                    Nucleo.Restaurar(data);
                    int nuevas = ContarNuevas(data);
                    bool bien = nuevas == nuevasReal && data.SquadStatsDictionary.Count == catalogoReal && Math.Abs(EjercitoReforzado.Factor - factorReal) < 1e-9
                                && EjercitoReforzado.ReglasInyectadas == reglasReal && Objetos.Numero == objetosReal && Nucleo.Actual == Nucleo.Real;
                    Linea(bien, "Vuelta a los módulos reales",
                        $"unidades 9xx {nuevas} (antes {nuevasReal}), catálogo {data.SquadStatsDictionary.Count} (antes {catalogoReal}), escuadras x{EjercitoReforzado.Factor:0.##} (antes x{factorReal:0.##}), " +
                        $"reglas del jugador {EjercitoReforzado.ReglasInyectadas} (antes {reglasReal}), objetos {Objetos.Numero} (antes {objetosReal})", ref ok, ref total);
                }
                catch (Exception e) { Linea(false, "Vuelta a los módulos reales", e.ToString(), ref ok, ref total); }
            }
        }

        // Unidades 900-999 en el catálogo
        // Units 900-999 in the catalog
        private static int ContarNuevas(TabletopTavernData data)
        {
            int n = 0;
            foreach (UnitName k in data.SquadStatsDictionary.Keys) if ((int)k >= 900 && (int)k <= 999) n++;
            return n;
        }

        // Comprueba en el juego un escenario ya aplicado (catálogo reconstruido)
        // Checks in the game a scenario that is already applied (catalog rebuilt)
        private static void ComprobarEscenario(TabletopTavernData data, string nombre, string[] ids, ResultadoModulos r,
                                               ref int baseArqueros, ref int baseMunicion, ref int ok, ref int total)
        {
            var cat = data.SquadStatsDictionary;
            var partes = new List<string>();
            bool bien = true;

            // Unidades: exactamente las del manifiesto simulado; la 900 solo con la facción A
            // Units: exactly those of the simulated manifest; 900 only with faction A
            var esperadas = new SortedSet<int>(r.Manifiesto.Unidades.Select(u => u.Id));
            var enCatalogo = new SortedSet<int>();
            foreach (UnitName k in cat.Keys) if ((int)k >= 900 && (int)k <= 999) enCatalogo.Add((int)k);
            bool con900 = cat.ContainsKey(UnidadGuardia.Guardia);
            bien &= enCatalogo.SetEquals(esperadas) && con900 == ids.Contains(MAstra);
            partes.Add($"unidades 9xx {enCatalogo.Count}/{esperadas.Count}{(con900 ? " (con la 900)" : " (sin la 900)")}");

            // Razas y héroes: los de los módulos de facción del escenario
            // Races and heroes: those from the scenario's faction modules
            bool r40 = data.GetRaceData((Race)40) != null, r41 = data.GetRaceData((Race)41) != null;
            bool h901 = HeroData._heroesByID.ContainsKey(901), h903 = HeroData._heroesByID.ContainsKey(903);
            bien &= r40 == ids.Contains(MAstra) && r41 == ids.Contains(MImperio) && h901 == ids.Contains(MAstra) && h903 == ids.Contains(MImperio);
            partes.Add($"RaceData 40 {r40}, 41 {r41}; héroe 901 {h901}, 903 {h903}");

            // Objetos: exactamente los de los módulos del escenario; uno ajeno sale como «None»
            // Items: exactly those from the scenario's modules; a foreign one comes out as «None»
            var obj = new SortedSet<int>(Objetos.Ids);
            var objEsp = new SortedSet<int>(r.Manifiesto.Objetos.Select(o => o.Id));
            bien &= obj.SetEquals(objEsp);
            int ajeno = new[] { 700, 710, 720 }.FirstOrDefault(x => !objEsp.Contains(x));
            string nombreAjeno = ajeno == 0 ? "-" : GearData.GetGear((GearID)ajeno).GearName;
            if (ajeno != 0) bien &= nombreAjeno == "None";
            partes.Add($"objetos [{Rango(obj)}]{(ajeno != 0 ? $", {ajeno} = {nombreAjeno}" : "")}");

            // Escuadras: PeasantBowmen con el multiplicador del escenario (la base se mide en «ninguno»)
            // Squads: PeasantBowmen with the scenario multiplier (the base is measured in «ninguno»)
            var pb = cat[UnitName.PeasantBowmen];
            if (ids.Length == 0) { baseArqueros = pb.baseUnitCount; baseMunicion = pb.Ammunition; }
            if (baseArqueros > 0)
            {
                int esp = Modulos.SoldadosEnCampo(baseArqueros, r.FactorEscuadras, r.MinimoSoldados);
                int espM = (int)(baseMunicion * (double)esp / baseArqueros + 0.5);
                bien &= pb.baseUnitCount == esp && pb.Ammunition == espM && Math.Abs(EjercitoReforzado.Factor - r.FactorEscuadras) < 1e-9;
                partes.Add($"escuadras x{r.FactorEscuadras:0.##}: PeasantBowmen {baseArqueros} -> {pb.baseUnitCount} (esperado {esp}), munición {baseMunicion} -> {pb.Ammunition} (esperada {espM})");
            }

            // Reglas del jugador: las del escenario para el héroe activo (forzado: Morvayne, 9) y ninguna para otro (Edric, 1)
            // Player rules: those of the scenario for the active hero (forced: Morvayne, 9) and none for another (Edric, 1)
            var arquero = cat[UnitName.PeasantBowmen];
            var canon = cat[UnitName.KaiserCannon];
            float alcance = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.Range, UnitName.PeasantBowmen, 9, arquero, Race.Gruntkin, 75f);
            float municion = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.Ammunition, UnitName.KaiserCannon, 9, canon, Race.Gruntkin, 40f);
            float alcanceOtro = HeroBonusRuleEvaluator.SumHeroStatBonus(UnitStat.Range, UnitName.PeasantBowmen, 1, arquero, Race.Gruntkin, 75f);
            float espAlc = r.Distancia?.Alcance ?? 0, espMun = 40f * ((r.Municion?.Multiplicador ?? 1) - 1);
            bien &= EjercitoReforzado.ReglasInyectadas == EjercitoReforzado.ReglasEsperadas && Math.Abs(alcance - espAlc) < 0.01f && Math.Abs(municion - espMun) < 0.01f
                    && Math.Abs(alcanceOtro) < 0.01f;
            partes.Add($"reglas del jugador {EjercitoReforzado.ReglasInyectadas}/{EjercitoReforzado.ReglasEsperadas}, alcance con el héroe activo (9) +{alcance} (esperado +{espAlc}), " +
                       $"munición del cañón +{municion} (esperado +{espMun}), alcance con otro héroe (Edric) +{alcanceOtro} (esperado 0)");

            // Avisos de la suma (con varias escuadras debe salir «manda el mayor»; también van al log del núcleo)
            // Sum warnings (with several squads it must show «manda el mayor»; they also go to the core log)
            int escuadras = ids.Count(id => id == MEsc50 || id == MEsc100 || id == MEsc150);
            if (escuadras > 1) bien &= r.Avisos.Any(a => a.Contains("manda el mayor"));
            foreach (string a in r.Avisos) Plugin.Aviso($"Módulos (simulación «{nombre}»): {a}");
            if (r.Avisos.Count > 0) partes.Add($"avisos: {string.Join(" | ", r.Avisos)}");

            Linea(bien, $"Simulación «{nombre}»", string.Join("; ", partes), ref ok, ref total);
        }
    }
}
