// AccionesHeroes.cs
// Acción propia de los héroes nuevos (901, 902, 903, 904) en la columna «Elige uno» de la
// pantalla de victoria de una batalla de campaña, como las de los héroes de serie (Purga de la plaga, Levantar a los
// muertos, Rituales prohibidos, Momento destinado, Saquear campo de batalla, Consumir supervivientes).
// El juego decide esas opciones por id de héroe dentro de EngagementPanel.ShowChoices (cadena de «if heroID == ...»
// comprobada en el binario instalado: GameAssembly.dll, ShowChoices 0x184086530, cmp 0xA/3/4/0xF/0x10/7/9/0xB/0xD/0xE);
// no hay datos de héroe ni regla oficial de mods que añada una. ShowChoices NO está inlineado: tiene una sola llamada
// real, desde ShowRewards (0x184087864), y los métodos que se usan aquí (AddChoice, Choose, QueueAutoContinue, WasTaken)
// también se llaman de verdad (escáner xref del 18-10-2026). Un postfix de ShowChoices añade la fila al final, después
// de las de serie, con el mismo AddChoice del juego, así que el juego la trata como suya: cuenta como opción ofrecida,
// Choose la apunta en el save («choice:» + clave) y QueueAutoContinue cierra la pantalla cuando toca.
// Los efectos son los que el juego ya aplica en sus propias opciones (curar, reclutar, consumible). Tabla, decisiones y
// textos de respaldo: LogicaAccionesHeroes.cs. Diseño: notas internas (no publicadas) («Acción propia tras la batalla»).
// Todo falla en blando: si algo no está como se espera, se escribe una vez en el log y la pantalla queda como la de serie
// (si falla al aplicar el efecto, la opción no se marca elegida y se puede elegir otra).
// Autor: ZeroitMods.
// AccionesHeroes.cs
// Custom action for the new heroes (901, 902, 903, 904) in the «Elige uno» column of the
// victory screen of a campaign battle, like those of the vanilla heroes (Purga de la plaga, Levantar a los
// muertos, Rituales prohibidos, Momento destinado, Saquear campo de batalla, Consumir supervivientes).
// The game decides those options by hero id inside EngagementPanel.ShowChoices (chain of «if heroID == ...»
// checked in the installed binary: GameAssembly.dll, ShowChoices 0x184086530, cmp 0xA/3/4/0xF/0x10/7/9/0xB/0xD/0xE);
// there is no hero data or official mod rule that adds one. ShowChoices is NOT inlined: it has a single real
// call, from ShowRewards (0x184087864), and the methods used here (AddChoice, Choose, QueueAutoContinue, WasTaken)
// are also really called (xref scanner of 18-10-2026). A ShowChoices postfix adds the row at the end, after
// the vanilla ones, with the game's own AddChoice, so the game treats it as its own: it counts as an offered option,
// Choose records it in the save («choice:» + key) and QueueAutoContinue closes the screen when due.
// The effects are those the game already applies in its own options (heal, recruit, consumable). Table, decisions and
// fallback texts: LogicaAccionesHeroes.cs. Design: internal notes (unpublished) («Acción propia tras la batalla»).
// Everything fails softly: if something is not as expected, it is logged once and the screen stays like the vanilla one
// (if applying the effect fails, the option is not marked as chosen and another can be picked).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Memori.Localization;
using TJ;
using TJ.Engagement;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: postfix de la columna «Elige uno» y efectos de cada acción
    // Section: postfix of the «Elige uno» column and effects of each action
    // -----------------------------------------------------------------------
    public static class AccionesHeroes
    {
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        // Aviso una sola vez por clave y sesión (la pantalla sale tras cada batalla)
        // Warn only once per key and session (the screen appears after each battle)
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
        /// Lista de parches para Plugin.cs: (nombre, tipo, método, argumentos, prefix, postfix, finalizer). Van en la
        /// instancia de Harmony de los héroes en campaña (CampanaHeroes), que se aplica con cualquier módulo instalado; el
        /// postfix solo actúa con un héroe nuevo de un módulo activo (FaccionesNuevas.EsHeroeNuevo).
        /// List of patches for Plugin.cs: (name, type, method, arguments, prefix, postfix, finalizer). They go in the
        /// Harmony instance of the heroes in campaign (CampanaHeroes), which is applied with any module installed; the
        /// postfix only acts with a new hero from an active module (FaccionesNuevas.EsHeroeNuevo).
        /// </summary>
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer)> Parches()
        {
            return new List<(string, Func<Type>, string, Func<Type[]>, string, string, string)>
            {
                // nameof comprueba al compilar que el método existe en el interop del juego instalado
                // nameof checks at compile time that the method exists in the installed game's interop
                ("Héroes nuevos: acción propia tras la batalla (EngagementPanel.ShowChoices)", () => typeof(EngagementPanel), nameof(EngagementPanel.ShowChoices), () => Type.EmptyTypes,
                    null, nameof(TrasShowChoices), null),
            };
        }

        // ---------------------------------------------------------------
        // Textos
        // Texts
        // ---------------------------------------------------------------
        // Texto del juego (LocalizationManager.GetText, el mismo que usa el panel); null si falla
        // Game text (LocalizationManager.GetText, the same one the panel uses); null if it fails
        private static string TextoJuego(string clave)
        {
            try { return LocalizationManager.Instance?.GetText(clave); } catch { return null; }
        }

        // ¿El juego está en español? Solo decide el idioma de los textos de respaldo (aquí y en el aviso del bloqueo en línea,
        // Saves.TextoAvisoOnline). 03-11-2026 (revisión de idiomas, hallazgo 6): se mira el código de idioma activo
        // (LocalizationOverrides._activeLocale, el que el juego pasa a SetActiveLocale al elegir idioma; VA 0x183204FA0 lo
        // guarda siempre, tenga o no textos de mods ese idioma). Si aún no lo hay, el texto de serie de «Ransom Captives»
        // Is the game in Spanish? It only decides the language of the fallback texts (here and in the online lock notice,
        // Saves.TextoAvisoOnline). 03-11-2026 (language review, finding 6): the active language code is checked
        // (LocalizationOverrides._activeLocale, the one the game passes to SetActiveLocale when choosing a language; VA 0x183204FA0 always
        // stores it, whether or not that language has mod texts). If there is none yet, the stock text of «Ransom Captives»
        internal static bool JuegoEnEspanol()
        {
            string codigo = null;
            try { codigo = LeerCodigoIdioma(); } catch { }
            return LogicaAccionesHeroes.EsEspanol(codigo, string.IsNullOrWhiteSpace(codigo) ? TextoJuego("Ransom Captives") : null);
        }

        // Código de idioma activo del juego (LocalizationOverrides._activeLocale). 03-11-2026 (revisión del 03-11, hallazgo
        // 3): aparte y sin inlining, como FallarColaRapida y AvisarEnPantalla. Si una actualización renombra o quita el
        // campo, el fallo de JIT (MissingFieldException/MissingMethodException) sale al llamar a este método, dentro del
        // try de JuegoEnEspanol, y no al compilar JuegoEnEspanol (que lo lanzaría a quien lo llama)
        // Active language code of the game (LocalizationOverrides._activeLocale). 03-11-2026 (review of 03-11, finding
        // 3): separate and without inlining, like FallarColaRapida and AvisarEnPantalla. If an update renames or removes the
        // field, the JIT failure (MissingFieldException/MissingMethodException) is raised when calling this method, inside the
        // try of JuegoEnEspanol, and not when compiling JuegoEnEspanol (which would throw it to its caller)
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static string LeerCodigoIdioma() => LocalizationOverrides._activeLocale;

        /// <summary>Código de idioma activo del juego (p. ej. «es», «zh-Hant») o null si aún no hay; nunca lanza.</summary>
        /// Active language code of the game (e.g. «es», «zh-Hant») or null if there is none yet; never throws.
        internal static string CodigoIdioma()
        {
            try { return LeerCodigoIdioma(); } catch { return null; }
        }

        // Texto de una clave de la acción con su respaldo si el módulo instalado no lo trae
        // Text of an action key with its fallback if the installed module does not provide it
        private static string Texto(string clave, string respaldoEs, string respaldoEn)
        {
            string t = TextoJuego(clave);
            if (!LogicaAccionesHeroes.TextoValido(t, clave))
                InfoUnaVez("texto" + clave, $"Acción de héroe: el módulo instalado no trae el texto «{clave}» (¿generado antes de las acciones?); se usa el de respaldo.");
            return LogicaAccionesHeroes.Texto(t, clave, respaldoEs, respaldoEn, JuegoEnEspanol());
        }

        // ---------------------------------------------------------------
        // Postfix de ShowChoices
        // Postfix of ShowChoices
        // ---------------------------------------------------------------
        /// <summary>
        /// Postfix de EngagementPanel.ShowChoices: con un héroe nuevo activo (fuera de la Marcha), añade su acción al final
        /// de «Elige uno» y deja la fila como la dejaría el juego al volver a la pantalla (elegida, apartada o libre).
        /// Nunca lanza.
        /// Postfix of EngagementPanel.ShowChoices: with an active new hero (outside the March), it adds its action at the end
        /// of «Elige uno» and leaves the row as the game would when returning to the screen (chosen, set aside or free).
        /// Never throws.
        /// </summary>
        public static void TrasShowChoices(EngagementPanel __instance)
        {
            try
            {
                if (__instance == null) return;
                var gestor = __instance.campaignSaveManager;
                var save = gestor?.SaveData;
                var vista = __instance.view;
                if (save == null || vista == null) return;
                var bonos = HeroBonusManager.InstanceIfExists;
                if (bonos == null) return;
                int heroe = bonos.ActiveHeroID;
                // Solo héroes nuevos de un módulo activo; los de serie y cualquier otro id, la pantalla tal cual
                // Only new heroes from an active module; for stock ones and any other id, the screen is left as is
                if (!FaccionesNuevas.EsHeroeNuevo(heroe)) return;
                var accion = LogicaAccionesHeroes.De(heroe);
                if (accion == null) return;

                // Lo que ofrece la acción se calcula al enseñar la pantalla (con el azar de la campaña, como el juego:
                // volver a la pantalla enseña lo mismo)
                // What the action offers is computed when the screen is shown (with the campaign's randomness, like the game:
                // returning to the screen shows the same thing)
                List<int> refuerzos = null;
                ConsumableEnum consumible = default;
                bool hayQueOfrecer = true;
                if (!save.InMarch)
                {
                    if (accion.Tipo == LogicaAccionesHeroes.Tipo.Refuerzos)
                    {
                        refuerzos = Refuerzos(gestor, accion, save.bookNumber);
                        hayQueOfrecer = refuerzos.Count > 0;
                    }
                    else if (accion.Tipo == LogicaAccionesHeroes.Tipo.ConsumibleAlAzar)
                    {
                        // Igual que Rituales prohibidos: azar de la campaña adelantado una tirada (la del consumible de la recompensa)
                        // Same as Rituales prohibidos: campaign randomness advanced by one roll (the reward consumable's roll)
                        var azar = gestor.GetCampaignRandom();
                        azar.Next();
                        consumible = ConsumableData.GetRandomConsumable(azar);
                    }
                }

                bool estaElegida = __instance.WasTaken(LogicaAccionesHeroes.PrefijoEleccion + accion.Clave);
                var fila = LogicaAccionesHeroes.Decidir(save.InMarch, hayQueOfrecer, estaElegida, __instance.choiceMade);
                if (fila == LogicaAccionesHeroes.Fila.NoOfrecer) return;

                EngagementChoiceRow row = AnadirFila(__instance, vista, accion, refuerzos, consumible);
                if (row == null) return;
                // El original ya escondió (o no) la línea de «nada que elegir» antes de esta fila
                // The original has already hidden (or not) the «nothing to choose» line before this row
                vista.ShowNoChoices(false);

                switch (fila)
                {
                    case LogicaAccionesHeroes.Fila.YaElegida:
                        // Como el bucle del final de ShowChoices: elegida sin sonido y las demás apartadas
                        // Like the loop at the end of ShowChoices: chosen one without sound and the others set aside
                        __instance.choiceMade = true;
                        vista.ChooseRow(row, true);
                        break;
                    case LogicaAccionesHeroes.Fila.Apartada:
                        // El juego ya restauró otra opción elegida: esta se aparta como las otras no elegidas
                        // The game has already restored another chosen option: this one is set aside like the other unchosen ones
                        row.PopAway();
                        break;
                    default:
                        var panel = __instance;
                        row.Button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(
                            new Action(() => AlElegir(panel, row, accion, refuerzos, consumible))));
                        break;
                }
                InfoUnaVez("ofrecida" + heroe, $"Acción de héroe: el héroe {heroe} ofrece «{accion.TituloEs}» tras la batalla.");
            }
            catch (Exception e) { AvisoUnaVez("showchoices", "Acción de héroe: TrasShowChoices: " + e.Message + " (la pantalla sigue como la de serie)."); }
        }

        // Añade la fila con el AddChoice del juego (marca la opción como ofrecida y guarda su clave para el save)
        // Adds the row with the game's AddChoice (marks the option as offered and stores its key for the save)
        private static EngagementChoiceRow AnadirFila(EngagementPanel p, EngagementPanelView vista, LogicaAccionesHeroes.Accion a,
                                                      List<int> refuerzos, ConsumableEnum consumible)
        {
            // El título lo traduce AddChoice (Text(clave)) y la clave es también la del save, así que no se cambia: si el
            // módulo instalado no trae el texto, se pone el de respaldo en la fila ya creada (PonerTitulo)
            // The title is translated by AddChoice (Text(key)) and the key is also the save key, so it is not changed: if the
            // installed module does not provide the text, the fallback is set on the already created row (PonerTitulo)
            string titulo = Texto(a.Clave, a.TituloEs, a.TituloEn);
            EngagementChoiceRow row;
            switch (a.Tipo)
            {
                case LogicaAccionesHeroes.Tipo.Refuerzos:
                {
                    // Como Levantar a los muertos: nombres de las unidades en la línea y sus iconos en la fila
                    // Like Levantar a los muertos: unit names in the line and their icons in the row
                    var nombres = refuerzos.Select(id => TextoJuego(((UnitName)id).ToString()) ?? id.ToString());
                    row = p.AddChoice(vista.Icon(EngagementPanelView.RewardIcon.Recruit), a.Clave, string.Join(", ", nombres));
                    if (row == null) return null;
                    var iconos = new Il2CppReferenceArray<Sprite>(refuerzos.Count);
                    for (int i = 0; i < refuerzos.Count; i++) iconos[i] = TabletopTavernData.Instance.GetUnitIcon((UnitName)refuerzos[i]);
                    row.SetUnits(iconos);
                    break;
                }
                case LogicaAccionesHeroes.Tipo.ConsumibleAlAzar:
                {
                    // Como Rituales prohibidos: icono y nombre del consumible, descripción al pasar el ratón
                    // Like Rituales prohibidos: consumable icon and name, description on mouse hover
                    string nombre = TextoJuego(consumible + "Name") ?? consumible.ToString();
                    row = p.AddChoice(SpriteData.GetSprite(consumible.ToString()), a.Clave, nombre);
                    if (row == null) return null;
                    try
                    {
                        string desc = CampaignManager.Instance.ConsumableManager.GetConsumableDescription(consumible);
                        row.SetTooltip(nombre, KeywordText.ForTooltip(desc, null), "");
                    }
                    catch (Exception e) { AvisoUnaVez("tooltip", "Acción de héroe: sin descripción del consumible (" + e.Message + ")."); }
                    break;
                }
                default:
                    // Curaciones: icono de la Purga de la plaga y la línea en cursiva de la acción
                    // Healing: icon of Purga de la plaga and the action's italic line
                    row = p.AddChoice(vista.Icon(EngagementPanelView.RewardIcon.Purge), a.Clave, Texto(a.ClaveDetalle, a.DetalleEs, a.DetalleEn));
                    if (row == null) return null;
                    break;
            }
            // Etiqueta «Opción del héroe», como las de serie
            // Label «Opción del héroe», like the stock ones
            row.SetTag(TextoJuego("engagementHeroOption") ?? "");
            // Si el módulo no trae el título, AddChoice puso la clave: se cambia por el texto de respaldo
            // If the module does not provide the title, AddChoice set the key: it is replaced by the fallback text
            if (!LogicaAccionesHeroes.TextoValido(TextoJuego(a.Clave), a.Clave)) PonerTitulo(row, titulo);
            return row;
        }

        // Cambia el título visible de la fila (solo con el texto de respaldo); si no se puede, se queda la clave
        // Changes the visible title of the row (only with the fallback text); if it cannot be done, the key stays
        private static void PonerTitulo(EngagementChoiceRow row, string titulo)
        {
            try
            {
                var textos = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var t in textos)
                {
                    if (t == null || !t.text.StartsWith("zeroH")) continue;
                    t.text = titulo;
                    return;
                }
            }
            catch (Exception e) { AvisoUnaVez("titulo", "Acción de héroe: no se pudo poner el título de respaldo (" + e.Message + ")."); }
        }

        // Unidades de los Refuerzos: las activas del manifiesto de la raza del héroe y la rareza del acto, al azar
        // de la campaña (adelantado dos tiradas para no seguir a las de la recompensa y de Rituales prohibidos)
        // Reinforcement units: the active ones from the manifest of the hero's race and the act's rarity, chosen at random
        // from the campaign (advanced by two rolls so as not to follow those of the reward and of Rituales prohibidos)
        private static List<int> Refuerzos(CampaignSaveManager gestor, LogicaAccionesHeroes.Accion a, int libro)
        {
            if (!Plugin.Operativo) return new List<int>();     // sin fábrica, las unidades 9xx no están en el catálogo
                                                               // no factory, the 9xx units are not in the catalog
            var (rareza, cantidad) = LogicaAccionesHeroes.RefuerzosPorLibro(libro);
            var candidatas = LogicaAccionesHeroes.Candidatas(UnidadGuardia.Manifiesto.Unidades, a.Raza, rareza, RazaEfectiva);
            if (candidatas.Count == 0)
            {
                InfoUnaVez("sinrefuerzos" + a.Raza + rareza, $"Acción de héroe: no hay unidades activas de la raza {a.Raza} y rareza {rareza} para los refuerzos; no se ofrece la acción.");
                return new List<int>();
            }
            var azar = gestor.GetCampaignRandom();
            azar.Next();
            azar.Next();
            return LogicaAccionesHeroes.Elegir(candidatas, cantidad, n => azar.Next(n));
        }

        // Raza efectiva de una unidad del manifiesto (la unidad 900 cuenta como 40 con «Guardia en la facción A»)
        // Effective race of a manifest unit (unit 900 counts as 40 with «Guardia en la facción A»)
        private static int RazaEfectiva(Manifiesto.Unidad u)
        {
            if (u.Id == UnidadGuardia.ID && FaccionesNuevas.GuardiaEnAstra) return 40;
            return LogicaAccionesHeroes.RazaNumerica(u.Raza, n => Enum.TryParse(n, out Race r) ? (int)r : -1);
        }

        // ---------------------------------------------------------------
        // Al elegir la acción (mismo orden que los botones de serie: comprobación, efecto, Choose, QueueAutoContinue)
        // When choosing the action (same order as the stock buttons: check, effect, Choose, QueueAutoContinue)
        // ---------------------------------------------------------------
        private static void AlElegir(EngagementPanel p, EngagementChoiceRow row, LogicaAccionesHeroes.Accion a, List<int> refuerzos, ConsumableEnum consumible)
        {
            try
            {
                if (p == null || row == null || p.choiceMade) return;
                var gestor = p.campaignSaveManager;
                if (gestor == null) return;
                switch (a.Tipo)
                {
                    case LogicaAccionesHeroes.Tipo.CurarTodas:
                        // Como Consumir supervivientes, para todas las unidades (ModifyTroopHealth ya aplica los bonos de curación)
                        // Like Consumir supervivientes, for all units (ModifyTroopHealth already applies healing bonuses)
                        gestor.ModifyTroopHealth(a.Cantidad);
                        break;
                    case LogicaAccionesHeroes.Tipo.CurarUnaAlAzar:
                        // Como Purga de la plaga
                        // Like Purga de la plaga
                        gestor.HealRandomUnitToFull();
                        break;
                    case LogicaAccionesHeroes.Tipo.Refuerzos:
                        // Como Levantar a los muertos: sin sitio no se elige (aviso del juego) y se reclutan las que quepan
                        // Like Levantar a los muertos: with no room it cannot be chosen (game notice) and as many as fit are recruited
                        if (!gestor.CheckForRoomToRecruit()) { Notificar(TextoJuego("NoRoomForUnit")); return; }
                        foreach (int id in refuerzos)
                        {
                            if (!gestor.CheckForRoomToRecruit()) break;
                            gestor.RecruitSquad(TabletopTavernData.Instance.GetSquadStats((UnitName)id), 1f, false, false, 0);
                        }
                        Sonido(() => Memori.Audio.IAudioRequester.Instance.PlaySFX(Memori.Audio.SFXData.RecruitUnit));
                        break;
                    case LogicaAccionesHeroes.Tipo.ConsumibleAlAzar:
                        // Como Rituales prohibidos: sin sitio no se elige (aviso del juego)
                        // Like Rituales prohibidos: with no room it cannot be chosen (game notice)
                        if (!gestor.HasRoomForConsumable()) { Notificar(TextoJuego("NoRoomForConsumable")); return; }
                        Sonido(() => Memori.Audio.IAudioRequester.Instance.PlaySFX(Memori.Audio.SFXData.CollectItem));
                        gestor.AquireConsumable(consumible);
                        break;
                }
                p.Choose(row);
                p.QueueAutoContinue();
            }
            catch (Exception e) { AvisoUnaVez("elegir", $"Acción de héroe: al elegir «{a?.TituloEs}»: {e.Message} (la opción no se marca elegida)."); }
        }

        // Aviso de error del juego (el mismo que usan sus botones); si falla, solo el log
        // Game error notice (the same one its buttons use); if it fails, log only
        private static void Notificar(string texto)
        {
            try { Memori.Notifications.NotificationManager.Instance.ErrorNotification(texto ?? ""); }
            catch (Exception e) { AvisoUnaVez("notificar", "Acción de héroe: no se pudo enseñar el aviso del juego (" + e.Message + ")."); }
        }

        // Sonido del juego (el mismo de sus botones); si falla, sin sonido
        // Game sound (the same as its buttons); if it fails, no sound
        private static void Sonido(Action reproducir)
        {
            try { reproducir(); } catch { }
        }
    }
}
