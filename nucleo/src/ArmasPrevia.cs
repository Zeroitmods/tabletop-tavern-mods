// ArmasPrevia.cs
// Retrato 3D de las unidades con modelo propio (Colección, reclutamiento y prestigio): esconde el arma cuerpo a cuerpo
// ('sword') de las unidades que cambian arco por espada, igual que hace el juego en batalla.
//   - En batalla el juego enseña solo el arma a distancia ('bow') y pasa a la de melé SOLO al quedarse sin munición
//     (SquadRanOutOfAmmoSystem) o cuando el jugador pulsa el modo melé de la escuadra; no cambia por entrar en cuerpo a
//     cuerpo (corregido en la revisión del 18-10). SwordSetUpSystem pone la escala de la espada a 0 al aparecer y
//     RangedMeleeConverterSystem alterna arco 1 / espada 0 <-> arco 0 / espada 1. Eso va por el 'role' de cada adjunto
//     del unit.json. Este plugin no toca ese cambio: lo decide el juego.
//   - En el retrato 3D, en cambio, ModUnitPreview monta el modelo con TJBakePreviewPlayer.Show, que instancia TODOS los
//     adjuntos sin mirar su papel: salían el fusil y el cuchillo (o una segunda copia del fusil) a la vez. Las unidades
//     de serie no pasan por ahí (usan su prefab de reclutamiento), por eso solo se notaba en las unidades nuevas.
//   - Este postfix deja el retrato como la batalla en reposo: si el visual tiene a la vez 'bow' y 'sword' (la misma
//     condición que UnitGPUAnimLoader.HasWeaponSwap), desactiva los adjuntos 'sword'. Una unidad con espada y sin arco
//     no se toca.
// Se usa SetActive(false) y no la escala: el Tick del reproductor reescribe la escala de cada adjunto en cada fotograma.
// Con el adjunto desactivado, además, el encuadre de la Colección (CollectionPreviewRig.Frame) no cuenta la espada.
// ArmasPrevia.cs
// 3D portrait of units with their own model (Collection, recruitment and prestige): hides the melee weapon
// ('sword') of units that swap bow for sword, just as the game does in battle.
//   - In battle the game shows only the ranged weapon ('bow') and switches to the melee one ONLY when out of ammo
//     (SquadRanOutOfAmmoSystem) or when the player presses the squad's melee mode; it does not change on entering melee
//     combat (fixed in the review of 18-10). SwordSetUpSystem sets the sword's scale to 0 on spawn and
//     RangedMeleeConverterSystem toggles bow 1 / sword 0 <-> bow 0 / sword 1. That goes by the 'role' of each attachment
//     in unit.json. This plugin does not touch that switch: the game decides it.
//   - In the 3D portrait, however, ModUnitPreview builds the model with TJBakePreviewPlayer.Show, which instantiates ALL the
//     attachments without looking at their role: the rifle and the knife (or a second copy of the rifle) showed up at once.
//     Stock units do not go through there (they use their recruitment prefab), so it was only noticeable on new units.
//   - This postfix leaves the portrait like the battle at rest: if the visual has both 'bow' and 'sword' (the same
//     condition as UnitGPUAnimLoader.HasWeaponSwap), it deactivates the 'sword' attachments. A unit with a sword and no bow
//     is left untouched.
// SetActive(false) is used and not the scale: the player's Tick rewrites each attachment's scale every frame.
// With the attachment deactivated, the Collection framing (CollectionPreviewRig.Frame) also does not count the sword.

