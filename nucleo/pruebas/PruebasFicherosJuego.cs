// PruebasFicherosJuego.cs
// Prueba 32 (17-11-2026, núcleo 0.7.9): ficheros del juego de los modelos propios reconstruidos desde la instalación
// (src\FicherosJuego.cs). Con carpetas de prueba: copia y parche, rider en subcarpeta, segunda pasada sin escribir,
// fichero estropeado que se rehace, juego actualizado (la base cambia: ni copia ni parche, modelo base), base que falta, rutas que
// se salen de la carpeta, declaración rota, parche que no cuadra y la sincronización desde BepInEx\plugins (lo
// reconstruido no se toma por un fichero del usuario). Con la carpeta de módulos real y el juego instalado: cada
// declaración se reconstruye entera desde el juego con el SHA-256 declarado, y cada modelo tiene todos sus ficheros
// (en el módulo o declarados) y ninguno con nombre de fichero del juego sin declarar.
// Autor: ZeroitMods.
// PruebasFicherosJuego.cs
// Test 32 (17-11-2026, core 0.7.9): game files of the custom models rebuilt from the installation
// (src\FicherosJuego.cs). With test folders: copy and patch, rider in subfolder, second pass without writing,
// corrupted file that gets redone, updated game (the base changes: neither copy nor patch, base model), missing base, paths that
// escape the folder, broken declaration, patch that does not match and synchronization from BepInEx\plugins (what was
// rebuilt is not taken for a user file). With the real modules folder and the game installed: each
// declaration is rebuilt entirely from the game with the declared SHA-256, and each model has all its files
// (in the module or declared) and none with a game file name left undeclared.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using ZeroNucleo;

internal static class PruebasFicherosJuego
{
    // -----------------------------------------------------------------------
    // Sección: utilidades
    // Section: utilities
    // -----------------------------------------------------------------------
    private static string Sha(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();

    // Fichero «del juego» de prueba: cabecera TJBA/TJBK de 16 bytes + datos pseudoaleatorios (semilla fija)
    // Test "game" file: 16-byte TJBA/TJBK header + pseudorandom data (fixed seed)
    private static byte[] FicheroJuego(string magia, int huesos, int frames, int largo, int semilla)
    {
        var b = new byte[largo];
        new Random(semilla).NextBytes(b);
        for (int i = 0; i < 4; i++) b[i] = (byte)magia[i];
        BitConverter.GetBytes(1).CopyTo(b, 4);
        BitConverter.GetBytes(huesos).CopyTo(b, 8);
        BitConverter.GetBytes(frames).CopyTo(b, 12);
        return b;
    }

    // Parche ZPR1 (como ficheros_juego.hacer_parche): cabecera + derivado XOR original en el tramo común + resto
    // ZPR1 patch (like ficheros_juego.hacer_parche): header + XOR derivative of the original in the common section + remainder
    private static byte[] Parche(byte[] derivado, byte[] original)
    {
        var p = new byte[12 + derivado.Length];
        new byte[] { (byte)'Z', (byte)'P', (byte)'R', (byte)'1' }.CopyTo(p, 0);
        BitConverter.GetBytes(derivado.Length).CopyTo(p, 4);
        BitConverter.GetBytes(original.Length).CopyTo(p, 8);
        for (int i = 0; i < derivado.Length; i++) p[12 + i] = (byte)(derivado[i] ^ (i < original.Length ? original[i] : 0));
        return p;
    }

    private static object Entrada(string destino, string origen, byte[] original, byte[] resultado, string parche = null) => new
    {
        destino, origen, shaOrigen = Sha(original), shaDestino = Sha(resultado),
        cabecera = Convert.ToHexString(original, 0, 16).ToLowerInvariant(), parche
    };

    private static void Declarar(string carpeta, params object[] entradas)
    {
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(Path.Combine(carpeta, FicherosJuego.NombreDeclaracion), JsonSerializer.Serialize(new { formato = 1, ficheros = entradas }));
    }

    private static void Escribir(string ruta, byte[] datos)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllBytes(ruta, datos);
    }

    private static bool MismosBytes(string ruta, byte[] esperado) => File.Exists(ruta) && File.ReadAllBytes(ruta).SequenceEqual(esperado);

