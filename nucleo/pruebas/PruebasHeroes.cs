// PruebasHeroes.cs
// Prueba 12 (17-10-2026): pantalla de héroes y ejército inicial de los héroes nuevos.
//   - CasillasHeroes: casillas necesarias con la facción misteriosa y los «próximamente» del juego instalado (20 + 2 + 2
//     = 24), tarjetas que hay que clonar, plantilla (la última normal) y orden de las tarjetas (clones antes de la
//     misteriosa, que sigue la última).
//   - Manifiesto real (si se pasa la carpeta de módulos): cada héroe nuevo con 3 unidades iniciales de su raza (la 900
//     cuenta como facción A) que se pueden pagar con el oro de su héroe de serie y dejan margen para un objeto común.
// Autor: ZeroitMods.
// PruebasHeroes.cs
// Test 12 (17-10-2026): heroes screen and starting army of the new heroes.
//   - CasillasHeroes: slots needed with the mystery faction and the "coming soon" ones of the installed game (20 + 2 + 2
//     = 24), cards to clone, template (the last normal one) and card order (clones before the
//     mystery one, which follows the last).
//   - Real manifest (if the modules folder is passed): each new hero with 3 starting units of its race (900
//     counts as faction A) that can be paid with the gold of its stock hero and leave room for a common item.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroNucleo;

internal static class PruebasHeroes
{
    // Oro inicial de los héroes de serie (HeroDataSO del juego instalado, leído el 17-10-2026) y coste de cada rareza
    // en el tesoro (GetUnitCost: 2/6/12/99); los mismos valores que valida generar_mod_facciones.py
    // Starting gold of the stock heroes (HeroDataSO from the installed game, read on 17-10-2026) and cost of each rarity
    // in the treasury (GetUnitCost: 2/6/12/99); the same values that generar_mod_facciones.py validates
    private static readonly Dictionary<int, int> OroSerie = new Dictionary<int, int>
    {
        { 1, 10 }, { 2, 10 }, { 3, 17 }, { 4, 17 }, { 5, 17 }, { 6, 12 }, { 7, 11 }, { 8, 12 }, { 9, 15 }, { 10, 16 },
        { 11, 11 }, { 12, 12 }, { 13, 10 }, { 14, 13 }, { 15, 15 }, { 16, 16 },
    };
    private static readonly Dictionary<string, int> CosteRareza = new Dictionary<string, int>
    {
        { "Common", 2 }, { "Uncommon", 6 }, { "Rare", 12 }, { "Legendary", 99 },
    };

    public static void Todas(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        // -------- Casillas de la pantalla de héroes
        // -------- Heroes screen slots
        comprobar(CasillasHeroes.Necesarias(20, 2, CasillasHeroes.CasillasMisterio) == 24,
            "pantalla de héroes: 20 jugables + 2 próximamente + la misteriosa = 24 casillas");
        comprobar(CasillasHeroes.Necesarias(16, 2, 2) == 20 && CasillasHeroes.TarjetasNuevas(16, 2, 2, 10) == 0,
            "pantalla de héroes: el juego de serie (16 + 2 + 2) cabe en sus 20 casillas sin clonar");
        comprobar(CasillasHeroes.TarjetasNuevas(20, 2, 2, 10) == 2,
            "pantalla de héroes: con 901-904 hay que clonar 2 tarjetas (de 10 a 12)");
        comprobar(CasillasHeroes.TarjetasNuevas(18, 2, 2, 10) == 1,
            "pantalla de héroes: con un solo módulo de facción (18 jugables) basta 1 tarjeta más");
        comprobar(CasillasHeroes.Necesarias(20, 0, 0) == 20 && CasillasHeroes.TarjetasNuevas(20, 0, 0, 8) == 2,
            "pantalla de héroes: juego sin facción misteriosa (versión anterior): 20 casillas, de 8 a 10 tarjetas");
        comprobar(CasillasHeroes.Tarjetas(21) == 11 && CasillasHeroes.Necesarias(-3, -1, 2) == 2,
            "pantalla de héroes: número impar de casillas redondea hacia arriba y los negativos cuentan 0");
        comprobar(CasillasHeroes.Plantilla(10, true) == 8 && CasillasHeroes.Plantilla(10, false) == 9 && CasillasHeroes.Plantilla(1, true) == -1,
            "pantalla de héroes: la plantilla es la última tarjeta normal (la penúltima si la última es la misteriosa)");
        var orden = CasillasHeroes.OrdenTarjetas(10, 2, true);
        comprobar(orden.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, -1, -2, 9 }),
            "pantalla de héroes: orden con misteriosa = 9 originales, 2 clones y la misteriosa al final (" + string.Join(",", orden) + ")");
        var ordenSin = CasillasHeroes.OrdenTarjetas(8, 2, false);
        comprobar(ordenSin.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, -1, -2 }),
            "pantalla de héroes: orden sin misteriosa = originales y los clones al final");
        // Con la reserva, los héroes caben todos: casillas - 2 >= jugables + próximamente (como LoadRoster)
        // With the reserve, all heroes fit: slots - 2 >= playable + coming soon (like LoadRoster)
        int casillas = (10 + CasillasHeroes.TarjetasNuevas(20, 2, 2, 10)) * 2;
        comprobar(Math.Min(casillas - 2, 22) == 22,
            "pantalla de héroes: tras ampliar, LoadRoster enseña los 22 (20 jugables y 2 próximamente) y deja la misteriosa");

        // -------- Ejército inicial de los héroes nuevos (manifiesto real)
        // -------- Starting army of the new heroes (real manifest)
        if (manifiesto == null)
        {
            Console.WriteLine("      (sin carpeta de módulos: no se comprueba el ejército inicial del manifiesto real)");
            return;
        }
        var unidades = manifiesto.Unidades.ToDictionary(u => u.Id);
        comprobar(manifiesto.Heroes.Count == 4, $"ejército inicial: 4 héroes nuevos en el manifiesto ({manifiesto.Heroes.Count})");
        foreach (var h in manifiesto.Heroes)
        {
            int coste = 0;
            bool deSuRaza = true, activas = true;
            foreach (int uid in h.Muestra)
            {
                if (!unidades.TryGetValue(uid, out var u)) { activas = false; continue; }
                activas &= u.Activa;
                string raza = uid == 900 ? "40" : u.Raza;
                deSuRaza &= raza == h.Raza.ToString();
                coste += CosteRareza.TryGetValue(u.Rareza ?? "", out int c) ? c : 99;
            }
            int oro = OroSerie.TryGetValue(h.OroComo, out int o) ? o : -1;
            comprobar(h.Muestra.Count == 3 && activas && deSuRaza && oro > 0 && coste <= oro - 2,
                $"ejército inicial del héroe {h.Id}: [{string.Join(", ", h.Muestra)}], coste {coste} con {oro} de oro (margen {oro - coste}; mínimo 2)");
        }
    }
}
