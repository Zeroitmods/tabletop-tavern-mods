// LogicaInlineados.cs
// Cálculos puros (sin tipos del juego ni de Unity) de ParchesInlineados.cs, para poder probarlos fuera del juego
// (proyecto pruebas\, PruebasInlineados.cs). ParchesInlineados.cs rodea los métodos que el compilador IL2CPP copió
// dentro de sus llamadores (notas internas (no publicadas)): se parchea el llamador, que abre un contexto,
// y los métodos que ese llamador sí llama de verdad leen el contexto para corregir lo que la copia hizo de serie.
// Aquí están el contexto anidado, las decisiones de raza y región y el descuento de bajas de las unidades 9xx en la
// estadística de Steam UnitKills (misma regla que LogicaLogros: las 9xx no suman).
// Autor: ZeroitMods.
// LogicaInlineados.cs
// Pure calculations (no game or Unity types) from ParchesInlineados.cs, so they can be tested outside the game
// (project pruebas\, PruebasInlineados.cs). ParchesInlineados.cs wraps the methods that the IL2CPP compiler copied
// inside their callers (internal notes (unpublished)): the caller is patched, which opens a context,
// and the methods that this caller really does call read the context to fix what the copy did by default.
// Here are the nested context, the race and region decisions and the discount of kills of 9xx units in the
// Steam stat UnitKills (same rule as LogicaLogros: 9xx units do not count).
// Author: ZeroitMods.

