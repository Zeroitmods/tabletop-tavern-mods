// PruebasAccionesHeroes.cs
// Prueba 22 (18-10-2026): acción propia de los héroes nuevos en «Elige uno» tras la batalla (LogicaAccionesHeroes.cs),
// fuera del juego:
//   - tabla: 901-904 tienen acción, con clave estable (la del save) y textos de respaldo; los héroes de serie no;
//   - decisión de la fila: Marcha y nada que ofrecer = sin fila; save con la acción elegida = elegida; otra opción ya
//     elegida = apartada; si no, fila normal;
//   - Refuerzos (héroe 902): rareza y número por acto (2 comunes, 1 poco común, 1 rara), candidatas de la raza 40
//     del manifiesto real (sin la unidad 900 salvo «Guardia en la facción A») y elección con repetición;
//   - textos: el del juego si existe; si falta (vacío o la propia clave), el de respaldo en el idioma del juego.
// El postfix de EngagementPanel.ShowChoices y los efectos (curar, reclutar, consumible) solo se pueden ver dentro del juego.
// Autor: ZeroitMods.
// PruebasAccionesHeroes.cs
// Test 22 (18-10-2026): custom action of the new heroes in «Elige uno» after the battle (LogicaAccionesHeroes.cs),
// outside the game:
//   - table: 901-904 have an action, with a stable key (the one in the save) and fallback texts; the base-game heroes do not;
//   - row decision: Marcha and nothing to offer = no row; save with the action chosen = chosen; another option already
//     chosen = set aside; otherwise, normal row;
//   - Refuerzos (hero 902): rarity and number per act (2 common, 1 uncommon, 1 rare), candidates from race 40
//     of the real manifest (without unit 900 except «Guardia en la facción A») and selection with repetition;
//   - texts: the game's own if it exists; if missing (empty or the key itself), the fallback in the game's language.
// The EngagementPanel.ShowChoices postfix and the effects (heal, recruit, consumable) can only be seen inside the game.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroNucleo;

internal static class PruebasAccionesHeroes
{
    public static void Todas(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        PruebaTabla(comprobar);
        PruebaDecision(comprobar);
        PruebaRefuerzos(comprobar, manifiesto);
        PruebaTextos(comprobar);
    }

    // ---------------------------------------------------------------
    // Tabla de acciones
    // Action table
    // ---------------------------------------------------------------
    private static void PruebaTabla(Action<bool, string> comprobar)
    {
        comprobar(new[] { 901, 902, 903, 904 }.All(h => LogicaAccionesHeroes.De(h) != null) && LogicaAccionesHeroes.Heroes.Count() == 4,
            "acciones de héroe: 901-904 tienen acción propia");
        comprobar(Enumerable.Range(0, 20).All(h => LogicaAccionesHeroes.De(h) == null) && LogicaAccionesHeroes.De(905) == null,
            "acciones de héroe: los héroes de serie (1-16) y los ids desconocidos no tienen acción del núcleo");
        comprobar(LogicaAccionesHeroes.Heroes.All(h => LogicaAccionesHeroes.De(h).Clave == $"zeroH{h}Accion"),
            "acciones de héroe: la clave del título (la del save) es zeroH<id>Accion");
        var tipos = LogicaAccionesHeroes.Heroes.Select(h => LogicaAccionesHeroes.De(h).Tipo).ToList();
        comprobar(tipos.Distinct().Count() == 4, "acciones de héroe: cada héroe tiene un tipo de efecto distinto");
        comprobar(LogicaAccionesHeroes.Heroes.Select(LogicaAccionesHeroes.De).All(a =>
                !string.IsNullOrEmpty(a.TituloEs) && !string.IsNullOrEmpty(a.TituloEn) &&
                (a.ClaveDetalle == null || (!string.IsNullOrEmpty(a.DetalleEs) && !string.IsNullOrEmpty(a.DetalleEn)))),
            "acciones de héroe: títulos y detalles de respaldo en español e inglés");
        // Estilo de las de serie: frase con punto en español y sin punto en inglés
        // Style of the base-game ones: sentence with a period in Spanish and without a period in English
        comprobar(LogicaAccionesHeroes.Heroes.Select(LogicaAccionesHeroes.De).Where(a => a.ClaveDetalle != null)
                .All(a => a.DetalleEs.EndsWith(".") && !a.DetalleEn.EndsWith(".")),
            "acciones de héroe: la línea en cursiva acaba en punto en español y sin punto en inglés, como las de serie");
        var y = LogicaAccionesHeroes.De(901);
        comprobar(y.Tipo == LogicaAccionesHeroes.Tipo.CurarTodas && Math.Abs(y.Cantidad - 0.25f) < 1e-6 && y.DetalleEs.Contains("25 %") && y.DetalleEn.Contains("25%"),
            "acciones de héroe: Yarrick cura un 25 % a todas las unidades y el texto lo dice (25 % / 25%)");
        comprobar(LogicaAccionesHeroes.De(902).Raza == 40, "acciones de héroe: los refuerzos de Creed son de Astra Militarum (40)");
    }

