// PruebasModulos.cs
// Prueba 9 (0.7.0): módulos del núcleo, fuera del juego y en carpetas temporales:
//   - declaraciones y Resolver: escuadras (manda la mayor, con aviso; redondeo int(base*m+0,5) desde 2 soldados),
//     munición, distancia, dependencias que faltan, núcleo demasiado viejo, id repetido y fusión de contenido con rutas
//     resueltas contra la carpeta de cada módulo;
//   - réplica del orden de mods del juego (modlist.json: orden, desactivados, carpetas que faltan, nuevas al final en
//     orden alfabético, «_» y carpetas sin mod.json fuera);
//   - módulos desactivados por defecto apuntados en modlist.json (con copia previa, conservando lo que había, sin repetir);
//   - sincronización desde BepInEx\plugins: copia con marcador, igual, actualización, instalación a mano intacta y
//     retirada a Mods\_ZeroRetirados cuando el paquete desaparece;
//   - sincronización fichero a fichero (lista de huellas en el marcador): una actualización aparta los ficheros que el
//     paquete ya no trae, respalda las ediciones locales antes de pisarlas, deja lo que añadió el usuario, completa los
//     marcadores de versiones anteriores, retira un módulo entero sin perder nada y funciona con espacios y acentos;
//   - modlist.json leído igual de estricto que el juego (con comentarios o comas finales el juego activa todo: aquí igual);
//   - cambios de perfil del gestor: la misma copia no se duplica en Mods\_ZeroRetirados; una distinta sí se guarda;
//   - registro de objetos: lo apuntado de un módulo desactivado se conserva al guardar y vuelve al reactivarlo.
// Con la carpeta maestra de módulos (modulos\), las combinaciones reales (cada módulo solo, escuadras juntas, contenido).
// Autor: ZeroitMods.
// PruebasModulos.cs
// Test 9 (0.7.0): core modules, outside the game and in temporary folders:
//   - declarations and Resolver: squads (the largest wins, with a warning; rounding int(base*m+0.5) from 2 soldiers),
//     ammo, range, missing dependencies, core too old, repeated id and content merge with paths
//     resolved against each module's folder;
//   - replica of the game's mod order (modlist.json: order, disabled ones, missing folders, new ones at the end in
//     alphabetical order, «_» and folders without mod.json left out);
//   - modules disabled by default recorded in modlist.json (with a prior backup, keeping what was there, no repeats);
//   - sync from BepInEx\plugins: copy with marker, same, update, manual install untouched and
//     removal to Mods\_ZeroRetirados when the package disappears;
//   - file-by-file sync (list of fingerprints in the marker): an update sets aside the files the
//     package no longer ships, backs up local edits before overwriting them, keeps what the user added, completes the
//     markers from earlier versions, removes an entire module without losing anything and works with spaces and accents;
//   - modlist.json read as strictly as the game does (with comments or trailing commas the game enables everything: same here);
//   - manager profile changes: the same copy is not duplicated in Mods\_ZeroRetirados; a different one is saved;
//   - gear registry: what was recorded for a disabled module is kept on save and comes back when it is re-enabled.
// With the master modules folder (modulos\), the real combinations (each module alone, squads together, content).
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ZeroNucleo;

