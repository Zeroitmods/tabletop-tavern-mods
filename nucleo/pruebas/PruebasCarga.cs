// PruebasCarga.cs
// Prueba 24 (01-11-2026, juego 1.11.1): carga tolerante del núcleo (LogicaCarga.cs), fuera del juego.
//   - Fases: una fase que lanza TypeLoadException o MissingMethodException (lo que hace el JIT al entrar en un método que
//     nombra un tipo que el juego quitó o movió) no impide las siguientes; el fallo llega al aviso con su nombre; un aviso
//     que a su vez falla no deja escapar nada.
//   - Lista de parches con tipos diferidos: si el tipo de un parche falla al resolverse, falla solo ese parche. Desde la
//     revisión del 01-11 se prueba el código real de Plugin.Parchear (LogicaCarga.AplicarUno) con tipos y métodos de .NET.
//   - Logros de ejército sin contexto (revisión del 01-11): historial de partidas con contenido nuevo y decisión de bloqueo.
//   - Tipos por nombre (ResolverTipo): candidatos en orden, con ensamblado y sin él, buscadores que lanzan, nada que
//     encontrar; y el caso real de HttpAnalyticsBackend (ubicación del juego 1.11.1 primero, la de 1.10.17 de respaldo).
// El JIT real del juego (un typeof de un tipo que ya no está en el interop) solo se puede ver dentro del juego; aquí se
// simula lanzando las mismas excepciones desde la acción de la fase.
// Autor: ZeroitMods.
// PruebasCarga.cs
// Test 24 (01-11-2026, game 1.11.1): tolerant loading of the core (LogicaCarga.cs), outside the game.
//   - Phases: a phase that throws TypeLoadException or MissingMethodException (what the JIT does when entering a method that
//     names a type the game removed or moved) does not prevent the following ones; the failure reaches the warning with its name; a warning
//     that itself fails lets nothing escape.
//   - List of patches with deferred types: if a patch's type fails to resolve, only that patch fails. Since the
//     review of 01-11, the real code of Plugin.Parchear (LogicaCarga.AplicarUno) is tested with .NET types and methods.
//   - Army achievements without context (review of 01-11): run history with new content and blocking decision.
//   - Types by name (ResolverTipo): candidates in order, with and without assembly, finders that throw, nothing to
//     find; and the real case of HttpAnalyticsBackend (game 1.11.1 location first, the 1.10.17 one as fallback).
// The game's real JIT (a typeof of a type that is no longer in the interop) can only be seen inside the game; here it is
// simulated by throwing the same exceptions from the phase's action.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using ZeroNucleo;

internal static class PruebasCarga
{
    public static void Todas(Action<bool, string> comprobar)
    {
        PruebaFases(comprobar);
        PruebaListaDiferida(comprobar);
        PruebaResolverTipo(comprobar);
        PruebaAplicarUno(comprobar);
        PruebaLogrosEjercito(comprobar);
    }