    // -----------------------------------------------------------------------
    // Sección: casos
    // Section: cases
    // -----------------------------------------------------------------------
    public static void Todas(Action<bool, string> comprobar, string carpetaModulos = null)
    {
        string raiz = Path.Combine(Path.GetTempPath(), "zero_pruebas_ficheros_juego_" + Guid.NewGuid().ToString("N"));
        try
        {
            string uv = Path.Combine(raiz, "juego", "UnitVisuals");
            string visual = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_900");
            // «Juego»: una base con anim.bin, anchors.bin, marcador y jinete
            // "Game": a base with anim.bin, anchors.bin, marker and rider
            byte[] anim = FicheroJuego("TJBA", 55, 3, 32 + 55 * 3 * 24, 1);      // largo que pide su cabecera
                                                                                 // length required by its header
            byte[] anclas = FicheroJuego("TJBK", 2, 5, 32 + 2 * 5 * 48, 2);
            byte[] marcador = FicheroJuego("TJBM", 3, 3, 166, 3);
            byte[] animJinete = FicheroJuego("TJBA", 50, 2, 32 + 50 * 2 * 24, 4);
            Escribir(Path.Combine(uv, "Base", "anim.bin"), anim);
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), anclas);
            Escribir(Path.Combine(uv, "Base", "attach_0.mesh.bin"), marcador);
            Escribir(Path.Combine(uv, "Base", "rider", "anim.bin"), animJinete);
            // Derivado de anim.bin (como el esqueleto a escala): la mitad de los bytes cambiados y 7 de más al final
            // Derivative of anim.bin (like the scaled skeleton): half the bytes changed and 7 extra at the end
            byte[] animDerivado = anim.Concat(new byte[] { 1, 2, 3, 4, 5, 6, 7 }).ToArray();
            for (int i = 16; i < animDerivado.Length; i += 2) animDerivado[i] ^= 0x5A;
            Escribir(Path.Combine(visual, "anim.bin.zpar"), Parche(animDerivado, anim));
            Declarar(visual,
                Entrada("anim.bin", "Base/anim.bin", anim, animDerivado, "anim.bin.zpar"),
                Entrada("anchors.bin", "Base/anchors.bin", anclas, anclas),
                Entrada("marcador_attach_0.mesh.bin", "Base/attach_0.mesh.bin", marcador, marcador),
                Entrada("rider/anim.bin", "Base/rider/anim.bin", animJinete, animJinete));

            // 1) Primera pasada: escribe los 4, el derivado byte a byte y el jinete en su subcarpeta
            // 1) First pass: writes the 4, the byte-by-byte derivative and the rider in its subfolder
            var r1 = FicherosJuego.Preparar(visual, uv);
            comprobar(r1.Ok && r1.Escritos == 4 && r1.Iguales == 0 && r1.Declarados == 4
                      && MismosBytes(Path.Combine(visual, "anim.bin"), animDerivado) && MismosBytes(Path.Combine(visual, "anchors.bin"), anclas)
                      && MismosBytes(Path.Combine(visual, "marcador_attach_0.mesh.bin"), marcador)
                      && MismosBytes(Path.Combine(visual, "rider", "anim.bin"), animJinete)
                      && !Directory.GetFiles(visual, "*.zero_tmp", SearchOption.AllDirectories).Any(),
                $"ficheros del juego: primera pasada reconstruye copia, parche y jinete byte a byte ({r1.Escritos} escritos, {r1.Motivo})");

            // 2) Segunda pasada: nada que escribir
            // 2) Second pass: nothing to write
            var r2 = FicherosJuego.Preparar(visual, uv);
            comprobar(r2.Ok && r2.Escritos == 0 && r2.Iguales == 4, "ficheros del juego: segunda pasada sin escribir nada (los 4 ya estaban bien)");

            // 3) Un fichero estropeado en la copia de Mods se rehace
            // 3) A corrupted file in the Mods copy is rebuilt
            File.WriteAllBytes(Path.Combine(visual, "anchors.bin"), new byte[] { 9, 9, 9 });
            var r3 = FicherosJuego.Preparar(visual, uv);
            comprobar(r3.Ok && r3.Escritos == 1 && MismosBytes(Path.Combine(visual, "anchors.bin"), anclas), "ficheros del juego: un fichero estropeado se vuelve a poner");

            // 4) PrepararUnaVez: la segunda vez no repite el trabajo
            // 4) PrepararUnaVez: the second time it doesn't repeat the work
            FicherosJuego.Olvidar();
            var u1 = FicherosJuego.PrepararUnaVez(visual, uv, out bool nueva1);
            var u2 = FicherosJuego.PrepararUnaVez(visual, uv, out bool nueva2);
            comprobar(nueva1 && !nueva2 && ReferenceEquals(u1, u2) && u1.Ok, "ficheros del juego: una vez por carpeta y sesión");
            FicherosJuego.Olvidar();