using System;
using MemoriStudios.TJBake;
using UnityEngine;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: parche del retrato 3D (TJBakePreviewPlayer.Show)
    // Section: 3D portrait patch (TJBakePreviewPlayer.Show)
    // -----------------------------------------------------------------------
    public static class ArmasPrevia
    {
        // Nombre del contenedor que crea TJBakePreviewPlayer.Show bajo el reproductor
        // Name of the container that TJBakePreviewPlayer.Show creates under the player
        private const string Contenedor = "TJBake Visual";

        // Para avisar una sola vez si algo falla (el retrato sigue saliendo como lo monta el juego)
        // To warn only once if something fails (the portrait still appears as the game builds it)
        private static bool _avisado;
        private static bool _informado;

        /// <summary>
        /// Postfix de TJBakePreviewPlayer.Show(TJBakeVisual, int). Los adjuntos cuelgan del contenedor con el nombre
        /// «Anchor {índice} {papel}» (TJBakeValidation.RoleName). Idempotente: corre también en el Show del jinete y en
        /// SetFlatColour(null), que vuelve a llamar a Show.
        /// Postfix of TJBakePreviewPlayer.Show(TJBakeVisual, int). The attachments hang from the container named
        /// «Anchor {índice} {papel}» (TJBakeValidation.RoleName). Idempotent: it also runs in the rider's Show and in
        /// SetFlatColour(null), which calls Show again.
        /// </summary>
        public static void TrasShow(TJBakePreviewPlayer __instance)
        {
            try
            {
                if (__instance == null) return;

                // Sufijos de los nombres sacados del propio juego (no literales)
                // Suffixes of the names taken from the game itself (not literals)
                string sufijoArco = " " + TJBakeValidation.RoleName(TJBakePropRole.Bow);
                string sufijoEspada = " " + TJBakeValidation.RoleName(TJBakePropRole.Sword);

                // Se recorren TODOS los contenedores hijos directos: al volver a llamar a Show (SetFlatColour(null)), el
                // contenedor viejo sigue ahí hasta el final del fotograma (Destroy es diferido) y Transform.Find podría
                // devolver ese en vez del nuevo
                // ALL direct child containers are traversed: when Show is called again (SetFlatColour(null)), the
                // old container is still there until the end of the frame (Destroy is deferred) and Transform.Find could
                // return that one instead of the new one
                Transform raiz = __instance.transform;
                for (int c = 0; c < raiz.childCount; c++)
                {
                    Transform contenido = raiz.GetChild(c);
                    if (contenido.name == Contenedor) EsconderEspadas(contenido, sufijoArco, sufijoEspada);
                }
            }
            catch (Exception e)
            {
                if (_avisado) return;
                _avisado = true;
                Plugin.Aviso("Retrato 3D: no se pudo esconder el arma de melé (" + e.GetType().Name + ": " + e.Message + "); se ve como lo monta el juego.");
            }
        }

        /// <summary>Esconde los adjuntos 'sword' de un contenedor si tiene también algún 'bow'.</summary>
        /// Hides the 'sword' attachments of a container if it also has any 'bow'.
        private static void EsconderEspadas(Transform contenido, string sufijoArco, string sufijoEspada)
        {
            {
                // 1) ¿Tiene este visual arco y espada a la vez? (si no, el juego no cambia de arma y no se toca nada)
                // 1) Does this visual have a bow and a sword at the same time? (if not, the game does not switch weapons and nothing is touched)
                bool hayArco = false, hayEspada = false;
                int n = contenido.childCount;
                for (int i = 0; i < n; i++)
                {
                    string nombre = contenido.GetChild(i).name;
                    if (!nombre.StartsWith("Anchor ", StringComparison.Ordinal)) continue;
                    if (nombre.EndsWith(sufijoArco, StringComparison.Ordinal)) hayArco = true;
                    else if (nombre.EndsWith(sufijoEspada, StringComparison.Ordinal)) hayEspada = true;
                }
                if (!hayArco || !hayEspada) return;

                // 2) Esconder los adjuntos de melé: en reposo solo se ve el arma a distancia, como en batalla
                // 2) Hide the melee attachments: at rest only the ranged weapon is visible, as in battle
                int escondidos = 0;
                for (int i = 0; i < n; i++)
                {
                    Transform hijo = contenido.GetChild(i);
                    string nombre = hijo.name;
                    if (nombre.StartsWith("Anchor ", StringComparison.Ordinal) && nombre.EndsWith(sufijoEspada, StringComparison.Ordinal)
                        && hijo.gameObject.activeSelf)
                    {
                        hijo.gameObject.SetActive(false);
                        escondidos++;
                    }
                }
                if (escondidos > 0 && !_informado)
                {
                    _informado = true;
                    Plugin.Info("Retrato 3D: arma de melé escondida en reposo en las unidades que cambian arco por espada (como en batalla).");
                }
            }
        }
    }
}
