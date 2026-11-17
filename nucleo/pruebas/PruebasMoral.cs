// PruebasMoral.cs
// Prueba 29 (03-11-2026, núcleo 0.7.6): moral de las escuadras del jugador con el módulo «Impertérritos»
// (src\LogicaMoral.cs). Lo que se comprueba: el umbral de huida queda inalcanzable, la moral se llena, el estado se corrige
// (rota -> flaquea con solo «sin huida»; firme con «sin flaquear») y no se escribe nada si no cambia nada.
// Autor: ZeroitMods.
// PruebasMoral.cs
// Test 29 (11-03-2026, core 0.7.6): morale of the player's squads with the «Impertérritos» module
// (src\LogicaMoral.cs). What is checked: the flee threshold becomes unreachable, morale fills up, the state is corrected
// (routed -> wavering with only «no fleeing»; steady with «no wavering») and nothing is written if nothing changes.
// Author: ZeroitMods.

using System;
using ZeroNucleo;

internal static class PruebasMoral
{
    // -----------------------------------------------------------------------
    // Sección: casos de LogicaMoral.Ajustar
    // Section: LogicaMoral.Ajustar cases
    // -----------------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        // Escuadra a mitad de moral, umbral de serie (5) y flaqueando, con los dos efectos
        // Squad at half morale, default threshold (5) and wavering, with both effects
        float actual = 20f, umbral = 5f; byte estado = LogicaMoral.Flaquea;
        bool cambio = LogicaMoral.Ajustar(ref actual, 60f, ref umbral, ref estado, true, true);
        comprobar(cambio && actual == 60f && umbral == LogicaMoral.UmbralSinHuida && estado == LogicaMoral.Firme,
            "moral: con los dos efectos, moral llena, umbral inalcanzable y estado firme");

        // Ya ajustada: no hay nada que escribir
        // Already adjusted: nothing to write
        cambio = LogicaMoral.Ajustar(ref actual, 60f, ref umbral, ref estado, true, true);
        comprobar(!cambio && actual == 60f && estado == LogicaMoral.Firme, "moral: ya ajustada, no cambia nada (no se escribe la entidad)");

        // Solo «sin huida»: la moral puede bajar y flaquear, pero una rota pasa a flaquear y el umbral no se alcanza
        // Only «no fleeing»: morale can drop and waver, but a routed squad becomes wavering and the threshold is not reached
        float a2 = 0f, u2 = 5f; byte e2 = LogicaMoral.Rota;
        cambio = LogicaMoral.Ajustar(ref a2, 60f, ref u2, ref e2, true, false);
        comprobar(cambio && a2 == 0f && u2 == LogicaMoral.UmbralSinHuida && e2 == LogicaMoral.Flaquea,
            "moral: solo sin huida, la moral no se toca, el umbral queda inalcanzable y una rota pasa a flaquear");
        // Con la moral en 0 (lo mínimo del juego) o tras una carga por el flanco (-12 sin suelo), sigue por encima del umbral
        // With morale at 0 (the game's minimum) or after a flank charge (-12 with no floor), it stays above the threshold
        comprobar(0f > u2 && -12f > u2 && -500f > u2, "moral: ni con 0 ni con restas de golpe se llega al umbral de huida");

        // Solo «sin flaquear»: moral llena y firme, el umbral de serie se queda
        // Only «no wavering»: full morale and steady, the default threshold remains
        float a3 = 10f, u3 = 5f; byte e3 = LogicaMoral.Flaquea;
        cambio = LogicaMoral.Ajustar(ref a3, 40f, ref u3, ref e3, false, true);
        comprobar(cambio && a3 == 40f && u3 == 5f && e3 == LogicaMoral.Firme, "moral: solo sin flaquear, moral llena y firme, umbral de serie");

        // 2.ª ronda de la revisión: solo «sin flaquear» no pone firme a una escuadra ya rota (huiría con estado firme)
        // 2nd round of the review: only «no wavering» does not make an already routed squad steady (it would flee with a steady state)
        float a7 = 0f, u7 = 5f; byte e7 = LogicaMoral.Rota;
        LogicaMoral.Ajustar(ref a7, 40f, ref u7, ref e7, false, true);
        comprobar(e7 == LogicaMoral.Rota && a7 == 40f, "moral: solo sin flaquear, una escuadra ya rota sigue rota");

        // Sin efectos: nada cambia
        // No effects: nothing changes
        float a4 = 10f, u4 = 5f; byte e4 = LogicaMoral.Rota;
        cambio = LogicaMoral.Ajustar(ref a4, 40f, ref u4, ref e4, false, false);
        comprobar(!cambio && a4 == 10f && u4 == 5f && e4 == LogicaMoral.Rota, "moral: sin efectos, la escuadra queda como la dejó el juego");

        // Moral máxima 0 (escuadra sin liderazgo): no se llena ni se fuerza firme; el umbral sí
        // Max morale 0 (squad without leadership): it is not filled or forced steady; the threshold is
        float a5 = 0f, u5 = 5f; byte e5 = LogicaMoral.Rota;
        cambio = LogicaMoral.Ajustar(ref a5, 0f, ref u5, ref e5, true, true);
        comprobar(cambio && a5 == 0f && u5 == LogicaMoral.UmbralSinHuida && e5 == LogicaMoral.Flaquea,
            "moral: con máxima 0 solo se aparta el umbral (rota pasa a flaquear)");

        // Un umbral ya muy negativo no se reescribe
        // An already very negative threshold is not rewritten
        float a6 = 60f, u6 = LogicaMoral.UmbralSinHuida * 2f; byte e6 = LogicaMoral.Firme;
        cambio = LogicaMoral.Ajustar(ref a6, 60f, ref u6, ref e6, true, true);
        comprobar(!cambio && u6 == LogicaMoral.UmbralSinHuida * 2f, "moral: un umbral ya inalcanzable no se reescribe");
    }
}
