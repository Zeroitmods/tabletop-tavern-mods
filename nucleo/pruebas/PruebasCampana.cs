// PruebasCampana.cs
// Prueba 13 (17-10-2026): los héroes nuevos fuera de su pantalla (LogicaCampanaHeroes.cs).
//   - Figura del mapa de campaña (PlayerToken.heroPrefabs, una por héroe de serie): los héroes de serie siguen igual, los
//     nuevos usan la de su héroe prestado y, si no cabe, la de Edric; nunca un índice fuera del array.
//   - Assets de héroe sin entrada (módulo apagado): se pide el prestado o Edric; con entrada, el mismo.
//   - Contador «hechas/total» del botón Colección con las razas nuevas.
//   - Array de figuras ampliado (LoadHero está inlineado en MapSceneManager.CompleteLoad): longitud necesaria, figura de
//     cada hueco nuevo (la del prestado de ese id), hueco vacío rellenado, tope de id y que LoadHero nunca lea fuera ni nulo.
//   - Manifiesto real (si se pasa la carpeta de módulos): prestados 901->1, 902->2, 903->10, 904->12 y sus figuras.
// Autor: ZeroitMods.
// PruebasCampana.cs
// Test 13 (17-10-2026): the new heroes outside their screen (LogicaCampanaHeroes.cs).
//   - Campaign map figure (PlayerToken.heroPrefabs, one per base-game hero): base-game heroes stay the same, the
//     new ones use that of their borrowed hero and, if it doesn't fit, Edric's; never an index outside the array.
//   - Hero assets with no entry (module off): the borrowed one or Edric is requested; with an entry, the same one.
//   - «done/total» counter of the Collection button with the new races.
//   - Extended figure array (LoadHero is inlined in MapSceneManager.CompleteLoad): required length, figure of
//     each new slot (that of the borrowed hero of that id), empty slot filled, id cap and that LoadHero never reads out of range or null.
//   - Real manifest (if the modules folder is passed): borrowed 901->1, 902->2, 903->10, 904->12 and their figures.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroNucleo;

internal static class PruebasCampana
{
    // Prestados del diseño (los del manifiesto de cada módulo)
    // Borrowed heroes by design (those in each module's manifest)
    private static readonly Dictionary<int, int> Prestados = new Dictionary<int, int> { { 901, 1 }, { 902, 2 }, { 903, 10 }, { 904, 12 } };
    private static int Prestado(int id) => Prestados.TryGetValue(id, out int p) ? p : -1;