    // ---------------------------------------------------------------
    // Revisión del 01-11: el paso a paso real de Plugin.Parchear (LogicaCarga.AplicarUno)
    // Review of 01-11: the real step by step of Plugin.Parchear (LogicaCarga.AplicarUno)
    // ---------------------------------------------------------------
    private static void PruebaAplicarUno(Action<bool, string> comprobar)
    {
        // Buscador como el de Plugin.Parchear (AccessTools.Method), aquí con reflexión de .NET
        // Finder like the one in Plugin.Parchear (AccessTools.Method), here using .NET reflection
        Func<Type, string, Type[], System.Reflection.MethodInfo> buscar = (t, m, a) => a == null ? t.GetMethod(m, Type.EmptyTypes) : t.GetMethod(m, a);
        var aplicados = new List<string>();
        Action<System.Reflection.MethodInfo> aplicar = mi => aplicados.Add(mi.DeclaringType.Name + "." + mi.Name);

        var e1 = LogicaCarga.AplicarUno(() => typeof(string), "Substring", () => new[] { typeof(int) }, buscar, aplicar, out var t1);
        var e2 = LogicaCarga.AplicarUno(() => throw new TypeLoadException("Memori.Steamworks.SteamAchievements"), "Unlock", null, buscar, aplicar, out var t2);
        var e3 = LogicaCarga.AplicarUno(() => null, "Record", null, buscar, aplicar, out _);
        var e4 = LogicaCarga.AplicarUno(() => typeof(string), "NoExiste", null, buscar, aplicar, out var t4);
        var e5 = LogicaCarga.AplicarUno(() => typeof(string), "Trim", null, buscar, mi => throw new InvalidOperationException("Harmony no pudo"), out _);
        var e6 = LogicaCarga.AplicarUno(() => typeof(string), "Trim", () => throw new TypeLoadException("argumento movido"), buscar, aplicar, out _);
        var e7 = LogicaCarga.AplicarUno(() => typeof(string), "Trim", null, buscar, aplicar, out _);

        comprobar(e1 == null && t1 == typeof(string) && e7 == null && aplicados.Count == 2 && aplicados[0] == "String.Substring" && aplicados[1] == "String.Trim",
            "aplicar uno: los parches con tipo y método existentes se aplican aunque otros fallen");
        comprobar(e2 is TypeLoadException && t2 == null, "aplicar uno: un tipo que lanza al resolverse da TypeLoadException y no se aplica");
        comprobar(e3 is TypeLoadException, "aplicar uno: un tipo que no se encuentra (null) da TypeLoadException");
        comprobar(e4 is MissingMethodException && t4 == typeof(string), "aplicar uno: un método que no existe da MissingMethodException (con el tipo para el log)");
        comprobar(e5 is InvalidOperationException, "aplicar uno: si falla Harmony al parchear, se devuelve su excepción sin escaparse");
        comprobar(e6 is TypeLoadException, "aplicar uno: unos argumentos que lanzan al resolverse solo tumban ese parche");
    }

    // ---------------------------------------------------------------
    // Revisión del 01-11: logros de ejército sin contexto de recuento
    // Review of 01-11: army achievements without count context
    // ---------------------------------------------------------------
    private static void PruebaLogrosEjercito(Action<bool, string> comprobar)
    {
        var anclados = new HashSet<string> { "run-mod" };
        Func<string, bool> anclada = p => anclados.Contains(p);
        // Historial como el del save real del 01-11: héroes 7 y 11 de serie, 901 y 903 nuevos; las 9xx guardadas como su base
        // History like the one in the real save of 01-11: stock heroes 7 and 11, new 901 and 903; the 9xx ones saved as their base
        var deSerie = new List<(string, int, IEnumerable<int>)> { ("run-a", 7, new[] { 37, 38, 35 }), ("run-b", 11, new[] { 66, 67 }) };
        var conHeroe = new List<(string, int, IEnumerable<int>)>(deSerie) { ("run-c", 901, new[] { 2, 70 }) };
        var conRegistro = new List<(string, int, IEnumerable<int>)>(deSerie) { ("run-mod", 7, new[] { 70, 70 }) };
        var conUnidad = new List<(string, int, IEnumerable<int>)>(deSerie) { ("run-d", 7, new[] { 37, 905 }) };

        comprobar(!LogicaLogros.HistorialConContenidoNuevo(deSerie, anclada), "logros de ejército: historial solo de serie y sin anclar, no se bloquea");
        comprobar(LogicaLogros.HistorialConContenidoNuevo(conHeroe, anclada), "logros de ejército: una partida con héroe 9xx en el historial bloquea");
        comprobar(LogicaLogros.HistorialConContenidoNuevo(conRegistro, anclada), "logros de ejército: una partida anclada a los módulos en el registro bloquea (aunque sus unidades se vean de serie)");
        comprobar(LogicaLogros.HistorialConContenidoNuevo(conUnidad, anclada), "logros de ejército: una unidad 9xx en el historial bloquea");
        comprobar(!LogicaLogros.HistorialConContenidoNuevo(null, anclada) && !LogicaLogros.HistorialConContenidoNuevo(deSerie, null),
            "logros de ejército: sin historial o sin registro no se inventa contenido nuevo");
        comprobar(LogicaLogros.BloquearSinContexto(true, false) && LogicaLogros.BloquearSinContexto(false, true) && !LogicaLogros.BloquearSinContexto(false, false),
            "logros de ejército: sin contexto se bloquea si la partida en curso está anclada o el historial tiene contenido nuevo");
    }