    // ---------------------------------------------------------------
    // Decisión de la fila
    // Row decision
    // ---------------------------------------------------------------
    private static void PruebaDecision(Action<bool, string> comprobar)
    {
        comprobar(LogicaAccionesHeroes.Decidir(true, true, false, false) == LogicaAccionesHeroes.Fila.NoOfrecer &&
                  LogicaAccionesHeroes.Decidir(true, true, true, false) == LogicaAccionesHeroes.Fila.NoOfrecer,
            "acciones de héroe: en la Marcha no hay fila (como las opciones de héroe de serie)");
        comprobar(LogicaAccionesHeroes.Decidir(false, false, false, false) == LogicaAccionesHeroes.Fila.NoOfrecer,
            "acciones de héroe: sin nada que ofrecer (p. ej. sin unidades de refuerzo) no hay fila");
        comprobar(LogicaAccionesHeroes.Decidir(false, true, false, false) == LogicaAccionesHeroes.Fila.Ofrecer,
            "acciones de héroe: fila normal si no se ha elegido nada");
        comprobar(LogicaAccionesHeroes.Decidir(false, true, true, false) == LogicaAccionesHeroes.Fila.YaElegida &&
                  LogicaAccionesHeroes.Decidir(false, true, true, true) == LogicaAccionesHeroes.Fila.YaElegida,
            "acciones de héroe: al volver a la pantalla con la acción elegida en el save, la fila sale elegida");
        comprobar(LogicaAccionesHeroes.Decidir(false, true, false, true) == LogicaAccionesHeroes.Fila.Apartada,
            "acciones de héroe: si ya se eligió otra opción (save), la fila sale apartada");
        comprobar(LogicaAccionesHeroes.PrefijoEleccion == "choice:", "acciones de héroe: prefijo del save igual que el del juego (choice:)");
    }

