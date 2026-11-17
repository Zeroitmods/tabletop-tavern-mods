// Manifiesto.cs
// Manifiesto de la «fábrica» de unidades nuevas (diseño §6.2). Desde la 0.7.0 (núcleo con módulos) cada módulo de
// contenido trae su parte (facciones_manifiesto.json dentro de su carpeta, lo genera
// generar_mod_facciones.py) y el núcleo las fusiona (Modulos.cs) solo con los módulos activos.
// Por unidad: id, unidad base, raza, si está activa, cómo se recluta, proyectil, caballería con jinete, sonidos,
// modelo y extras propios (prestigio y estela de la 900). Desde la 0.5.0 (F2) también las razas nuevas 40/41 (colores,
// pasiva, bonus de campaña, región, música y raza sustituta para el disco) y los héroes 901-904 (raza, emblema,
// hechizo, oro, figura prestada, retrato y ejército de muestra), ya tipados.
// Desde la 0.6.0 también los objetos nuevos (gear) 700-729: rareza, valor, respaldo para el disco, afinidad, comandante,
// icono y reglas de bonus (en el formato del cargador oficial de hero_bonus_rules.json, más «ListaCalculada»).
// 0.7.0: si un fichero falta o está roto se usa un manifiesto VACÍO (antes, uno mínimo con la 900): sin módulos de
// contenido no se registra nada. Cada entrada apunta, al fusionar, el módulo que la trae y su carpeta.
// 0.7.4: también los hechizos propios de los héroes nuevos (ids 1900-1999): clon de un hechizo registrado del juego con
// los campos que cambian (Hechizos.cs, LogicaHechizos.cs) y el «PlanB» de cada héroe (hechizo oficial si el propio falla).
// .NET puro (System.Text.Json) para poder probarlo fuera del juego (proyecto pruebas\).
// Manifiesto.cs
// Manifest of the new units "factory" (design §6.2). Since 0.7.0 (core with modules) each content
// module brings its own part (facciones_manifiesto.json inside its folder, produced by
// generar_mod_facciones.py) and the core merges them (Modulos.cs) only with the active modules.
// Per unit: id, base unit, race, whether it is active, how it is recruited, projectile, cavalry with rider, sounds,
// model and own extras (prestige and trail of the 900). Since 0.5.0 (F2) also the new races 40/41 (colors,
// passive, campaign bonus, region, music and substitute race for the disk) and heroes 901-904 (race, emblem,
// spell, gold, borrowed figure, portrait and sample army), already typed.
// Since 0.6.0 also the new gear 700-729: rarity, value, fallback for the disk, affinity, commander,
// icon and bonus rules (in the format of the official hero_bonus_rules.json loader, plus "ListaCalculada").
// 0.7.0: if a file is missing or broken, an EMPTY manifest is used (before, a minimal one with the 900): without content
// modules nothing is registered. When merging, each entry records the module that brings it and its folder.
// 0.7.4: also the own spells of the new heroes (ids 1900-1999): clone of a registered game spell with
// the fields that change (Hechizos.cs, LogicaHechizos.cs) and each hero's "PlanB" (official spell if their own fails).
// Pure .NET (System.Text.Json) so it can be tested outside the game (pruebas\ project).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: forma del manifiesto en disco y lectura
    // Section: shape of the manifest on disk and reading
    // -----------------------------------------------------------------------
    public class Manifiesto
    {
        public const string NombreFichero = "facciones_manifiesto.json";

        // Valores del campo «reclutamiento»
        // Values of the "reclutamiento" field
        public const string ReclutaPorRaza = "raza";                     // entra en la lista de su raza (jugador e IA)
                                                                         // enters the list of its race (player and AI)
        public const string ReclutaOfertasJugador = "ofertas_jugador";   // solo ofertas del jugador (la 900 en F1)
                                                                         // player offers only (the 900 in F1)

        /// <summary>Una unidad nueva tal como la describe el manifiesto.</summary>
        /// A new unit as described by the manifest.
        public class Unidad
        {
            public int Id { get; set; }
            public string NombreEs { get; set; }
            public string NombreEn { get; set; }
            public string Grupo { get; set; }             // sueltas, astra o imperio
                                                          // loose, astra or imperio
            public string Base { get; set; }              // nombre de la unidad de serie que se clona (enum UnitName)
                                                          // name of the vanilla unit being cloned (enum UnitName)
            public string Raza { get; set; }              // raza que se escribe en sus assets (enum Race del juego)
                                                          // race written into its assets (the game's Race enum)
            public string RazaFinal { get; set; }         // raza prevista para fases posteriores (40/41)
                                                          // race planned for later phases (40/41)
            public bool Activa { get; set; }              // false = solo en datos (catálogo), no se recluta
                                                          // false = data only (catalog), not recruited
            public string Reclutamiento { get; set; }     // ReclutaPorRaza u ReclutaOfertasJugador
                                                          // ReclutaPorRaza or ReclutaOfertasJugador
            public string Proyectil { get; set; }         // bala, bala_canon, flecha, flecha_fuego, hacha, virote...
            public bool Caballeria { get; set; }          // necesita el componente Cavalry (jinete sobre montura)
                                                          // needs the Cavalry component (rider on a mount)
            public string Sonidos { get; set; }           // base, laser, blaster o pesado (propios_guardia = laser); LogicaSonidos.cs
                                                          // base, laser, blaster or heavy (propios_guardia = laser); LogicaSonidos.cs
            public string Modelo { get; set; }            // base o subcarpeta del modelo propio
                                                          // base or subfolder of the own model
            public bool PrestigioPropio { get; set; }     // extra de prestigio de la Guardia (Prestigio.cs)
                                                          // Guard prestige extra (Prestigio.cs)
            public bool EstelaRoja { get; set; }          // estela roja de los disparos (Batalla.cs)
                                                          // red trail of the shots (Batalla.cs)
            public string Tipo { get; set; }
            public string Tamano { get; set; }
            public string Rareza { get; set; }
            // 0.7.0: módulo que la trae y su carpeta (los pone Modulos.Fusionar; no van en el JSON)
            // 0.7.0: module that brings it and its folder (set by Modulos.Fusionar; not in the JSON)
            [JsonIgnore] public string Modulo { get; set; }
            [JsonIgnore] public string Carpeta { get; set; }
        }

        /// <summary>Una raza (facción) nueva: 40 facción A o 41 facción B (F2).</summary>
        /// A new race (faction): 40 faction A or 41 faction B (F2).
        public class Raza
        {
            public int Id { get; set; }                     // número del enum Race que usa el juego (40, 41)
                                                            // number of the Race enum used by the game (40, 41)
            public string Clave { get; set; }               // nombre interno legible (AstraMilitarum...)
                                                            // readable internal name (AstraMilitarum...)
            public string NombreEs { get; set; }
            public string NombreEn { get; set; }
            public bool Activa { get; set; }                // false = la raza no se registra (todo como en F1)
                                                            // false = the race is not registered (everything as in F1)
            public List<string> Colores { get; set; } = new List<string>();   // primario, secundario, acento (#RRGGBB)
                                                                              // primary, secondary, accent (#RRGGBB)
            public string PasivaPrestada { get; set; }      // raza de serie cuya pasiva de batalla se presta
                                                            // vanilla race whose battle passive is borrowed
            public string BonusCampanaPrestado { get; set; }// ImperialEdict o TesoreriaDoble
                                                            // ImperialEdict or TesoreriaDoble
            public string RegionMapa { get; set; }          // región del mapa prestada (nombre de MapThemeManager)
                                                            // borrowed map region (MapThemeManager name)
            public string RazaRegion { get; set; }          // raza de serie dueña de esa región (lo que se pasa al juego)
                                                            // vanilla race that owns that region (what is passed to the game)
            public string Musica { get; set; }              // raza de serie cuya música se presta
                                                            // vanilla race whose music is borrowed
            public string DatosDe { get; set; }             // raza de serie cuyo RaceData (peana, región) se copia
                                                            // vanilla race whose RaceData (pedestal, region) is copied
            public string RazaSustituta { get; set; }       // raza de serie que se guarda en disco en su lugar
                                                            // vanilla race that is saved to disk in its place
            public int ActoCampana { get; set; }            // acto de campaña (1-3) en el que sale como enemiga
                                                            // campaign act (1-3) in which it appears as an enemy
            // 0.7.6 (prueba de Zero del 03-11-2026): actos (1-3) en los que sale como enemiga; si viene vacío, solo ActoCampana
            // 0.7.6 (Zero's test of 03-11-2026): acts (1-3) in which it appears as an enemy; if empty, only ActoCampana
            public List<int> ActosCampana { get; set; } = new List<int>();
            public int PorcentajeCiudades { get; set; }     // % de ciudades del mapa de esta raza
                                                            // % of the map's cities belonging to this race
            public List<int> Heroes { get; set; } = new List<int>();
            [JsonIgnore] public string Modulo { get; set; }     // 0.7.0: módulo que la trae
                                                                // 0.7.0: module that brings it
        }

        /// <summary>Un héroe (comandante) nuevo: 901-904 (F2).</summary>
        /// A new hero (commander): 901-904 (F2).
        public class Heroe
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int Raza { get; set; }                   // 40 o 41
            public int Emblema { get; set; }                // unidad emblema (id 9xx)
                                                            // emblem unit (id 9xx)
            public string Hechizo { get; set; }             // hechizo emblema: nombre del enum Spell o id propio (1900-1999)
                                                            // emblem spell: name from the Spell enum or its own id (1900-1999)
            // 0.7.4: hechizo oficial (enum Spell) que lleva si el propio no se puede registrar (antes que el del prestado)
            // 0.7.4: official spell (Spell enum) it carries if its own cannot be registered (takes priority over the borrowed hero's)
            public string PlanB { get; set; }
            public int OroComo { get; set; }                // héroe de serie cuyo oro inicial se copia
                                                            // stock hero whose starting gold is copied
            public int PrestadoDe { get; set; }             // héroe de serie que presta figura 3D y retrato de respaldo
                                                            // stock hero that lends the 3D figure and fallback portrait
            public string Retrato { get; set; }             // PNG del retrato, relativo a la carpeta del mod
                                                            // portrait PNG, relative to the mod folder
            // F5: modelo 3D propio (formato ZHM1, ligado al esqueleto del prefab prestado), relativo a la carpeta del mod;
            // su textura es el mismo nombre con .png. Vacío = se ve la figura prestada (ModeloHeroes.cs)
            // F5: custom 3D model (ZHM1 format, bound to the skeleton of the borrowed prefab), relative to the mod folder;
            // its texture is the same name with .png. Empty = the borrowed figure is shown (ModeloHeroes.cs)
            public string Modelo { get; set; }
            public List<int> Muestra { get; set; } = new List<int>();   // ejército inicial (el menú lo carga y lo cobra del tesoro)
                                                                        // starting army (the menu loads it and charges it to the treasury)
            public bool Activa { get; set; }
            [JsonIgnore] public string Modulo { get; set; }     // 0.7.0: módulo que lo trae (Retrato ya es ruta completa)
                                                                // 0.7.0: module that brings it (Retrato is already a full path)
        }

        /// <summary>Una regla de un objeto nuevo: un stat o un rasgo para las unidades que cumplan la condición (0.6.0).</summary>
        /// A rule for a new gear item: a stat or a trait for the units that meet the condition (0.6.0).
        public class ReglaObjeto
        {
            public string Tipo { get; set; }                // «stat» o «atributo»
                                                            // «stat» or «atributo»
            public CondicionObjeto Condicion { get; set; } = new CondicionObjeto();
            public string Stat { get; set; }                // enum UnitStat (tipo stat)
                                                            // UnitStat enum (stat type)
            public string Magnitud { get; set; }            // Flat o PercentOfCurrentValue
                                                            // Flat or PercentOfCurrentValue
            public string Valor { get; set; }               // texto con punto decimal (0.2 = +20 % si es porcentaje)
                                                            // text with a decimal point (0.2 = +20 % if it is a percentage)
            public bool Principal { get; set; }             // su valor sigue a gear_overrides.json si lo hubiera
                                                            // its value follows gear_overrides.json if present
            public string Atributo { get; set; }            // enum UnitAttribute (tipo atributo)
                                                            // UnitAttribute enum (attribute type)
        }

        /// <summary>
        /// Condición de una regla de objeto: la del cargador oficial (filterKind Unconditional, RarityTier, UnitType,
        /// UnitSize, UnitTag, UnitName) o «ListaCalculada» (el plugin la convierte en una lista de unidades del catálogo).
        /// Condition of a gear rule: the official loader's (filterKind Unconditional, RarityTier, UnitType,
        /// UnitSize, UnitTag, UnitName) or «ListaCalculada» (the plugin turns it into a list of units from the catalog).
        /// </summary>
        public class CondicionObjeto
        {
            public string FilterKind { get; set; }
            public string RequiredRarityTier { get; set; }
            public List<string> UnitTypes { get; set; }
            public List<string> UnitSizes { get; set; }
            public string Tag { get; set; }
            // 0.7.2: UnitName: unidades concretas, como el cargador del juego (nombre del enum UnitName o número, p. ej. "946")
            // 0.7.2: UnitName: specific units, like the game's loader (UnitName enum name or number, e.g. "946")
            public List<string> UnitNames { get; set; }
            // ListaCalculada: unidades que cumplen todo lo dado (Y entre campos, O dentro de cada uno)
            // ListaCalculada: units that meet everything given (AND across fields, OR within each one)
            public List<string> Rarezas { get; set; }
            public List<string> Tamanos { get; set; }
            public List<string> Tipos { get; set; }
        }

        /// <summary>Un objeto nuevo (gear) 700-729 (0.6.0).</summary>
        /// A new gear item 700-729 (0.6.0).
        public class Objeto
        {
            public int Id { get; set; }
            public string NombreEs { get; set; }
            public string NombreEn { get; set; }
            public string Rareza { get; set; }              // Common, Uncommon o Rare (enum GearRarity)
                                                            // Common, Uncommon or Rare (GearRarity enum)
            public int Valor { get; set; }                  // GearModifierValue (número principal)
                                                            // GearModifierValue (main number)
            public string Respaldo { get; set; }            // objeto de serie (enum GearID) con el que se guarda en disco
                                                            // stock gear item (GearID enum) it is saved to disk as
            public string Afinidad { get; set; }            // facción A, starwars o una raza del juego (IronLegion...)
                                                            // faction A, starwars or a game race (IronLegion...)
            public int Comandante { get; set; }             // héroe con el que sale con el doble de peso (0 = ninguno)
                                                            // hero with which it appears with double weight (0 = none)
            public string Icono { get; set; }               // PNG relativo a la carpeta del mod
                                                            // PNG relative to the mod folder
            public bool Activo { get; set; } = true;        // false = no se registra ni sale (se documenta el motivo)
                                                            // false = it is not registered and does not appear (the reason is documented)
            public List<ReglaObjeto> Reglas { get; set; } = new List<ReglaObjeto>();
            [JsonIgnore] public string Modulo { get; set; }     // 0.7.0: módulo que lo trae (Icono ya es ruta completa)
                                                                // 0.7.0: module that brings it (Icono is already a full path)
        }

        /// <summary>Un cambio de estadística de un aura propia (par de BonusStats del juego: estadística y valor).</summary>
        /// A stat change of a custom aura (the game's BonusStats pair: stat and value).
        public class StatHechizo
        {
            public string Stat { get; set; }                // enum UnitStat (MeleeDefense, Armor, Accuracy...)
                                                            // UnitStat enum (MeleeDefense, Armor, Accuracy...)
            public float Valor { get; set; }                // con decimales (la armadura va en fracción: -0.35)
                                                            // with decimals (armor is a fraction: -0.35)
        }

        /// <summary>
        /// Un hechizo propio (0.7.4): id 1900-1999, clon de un hechizo REGISTRADO del juego («Base») con los campos que
        /// cambian. Lo que va a null se queda como en la base. Lo inyecta Hechizos.cs en el registro del juego.
        /// A custom spell (0.7.4): id 1900-1999, a clone of a REGISTERED game spell («Base») with the fields that
        /// change. Whatever is null stays as in the base. It is injected by Hechizos.cs into the game's registry.
        /// </summary>
        public class Hechizo
        {
            public int Id { get; set; }                     // 1900-1999 (id del héroe + 1000)
                                                            // 1900-1999 (hero id + 1000)
            public int Heroe { get; set; }                  // héroe nuevo dueño (solo se registra si ese héroe está activo)
                                                            // new owner hero (only registered if that hero is active)
            public string Base { get; set; }                // hechizo registrado que se clona (enum Spell)
                                                            // registered spell that is cloned (Spell enum)
            public string Raza { get; set; }                // raza de la carta (enum Race de serie, o «40»/«41» con banda propia desde el 03-11-2026)
                                                            // card race (stock Race enum, or «40»/«41» with its own banner since 03-11-2026)
            public string EfectoDe { get; set; }            // hechizo registrado del que se toman efecto visual y sonidos
                                                            // registered spell from which the visual effect and sounds are taken
            public int? Mana { get; set; }                  // SpellManaCost
            public float? Enfriamiento { get; set; }        // SpellCooldown (s)
            public int? Valor { get; set; }                 // SpellModifierValue (número principal del texto: {1})
                                                            // SpellModifierValue (main number of the text: {1})
            public float? Radio { get; set; }               // SpellRadius
            public float? Preparacion { get; set; }         // SpellWarmUpDuration (s)
            public float? Duracion { get; set; }            // SpellDuration (s: {2} del texto)
                                                            // SpellDuration (s: {2} of the text)
            public float? Fuerza { get; set; }              // SpellForce (retroceso)
                                                            // SpellForce (knockback)
            public string Equipo { get; set; }              // TargetTeam: Player, Enemy o Neutral (todos)
                                                            // TargetTeam: Player, Enemy or Neutral (all)
            public bool? UnSoloGolpe { get; set; }          // IsOneOff
            public float? Sacudida { get; set; }            // ImpactShake
            public bool? IconoEstado { get; set; }          // ShowsStatusIcon
            public string TipoBonus { get; set; }           // BonusType (enum BattlefieldBonusEnum): None o LesserMoraleSpell
                                                            // BonusType (BattlefieldBonusEnum enum): None or LesserMoraleSpell
            public string StatBonus { get; set; }           // BonusUnitStat (enum UnitStat) del bonus único
                                                            // BonusUnitStat (UnitStat enum) of the single bonus
            public List<StatHechizo> Stats { get; set; }    // BonusStats (null = los de la base; vacía = ninguna)
                                                            // BonusStats (null = those of the base; empty = none)
            public string NombreEs { get; set; }
            public string NombreEn { get; set; }
            [JsonIgnore] public string Modulo { get; set; }     // módulo que lo trae (lo pone Modulos.Fusionar)
                                                                // module that brings it (set by Modulos.Fusionar)
        }

        public int Version { get; set; }
        public string Descripcion { get; set; }
        public List<Unidad> Unidades { get; set; } = new List<Unidad>();
        // F2: razas y héroes nuevos
        // F2: new races and heroes
        public List<Raza> Razas { get; set; } = new List<Raza>();
        public List<Heroe> Heroes { get; set; } = new List<Heroe>();
        // 0.6.0: objetos nuevos (gear)
        // 0.6.0: new gear
        public List<Objeto> Objetos { get; set; } = new List<Objeto>();
        // 0.7.4: hechizos propios de los héroes nuevos (ids 1900-1999)
        // 0.7.4: custom spells of the new heroes (ids 1900-1999)
        public List<Hechizo> Hechizos { get; set; } = new List<Hechizo>();

        /// <summary>De dónde salió: ruta del fichero, «módulos ...» o el motivo de que esté vacío.</summary>
        /// Where it came from: file path, «módulos ...» or the reason it is empty.
        public string Origen { get; private set; }
        internal void FijarOrigen(string origen) => Origen = origen;

        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        /// <summary>
        /// Lee el manifiesto de <paramref name="ruta"/>. Si no existe o está roto devuelve un manifiesto VACÍO y deja el
        /// motivo en <paramref name="motivo"/>. Sin <paramref name="vacioSiFalla"/>, un fichero sin unidades también se
        /// trata como fallo (manifiesto completo de antes); con él, vale una parte con solo objetos o solo razas.
        /// Reads the manifest from <paramref name="ruta"/>. If it does not exist or is broken, it returns an EMPTY manifest and leaves the
        /// reason in <paramref name="motivo"/>. Without <paramref name="vacioSiFalla"/>, a file without units is also
        /// treated as a failure (the previous full manifest); with it, a part with only gear or only races is valid.
        /// </summary>
        public static Manifiesto Leer(string ruta, out string motivo, bool vacioSiFalla = false)
        {
            motivo = null;
            try
            {
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) { motivo = "no existe " + ruta; return Vacio(motivo); }
                var m = JsonSerializer.Deserialize<Manifiesto>(File.ReadAllText(ruta), Opciones);
                if (m == null) { motivo = "vacío " + ruta; return Vacio(motivo); }
                m.Unidades ??= new List<Unidad>();
                if (!vacioSiFalla && m.Unidades.Count == 0) { motivo = "sin unidades en " + ruta; return Vacio(motivo); }
                m.Unidades.RemoveAll(u => u == null || u.Id <= 0 || string.IsNullOrEmpty(u.Base));
                // Ids repetidos: se queda la primera aparición
                // Repeated ids: the first occurrence is kept
                var vistos = new HashSet<int>();
                m.Unidades.RemoveAll(u => !vistos.Add(u.Id));
                m.Razas ??= new List<Raza>();
                m.Heroes ??= new List<Heroe>();
                m.Razas.RemoveAll(r => r == null || r.Id < 40 || r.Id > 99);
                m.Heroes.RemoveAll(h => h == null || h.Id < 900 || h.Id > 999);
                // Objetos: solo ids 700-729, sin repetir, con su lista de reglas
                // Gear: only ids 700-729, no repeats, with its list of rules
                m.Objetos ??= new List<Objeto>();
                var objetosVistos = new HashSet<int>();
                m.Objetos.RemoveAll(o => o == null || o.Id < 700 || o.Id > 729 || !objetosVistos.Add(o.Id));
                foreach (var o in m.Objetos) { o.Reglas ??= new List<ReglaObjeto>(); o.Reglas.RemoveAll(r => r == null || r.Condicion == null); }
                // 0.7.4: hechizos propios solo con id 1900-1999, sin repetir y con su hechizo base
                // 0.7.4: custom spells only with id 1900-1999, no repeats and with their base spell
                m.Hechizos ??= new List<Hechizo>();
                var hechizosVistos = new HashSet<int>();
                m.Hechizos.RemoveAll(h => h == null || !LogicaHechizos.EsIdPropio(h.Id) || string.IsNullOrWhiteSpace(h.Base) || !hechizosVistos.Add(h.Id));
                m.Origen = ruta;
                return m;
            }
            catch (Exception e)
            {
                motivo = $"ilegible ({e.GetType().Name}: {e.Message})";
                return Vacio(motivo);
            }
        }

        /// <summary>Manifiesto vacío (0.7.0): sin unidades, razas, héroes ni objetos; el juego queda de serie.</summary>
        /// Empty manifest (0.7.0): no units, races, heroes or gear; the game stays stock.
        public static Manifiesto Vacio(string origen) => new Manifiesto { Version = 0, Origen = "vacío (" + origen + ")" };
    }
}
