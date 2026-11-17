// LogicaCarga.cs
// Carga tolerante del núcleo (juego 1.11.1, 01-11-2026), sin tipos del juego ni de Unity para poder probarla fuera del
// juego (proyecto pruebas\, PruebasCarga.cs).
// Por qué existe: en la 1.11.1 el juego movió HttpAnalyticsBackend de TabletopTavern.Analytics (TabletopTavern.Core.dll) a
// Memori.Analytics (Memori.Analytics.dll). AplicarParchesTelemetria lo nombraba con typeof(...): el JIT resuelve todos los
// tipos de un método al compilarlo, antes de ejecutar su primera instrucción, así que el TypeLoadException salió «desde»
// ese método sin pasar por su try/catch y tumbó Load() entero (ni facciones, ni héroes, ni modelos). Dos defensas:
//   1) Fases: cada bloque independiente de Load() se llama a través de Fase(...), con el try fuera del método que nombra
//      los tipos; un fallo de JIT o de carga de tipo en una fase queda en esa fase y las demás siguen.
//   2) Tipos por nombre: ResolverTipo busca un tipo por una lista de nombres candidatos (con su ensamblado, y si no, por el
//      nombre completo en cualquier ensamblado cargado), para los parches cuyo tipo ya se ha movido alguna vez.
// Autor: ZeroitMods.
// LogicaCarga.cs
// Tolerant loading of the core (game 1.11.1, 01-11-2026), with no game or Unity types so it can be tested outside the
// game (project pruebas\, PruebasCarga.cs).
// Why it exists: in 1.11.1 the game moved HttpAnalyticsBackend from TabletopTavern.Analytics (TabletopTavern.Core.dll) to
// Memori.Analytics (Memori.Analytics.dll). AplicarParchesTelemetria named it with typeof(...): the JIT resolves all the
// types of a method when compiling it, before running its first instruction, so the TypeLoadException came out "from"
// that method without going through its try/catch and brought down the whole Load() (no factions, no heroes, no models). Two defenses:
//   1) Phases: each independent block of Load() is called through Fase(...), with the try outside the method that names
//      the types; a JIT or type-load failure in a phase stays in that phase and the others carry on.
//   2) Types by name: ResolverTipo looks up a type from a list of candidate names (with its assembly, and if that fails, by the
//      full name in any loaded assembly), for the patches whose type has already been moved at some point.
// Author: ZeroitMods.