            // 5) Juego actualizado (la base cambia aunque sea un byte y con la misma cabecera): ni copia ni parche, falla sin
            //    escribir (revisión independiente, hallazgo 3: los slots y los pesos se hicieron contra el fichero viejo)
            // 5) Updated game (the base changes even by one byte and with the same header): neither copy nor patch, fails without
            //    writing (independent review, finding 3: the slots and the weights were made against the old file)
            byte[] anclasNuevas = (byte[])anclas.Clone();
            anclasNuevas[500] ^= 0xFF;
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), anclasNuevas);
            string soloCopia = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_901");
            Declarar(soloCopia, Entrada("anchors.bin", "Base/anchors.bin", anclas, anclas));
            var r5 = FicherosJuego.Preparar(soloCopia, uv);
            comprobar(!r5.Ok && r5.Motivo.Contains("Base/anchors.bin") && !File.Exists(Path.Combine(soloCopia, "anchors.bin")),
                $"ficheros del juego: copia de una base actualizada (misma cabecera) falla sin escribir ({r5.Motivo})");
            byte[] animNueva = (byte[])anim.Clone();
            animNueva[700] ^= 0xFF;
            Escribir(Path.Combine(uv, "Base", "anim.bin"), animNueva);
            byte[] antes = File.ReadAllBytes(Path.Combine(visual, "anim.bin"));
            // 0.7.11: si en la carpeta ya están los ficheros de cuando se hizo el módulo (los dejó un núcleo anterior), valen
            // tal cual (Previos), sin tocar nada; sin ellos, un parche sobre una base cambiada sigue fallando sin escribir
            // 0.7.11: if the files from when the module was made are already in the folder (left by an earlier core), they are used
            // as-is (Previos), without touching anything; without them, a patch on a changed base still fails without writing
            var r5b = FicherosJuego.Preparar(visual, uv);
            comprobar(r5b.Ok && r5b.Modo == FicherosJuego.Modo.Previos && r5b.Escritos == 0 && r5b.CarpetaFinal == visual
                      && MismosBytes(Path.Combine(visual, "anim.bin"), antes) && r5b.Avisos.Any(a => a.Contains("Base/anim.bin")),
                $"ficheros del juego: base cambiada pero con los ficheros de antes en la carpeta: se usan tal cual (Previos) ({r5b.Motivo})");
            string sinPrevios = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_908");
            Escribir(Path.Combine(sinPrevios, "anim.bin.zpar"), Parche(animDerivado, anim));
            Declarar(sinPrevios, Entrada("anim.bin", "Base/anim.bin", anim, animDerivado, "anim.bin.zpar"));
            var r5c = FicherosJuego.Preparar(sinPrevios, uv, Path.Combine(raiz, "adaptados"));
            comprobar(!r5c.Ok && r5c.Motivo.Contains("Base/anim.bin") && r5c.Motivo.Contains("parche") && !File.Exists(Path.Combine(sinPrevios, "anim.bin"))
                      && !Directory.Exists(Path.Combine(raiz, "adaptados")),
                $"ficheros del juego: un parche sobre una base cambiada y sin ficheros de antes falla sin escribir ni adaptar ({r5c.Motivo})");
            Escribir(Path.Combine(uv, "Base", "anim.bin"), anim);
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), anclas);
            // Cabecera distinta (otros huesos): ni la copia
            // Different header (other bones): not even the copy
            byte[] otraCabecera = (byte[])anclas.Clone();
            otraCabecera[8] = 3;
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), otraCabecera);
            comprobar(!FicherosJuego.Preparar(soloCopia, uv).Ok, "ficheros del juego: copia de una base con otra cabecera (otras anclas) falla");
            // Misma cabecera pero truncado: falla
            // Same header but truncated: fails
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), anclas.Take(16).ToArray());
            comprobar(!FicherosJuego.Preparar(soloCopia, uv).Ok, "ficheros del juego: copia de una base truncada con la misma cabecera falla");
            Escribir(Path.Combine(uv, "Base", "anchors.bin"), anclas);
            // Con la base de vuelta a la original, la copia sí se reconstruye
            // With the base restored to the original, the copy is rebuilt
            comprobar(FicherosJuego.Preparar(soloCopia, uv).Ok && MismosBytes(Path.Combine(soloCopia, "anchors.bin"), anclas),
                "ficheros del juego: con la base original otra vez, la copia se reconstruye");
            // Un marcador cambiado tampoco
            // A changed marker doesn't either
            string soloMarcador = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_906");
            Declarar(soloMarcador, Entrada("marcador_attach_0.mesh.bin", "Base/attach_0.mesh.bin", marcador, marcador));
            byte[] marcadorNuevo = (byte[])marcador.Clone();
            marcadorNuevo[100] ^= 1;
            Escribir(Path.Combine(uv, "Base", "attach_0.mesh.bin"), marcadorNuevo);
            comprobar(!FicherosJuego.Preparar(soloMarcador, uv).Ok, "ficheros del juego: un marcador de la base cambiado falla");
            Escribir(Path.Combine(uv, "Base", "attach_0.mesh.bin"), marcador);
            // Destino repetido sin distinguir mayúsculas (revisión independiente, hallazgo 4)
            // Duplicate destination ignoring case (external review, finding 4)
            string repetido = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_907");
            Declarar(repetido, Entrada("anchors.bin", "Base/anchors.bin", anclas, anclas), Entrada("ANCHORS.bin", "Base/anchors.bin", anclas, anclas));
            comprobar(!FicherosJuego.Preparar(repetido, uv).Ok && !File.Exists(Path.Combine(repetido, "anchors.bin")),
                "ficheros del juego: un destino repetido (anchors.bin / ANCHORS.bin) falla sin escribir");

            // 6) Falta la base o la carpeta del juego: falla con motivo, sin excepción
            // 6) The base or the game folder is missing: fails with a reason, no exception
            string sinBase = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_902");
            Declarar(sinBase, Entrada("anim.bin", "NoExiste/anim.bin", anim, anim));
            var r6 = FicherosJuego.Preparar(sinBase, uv);
            var r6b = FicherosJuego.Preparar(visual, Path.Combine(raiz, "no_hay_juego"));
            comprobar(!r6.Ok && r6.Motivo.Contains("NoExiste/anim.bin") && !File.Exists(Path.Combine(sinBase, "anim.bin")) && !r6b.Ok,
                "ficheros del juego: base que falta o juego sin encontrar: falla con motivo y no escribe nada");

            // 7) Rutas que se salen de su carpeta, absolutas o con «\»: se rechazan
            // 7) Paths that escape their folder, absolute or with "\": rejected
            foreach (var (destino, origen) in new[] { ("../fuera.bin", "Base/anim.bin"), ("anim.bin", "../../juego/x"), ("anim.bin", "C:/x/anim.bin"),
                                                         ("sub\\anim.bin", "Base/anim.bin"), ("/anim.bin", "Base/anim.bin"), ("./anim.bin", "Base/anim.bin") })
            {
                string malo = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_903");
                Declarar(malo, Entrada(destino, origen, anim, anim));
                comprobar(!FicherosJuego.Preparar(malo, uv).Ok && !File.Exists(Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "fuera.bin")),
                    $"ficheros del juego: ruta no válida rechazada (destino «{destino}», origen «{origen}»)");
            }

            // 8) Declaración rota, vacía o con un SHA mal escrito
            // 8) Broken or empty declaration, or with a misspelled SHA
            string rota = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_904");
            Directory.CreateDirectory(rota);
            File.WriteAllText(Path.Combine(rota, FicherosJuego.NombreDeclaracion), "{ esto no es json");
            bool okRota = FicherosJuego.Preparar(rota, uv).Ok;
            File.WriteAllText(Path.Combine(rota, FicherosJuego.NombreDeclaracion), "{\"formato\":1,\"ficheros\":[]}");
            bool okVacia = FicherosJuego.Preparar(rota, uv).Ok;
            File.WriteAllText(Path.Combine(rota, FicherosJuego.NombreDeclaracion),
                "{\"formato\":1,\"ficheros\":[{\"destino\":\"anim.bin\",\"origen\":\"Base/anim.bin\",\"shaOrigen\":\"xyz\",\"shaDestino\":\"xyz\"}]}");
            bool okSha = FicherosJuego.Preparar(rota, uv).Ok;
            comprobar(!okRota && !okVacia && !okSha, "ficheros del juego: declaración ilegible, vacía o con SHA-256 mal escrito: falla");
            comprobar(FicherosJuego.Preparar(Path.Combine(raiz, "sin_declaracion"), uv).Ok, "ficheros del juego: carpeta sin declaración: nada que hacer");

            // 9) Parche que no es para ese fichero (otro largo) o estropeado
            // 9) Patch that isn't for that file (different length) or corrupted
            string malParche = Path.Combine(raiz, "Mods", "ZeroPrueba", "unit_visuals", "_905");
            Escribir(Path.Combine(malParche, "anim.bin.zpar"), Parche(animDerivado, anclas));
            Declarar(malParche, Entrada("anim.bin", "Base/anim.bin", anim, animDerivado, "anim.bin.zpar"));
            bool okLargo = FicherosJuego.Preparar(malParche, uv).Ok;
            byte[] p = Parche(animDerivado, anim);
            p[100] ^= 1;
            Escribir(Path.Combine(malParche, "anim.bin.zpar"), p);
            bool okRoto = FicherosJuego.Preparar(malParche, uv).Ok;
            comprobar(!okLargo && !okRoto && !File.Exists(Path.Combine(malParche, "anim.bin")),
                "ficheros del juego: parche para otro fichero o con un byte cambiado: falla (SHA-256) y no escribe");
            comprobar(FicherosJuego.AplicarParche(new byte[5], anim, out _) == null, "ficheros del juego: algo que no es un parche ZPR1 se rechaza");

            // 10) Sincronización desde BepInEx\plugins: lo reconstruido no se avisa como fichero del usuario, y lo que copió
            //     el núcleo del paquete viejo (con los ficheros del juego dentro) se aparta al actualizar
            // 10) Sync from BepInEx\plugins: what was rebuilt is not flagged as a user file, and what the core copied
            //     from the old package (with the game files inside) is set aside on update
            string plugins = Path.Combine(raiz, "BepInEx", "plugins", "Zero-Prueba");
            string mods = Path.Combine(raiz, "Mods sinc");
            string fuente = Path.Combine(plugins, "ZeroPrueba");
            Directory.CreateDirectory(fuente);
            Directory.CreateDirectory(mods);
            File.WriteAllText(Path.Combine(fuente, "mod.json"), "{\"displayName\":\"ZeroPrueba\",\"author\":\"ZeroitMods\",\"version\":\"1.0.0\"}");
            File.WriteAllText(Path.Combine(fuente, DeclaracionModulo.NombreFichero), "{\"id\":\"ZeroPrueba\",\"version\":\"1.0.0\",\"nucleoMinimo\":\"0.7.0\"}");
            string fv = Path.Combine(fuente, "unit_visuals", "_900");
            Escribir(Path.Combine(fv, "anim.bin"), anim);                       // paquete viejo: el fichero del juego dentro
                                                                                // old package: the game file inside
            Escribir(Path.Combine(fv, "unit.json"), new byte[] { 123, 125 });
            SincronizarModulos.Sincronizar(Path.Combine(raiz, "BepInEx", "plugins"), mods);
            // Paquete nuevo: sin anim.bin, con declaración
            // New package: without anim.bin, with declaration
            File.Delete(Path.Combine(fv, "anim.bin"));
            Declarar(fv, Entrada("anim.bin", "Base/anim.bin", anim, anim));
            var s1 = SincronizarModulos.Sincronizar(Path.Combine(raiz, "BepInEx", "plugins"), mods);
            string copia = Path.Combine(mods, "ZeroPrueba", "unit_visuals", "_900");
            comprobar(!File.Exists(Path.Combine(copia, "anim.bin")) && s1.Obsoletos.Contains("ZeroPrueba/unit_visuals/_900/anim.bin"),
                $"ficheros del juego: al actualizar, el anim.bin que traía el paquete viejo se aparta como obsoleto [{s1}]");
            var rCopia = FicherosJuego.Preparar(copia, uv);
            File.WriteAllText(Path.Combine(fuente, "README.md"), "cambio");    // fuerza otra actualización
                                                                               // forces another update
            var s2 = SincronizarModulos.Sincronizar(Path.Combine(raiz, "BepInEx", "plugins"), mods);
            comprobar(rCopia.Ok && MismosBytes(Path.Combine(copia, "anim.bin"), anim) && s2.Actualizados.Contains("ZeroPrueba")
                      && !s2.Avisos.Any(a => a.Contains("anim.bin")) && !s2.Obsoletos.Any(),
                $"ficheros del juego: lo reconstruido por el núcleo se queda en Mods sin aviso en las actualizaciones [{s2}]");
            comprobar(FicherosJuego.DestinosDeclarados(fuente).SetEquals(new[] { "unit_visuals/_900/anim.bin" }),
                "ficheros del juego: DestinosDeclarados da las rutas relativas al módulo");

            // 12) 0.7.11: adaptación a la base actual (el juego volvió a hornear la base: mismos huesos, más fotogramas)
            // 12) 0.7.11: adaptation to the current base (the game re-baked the base: same bones, more frames)
            Adaptacion(comprobar, raiz);
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch { /* temporal: si queda, no pasa nada */ }
                                                          // temporary: if it remains, no harm done
        }

        // 11) Módulos reales contra el juego instalado
        // 11) Real modules against the installed game
        Reales(comprobar, carpetaModulos);
    }

    // -----------------------------------------------------------------------
    // Sección: 0.7.11, adaptación a la base actual del juego
    // Section: 0.7.11, adaptation to the current game base
    // -----------------------------------------------------------------------
    // unit.json mínimo: esqueleto, ritmo, slots (inicio y largo) y anclas
    // Minimal unit.json: skeleton, pace, slots (start and length) and anchors
    private static string UnitJson(string[] huesos, int ritmo, (int inicio, int largo)[] slots, string[] anclas) => JsonSerializer.Serialize(new
    {
        format = 1, kind = "unit", unitName = "x",
        skeleton = new { boneCount = huesos.Length, names = huesos },
        frameRate = ritmo,
        slots = slots.Select((s, i) => new { id = i, name = "s" + i, start = s.inicio, count = s.largo, loop = true }).ToArray(),
        anchors = anclas,
        materials = new object[0],
        variants = new[] { new { lods = new[] { new { meshes = new[] { new { file = "body_0.mesh.bin", material = 0 } } } } } },
    });

    private static void Adaptacion(Action<bool, string> comprobar, string raiz)
    {
        string uv = Path.Combine(raiz, "juego2", "UnitVisuals");
        string adaptados = Path.Combine(raiz, "adaptados2");
        string[] huesos = { "Hips", "Spine", "Head" };
        string[] anclas = { "RightHand", "LeftHand" };
        // Base vieja (la del módulo): 4 fotogramas; base nueva del juego: 6 (re-horneada), mismos huesos y anclas
        // Old base (the module's): 4 frames; new game base: 6 (re-baked), same bones and anchors
        byte[] animVieja = FicheroJuego("TJBA", 3, 4, 32 + 3 * 4 * 24, 11), anclasViejas = FicheroJuego("TJBK", 2, 4, 32 + 2 * 4 * 48, 12);
        byte[] animNueva = FicheroJuego("TJBA", 3, 6, 32 + 3 * 6 * 24, 13), anclasNuevas = FicheroJuego("TJBK", 2, 6, 32 + 2 * 6 * 48, 14);
        byte[] marcador = FicheroJuego("TJBM", 1, 1, 100, 15);
        Escribir(Path.Combine(uv, "Oni", "anim.bin"), animNueva);
        Escribir(Path.Combine(uv, "Oni", "anchors.bin"), anclasNuevas);
        Escribir(Path.Combine(uv, "Oni", "attach_0.mesh.bin"), marcador);
        File.WriteAllText(Path.Combine(uv, "Oni", "unit.json"), UnitJson(huesos, 30, new[] { (0, 4), (4, 2) }, anclas));

        // Módulo: unit.json con los slots viejos (hechos contra 4 fotogramas), malla y textura propias, y la declaración
        // Module: unit.json with the old slots (made against 4 frames), custom mesh and texture, and the declaration
        string visual = Path.Combine(raiz, "Mods2", "ZeroPrueba", "unit_visuals", "_944");
        Directory.CreateDirectory(visual);
        File.WriteAllText(Path.Combine(visual, "unit.json"), UnitJson(huesos, 25, new[] { (0, 3), (3, 1) }, anclas));
        Escribir(Path.Combine(visual, "body_0.mesh.bin"), new byte[] { 1, 2, 3 });
        Escribir(Path.Combine(visual, "cuerpo.png"), new byte[] { 4, 5, 6 });
        Declarar(visual, Entrada("anim.bin", "Oni/anim.bin", animVieja, animVieja), Entrada("anchors.bin", "Oni/anchors.bin", anclasViejas, anclasViejas),
                 Entrada("marcador_attach_0.mesh.bin", "Oni/attach_0.mesh.bin", marcador, marcador));

        // a) Sin carpeta de adaptados: falla como en la 0.7.10 (nada que usar)
        // a) Without an adapted folder: fails as in 0.7.10 (nothing to use)
        comprobar(!FicherosJuego.Preparar(visual, uv).Ok, "adaptación: sin carpeta de adaptados, una base cambiada sigue cayendo al modelo base");

        // b) Con carpeta de adaptados: visual montado aparte con los ficheros ACTUALES y los slots de la base actual
        // b) With an adapted folder: visual assembled separately with the CURRENT files and the current base's slots
        var r = FicherosJuego.Preparar(visual, uv, adaptados);
        string final = r.CarpetaFinal ?? "";
        bool fuera = final.StartsWith(adaptados, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(final) == "ZeroPrueba_944";
        var json = File.Exists(Path.Combine(final, "unit.json")) ? JsonDocument.Parse(File.ReadAllText(Path.Combine(final, "unit.json"))).RootElement : default;
        bool slotsNuevos = json.ValueKind == JsonValueKind.Object && json.GetProperty("slots").GetArrayLength() == 2
                           && json.GetProperty("slots")[1].GetProperty("start").GetInt32() == 4 && json.GetProperty("slots")[1].GetProperty("count").GetInt32() == 2
                           && json.GetProperty("frameRate").GetInt32() == 30;
        comprobar(r.Ok && r.Modo == FicherosJuego.Modo.Adaptado && fuera && slotsNuevos
                  && MismosBytes(Path.Combine(final, "anim.bin"), animNueva) && MismosBytes(Path.Combine(final, "anchors.bin"), anclasNuevas)
                  && MismosBytes(Path.Combine(final, "marcador_attach_0.mesh.bin"), marcador)
                  && MismosBytes(Path.Combine(final, "body_0.mesh.bin"), new byte[] { 1, 2, 3 }) && MismosBytes(Path.Combine(final, "cuerpo.png"), new byte[] { 4, 5, 6 })
                  && !File.Exists(Path.Combine(final, FicherosJuego.NombreDeclaracion))
                  && !File.Exists(Path.Combine(visual, "anim.bin")) && File.ReadAllText(Path.Combine(visual, "unit.json")).Contains("\"count\":1"),
            $"adaptación: base re-horneada, visual montado aparte con los ficheros y slots actuales y el módulo sin tocar ({r.Motivo}; {final})");
        var r2 = FicherosJuego.Preparar(visual, uv, adaptados);
        comprobar(r2.Ok && r2.Modo == FicherosJuego.Modo.Adaptado && r2.Escritos == 0, "adaptación: la segunda vez no reescribe nada");

        // c) Casos en los que NO se adapta (más seguro el modelo base)
        // c1) otro esqueleto en la base
        // c) Cases where adaptation is NOT done (base model is safer)
        // c1) different skeleton in the base
        File.WriteAllText(Path.Combine(uv, "Oni", "unit.json"), UnitJson(new[] { "Hips", "Spine", "Cabeza" }, 30, new[] { (0, 4), (4, 2) }, anclas));
        var c1 = FicherosJuego.Preparar(visual, uv, adaptados);
        // c2) slots que no llegan a los fotogramas del anim.bin
        // c2) slots that do not reach the anim.bin frames
        File.WriteAllText(Path.Combine(uv, "Oni", "unit.json"), UnitJson(huesos, 30, new[] { (0, 4), (4, 1) }, anclas));
        var c2 = FicherosJuego.Preparar(visual, uv, adaptados);
        // c3) anchors.bin con otro número de anclas que su unit.json
        // c3) anchors.bin with a different anchor count than its unit.json
        File.WriteAllText(Path.Combine(uv, "Oni", "unit.json"), UnitJson(huesos, 30, new[] { (0, 4), (4, 2) }, new[] { "RightHand" }));
        var c3 = FicherosJuego.Preparar(visual, uv, adaptados);
        // c4) anim.bin truncado (la cabecera pide más bytes)
        // c4) truncated anim.bin (header requests more bytes)
        File.WriteAllText(Path.Combine(uv, "Oni", "unit.json"), UnitJson(huesos, 30, new[] { (0, 4), (4, 2) }, anclas));
        Escribir(Path.Combine(uv, "Oni", "anim.bin"), animNueva.Take(animNueva.Length - 1).ToArray());
        var c4 = FicherosJuego.Preparar(visual, uv, adaptados);
        Escribir(Path.Combine(uv, "Oni", "anim.bin"), animNueva);
        // c5) anclas propias sin declarar (como la 952): calculadas para los fotogramas viejos
        // c5) custom anchors without declaration (like the 952): calculated for the old frames
        string propias = Path.Combine(raiz, "Mods2", "ZeroPrueba", "unit_visuals", "_952");
        Directory.CreateDirectory(propias);
        File.WriteAllText(Path.Combine(propias, "unit.json"), UnitJson(huesos, 25, new[] { (0, 3), (3, 1) }, anclas));
        Escribir(Path.Combine(propias, "anchors.bin"), anclasViejas);
        Declarar(propias, Entrada("anim.bin", "Oni/anim.bin", animVieja, animVieja));
        var c5 = FicherosJuego.Preparar(propias, uv, adaptados);
        comprobar(!c1.Ok && c1.Motivo.Contains("esqueleto") && !c2.Ok && c2.Motivo.Contains("fotogramas") && !c3.Ok && c3.Motivo.Contains("anclas")
                  && !c4.Ok && c4.Motivo.Contains("formato") && !c5.Ok && c5.Motivo.Contains("anclas propias"),
            $"adaptación: no se adapta con otro esqueleto, slots o anclas que no cuadran, anim.bin truncado ni anclas propias " +
            $"({c1.Motivo} | {c2.Motivo} | {c3.Motivo} | {c4.Motivo} | {c5.Motivo})");
        // e) Caja de las mallas: agrandada por eje en max(mitad del tamaño, 1); lo que no es TJBM o tiene caja rota, igual
        // e) Mesh bounding box: enlarged per axis by max(half the size, 1); anything that is not TJBM or has a broken box, unchanged
        var malla = new byte[64];
        System.Text.Encoding.ASCII.GetBytes("TJBM").CopyTo(malla, 0);
        float[] caja = { -1f, 0f, -0.5f, 1f, 4f, 0.5f };
        for (int i = 0; i < 6; i++) BitConverter.GetBytes(caja[i]).CopyTo(malla, 20 + 4 * i);
        byte[] grande = FicherosJuego.AgrandarCaja(malla);
        float G(int i) => BitConverter.ToSingle(grande, 20 + 4 * i);
        var rota = (byte[])malla.Clone();
        BitConverter.GetBytes(float.NaN).CopyTo(rota, 20);
        comprobar(G(0) == -2f && G(3) == 2f && G(1) == -2f && G(4) == 6f && G(2) == -1.5f && G(5) == 1.5f
                  && malla.Skip(44).SequenceEqual(grande.Skip(44)) && BitConverter.ToSingle(malla, 20) == -1f
                  && FicherosJuego.AgrandarCaja(new byte[] { 1, 2, 3 }).Length == 3 && FicherosJuego.AgrandarCaja(rota) == rota,
            "adaptación: la caja de la malla se agranda con margen (el resto de la malla y el original no cambian)");
        // d) La cabecera: lectura correcta y rechazo de otra magia o versión
        // d) The header: correct reading and rejection of a different magic value or version
        comprobar(FicherosJuego.LeerCabecera(animNueva, "TJBA", 2, out int h, out int f) && h == 3 && f == 6
                  && !FicherosJuego.LeerCabecera(anclasNuevas, "TJBA", 2, out _, out _) && !FicherosJuego.LeerCabecera(new byte[10], "TJBA", 2, out _, out _),
            "adaptación: cabecera TJBA/TJBK leída (huesos y fotogramas) y rechazada si no es la esperada");
    }

    // Carpeta StreamingAssets\UnitVisuals del juego instalado (TABLETOP_TAVERN_JUEGO o la ruta conocida), o null
    // StreamingAssets\UnitVisuals folder of the installed game (TABLETOP_TAVERN_JUEGO or the known path), or null
    private static string UnitVisualsJuego()
    {
        foreach (string j in new[] { Environment.GetEnvironmentVariable("TABLETOP_TAVERN_JUEGO"), @"C:\Program Files (x86)\Steam\steamapps\common\TabletopTavern" })
        {
            if (string.IsNullOrEmpty(j)) continue;
            string uv = Path.Combine(j, "TabletopTavern_Data", "StreamingAssets", "UnitVisuals");
            if (Directory.Exists(uv)) return uv;
        }
        return null;
    }

    private static void Reales(Action<bool, string> comprobar, string carpetaModulos)
    {
        string uv = UnitVisualsJuego();
        if (carpetaModulos == null || !Directory.Exists(carpetaModulos) || uv == null)
        {
            Console.WriteLine("(prueba 32 con los módulos reales omitida: falta la carpeta de módulos o el juego instalado)");
            return;
        }
        var nombresJuego = new System.Text.RegularExpressions.Regex(@"^(anim\.bin|anchors\.bin|marcador_.*\.mesh\.bin)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        int carpetas = 0, ficheros = 0, malas = 0, incompletos = 0, sinDeclarar = 0;
        var detalle = new List<string>();
        string tmp = Path.Combine(Path.GetTempPath(), "zero_pruebas_fj_reales_" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string visual in Directory.GetDirectories(carpetaModulos).SelectMany(m =>
                         Directory.Exists(Path.Combine(m, "unit_visuals")) ? Directory.GetDirectories(Path.Combine(m, "unit_visuals"), "_*") : new string[0]))
            {
                string nombre = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(visual))) + "/" + Path.GetFileName(visual);
                var declarados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (FicherosJuego.TieneDeclaracion(visual))
                {
                    // Copia de la declaración y los parches a una carpeta temporal (la copia maestra no se toca)
                    // Copy of the declaration and the patches to a temporary folder (the master copy is not touched)
                    carpetas++;
                    string t = Path.Combine(tmp, nombre.Replace('/', '_'));
                    Directory.CreateDirectory(t);
                    File.Copy(Path.Combine(visual, FicherosJuego.NombreDeclaracion), Path.Combine(t, FicherosJuego.NombreDeclaracion));
                    foreach (string zp in Directory.GetFiles(visual, "*.zpar", SearchOption.AllDirectories))
                        Escribir(Path.Combine(t, Path.GetRelativePath(visual, zp)), File.ReadAllBytes(zp));
                    var r = FicherosJuego.Preparar(t, uv);
                    ficheros += r.Escritos;
                    if (!r.Ok || r.Escritos != r.Declarados || r.Avisos.Count > 0) { malas++; detalle.Add($"{nombre}: {r.Motivo}"); }
                    using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(visual, FicherosJuego.NombreDeclaracion)));
                    foreach (var e in doc.RootElement.GetProperty("ficheros").EnumerateArray()) declarados.Add(e.GetProperty("destino").GetString());
                }
                // Cada fichero que pide el unit.json (anim.bin, anchors.bin si hay anclas, mallas, jinete) está o se declara
                // Each file that unit.json requires (anim.bin, anchors.bin if there are anchors, meshes, rider) is present or declared
                string uj = Path.Combine(visual, "unit.json");
                if (File.Exists(uj))
                {
                    var faltan = new List<string>();
                    void Pide(string rel) { if (!File.Exists(Path.Combine(visual, rel.Replace('/', Path.DirectorySeparatorChar))) && !declarados.Contains(rel)) faltan.Add(rel); }
                    void Revisar(JsonElement j, string pre)
                    {
                        Pide(pre + "anim.bin");
                        if (j.TryGetProperty("anchors", out var an) && an.GetArrayLength() > 0) Pide(pre + "anchors.bin");
                        foreach (var v in j.GetProperty("variants").EnumerateArray())
                        {
                            foreach (var lod in v.GetProperty("lods").EnumerateArray())
                                foreach (var me in lod.GetProperty("meshes").EnumerateArray()) Pide(pre + me.GetProperty("file").GetString());
                            if (v.TryGetProperty("attachments", out var at))
                                foreach (var a in at.EnumerateArray()) Pide(pre + a.GetProperty("file").GetString());
                        }
                    }
                    using var docU = JsonDocument.Parse(File.ReadAllText(uj));
                    Revisar(docU.RootElement, "");
                    if (docU.RootElement.TryGetProperty("rider", out var rider) && !string.IsNullOrEmpty(rider.GetString()))
                    {
                        string rj = Path.Combine(visual, rider.GetString(), "unit.json");
                        if (File.Exists(rj)) { using var docR = JsonDocument.Parse(File.ReadAllText(rj)); Revisar(docR.RootElement, rider.GetString().TrimEnd('/', '\\') + "/"); }
                        else faltan.Add(rider.GetString() + "/unit.json");
                    }
                    if (faltan.Count > 0) { incompletos++; detalle.Add($"{nombre}: faltan {string.Join(", ", faltan)}"); }
                }
                // Ningún fichero con nombre de fichero del juego en la carpeta (salvo la excepción revisada de la 952)
                // No file with a game file name in the folder (except the reviewed exception for 952)
                foreach (string f in Directory.GetFiles(visual, "*", SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(visual, f).Replace('\\', '/');
                    if (nombresJuego.IsMatch(Path.GetFileName(f)) && !(nombre.EndsWith("/_952") && rel == "anchors.bin")) { sinDeclarar++; detalle.Add($"{nombre}/{rel}: nombre de fichero del juego"); }
                }
            }
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { /* temporal */ }
                                                         // temporary
        }
        comprobar(carpetas > 0 && malas == 0, $"ficheros del juego (módulos reales): {carpetas} declaraciones reconstruidas desde el juego con su SHA-256 ({ficheros} ficheros) {string.Join("; ", detalle.Take(5))}");
        comprobar(incompletos == 0, $"ficheros del juego (módulos reales): cada modelo tiene todos sus ficheros, en el módulo o declarados {string.Join("; ", detalle.Take(5))}");
        comprobar(sinDeclarar == 0, $"ficheros del juego (módulos reales): ningún anim.bin, anchors.bin ni marcador en los módulos (salvo las anclas calculadas de la 952) {string.Join("; ", detalle.Take(5))}");
    }
}
