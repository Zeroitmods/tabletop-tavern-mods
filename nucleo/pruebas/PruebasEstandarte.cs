// PruebasEstandarte.cs
// Prueba 30 (03-11-2026, núcleo 0.7.7): iconos del estandarte (src\LogicaEstandarte.cs y efecto iconosEstandarte de
// Modulos.cs) y el icono elegido en el registro del núcleo (RegistroGuardias). Lo que se comprueba: ids y rutas válidos e
// inválidos, repetidos, códigos, nombres de los iconos de serie, elección disponible o no, qué banderas llevan el icono,
// la lectura de zero_modulo.json con el efecto, la mezcla de dos módulos y que la elección se guarda y se relee.
// Autor: ZeroitMods.
// PruebasEstandarte.cs
// Test 30 (11-03-2026, core 0.7.7): banner icons (src\LogicaEstandarte.cs and the iconosEstandarte effect of
// Modulos.cs) and the icon chosen in the core registry (RegistroGuardias). What is checked: valid and invalid ids and paths,
// duplicates, codes, names of the default icons, whether the choice is available or not, which flags carry the icon,
// reading zero_modulo.json with the effect, merging two modules, and that the choice is saved and read back.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroNucleo;

internal static class PruebasEstandarte
{
    // -----------------------------------------------------------------------
    // Sección: casos
    // Section: cases
    // -----------------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        // --- Textos propios y cambio de idioma (03-11, núcleo 0.7.8) ---
        // Los títulos y avisos emergentes del estandarte se clonan sin la localización del juego: todos deben pasar por
        // PonerTitulo / Tooltip(clave…), que los apuntan para rehacerlos al cambiar de idioma (RefrescarTextos)
        // --- Custom texts and language change (11-03, core 0.7.8) ---
        // The banner's titles and pop-up notices are cloned without the game's localization: all of them must go through
        // PonerTitulo / Tooltip(key…), which record them so they can be rebuilt when the language changes (RefrescarTextos)
        string fuente = null;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null && fuente == null; d = d.Parent)
        {
            string f = Path.Combine(d.FullName, "src", "Estandarte.cs");
            if (File.Exists(f)) fuente = File.ReadAllText(f);
        }
        comprobar(fuente != null, "estandarte: se encuentra src\\Estandarte.cs");
        if (fuente != null)
        {
            comprobar(System.Text.RegularExpressions.Regex.Matches(fuente, @"\.text\s*=\s*Texto\(").Count == 1,
                      "estandarte: «.text = Texto(…)» solo en PonerTitulo (los demás títulos pasan por él y se apuntan)");
            comprobar(System.Text.RegularExpressions.Regex.Matches(fuente, @"SetUpToolTip\(").Count == 2,
                      "estandarte: SetUpToolTip solo en Tooltip y en RefrescarTextos");
            comprobar(fuente.Contains("RefrescarTextos();") && fuente.IndexOf("RefrescarTextos();") != fuente.LastIndexOf("RefrescarTextos();"),
                      "estandarte: los textos se rehacen en EnsureBuilt y en Refresh");
        }

        string raiz = Path.Combine(Path.GetTempPath(), "zero_pruebas_estandarte_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(raiz);
        try
        {
            // --- Ids y rutas ---
            // --- Ids and paths ---
            comprobar(LogicaEstandarte.IdValido("corona") && LogicaEstandarte.IdValido("astra_calavera_2"), "estandarte: ids válidos");
            comprobar(!LogicaEstandarte.IdValido("Corona") && !LogicaEstandarte.IdValido("a b") && !LogicaEstandarte.IdValido("") && !LogicaEstandarte.IdValido(null)
                      && !LogicaEstandarte.IdValido(new string('a', 41)), "estandarte: ids no válidos (mayúsculas, espacios, vacío, largo)");
            string carpeta = Path.Combine(raiz, "ZeroHeraldica");
            Directory.CreateDirectory(carpeta);
            string ok = LogicaEstandarte.RutaSegura(carpeta, "estandarte/corona.png", out string m1);
            comprobar(ok != null && m1 == null && ok.StartsWith(Path.GetFullPath(carpeta)), "estandarte: ruta relativa dentro de la carpeta del módulo");
            comprobar(LogicaEstandarte.RutaSegura(carpeta, "../otro/x.png", out _) == null, "estandarte: ruta con «..» rechazada");
            comprobar(LogicaEstandarte.RutaSegura(carpeta, @"C:\x.png", out _) == null && LogicaEstandarte.RutaSegura(carpeta, "/x.png", out _) == null, "estandarte: ruta absoluta rechazada");
            comprobar(LogicaEstandarte.RutaSegura(carpeta, "x.jpg", out _) == null && LogicaEstandarte.RutaSegura(carpeta, "", out _) == null, "estandarte: solo .png y no vacía");

            // --- Resolver: repetidos, inválidos y máximo ---
            // --- Resolve: duplicates, invalid entries and maximum ---
            var avisos = new List<string>();
            var decl = new List<IconoEstandarteDecl>
            {
                new IconoEstandarteDecl { Id = "corona", Fichero = "estandarte/corona.png", Clave = "zeroIconoEstandarte_corona" },
                new IconoEstandarteDecl { Id = "corona", Fichero = "estandarte/corona2.png" },
                new IconoEstandarteDecl { Id = "MAL", Fichero = "estandarte/mal.png" },
                new IconoEstandarteDecl { Id = "fuera", Fichero = "../fuera.png" },
                null,
            };
            var res = LogicaEstandarte.Resolver("ZeroHeraldica", carpeta, decl, avisos);
            comprobar(res.Count == 1 && res[0].Codigo == "ZeroHeraldica:corona" && res[0].Clave == "zeroIconoEstandarte_corona" && avisos.Count == 3,
                "estandarte: Resolver se queda con el bueno y avisa de repetido, id no válido y ruta fuera");
            var muchos = Enumerable.Range(0, LogicaEstandarte.MaximoPorModulo + 5).Select(i => new IconoEstandarteDecl { Id = "i" + i, Fichero = $"i{i}.png" }).ToList();
            var avisos2 = new List<string>();
            comprobar(LogicaEstandarte.Resolver("M", carpeta, muchos, avisos2).Count == LogicaEstandarte.MaximoPorModulo && avisos2.Count == 1, "estandarte: se recorta al máximo por módulo con un aviso");

            // Revisión externa 0.7.7, hallazgo 9: un módulo llamado «juego» chocaría con los iconos de serie
            // External review 0.7.7, finding 9: a module named «juego» would clash with the base-game icons
            var avisos3 = new List<string>();
            comprobar(LogicaEstandarte.Resolver("juego", carpeta, new[] { new IconoEstandarteDecl { Id = "x", Fichero = "x.png" } }, avisos3).Count == 0 && avisos3.Count == 1,
                "estandarte: un módulo con id «juego» no aporta iconos (choca con el prefijo de serie)");

            // --- Códigos y nombres de los iconos de serie ---
            // --- Codes and names of the default icons ---
            comprobar(LogicaEstandarte.CodigoJuego("RifleOutline") == "juego:RifleOutline", "estandarte: código de un icono de serie");
            comprobar(LogicaEstandarte.NombreLegible("Spears Outline") == "Spears" && LogicaEstandarte.NombreLegible("raptorsOutline") == "Raptors"
                      && LogicaEstandarte.NombreLegible("Cannon2Outline") == "Cannon2", "estandarte: nombre legible de las texturas de serie");
            comprobar(LogicaEstandarte.ClaveTexturaJuego("Glaive Outline") == "zeroIconoJuegoGlaive", "estandarte: clave de texto de un icono de serie");

            // --- Elección disponible y banderas ---
            // --- Available choice and flags ---
            var disp = new HashSet<string> { "juego:RifleOutline", "ZeroHeraldica:corona" };
            comprobar(LogicaEstandarte.ElegidoDisponible("ZeroHeraldica:corona", disp) == "ZeroHeraldica:corona", "estandarte: elección disponible se usa");
            comprobar(LogicaEstandarte.ElegidoDisponible("ZeroAstraMilitarum:astra_calavera", disp) == null, "estandarte: elección de un módulo apagado vuelve al de serie");
            comprobar(LogicaEstandarte.ElegidoDisponible(null, disp) == null && LogicaEstandarte.ElegidoDisponible("", disp) == null, "estandarte: sin elección, el de serie");
            comprobar(LogicaEstandarte.BanderaLlevaIcono(3, true) && !LogicaEstandarte.BanderaLlevaIcono(-3, true) && !LogicaEstandarte.BanderaLlevaIcono(0, true)
                      && !LogicaEstandarte.BanderaLlevaIcono(3, false), "estandarte: solo las banderas del jugador (SquadId > 0) y con icono elegido");

            // --- zero_modulo.json con el efecto y mezcla de dos módulos ---
            // --- zero_modulo.json with the effect and merging two modules ---
            var a = Modulo(raiz, "ZeroHeraldica", "[{\"id\":\"corona\",\"fichero\":\"estandarte/corona.png\",\"clave\":\"zeroIconoEstandarte_corona\",\"nombreEs\":\"Corona\",\"nombreEn\":\"Crown\"}]");
            var b = Modulo(raiz, "ZeroAstraMilitarum", "[{\"id\":\"astra_calavera\",\"fichero\":\"estandarte/astra_calavera.png\"},{\"id\":\"astra_rifles\",\"fichero\":\"estandarte/astra_rifles.png\"}]");
            comprobar(a.Declaracion?.Efectos?.IconosEstandarte?.Count == 1 && a.Declaracion.Efectos.IconosEstandarte[0].NombreEn == "Crown", "estandarte: zero_modulo.json lee efectos.iconosEstandarte");
            var r = Modulos.Resolver(new[] { a, b }, "0.7.7");
            comprobar(r.IconosEstandarte.Count == 3 && r.IconosEstandarte.Select(x => x.Codigo).SequenceEqual(new[] { "ZeroHeraldica:corona", "ZeroAstraMilitarum:astra_calavera", "ZeroAstraMilitarum:astra_rifles" }),
                "estandarte: los iconos de varios módulos se juntan en su orden");
            b.Activo = false;
            comprobar(Modulos.Resolver(new[] { a, b }, "0.7.7").IconosEstandarte.Count == 1, "estandarte: un módulo desactivado no aporta iconos");
            comprobar(Modulos.Resolver(new[] { a }, "0.7.6").IconosEstandarte.Count == 0, "estandarte: con un núcleo más viejo que nucleoMinimo no hay iconos");

            // --- Registro: la elección se guarda y se relee; null la borra ---
            // --- Registry: the choice is saved and read back; null clears it ---
            string ruta = Path.Combine(raiz, "registro.json");
            var reg = new RegistroGuardias(ruta);
            reg.FijarIconoEstandarte("ZeroHeraldica:corona");
            var reg2 = new RegistroGuardias(ruta);
            reg2.Cargar();
            comprobar(reg2.IconoEstandarte == "ZeroHeraldica:corona", "estandarte: el icono elegido se guarda en el registro del núcleo y se relee");
            int antes = reg2.Escrituras;
            reg2.FijarIconoEstandarte("ZeroHeraldica:corona");
            comprobar(reg2.Escrituras == antes, "estandarte: elegir el mismo icono no reescribe el registro");
            reg2.FijarIconoEstandarte(null);
            var reg3 = new RegistroGuardias(ruta);
            reg3.Cargar();
            comprobar(reg3.IconoEstandarte == null, "estandarte: volver al icono del héroe borra la elección");
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch { }
        }
    }

    // Módulo de prueba activo con zero_modulo.json (núcleo mínimo 0.7.7) y el efecto dado
    // Test module active with zero_modulo.json (minimum core 0.7.7) and the given effect
    private static ModuloEncontrado Modulo(string raiz, string id, string iconosJson)
    {
        string carpeta = Path.Combine(raiz, id);
        Directory.CreateDirectory(carpeta);
        string json = "{\"formato\":1,\"id\":\"" + id + "\",\"nombre\":\"" + id + "\",\"version\":\"1.0.0\",\"nucleoMinimo\":\"0.7.7\",\"efectos\":{\"iconosEstandarte\":" + iconosJson + "}}";
        string fichero = Path.Combine(carpeta, DeclaracionModulo.NombreFichero);
        File.WriteAllText(fichero, json);
        var d = Modulos.LeerDeclaracion(fichero, out _);
        return new ModuloEncontrado { Declaracion = d, Carpeta = carpeta, NombreCarpeta = id, Activo = true };
    }
}