    // ---------------------------------------------------------------
    // Fases de Load(): un fallo queda en su fase
    // Load() phases: a failure stays within its phase
    // ---------------------------------------------------------------
    private static void PruebaFases(Action<bool, string> comprobar)
    {
        var hechas = new List<string>();
        var fallos = new List<(string, Exception)>();
        Action<string, Exception> alFallar = (n, e) => fallos.Add((n, e));

        bool r1 = LogicaCarga.Fase("clasificaciones", () => hechas.Add("clasificaciones"), alFallar);
        bool r2 = LogicaCarga.Fase("telemetría", () => throw new TypeLoadException("Could not load type 'TabletopTavern.Analytics.HttpAnalyticsBackend'"), alFallar);
        bool r3 = LogicaCarga.Fase("logros", () => throw new MissingMethodException("SteamAchievements", "Unlock"), alFallar);
        bool r4 = LogicaCarga.Fase("fábrica", () => hechas.Add("fábrica"), alFallar);

        comprobar(r1 && !r2 && !r3 && r4, "fases: la que termina da true y la que lanza da false");
        comprobar(hechas.Count == 2 && hechas[0] == "clasificaciones" && hechas[1] == "fábrica",
            "fases: tras un TypeLoadException y un MissingMethodException siguen ejecutándose las fases de después");
        comprobar(fallos.Count == 2 && fallos[0].Item1 == "telemetría" && fallos[0].Item2 is TypeLoadException
                  && fallos[1].Item1 == "logros" && fallos[1].Item2 is MissingMethodException,
            "fases: cada fallo llega al aviso con el nombre de su fase y su excepción");

        bool sinEscape = true;
        try { LogicaCarga.Fase("aviso roto", () => throw new InvalidOperationException("x"), (n, e) => throw new Exception("el aviso también falla")); }
        catch { sinEscape = false; }
        comprobar(sinEscape, "fases: si el aviso del fallo también lanza, no se escapa nada de la fase");
        comprobar(LogicaCarga.Fase("nula", null, alFallar), "fases: una acción nula cuenta como terminada");

        string d = LogicaCarga.Describir(new TypeLoadException("falta", new InvalidOperationException("interna")));
        comprobar(d.Contains("TypeLoadException") && d.Contains("falta") && d.Contains("interna"),
            "fases: el texto del aviso lleva el tipo, el mensaje y la excepción interna");
    }

    // ---------------------------------------------------------------
    // Lista de parches con tipos diferidos: un tipo que falta solo tumba su parche
    // List of patches with deferred types: a missing type only brings down its own patch
    // ---------------------------------------------------------------
    private static void PruebaListaDiferida(Action<bool, string> comprobar)
    {
        // Igual que las listas de LogrosFiltrados, ParchesInlineados...: el tipo es un Func que se evalúa al aplicar
        // Same as the lists of LogrosFiltrados, ParchesInlineados...: the type is a Func evaluated when applying
        var lista = new List<(string nombre, Func<Type> tipo)>
        {
            ("existe", () => typeof(string)),
            ("movido", () => throw new TypeLoadException("Could not load type 'Memori.Steamworks.SteamAchievements'")),
            ("nulo", () => null),
            ("otro", () => typeof(int)),
        };
        var aplicados = new List<string>();
        var fallidos = new List<string>();
        foreach (var p in lista)
        {
            // El código real de Plugin.Parchear (LogicaCarga.AplicarUno): resolver dentro de su try; aquí el método es ToString
            // The real code of Plugin.Parchear (LogicaCarga.AplicarUno): resolve inside its try; here the method is ToString
            var e = LogicaCarga.AplicarUno(p.tipo, "ToString", null, (t, m, a) => t.GetMethod(m, Type.EmptyTypes), mi => aplicados.Add(p.nombre), out _);
            if (e != null) fallidos.Add(p.nombre);
        }
        comprobar(aplicados.Count == 2 && aplicados[0] == "existe" && aplicados[1] == "otro",
            "lista diferida: los parches con su tipo se aplican aunque otro de la lista falle");
        comprobar(fallidos.Count == 2 && fallidos[0] == "movido" && fallidos[1] == "nulo",
            "lista diferida: fallan solo el parche cuyo tipo lanza y el que no encuentra el tipo");
    }