    // ---------------------------------------------------------------
    // Refuerzos
    // ---------------------------------------------------------------
    private static void PruebaRefuerzos(Action<bool, string> comprobar, Manifiesto manifiesto)
    {
        comprobar(LogicaAccionesHeroes.RefuerzosPorLibro(0) == ("Common", 2) && LogicaAccionesHeroes.RefuerzosPorLibro(1) == ("Common", 2) &&
                  LogicaAccionesHeroes.RefuerzosPorLibro(2) == ("Uncommon", 1) && LogicaAccionesHeroes.RefuerzosPorLibro(3) == ("Rare", 1) &&
                  LogicaAccionesHeroes.RefuerzosPorLibro(9) == ("Rare", 1),
            "refuerzos: acto 1 = 2 comunes, acto 2 = 1 poco común, acto 3 o más = 1 rara");

        // Manifiesto sintético: raza por número o por nombre, activas e inactivas
        // Synthetic manifest: race by number or by name, active and inactive
        var u = new List<Manifiesto.Unidad>
        {
            new Manifiesto.Unidad { Id = 900, Raza = "IronLegion", Rareza = "Common", Activa = true },
            new Manifiesto.Unidad { Id = 901, Raza = "40", Rareza = "Common", Activa = true },
            new Manifiesto.Unidad { Id = 902, Raza = "40", Rareza = "Common", Activa = false },
            new Manifiesto.Unidad { Id = 903, Raza = "40", Rareza = "Uncommon", Activa = true },
            new Manifiesto.Unidad { Id = 921, Raza = "41", Rareza = "Common", Activa = true },
            null,
        };
        Func<string, int> nombres = n => n == "IronLegion" ? 1 : -1;
        Func<Manifiesto.Unidad, int> sinGuardia = x => LogicaAccionesHeroes.RazaNumerica(x.Raza, nombres);
        Func<Manifiesto.Unidad, int> conGuardia = x => x.Id == 900 ? 40 : LogicaAccionesHeroes.RazaNumerica(x.Raza, nombres);
        comprobar(LogicaAccionesHeroes.Candidatas(u, 40, "Common", sinGuardia).SequenceEqual(new[] { 901 }),
            "refuerzos: comunes de la raza 40 = solo las activas de esa raza (sin la 902 inactiva, la 900 de Iron Legion ni la 921 de la 41)");
        comprobar(LogicaAccionesHeroes.Candidatas(u, 40, "Common", conGuardia).SequenceEqual(new[] { 900, 901 }),
            "refuerzos: con «Guardia en Astra Militarum» la 900 entra en las comunes de la 40");
        comprobar(LogicaAccionesHeroes.Candidatas(u, 40, "Rare", sinGuardia).Count == 0 && LogicaAccionesHeroes.Candidatas(null, 40, "Common", sinGuardia).Count == 0,
            "refuerzos: sin candidatas (rareza sin unidades o sin manifiesto) la lista sale vacía");
        comprobar(LogicaAccionesHeroes.RazaNumerica("40", nombres) == 40 && LogicaAccionesHeroes.RazaNumerica("IronLegion", nombres) == 1 &&
                  LogicaAccionesHeroes.RazaNumerica("", nombres) == -1 && LogicaAccionesHeroes.RazaNumerica("Nada", nombres) == -1,
            "refuerzos: raza numérica por número o por nombre del enum, -1 si no se conoce");

        // Elección con repetición y azar fuera de rango
        // Selection with repetition and out-of-range randomness
        var cand = new List<int> { 901, 902, 903 };
        var secuencia = new Queue<int>(new[] { 2, 2, 0 });
        comprobar(LogicaAccionesHeroes.Elegir(cand, 3, n => secuencia.Dequeue()).SequenceEqual(new[] { 903, 903, 901 }),
            "refuerzos: elige con repetición, como Levantar a los muertos");
        comprobar(LogicaAccionesHeroes.Elegir(cand, 2, n => 99).SequenceEqual(new[] { 901, 901 }) &&
                  LogicaAccionesHeroes.Elegir(new List<int>(), 2, n => 0).Count == 0 && LogicaAccionesHeroes.Elegir(cand, 1, null).Count == 0,
            "refuerzos: un azar fuera de rango cae en la primera; sin candidatas o sin azar, nada");

        // Con el manifiesto real de los módulos: hay candidatas de la raza 40 en las tres rarezas
        // With the real manifest of the modules: there are race 40 candidates in all three rarities
        if (manifiesto == null || manifiesto.Unidades.Count == 0) return;
        Func<Manifiesto.Unidad, int> real = x => LogicaAccionesHeroes.RazaNumerica(x.Raza, n => -1);
        foreach (int libro in new[] { 1, 2, 3 })
        {
            var (rareza, cantidad) = LogicaAccionesHeroes.RefuerzosPorLibro(libro);
            var lista = LogicaAccionesHeroes.Candidatas(manifiesto.Unidades, 40, rareza, real);
            comprobar(lista.Count > 0 && lista.All(id => id >= 901 && id <= 913),
                $"refuerzos (módulos reales): acto {libro}, {cantidad} de rareza {rareza} entre {lista.Count} unidades activas de Astra Militarum ({string.Join(", ", lista)})");
        }
    }

