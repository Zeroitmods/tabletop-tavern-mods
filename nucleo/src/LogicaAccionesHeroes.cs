// LogicaAccionesHeroes.cs
// Lógica pura (sin tipos del juego ni de Unity) de la «acción propia» de los héroes nuevos 901-904 en la columna
// «Elige uno» de la pantalla de victoria, para poder probarla fuera del juego (proyecto pruebas\, PruebasAccionesHeroes.cs).
// El juego da esa opción por id de héroe en código (EngagementPanel.ShowChoices: 7 Purga de la plaga, 9 y 10 Levantar a
// los muertos, 9 Rituales prohibidos, 11 Momento destinado, 13 y 14 Saquear campo de batalla, 15 y 16 Consumir
// supervivientes); no hay datos ni regla oficial de mods para añadir una. AccionesHeroes.cs añade la fila con un postfix
// de ShowChoices y usa esta tabla y estas decisiones. Diseño y equilibrio: notas internas (no publicadas) («Acción
// propia tras la batalla»).
// Autor: ZeroitMods.
// LogicaAccionesHeroes.cs
// Pure logic (no game or Unity types) of the «own action» of the new heroes 901-904 in the
// «Choose one» column of the victory screen, so it can be tested outside the game (pruebas\ project, PruebasAccionesHeroes.cs).
// The game gives that option by hero id in code (EngagementPanel.ShowChoices: 7 Plague Purge, 9 and 10 Raise the
// Dead, 9 Forbidden Rituals, 11 Destined Moment, 13 and 14 Plunder Battlefield, 15 and 16 Consume
// Survivors); there is no official mod data or rule to add one. AccionesHeroes.cs adds the row with a postfix
// of ShowChoices and uses this table and these decisions. Design and balance: internal notes (unpublished) («Own
// action after the battle»).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tabla de acciones y decisiones de la fila
    // Section: action table and row decisions
    // -----------------------------------------------------------------------
    public static class LogicaAccionesHeroes
    {
        /// <summary>Prefijo con el que el juego apunta en el save la opción elegida («choice:» + clave del título).</summary>
        /// Prefix the game uses to record the chosen option in the save («choice:» + title key).
        public const string PrefijoEleccion = "choice:";

        /// <summary>Tipos de efecto: los cuatro son efectos que el juego ya aplica en sus propias opciones de héroe.</summary>
        /// Effect types: all four are effects the game already applies in its own hero options.
        public enum Tipo
        {
            CurarTodas,        // como Consumir supervivientes (ModifyTroopHealth), pero para todas las unidades
                               // like Consumir supervivientes (ModifyTroopHealth), but for all units
            Refuerzos,         // como Levantar a los muertos (RecruitSquad), con unidades de la raza del héroe
                               // like Levantar a los muertos (RecruitSquad), with units of the hero's race
            CurarUnaAlAzar,    // como Purga de la plaga (HealRandomUnitToFull)
                               // like Purga de la plaga (HealRandomUnitToFull)
            ConsumibleAlAzar,  // como Rituales prohibidos (GetRandomConsumable + AquireConsumable)
                               // like Rituales prohibidos (GetRandomConsumable + AquireConsumable)
        }

        /// <summary>Una acción de héroe: clave de texto (también la del save, no se cambia nunca) y textos de respaldo.</summary>
        /// A hero action: text key (also the save's key, never changed) and fallback texts.
        public sealed class Accion
        {
            public int Heroe;
            public Tipo Tipo;
            public string Clave;          // clave del título en localization_overrides.json y en spoilsTaken
                                          // title key in localization_overrides.json and in spoilsTaken
            public string ClaveDetalle;   // clave de la línea en cursiva (solo los tipos que no enseñan unidades ni objeto)
                                          // key of the italic line (only for types that show neither units nor an item)
            public float Cantidad;        // fracción de salud que cura (CurarTodas)
                                          // fraction of health it heals (CurarTodas)
            public int Raza;              // raza de las unidades de refuerzo (Refuerzos)
                                          // race of the reinforcement units (Refuerzos)
            public string TituloEs, TituloEn, DetalleEs, DetalleEn;   // respaldo si el módulo instalado no trae los textos
                                                                      // fallback if the installed module does not include the texts
        }

        // Clave del título y de la línea de detalle (mismo convenio que los bonus: zeroH<id>...)
        // Key of the title and of the detail line (same convention as the bonuses: zeroH<id>...)
        public static string ClaveTitulo(int heroe) => $"zeroH{heroe}Accion";
        public static string ClaveDetalle(int heroe) => $"zeroH{heroe}AccionDesc";

        /// <summary>Lo que cura «Disciplina» (héroe 901): el 25 % de la salud máxima de cada unidad.</summary>
        /// What «Disciplina» (hero 901) heals: 25% of each unit's maximum health.
        public const float CuraYarrick = 0.25f;

        // Tabla de los cuatro héroes (los textos de verdad van en el generador, TEXTOS_HEROES; estos son el respaldo)
        // Table of the four heroes (the real texts are in the generator, TEXTOS_HEROES; these are the fallback)
        private static readonly Dictionary<int, Accion> _tabla = new Dictionary<int, Accion>
        {
            [901] = new Accion
            {
                Heroe = 901, Tipo = Tipo.CurarTodas, Clave = ClaveTitulo(901), ClaveDetalle = ClaveDetalle(901), Cantidad = CuraYarrick,
                TituloEs = "Disciplina del comisariato", TituloEn = "Commissariat Discipline",
                DetalleEs = "Las unidades se curan hasta el 25 % de su salud máxima.", DetalleEn = "Units heal up to 25% of their max health",
            },
            [902] = new Accion
            {
                Heroe = 902, Tipo = Tipo.Refuerzos, Clave = ClaveTitulo(902), ClaveDetalle = null, Raza = 40,
                TituloEs = "Refuerzos de Cadia", TituloEn = "Cadian Reinforcements",
            },
            [903] = new Accion
            {
                Heroe = 903, Tipo = Tipo.CurarUnaAlAzar, Clave = ClaveTitulo(903), ClaveDetalle = ClaveDetalle(903),
                TituloEs = "Estrangulamiento de la Fuerza", TituloEn = "Force Choke",
                DetalleEs = "Ejecuta a un oficial enemigo para curar por completo a una unidad al azar.",
                DetalleEn = "Execute an enemy officer to fully heal a random unit",
            },
            [904] = new Accion
            {
                Heroe = 904, Tipo = Tipo.ConsumibleAlAzar, Clave = ClaveTitulo(904), ClaveDetalle = null,
                TituloEs = "Estudiar su arte", TituloEn = "Study Their Art",
            },
        };

        /// <summary>Héroes con acción propia (para el log y las pruebas).</summary>
        /// Heroes with their own action (for the log and the tests).
        public static IEnumerable<int> Heroes => _tabla.Keys;

        /// <summary>Acción del héroe, o null si no tiene (héroes de serie y cualquier otro id).</summary>
        /// The hero's action, or null if it has none (stock heroes and any other id).
        public static Accion De(int heroe) => _tabla.TryGetValue(heroe, out var a) ? a : null;

        // ---------------------------------------------------------------
        // Qué hacer con la fila al enseñar la pantalla (también al volver a una pantalla ya resuelta)
        // What to do with the row when showing the screen (also when returning to an already resolved screen)
        // ---------------------------------------------------------------
        public enum Fila
        {
            NoOfrecer,      // en la Marcha, sin acción o sin nada que ofrecer: la pantalla queda como la de serie
                            // on the March, with no action or nothing to offer: the screen stays like the stock one
            Ofrecer,        // fila normal, se puede elegir
                            // normal row, can be chosen
            YaElegida,      // el save dice que se eligió esta: se marca elegida, sin sonido
                            // the save says this one was chosen: it is marked as chosen, without sound
            Apartada,       // ya se eligió otra opción (save): la fila se aparta como las demás no elegidas
                            // another option was already chosen (save): the row is set aside like the other unchosen ones
        }

        /// <summary>
        /// Decide la fila. <paramref name="enMarcha"/>: la Marcha apaga todas las opciones de héroe (como el juego);
        /// <paramref name="hayQueOfrecer"/>: hay acción y su efecto tiene algo que dar (p. ej. unidades de refuerzo);
        /// <paramref name="estaElegida"/>: el save tiene «choice:» + su clave; <paramref name="otraElegida"/>: el juego ya
        /// restauró otra opción elegida (choiceMade antes de añadir la fila).
        /// Decides the row. <paramref name="enMarcha"/>: the March turns off all hero options (like the game);
        /// <paramref name="hayQueOfrecer"/>: there is an action and its effect has something to give (e.g. reinforcement units);
        /// <paramref name="estaElegida"/>: the save has «choice:» + its key; <paramref name="otraElegida"/>: the game already
        /// restored another chosen option (choiceMade before adding the row).
        /// </summary>
        public static Fila Decidir(bool enMarcha, bool hayQueOfrecer, bool estaElegida, bool otraElegida)
        {
            if (enMarcha || !hayQueOfrecer) return Fila.NoOfrecer;
            if (estaElegida) return Fila.YaElegida;
            return otraElegida ? Fila.Apartada : Fila.Ofrecer;
        }

        // ---------------------------------------------------------------
        // Refuerzos (héroe 902): rareza y número por acto, como Levantar a los muertos (3/2/1), pero uno menos en
        // los dos primeros actos: 2 comunes, 1 poco común, 1 rara
        // Reinforcements (hero 902): rarity and number per act, like Raise the Dead (3/2/1), but one fewer in
        // the first two acts: 2 common, 1 uncommon, 1 rare
        // ---------------------------------------------------------------
        public static (string rareza, int cantidad) RefuerzosPorLibro(int libro)
        {
            if (libro <= 1) return ("Common", 2);
            if (libro == 2) return ("Uncommon", 1);
            return ("Rare", 1);
        }

        /// <summary>
        /// Unidades que se pueden ofrecer como refuerzo: las activas del manifiesto de la raza pedida y la rareza pedida,
        /// por id. <paramref name="razaDe"/> da la raza efectiva de cada unidad (p. ej. la unidad 900 pasa a la
        /// 40 con «Guardia en la facción A»); -1 si no tiene.
        /// Units that can be offered as reinforcements: the active ones in the manifest for the requested race and the requested rarity,
        /// by id. <paramref name="razaDe"/> gives the effective race of each unit (e.g. unit 900 becomes
        /// 40 with «Guardia en la facción A»); -1 if it has none.
        /// </summary>
        public static List<int> Candidatas(IEnumerable<Manifiesto.Unidad> unidades, int raza, string rareza, Func<Manifiesto.Unidad, int> razaDe)
        {
            if (unidades == null || razaDe == null) return new List<int>();
            return unidades.Where(u => u != null && u.Activa && string.Equals(u.Rareza, rareza, StringComparison.OrdinalIgnoreCase) && razaDe(u) == raza)
                           .Select(u => u.Id).Distinct().OrderBy(id => id).ToList();
        }

        /// <summary>Raza de una unidad del manifiesto como número (enum Race del juego); -1 si no es un número ni un nombre conocido.</summary>
        /// Race of a manifest unit as a number (the game's Race enum); -1 if it is neither a number nor a known name.
        public static int RazaNumerica(string raza, Func<string, int> nombreARaza)
        {
            if (string.IsNullOrEmpty(raza)) return -1;
            if (int.TryParse(raza, out int n)) return n;
            return nombreARaza == null ? -1 : nombreARaza(raza);
        }

        /// <summary>
        /// Elige <paramref name="cantidad"/> unidades al azar (con repetición, como Levantar a los muertos) con
        /// <paramref name="siguiente"/>(n) = entero en [0, n). Lista vacía si no hay candidatas.
        /// Picks <paramref name="cantidad"/> random units (with repetition, like Raise Dead) using
        /// <paramref name="siguiente"/>(n) = integer in [0, n). Empty list if there are no candidates.
        /// </summary>
        public static List<int> Elegir(IReadOnlyList<int> candidatas, int cantidad, Func<int, int> siguiente)
        {
            var r = new List<int>();
            if (candidatas == null || candidatas.Count == 0 || siguiente == null) return r;
            for (int i = 0; i < cantidad; i++)
            {
                int k = siguiente(candidatas.Count);
                if (k < 0 || k >= candidatas.Count) k = 0;    // un azar fuera de rango no rompe la fila
                                                              // an out-of-range random value does not break the row
                r.Add(candidatas[k]);
            }
            return r;
        }

        // ---------------------------------------------------------------
        // Textos: el del juego si existe; si no (módulo instalado de antes), el de respaldo en el idioma del juego
        // Texts: the game's own if it exists; otherwise (module installed earlier), the fallback in the game's language
        // ---------------------------------------------------------------
        /// <summary>¿El texto que devolvió el juego vale? (no vacío y distinto de la propia clave, que es lo que da si falta)</summary>
        /// Is the text the game returned valid? (not empty and different from the key itself, which is what it returns when missing)
        public static bool TextoValido(string texto, string clave) => !string.IsNullOrWhiteSpace(texto) && texto.Trim() != clave;

        /// <summary>Texto del juego o respaldo (español si <paramref name="espanol"/>, inglés si no).</summary>
        /// Game text or fallback (Spanish if <paramref name="espanol"/>, English otherwise).
        public static string Texto(string delJuego, string clave, string respaldoEs, string respaldoEn, bool espanol)
            => TextoValido(delJuego, clave) ? delJuego : (espanol ? respaldoEs : respaldoEn) ?? clave;

        // ---------------------------------------------------------------
        // Idioma del juego (03-11-2026, revisión de idiomas, hallazgo 6): decide el idioma de los textos de respaldo
        // Game language (03-11-2026, language review, finding 6): decides the language of the fallback texts
        // ---------------------------------------------------------------
        /// <summary>Texto de serie de la clave «Ransom Captives» en español (tabla del juego 1.11.1). Solo se usa si no hay código de idioma.</summary>
        /// Default text for the key «Ransom Captives» in Spanish (game table 1.11.1). Only used if there is no language code.
        public const string RescateEnEspanol = "Cobrar rescate de prisioneros";

        /// <summary>
        /// ¿El juego está en español? Primero el código de idioma activo (el que el juego pasa a
        /// LocalizationOverrides.SetActiveLocale: «es», «en», «fr»…); si no lo hay todavía (null o vacío), el texto de serie
        /// de «Ransom Captives», como hasta la 0.7.5.
        /// Is the game in Spanish? First the active language code (the one the game passes to
        /// LocalizationOverrides.SetActiveLocale: «es», «en», «fr»…); if there is none yet (null or empty), the default text
        /// of «Ransom Captives», as up to 0.7.5.
        /// </summary>
        public static bool EsEspanol(string codigoIdioma, string textoRescate)
        {
            if (!string.IsNullOrWhiteSpace(codigoIdioma))
                return codigoIdioma.Trim().StartsWith("es", StringComparison.OrdinalIgnoreCase);
            return textoRescate == RescateEnEspanol;
        }
    }
}
