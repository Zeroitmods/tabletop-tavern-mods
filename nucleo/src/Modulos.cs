// Modulos.cs
// Módulos del núcleo (0.7.0). Un módulo es una carpeta de mod oficial del juego (mod.json y sus JSON) que además trae
// un fichero declarativo propio, zero_modulo.json, que el núcleo lee. El núcleo no aplica nada por sí mismo: suma lo que
// declaran los módulos ACTIVOS en el menú de mods del juego. Sin ningún módulo activo, el juego queda de serie.
// Modulos.cs
// Core modules (0.7.0). A module is an official game mod folder (mod.json and its JSONs) that also brings
// its own declarative file, zero_modulo.json, which the core reads. The core applies nothing by itself: it adds up what the
// modules ACTIVE in the game's mod menu declare. With no active module, the game stays vanilla.
//
// Qué puede declarar un módulo (zero_modulo.json):
//   - efectos.escuadras        soldados x multiplicador (con la munición escalada igual) a toda unidad de 2 o más
//                              soldados, en los dos bandos. Si hay varios activos manda el MAYOR (nunca se multiplican).
//   - efectos.municionJugador  munición x multiplicador del ejército del jugador (reglas de bonus de héroe). Si hay
//                              varios activos manda el de mayor multiplicador.
//   - efectos.distanciaJugador +alcance, +precisión y +daño a distancia del jugador a las unidades de los tipos dados. Si
//                              hay varios activos manda el mayor (suma de los tres bonus; a igualdad, el primero).
//   - efectos.moralJugador     (0.7.6) las escuadras del jugador no flaquean ni huyen por moral en las batallas en
//                              tiempo real y en la resolución automática (MoralJugador.cs); la IA, igual que siempre.
//                              Con varios activos, uno basta; «sinVacilar» implica «sinHuida».
//   - efectos.iconosEstandarte (0.7.7) iconos para el estandarte del jugador ([{id, fichero, clave, nombreEs, nombreEn}],
//                              PNG de 256x256 en la carpeta del módulo; Estandarte.cs y LogicaEstandarte.cs). Se juntan
//                              los de todos los módulos activos; el elegido se guarda en el registro del núcleo.
//   - contenido                fichero del módulo con su parte del manifiesto (unidades 9xx, razas, héroes y objetos),
//                              con las rutas (retratos, iconos, modelo) relativas a la carpeta del módulo.
//   - dependencias             otros módulos (por id) que tienen que estar activos; si falta uno, el módulo no se aplica.
//   - nucleoMinimo             versión mínima del núcleo; si el núcleo es más viejo, el módulo no se aplica.
//   - activoPorDefecto         false = la primera vez que el núcleo ve la carpeta la apunta DESACTIVADA en modlist.json.
// Este fichero es .NET puro (System.Text.Json) para poder probarlo fuera del juego (proyecto pruebas\).
// Autor: ZeroitMods.
// What a module can declare (zero_modulo.json):
//   - efectos.escuadras        soldiers x multiplier (with ammo scaled the same way) for every unit of 2 or more
//                              soldiers, on both sides. If several are active, the HIGHEST wins (they are never multiplied together).
//   - efectos.municionJugador  ammo x multiplier for the player's army (hero bonus rules). If several are
//                              active, the one with the highest multiplier wins.
//   - efectos.distanciaJugador +range, +accuracy and +ranged damage for the player's units of the given types. If
//                              several are active, the highest wins (sum of the three bonuses; on a tie, the first).
//   - efectos.moralJugador     (0.7.6) the player's squads do not waver or flee due to morale in real-time battles
//                              and in auto-resolve (MoralJugador.cs); the AI works as always.
//                              With several active, one is enough; «sinVacilar» implies «sinHuida».
//   - efectos.iconosEstandarte (0.7.7) icons for the player's banner ([{id, fichero, clave, nombreEs, nombreEn}],
//                              256x256 PNG in the module folder; Estandarte.cs and LogicaEstandarte.cs). Those from
//                              all active modules are combined; the chosen one is saved in the core registry.
//   - contenido                module file with its part of the manifest (9xx units, races, heroes and gear),
//                              with the paths (portraits, icons, model) relative to the module folder.
//   - dependencias             other modules (by id) that must be active; if one is missing, the module is not applied.
//   - nucleoMinimo             minimum core version; if the core is older, the module is not applied.
//   - activoPorDefecto         false = the first time the core sees the folder it records it as DISABLED in modlist.json.
// This file is pure .NET (System.Text.Json) so it can be tested outside the game (pruebas\ project).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: forma de zero_modulo.json
    // Section: shape of zero_modulo.json
    // -----------------------------------------------------------------------
    /// <summary>Declaración de un módulo tal como está en su zero_modulo.json.</summary>
    /// Declaration of a module as it appears in its zero_modulo.json.
    public class DeclaracionModulo
    {
        public const string NombreFichero = "zero_modulo.json";

        public int Formato { get; set; } = 1;
        public string Id { get; set; }                     // id estable (= nombre de carpeta recomendado), p. ej. ZeroEscuadras50
                                                           // stable id (= recommended folder name), e.g. ZeroEscuadras50
        public string Nombre { get; set; }                 // nombre visible (el mismo que mod.json)
                                                           // display name (the same as mod.json)
        public string Version { get; set; } = "1.0.0";
        public string NucleoMinimo { get; set; } = "0.7.0";
        public bool ActivoPorDefecto { get; set; } = true;
        public List<string> Dependencias { get; set; } = new List<string>();
        public string Contenido { get; set; }              // fichero de manifiesto del módulo (relativo a su carpeta) o null
                                                           // module manifest file (relative to its folder) or null
        public EfectosModulo Efectos { get; set; } = new EfectosModulo();
        // 0.7.12: nombre corto para el menú de mods del juego por código de idioma («es», «en», «zh-Hant»...); null = el de
        // mod.json. Los núcleos anteriores ignoran el campo (System.Text.Json no falla con propiedades desconocidas)
        // 0.7.12: short name for the game's mod menu per language code («es», «en», «zh-Hant»...); null = the mod.json
        // one. Older cores ignore the field (System.Text.Json does not fail on unknown properties)
        public Dictionary<string, string> NombreMenu { get; set; }
    }

    /// <summary>Efectos que puede declarar un módulo (cada uno null = no lo trae).</summary>
    /// Effects a module can declare (each null = it doesn't provide it).
    public class EfectosModulo
    {
        public EfectoEscuadras Escuadras { get; set; }
        public EfectoMunicion MunicionJugador { get; set; }
        public EfectoDistancia DistanciaJugador { get; set; }
        public EfectoMoral MoralJugador { get; set; }      // 0.7.6: escuadras del jugador indesmoralizables
                                                           // 0.7.6: player squads that can't be demoralized
        public List<IconoEstandarteDecl> IconosEstandarte { get; set; }   // 0.7.7: iconos del estandarte (Estandarte.cs)
                                                                          // 0.7.7: banner icons (Estandarte.cs)
    }

    /// <summary>Soldados x multiplicador a las unidades de minimoSoldados o más (redondeo int(base*m+0,5)).</summary>
    /// Soldiers x multiplier for units with minimoSoldados or more (rounding int(base*m+0.5)).
    public class EfectoEscuadras
    {
        public double Multiplicador { get; set; } = 1.0;
        public int MinimoSoldados { get; set; } = 2;
    }

    /// <summary>Munición x multiplicador del jugador para los tipos de unidad dados.</summary>
    /// Ammo x player multiplier for the given unit types.
    public class EfectoMunicion
    {
        public int Multiplicador { get; set; } = 1;
        public List<string> Tipos { get; set; } = new List<string> { "Ranged", "Hybrid", "Artillery" };
    }

    /// <summary>Bonus planos del jugador a distancia para los tipos de unidad dados.</summary>
    /// Flat player ranged bonuses for the given unit types.
    public class EfectoDistancia
    {
        public int Alcance { get; set; }
        public int Precision { get; set; }
        public int Dano { get; set; }
        public List<string> Tipos { get; set; } = new List<string> { "Ranged" };
    }

    /// <summary>
    /// 0.7.6 (módulo «Impertérritos», 03-11-2026): la moral del jugador. <see cref="SinHuida"/>: ninguna escuadra del jugador
    /// se rompe ni huye por moral; <see cref="SinVacilar"/>: además, su moral se mantiene llena (no flaquea). La retirada
    /// que ordena el propio jugador sigue funcionando.
    /// 0.7.6 (module «Impertérritos», 03-11-2026): the player's morale. <see cref="SinHuida"/>: no player squad
    /// breaks or flees due to morale; <see cref="SinVacilar"/>: additionally, their morale stays full (no wavering). The retreat
    /// ordered by the player themselves still works.
    /// </summary>
    public class EfectoMoral
    {
        public bool SinHuida { get; set; } = true;
        public bool SinVacilar { get; set; } = true;
    }

    // -----------------------------------------------------------------------
    // Sección: un módulo encontrado en disco
    // Section: a module found on disk
    // -----------------------------------------------------------------------
    /// <summary>Un módulo encontrado en la carpeta Mods del juego, con su declaración y si el juego lo tiene activo.</summary>
    /// A module found in the game's Mods folder, with its declaration and whether the game has it active.
    public class ModuloEncontrado
    {
        public DeclaracionModulo Declaracion;
        public string Carpeta;            // ruta completa de la carpeta del módulo
                                          // full path of the module folder
        public string NombreCarpeta;      // nombre de la carpeta (lo que va en modlist.json)
                                          // folder name (what goes in modlist.json)
        public bool Activo;               // activo en el orden de carga del juego (modlist.json)
                                          // active in the game's load order (modlist.json)
        public string Id => Declaracion?.Id ?? NombreCarpeta;
        public override string ToString() => $"{Id} ({NombreCarpeta}{(Activo ? ", activo" : ", desactivado")})";
    }

    // -----------------------------------------------------------------------
    // Sección: resultado de sumar los módulos activos
    // Section: result of combining the active modules
    // -----------------------------------------------------------------------
    /// <summary>Lo que aplica el núcleo en esta sesión: manifiesto fusionado y efectos.</summary>
    /// What the core applies in this session: merged manifest and effects.
    public class ResultadoModulos
    {
        public Manifiesto Manifiesto = Manifiesto.Vacio("sin módulos");
        public List<ModuloEncontrado> Aplicados = new List<ModuloEncontrado>();
        public List<string> Rechazados = new List<string>();        // «id: motivo»
                                                                    // «id: reason»
        public List<string> Avisos = new List<string>();
        // Escuadras: multiplicador ganador (1 = de serie) y de qué módulo sale
        // Squads: winning multiplier (1 = default) and which module it comes from
        public double FactorEscuadras = 1.0;
        public int MinimoSoldados = 2;
        public string ModuloEscuadras;
        // Munición del jugador (1 = sin efecto) y bonus a distancia (null = sin efecto)
        // Player ammo (1 = no effect) and ranged bonus (null = no effect)
        public EfectoMunicion Municion;
        public EfectoDistancia Distancia;
        // 0.7.6: moral del jugador (null = de serie)
        // 0.7.6: player morale (null = default)
        public EfectoMoral Moral;
        public string ModuloMoral;
        // 0.7.7: iconos del estandarte de los módulos activos (en su orden)
        // 0.7.7: banner icons from the active modules (in their order)
        public List<IconoEstandarte> IconosEstandarte = new List<IconoEstandarte>();
        public bool HayContenido => Manifiesto.Unidades.Count > 0 || Manifiesto.Objetos.Count > 0 || Manifiesto.Razas.Count > 0;
        public bool Aplicado(string id) => Aplicados.Any(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Sección: lectura, resolución y fusión
    // Section: reading, resolution and merging
    // -----------------------------------------------------------------------
    public static class Modulos
    {
        // Límites razonables de los efectos (un valor fuera se recorta y se avisa)
        // Reasonable limits for the effects (an out-of-range value is clamped and a warning is issued)
        public const double FactorMaximo = 5.0;
        public const int MunicionMaxima = 10, BonusMaximo = 100;

        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>Lee un zero_modulo.json. null y el motivo si falta o está roto.</summary>
        /// Reads a zero_modulo.json. null and the reason if it is missing or broken.
        public static DeclaracionModulo LeerDeclaracion(string ruta, out string motivo)
        {
            motivo = null;
            try
            {
                if (!File.Exists(ruta)) { motivo = "no existe " + ruta; return null; }
                var d = JsonSerializer.Deserialize<DeclaracionModulo>(File.ReadAllText(ruta), Opciones);
                if (d == null) { motivo = "vacío"; return null; }
                d.Dependencias ??= new List<string>();
                d.Efectos ??= new EfectosModulo();
                if (string.IsNullOrWhiteSpace(d.Id)) d.Id = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(ruta)));
                return d;
            }
            catch (Exception e)
            {
                motivo = $"ilegible ({e.GetType().Name}: {e.Message})";
                return null;
            }
        }

        /// <summary>
        /// Busca los módulos en la carpeta Mods (subcarpetas con mod.json y zero_modulo.json; las que empiezan por «_»
        /// las salta el juego y también el núcleo) y marca los activos según <paramref name="activas"/> (nombres de carpeta).
        /// Looks for modules in the Mods folder (subfolders with mod.json and zero_modulo.json; those starting with «_»
        /// are skipped by the game and also by the core) and marks the active ones according to <paramref name="activas"/> (folder names).
        /// </summary>
        public static List<ModuloEncontrado> Buscar(string modsRoot, ICollection<string> activas, List<string> avisos = null)
        {
            var r = new List<ModuloEncontrado>();
            if (string.IsNullOrEmpty(modsRoot) || !Directory.Exists(modsRoot)) return r;
            var set = new HashSet<string>(activas ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (string dir in Directory.GetDirectories(modsRoot).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                string nombre = Path.GetFileName(dir);
                if (!OrdenMods.EsCarpetaDeMod(nombre)) continue;
                string decl = Path.Combine(dir, DeclaracionModulo.NombreFichero);
                if (!File.Exists(decl) || !File.Exists(Path.Combine(dir, OrdenMods.ManifiestoMod))) continue;
                var d = LeerDeclaracion(decl, out string motivo);
                if (d == null) { avisos?.Add($"{nombre}: zero_modulo.json {motivo}; no se usa"); continue; }
                r.Add(new ModuloEncontrado { Declaracion = d, Carpeta = dir, NombreCarpeta = nombre, Activo = set.Contains(nombre) });
            }
            return r;
        }

        /// <summary>
        /// Suma los módulos ACTIVOS de <paramref name="modulos"/>: comprueba versión del núcleo, ids repetidos y
        /// dependencias, fusiona su contenido (rutas resueltas contra la carpeta de cada módulo) y elige los efectos.
        /// Combines the ACTIVE modules from <paramref name="modulos"/>: checks core version, duplicate ids and
        /// dependencies, merges their content (paths resolved against each module's folder) and picks the effects.
        /// </summary>
        public static ResultadoModulos Resolver(IEnumerable<ModuloEncontrado> modulos, string versionNucleo)
        {
            var res = new ResultadoModulos();
            var candidatos = new List<ModuloEncontrado>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Activos, sin repetir id y con un núcleo suficiente
            // 1) Active, without duplicate ids and with a sufficient core
            foreach (var m in modulos.Where(x => x != null && x.Activo && x.Declaracion != null))
            {
                if (!ids.Add(m.Id)) { res.Rechazados.Add($"{m.Id} ({m.NombreCarpeta}): id repetido en otra carpeta activa; se usa la primera"); continue; }
                if (CompararVersiones(versionNucleo, m.Declaracion.NucleoMinimo) < 0)
                { res.Rechazados.Add($"{m.Id}: necesita el núcleo {m.Declaracion.NucleoMinimo} o más nuevo (este es {versionNucleo})"); continue; }
                candidatos.Add(m);
            }

            // 2) Dependencias (se repite hasta que no cae ninguno más: una dependencia rechazada arrastra a quien la pide)
            // 2) Dependencies (repeated until no more drop out: a rejected dependency drags down whoever requires it)
            bool cambio = true;
            while (cambio)
            {
                cambio = false;
                var presentes = new HashSet<string>(candidatos.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
                foreach (var m in candidatos.ToList())
                {
                    var faltan = m.Declaracion.Dependencias.Where(d => !string.IsNullOrWhiteSpace(d) && !presentes.Contains(d)).ToList();
                    if (faltan.Count == 0) continue;
                    res.Rechazados.Add($"{m.Id}: le falta activo {string.Join(", ", faltan)}");
                    candidatos.Remove(m);
                    cambio = true;
                }
            }

            // 3) Contenido fusionado y efectos
            // 3) Merged content and effects
            var fusion = Manifiesto.Vacio("módulos");
            var origenes = new List<string>();
            foreach (var m in candidatos)
            {
                res.Aplicados.Add(m);
                var e = m.Declaracion.Efectos;
                if (e.Escuadras != null)
                {
                    double f = e.Escuadras.Multiplicador;
                    if (double.IsNaN(f) || f < 1.0 || f > FactorMaximo)
                    {
                        res.Avisos.Add($"{m.Id}: multiplicador de escuadras {f} fuera de 1-{FactorMaximo}; se recorta");
                        f = double.IsNaN(f) ? 1.0 : Math.Clamp(f, 1.0, FactorMaximo);
                    }
                    if (res.ModuloEscuadras != null)
                        res.Avisos.Add($"varios módulos de escuadras activos ({res.ModuloEscuadras} x{res.FactorEscuadras:0.##} y {m.Id} x{f:0.##}): manda el mayor, no se multiplican");
                    if (res.ModuloEscuadras == null || f > res.FactorEscuadras)
                    {
                        res.FactorEscuadras = f;
                        res.MinimoSoldados = Math.Max(1, e.Escuadras.MinimoSoldados);
                        res.ModuloEscuadras = m.Id;
                    }
                }
                if (e.MunicionJugador != null)
                {
                    if (res.Municion != null) res.Avisos.Add($"varios módulos de munición activos: manda {m.Id} si su multiplicador es mayor");
                    var mu = new EfectoMunicion { Multiplicador = Math.Clamp(e.MunicionJugador.Multiplicador, 1, MunicionMaxima), Tipos = Tipos(e.MunicionJugador.Tipos, "Ranged", "Hybrid", "Artillery") };
                    if (res.Municion == null || mu.Multiplicador > res.Municion.Multiplicador) res.Municion = mu;
                }
                if (e.DistanciaJugador != null)
                {
                    var di = new EfectoDistancia
                    {
                        Alcance = Math.Clamp(e.DistanciaJugador.Alcance, -BonusMaximo, BonusMaximo),
                        Precision = Math.Clamp(e.DistanciaJugador.Precision, -BonusMaximo, BonusMaximo),
                        Dano = Math.Clamp(e.DistanciaJugador.Dano, -BonusMaximo, BonusMaximo),
                        Tipos = Tipos(e.DistanciaJugador.Tipos, "Ranged"),
                    };
                    // 0.7.1: con varios, manda el MAYOR (suma de alcance, precisión y daño; a igualdad, el primero), como
                    // la munición y las escuadras; nunca se suman
                    // 0.7.1: with several, the HIGHEST wins (sum of range, accuracy and damage; on a tie, the first), like
                    // ammo and squads; they are never added together
                    if (res.Distancia != null)
                        res.Avisos.Add($"varios módulos de stats a distancia activos: manda el mayor (suma de alcance, precisión y daño), no se suman ({m.Id} +{SumaDistancia(di)} frente a +{SumaDistancia(res.Distancia)})");
                    if (res.Distancia == null || SumaDistancia(di) > SumaDistancia(res.Distancia)) res.Distancia = di;
                }
                if (e.MoralJugador != null && (e.MoralJugador.SinHuida || e.MoralJugador.SinVacilar))
                {
                    // 0.7.6: con varios, se juntan (basta con que uno lo pida); nunca hay nada que sumar
                    // 0.7.6: with several, they are combined (one asking for it is enough); there is never anything to add up
                    if (res.Moral == null) { res.Moral = new EfectoMoral { SinHuida = false, SinVacilar = false }; res.ModuloMoral = m.Id; }
                    else res.Avisos.Add($"varios módulos de moral activos ({res.ModuloMoral} y {m.Id}): se juntan, no se suman");
                    res.Moral.SinHuida |= e.MoralJugador.SinHuida;
                    res.Moral.SinVacilar |= e.MoralJugador.SinVacilar;
                    // Revisión externa (2.ª ronda, hallazgo 9): no flaquear sin poder romperse no tiene sentido; lo implica
                    // External review (round 2, finding 9): not wavering without being able to break makes no sense; it implies it
                    if (res.Moral.SinVacilar) res.Moral.SinHuida = true;
                }
                if (e.IconosEstandarte != null && e.IconosEstandarte.Count > 0)
                    res.IconosEstandarte.AddRange(LogicaEstandarte.Resolver(m.Id, m.Carpeta, e.IconosEstandarte, res.Avisos));
                if (!string.IsNullOrWhiteSpace(m.Declaracion.Contenido))
                {
                    string ruta = Path.Combine(m.Carpeta, m.Declaracion.Contenido);
                    var parte = Manifiesto.Leer(ruta, out string motivo, vacioSiFalla: true);
                    if (motivo != null) res.Avisos.Add($"{m.Id}: contenido {motivo}");
                    Fusionar(fusion, parte, m, res.Avisos);
                    origenes.Add(m.Id);
                }
            }
            if (res.Municion != null && res.Municion.Multiplicador <= 1) res.Municion = null;
            if (res.Distancia != null && res.Distancia.Alcance == 0 && res.Distancia.Precision == 0 && res.Distancia.Dano == 0) res.Distancia = null;
            fusion.FijarOrigen(origenes.Count == 0 ? "sin módulos de contenido" : "módulos " + string.Join(", ", origenes));
            res.Manifiesto = fusion;
            return res;
        }

        // Tamaño de un efecto de stats a distancia para elegir el mayor (suma de sus tres bonus)
        // Size of a ranged stats effect, used to pick the largest (sum of its three bonuses)
        private static int SumaDistancia(EfectoDistancia d) => d.Alcance + d.Precision + d.Dano;

        // Tipos de unidad de un efecto (los de por defecto si no trae ninguno)
        // Unit types of an effect (the default ones if it has none)
        private static List<string> Tipos(List<string> dados, params string[] porDefecto)
        {
            var t = (dados ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return t.Count > 0 ? t : porDefecto.ToList();
        }

        /// <summary>
        /// Añade al manifiesto fusionado la parte de un módulo: cada entrada apunta su módulo y su carpeta, y las rutas
        /// relativas (retrato, icono, modelo) pasan a ser completas. Un id repetido se queda con la primera aparición.
        /// Adds a module's part to the merged manifest: each entry records its module and its folder, and the relative
        /// paths (portrait, icon, model) become full paths. A repeated id keeps the first occurrence.
        /// </summary>
        public static void Fusionar(Manifiesto destino, Manifiesto parte, ModuloEncontrado m, List<string> avisos)
        {
            if (parte == null) return;
            string Completa(string rel) => string.IsNullOrEmpty(rel) || Path.IsPathRooted(rel) ? rel
                : Path.Combine(m.Carpeta, rel.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            foreach (var u in parte.Unidades)
            {
                if (destino.Unidades.Any(x => x.Id == u.Id)) { avisos?.Add($"{m.Id}: la unidad {u.Id} ya la trae otro módulo; se ignora"); continue; }
                u.Modulo = m.Id; u.Carpeta = m.Carpeta;
                destino.Unidades.Add(u);
            }
            foreach (var r in parte.Razas)
            {
                if (destino.Razas.Any(x => x.Id == r.Id)) { avisos?.Add($"{m.Id}: la raza {r.Id} ya la trae otro módulo; se ignora"); continue; }
                r.Modulo = m.Id;
                destino.Razas.Add(r);
            }
            foreach (var h in parte.Heroes)
            {
                if (destino.Heroes.Any(x => x.Id == h.Id)) { avisos?.Add($"{m.Id}: el héroe {h.Id} ya lo trae otro módulo; se ignora"); continue; }
                h.Modulo = m.Id; h.Retrato = Completa(h.Retrato); h.Modelo = Completa(h.Modelo);
                destino.Heroes.Add(h);
            }
            foreach (var o in parte.Objetos)
            {
                if (destino.Objetos.Any(x => x.Id == o.Id)) { avisos?.Add($"{m.Id}: el objeto {o.Id} ya lo trae otro módulo; se ignora"); continue; }
                o.Modulo = m.Id; o.Icono = Completa(o.Icono);
                destino.Objetos.Add(o);
            }
            // 0.7.4: hechizos propios de los héroes nuevos (ids 1900-1999)
            // 0.7.4: custom spells of the new heroes (ids 1900-1999)
            foreach (var h in parte.Hechizos ?? new List<Manifiesto.Hechizo>())
            {
                if (destino.Hechizos.Any(x => x.Id == h.Id)) { avisos?.Add($"{m.Id}: el hechizo {h.Id} ya lo trae otro módulo; se ignora"); continue; }
                h.Modulo = m.Id;
                destino.Hechizos.Add(h);
            }
        }

        /// <summary>Compara versiones «a.b.c» (las partes que falten cuentan como 0). Negativo si a &lt; b.</summary>
        /// Compares versions «a.b.c» (missing parts count as 0). Negative if a &lt; b.
        public static int CompararVersiones(string a, string b)
        {
            int[] Partes(string v) => (v ?? "0").Split('.', '-', '+').Take(3).Select(p => int.TryParse(p, out int n) ? n : 0)
                                                .Concat(new[] { 0, 0, 0 }).Take(3).ToArray();
            var x = Partes(a); var y = Partes(b);
            for (int i = 0; i < 3; i++) if (x[i] != y[i]) return x[i].CompareTo(y[i]);
            return 0;
        }

        /// <summary>Soldados en el campo con un multiplicador (mismo redondeo de siempre: int(base*m+0,5), umbral mínimo).</summary>
        /// Soldiers on the field with a multiplier (same rounding as always: int(base*m+0.5), minimum threshold).
        public static int SoldadosEnCampo(int baseSoldados, double factor, int minimo = 2) =>
            baseSoldados >= minimo && factor > 1.0 ? (int)(baseSoldados * factor + 0.5) : baseSoldados;

        /// <summary>
        /// Entrada de una unidad en el unit_overrides.json de un módulo (campo → texto tal cual, p. ej. «baseUnitCount» →
        /// «10»), o null si el fichero o la unidad no están. La autoprueba la usa para comparar el catálogo con lo que
        /// dice el módulo en vez de con números escritos a mano (que se quedan viejos con cada ajuste de equilibrio).
        /// Entry of a unit in a module's unit_overrides.json (field → text as is, e.g. «baseUnitCount» →
        /// «10»), or null if the file or the unit is not there. The self-test uses it to compare the catalog with what the
        /// module says instead of with hand-written numbers (which go stale with every balance tweak).
        /// </summary>
        public static Dictionary<string, string> LeerOverrideUnidad(string carpetaModulo, int id)
        {
            string ruta = string.IsNullOrEmpty(carpetaModulo) ? null : Path.Combine(carpetaModulo, "unit_overrides.json");
            if (ruta == null || !File.Exists(ruta)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
            if (!doc.RootElement.TryGetProperty("overrides", out var lista) || lista.ValueKind != JsonValueKind.Array) return null;
            string clave = id.ToString();
            foreach (var e in lista.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty("unitName", out var n) || n.ToString() != clave) continue;
                // Todos los campos como texto (el formato del juego los escribe entre comillas)
                // All fields as text (the game's format writes them in quotes)
                var r = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var p in e.EnumerateObject()) r[p.Name] = p.Value.ToString();
                return r;
            }
            return null;
        }
    }
}
