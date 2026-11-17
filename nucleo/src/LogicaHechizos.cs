// LogicaHechizos.cs
// Elección del hechizo emblema de un héroe nuevo (901-904) y validación de los hechizos propios (0.7.4), sin tipos del
// juego ni de Unity para poder probarla fuera del juego (proyecto pruebas\, PruebasDatos1111.cs y PruebasHechizos.cs). La
// usan HeroesNuevos.Montar y Hechizos.cs.
// Por qué existe (juego 1.11.1, 01-11-2026): el héroe 902 pedía ArtilleryBombardment, que sigue en el enum Spell pero ya no
// tiene asset en el registro de hechizos (TJ.Spells.SpellRegistry). El núcleo solo miraba el enum, así que lo aceptaba y la
// pantalla de héroes y la Colección escondían la carta del hechizo (ranura 1 vacía). Ahora el hechizo pedido tiene que estar
// REGISTRADO; si no lo está, se avisa en el log y se busca un sustituto seguro: primero el «plan B» del manifiesto (0.7.4),
// después el del héroe prestado y, si tampoco vale, RallyTheBanners (común, siempre disponible para cualquier héroe).
// Si el registro no se puede consultar (tipo movido en una actualización, excepción) o no reconoce ni el hechizo común, se
// considera «desconocido» y se mantiene lo de antes: vale cualquier hechizo que esté en el enum.
// 0.7.4 (decisión de Zero 01-11-2026): hechizos PROPIOS con id 1900-1999 (id del héroe + 1000). No están en el enum Spell
// (el juego acepta cualquier int: el save lo guarda como número), así que «existe» quiere decir «nombre del enum o id
// propio»; el registro dice si el núcleo lo pudo inyectar. Validar comprueba la definición del manifiesto antes de
// clonarla (rango, base registrada, maná, radio, duración, equipo y estadísticas que admite el juego).
// Autor: ZeroitMods.
// LogicaHechizos.cs
// Choosing the emblem spell of a new hero (901-904) and validating custom spells (0.7.4), with no game or Unity types
// so it can be tested outside the game (tests project\, PruebasDatos1111.cs and PruebasHechizos.cs). It is
// used by HeroesNuevos.Montar and Hechizos.cs.
// Why it exists (game 1.11.1, 01-11-2026): hero 902 asked for ArtilleryBombardment, which is still in the Spell enum but no longer
// has an asset in the spell registry (TJ.Spells.SpellRegistry). The core only looked at the enum, so it accepted it and the
// heroes screen and the Collection hid the spell card (empty slot 1). Now the requested spell has to be
// REGISTERED; if it is not, a warning is logged and a safe substitute is looked for: first the manifest's "plan B" (0.7.4),
// then the borrowed hero's and, if that is not valid either, RallyTheBanners (common, always available to any hero).
// If the registry cannot be queried (type moved in an update, exception) or does not recognize even the common spell, it is
// considered "unknown" and the previous behavior is kept: any spell that is in the enum is valid.
// 0.7.4 (Zero's decision 01-11-2026): CUSTOM spells with id 1900-1999 (hero id + 1000). They are not in the Spell enum
// (the game accepts any int: the save stores it as a number), so "exists" means "enum name or custom
// id"; the registry tells whether the core was able to inject it. Validate checks the manifest definition before
// cloning it (range, registered base, mana, radius, duration, team and stats supported by the game).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: hechizo emblema de los héroes nuevos y hechizos propios
    // Section: emblem spell of the new heroes and custom spells
    // -----------------------------------------------------------------------
    public static class LogicaHechizos
    {
        /// <summary>Qué dice el registro de hechizos del juego de un hechizo.</summary>
        /// What the game's spell registry says about a spell.
        public enum Estado { Registrado, NoRegistrado, Desconocido }

        /// <summary>Hechizo común que siempre está registrado (SpellLoadout.AlwaysAvailableSpells): último sustituto.</summary>
        /// Common spell that is always registered (SpellLoadout.AlwaysAvailableSpells): last substitute.
        public const string HechizoSeguro = "RallyTheBanners";

        /// <summary>Rango reservado para los hechizos propios (0.7.4): no choca con unidades 9xx, objetos 7xx ni héroes.</summary>
        /// Range reserved for custom spells (0.7.4): does not clash with 9xx units, 7xx gear or heroes.
        public const int PrimerIdPropio = 1900, UltimoIdPropio = 1999;

        /// <summary>Estadísticas que el juego sabe aplicar desde BonusStats (BattlefieldBonusSystem, caso SpellStatBonus).</summary>
        /// Stats the game knows how to apply from BonusStats (BattlefieldBonusSystem, SpellStatBonus case).
        public static readonly string[] StatsAdmitidas = { "MeleeAttack", "MeleeDefense", "WeaponStrength", "Speed", "Accuracy", "Range", "Armor" };

        /// <summary>Tipos de bonus único que admite un hechizo propio de aura (fase 1: ninguno o moral por segundo).</summary>
        /// Single bonus types supported by a custom aura spell (phase 1: none or morale per second).
        public static readonly string[] TiposBonusAdmitidos = { "None", "LesserMoraleSpell" };

        /// <summary>Valores de TargetTeam (enum Team del juego).</summary>
        /// TargetTeam values (the game's Team enum).
        public static readonly string[] EquiposAdmitidos = { "Player", "Enemy", "Neutral" };

        /// <summary>¿Es un id del rango de hechizos propios?</summary>
        /// Is it an id in the custom spell range?
        public static bool EsIdPropio(int id) => id >= PrimerIdPropio && id <= UltimoIdPropio;

        /// <summary>Id propio escrito como texto («1901»), o -1 si el texto no es un id del rango.</summary>
        /// Custom id written as text ("1901"), or -1 if the text is not an id in the range.
        public static int IdPropio(string texto) =>
            !string.IsNullOrWhiteSpace(texto) && int.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int id) && EsIdPropio(id) ? id : -1;

        // ---------------------------------------------------------------
        // Validación de la definición de un hechizo propio (antes de clonarlo)
        // Validation of a custom spell definition (before cloning it)
        // ---------------------------------------------------------------
        /// <summary>
        /// Comprueba la definición de un hechizo propio. <paramref name="registrado"/> dice si un nombre del enum Spell tiene
        /// asset en el registro del juego (base y efecto). Devuelve null si vale o el motivo (para el log) si no.
        /// Checks the definition of a custom spell. <paramref name="registrado"/> says whether a name from the Spell enum has an
        /// asset in the game's registry (base and effect). Returns null if valid, or the reason (for the log) if not.
        /// </summary>
        public static string Validar(Manifiesto.Hechizo h, Func<string, bool> registrado)
        {
            if (h == null) return "definición vacía";
            if (!EsIdPropio(h.Id)) return $"id {h.Id} fuera del rango de hechizos propios ({PrimerIdPropio}-{UltimoIdPropio})";
            if (string.IsNullOrWhiteSpace(h.Base)) return "no dice el hechizo base";
            bool Reg(string n)
            {
                try { return registrado != null && registrado(n); }
                catch { return false; }
            }
            if (!Reg(h.Base)) return $"el hechizo base «{h.Base}» no está registrado en el juego";
            if (!string.IsNullOrWhiteSpace(h.EfectoDe) && !Reg(h.EfectoDe)) return $"el hechizo del efecto «{h.EfectoDe}» no está registrado en el juego";
            if (string.IsNullOrWhiteSpace(h.Raza)) return "no dice la raza de la carta";
            // Desde el 03-11-2026 (decisión de Zero) la carta puede ir en la banda propia de su facción nueva (40/41): el
            // núcleo añade esas razas al orden de bandas del grimorio (Hechizos.AmpliarBandas). Otro número no vale
            // (revisión externa del 03-11, hallazgo 8: los números de serie, 0-39, siguen valiendo como antes)
            // Since 03-11-2026 (Zero's decision) the card can go in the own band of its new faction (40/41): the
            // core adds those races to the grimoire's band order (Hechizos.AmpliarBandas). Any other number is invalid
            // (external review of 03-11, finding 8: the stock numbers, 0-39, remain valid as before)
            if (int.TryParse(h.Raza.Trim(), out int n) && (n < 0 || (n >= 40 && !EsRazaBandaPropia(n)))) return $"raza de la carta {n} desconocida (de serie, 40 o 41)";
            if (h.Mana.HasValue && (h.Mana < 1 || h.Mana > 7)) return $"maná {h.Mana} fuera de 1-7";
            if (h.Enfriamiento.HasValue && (h.Enfriamiento < 1 || h.Enfriamiento > 60)) return $"enfriamiento {h.Enfriamiento} fuera de 1-60 s";
            if (h.Radio.HasValue && (h.Radio < 1 || h.Radio > 30)) return $"radio {h.Radio} fuera de 1-30";
            if (h.Duracion.HasValue && (h.Duracion < 0 || h.Duracion > 60)) return $"duración {h.Duracion} fuera de 0-60 s";
            if (h.Preparacion.HasValue && (h.Preparacion < 0 || h.Preparacion > 10)) return $"preparación {h.Preparacion} fuera de 0-10 s";
            if (h.Fuerza.HasValue && (h.Fuerza < 0 || h.Fuerza > 40)) return $"fuerza {h.Fuerza} fuera de 0-40";
            if (h.Valor.HasValue && Math.Abs(h.Valor.Value) > 500) return $"valor {h.Valor} fuera de ±500";
            if (!string.IsNullOrWhiteSpace(h.Equipo) && Array.IndexOf(EquiposAdmitidos, h.Equipo) < 0) return $"equipo «{h.Equipo}» desconocido (Player, Enemy o Neutral)";
            if (!string.IsNullOrWhiteSpace(h.TipoBonus) && Array.IndexOf(TiposBonusAdmitidos, h.TipoBonus) < 0) return $"tipo de bonus «{h.TipoBonus}» no admitido (None o LesserMoraleSpell)";
            if (h.Stats != null)
            {
                if (h.Stats.Count > 4) return $"{h.Stats.Count} estadísticas (máximo 4)";
                foreach (var s in h.Stats)
                {
                    if (s == null || Array.IndexOf(StatsAdmitidas, s.Stat) < 0) return $"estadística «{s?.Stat}» que el juego no aplica desde un hechizo";
                    if (Math.Abs(s.Valor) > 100) return $"valor {s.Valor} de {s.Stat} fuera de ±100";
                }
            }
            if (string.IsNullOrWhiteSpace(h.NombreEs) || string.IsNullOrWhiteSpace(h.NombreEn)) return "sin nombre en español o en inglés";
            return null;
        }

        // ---------------------------------------------------------------
        // Elección del hechizo emblema
        // Choosing the emblem spell
        // ---------------------------------------------------------------
        /// <summary>Elegir sin plan B (como en la 0.7.3): primero el del prestado y después RallyTheBanners.</summary>
        /// Choose without plan B (as in 0.7.3): first the borrowed hero's and then RallyTheBanners.
        public static string Elegir(int heroe, string pedido, Func<string, bool> enEnum, Func<string, Estado> registro,
                                    string prestado, out string aviso)
            => Elegir(heroe, pedido, enEnum, registro, null, prestado, out aviso);

        /// <summary>
        /// Elige el hechizo emblema de un héroe nuevo. <paramref name="enEnum"/> dice si un nombre existe (enum Spell o id
        /// propio 1900-1999); <paramref name="registro"/>, si tiene asset en el registro (o Desconocido si no se puede saber).
        /// Sustitutos, por este orden: <paramref name="planB"/> (0.7.4), <paramref name="prestado"/> y RallyTheBanners.
        /// Devuelve el nombre elegido y, en <paramref name="aviso"/>, el texto para el log cuando no se usa el pedido o no se
        /// pudo comprobar (null si todo cuadra).
        /// Chooses the emblem spell of a new hero. <paramref name="enEnum"/> says whether a name exists (Spell enum or custom
        /// id 1900-1999); <paramref name="registro"/>, whether it has an asset in the registry (or Desconocido if it cannot be known).
        /// Substitutes, in this order: <paramref name="planB"/> (0.7.4), <paramref name="prestado"/> and RallyTheBanners.
        /// Returns the chosen name and, in <paramref name="aviso"/>, the log text when the requested one is not used or could not
        /// be checked (null if everything fits).
        /// </summary>
        public static string Elegir(int heroe, string pedido, Func<string, bool> enEnum, Func<string, Estado> registro,
                                    string planB, string prestado, out string aviso)
        {
            if (enEnum == null) throw new ArgumentNullException(nameof(enEnum));
            if (registro == null) throw new ArgumentNullException(nameof(registro));
            aviso = null;
            // Estado con red: una excepción del registro cuenta como «desconocido», nunca tumba el alta del héroe
            // Safety net: a registry exception counts as "unknown" and never breaks the hero's creation
            Estado Consultar(string n)
            {
                try { return registro(n); }
                catch { return Estado.Desconocido; }
            }
            bool EnEnum(string n)
            {
                if (string.IsNullOrWhiteSpace(n)) return false;
                try { return enEnum(n); }
                catch { return false; }
            }

            // Si el registro no reconoce ni el hechizo común siempre disponible, no es fiable: se trata como desconocido
            // If the registry does not recognize even the always-available common spell, it is unreliable: treat it as unknown
            bool registroFiable = Consultar(HechizoSeguro) == Estado.Registrado;
            bool propio = IdPropio(pedido) >= 0;

            string motivo;
            if (!EnEnum(pedido))
            {
                motivo = string.IsNullOrWhiteSpace(pedido)
                    ? "no trae hechizo en el manifiesto"
                    : $"pide el hechizo «{pedido}», que no existe en el juego";
            }
            else
            {
                Estado estado = registroFiable ? Consultar(pedido) : Estado.Desconocido;
                if (estado == Estado.Registrado) return pedido;
                // Un hechizo propio sin registro fiable no se puede dar por bueno: su carta saldría vacía (no es del juego)
                // A custom spell without a reliable registry cannot be accepted: its card would come out empty (it is not from the game)
                if (estado == Estado.Desconocido && !propio)
                {
                    aviso = $"Héroe nuevo {heroe}: no se ha podido comprobar en el registro de hechizos del juego si «{pedido}» " +
                            "tiene carta; se usa igual (si en la pantalla de héroes sale sin hechizo, es esto).";
                    return pedido;
                }
                motivo = propio
                    ? $"su hechizo propio {pedido} no está registrado en el juego (no se pudo inyectar)"
                    : $"pide el hechizo «{pedido}», que el juego ya no tiene registrado (su carta saldría vacía)";
            }

            // Sustituto seguro: el plan B, el del héroe prestado y, si no vale, el común siempre disponible
            // Safe substitute: plan B, the borrowed hero's and, if that is not valid, the always-available common one
            foreach (string candidato in new[] { planB, prestado, HechizoSeguro })
            {
                if (!EnEnum(candidato) || candidato == pedido || IdPropio(candidato) >= 0) continue;
                Estado e = registroFiable ? Consultar(candidato) : Estado.Desconocido;
                if (e == Estado.NoRegistrado) continue;
                string deQuien = candidato == planB ? "su plan B" : candidato == prestado ? "el de su héroe prestado" : "el común que tiene cualquier héroe";
                aviso = $"Héroe nuevo {heroe}: {motivo}; lleva «{candidato}», {deQuien}. Revisa «Hechizo» en el manifiesto de su módulo.";
                return candidato;
            }
            aviso = $"Héroe nuevo {heroe}: {motivo} y no hay ningún sustituto registrado; lleva «{prestado}», el de su héroe prestado.";
            return prestado;
        }

        // ---------------------------------------------------------------
        // Logro «Grand Grimoire» sin hechizos propios (0.7.4)
        // "Grand Grimoire" achievement without custom spells (0.7.4)
        // ---------------------------------------------------------------
        /// <summary>
        /// ¿Ha lanzado el jugador todos los hechizos de SERIE registrados? <paramref name="registrados"/> son los ids del
        /// registro del juego (con los propios, que no cuentan) y <paramref name="lanzados"/> los de spellsEverCast. false si
        /// el registro no tiene ninguno de serie.
        /// Has the player cast all the registered SERIES spells? <paramref name="registrados"/> are the ids from the
        /// game's registry (including custom ones, which do not count) and <paramref name="lanzados"/> those from spellsEverCast. false if
        /// the registry has no series spells.
        /// </summary>
        public static bool GrimorioDeSerieCompleto(IEnumerable<int> registrados, ICollection<int> lanzados)
        {
            if (registrados == null || lanzados == null) return false;
            int deSerie = 0;
            foreach (int id in registrados)
            {
                if (EsIdPropio(id) || id <= 0) continue;
                deSerie++;
                if (!lanzados.Contains(id)) return false;
            }
            return deSerie > 0;
        }

        /// <summary>Ronda 2: la lista sin los ids propios (pendientes de «Grand Grimoire» según el juego). null si es null.</summary>
        /// Round 2: the list without the custom ids (pending for "Grand Grimoire" according to the game). null if it is null.
        public static List<int> SinPropios(IEnumerable<int> ids)
        {
            if (ids == null) return null;
            var r = new List<int>();
            foreach (int id in ids) if (!EsIdPropio(id)) r.Add(id);
            return r;
        }

        /// <summary>¿Lleva el registro algún hechizo propio? (solo entonces el núcleo concede «Grand Grimoire» por su cuenta)</summary>
        /// Does the registry contain any custom spell? (only then does the core grant "Grand Grimoire" on its own)
        public static bool HayPropios(IEnumerable<int> registrados)
        {
            if (registrados == null) return false;
            foreach (int id in registrados) if (EsIdPropio(id)) return true;
            return false;
        }

        // ---------------------------------------------------------------
        // Bandas propias del grimorio (03-11-2026, decisión de Zero): «facción A» (40) e «facción B» (41)
        // Grimoire's own bands (03-11-2026, Zero's decision): «faction A» (40) and «faction B» (41)
        // ---------------------------------------------------------------
        /// <summary>¿Es una raza nueva que puede tener banda propia en el grimorio? (40 o 41)</summary>
        /// Is it a new race that can have its own band in the grimoire? (40 or 41)
        public static bool EsRazaBandaPropia(int raza) => raza == 40 || raza == 41;

        /// <summary>
        /// Orden de bandas ampliado: el del juego tal cual y, detrás, las razas nuevas de <paramref name="razasConCarta"/>
        /// (solo 40/41, en orden y sin repetir) que aún no estén. null si no hay nada que añadir (el juego se deja igual).
        /// Extended band order: the game's as is and, after it, the new races from <paramref name="razasConCarta"/>
        /// (only 40/41, in order and without repeats) that are not yet there. null if there is nothing to add (the game is left unchanged).
        /// </summary>
        public static int[] OrdenBandasAmpliado(IReadOnlyList<int> ordenJuego, IEnumerable<int> razasConCarta)
        {
            if (ordenJuego == null || razasConCarta == null) return null;
            var nuevas = razasConCarta.Where(EsRazaBandaPropia).Distinct().OrderBy(r => r).Where(r => !ordenJuego.Contains(r)).ToList();
            if (nuevas.Count == 0) return null;
            return ordenJuego.Concat(nuevas).ToArray();
        }
    }
}
