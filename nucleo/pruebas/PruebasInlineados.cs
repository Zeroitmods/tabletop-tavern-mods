// PruebasInlineados.cs
// Prueba 17 (18-10-2026): lógica de los arreglos de inlining (LogicaInlineados.cs, usada por ParchesInlineados.cs).
//   - Contexto anidado: abrir y cerrar devuelve el de fuera; cerrar sin abrir no falla; y el contexto aguanta el patrón
//     abrir + cerrar en un finally aunque haya una excepción. Ojo (revisión del 18-10, L-11): esa última prueba simula el
//     patrón con un try/finally de C#; NO prueba Harmony ni que ParchesInlineados registre de verdad el finalizer (eso
//     solo se puede ver dentro del juego).
//   - Ciudad: solo una raza nueva abre el contexto; la guarnición cambia solo dentro de GenerateTown.
//   - Región: se cambia solo dentro de MapNode.SetUp y con raza nueva.
//   - UnitKills: las bajas de las escuadras 9xx no suman; descontadas escuadra a escuadra, el total es el de serie y
//     ningún paso intermedio lo supera (el umbral de un logro no se cruza antes).
// Autor: ZeroitMods.
// PruebasInlineados.cs
// Test 17 (18-10-2026): logic of the inlining fixes (LogicaInlineados.cs, used by ParchesInlineados.cs).
//   - Nested context: opening and closing returns the outer one; closing without opening does not fail; and the context holds up the pattern
//     open + close in a finally even if there is an exception. Note (review of 18-10, L-11): that last test simulates the
//     pattern with a C# try/finally; it does NOT test Harmony nor that ParchesInlineados really registers the finalizer (that
//     can only be seen inside the game).
//   - Town: only a new race opens the context; the garrison changes only inside GenerateTown.
//   - Region: it is changed only inside MapNode.SetUp and with a new race.
//   - UnitKills: kills from 9xx squads do not count; subtracted squad by squad, the total is the stock one and
//     no intermediate step exceeds it (an achievement threshold is not crossed early).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroNucleo;