    // ---------------------------------------------------------------
    // Tipos por nombre
    // Types by name
    // ---------------------------------------------------------------
    private static void PruebaResolverTipo(Action<bool, string> comprobar)
    {
        Func<string, Type> calificado = n => Type.GetType(n, false);
        // Buscador por nombre completo simulado (en el juego, AccessTools.TypeByName sobre los ensamblados cargados)
        // Simulated finder by full name (in the game, AccessTools.TypeByName over the loaded assemblies)
        var conocidos = new Dictionary<string, Type> { { "Prueba.SoloPorNombre", typeof(Uri) } };
        Func<string, Type> porNombre = n => conocidos.TryGetValue(n, out var t) ? t : null;

        comprobar(LogicaCarga.ResolverTipo("System.Text.StringBuilder, System.Runtime", calificado, porNombre) == typeof(System.Text.StringBuilder),
            "tipos: un nombre con su ensamblado se encuentra");
        comprobar(LogicaCarga.ResolverTipo("No.Existe, NoHayEnsamblado|System.Text.StringBuilder, System.Runtime", calificado, porNombre) == typeof(System.Text.StringBuilder),
            "tipos: si el primer candidato no existe se prueba el siguiente");
        comprobar(LogicaCarga.ResolverTipo("Prueba.SoloPorNombre, EnsambladoQueNoEsta", calificado, porNombre) == typeof(Uri),
            "tipos: si el ensamblado no está, se busca por el nombre completo (parte antes de la coma)");
        comprobar(LogicaCarga.ResolverTipo("No.Existe, Nada| Otro.Tampoco ", calificado, porNombre) == null,
            "tipos: sin ningún candidato encontrado devuelve null (el parche avisa y no se aplica)");
        comprobar(LogicaCarga.ResolverTipo("", calificado, porNombre) == null && LogicaCarga.ResolverTipo(null, calificado, porNombre) == null,
            "tipos: una lista vacía o null devuelve null");
        comprobar(LogicaCarga.ResolverTipo("Prueba.SoloPorNombre", n => throw new System.IO.FileLoadException("roto"), porNombre) == typeof(Uri),
            "tipos: un buscador que lanza cuenta como «no encontrado» y se sigue con el otro");

        // Caso real del juego 1.11.1: HttpAnalyticsBackend. Los mismos candidatos que Plugin.TipoHttpAnalyticsBackend
        // Real case from game 1.11.1: HttpAnalyticsBackend. The same candidates as Plugin.TipoHttpAnalyticsBackend
        const string candidatos = "Memori.Analytics.HttpAnalyticsBackend, Memori.Analytics|TabletopTavern.Analytics.HttpAnalyticsBackend, TabletopTavern.Core";
        Type nuevo = typeof(Version), viejo = typeof(Guid);
        Func<string, Type> juego111 = n => n == "Memori.Analytics.HttpAnalyticsBackend, Memori.Analytics" ? nuevo : null;
        Func<string, Type> juego11017 = n => n == "TabletopTavern.Analytics.HttpAnalyticsBackend, TabletopTavern.Core" ? viejo : null;
        comprobar(LogicaCarga.ResolverTipo(candidatos, juego111, n => null) == nuevo,
            "telemetría: con el interop del juego 1.11.1 se usa Memori.Analytics.HttpAnalyticsBackend");
        comprobar(LogicaCarga.ResolverTipo(candidatos, juego11017, n => null) == viejo,
            "telemetría: con un interop como el de 1.10.17 se usa la ubicación vieja de respaldo");
        comprobar(LogicaCarga.ResolverTipo(candidatos, n => null, n => null) == null,
            "telemetría: si no está en ninguna de las dos, null (aviso «Parche no aplicado», el núcleo sigue)");
    }
}