using System;
using System.Reflection;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: fases de carga y resolución de tipos por nombre
    // Section: loading phases and type resolution by name
    // -----------------------------------------------------------------------
    public static class LogicaCarga
    {
        /// <summary>
        /// Ejecuta una fase de la carga. Devuelve true si terminó; si lanza (también un TypeLoadException o un
        /// MissingMethodException del JIT al entrar en el método de la fase), llama a <paramref name="alFallar"/> con el
        /// nombre y la excepción y devuelve false. Nunca deja escapar la excepción. Un fallo dentro de alFallar se ignora.
        /// Runs a loading phase. Returns true if it finished; if it throws (including a TypeLoadException or a
        /// MissingMethodException from the JIT when entering the phase method), it calls <paramref name="alFallar"/> with the
        /// name and the exception and returns false. It never lets the exception escape. A failure inside alFallar is ignored.
        /// </summary>
        public static bool Fase(string nombre, Action accion, Action<string, Exception> alFallar)
        {
            try
            {
                accion?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                try { alFallar?.Invoke(nombre, e); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Busca un tipo por una lista de candidatos separados por «|», en orden. Cada candidato es «Espacio.Tipo, Ensamblado»
        /// o solo «Espacio.Tipo». Para cada uno prueba primero <paramref name="porNombreCalificado"/> con el candidato entero
        /// (en el juego, Type.GetType, que sí dispara la carga del ensamblado del interop) y después
        /// <paramref name="porNombreCompleto"/> con la parte anterior a la coma (en el juego, AccessTools.TypeByName, que solo
        /// mira los ensamblados ya cargados). Una excepción de un buscador cuenta como «no encontrado». Null si ninguno.
        /// Looks up a type from a list of candidates separated by "|", in order. Each candidate is "Namespace.Type, Assembly"
        /// or just "Namespace.Type". For each one it first tries <paramref name="porNombreCalificado"/> with the whole candidate
        /// (in the game, Type.GetType, which does trigger loading of the interop assembly) and then
        /// <paramref name="porNombreCompleto"/> with the part before the comma (in the game, AccessTools.TypeByName, which only
        /// looks at already loaded assemblies). An exception from a finder counts as "not found". Null if none.
        /// </summary>
        public static Type ResolverTipo(string candidatos, Func<string, Type> porNombreCalificado, Func<string, Type> porNombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(candidatos)) return null;
            foreach (string bruto in candidatos.Split('|'))
            {
                string candidato = bruto.Trim();
                if (candidato.Length == 0) continue;
                Type t = Probar(porNombreCalificado, candidato);
                if (t != null) return t;
                int coma = candidato.IndexOf(',');
                string completo = coma < 0 ? candidato : candidato.Substring(0, coma).Trim();
                t = Probar(porNombreCompleto, completo);
                if (t != null) return t;
            }
            return null;
        }

        // Llama a un buscador y convierte cualquier excepción en «no encontrado»
        // Calls a finder and turns any exception into "not found"
        private static Type Probar(Func<string, Type> buscador, string nombre)
        {
            if (buscador == null || string.IsNullOrEmpty(nombre)) return null;
            try { return buscador(nombre); }
            catch { return null; }
        }

        /// <summary>
        /// Revisión del 01-11-2026: un parche de Plugin.Parchear, paso a paso y sin dejar escapar nada. Resuelve el tipo
        /// diferido (null = TypeLoadException), los argumentos diferidos, busca el método con <paramref name="buscar"/> (en el
        /// juego, AccessTools.Method; null = MissingMethodException) y lo pasa a <paramref name="aplicar"/> (en el juego,
        /// Harmony.Patch). Devuelve null si todo fue bien o la excepción del paso que falló; <paramref name="tipo"/> queda con
        /// el tipo resuelto (o null) para el texto del log.
        /// Review of 01-11-2026: a patch from Plugin.Parchear, step by step and without letting anything escape. It resolves the
        /// deferred type (null = TypeLoadException), the deferred arguments, looks up the method with <paramref name="buscar"/> (in the
        /// game, AccessTools.Method; null = MissingMethodException) and passes it to <paramref name="aplicar"/> (in the game,
        /// Harmony.Patch). Returns null if everything went well or the exception from the step that failed; <paramref name="tipo"/> is left with
        /// the resolved type (or null) for the log text.
        /// </summary>
        public static Exception AplicarUno(Func<Type> tipoDiferido, string metodo, Func<Type[]> argumentosDiferidos,
                                           Func<Type, string, Type[], MethodInfo> buscar, Action<MethodInfo> aplicar, out Type tipo)
        {
            tipo = null;
            try
            {
                tipo = tipoDiferido?.Invoke();
                if (tipo == null) throw new TypeLoadException("no se encuentra el tipo del juego (¿lo movió o quitó una actualización?)");
                Type[] argumentos = argumentosDiferidos?.Invoke();
                MethodInfo original = buscar?.Invoke(tipo, metodo, argumentos);
                if (original == null) throw new MissingMethodException(tipo.FullName, metodo);
                aplicar?.Invoke(original);
                return null;
            }
            catch (Exception e) { return e; }
        }

        /// <summary>Texto corto de una excepción para el log: tipo, mensaje y, si la hay, la interna.</summary>
        /// Short text of an exception for the log: type, message and, if there is one, the inner exception.
        public static string Describir(Exception e)
        {
            if (e == null) return "(sin excepción)";
            string t = e.GetType().Name + ": " + e.Message;
            if (e.InnerException != null) t += " (" + e.InnerException.GetType().Name + ": " + e.InnerException.Message + ")";
            return t;
        }
    }
}