    // ---------------------------------------------------------------
    // Textos con respaldo
    // Texts with fallback
    // ---------------------------------------------------------------
    private static void PruebaTextos(Action<bool, string> comprobar)
    {
        comprobar(LogicaAccionesHeroes.TextoValido("Refuerzos de Cadia", "zeroH902Accion") && !LogicaAccionesHeroes.TextoValido("zeroH902Accion", "zeroH902Accion") &&
                  !LogicaAccionesHeroes.TextoValido(" ", "x") && !LogicaAccionesHeroes.TextoValido(null, "x"),
            "textos: vale el del juego si no está vacío ni es la propia clave");
        comprobar(LogicaAccionesHeroes.Texto("Del juego", "k", "es", "en", true) == "Del juego" &&
                  LogicaAccionesHeroes.Texto("k", "k", "es", "en", true) == "es" && LogicaAccionesHeroes.Texto("", "k", "es", "en", false) == "en" &&
                  LogicaAccionesHeroes.Texto(null, "k", null, null, true) == "k",
            "textos: sin texto del juego, respaldo en el idioma del juego (y la clave si tampoco hay respaldo)");
        // 03-11-2026 (revisión de idiomas, hallazgo 6): el idioma sale del código activo; el texto de serie solo si no hay código
        // 03-11-2026 (language review, finding 6): the language comes from the active code; the base-game text only if there is no code
        comprobar(LogicaAccionesHeroes.EsEspanol("es", null) && LogicaAccionesHeroes.EsEspanol("es-ES", "Ransom Captives") &&
                  !LogicaAccionesHeroes.EsEspanol("en", LogicaAccionesHeroes.RescateEnEspanol) && !LogicaAccionesHeroes.EsEspanol("fr", null) &&
                  !LogicaAccionesHeroes.EsEspanol("zh-Hant", null),
            "idioma: con código de idioma manda el código (es, es-ES = español; en, fr, zh-Hant = no)");
        comprobar(LogicaAccionesHeroes.EsEspanol(null, LogicaAccionesHeroes.RescateEnEspanol) && LogicaAccionesHeroes.EsEspanol("", LogicaAccionesHeroes.RescateEnEspanol) &&
                  !LogicaAccionesHeroes.EsEspanol(null, "Ransom Captives") && !LogicaAccionesHeroes.EsEspanol(" ", null),
            "idioma: sin código, se mira el texto de serie de «Ransom Captives»");
        // 03-11-2026 (revisión del 03-11, hallazgo 8): elección del aviso del bloqueo en línea (Saves.TextoAvisoOnline, misma
        // lógica): con la clave del módulo, el texto del módulo; sin ella (el juego devuelve la clave o nada), el respaldo
        // en español si el código activo es es/es-ES y en inglés con fr, zh-Hant o sin código ni texto de serie
        // 03-11-2026 (review of 03-11, finding 8): choice of the online lock warning (Saves.TextoAvisoOnline, same
        // logic): with the module key, the module text; without it (the game returns the key or nothing), the fallback
        // in Spanish if the active code is es/es-ES and in English with fr, zh-Hant or with no code and no base-game text
        const string clave = "zeroMultijugadorBloqueado";
        string Aviso(string delJuego, string codigo) =>
            LogicaAccionesHeroes.Texto(delJuego, clave, "ES", "EN", !LogicaAccionesHeroes.TextoValido(delJuego, clave) && LogicaAccionesHeroes.EsEspanol(codigo, null));
        comprobar(Aviso("Le jeu en ligne est désactivé", "fr") == "Le jeu en ligne est désactivé" && Aviso(clave, "es") == "ES" &&
                  Aviso(null, "es-ES") == "ES" && Aviso(clave, "fr") == "EN" && Aviso("", "zh-Hant") == "EN" && Aviso(clave, null) == "EN",
            "aviso en línea: texto del módulo si lo trae; si no, respaldo según el código de idioma (es/es-ES español; fr, zh-Hant o sin código, inglés)");
    }
}