internal static class PruebasInlineados
{
    public static void Todas(Action<bool, string> comprobar)
    {
        // -------- Contexto anidado
        // -------- Nested context
        var c = new ContextoAnidado<int>();
        comprobar(!c.Abierto && c.Profundidad == 0, "inlining: el contexto empieza cerrado");
        c.Abrir(40);
        c.Abrir(41);
        bool dentro = c.Abierto && c.Valor == 41 && c.Profundidad == 2;
        c.Cerrar();
        bool fuera = c.Abierto && c.Valor == 40 && c.Profundidad == 1;
        c.Cerrar();
        comprobar(dentro && fuera && !c.Abierto && c.Profundidad == 0,
            "inlining: contexto anidado 40 -> 41; al cerrar el de dentro vuelve el 40 y luego queda cerrado");
        c.Cerrar();
        comprobar(!c.Abierto && c.Profundidad == 0 && c.Valor == 0, "inlining: cerrar sin abrir no falla y lo deja cerrado");

        // Contexto con el patrón abrir + cerrar en un finally y una excepción en medio (simulación en C# del prefix +
        // finalizer; no ejecuta Harmony: solo comprueba que el contexto queda cerrado si se cierra en el finally)
        // Context with the open + close pattern in a finally and an exception in the middle (C# simulation of the prefix +
        // finalizer; it does not run Harmony: it only checks that the context ends up closed if it is closed in the finally)
        var ctx = new ContextoAnidado<int>();
        void MetodoDelJuego(bool lanza)
        {
            ctx.Abrir(40);                                             // prefix
            try { if (lanza) throw new InvalidOperationException("fallo del juego"); }
            finally { ctx.Cerrar(); }                                  // finalizer (Harmony lo pone en un finally)
                                                                       // finalizer (Harmony puts it in a finally)
        }
        try { MetodoDelJuego(true); } catch (InvalidOperationException) { }
        MetodoDelJuego(false);
        comprobar(!ctx.Abierto && ctx.Profundidad == 0, "inlining (simulado en C#, sin Harmony): abrir y cerrar en un finally deja el contexto cerrado aunque haya una excepción");

        // -------- Ciudad (GenerateTownRace copiado en GenerateTown)
        // -------- Town (GenerateTownRace copied into GenerateTown)
        Func<int, bool> esNueva = r => r == 40 || r == 41;
        comprobar(LogicaInlineados.RazaCiudadNueva(40, esNueva) == 40 && LogicaInlineados.RazaCiudadNueva(41, esNueva) == 41,
            "ciudad: GenerateTownRace da 40/41 -> el contexto lleva la raza nueva");
        comprobar(LogicaInlineados.RazaCiudadNueva(3, esNueva) == LogicaInlineados.SinRaza && LogicaInlineados.RazaCiudadNueva(40, null) == LogicaInlineados.SinRaza,
            "ciudad: raza de serie (o sin facciones) -> sin raza nueva, la copia del juego ya acierta");
        comprobar(LogicaInlineados.RazaGuarnicion(5, true, 40) == 40,
            "ciudad: dentro de GenerateTown con raza nueva, la guarnición se pide con la 40");
        comprobar(LogicaInlineados.RazaGuarnicion(5, false, 40) == 5 && LogicaInlineados.RazaGuarnicion(5, true, LogicaInlineados.SinRaza) == 5,
            "ciudad: fuera de GenerateTown (batallas, ofertas) o con raza de serie, la raza pedida no se toca");

        // -------- Región del nodo (GetMapRegion copiado en MapNode.SetUp)
        // -------- Node region (GetMapRegion copied into MapNode.SetUp)
        comprobar(LogicaInlineados.CambiarRegion(true, 41, esNueva), "región: nodo de raza nueva dentro de SetUp -> región prestada");
        comprobar(!LogicaInlineados.CambiarRegion(true, 2, esNueva) && !LogicaInlineados.CambiarRegion(false, 41, esNueva) && !LogicaInlineados.CambiarRegion(true, 41, null),
            "región: nodo de serie, fuera de SetUp (TownPanel, EngagementPanel) o sin facciones -> no se toca");

        // -------- UnitKills sin las bajas de las 9xx
        // -------- UnitKills without the 9xx kills
        var escuadras = new List<(int unidad, int bajas)> { (12, 30), (900, 25), (3, 10), (944, 7), (921, 0), (960, -2) };
        int nuevas = LogicaInlineados.BajasNuevas(escuadras);
        comprobar(nuevas == 32, $"UnitKills: bajas de escuadras 9xx = 25 + 7 (las de serie, 0 y negativas no cuentan) ({nuevas})");
        comprobar(LogicaInlineados.BajasNuevas(null) == 0 && LogicaInlineados.BajasNuevas(new[] { (1, 5), (2, 9) }) == 0,
            "UnitKills: sin escuadras o solo de serie -> nada que descontar");

        // El juego suma escuadra a escuadra (orden del ejército) y al final las bajas de hechizos; el descuento se reparte.
        // Sus contadores de bajas nunca son negativos: la secuencia que suma el juego no lleva la entrada -2 de arriba
        // The game adds up squad by squad (army order) and at the end the spell kills; the subtraction is spread out.
        // Its kill counters are never negative: the sequence the game adds up does not include the -2 entry above
        int pendiente = nuevas, acumulado = 0, deSerie = escuadras.Where(e => e.unidad < 900 && e.bajas > 0).Sum(e => e.bajas), hechizos = 4;
        bool nuncaPasa = true;
        foreach (var (_, bajas) in escuadras.Where(e => e.bajas >= 0))
        {
            acumulado += LogicaInlineados.Descontar(bajas, ref pendiente);
            if (acumulado > deSerie) nuncaPasa = false;
        }
        acumulado += LogicaInlineados.Descontar(hechizos, ref pendiente);
        comprobar(pendiente == 0 && acumulado == deSerie + hechizos && nuncaPasa,
            $"UnitKills: total sumado = bajas de serie + hechizos ({acumulado} = {deSerie} + {hechizos}) y ningún paso pasa del de serie");
        int p2 = 5;
        comprobar(LogicaInlineados.Descontar(3, ref p2) == 0 && p2 == 2 && LogicaInlineados.Descontar(10, ref p2) == 8 && p2 == 0 && LogicaInlineados.Descontar(6, ref p2) == 6,
            "UnitKills: el descuento no baja de 0 y, agotado lo pendiente, el resto suma tal cual");
        int p3 = 4;
        comprobar(LogicaInlineados.Descontar(0, ref p3) == 0 && p3 == 4 && LogicaInlineados.Descontar(-1, ref p3) == -1 && p3 == 4,
            "UnitKills: un valor 0 o negativo pasa tal cual y no gasta lo pendiente");
        // Con la regla común: sin bajas 9xx el total es el del juego (las 9xx ni suman ni bloquean)
        // With the common rule: without 9xx kills the total is the game's (the 9xx neither add nor block)
        int p4 = LogicaInlineados.BajasNuevas(new[] { (1, 8), (2, 2) });
        comprobar(LogicaInlineados.Descontar(8, ref p4) + LogicaInlineados.Descontar(2, ref p4) == 10,
            "UnitKills: ejército solo de serie -> suma lo mismo que el juego");
    }
}