internal static class PruebasModulos
{
    // ---------------------------------------------------------------
    // Ayudantes para crear módulos de prueba
    // Helpers to create test modules
    // ---------------------------------------------------------------
    private static string Temporal()
    {
        string d = Path.Combine(Path.GetTempPath(), "zero_modulos_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    // Crea una carpeta de módulo con mod.json, zero_modulo.json y, opcionalmente, un manifiesto de contenido
    // Creates a module folder with mod.json, zero_modulo.json and, optionally, a content manifest
    private static string Modulo(string raiz, string carpeta, object declaracion, string contenido = null)
    {
        string d = Path.Combine(raiz, carpeta);
        Directory.CreateDirectory(d);
        File.WriteAllText(Path.Combine(d, "mod.json"), "{\"displayName\":\"" + carpeta + "\",\"author\":\"ZeroitMods\",\"version\":\"1.0.0\"}");
        File.WriteAllText(Path.Combine(d, DeclaracionModulo.NombreFichero), JsonSerializer.Serialize(declaracion));
        if (contenido != null) File.WriteAllText(Path.Combine(d, "facciones_manifiesto.json"), contenido);
        return d;
    }

    private static ModuloEncontrado Activo(string carpeta)
    {
        var d = Modulos.LeerDeclaracion(Path.Combine(carpeta, DeclaracionModulo.NombreFichero), out _);
        return new ModuloEncontrado { Declaracion = d, Carpeta = carpeta, NombreCarpeta = Path.GetFileName(carpeta), Activo = true };
    }

    private static object Escuadras(string id, double m, bool porDefecto = true) =>
        new { id, nombre = id, version = "1.0.0", nucleoMinimo = "0.7.0", activoPorDefecto = porDefecto, efectos = new { escuadras = new { multiplicador = m, minimoSoldados = 2 } } };

    // ---------------------------------------------------------------
    // Pruebas con módulos de prueba
    // Tests with test modules
    // ---------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar)
    {
        string raiz = Temporal();
        try
        {
            // --- Escuadras: manda la mayor y se avisa; redondeo y umbral ---
            // --- Squads: the largest wins and a warning is given; rounding and threshold ---
            var e50 = Activo(Modulo(raiz, "ZeroEscuadras50", Escuadras("ZeroEscuadras50", 1.5)));
            var e100 = Activo(Modulo(raiz, "ZeroEscuadras100", Escuadras("ZeroEscuadras100", 2.0, false)));
            var e150 = Activo(Modulo(raiz, "ZeroEscuadras150", Escuadras("ZeroEscuadras150", 2.5, false)));
            var r = Modulos.Resolver(new[] { e100, e50, e150 }, "0.7.0");
            comprobar(Math.Abs(r.FactorEscuadras - 2.5) < 1e-9 && r.ModuloEscuadras == "ZeroEscuadras150" && r.Avisos.Count(a => a.Contains("manda el mayor")) == 2,
                "escuadras: con +50, +100 y +150 % activos manda x2,5 y se avisa (nunca se multiplican)");
            comprobar(Modulos.SoldadosEnCampo(48, 1.5) == 72 && Modulos.SoldadosEnCampo(20, 1.5) == 30 && Modulos.SoldadosEnCampo(2, 1.5) == 3
                      && Modulos.SoldadosEnCampo(1, 2.5) == 1 && Modulos.SoldadosEnCampo(8, 2.0) == 16 && Modulos.SoldadosEnCampo(8, 2.5) == 20
                      && Modulos.SoldadosEnCampo(5, 2.5) == 13 && Modulos.SoldadosEnCampo(48, 1.0) == 48,
                "escuadras: int(base*m+0,5) desde 2 soldados (48->72, 20->30, 2->3, 1->1, 8->16/20, 5->13; x1 no cambia)");
            var solo50 = Modulos.Resolver(new[] { e50 }, "0.7.0");
            comprobar(Math.Abs(solo50.FactorEscuadras - 1.5) < 1e-9 && solo50.Avisos.Count == 0, "escuadras: un solo módulo, sin aviso");
            var e50Desact = Activo(e50.Carpeta); e50Desact.Activo = false;
            comprobar(Modulos.Resolver(new[] { e50Desact }, "0.7.0").FactorEscuadras == 1.0, "escuadras: un módulo desactivado no aplica nada");

            // --- Munición, distancia, dependencias, versión e id repetido ---
            // --- Ammo, range, dependencies, version and repeated id ---
            var mun = Activo(Modulo(raiz, "ZeroMunicion", new { id = "ZeroMunicion", efectos = new { municionJugador = new { multiplicador = 3, tipos = new[] { "Ranged", "Hybrid", "Artillery" } } } }));
            var dis = Activo(Modulo(raiz, "ZeroStatsDistancia", new { id = "ZeroStatsDistancia", efectos = new { distanciaJugador = new { alcance = 10, precision = 10, dano = 3, tipos = new[] { "Ranged" } } } }));
            var dep = Activo(Modulo(raiz, "ZeroDepende", new { id = "ZeroDepende", dependencias = new[] { "ZeroNoExiste" }, efectos = new { municionJugador = new { multiplicador = 5 } } }));
            var viejo = Activo(Modulo(raiz, "ZeroFuturo", new { id = "ZeroFuturo", nucleoMinimo = "9.0.0", efectos = new { escuadras = new { multiplicador = 4.0 } } }));
            var copia = Activo(Modulo(raiz, "ZeroMunicionCopia", new { id = "ZeroMunicion", efectos = new { municionJugador = new { multiplicador = 9 } } }));
            var r2 = Modulos.Resolver(new[] { mun, dis, dep, viejo, copia }, "0.7.0");
            comprobar(r2.Municion?.Multiplicador == 3 && r2.Municion.Tipos.SequenceEqual(new[] { "Ranged", "Hybrid", "Artillery" })
                      && r2.Distancia?.Alcance == 10 && r2.Distancia.Precision == 10 && r2.Distancia.Dano == 3 && r2.FactorEscuadras == 1.0,
                "munición x3 (Ranged, Hybrid, Artillery) y distancia +10/+10/+3 (Ranged); sin escuadras, de serie");
            comprobar(r2.Rechazados.Count == 3 && r2.Rechazados.Any(x => x.StartsWith("ZeroDepende") && x.Contains("ZeroNoExiste"))
                      && r2.Rechazados.Any(x => x.StartsWith("ZeroFuturo") && x.Contains("9.0.0")) && r2.Rechazados.Any(x => x.Contains("repetido"))
                      && r2.Aplicados.Count == 2,
                "rechazados: dependencia que falta, núcleo demasiado viejo e id repetido (se queda el primero)");
            // 0.7.6: efecto de moral del jugador; con dos módulos se juntan (basta uno) y avisan; todo false = sin efecto
            // 0.7.6: player morale effect; with two modules they are combined (one is enough) and warn; all false = no effect
            var mo1 = Activo(Modulo(raiz, "ZeroMoralA", new { id = "ZeroMoralA", efectos = new { moralJugador = new { sinHuida = true, sinVacilar = false } } }));
            var mo2 = Activo(Modulo(raiz, "ZeroMoralB", new { id = "ZeroMoralB", efectos = new { moralJugador = new { sinHuida = false, sinVacilar = true } } }));
            var mo0 = Activo(Modulo(raiz, "ZeroMoralC", new { id = "ZeroMoralC", efectos = new { moralJugador = new { sinHuida = false, sinVacilar = false } } }));
            var rm = Modulos.Resolver(new[] { mo1, mo2 }, "0.7.0");
            var rm0 = Modulos.Resolver(new[] { mo0 }, "0.7.0");
            var rmDef = Modulos.Resolver(new[] { Activo(Modulo(raiz, "ZeroMoralD", new { id = "ZeroMoralD", efectos = new { moralJugador = new { } } })) }, "0.7.0");
            var rmV = Modulos.Resolver(new[] { mo2 }, "0.7.0");
            comprobar(rmV.Moral != null && rmV.Moral.SinVacilar && rmV.Moral.SinHuida, "moral del jugador: «sin flaquear» implica «sin huida»");
            comprobar(rm.Moral != null && rm.Moral.SinHuida && rm.Moral.SinVacilar && rm.ModuloMoral == "ZeroMoralA" && rm.Avisos.Any(a => a.Contains("moral"))
                      && rm0.Moral == null && rmDef.Moral != null && rmDef.Moral.SinHuida && rmDef.Moral.SinVacilar && r2.Moral == null,
                "moral del jugador: dos módulos se juntan con aviso; todo false = de serie; sin campos = los dos por defecto; sin el efecto, null");
            comprobar(Modulos.CompararVersiones("0.7.0", "0.7.0") == 0 && Modulos.CompararVersiones("0.7.0", "0.10.0") < 0 && Modulos.CompararVersiones("1.0", "0.9.9") > 0,
                "versiones: 0.7.0 = 0.7.0, 0.7.0 < 0.10.0, 1.0 > 0.9.9");

            // --- Contenido: fusión con rutas completas y conflictos ---
            // --- Content: merge with full paths and conflicts ---
            string parteA = "{\"unidades\":[{\"id\":941,\"base\":\"Oni\",\"raza\":\"IronLegion\",\"activa\":true,\"reclutamiento\":\"raza\"}]," +
                            "\"objetos\":[{\"id\":720,\"rareza\":\"Common\",\"respaldo\":\"ArmingSwords\",\"icono\":\"objetos\\\\720.png\",\"reglas\":[{\"tipo\":\"stat\",\"condicion\":{\"filterKind\":\"Unconditional\"},\"stat\":\"MeleeAttack\",\"valor\":\"1\"}]}]}";
            string parteB = "{\"unidades\":[{\"id\":941,\"base\":\"Oni\"},{\"id\":900,\"base\":\"EisenmannRegiment\",\"modelo\":\"unit_visuals\\\\_900\"}]," +
                            "\"razas\":[{\"id\":40,\"activa\":true}],\"heroes\":[{\"id\":901,\"raza\":40,\"retrato\":\"heroes\\\\901.png\",\"activa\":true}]}";
            var ca = Activo(Modulo(raiz, "ZeroA", new { id = "ZeroA", contenido = "facciones_manifiesto.json" }, parteA));
            var cb = Activo(Modulo(raiz, "ZeroB", new { id = "ZeroB", contenido = "facciones_manifiesto.json" }, parteB));
            var r3 = Modulos.Resolver(new[] { ca, cb }, "0.7.0");
            var u900 = r3.Manifiesto.Unidades.Find(u => u.Id == 900);
            var o720 = r3.Manifiesto.Objetos.Find(o => o.Id == 720);
            var h901 = r3.Manifiesto.Heroes.Find(h => h.Id == 901);
            comprobar(r3.Manifiesto.Unidades.Count == 2 && r3.Avisos.Any(a => a.Contains("941")) && u900?.Modulo == "ZeroB" && u900.Carpeta == cb.Carpeta
                      && r3.Manifiesto.Unidades.Find(u => u.Id == 941).Modulo == "ZeroA",
                "contenido: la 941 repetida se queda con el primer módulo (con aviso); cada unidad apunta su módulo y carpeta");
            comprobar(o720 != null && o720.Icono == Path.Combine(ca.Carpeta, "objetos", "720.png") && h901 != null && h901.Retrato == Path.Combine(cb.Carpeta, "heroes", "901.png")
                      && r3.Manifiesto.Razas.Count == 1 && r3.HayContenido,
                "contenido: icono y retrato pasan a ruta completa dentro de la carpeta de su módulo");
            var soloEfectos = Modulos.Resolver(new[] { e50, mun }, "0.7.0");
            comprobar(!soloEfectos.HayContenido && soloEfectos.Manifiesto.Unidades.Count == 0, "solo módulos de efectos: sin contenido (la fábrica no se usa)");

            PruebaOrden(comprobar);
            PruebaSincronizacion(comprobar);
            PruebaSincronizacionFicheros(comprobar);
            PruebaModlistEstricto(comprobar);
            PruebaCambiosDePerfil(comprobar);

            // --- Omisión de parches (Diagnostico.OmitirParches): el bloqueo de clasificaciones no se omite con módulos aplicados ---
            // --- Patch skipping (Diagnostico.OmitirParches): the ranking block is not skipped when modules are applied ---
            comprobar(Omisiones.EnLista("ToJson, submit ,WriteAllText", "Submit") && !Omisiones.EnLista("ToJson,SubmitX", "Submit") && !Omisiones.EnLista(null, "Submit"),
                "omisiones: la lista se lee sin distinguir mayúsculas, con espacios, y solo por nombre exacto");
            comprobar(!Omisiones.SePuedeOmitir("Submit", true, true) && Omisiones.SePuedeOmitir("Submit", false, true)
                      && Omisiones.SePuedeOmitir("Submit", true, false) && Omisiones.SePuedeOmitir("ToJson", true, true),
                "omisiones: Submit no se omite con módulos aplicados y el bloqueo activo; sin módulos, con el bloqueo apagado u otro método, sí");
            PruebaRegistroObjetos(comprobar);
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // ---------------------------------------------------------------
    // Orden de mods (réplica del juego) y desactivados por defecto
    // Mod order (replica of the game) and disabled by default
    // ---------------------------------------------------------------
    private static void PruebaOrden(Action<bool, string> comprobar)
    {
        string mods = Temporal();
        try
        {
            foreach (string n in new[] { "B", "A", "C", "Nuevo2", "Nuevo1", "_Plantilla" }) Modulo(mods, n, new { id = n });
            Directory.CreateDirectory(Path.Combine(mods, "SinModJson"));
            File.WriteAllText(Path.Combine(mods, "modlist.json"),
                "{\"loadOrder\":[{\"folder\":\"C\",\"enabled\":true},{\"folder\":\"A\",\"enabled\":false},{\"folder\":\"Falta\",\"enabled\":true},{\"folder\":\"B\",\"enabled\":true}]}");
            var activas = OrdenMods.CarpetasActivas(mods);
            comprobar(activas.SequenceEqual(new[] { "C", "B", "Nuevo1", "Nuevo2" }),
                $"orden de mods como el juego: lista en su orden sin desactivados ni carpetas que faltan, nuevas al final por orden alfabético, sin «_» ni carpetas sin mod.json ({string.Join(",", activas)})");

            // Desactivados por defecto: se apuntan una vez, con copia previa, conservando lo que había
            // Disabled by default: recorded once, with a prior backup, keeping what was there
            string d100 = Modulo(mods, "ZeroEscuadras100", Escuadras("ZeroEscuadras100", 2.0, false));
            string d150 = Modulo(mods, "ZeroEscuadras150", Escuadras("ZeroEscuadras150", 2.5, false));
            Modulo(mods, "ZeroEscuadras50", Escuadras("ZeroEscuadras50", 1.5, true));
            string copias = Path.Combine(mods, "..", Path.GetFileName(mods) + "_copias");
            var encontrados = Modulos.Buscar(mods, null);
            var puestos = OrdenMods.ApuntarDesactivados(mods, encontrados, copias);
            var lista = OrdenMods.LeerLista(mods, out string motivo);
            var activas2 = OrdenMods.CarpetasActivas(mods);
            comprobar(puestos.OrderBy(x => x).SequenceEqual(new[] { "ZeroEscuadras100", "ZeroEscuadras150" }) && motivo == null
                      && lista.Take(4).Select(e => e.Folder).SequenceEqual(new[] { "C", "A", "Falta", "B" }) && !lista[1].Enabled
                      && lista.Any(e => e.Folder == "ZeroEscuadras100" && !e.Enabled) && !lista.Any(e => e.Folder == "ZeroEscuadras50")
                      && Directory.GetFiles(copias, "modlist-*.json").Length == 1,
                "desactivados por defecto: +100 y +150 % apuntados con enabled false, lo que había se conserva en su orden, +50 % no se apunta (lo activa el juego) y hay copia previa");
            comprobar(!activas2.Contains("ZeroEscuadras100") && !activas2.Contains("ZeroEscuadras150") && activas2.Contains("ZeroEscuadras50"),
                "tras apuntarlos, el juego vería +50 % activo y +100 / +150 % desactivados");
            var otra = OrdenMods.ApuntarDesactivados(mods, Modulos.Buscar(mods, null), copias);
            comprobar(otra.Count == 0 && Directory.GetFiles(copias, "modlist-*.json").Length == 1, "una segunda vez no apunta nada (el usuario manda desde el menú) ni hace otra copia");
            // El usuario activa el +100 % desde el menú: el núcleo no lo vuelve a desactivar
            // The user enables the +100 % from the menu: the core does not disable it again
            var l = OrdenMods.LeerLista(mods, out _);
            l.First(e => e.Folder == "ZeroEscuadras100").Enabled = true;
            OrdenMods.EscribirLista(mods, l);
            OrdenMods.ApuntarDesactivados(mods, Modulos.Buscar(mods, null), copias);
            comprobar(OrdenMods.CarpetasActivas(mods).Contains("ZeroEscuadras100"), "si el usuario lo activa en el menú, se queda activo");
            // Sin modlist.json: se crea solo con las entradas desactivadas (las demás carpetas siguen siendo nuevas = activas)
            // No modlist.json: it is created with only the disabled entries (the other folders are still new = enabled)
            File.Delete(Path.Combine(mods, "modlist.json"));
            OrdenMods.ApuntarDesactivados(mods, Modulos.Buscar(mods, null), copias);
            var sinLista = OrdenMods.CarpetasActivas(mods);
            comprobar(!sinLista.Contains("ZeroEscuadras100") && sinLista.Contains("A") && sinLista.Contains("ZeroEscuadras50"),
                "sin modlist.json se crea con los desactivados por defecto y el resto de carpetas siguen activas");
            // Formato: lo lee el lector del juego (JsonUtility) igual; aquí, que sea JSON válido con loadOrder
            // Format: the game's reader (JsonUtility) reads it the same way; here, it just needs to be valid JSON with loadOrder
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(mods, "modlist.json")));
            comprobar(doc.RootElement.GetProperty("loadOrder").GetArrayLength() == 2, "modlist.json escrito es JSON válido con loadOrder");
            _ = d100; _ = d150;
        }
        finally { try { Directory.Delete(mods, true); Directory.Delete(mods + "_copias", true); } catch { } }
    }

    // ---------------------------------------------------------------
    // Sincronización desde BepInEx\plugins (gestor tipo Thunderstore / r2modman)
    // Sync from BepInEx\plugins (Thunderstore / r2modman-style manager)
    // ---------------------------------------------------------------
    private static void PruebaSincronizacion(Action<bool, string> comprobar)
    {
        string raiz = Temporal();
        try
        {
            string plugins = Path.Combine(raiz, "BepInEx", "plugins");
            string mods = Path.Combine(raiz, "Mods");
            Directory.CreateDirectory(mods);
            // Paquete de gestor: BepInEx\plugins\Zero-ZeroEscuadras100\ZeroEscuadras100\...
            // Manager package: BepInEx\plugins\Zero-ZeroEscuadras100\ZeroEscuadras100\...
            string fuente = Modulo(Path.Combine(plugins, "Zero-ZeroEscuadras100"), "ZeroEscuadras100", Escuadras("ZeroEscuadras100", 2.0, false));
            Directory.CreateDirectory(Path.Combine(fuente, "sub"));
            File.WriteAllText(Path.Combine(fuente, "sub", "dato.txt"), "1");
            // Otro con la carpeta con nombre distinto del id (se copia con el id)
            // Another one with the folder named differently from the id (copied using the id)
            Modulo(Path.Combine(plugins, "Zero-Municion"), "plugins", new { id = "ZeroMunicion", efectos = new { municionJugador = new { multiplicador = 3 } } });
            // Instalado a mano en Mods (sin marcador) y también en el gestor: no se toca
            // Manually installed in Mods (no marker) and also in the manager: left untouched
            Modulo(mods, "ZeroStatsDistancia", new { id = "ZeroStatsDistancia", version = "1.0.0" });
            File.WriteAllText(Path.Combine(mods, "ZeroStatsDistancia", "mano.txt"), "a mano");
            Modulo(Path.Combine(plugins, "Zero-Stats"), "ZeroStatsDistancia", new { id = "ZeroStatsDistancia", version = "1.1.0" });

            var i1 = SincronizarModulos.Sincronizar(plugins, mods);
            string copia = Path.Combine(mods, "ZeroEscuadras100");
            comprobar(i1.Fuentes == 3 && i1.Copiados.OrderBy(x => x).SequenceEqual(new[] { "ZeroEscuadras100", "ZeroMunicion" }) && File.Exists(Path.Combine(copia, "sub", "dato.txt"))
                      && File.Exists(Path.Combine(copia, SincronizarModulos.Marcador)) && Directory.Exists(Path.Combine(mods, "ZeroMunicion")),
                $"sincronización: los módulos del gestor se copian a Mods con su marcador (también subcarpetas; la carpeta toma el id) [{i1}]");
            comprobar(i1.AMano.SequenceEqual(new[] { "ZeroStatsDistancia" }) && File.Exists(Path.Combine(mods, "ZeroStatsDistancia", "mano.txt")) && i1.Avisos.Any(a => a.Contains("1.1.0")),
                "sincronización: una instalación a mano (sin marcador) no se toca y se avisa de la versión distinta del gestor");
            var i2 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i2.Iguales.Count == 2 && i2.Copiados.Count == 0 && i2.Actualizados.Count == 0, "sincronización: sin cambios en la segunda vuelta");
            File.WriteAllText(Path.Combine(fuente, "sub", "dato.txt"), "2");
            var i3 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i3.Actualizados.SequenceEqual(new[] { "ZeroEscuadras100" }) && File.ReadAllText(Path.Combine(copia, "sub", "dato.txt")) == "2",
                "sincronización: si el paquete cambia (huella), se actualiza la copia");
            // Se desinstala el paquete: la copia se aparta a Mods\_ZeroRetirados (el juego ignora las carpetas con «_»)
            // The package is uninstalled: the copy is set aside in Mods\_ZeroRetirados (the game ignores folders with «_»)
            Directory.Move(Path.Combine(plugins, "Zero-ZeroEscuadras100"), Path.Combine(raiz, "fuera"));
            var i4 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i4.Retirados.SequenceEqual(new[] { "ZeroEscuadras100" }) && !Directory.Exists(copia)
                      && Directory.GetDirectories(Path.Combine(mods, SincronizarModulos.CarpetaRetirados)).Length == 1
                      && !OrdenMods.CarpetasActivas(mods).Any(n => n.StartsWith("_")) && Directory.Exists(Path.Combine(mods, "ZeroStatsDistancia")),
                "sincronización: paquete desinstalado -> su copia pasa a Mods\\_ZeroRetirados (nada se borra; el juego no la carga); lo puesto a mano sigue");
            var i5 = SincronizarModulos.Sincronizar(Path.Combine(raiz, "no_existe"), mods);
            comprobar(i5.Retirados.Count == 0 && Directory.Exists(Path.Combine(mods, "ZeroMunicion")), "si BepInEx\\plugins no se puede recorrer, no se retira nada");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // ---------------------------------------------------------------
    // Sincronización fichero a fichero: obsoletos, ediciones locales, marcadores antiguos, retirada; con espacios y acentos
    // File-by-file sync: obsolete files, local edits, old markers, removal; with spaces and accents
    // ---------------------------------------------------------------
    private static void PruebaSincronizacionFicheros(Action<bool, string> comprobar)
    {
        // Raíz con espacios y acentos (como C:\Users\Zoë\Mis Juegos\…)
        // Root with spaces and accents (like C:\Users\Zoë\Mis Juegos\…)
        string raiz = Path.Combine(Temporal(), "Mis Juegos ñandú á");
        try
        {
            string plugins = Path.Combine(raiz, "BepInEx Ñ", "plugins");
            string mods = Path.Combine(raiz, "Mods de prueba");
            string retirados = Path.Combine(mods, SincronizarModulos.CarpetaRetirados);
            Directory.CreateDirectory(mods);

            // Paquete con overrides, un audio de nombre acentuado en una subcarpeta y una configuración
            // Package with overrides, an audio file with an accented name in a subfolder, and a configuration
            string fuente = Modulo(Path.Combine(plugins, "Zero-Guardia Imperial"), "ZeroGuardia", new { id = "ZeroGuardia", version = "1.0.0" });
            Escribir(fuente, "unit_overrides.json", "v1");
            Escribir(fuente, "localization_overrides.json", "loc1");
            Escribir(fuente, "sonidos/canción de guerra.txt", "c1");
            Escribir(fuente, "datos/configuración.json", "{\"a\":1}");
            string copia = Path.Combine(mods, "ZeroGuardia");

            // 1) Primera copia: el marcador apunta la huella de cada fichero (claves con acentos incluidas)
            // 1) First copy: the marker records the fingerprint of each file (including keys with accents)
            var i1 = SincronizarModulos.Sincronizar(plugins, mods);
            var lista1 = FicherosDelMarcador(copia);
            comprobar(i1.Copiados.SequenceEqual(new[] { "ZeroGuardia" }) && Leer(copia, "sonidos/canción de guerra.txt") == "c1"
                      && lista1 != null && lista1.Count == 6 && lista1.Contains("sonidos/canción de guerra.txt") && lista1.Contains("datos/configuración.json"),
                $"sincronización por ficheros: la copia (rutas con espacios y acentos) guarda en el marcador la huella de cada fichero [{i1}]");

            // 2) Edición local sin cambio en el paquete: no se toca nada
            // 2) Local edit with no change in the package: nothing is touched
            Escribir(copia, "datos/configuración.json", "mío");
            var i2 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i2.Iguales.SequenceEqual(new[] { "ZeroGuardia" }) && Leer(copia, "datos/configuración.json") == "mío" && !Directory.Exists(retirados),
                "sincronización por ficheros: una edición local se conserva mientras el paquete no cambie");

            // 3) Actualización: el paquete retira unit_overrides.json, cambia dos ficheros (uno editado en la copia) y trae uno nuevo;
            //    el usuario además añadió un fichero suyo a la copia
            // 3) Update: the package removes unit_overrides.json, changes two files (one edited in the copy) and brings a new one;
            //    the user also added a file of their own to the copy
            Escribir(copia, "extra del usuario.txt", "mío también");
            File.Delete(Path.Combine(fuente, "unit_overrides.json"));
            Escribir(fuente, "sonidos/canción de guerra.txt", "c2");
            Escribir(fuente, "datos/configuración.json", "{\"a\":2}");
            Escribir(fuente, "nuevo/añadido.txt", "n1");
            var i3 = SincronizarModulos.Sincronizar(plugins, mods);
            string[] resp3 = Directory.Exists(retirados) ? Directory.GetDirectories(retirados, "ZeroGuardia-*-actualizacion") : new string[0];
            string r3 = resp3.Length == 1 ? resp3[0] : null;
            comprobar(i3.Actualizados.SequenceEqual(new[] { "ZeroGuardia" }) && !File.Exists(Path.Combine(copia, "unit_overrides.json"))
                      && r3 != null && Leer(r3, "unit_overrides.json") == "v1" && i3.Obsoletos.SequenceEqual(new[] { "ZeroGuardia/unit_overrides.json" }),
                $"sincronización por ficheros: un fichero que el paquete ya no trae sale de la copia (ya no se carga) y queda apartado en _ZeroRetirados [{i3}]");
            comprobar(r3 != null && Leer(copia, "datos/configuración.json") == "{\"a\":2}" && Leer(r3, "datos/configuración.json") == "mío"
                      && i3.Respaldados.SequenceEqual(new[] { "ZeroGuardia/datos/configuración.json" }) && i3.Avisos.Any(a => a.Contains("configuración.json") && a.Contains("editado")),
                "sincronización por ficheros: un fichero editado en la copia se respalda (con aviso) antes de poner el del paquete");
            comprobar(r3 != null && Leer(copia, "sonidos/canción de guerra.txt") == "c2" && !File.Exists(Path.Combine(r3, "sonidos", "canción de guerra.txt"))
                      && Leer(copia, "nuevo/añadido.txt") == "n1" && Leer(copia, "localization_overrides.json") == "loc1",
                "sincronización por ficheros: lo no editado se actualiza sin respaldo, lo nuevo se copia y lo igual se queda");
            var lista3 = FicherosDelMarcador(copia);
            comprobar(Leer(copia, "extra del usuario.txt") == "mío también" && i3.Avisos.Any(a => a.Contains("extra del usuario.txt"))
                      && lista3 != null && !lista3.Contains("unit_overrides.json") && lista3.Contains("nuevo/añadido.txt") && !lista3.Contains("extra del usuario.txt"),
                "sincronización por ficheros: lo que añadió el usuario se deja (con aviso) y el marcador queda con la lista nueva");

            // 4) Otra vuelta sin cambios: nada que hacer, ningún respaldo más
            // 4) Another pass with no changes: nothing to do, no further backup
            var i4 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i4.Iguales.SequenceEqual(new[] { "ZeroGuardia" }) && i4.Obsoletos.Count == 0 && i4.Respaldados.Count == 0 && Directory.GetDirectories(retirados).Length == 1,
                "sincronización por ficheros: sin cambios en la vuelta siguiente (no se repiten respaldos)");

            // 4b) Política: si el paquete cambia, la copia queda igual que el paquete aunque el fichero editado no cambiara en él
            //     (la edición local queda respaldada)
            // 4b) Policy: if the package changes, the copy ends up identical to the package even if the edited file did not change in it
            //     (the local edit is backed up)
            Escribir(copia, "localization_overrides.json", "loc mío");
            Escribir(fuente, "sonidos/canción de guerra.txt", "c3");
            var i4b = SincronizarModulos.Sincronizar(plugins, mods);
            bool respaldoLoc = Directory.GetDirectories(retirados, "ZeroGuardia-*").Any(d => Leer(d, "localization_overrides.json") == "loc mío");
            comprobar(i4b.Respaldados.SequenceEqual(new[] { "ZeroGuardia/localization_overrides.json" }) && respaldoLoc
                      && Leer(copia, "localization_overrides.json") == "loc1" && Leer(copia, "sonidos/canción de guerra.txt") == "c3",
                "sincronización por ficheros: con el paquete cambiado, una edición local de un fichero que el paquete no cambió también se respalda y se repone");

            // 5) Marcadores de la versión anterior (sin lista de ficheros)
            // 5) Markers from the previous version (without a file list)
            string fLeg = Modulo(Path.Combine(plugins, "Zero-Legado"), "ZeroLegado", new { id = "ZeroLegado", version = "1.0.0" });
            Escribir(fLeg, "a.txt", "a1");
            Escribir(fLeg, "viejo.json", "x");
            string cLeg = Path.Combine(mods, "ZeroLegado");
            SincronizarModulos.Sincronizar(plugins, mods);
            QuitarListaDelMarcador(cLeg);
            var i5 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i5.Iguales.Contains("ZeroLegado") && FicherosDelMarcador(cLeg)?.Contains("a.txt") == true,
                "marcador antiguo: con el paquete sin cambios se completa la lista de ficheros sin tocar la copia");
            // 5b) Antiguo e intacto: la copia es del núcleo entera; se aparta lo obsoleto y no se respalda nada más
            // 5b) Old and untouched: the copy is entirely from the core; obsolete files are set aside and nothing else is backed up
            QuitarListaDelMarcador(cLeg);
            File.Delete(Path.Combine(fLeg, "viejo.json"));
            Escribir(fLeg, "a.txt", "a2");
            var i6 = SincronizarModulos.Sincronizar(plugins, mods);
            comprobar(i6.Actualizados.Contains("ZeroLegado") && i6.Obsoletos.SequenceEqual(new[] { "ZeroLegado/viejo.json" }) && i6.Respaldados.Count == 0
                      && !File.Exists(Path.Combine(cLeg, "viejo.json")) && Leer(cLeg, "a.txt") == "a2" && FicherosDelMarcador(cLeg) != null,
                $"marcador antiguo con la copia intacta: se aparta lo obsoleto y se actualiza sin respaldos [{i6}]");
            // 5c) Antiguo y editado: no se sabe qué tocó el usuario, se respalda lo que se pisa (con aviso)
            // 5c) Old and edited: it is unknown what the user touched, so whatever gets overwritten is backed up (with a warning)
            QuitarListaDelMarcador(cLeg);
            Escribir(cLeg, "a.txt", "mío");
            Escribir(fLeg, "a.txt", "a3");
            var i7 = SincronizarModulos.Sincronizar(plugins, mods);
            bool respaldoLeg = Directory.GetDirectories(retirados, "ZeroLegado-*").Any(d => File.Exists(Path.Combine(d, "a.txt")) && Leer(d, "a.txt") == "mío");
            comprobar(i7.Actualizados.Contains("ZeroLegado") && i7.Respaldados.SequenceEqual(new[] { "ZeroLegado/a.txt" }) && respaldoLeg && Leer(cLeg, "a.txt") == "a3"
                      && i7.Avisos.Any(a => a.Contains("versión anterior")),
                "marcador antiguo con la copia editada: se respalda lo que se pisa y se avisa");

