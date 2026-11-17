// PruebasInteres.cs
// Prueba 19 (18-10-2026): Tesorería imperial de la raza 41 (LogicaInteres.TesoreriaImperial), fuera del juego.
// Decisión de Zero (revision_18oct_equilibrio.md §2.1): el interés doble solo vale para los primeros 5 de interés.
// Se comprueba: interés 0 o negativo sin tocar, el tramo de 1 a 5 se duplica entero, por encima de 5 solo se suman 5,
// el máximo con el tope 30 de «Zero - Economía» es 35 (antes 60) y con el tope 5 de serie es 10, como Tokugawa.
// Autor: ZeroitMods.
// PruebasInteres.cs
// Test 19 (18-10-2026): Imperial Treasury of race 41 (LogicaInteres.TesoreriaImperial), outside the game.
// Zero's decision (revision_18oct_equilibrio.md §2.1): double interest only applies to the first 5 of interest.
// It checks: interest 0 or negative left untouched, the 1 to 5 range is fully doubled, above 5 only 5 is added,
// the maximum with the cap of 30 from "Zero - Economía" is 35 (previously 60) and with the stock cap of 5 it is 10, like Tokugawa.
// Author: ZeroitMods.

using System;
using ZeroNucleo;

internal static class PruebasInteres
{
    public static void Todas(Action<bool, string> comprobar)
    {
        // Sección: casos límite (nada que duplicar)
        // Section: edge cases (nothing to double)
        comprobar(LogicaInteres.TesoreriaImperial(0) == 0 && LogicaInteres.TesoreriaImperial(-3) == -3,
                  "interés: con 0 o negativo la Tesorería imperial no cambia nada");

        // Sección: tramo doble (de 1 a 5 de interés se duplica entero)
        // Section: double range (from 1 to 5 of interest it is fully doubled)
        bool tramo = true;
        for (int i = 1; i <= LogicaInteres.TramoDoble; i++)
            tramo &= LogicaInteres.TesoreriaImperial(i) == i * 2;
        comprobar(tramo && LogicaInteres.TramoDoble == 5, "interés: de 1 a 5 se duplica entero (tramo de 5, el tope de serie)");

        // Sección: por encima del tramo solo se suman 5
        // Section: above the range only 5 is added
        comprobar(LogicaInteres.TesoreriaImperial(6) == 11 && LogicaInteres.TesoreriaImperial(12) == 17,
                  "interés: por encima de 5 solo se suman 5 (6 -> 11, 12 -> 17)");

        // Sección: topes reales (30 de Zero - Economía y 5 de serie)
        // Section: real caps (30 from Zero - Economía and 5 stock)
        comprobar(LogicaInteres.TesoreriaImperial(30) == 35 && LogicaInteres.TesoreriaImperial(5) == 10,
                  "interés: máximo 35 con el tope 30 de Economía sin objetos de interés (antes 60) y 10 con el tope 5 de serie (como Tokugawa)");
    }
}