    public static void Todas(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        // -------- Figura del mapa (índice desde 0 en un array de 16 figuras de serie)
        // -------- Map figure (0-based index in an array of 16 base-game figures)
        comprobar(LogicaCampanaHeroes.IndiceFigura(7, 16, Prestado, 1) == 6 && !LogicaCampanaHeroes.FueraDeRango(7, 16),
            "mapa: héroe de serie 7 -> figura 6 (sin cambios)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(-1, 16, Prestado, 1) == 0 && !LogicaCampanaHeroes.FueraDeRango(-1, 16),
            "mapa: sin héroe activo (-1) -> figura 0, como el juego");
        comprobar(LogicaCampanaHeroes.IndiceFigura(16, 16, Prestado, 1) == 15, "mapa: último héroe de serie (16) -> figura 15");
        comprobar(LogicaCampanaHeroes.FueraDeRango(901, 16) && LogicaCampanaHeroes.IndiceFigura(901, 16, Prestado, 1) == 0,
            "mapa: Yarrick (901) -> figura de Edric (0)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(902, 16, Prestado, 1) == 1, "mapa: Creed (902) -> figura del héroe 2 (1)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(903, 16, Prestado, 1) == 9, "mapa: Vader (903) -> figura del héroe 10 (9)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(904, 16, Prestado, 1) == 11, "mapa: Thrawn (904) -> figura del héroe 12 (11)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(904, 8, Prestado, 1) == 0,
            "mapa: prestado que no cabe (12 en 8 figuras) -> figura de Edric");
        comprobar(LogicaCampanaHeroes.IndiceFigura(905, 16, Prestado, 1) == 0, "mapa: héroe nuevo desconocido -> figura de Edric");
        comprobar(LogicaCampanaHeroes.IndiceFigura(17, 16, Prestado, 1) == 0, "mapa: id 17 con 16 figuras -> figura de Edric");
        comprobar(LogicaCampanaHeroes.IndiceFigura(0, 16, Prestado, 1) == 0 && LogicaCampanaHeroes.FueraDeRango(0, 16),
            "mapa: id 0 (fuera del array) -> figura de Edric");
        comprobar(LogicaCampanaHeroes.IndiceFigura(901, 16, Prestado, 99) == 0, "mapa: por defecto fuera de rango -> primera figura");
        comprobar(LogicaCampanaHeroes.IndiceFigura(901, 0, Prestado, 1) == -1, "mapa: sin figuras -> -1 (no se enciende nada)");
        comprobar(LogicaCampanaHeroes.IndiceFigura(901, 16, null, 1) == 0, "mapa: sin tabla de prestados -> figura de Edric");
        comprobar(LogicaCampanaHeroes.IndiceFigura(903, 16, id => throw new InvalidOperationException("tabla rota"), 1) == 0,
            "mapa: tabla de prestados que falla -> figura de Edric (sin excepción)");
        // Ningún id de -5 a 1000 da un índice fuera del array
        // No id from -5 to 1000 gives an index outside the array
        bool siempreDentro = true;
        for (int id = -5; id <= 1000; id++)
        {
            int i = LogicaCampanaHeroes.IndiceFigura(id, 16, Prestado, 1);
            if (i < 0 || i >= 16) { siempreDentro = false; break; }
        }
        comprobar(siempreDentro, "mapa: ningún id de -5 a 1000 da una figura fuera del array de 16");

        // -------- Array de figuras ampliado (LoadHero inlineado en CompleteLoad: se amplía heroPrefabs antes de que corra)
        // -------- Extended figure array (LoadHero inlined in CompleteLoad: heroPrefabs is extended before it runs)
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(7, 16) == 16, "ampliar: héroe de serie 7 con 16 figuras -> sin cambios");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(-1, 16) == 16, "ampliar: sin héroe activo (-1 = héroe 1) -> sin cambios");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(901, 16) == 901, "ampliar: Yarrick (901) -> 901 figuras");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(903, 904) == 904, "ampliar: Vader con el array ya ampliado a 904 -> sin cambios");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(0, 16) == -1 && LogicaCampanaHeroes.LongitudNecesaria(-7, 16) == -1,
            "ampliar: id 0 o negativo -> sin arreglo (-1)");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(1000, 16) == -1 && LogicaCampanaHeroes.LongitudNecesaria(int.MaxValue, 16) == -1,
            "ampliar: id por encima del tope (999) -> sin arreglo, nunca un array enorme");
        comprobar(LogicaCampanaHeroes.LongitudNecesaria(901, 0) == 901, "ampliar: sin figuras -> 901 (sin origen; el plugin lo avisa)");

        // Simulación del arreglo del plugin (CampanaHeroes.AsegurarFiguraEnMapa) con nombres en vez de GameObjects
        // Simulation of the plugin's fix (CampanaHeroes.AsegurarFiguraEnMapa) using names instead of GameObjects
        string[] Asegurar(string[] figuras, int heroe)
        {
            int longitud = figuras.Length;
            int necesaria = LogicaCampanaHeroes.LongitudNecesaria(heroe, longitud);
            if (necesaria < 0) return figuras;
            bool Llena(int i) => i >= 0 && i < longitud && figuras[i] != null;
            string Origen(int id) { int o = LogicaCampanaHeroes.OrigenFigura(id, longitud, Llena, Prestado, 1); return o >= 0 ? figuras[o] : null; }
            var destino = figuras;
            if (necesaria > longitud)
            {
                destino = new string[necesaria];
                Array.Copy(figuras, destino, longitud);
                for (int i = longitud; i < necesaria; i++) destino[i] = Origen(i + 1);
            }
            int hueco = (heroe == -1 ? 1 : heroe) - 1;
            if (destino[hueco] == null) destino[hueco] = Origen(hueco + 1);
            return destino;
        }
        var serie16 = Enumerable.Range(1, 16).Select(i => "figura" + i).ToArray();
        var a901 = Asegurar(serie16, 901);
        comprobar(a901.Length == 901 && a901[900] == "figura1" && a901.All(f => f != null) && a901.Take(16).SequenceEqual(serie16),
            "ampliar: Yarrick -> heroPrefabs[900] = figura de Edric, las 16 de serie intactas y ningún hueco nulo");
        var a904 = Asegurar(serie16, 904);
        comprobar(a904.Length == 904 && a904[903] == "figura12" && a904[902] == "figura10" && a904[901] == "figura2" && a904[900] == "figura1",
            "ampliar: Thrawn (904) -> sus huecos 901-904 con las figuras de sus prestados (1, 2, 10, 12)");
        var a903 = Asegurar(a904, 903);
        comprobar(ReferenceEquals(a903, a904) && a903[902] == "figura10", "ampliar: Vader tras ampliar para Thrawn -> mismo array, figura del héroe 10");
        comprobar(ReferenceEquals(Asegurar(serie16, 7), serie16), "ampliar: héroe de serie -> mismo array, sin tocar");
        var conHueco = (string[])a904.Clone(); conHueco[901] = null;
        var relleno = Asegurar(conHueco, 902);
        comprobar(ReferenceEquals(relleno, conHueco) && relleno[901] == "figura2", "ampliar: hueco vacío dentro del array (902) -> se rellena sin ampliar");
        var sinEdric = (string[])serie16.Clone(); sinEdric[0] = null;
        comprobar(Asegurar(sinEdric, 901)[900] == "figura2", "ampliar: Edric vacío -> primera figura llena (la 2)");
        var vacias = new string[16];
        comprobar(Asegurar(vacias, 901)[900] == null && LogicaCampanaHeroes.OrigenFigura(901, 16, i => false, Prestado, 1) == -1,
            "ampliar: sin ninguna figura -> sin origen (-1)");
        comprobar(LogicaCampanaHeroes.OrigenFigura(902, 16, i => throw new InvalidOperationException("roto"), Prestado, 1) == -1,
            "ampliar: comprobación de figura que falla -> sin origen, sin excepción");
        // Barrido: para cualquier id de -1 a 999 (menos 0) el hueco que leerá LoadHero existe y tiene figura
        // Sweep: for any id from -1 to 999 (except 0) the slot that LoadHero will read exists and has a figure
        bool siempreConFigura = true;
        for (int id = -1; id <= LogicaCampanaHeroes.TopeHeroeMapa; id++)
        {
            if (id == 0) continue;
            var r = Asegurar(serie16, id);
            int h = (id == -1 ? 1 : id) - 1;
            if (h >= r.Length || r[h] == null) { siempreConFigura = false; break; }
        }
        comprobar(siempreConFigura, "ampliar: ningún id de -1 a 999 deja a LoadHero fuera del array o con figura nula");

        // -------- Assets de héroe sin entrada (entradas de serie 1-16; con el módulo activo, también 901-904)
        // -------- Hero assets with no entry (stock entries 1-16; with the module enabled, also 901-904)
        var deSerie = new HashSet<int>(Enumerable.Range(1, 16));
        var conModulo = new HashSet<int>(deSerie.Concat(Prestados.Keys));
        comprobar(LogicaCampanaHeroes.HeroeConAssets(5, deSerie.Contains, Prestado, 1) == 5, "assets: héroe de serie -> el mismo");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(903, conModulo.Contains, Prestado, 1) == 903, "assets: Vader con su módulo activo -> el mismo (retrato propio)");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(903, deSerie.Contains, Prestado, 1) == 10, "assets: Vader con el módulo apagado -> héroe 10");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(901, deSerie.Contains, Prestado, 1) == 1, "assets: Yarrick con el módulo apagado -> Edric");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(950, deSerie.Contains, Prestado, 1) == 1, "assets: héroe desconocido -> Edric");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(904, new HashSet<int> { 1 }.Contains, Prestado, 1) == 1,
            "assets: prestado sin entrada -> Edric");
        comprobar(LogicaCampanaHeroes.HeroeConAssets(904, new HashSet<int>().Contains, Prestado, 1) == 904,
            "assets: nada con entrada -> el mismo (decide el juego)");

        // -------- Contador del botón Colección
        // -------- Collection button counter
        comprobar(LogicaCampanaHeroes.SumarAContador("120/200", 3, 25, out int h1, out int t1) && h1 == 123 && t1 == 225,
            "contador: 120/200 + 3/25 -> 123/225");
        comprobar(LogicaCampanaHeroes.SumarAContador(" 0 / 140 ", 0, 25, out int h2, out int t2) && h2 == 0 && t2 == 165,
            "contador: con espacios y nada recogido -> 0/165");
        comprobar(!LogicaCampanaHeroes.SumarAContador("<color=#fff>120/200</color>", 3, 25, out _, out _),
            "contador: texto con etiquetas -> no se toca");
        comprobar(!LogicaCampanaHeroes.SumarAContador("120/200", 0, 0, out _, out _), "contador: nada que sumar -> no se toca");
        comprobar(!LogicaCampanaHeroes.SumarAContador("120/200", 5, 3, out _, out _), "contador: más recogidas que total -> no se toca");
        comprobar(!LogicaCampanaHeroes.SumarAContador(null, 1, 2, out _, out _), "contador: sin texto -> no se toca");

        // -------- Actos en los que salen las razas nuevas como enemigas (0.7.6, prueba de Zero del 03-11-2026)
        // -------- Acts in which the new races appear as enemies (0.7.6, Zero's test of 03-11-2026)
        comprobar(LogicaCampanaHeroes.SaleEnActo(1, 2, new[] { 1, 2, 3 }) && LogicaCampanaHeroes.SaleEnActo(3, 2, new[] { 1, 2, 3 })
                  && !LogicaCampanaHeroes.SaleEnActo(4, 2, new[] { 1, 2, 3 }) && !LogicaCampanaHeroes.SaleEnActo(0, 2, new[] { 1, 2, 3 }),
            "actos: con la lista 1-3 sale en los tres actos (y en ninguno fuera de 1-3)");
        comprobar(LogicaCampanaHeroes.SaleEnActo(2, 2, null) && !LogicaCampanaHeroes.SaleEnActo(1, 2, null)
                  && LogicaCampanaHeroes.SaleEnActo(3, 3, new int[0]) && LogicaCampanaHeroes.SaleEnActo(3, 3, new[] { 7, 0 })
                  && !LogicaCampanaHeroes.SaleEnActo(1, 3, new[] { 7, 0 }),
            "actos: sin lista (o con números fuera de 1-3) vale el actoCampana de siempre");
        comprobar(!LogicaCampanaHeroes.SaleEnActo(2, 2, new[] { 1 }), "actos: si hay lista válida manda la lista, no el actoCampana");
        comprobar(Math.Abs(LogicaCampanaHeroes.ProbabilidadNuevas(2, 2) - 0.5) < 1e-9 && Math.Abs(LogicaCampanaHeroes.ProbabilidadNuevas(3, 1) - 0.25) < 1e-9
                  && LogicaCampanaHeroes.ProbabilidadNuevas(3, 0) == 0 && LogicaCampanaHeroes.ProbabilidadNuevas(-1, 1) == 1,
            "actos: cada raza nueva pesa como una de serie (acto 1: 2+2 -> 50 %; acto 2 con Creed: 3+1 -> 25 %)");
        // 0.7.11: con reglas 1 o más (orden abierto) la nueva pesa como cada raza del bombo abierto, no de la lista fija
        // 0.7.11: with rules 1 or more (open order) the new one weighs the same as each race in the open pool, not in the fixed list
        comprobar(LogicaCampanaHeroes.DeSerieParaPeso(0, 3, 7) == 3 && LogicaCampanaHeroes.DeSerieParaPeso(1, 3, 7) == 7
                  && LogicaCampanaHeroes.DeSerieParaPeso(2, 2, -1) == 0
                  && Math.Abs(LogicaCampanaHeroes.ProbabilidadNuevas(LogicaCampanaHeroes.DeSerieParaPeso(1, 2, 7), 2) - 2.0 / 9) < 1e-9,
            "actos: con orden abierto (reglas 1+) la nueva pesa como cada raza del bombo abierto (7+2 -> 22 %)");

        // -------- Regla del sorteo por partida (2.ª ronda de la revisión): las empezadas antes de la 0.7.6 siguen con la de antes
        // -------- Draw rule per run (2nd round of the review): runs started before 0.7.6 keep using the previous one
        comprobar(LogicaCampanaHeroes.ReglaActos(0, 1, 0) == LogicaCampanaHeroes.ReglaNueva && LogicaCampanaHeroes.ReglaActos(0, 1, 2) == LogicaCampanaHeroes.ReglaAntigua
                  && LogicaCampanaHeroes.ReglaActos(0, 2, 0) == LogicaCampanaHeroes.ReglaAntigua && LogicaCampanaHeroes.ReglaActos(2, 3, 9) == LogicaCampanaHeroes.ReglaNueva
                  && LogicaCampanaHeroes.ReglaActos(1, 1, 0) == LogicaCampanaHeroes.ReglaAntigua && LogicaCampanaHeroes.ReglaActos(7, 1, 0) == LogicaCampanaHeroes.ReglaNueva,
            "actos: partida nueva (acto 1, sin batallas) -> regla nueva; empezada antes -> la de antes; la apuntada manda siempre");

        // -------- Vista 3D de la Colección (prueba de Zero del 03-11-2026): id «hero:<id>:<bloqueado>» de la 1.11.1
        // -------- 3D view of the Collection (Zero's test of 03-11-2026): id «hero:<id>:<bloqueado>» from 1.11.1
        comprobar(LogicaCampanaHeroes.HeroeDeColeccion("hero:903:False", out int c1) && c1 == 903 &&
                  LogicaCampanaHeroes.HeroeDeColeccion("hero:904:True", out int c2) && c2 == 904 &&
                  LogicaCampanaHeroes.HeroeDeColeccion("hero:901", out int c3) && c3 == 901,
            "Colección: el id del héroe sale de «hero:903:False», «hero:904:True» y del formato antiguo «hero:901»");
        comprobar(!LogicaCampanaHeroes.HeroeDeColeccion("unit:900:True:False", out _) && !LogicaCampanaHeroes.HeroeDeColeccion(null, out _) &&
                  !LogicaCampanaHeroes.HeroeDeColeccion("", out _) && !LogicaCampanaHeroes.HeroeDeColeccion("hero:", out _) &&
                  !LogicaCampanaHeroes.HeroeDeColeccion("hero:abc:True", out _) && !LogicaCampanaHeroes.HeroeDeColeccion("hero:99999999999", out _),
            "Colección: unidades, vacío, sin número o número que no cabe -> no es un héroe");
        comprobar(!LogicaCampanaHeroes.HeroeDeColeccion("hero:99999999999", out int c4) && c4 == -1 && !LogicaCampanaHeroes.HeroeDeColeccion("unit:5", out int c5) && c5 == -1,
            "Colección: si no es un héroe, el id sale -1 (también con un número que no cabe)");

        // -------- Manifiesto real: prestados del diseño y su figura
        // -------- Real manifest: borrowed heroes from the design and their figure
        if (manifiesto == null || manifiesto.Heroes == null || manifiesto.Heroes.Count == 0)
        {
            Console.WriteLine("(sin manifiesto con héroes: se omiten las comprobaciones de los prestados reales)");
            return;
        }
        foreach (var par in Prestados)
        {
            var h = manifiesto.Heroes.FirstOrDefault(x => x.Id == par.Key);
            comprobar(h != null && h.PrestadoDe == par.Value, $"manifiesto: el héroe {par.Key} presta del {par.Value} (lee {h?.PrestadoDe.ToString() ?? "nada"})");
            if (h == null) continue;
            int indice = LogicaCampanaHeroes.IndiceFigura(h.Id, 16, id => manifiesto.Heroes.First(x => x.Id == id).PrestadoDe, 1);
            comprobar(indice == par.Value - 1, $"manifiesto: el héroe {par.Key} enciende la figura {par.Value - 1} del mapa");
        }
    }
}
