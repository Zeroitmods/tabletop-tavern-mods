// EjercitoReforzado.cs
// Efectos de los módulos de escuadras, munición y stats a distancia (0.7.0). Hasta la 0.6.1 era un efecto global fijo,
// «Ejército reforzado» (soldados x1,5 + munición x3 + stats a distancia); desde la 0.7.0 cada parte es un módulo aparte
// que se activa en el menú de mods del juego, y aquí solo se aplica lo que declaran los activos (Nucleo.cs, Modulos.cs):
// EjercitoReforzado.cs
// Effects of the squad, ammo and ranged stats modules (0.7.0). Up to 0.6.1 it was a fixed global effect,
// «Ejército reforzado» (soldiers x1.5 + ammo x3 + ranged stats); since 0.7.0 each part is a separate module
// that is enabled in the game's mod menu, and here only what the assets declare is applied (Nucleo.cs, Modulos.cs):
//
//   1) Escuadras (módulos «Escuadras +50 %», «+100 %», «+150 %»; los dos bandos): tras cada recarga del catálogo (postfix
//      de TabletopTavernData.ReloadData), toda unidad de minimoSoldados (2) o más soldados pasa a int(base * m + 0,5), y
//      su munición se escala en la misma proporción (la munición es un fondo común de la escuadra: así cada soldado
//      conserva la suya). Si hay varios módulos de escuadras activos manda el de mayor multiplicador (se avisa; nunca se
//      multiplican entre sí). Se recorre el catálogo REAL (sus claves copiadas a una lista .NET), así entran solas las
//      unidades nuevas y las que añada el estudio. El catálogo se reconstruye desde cero en cada ReloadData y el prefix
//      de ReloadData rearma el escalado: una sola vez por recarga. Las unidades de 1 soldado no cambian.
//   2) Bonus del jugador (solo el ejército del jugador; la IA no): módulo «Munición x3» (toda unidad con munición:
//      Ranged, Hybrid, Artillery) y módulo «Stats a distancia» (+10 de alcance, +10 de precisión y +3 de daño a distancia
//      a las Ranged). Son reglas de bonus de héroe SOLO para el héroe activo de la partida (0.7.1; hasta la 0.7.0 eran
//      para todos los héroes, y un señor de la guerra enemigo las recibía también, porque el juego le aplica las reglas de
//      su héroe). Si el señor de la guerra enemigo usa el mismo héroe que el jugador, mientras dure ese enfrentamiento no
//      se ponen (si no, también se las daría al enemigo), como hacen los objetos (Objetos.cs). En el menú y en las
//      batallas personalizadas no hay héroe activo: no se pone ninguna. Se añaden después de que el juego cargue las de
//      serie y las de los mods (postfix de HeroBonusManager.LoadRulesFromResourcesAndOverrides) y se vuelven a poner
//      cuando cambia el héroe o el señor de la guerra (postfix de HeroBonusManager.OnGameStateChanged y de
//      CampaignSaveManager.SaveEnemyArmy: el juego reconstruye todas las reglas y el postfix añade las que tocan). Para
//      no tocar structs con referencias desde el interop (ver AssetsGuardia.cs), las crea el PROPIO cargador del juego a
//      partir de un hero_bonus_rules.json que el plugin escribe en BepInEx\config\ZeroNucleo_reglas\, en una lista
//      temporal, y luego se añaden en bloque (AddRange) a la lista real, que es la misma que lee el ECS.
//   1) Squads (modules «Escuadras +50 %», «+100 %», «+150 %»; both sides): after each catalog reload (postfix
//      of TabletopTavernData.ReloadData), every unit with minimoSoldados (2) or more soldiers becomes int(base * m + 0.5), and
//      its ammo is scaled in the same proportion (ammo is a shared pool of the squad: this way each soldier
//      keeps their own). If several squad modules are active, the one with the highest multiplier wins (a warning is given; they are never
//      multiplied together). The REAL catalog is traversed (its keys copied to a .NET list), so new units
//      and those the studio adds are included automatically. The catalog is rebuilt from scratch on each ReloadData and the prefix
//      of ReloadData rearms the scaling: only once per reload. Units with 1 soldier do not change.
//   2) Player bonus (only the player's army; not the AI): module «Munición x3» (every unit with ammo:
//      Ranged, Hybrid, Artillery) and module «Stats a distancia» (+10 range, +10 accuracy and +3 ranged damage
//      to Ranged units). They are hero bonus rules ONLY for the run's active hero (0.7.1; up to 0.7.0 they were
//      for all heroes, and an enemy warlord received them too, because the game applies the rules of
//      its hero). If the enemy warlord uses the same hero as the player, for the duration of that encounter they are
//      not set (otherwise it would give them to the enemy too), as the gear does (Objetos.cs). In the menu and in
//      custom battles there is no active hero: none is set. They are added after the game loads the default ones
//      and those from mods (postfix of HeroBonusManager.LoadRulesFromResourcesAndOverrides) and are set again
//      when the hero or the warlord changes (postfix of HeroBonusManager.OnGameStateChanged and of
//      CampaignSaveManager.SaveEnemyArmy: the game rebuilds all the rules and the postfix adds the relevant ones). To
//      avoid touching structs with references from the interop (see AssetsGuardia.cs), the game's OWN loader creates them
//      from a hero_bonus_rules.json that the plugin writes to BepInEx\config\ZeroNucleo_reglas\, into a
//      temporary list, and then they are added in bulk (AddRange) to the real list, which is the one the ECS reads.
//
//   3) 0.7.4 (decisión de Zero 01-11-2026; revisión de datos I6): munición x3 también para las HÍBRIDAS. La guía oficial
//      dice que las híbridas no reciben ningún bonus de munición del juego (ni de prestigio, ni Deep Quivers, ni la regla
//      de Bertha, que filtra Ranged y Artillery). Para no depender de que el juego acepte nuestra regla con «Hybrid», un
//      postfix de HeroBonusManager.GetHeroStatBonus (las dos sobrecargas estáticas: la que lee la batalla al crear cada
//      escuadra y la de la ficha) añade el +200 % a una híbrida del héroe activo si el juego no lo ha puesto ya (se busca
//      por el nombre del bonus, así nunca se suma dos veces). Límite: si el juego descartara las híbridas antes de pedir
//      los bonus de héroe, esto no llega; se ve en batalla (munición de una 903, 929, 945 o 953 con y sin el módulo).
//   3) 0.7.4 (decision by Zero 01-11-2026; data review I6): ammo x3 also for HYBRIDS. The official guide
//      says hybrids receive no ammo bonus from the game (neither from prestige, nor Deep Quivers, nor Bertha's
//      rule, which filters Ranged and Artillery). So as not to depend on the game accepting our rule with «Hybrid», a
//      postfix of HeroBonusManager.GetHeroStatBonus (both static overloads: the one the battle reads when creating each
//      squad and the one for the sheet) adds the +200 % to a hybrid of the active hero if the game has not already set it (it is looked up
//      by the bonus name, so it is never added twice). Limit: if the game discarded hybrids before requesting
//      the hero bonuses, this does not apply; it can be checked in battle (ammo of a 903, 929, 945 or 953 with and without the module).
//
// Independiente de la fábrica de unidades: sus parches van en una instancia de Harmony propia
// (Plugin.AplicarParchesReforzado). Si fallan las firmas críticas de la fábrica, estos efectos siguen; solo se desactivan
// si fallan SUS propias firmas (Operativo = false).
// Independent of the unit factory: its patches go in its own Harmony instance
// (Plugin.AplicarParchesReforzado). If the factory's critical signatures fail, these effects continue; they are only disabled
// if THEIR own signatures fail (Operativo = false).
//
// Mod antiguo ZeroMunicionAlcance todavía activo (no debería: está retirado): para no escalar dos veces, se lee su tabla
// (unit_overrides.json) y su hero_bonus_rules.json, y el plugin solo escala las unidades que esa tabla no trae (las 9xx)
// y no añade reglas si ese fichero ya cubre al héroe activo. Se avisa en el log.
// Old mod ZeroMunicionAlcance still active (it shouldn't be: it is retired): to avoid scaling twice, its table
// (unit_overrides.json) and its hero_bonus_rules.json are read, and the plugin only scales the units that table does not include (the 9xx)
// and adds no rules if that file already covers the active hero. A warning is written to the log.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BepInEx;
using Il2CppInterop.Runtime;
using Memori.Localization;
using TJ;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: escalado del catálogo y reglas del jugador
    // Section: catalog scaling and player rules
    // -----------------------------------------------------------------------
    public static class EjercitoReforzado
    {
        // Efectos en vigor (los pone Configurar con los módulos aplicados; 1 / null = de serie)
        // Effects in force (set by Configurar with the applied modules; 1 / null = default)
        /// <summary>Multiplicador de soldados del módulo de escuadras que manda (1 = sin módulo de escuadras).</summary>
        /// Soldier multiplier of the winning squad module (1 = no squad module).
        public static double Factor { get; private set; } = 1.0;
        public static int MinimoSoldados { get; private set; } = 2;
        /// <summary>Munición del jugador (null = sin módulo de munición). x3 -> regla PercentOfCurrentValue de +200 %.</summary>
        /// Player ammo (null = no ammo module). x3 -> PercentOfCurrentValue rule of +200 %.
        public static EfectoMunicion Municion { get; private set; }
        /// <summary>Stats a distancia del jugador (null = sin módulo de stats).</summary>
        /// Player ranged stats (null = no stats module).
        public static EfectoDistancia Distancia { get; private set; }
        public static int MultiplicadorMunicion => Municion?.Multiplicador ?? 1;
        public static int ExtraAlcance => Distancia?.Alcance ?? 0;
        public static int ExtraPrecision => Distancia?.Precision ?? 0;
        public static int ExtraDano => Distancia?.Dano ?? 0;

        /// <summary>true si los parches propios del efecto se aplicaron (no depende de Plugin.Operativo).</summary>
        /// true if the effect's own patches were applied (does not depend on Plugin.Operativo).
        public static bool Operativo { get; internal set; }

        // Estado de la recarga actual
        // Status of the current reload
        private static bool _escalado;
        private static readonly Dictionary<int, (int soldados, int municion)> _antes = new Dictionary<int, (int, int)>();
        private static bool _avisoModViejo, _reglasSerieVolcadas;
        private static List<int> _heroesUsados = new List<int>();

        /// <summary>Unidades escaladas en la última recarga.</summary>
        /// Units scaled in the last reload.
        public static int Escaladas => _antes.Count;
        /// <summary>Unidades que el mod antiguo ya escala (y por eso el plugin no toca), en la última recarga.</summary>
        /// Units the old mod already scales (and which the plugin therefore leaves alone), in the last reload.
        public static int OmitidasPorModViejo { get; private set; }
        /// <summary>Reglas del jugador añadidas en la última carga de reglas (0 = ninguna) y las esperadas.</summary>
        /// Player rules added in the last rules load (0 = none) and the expected ones.
        public static int ReglasInyectadas { get; private set; }
        public static int ReglasEsperadas => _heroesUsados.Count * ReglasPorHeroe;
        /// <summary>Reglas por héroe con los efectos en vigor (1 de munición; hasta 3 de distancia, sin las de valor 0).</summary>
        /// Per-hero rules with the effects in force (1 for ammo; up to 3 for range, excluding those with value 0).
        public static int ReglasPorHeroe => (Municion != null ? 1 : 0) +
            (Distancia == null ? 0 : (ExtraAlcance != 0 ? 1 : 0) + (ExtraPrecision != 0 ? 1 : 0) + (ExtraDano != 0 ? 1 : 0));
        public static IReadOnlyList<int> HeroesUsados => _heroesUsados;
        /// <summary>De dónde salió el héroe de las reglas: «héroe activo», «prueba» o el motivo de no poner ninguna.</summary>
        /// Where the rules' hero came from: «héroe activo», «prueba» or the reason none was set.
        public static string OrigenHeroes { get; private set; } = "(sin cargar)";
        /// <summary>Reglas de bonus de héroe totales antes y después de añadir las nuestras (para la autoprueba).</summary>
        /// Total hero bonus rules before and after adding ours (for the self-test).
        public static int ReglasAntes { get; private set; }
        public static int ReglasDespues { get; private set; }


        /// <summary>Fija los efectos con lo que suman los módulos aplicados (Nucleo.Aplicar) y lo deja en el log.</summary>
        /// Sets the effects from what the applied modules add up to (Nucleo.Aplicar) and writes it to the log.
        public static void Configurar(ResultadoModulos r)
        {
            Factor = r?.FactorEscuadras ?? 1.0;
            MinimoSoldados = r?.MinimoSoldados ?? 2;
            Municion = r?.Municion;
            Distancia = r?.Distancia;
            Plugin.Info($"Efectos de los módulos: escuadras {(Factor > 1.0 ? $"x{Factor:0.##} desde {MinimoSoldados} soldados ({r?.ModuloEscuadras})" : "de serie")}, " +
                        $"munición del jugador {(Municion == null ? "de serie" : $"x{Municion.Multiplicador} ({string.Join("/", Municion.Tipos)})")}, " +
                        $"stats a distancia {(Distancia == null ? "de serie" : $"+{ExtraAlcance} alcance, +{ExtraPrecision} precisión, +{ExtraDano} daño ({string.Join("/", Distancia.Tipos)})")}.");
        }

        // ---------------------------------------------------------------
        // Mod antiguo: ¿activo? y qué cubre
        // Old mod: active? and what it covers
        // ---------------------------------------------------------------
        /// <summary>True si el mod antiguo ZeroMunicionAlcance sigue activo en el menú de mods (avisa una vez).</summary>
        /// True if the old ZeroMunicionAlcance mod is still active in the mods menu (warns once).
        public static bool ModViejoActivo()
        {
            bool activo = UnidadGuardia.ModsActivos().Contains(UnidadGuardia.CarpetaModViejo);
            if (activo && !_avisoModViejo)
            {
                _avisoModViejo = true;
                Plugin.Aviso("El mod antiguo «Zero - Ejército reforzado» (ZeroMunicionAlcance) sigue ACTIVO (está retirado desde la 0.4.0): " +
                             "el plugin solo escala las unidades que su tabla no trae y solo añade reglas a los héroes que no cubre. Desactívalo en el menú de mods.");
            }
            return activo;
        }

        // Ids de unidad que el mod antiguo escala (su unit_overrides.json con baseUnitCount); vacío si no está activo
        // Unit ids that the old mod scales (its unit_overrides.json with baseUnitCount); empty if it is not active
        private static HashSet<int> UnidadesDelModViejo()
        {
            var r = new HashSet<int>();
            string carpeta = UnidadGuardia.RutaModActivo(UnidadGuardia.CarpetaModViejo);
            string ruta = carpeta == null ? null : Path.Combine(carpeta, "unit_overrides.json");
            if (ruta == null || !File.Exists(ruta)) return r;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
                foreach (var e in doc.RootElement.GetProperty("overrides").EnumerateArray())
                {
                    if (!e.TryGetProperty("baseUnitCount", out _) || !e.TryGetProperty("unitName", out var n)) continue;
                    if (Enum.TryParse(n.GetString(), out UnitName u)) r.Add((int)u);
                }
            }
            catch (Exception ex) { Plugin.Aviso("Ejército reforzado: no se pudo leer la tabla del mod antiguo: " + ex.Message); }
            return r;
        }

        // Héroes que el hero_bonus_rules.json del mod antiguo cubre (sus reglas sustituyen a las de serie e incluyen las nuestras)
        // Heroes covered by the old mod's hero_bonus_rules.json (its rules replace the default ones and include ours)
        private static HashSet<int> HeroesDelModViejo()
        {
            var r = new HashSet<int>();
            string carpeta = UnidadGuardia.RutaModActivo(UnidadGuardia.CarpetaModViejo);
            string ruta = carpeta == null ? null : Path.Combine(carpeta, HeroBonusRuleOverrideLoader.FileName);
            if (ruta == null || !File.Exists(ruta)) return r;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
                foreach (var e in doc.RootElement.GetProperty("statRules").EnumerateArray())
                    if (e.TryGetProperty("heroID", out var h) && int.TryParse(h.GetString(), out int id)) r.Add(id);
            }
            catch (Exception ex) { Plugin.Aviso("Ejército reforzado: no se pudo leer las reglas del mod antiguo: " + ex.Message); }
            return r;
        }

        /// <summary>Soldados y munición de una unidad antes del escalado de esta recarga (false si no se escaló).</summary>
        /// A unit's soldiers and ammo before this reload's scaling (false if it was not scaled).
        public static bool ValoresBase(UnitName u, out int soldados, out int municion)
        {
            soldados = municion = 0;
            if (!_antes.TryGetValue((int)u, out var v)) return false;
            soldados = v.soldados;
            municion = v.municion;
            return true;
        }

        /// <summary>Soldados en el campo para una base dada con el multiplicador en vigor (mismo redondeo que el mod antiguo).</summary>
        /// Soldiers in the field for a given base with the multiplier in force (same rounding as the old mod).
        public static int EnCampo(int baseSoldados) => Modulos.SoldadosEnCampo(baseSoldados, Factor, MinimoSoldados);

        // ---------------------------------------------------------------
        // 1) Soldados x1,5 y munición escalada en el catálogo
        // 1) Soldiers x1.5 and scaled ammo in the catalog
        // ---------------------------------------------------------------
        /// <summary>Prefix de ReloadData (instancia propia): el catálogo se va a reconstruir, se puede volver a escalar.</summary>
        /// ReloadData prefix (own instance): the catalog is about to be rebuilt, so it can be scaled again.
        public static void AntesDeRecarga()
        {
            _escalado = false;
            _antes.Clear();
        }

        /// <summary>Postfix de ReloadData (instancia propia, prioridad baja: después del alta de las unidades nuevas).</summary>
        /// ReloadData postfix (own instance, low priority: after the new units are registered).
        public static void TrasRecarga(TabletopTavernData __instance)
        {
            try { EscalarCatalogo(__instance, "ReloadData"); }
            catch (Exception e) { Plugin.Error("Ejército reforzado (escalado): " + e); }
            // Sin módulos de contenido la fábrica no tiene parches: la comprobación de coherencia y la autoprueba se
            // lanzan desde aquí (con fábrica, las lanza su propio postfix, que va antes que este)
            // Without content modules the factory has no patches: the consistency check and the self-test are
            // launched from here (with a factory, its own postfix launches them, and it runs before this one)
            if (!Plugin.Operativo)
            {
                try { Nucleo.ComprobarCoherencia(); AutoPrueba.EjecutarUnaVez(__instance); }
                catch (Exception e) { Plugin.Error("Autoprueba (sin fábrica): " + e); }
            }
        }

        // Claves reales del catálogo copiadas a una lista .NET (respaldo: enum del juego + manifiesto)
        // Real catalog keys copied to a .NET list (fallback: game enum + manifest)
        private static List<int> ClavesDelCatalogo(Il2CppGeneric.Dictionary<UnitName, SquadStats> dic)
        {
            var claves = new List<int>();
            try
            {
                foreach (UnitName k in dic.Keys) claves.Add((int)k);
                return claves;
            }
            catch (Exception e)
            {
                Plugin.Aviso("Ejército reforzado: no se pudieron leer las claves del catálogo (" + e.Message + "); se recorre el enum y el manifiesto.");
                var ids = new SortedSet<int>(Enum.GetValues(typeof(UnitName)).Cast<UnitName>().Select(u => (int)u));
                foreach (var n in UnidadGuardia.Todas) ids.Add(n.Numero);
                return ids.Where(i => dic.ContainsKey((UnitName)i)).ToList();
            }
        }

        /// <summary>Escala soldados y munición de todo el catálogo (una vez por recarga).</summary>
        /// Scales soldiers and ammo for the whole catalog (once per reload).
        public static void EscalarCatalogo(TabletopTavernData data, string origen)
        {
            if (_escalado || !Operativo || Factor <= 1.0) return;
            var dic = data.SquadStatsDictionary;
            if (dic == null || dic.Count == 0) return;
            _escalado = true;
            // Con el mod antiguo activo, sus unidades ya vienen escaladas: no se tocan
            // With the old mod active, its units already come scaled: they are not touched
            var yaEscaladas = ModViejoActivo() ? UnidadesDelModViejo() : new HashSet<int>();
            OmitidasPorModViejo = 0;
            foreach (int id in ClavesDelCatalogo(dic))
            {
                var clave = (UnitName)id;
                if (!dic.ContainsKey(clave)) continue;
                if (yaEscaladas.Contains(id)) { OmitidasPorModViejo++; continue; }
                SquadStats s = dic[clave];
                int baseSoldados = s.baseUnitCount;
                if (baseSoldados < MinimoSoldados) continue;
                int nuevo = EnCampo(baseSoldados);
                int baseMunicion = s.Ammunition;
                _antes[id] = (baseSoldados, baseMunicion);
                s.baseUnitCount = nuevo;
                if (baseMunicion > 0) s.Ammunition = (int)(baseMunicion * (double)nuevo / baseSoldados + 0.5);
                dic[clave] = s;
            }
            Plugin.Info($"[{origen}] Escuadras: {_antes.Count} unidades con soldados x{Factor:0.##} y munición escalada " +
                        $"(catálogo real, de serie y nuevas; las de 1 soldado no cambian)" +
                        (OmitidasPorModViejo > 0 ? $"; {OmitidasPorModViejo} omitidas porque ya las escala el mod antiguo." : "."));
        }

        // ---------------------------------------------------------------
        // 2) Reglas de bonus del jugador
        // 2) Player bonus rules
        // ---------------------------------------------------------------
        // ---------------------------------------------------------------
        // Héroe de las reglas (0.7.1): el activo del jugador, sin señor de la guerra del mismo héroe
        // Rules' hero (0.7.1): the player's active one, without a warlord of the same hero
        // ---------------------------------------------------------------
        // Estado forzado por la autoprueba (null = el del juego): héroe y señor de la guerra
        // State forced by the self-test (null = the game's own): hero and warlord
        private static (int heroe, int senor)? _forzado;
        // Firma del estado con el que se pusieron las reglas la última vez, y si hay una recarga nuestra en curso
        // Signature of the state the rules were last set with, and whether one of our reloads is in progress
        private static string _firmaAplicada = "";
        private static bool _recargando;
        private static string _ultimoLog = "";
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        // Héroe activo según el juego (lo pone HeroBonusManager.OnGameStateChanged; -1 en el menú y en las batallas
        // personalizadas, 0 antes de la primera escena). Se lee en vivo: el juego recarga las reglas DENTRO de su
        // OnGameStateChanged, antes de que corra nuestro postfix.
        // Active hero according to the game (set by HeroBonusManager.OnGameStateChanged; -1 in the menu and in custom
        // battles, 0 before the first scene). Read live: the game reloads the rules INSIDE its
        // OnGameStateChanged, before our postfix runs.
        private static int HeroeActivo()
        {
            try
            {
                var g = HeroBonusManager.InstanceIfExists;
                if (g != null) return g.ActiveHeroID;
                // Respaldo sin gestor de bonus: si hay partida de campaña en memoria (mapa), su héroe; si no, ninguno
                // Fallback without a bonus manager: if there is a campaign run in memory (map), its hero; otherwise, none
                var datos = CampaignManager.InstanceIfExists?.CampaignSaveManager?.SaveData;
                return datos != null && !Memori.SaveData.SaveDataHandler.IsCustomBattle() ? datos.heroID : -1;
            }
            catch { return -1; }
        }

        // Señor de la guerra del ejército enemigo guardado (0 = ninguno): primero la partida en memoria, si no el save
        // Warlord of the saved enemy army (0 = none): first the run in memory, otherwise the save
        private static int SenorDeLaGuerra()
        {
            try
            {
                var datos = CampaignManager.InstanceIfExists?.CampaignSaveManager?.SaveData;
                if (datos != null) return datos.enemyWarlordHeroID;
            }
            catch { }
            try { return Memori.SaveData.SaveDataHandler.Load()?.enemyWarlordHeroID ?? 0; }
            catch { return 0; }
        }

        // Estado que toca ahora: héroe (-1 = ninguno) y si se omiten las reglas por un señor de la guerra del mismo héroe
        // State that applies now: hero (-1 = none) and whether the rules are skipped because of a warlord of the same hero
        private static (int heroe, bool omitido) EstadoReglas()
        {
            if (_forzado.HasValue)
            {
                var f = _forzado.Value;
                return f.heroe > 0 ? (f.heroe, f.senor == f.heroe) : (-1, false);
            }
            int h = HeroeActivo();
            if (h <= 0) return (-1, false);
            int s = SenorDeLaGuerra();
            return (h, s > 0 && s == h);
        }

        // Firma de un estado con los efectos en vigor (sin efectos de reglas no hace falta recargar nunca)
        // Signature of a state with the effects in force (with no rule effects there is never a need to reload)
        private static string Firma((int heroe, bool omitido) e) =>
            ReglasPorHeroe == 0 ? "sin efectos" : $"{e.heroe}|{e.omitido}|x{MultiplicadorMunicion}|{ExtraAlcance}/{ExtraPrecision}/{ExtraDano}";

        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        /// <summary>
        /// Si el héroe activo o el señor de la guerra han cambiado desde que se pusieron las reglas, las reconstruye con el
        /// propio juego (y nuestro postfix, TrasCargarReglas, añade las que tocan). Las de los objetos se rehacen en la misma
        /// recarga (su postfix va en el mismo método).
        /// If the active hero or the warlord has changed since the rules were set, rebuilds them with the
        /// game itself (and our postfix, TrasCargarReglas, adds the ones that apply). Item rules are redone in the same
        /// reload (their postfix is in the same method).
        /// </summary>
        public static void Sincronizar(string motivo)
        {
            // Sin módulos de munición ni de stats no hay reglas nuestras que poner ni quitar
            // Without ammo or stats modules there are no rules of ours to add or remove
            if (!Operativo || _recargando || ReglasPorHeroe == 0) return;
            try
            {
                if (Firma(EstadoReglas()) == _firmaAplicada) return;
                RecargarReglas();
                Plugin.TrazaUnaVez("EjercitoReforzado.Sincronizar " + motivo);
            }
            catch (Exception e) { Plugin.Error($"Ejército reforzado (sincronizar, {motivo}): {e}"); }
        }

        // Reconstruye las reglas de héroe del juego con las carpetas de mods activas al arrancar
        // Rebuilds the game's hero rules with the mod folders active at startup
        private static void RecargarReglas()
        {
            _recargando = true;
            try { HeroBonusManager.LoadRulesFromResourcesAndOverrides(Nucleo.CarpetasParaJuego()); }
            finally { _recargando = false; }
        }

        /// <summary>Postfix de HeroBonusManager.OnGameStateChanged (instancia propia): héroe de la escena nueva.</summary>
        /// Postfix of HeroBonusManager.OnGameStateChanged (own instance): hero of the new scene.
        public static void TrasOnGameStateChanged() => Sincronizar("cambio de escena");

        /// <summary>
        /// Postfix de CampaignSaveManager.SaveEnemyArmy (instancia propia): el juego acaba de decidir el señor de la guerra
        /// del enfrentamiento (también para la resolución automática, que no cambia de escena). Se rehacen las reglas del
        /// jugador y las de los objetos si hace falta.
        /// Postfix of CampaignSaveManager.SaveEnemyArmy (own instance): the game has just decided the warlord
        /// of the encounter (also for auto-resolve, which does not change scene). The player's rules and
        /// the item rules are redone if needed.
        /// </summary>
        public static void TrasSaveEnemyArmy()
        {
            Sincronizar("ejército enemigo");
            Objetos.Sincronizar("ejército enemigo");
        }

        /// <summary>Solo autoprueba: fija el héroe y el señor de la guerra de las reglas (null = el del juego) y las recarga.</summary>
        /// Self-test only: sets the rules' hero and warlord (null = the game's own) and reloads them.
        internal static void ForzarParaPrueba(int? heroe, int senor = 0)
        {
            FijarForzado(heroe, senor);
            RecargarReglas();
        }

        /// <summary>Solo autoprueba: fija el estado forzado SIN recargar (para simulaciones que ya recargan el catálogo).</summary>
        /// Self-test only: sets the forced state WITHOUT reloading (for simulations that already reload the catalog).
        internal static void FijarForzado(int? heroe, int senor = 0) =>
            _forzado = heroe.HasValue ? (heroe.Value, senor) : ((int, int)?)null;

        // JSON con las reglas en el formato oficial de hero_bonus_rules.json (el mismo que generaba el mod antiguo). Los
        // nombres que enseña la ficha («Intendencia», «Vista de águila», «Manos firmes», «Puntas templadas») van en el
        // localization_overrides.json de cada módulo con estas claves.
        // JSON with the rules in the official hero_bonus_rules.json format (the same one the old mod generated). The
        // names shown on the card («Intendencia», «Vista de águila», «Manos firmes», «Puntas templadas») go in the
        // localization_overrides.json of each module with these keys.
        private static string JsonReglas(IEnumerable<int> heroes)
        {
            string Lista(List<string> tipos) => string.Join(",", tipos.Select(x => "\"" + x + "\""));
            string Regla(int heroe, string clave, string tipos, string stat, string magnitud, int valor) =>
                "{\"heroID\":\"" + heroe + "\",\"localizationKey\":\"" + clave + "\",\"condition\":{\"filterKind\":\"UnitType\"," +
                "\"unitTypes\":[" + tipos + "]},\"stat\":\"" + stat + "\",\"magnitudeKind\":\"" + magnitud + "\",\"value\":\"" + valor + "\"}";
            var reglas = new List<string>();
            foreach (int h in heroes)
            {
                if (Municion != null)
                    reglas.Add(Regla(h, "zeroMunicionTriple", Lista(Municion.Tipos), "Ammunition", "PercentOfCurrentValue", MultiplicadorMunicion - 1));
                if (Distancia == null) continue;
                string tiradores = Lista(Distancia.Tipos);
                if (ExtraAlcance != 0) reglas.Add(Regla(h, "zeroAlcanceExtra", tiradores, "Range", "Flat", ExtraAlcance));
                if (ExtraPrecision != 0) reglas.Add(Regla(h, "zeroPrecisionExtra", tiradores, "Accuracy", "Flat", ExtraPrecision));
                if (ExtraDano != 0) reglas.Add(Regla(h, "zeroDanoDistancia", tiradores, "MissileStrength", "Flat", ExtraDano));
            }
            return "{\"statRules\":[\n" + string.Join(",\n", reglas) + "\n]}";
        }

        // Carpeta donde el plugin deja su hero_bonus_rules.json (fuera de Mods: el juego no la carga como mod)
        // Folder where the plugin leaves its hero_bonus_rules.json (outside Mods: the game does not load it as a mod)
        private static string CarpetaReglas => Path.Combine(Paths.ConfigPath, Plugin.Prefijo + "_reglas");

        /// <summary>
        /// Postfix de HeroBonusManager.LoadRulesFromResourcesAndOverrides (instancia propia): añade las reglas del jugador
        /// para el héroe activo (o ninguna: menú, batalla personalizada o señor de la guerra del mismo héroe).
        /// Postfix of HeroBonusManager.LoadRulesFromResourcesAndOverrides (own instance): adds the player's rules
        /// for the active hero (or none: menu, custom battle or warlord of the same hero).
        /// </summary>
        public static void TrasCargarReglas()
        {
            Plugin.TrazaUnaVez("TrasCargarReglas");
            ReglasInyectadas = 0;
            try
            {
                _heroesUsados = new List<int>();
                var estado = EstadoReglas();
                _firmaAplicada = Firma(estado);
                if (!Operativo || ReglasPorHeroe == 0) return;
                var reales = HeroBonusManager._statRules;
                if (reales == null) { Plugin.Aviso("Ejército reforzado: la lista de reglas de héroe aún no existe."); return; }
                VolcarReglasDeSerie(reales);

                // Héroe: el activo de la partida (sin héroe no hay reglas: la IA y las batallas personalizadas no las llevan)
                // Hero: the run's active one (without a hero there are no rules: the AI and custom battles do not have them)
                int heroe = estado.heroe;
                if (heroe <= 0) { OrigenHeroes = "sin héroe activo"; return; }
                if (estado.omitido)
                {
                    OrigenHeroes = "omitidas: señor de la guerra del mismo héroe";
                    AvisoUnaVez("senor" + heroe, $"Bonus del jugador: el señor de la guerra enemigo usa el mismo héroe ({heroe}); mientras dure ese enfrentamiento no se ponen (si no, también se los daría al enemigo).");
                    return;
                }
                // Mod antiguo activo que ya cubre a este héroe: sus reglas ya incluyen las nuestras
                // Active old mod that already covers this hero: its rules already include ours
                if (ModViejoActivo() && HeroesDelModViejo().Contains(heroe)) { OrigenHeroes = "cubierto por el mod antiguo"; return; }
                OrigenHeroes = _forzado.HasValue ? "prueba" : "héroe activo";
                var heroes = new List<int> { heroe };
                _heroesUsados = heroes;

                // Fichero de reglas (se reescribe solo si cambia)
                // Rules file (rewritten only if it changes)
                Directory.CreateDirectory(CarpetaReglas);
                string ruta = Path.Combine(CarpetaReglas, HeroBonusRuleOverrideLoader.FileName);
                string json = JsonReglas(heroes);
                if (!File.Exists(ruta) || File.ReadAllText(ruta) != json) File.WriteAllText(ruta, json, new UTF8Encoding(false));

                // El cargador del juego crea las reglas en listas temporales (sustituye por héroe, pero están vacías)
                // The game's loader creates the rules in temporary lists (replaces per hero, but they are empty)
                var temporal = new Il2CppGeneric.List<HeroStatBonusRule>();
                var atributos = new Il2CppGeneric.List<HeroAttributeBonusRule>();
                var faccion = new Il2CppGeneric.List<FactionBonusRule>();
                HeroBonusRuleOverrideLoader.ApplyOverridesFromModFolder(CarpetaReglas, temporal, atributos, faccion);
                if (temporal.Count == 0) { Plugin.Error("Ejército reforzado: el cargador del juego no creó ninguna regla del jugador."); return; }

                ReglasAntes = reales.Count;
                reales.AddRange(temporal.Cast<Il2CppGeneric.IEnumerable<HeroStatBonusRule>>());
                ReglasDespues = reales.Count;
                ReglasInyectadas = ReglasDespues - ReglasAntes;
                // En el log solo cuando cambia el estado (las reglas se reconstruyen en cada cambio de escena)
                // In the log only when the state changes (the rules are rebuilt on every scene change)
                if (_firmaAplicada != _ultimoLog)
                {
                    _ultimoLog = _firmaAplicada;
                    Plugin.Info($"Bonus del jugador: {ReglasInyectadas} reglas añadidas para el héroe {heroe} ({OrigenHeroes}). " +
                                $"Munición x{MultiplicadorMunicion}; a distancia +{ExtraAlcance} alcance, +{ExtraPrecision} precisión, " +
                                $"+{ExtraDano} daño. Reglas de héroe: {ReglasAntes} -> {ReglasDespues}.");
                }
            }
            catch (Exception e) { Plugin.Error("Ejército reforzado (reglas del jugador): " + e); }
        }

        // ---------------------------------------------------------------
        // 0.7.4: munición x3 de las híbridas (postfix de las dos sobrecargas estáticas de GetHeroStatBonus)
        // 0.7.4: x3 ammo for hybrids (postfix of the two static overloads of GetHeroStatBonus)
        // ---------------------------------------------------------------
        private const string ClaveBonusMunicion = "zeroMunicionTriple";
        private static readonly HashSet<string> _avisosHibridas = new HashSet<string>();
        /// <summary>Veces que se ha añadido el bonus de munición a una híbrida en esta sesión (para la autoprueba y el log).</summary>
        /// Number of times the ammo bonus has been added to a hybrid in this session (for the self-test and the log).
        public static int MunicionHibridasAnadida { get; private set; }
        /// <summary>
        /// Veces que el juego ya traía el bonus de munición de una híbrida (no hizo falta añadirlo). Solo cuenta la sobrecarga
        /// de 5 argumentos: la de 4 llama a la de 5, así que allí el bonus casi siempre lo acaba de poner la de 5 (nuestro).
        /// Number of times the game already provided a hybrid's ammo bonus (no need to add it). Only counts the 5-argument
        /// overload: the 4-argument one calls the 5-argument one, so there the bonus has almost always just been set by the 5-argument one (ours).
        /// </summary>
        public static int MunicionHibridasDelJuego { get; private set; }

        /// <summary>
        /// Postfix de HeroBonusManager.GetHeroStatBonus(UnitStat, UnitName, int, float, Race) (la que usa la batalla): con el
        /// módulo de munición activo y una híbrida del héroe activo del jugador, añade el bonus de munición si el juego no lo
        /// ha puesto.
        /// Postfix of HeroBonusManager.GetHeroStatBonus(UnitStat, UnitName, int, float, Race) (the one battle uses): with the
        /// ammo module active and a hybrid of the player's active hero, adds the ammo bonus if the game has not
        /// set it.
        /// </summary>
        public static void TrasGetHeroStatBonus(UnitStat __0, UnitName __1, int __2, float __3, Il2CppGeneric.List<UnitStatBonus> __result)
            => MunicionHibrida(__0, __1, __2, __3, __result, true);

        /// <summary>
        /// Postfix de la sobrecarga estática de 4 argumentos. 0.7.5 (hallazgo 31): la ficha (UnitStatUI) llama a la de
        /// INSTANCIA de 3 argumentos, que llama a esta, y esta a la de 5; normalmente la de 5 ya lo ha resuelto. Si el juego
        /// tuviera la de 5 copiada dentro de esta (inlining), lo pone aquí. No cuenta «del juego».
        /// Postfix of the 4-argument static overload. 0.7.5 (finding 31): the card (UnitStatUI) calls the 3-argument
        /// INSTANCE one, which calls this one, and this one calls the 5-argument one; normally the 5-argument one has already resolved it. If the game
        /// had the 5-argument one copied inside this one (inlining), it sets it here. It does not count as «del juego».
        /// </summary>
        public static void TrasGetHeroStatBonus4(UnitStat __0, UnitName __1, int __2, float __3, Il2CppGeneric.List<UnitStatBonus> __result)
            => MunicionHibrida(__0, __1, __2, __3, __result, false);

        // Lógica común de los dos postfix: añade a la lista del juego el +200 % de munición de una híbrida si falta
        // Logic shared by both postfixes: adds the +200 % ammo of a hybrid to the game's list if it is missing
        private static void MunicionHibrida(UnitStat __0, UnitName __1, int __2, float __3, Il2CppGeneric.List<UnitStatBonus> __result, bool contarDelJuego)
        {
            if (__0 != UnitStat.Ammunition || __result == null || !Operativo || Municion == null || MultiplicadorMunicion <= 1) return;
            try
            {
                if (!_heroesUsados.Contains(__2) || Municion.Tipos == null || !Municion.Tipos.Contains("Hybrid")) return;
                var data = TabletopTavernData.InstanceIfExists;
                if (data == null || data.GetUnitTypeFromUnitName(__1) != UnitType.Hybrid) return;
                string nombre = NombreBonusMunicion();
                if (TieneBonus(__result, nombre))
                {
                    if (contarDelJuego)
                    {
                        MunicionHibridasDelJuego++;
                        if (MunicionHibridasDelJuego == 1)
                            Plugin.Info($"Munición x{MultiplicadorMunicion} de las híbridas: el juego ya aplica la regla a {(int)__1} con el héroe {__2} (el núcleo no añade nada).");
                    }
                    return;
                }
                float valor = __3 * (MultiplicadorMunicion - 1);
                if (valor <= 0) return;
                var bonus = new UnitStatBonus(UnitStat.Ammunition, nombre, valor);
                HeroesNuevos.InvocarLista(__result.Pointer, "Add", IL2CPP.il2cpp_object_unbox(bonus.Pointer));
                MunicionHibridasAnadida++;
                if (MunicionHibridasAnadida <= 3)
                    Plugin.Info($"Munición x{MultiplicadorMunicion} de las híbridas: {(int)__1} recibe +{valor:0} de munición (de {__3:0}) con el héroe {__2}; el juego no la ponía.");
            }
            catch (Exception e)
            {
                lock (_avisosHibridas) { if (!_avisosHibridas.Add("hibridas")) return; }
                Plugin.Aviso("Munición de las híbridas (se queda la del juego): " + e.Message);
            }
        }

        // Nombre del bonus de munición tal como lo pone el juego (texto de la clave en el idioma actual)
        // Name of the ammo bonus as the game displays it (the key's text in the current language)
        private static string NombreBonusMunicion()
        {
            try { return LocalizationManager.Instance?.GetText(ClaveBonusMunicion) ?? ClaveBonusMunicion; }
            catch { return ClaveBonusMunicion; }
        }

        // ¿Trae ya la lista un bonus con ese nombre? Se lee cada elemento copiado por IL2CPP (get_Item), como VolcarReglasDeSerie
        // Does the list already contain a bonus with that name? Each element is read as a copy made by IL2CPP (get_Item), like VolcarReglasDeSerie
        private static unsafe bool TieneBonus(Il2CppGeneric.List<UnitStatBonus> lista, string nombre)
        {
            IntPtr clase = IL2CPP.il2cpp_object_get_class(lista.Pointer);
            IntPtr m = IL2CPP.il2cpp_class_get_method_from_name(clase, "get_Item", 1);
            if (m == IntPtr.Zero) throw new MissingMethodException("List<UnitStatBonus>", "get_Item");
            int indice = 0;
            void** args = stackalloc void*[1];
            args[0] = &indice;
            for (int i = 0; i < lista.Count; i++)
            {
                indice = i;
                IntPtr exc = IntPtr.Zero;
                IntPtr caja = IL2CPP.il2cpp_runtime_invoke(m, lista.Pointer, args, ref exc);
                if (exc != IntPtr.Zero || caja == IntPtr.Zero) continue;
                var b = new UnitStatBonus(caja);
                if (b.UnitStat == UnitStat.Ammunition && b.BonusName == nombre) return true;
            }
            return false;
        }

        // Escribe en el log, una vez por sesión, las reglas de héroe que hay antes de añadir las nuestras (las de serie
        // más las de otros mods). Se leen copiando cada elemento por IL2CPP (get_Item) y leyendo solo campos simples.
        // Writes to the log, once per session, the hero rules that exist before ours are added (the built-in ones
        // plus those from other mods). They are read by copying each element through IL2CPP (get_Item) and reading only simple fields.
        private static unsafe void VolcarReglasDeSerie(Il2CppGeneric.List<HeroStatBonusRule> lista)
        {
            if (_reglasSerieVolcadas) return;
            _reglasSerieVolcadas = true;
            try
            {
                IntPtr clase = IL2CPP.il2cpp_object_get_class(lista.Pointer);
                IntPtr m = IL2CPP.il2cpp_class_get_method_from_name(clase, "get_Item", 1);
                if (m == IntPtr.Zero) return;
                var lineas = new List<string>();
                int indice = 0;
                void** args = stackalloc void*[1];
                args[0] = &indice;
                for (int i = 0; i < lista.Count; i++)
                {
                    indice = i;
                    IntPtr exc = IntPtr.Zero;
                    IntPtr caja = IL2CPP.il2cpp_runtime_invoke(m, lista.Pointer, args, ref exc);
                    if (exc != IntPtr.Zero || caja == IntPtr.Zero) continue;
                    var r = new HeroStatBonusRule(caja);
                    lineas.Add($"{r.HeroID}:{r.Stat}{(r.MagnitudeKind == BonusMagnitudeKind.Flat ? "+" : " x")}{r.Value}({r.Condition.FilterKind})");
                }
                Plugin.Info($"Reglas de héroe de serie en el juego instalado ({lineas.Count}): {string.Join(" | ", lineas)}");
            }
            catch (Exception e) { Plugin.Aviso("No se pudieron volcar las reglas de héroe de serie: " + e.Message); }
        }
    }
}
