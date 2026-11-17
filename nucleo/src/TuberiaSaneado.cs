// TuberiaSaneado.cs
// Tubería completa del saneado de saves (0.7.5, ronda 2 de la revisión independiente): todas las etapas en el mismo orden
// que Saves.SanearSalida (unidades, razas, objetos, héroes, historial y hechizos) y la vuelta en el de Saves.AntesDeFromJson.
// .NET puro (sin tipos del juego): la usan el vigilante de saves (Saves.cs) y las pruebas fuera del juego
// (pruebas\PruebasRevisionTotal.cs, que pasan por aquí los saves reales copiados). Saves.SanearSalida sigue llamando etapa
// a etapa para escribir en el log qué cambió cada una, con este mismo orden.
// Autor: ZeroitMods.
// TuberiaSaneado.cs
// Full save sanitizing pipeline (0.7.5, round 2 of the independent review): all stages in the same order
// as Saves.SanearSalida (units, races, gear, heroes, history and spells) and the way back in that of Saves.AntesDeFromJson.
// Pure .NET (no game types): used by the save watcher (Saves.cs) and the out-of-game tests
// (pruebas\PruebasRevisionTotal.cs, which run copied real saves through here). Saves.SanearSalida still calls stage
// by stage to log what each one changed, in this same order.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: tablas del saneado y tubería de ida y vuelta
    // Section: sanitizing tables and round-trip pipeline
    // -----------------------------------------------------------------------
    /// <summary>Las tablas que usa el saneado (cualquiera puede ser null: esa etapa no hace nada).</summary>
    /// The tables used by sanitizing (any of them may be null: that stage does nothing).
    public sealed class TablasSaneado
    {
        public TablaUnidades Unidades;
        public TablaRazas Razas;              // razas de las facciones activas (ciudad, campo, temas; vuelta del historial)
                                              // races of the active factions (city, field, themes; history way back)
        public TablaRazas RazasHistorial;     // razas de todos los módulos instalados (ida del historial); null = Razas
                                              // races of all installed modules (history way out); null = Razas
        public TablaObjetos Objetos;
        public bool ObjetosActivos;           // ¿se restauran / olvidan los objetos? (Objetos.ActivosEnCache)
                                              // are the items restored / forgotten? (Objetos.ActivosEnCache)
        public TablaHeroes Heroes;
        public Func<int, bool> HechizoDisponible;
        internal TablaRazas RazasIda => RazasHistorial ?? Razas;
    }

    public static class TuberiaSaneado
    {
        /// <summary>Ida al disco: el texto sin ningún id nuevo, apuntando lo necesario en el registro.</summary>
        /// Way to disk: the text without any new id, noting down what is needed in the core registry.
        public static string ParaDisco(string json, TablasSaneado t, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || t == null || registro == null) return json;
            string r = t.Unidades == null ? json : SaneadoSaves.ParaDisco(json, t.Unidades, registro);
            r = SaneadoSaves.RazasParaDisco(r, t.Razas, registro);
            r = SaneadoObjetos.ParaDisco(r, t.Objetos, registro, t.ObjetosActivos);
            r = SaneadoSaves.HeroesParaDisco(r, t.Heroes, registro);
            r = SaneadoSaves.HistorialParaDisco(r, t.Heroes, t.RazasIda, registro);
            return SaneadoHechizos.ParaDisco(r, registro);
        }

        /// <summary>Vuelta del disco: lo apuntado de los módulos activos vuelve a su sitio.</summary>
        /// Way back from disk: what was noted down for the active modules goes back to its place.
        public static string DesdeDisco(string json, TablasSaneado t, RegistroGuardias registro)
        {
            if (string.IsNullOrEmpty(json) || t == null || registro == null) return json;
            string r = t.Unidades == null ? json : SaneadoSaves.DesdeDisco(json, t.Unidades, registro);
            if (t.Razas != null) r = SaneadoSaves.RazasDesdeDisco(r, t.Razas, registro);
            if (t.Objetos != null && t.ObjetosActivos) r = SaneadoObjetos.DesdeDisco(r, t.Objetos, registro);
            r = SaneadoSaves.HeroesDesdeDisco(r, t.Heroes, registro);
            r = SaneadoSaves.HistorialDesdeDisco(r, t.Heroes, t.Razas, registro);
            return t.HechizoDisponible == null ? r : SaneadoHechizos.DesdeDisco(r, registro, t.HechizoDisponible);
        }

        /// <summary>Ids nuevos que quedan en un texto, por tipo (todo a 0 tras ParaDisco).</summary>
        /// New ids left in a text, by type (all at 0 after ParaDisco).
        public static Dictionary<string, int> ResiduosPorTipo(string texto, TablasSaneado t) => new Dictionary<string, int>
        {
            { "unidad(es)", SaneadoSaves.ContarResiduos(texto, t?.Unidades) },
            { "raza(s)", SaneadoSaves.ContarResiduosRazas(texto, t?.Razas) },
            { "objeto(s)", SaneadoObjetos.ContarResiduos(texto, t?.Objetos) },
            { "héroe(s)", SaneadoSaves.ContarResiduosHeroes(texto, t?.Heroes) },
            { "en el historial", SaneadoSaves.ContarResiduosHistorial(texto, t?.Heroes, t?.RazasIda) },
            { "hechizo(s) propio(s)", SaneadoHechizos.ContarResiduos(texto) },
        };
    }
}