using System.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: contexto anidado (lo abre el prefix de un método y lo cierra su finalizer)
    // Section: nested context (opened by a method's prefix and closed by its finalizer)
    // -----------------------------------------------------------------------
    /// <summary>
    /// Contexto de un método en curso con su valor. Abrir guarda el valor de fuera en una pila y Cerrar lo devuelve, así
    /// una llamada anidada no deja el contexto de fuera vacío. Cerrar sin haber abierto no falla (deja el contexto cerrado).
    /// Solo hilo principal, como los métodos del juego que lo usan.
    /// Context of a method in progress with its value. Opening saves the outer value on a stack and Closing restores it, so
    /// a nested call does not leave the outer context empty. Closing without having opened does not fail (it leaves the context closed).
    /// Main thread only, like the game methods that use it.
    /// </summary>
    public sealed class ContextoAnidado<T>
    {
        private readonly Stack<(bool abierto, T valor)> _antes = new Stack<(bool, T)>();

        /// <summary>true = hay un método en curso que abrió el contexto.</summary>
        /// true = there is a method in progress that opened the context.
        public bool Abierto { get; private set; }
        /// <summary>Valor del contexto abierto (el por defecto de T si está cerrado).</summary>
        /// Value of the open context (the default of T if it is closed).
        public T Valor { get; private set; }
        /// <summary>Profundidad de anidamiento (0 = cerrado del todo); para las pruebas y para detectar fugas.</summary>
        /// Nesting depth (0 = fully closed); for tests and for detecting leaks.
        public int Profundidad => _antes.Count;

        /// <summary>Abre el contexto con un valor y guarda el que hubiera antes.</summary>
        /// Opens the context with a value and saves the previous one, if any.
        public void Abrir(T valor)
        {
            _antes.Push((Abierto, Valor));
            Abierto = true;
            Valor = valor;
        }

        /// <summary>Cierra el contexto y devuelve el de fuera (o lo deja cerrado si no había ninguno).</summary>
        /// Closes the context and returns the outer one (or leaves it closed if there was none).
        public void Cerrar()
        {
            if (_antes.Count == 0) { Abierto = false; Valor = default; return; }
            var (abierto, valor) = _antes.Pop();
            Abierto = abierto;
            Valor = valor;
        }
    }

    // -----------------------------------------------------------------------
    // Sección: decisiones de los arreglos de inlining
    // Section: decisions for the inlining fixes
    // -----------------------------------------------------------------------
    public static class LogicaInlineados
    {
        /// <summary>Valor de «sin raza nueva» en los contextos de ciudad y de nodo.</summary>
        /// Value of «no new race» in the town and node contexts.
        public const int SinRaza = -1;

        /// <summary>
        /// Raza nueva de la ciudad: la que devuelve GenerateTownRace llamado desde el plugin (pasa por el parche de las
        /// ciudades) si es una raza nueva; si es de serie, SinRaza (la copia inlineada del juego ya acierta).
        /// New race of the town: the one returned by GenerateTownRace when called from the plugin (it goes through the town
        /// patch) if it is a new race; if it is a stock one, SinRaza (the game's inlined copy already gets it right).
        /// </summary>
        public static int RazaCiudadNueva(int calculada, System.Func<int, bool> esNueva)
            => esNueva != null && esNueva(calculada) ? calculada : SinRaza;

        /// <summary>
        /// Raza que debe recibir GetSquadsWithTiersFromRace: la nueva de la ciudad si hay contexto de GenerateTown con
        /// raza nueva; si no, la pedida (fuera de GenerateTown lo llaman PrepareBattle y las ofertas de reclutamiento).
        /// Race that GetSquadsWithTiersFromRace should receive: the town's new one if there is a GenerateTown context with a
        /// new race; otherwise, the requested one (outside GenerateTown it is called by PrepareBattle and the recruitment offers).
        /// </summary>
        public static int RazaGuarnicion(int pedida, bool hayContexto, int razaContexto)
            => hayContexto && razaContexto != SinRaza ? razaContexto : pedida;

        /// <summary>
        /// ¿Hay que cambiar la región que llega a GenerateNodeWeather / GenerateNodeBiome? Solo dentro de MapNode.SetUp
        /// (contexto abierto) y si el nodo es de una raza nueva; fuera (TownPanel, EngagementPanel) la región ya llega bien.
        /// Must the region that reaches GenerateNodeWeather / GenerateNodeBiome be changed? Only inside MapNode.SetUp
        /// (context open) and if the node is of a new race; outside (TownPanel, EngagementPanel) the region already arrives correctly.
        /// </summary>
        public static bool CambiarRegion(bool hayContexto, int razaNodo, System.Func<int, bool> esNueva)
            => hayContexto && esNueva != null && esNueva(razaNodo);

        /// <summary>
        /// Bajas hechas en la batalla por escuadras del jugador de unidades nuevas (9xx): las que no deben sumar a la
        /// estadística UnitKills de Steam. Entradas (unidad de la escuadra, bajas de la escuadra); las negativas no cuentan.
        /// Kills made in battle by player squads of new units (9xx): those that must not count toward the
        /// Steam UnitKills stat. Entries (squad unit, squad kills); negative ones are not counted.
        /// </summary>
        public static int BajasNuevas(IEnumerable<(int unidad, int bajas)> escuadras)
        {
            // se acumula en long y se topa en int.MaxValue: con contadores anómalos no da la vuelta a negativo
            // accumulates in a long and is capped at int.MaxValue: with anomalous counters it does not wrap around to negative
            long total = 0;
            if (escuadras == null) return 0;
            foreach (var (unidad, bajas) in escuadras)
                if (!LogicaLogros.EsUnidadDeSerie(unidad) && bajas > 0) total += bajas;
            return (int)System.Math.Min(total, int.MaxValue);
        }

        /// <summary>
        /// Descuenta del valor que el juego va a sumar a la estadística lo que quede pendiente de las bajas nuevas, sin
        /// bajar de 0, y rebaja lo pendiente. El juego suma escuadra a escuadra; el total que llega a Steam al acabar es
        /// el de la batalla sin las bajas nuevas, y por el camino nunca pasa de él (el umbral de un logro no se cruza antes).
        /// Subtracts whatever is still pending from the new kills from the value the game is about to add to the stat, without
        /// going below 0, and reduces the pending amount. The game adds squad by squad; the total that reaches Steam at the end is
        /// that of the battle without the new kills, and along the way it never exceeds it (an achievement threshold is never crossed early).
        /// </summary>
        public static int Descontar(int valor, ref int pendiente)
        {
            if (valor <= 0 || pendiente <= 0) return valor;
            int d = valor < pendiente ? valor : pendiente;
            pendiente -= d;
            return valor - d;
        }
    }
}