            // 6) Módulo retirado (paquete desinstalado) con ediciones dentro: la carpeta entera pasa a _ZeroRetirados, nada se pierde
            // 6) Removed module (package uninstalled) with edits inside: the whole folder goes to _ZeroRetirados, nothing is lost
            Escribir(copia, "datos/configuración.json", "mío2");
            Directory.Move(Path.Combine(plugins, "Zero-Guardia Imperial"), Path.Combine(raiz, "fuera del gestor"));
            var i8 = SincronizarModulos.Sincronizar(plugins, mods);
            string[] ret8 = Directory.GetDirectories(retirados, "ZeroGuardia-*").Where(d => !Path.GetFileName(d).Contains("-actualizacion")).ToArray();
            comprobar(i8.Retirados.SequenceEqual(new[] { "ZeroGuardia" }) && !Directory.Exists(copia) && ret8.Length == 1
                      && Leer(ret8[0], "datos/configuración.json") == "mío2" && Leer(ret8[0], "extra del usuario.txt") == "mío también"
                      && Directory.Exists(cLeg) && !OrdenMods.CarpetasActivas(mods).Contains("ZeroGuardia"),
                $"módulo retirado: la copia entera (con lo editado y lo añadido) pasa a _ZeroRetirados y el juego deja de cargarla [{i8}]");
        }
        finally { try { Directory.Delete(Path.GetDirectoryName(raiz), true); } catch { } }
    }

    // ---------------------------------------------------------------
    // modlist.json estricto como JsonUtility: con comentarios o comas finales el juego no la lee y activa todas las carpetas
    // modlist.json is strict like JsonUtility: with comments or trailing commas the game does not read it and enables all folders
    // ---------------------------------------------------------------
    private static void PruebaModlistEstricto(Action<bool, string> comprobar)
    {
        string mods = Temporal();
        try
        {
            foreach (string n in new[] { "B", "A", "C" }) Modulo(mods, n, new { id = n });
            File.WriteAllText(Path.Combine(mods, "modlist.json"), "{\"loadOrder\":[{\"folder\":\"C\",\"enabled\":true},{\"folder\":\"A\",\"enabled\":false}]}");
            comprobar(OrdenMods.CarpetasActivas(mods).SequenceEqual(new[] { "C", "B" }), "modlist.json estricta y válida: se respeta (A desactivado)");

            // Coma final: el juego no la lee -> todas activas por orden alfabético (también A, desactivada en la lista)
            // Trailing comma: the game does not read it -> all enabled in alphabetical order (also A, disabled in the list)
            File.WriteAllText(Path.Combine(mods, "modlist.json"), "{\"loadOrder\":[{\"folder\":\"C\",\"enabled\":true},{\"folder\":\"A\",\"enabled\":false},]}");
            var l1 = OrdenMods.LeerLista(mods, out string m1);
            var a1 = OrdenMods.CarpetasActivas(mods);
            comprobar(l1 == null && m1 != null && a1.SequenceEqual(new[] { "A", "B", "C" }),
                $"modlist.json con coma final: ilegible como para el juego -> todas activas en orden alfabético ({string.Join(",", a1)})");

            // Comentario: igual
            // Comment: same
            File.WriteAllText(Path.Combine(mods, "modlist.json"), "{\n // mis mods\n \"loadOrder\":[{\"folder\":\"A\",\"enabled\":false}]}");
            var l2 = OrdenMods.LeerLista(mods, out string m2);
            var a2 = OrdenMods.CarpetasActivas(mods);
            comprobar(l2 == null && m2 != null && a2.SequenceEqual(new[] { "A", "B", "C" }),
                $"modlist.json con comentario: ilegible como para el juego -> todas activas ({string.Join(",", a2)})");

            // Con la lista ilegible no se reescribe (no se apuntan desactivados) y se avisa
            // With an unreadable list it is not rewritten (disabled ones are not recorded) and a warning is issued
            Modulo(mods, "ZeroEscuadras100", Escuadras("ZeroEscuadras100", 2.0, false));
            var avisos = new List<string>();
            string antes = File.ReadAllText(Path.Combine(mods, "modlist.json"));
            var puestos = OrdenMods.ApuntarDesactivados(mods, Modulos.Buscar(mods, null), Path.Combine(mods, "_copias"), avisos);
            comprobar(puestos.Count == 0 && File.ReadAllText(Path.Combine(mods, "modlist.json")) == antes && avisos.Any(a => a.Contains("modlist.json")),
                "modlist.json ilegible: no se toca y se avisa (el núcleo deduce lo mismo que el juego)");
        }
        finally { try { Directory.Delete(mods, true); } catch { } }
    }

    // ---------------------------------------------------------------
    // Cambios de perfil del gestor (r2modman): cada perfil tiene su BepInEx\plugins; el módulo solo está en el perfil A
    // Manager profile switches (r2modman): each profile has its own BepInEx\plugins; the module is only in profile A
    // ---------------------------------------------------------------
    private static void PruebaCambiosDePerfil(Action<bool, string> comprobar)
    {
        string raiz = Temporal();
        try
        {
            string pluginsA = Path.Combine(raiz, "perfiles", "Perfil A", "BepInEx", "plugins");
            string pluginsB = Path.Combine(raiz, "perfiles", "Perfil B", "BepInEx", "plugins");
            Directory.CreateDirectory(pluginsB);
            string mods = Path.Combine(raiz, "Mods");
            string retirados = Path.Combine(mods, SincronizarModulos.CarpetaRetirados);
            Directory.CreateDirectory(mods);
            string fuente = Modulo(Path.Combine(pluginsA, "Zero-Municion"), "ZeroMunicion", new { id = "ZeroMunicion", version = "1.0.0" });
            Escribir(fuente, "unit_overrides.json", "u1");
            string copia = Path.Combine(mods, "ZeroMunicion");

            // A -> B tres veces: la copia sale de Mods en cada arranque con B, pero solo se guarda una retirada
            // A -> B three times: the copy is removed from Mods on every launch with B, but only one removal is saved
            int retiradasEnB = 0;
            for (int i = 0; i < 3; i++)
            {
                SincronizarModulos.Sincronizar(pluginsA, mods);
                var iB = SincronizarModulos.Sincronizar(pluginsB, mods);
                if (iB.Retirados.SequenceEqual(new[] { "ZeroMunicion" }) && !Directory.Exists(copia)) retiradasEnB++;
            }
            int guardadas = Directory.Exists(retirados) ? Directory.GetDirectories(retirados, "ZeroMunicion-*").Length : 0;
            comprobar(retiradasEnB == 3 && guardadas == 1,
                $"cambios de perfil: la copia sale de Mods en cada arranque con el perfil sin el módulo, pero en _ZeroRetirados queda una sola ({guardadas})");

            // Si la copia tenía cambios propios (contenido distinto), sí se guarda aparte: nada se pierde
            // If the copy had its own changes (different content), it is saved separately: nothing is lost
            SincronizarModulos.Sincronizar(pluginsA, mods);
            Escribir(copia, "unit_overrides.json", "editado");
            var iB2 = SincronizarModulos.Sincronizar(pluginsB, mods);
            var dirs = Directory.GetDirectories(retirados, "ZeroMunicion-*");
            comprobar(iB2.Retirados.SequenceEqual(new[] { "ZeroMunicion" }) && dirs.Length == 2 && dirs.Any(d => Leer(d, "unit_overrides.json") == "editado")
                      && dirs.Any(d => Leer(d, "unit_overrides.json") == "u1"),
                "cambios de perfil: una copia con contenido distinto se guarda aparte (no se descarta nada que no esté ya guardado)");

            // Un respaldo parcial «-actualizacion» con el mismo contenido no cuenta como retirada igual
            // A partial backup «-actualizacion» with the same content does not count as an identical removal
            string parcial = Path.Combine(retirados, "ZeroMunicion-20260101-000000-actualizacion");
            foreach (string d in Directory.GetDirectories(retirados, "ZeroMunicion-*")) Directory.Move(d, Path.Combine(raiz, Path.GetFileName(d)));
            SincronizarModulos.Sincronizar(pluginsA, mods);
            Directory.CreateDirectory(parcial);
            foreach (string f in Directory.GetFiles(copia)) if (Path.GetFileName(f) != SincronizarModulos.Marcador) File.Copy(f, Path.Combine(parcial, Path.GetFileName(f)));
            SincronizarModulos.Sincronizar(pluginsB, mods);
            comprobar(Directory.GetDirectories(retirados, "ZeroMunicion-*").Count(d => !d.EndsWith("-actualizacion")) == 1,
                "cambios de perfil: un respaldo parcial de actualización no evita guardar la retirada entera");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // Escribe un fichero de prueba (ruta relativa con «/»), creando las subcarpetas
    // Writes a test file (relative path with «/»), creating the subfolders
    private static void Escribir(string carpeta, string rel, string texto)
    {
        string ruta = Path.Combine(carpeta, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllText(ruta, texto);
    }

    // Lee un fichero de prueba (null si no existe)
    // Reads a test file (null if it does not exist)
    private static string Leer(string carpeta, string rel)
    {
        string ruta = Path.Combine(carpeta, rel);
        return File.Exists(ruta) ? File.ReadAllText(ruta) : null;
    }

    // Lista de ficheros apuntada en el marcador de una copia (null si el marcador no la tiene)
    // List of files recorded in a copy's marker (null if the marker does not have one)
    private static List<string> FicherosDelMarcador(string copia)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(copia, SincronizarModulos.Marcador)));
        if (!doc.RootElement.TryGetProperty("Ficheros", out var f) || f.ValueKind != JsonValueKind.Object) return null;
        return f.EnumerateObject().Select(p => p.Name).ToList();
    }

    // Deja el marcador como lo escribía la versión anterior (sin lista de ficheros)
    // Leaves the marker as the previous version wrote it (without a file list)
    private static void QuitarListaDelMarcador(string copia)
    {
        string ruta = Path.Combine(copia, SincronizarModulos.Marcador);
        string json;
        using (var doc = JsonDocument.Parse(File.ReadAllText(ruta)))
        {
            var r = doc.RootElement;
            json = JsonSerializer.Serialize(new
            {
                Origen = r.GetProperty("Origen").GetString(), Huella = r.GetProperty("Huella").GetString(),
                Version = r.GetProperty("Version").GetString(), Fecha = r.GetProperty("Fecha").GetString()
            });
        }
        File.WriteAllText(ruta, json);
    }

    // ---------------------------------------------------------------
    // Registro de objetos con un módulo desactivado
    // Gear registry with a module disabled
    // ---------------------------------------------------------------
    private static void PruebaRegistroObjetos(Action<bool, string> comprobar)
    {
        var todos = new TablaObjetos(new Dictionary<int, int> { { 700, 17 }, { 720, 4 } }, Enumerable.Range(1, 40));
        var soloAstra = new TablaObjetos(new Dictionary<int, int> { { 700, 17 } }, Enumerable.Range(1, 40));
        var reg = new RegistroGuardias(null);
        string conAmbos = "{\"Gear\": [1,720,700], \"SoldGear\": [720], \"snapShot\": false}";
        string disco = SaneadoObjetos.ParaDisco(conAmbos, todos, reg, true);
        // Se desactiva el módulo de sueltas (720): al leer no se restaura y al guardar no se olvida
        // The loose items module (720) is disabled: it is not restored on read and not forgotten on save
        string leido = SaneadoObjetos.DesdeDisco(disco, soloAstra, reg);
        string guardado = SaneadoObjetos.ParaDisco(leido, soloAstra, reg, true);
        bool conserva = reg.ObjetosPosicion("Gear").Any(kv => kv.Value.real == 720) && reg.ObjetosLista("SoldGear").Contains(720);
        comprobar(!leido.Contains("720") && leido.Contains("700") && conserva && guardado == disco,
            "registro de objetos: con el módulo del 720 desactivado no se restaura, pero lo apuntado se conserva al guardar");
        string vuelta = SaneadoObjetos.DesdeDisco(guardado, todos, reg);
        comprobar(vuelta == conAmbos, "registro de objetos: al reactivar el módulo, el 720 vuelve a su sitio (Gear y vendidos)");
    }

    // ---------------------------------------------------------------
    // Módulos reales (carpeta maestra generada)
    // Real modules (generated master folder)
    // ---------------------------------------------------------------
    public static void Reales(string carpeta, Action<bool, string> comprobar)
    {
        var todos = Modulos.Buscar(carpeta, Directory.GetDirectories(carpeta).Select(Path.GetFileName).ToList());
        string[] esperados = { "ZeroAstraMilitarum", "ZeroEconomia", "ZeroEscuadras100", "ZeroEscuadras150", "ZeroEscuadras50", "ZeroImperioGalactico",
                               "ZeroImperterritos", "ZeroMunicion", "ZeroMunicion2", "ZeroMunicion5", "ZeroStatsDistancia", "ZeroUnidadesSueltas",
                               "ZeroHeraldica" };   // 0.7.6: + Impertérritos; 0.7.7: + Heráldica; 03-11 noche: + Munición x2 y x5
                                                    // 0.7.6: + Impertérritos; 0.7.7: + Heráldica; 03-11 night: + Munición x2 and x5
        comprobar(todos.Select(m => m.Id).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(esperados.OrderBy(x => x, StringComparer.Ordinal))
                  && todos.All(m => m.Id == m.NombreCarpeta && Modulos.CompararVersiones(m.Declaracion.NucleoMinimo, Program.VersionNucleo) <= 0),
            // Revisión del 18-10: el núcleo mínimo de cada módulo no pasa de la versión actual del núcleo (antes se exigía un 0.7.0 fijo)
            // Review of 10-18: each module's minimum core does not exceed the current core version (previously a fixed 0.7.0 was required)
            $"módulos reales: los 13 esperados, cada carpeta con su id y núcleo mínimo <= {Program.VersionNucleo} ({string.Join(", ", todos.Select(m => m.Id + " " + m.Declaracion.NucleoMinimo))})");
        var porDefectoNo = todos.Where(m => !m.Declaracion.ActivoPorDefecto).Select(m => m.Id).OrderBy(x => x).ToList();
        // 03-11 noche: las variantes de Munición (x2, x5) vienen desactivadas como las de Escuadras; la x3 es la principal
        // 03-11 night: the Munición variants (x2, x5) come disabled like those of Escuadras; x3 is the main one
        comprobar(porDefectoNo.SequenceEqual(new[] { "ZeroEscuadras100", "ZeroEscuadras150", "ZeroMunicion2", "ZeroMunicion5" }),
                  "módulos reales: solo Escuadras +100/+150 % y Munición x2/x5 vienen desactivados por defecto");
        ResultadoModulos Con(params string[] ids)
        {
            var l = todos.Select(m => new ModuloEncontrado { Declaracion = m.Declaracion, Carpeta = m.Carpeta, NombreCarpeta = m.NombreCarpeta, Activo = ids.Contains(m.Id) }).ToList();
            return Modulos.Resolver(l, Program.VersionNucleo);
        }
        var nada = Con();
        comprobar(!nada.HayContenido && nada.FactorEscuadras == 1.0 && nada.Municion == null && nada.Distancia == null && nada.Moral == null, "módulos reales: ninguno activo = juego de serie");
        // 0.7.6: «Impertérritos» solo trae la moral del jugador (sin huida y sin flaquear), activo por defecto
        // 0.7.6: «Impertérritos» only brings the player's morale (no fleeing and no wavering), enabled by default
        var moral = Con("ZeroImperterritos");
        comprobar(moral.Rechazados.Count == 0 && moral.Moral != null && moral.Moral.SinHuida && moral.Moral.SinVacilar && moral.ModuloMoral == "ZeroImperterritos"
                  && !moral.HayContenido && moral.Municion == null && moral.Distancia == null && moral.FactorEscuadras == 1.0,
            "módulo Impertérritos solo: moral del jugador sin huida y sin flaquear, nada más");
        var defecto = Con(todos.Where(m => m.Declaracion.ActivoPorDefecto).Select(m => m.Id).ToArray());
        comprobar(defecto.Rechazados.Count == 0 && Math.Abs(defecto.FactorEscuadras - 1.5) < 1e-9 && defecto.Municion?.Multiplicador == 3 && defecto.Distancia?.Alcance == 10 && defecto.Moral != null
                  && defecto.Manifiesto.Unidades.Count == 60 && defecto.Manifiesto.Objetos.Count == 22 && defecto.Manifiesto.Razas.Count == 2 && defecto.Manifiesto.Heroes.Count == 4,
            "módulos reales por defecto: escuadras x1,5, munición x3, distancia +10, moral del jugador, 60 unidades, 22 objetos, 2 razas y 4 héroes");
        var tres = Con("ZeroEscuadras50", "ZeroEscuadras100", "ZeroEscuadras150");
        comprobar(Math.Abs(tres.FactorEscuadras - 2.5) < 1e-9 && tres.Avisos.Any(a => a.Contains("manda el mayor")), "módulos reales: las tres escuadras juntas -> x2,5 con aviso");
        var astra = Con("ZeroAstraMilitarum");
        var imperio = Con("ZeroImperioGalactico");
        var sueltas = Con("ZeroUnidadesSueltas");
        comprobar(astra.Manifiesto.Unidades.Select(u => u.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(900, 14)) && astra.Manifiesto.Objetos.Select(o => o.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(700, 7))
                  && astra.Manifiesto.Razas.Select(r => r.Id).SequenceEqual(new[] { 40 }) && astra.Manifiesto.Heroes.Select(h => h.Id).OrderBy(x => x).SequenceEqual(new[] { 901, 902 }),
            "módulo Astra Militarum solo: 900-913 (con la 900), objetos 700-706, raza 40 y héroes 901-902");
        comprobar(imperio.Manifiesto.Unidades.Select(u => u.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(921, 14)) && imperio.Manifiesto.Objetos.Select(o => o.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(710, 7))
                  && imperio.Manifiesto.Razas.Select(r => r.Id).SequenceEqual(new[] { 41 }) && imperio.Manifiesto.Heroes.Select(h => h.Id).OrderBy(x => x).SequenceEqual(new[] { 903, 904 }),
            "módulo Imperio Galáctico solo: 921-934, objetos 710-716, raza 41 y héroes 903-904 (sin la 900)");
        comprobar(sueltas.Manifiesto.Unidades.Select(u => u.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(941, 32)) && sueltas.Manifiesto.Objetos.Select(o => o.Id).OrderBy(x => x).SequenceEqual(Enumerable.Range(720, 8))
                  && sueltas.Manifiesto.Razas.Count == 0 && sueltas.Manifiesto.Heroes.Count == 0,
            "módulo de unidades sueltas solo: 941-972, objetos 720-727, sin razas ni héroes");
        var faccionesSinSueltas = Con("ZeroAstraMilitarum", "ZeroImperioGalactico");
        comprobar(faccionesSinSueltas.Manifiesto.Unidades.Count == 28 && !faccionesSinSueltas.Manifiesto.Unidades.Any(u => u.Id >= 941) && faccionesSinSueltas.Manifiesto.Objetos.Count == 14
                  && faccionesSinSueltas.Manifiesto.Heroes.All(h => h.Muestra.All(id => faccionesSinSueltas.Manifiesto.Unidades.Any(u => u.Id == id))),
            "facciones sin sueltas: 28 unidades y 14 objetos; los ejércitos de muestra de los héroes no necesitan las sueltas");
        comprobar(astra.Manifiesto.Heroes.All(h => File.Exists(h.Retrato)) && astra.Manifiesto.Objetos.All(o => File.Exists(o.Icono))
                  && Directory.Exists(Path.Combine(astra.Manifiesto.Unidades.Find(u => u.Id == 900).Carpeta, "unit_visuals", "_900"))
                  && imperio.Manifiesto.Heroes.All(h => File.Exists(h.Retrato)) && sueltas.Manifiesto.Objetos.All(o => File.Exists(o.Icono)),
            "recursos de cada módulo dentro de su carpeta: retratos, iconos y modelo de la 900 (Astra Militarum)");
        // Autoprueba de la 944 (18-10): lee los stats esperados del unit_overrides.json del módulo de la unidad (antes eran
        // números fijos y fallaba tras cada ajuste de equilibrio). Aquí: la lectura encuentra la 944 y las unidades
        // retocadas hoy en sus módulos, con los campos numéricos legibles; una unidad que no está da null
        // Self-test of 944 (10-18): reads the expected stats from the unit's module unit_overrides.json (previously they were
        // fixed numbers and it failed after every balance tweak). Here: the read finds 944 and the units
        // tweaked today in their modules, with readable numeric fields; a unit that is not there returns null
        var todas = sueltas.Manifiesto.Unidades.Concat(imperio.Manifiesto.Unidades).ToList();
        var lecturas = new[] { 944, 959, 948, 949, 921, 929, 932, 934 }.Select(id =>
        {
            var un = todas.Find(x => x.Id == id);
            var ov = un == null ? null : Modulos.LeerOverrideUnidad(un.Carpeta, id);
            bool bien = ov != null && ov.TryGetValue("unitName", out var n) && n == id.ToString()
                        && int.TryParse(ov.GetValueOrDefault("baseUnitCount"), out int sb) && sb > 0
                        && int.TryParse(ov.GetValueOrDefault("hitPointsPerUnit"), out int hp) && hp > 0;
            return (id, bien, sold: ov?.GetValueOrDefault("baseUnitCount"));
        }).ToList();
        var u944 = sueltas.Manifiesto.Unidades.Find(x => x.Id == 944);
        comprobar(lecturas.All(l => l.bien) && Modulos.LeerOverrideUnidad(u944?.Carpeta, 99999) == null && Modulos.LeerOverrideUnidad(null, 944) == null,
            $"unit_overrides.json de los módulos: entradas leídas para la autoprueba ({string.Join(", ", lecturas.Select(l => $"{l.id} soldados {l.sold ?? "?"}{(l.bien ? "" : " MAL")}"))}); id inexistente y carpeta nula -> null");
    }
}
