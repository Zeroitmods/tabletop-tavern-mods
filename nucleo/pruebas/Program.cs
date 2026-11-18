// Program.cs
// Pruebas del saneado de saves: inyecta unidades nuevas (900, 907, 941, 944) en saves reales copiados, comprueba
// que la ida al disco no deja ninguna y que la vuelta restaura exactamente el texto original (por GUID, también en el
// ejército enemigo). También: registro antiguo (versión 1) que se convierte solo, manifiesto de la fábrica y, en la
// prueba 0, el lector de WAV de la F3. Desde la 0.5.0 (F2), prueba 7: razas nuevas 40/41 en el save (ciudad, campo de
// batalla y temas de taberna) y manifiesto con las facciones nuevas activas.
// Desde la 0.6.0, prueba 8: objetos nuevos 700-727 (saneado en saves reales con choque de respaldo, instantánea, listas de
// vendidos, rotos, colección e historial; sorteo; JSON de reglas; manifiesto con los 22 objetos).
// Desde la 0.7.0 (núcleo con módulos), prueba 9: módulos (declaraciones, escuadras «manda la mayor», dependencias,
// versión del núcleo, réplica del orden de mods del juego, módulos desactivados por defecto en modlist.json,
// sincronización desde BepInEx\plugins con copias, actualizaciones, instalaciones a mano y retiradas, y conservación en el
// registro de los objetos de módulos desactivados) y, con la carpeta maestra de módulos, las combinaciones reales.
// Pruebas 10-14 en sus ficheros (PruebasRevision, PruebasInterfaz, PruebasHeroes, PruebasCampana, PruebasLogros) y, desde la
// revisión de la 0.7.2, prueba 15: doble pasada del saneado y victorias de héroes nuevos (PruebasDoblePasada.cs).
// Prueba 18 (18-10-2026): arreglos de la revisión de código del 18-10 (PruebasArreglos18.cs).
// Prueba 19 (18-10-2026): Tesorería imperial con el x2 solo sobre los primeros 5 de interés (PruebasInteres.cs).
// Prueba 34 (18-11-2026, núcleo 0.7.13): migración de Batalla.JineteCaballeria a encendida (PruebasJinete.cs).
// Prueba 20 (18-10-2026, noche): arreglos de la segunda revisión (registro bloqueado, campos nuevos, iconos 2:3; PruebasRonda2.cs).
// Prueba 21 (18-10-2026, noche): revisión externa (informes de fallo sin módulos, aviso de versión; PruebasRevisionExterna.cs).
// Prueba 24 (01-11-2026, juego 1.11.1): carga tolerante a tipos y métodos que el juego quita o mueve (fases de Load() y tipos
// por nombre; PruebasCarga.cs).
// Prueba 25 (01-11-2026, juego 1.11.1): arreglos de la revisión de datos (PruebasDatos1111.cs).
// Prueba 26 (02-11-2026, núcleo 0.7.4): hechizos propios, saneado de sus ids y sorteo por rareza (PruebasHechizos.cs).
// Prueba 28 (03-11-2026, crash al arrancar): ningún ref/out de struct no blittable del juego por el envoltorio del interop
// (escaneo de la DLL compilada; PruebasInterop.cs).
// Prueba 29 (03-11-2026, núcleo 0.7.6): moral del jugador del módulo «Impertérritos» (PruebasMoral.cs).
// Prueba 32 (17-11-2026, núcleo 0.7.9): ficheros del juego reconstruidos desde la instalación (PruebasFicherosJuego.cs).
// Uso: dotnet run -- <carpeta con campaignSaveData.json y playerSaveData.json> [carpeta de módulos (modulos\) o ruta de un facciones_manifiesto.json]
// Program.cs
// Sanitizing tests for saves: injects new units (900, 907, 941, 944) into copied real saves, checks
// that the trip to disk leaves none behind and that the return trip restores the original text exactly (by GUID, also in the
// enemy army). Also: old registry (version 1) that converts itself, factory manifest and, in
// test 0, the F3 WAV reader. Since 0.5.0 (F2), test 7: new races 40/41 in the save (city, battlefield
// and tavern themes) and manifest with the new factions active.
// Since 0.6.0, test 8: new gear 700-727 (sanitizing in real saves with backup collision, snapshot, lists of
// sold, broken, collection and history; draw; rules JSON; manifest with the 22 items).
// Since 0.7.0 (core with modules), test 9: modules (declarations, squads "the largest rules", dependencies,
// core version, replica of the game's mod order, modules disabled by default in modlist.json,
// sync from BepInEx\plugins with copies, updates, manual installs and removals, and preservation in the
// registry of the items of disabled modules) and, with the master modules folder, the real combinations.
// Tests 10-14 in their own files (PruebasRevision, PruebasInterfaz, PruebasHeroes, PruebasCampana, PruebasLogros) and, since the
// review of 0.7.2, test 15: double pass of the sanitizing and wins of new heroes (PruebasDoblePasada.cs).
// Test 18 (18-10-2026): fixes from the code review of 10-18 (PruebasArreglos18.cs).
// Test 19 (18-10-2026): Imperial Treasury with the x2 only on the first 5 of interest (PruebasInteres.cs).
// Test 34 (18-11-2026, core 0.7.13): migration of Batalla.JineteCaballeria to on (PruebasJinete.cs).
// Test 20 (18-10-2026, night): fixes from the second review (locked registry, new fields, 2:3 icons; PruebasRonda2.cs).
// Test 21 (18-10-2026, night): external review (crash reports without modules, version notice; PruebasRevisionExterna.cs).
// Test 24 (01-11-2026, game 1.11.1): loading tolerant of types and methods that the game removes or moves (phases of Load() and types
// by name; PruebasCarga.cs).
// Test 25 (01-11-2026, game 1.11.1): fixes from the data review (PruebasDatos1111.cs).
// Test 26 (02-11-2026, core 0.7.4): custom spells, sanitizing of their ids and draw by rarity (PruebasHechizos.cs).
// Test 28 (03-11-2026, crash on startup): no ref/out of a non-blittable game struct through the interop wrapper
// (scan of the compiled DLL; PruebasInterop.cs).
// Test 29 (03-11-2026, core 0.7.6): player morale of the module "Impertérritos" (PruebasMoral.cs).
// Test 32 (17-11-2026, core 0.7.9): game files rebuilt from the installation (PruebasFicherosJuego.cs).
// Usage: dotnet run -- <carpeta con campaignSaveData.json y playerSaveData.json> [modules folder (modulos\) or path to a facciones_manifiesto.json]

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroNucleo;

internal static class Program
{
    private static int fallos = 0;

    // Versión del núcleo para resolver los módulos reales (revisión del 18-10): se lee de la constante VERSION de
    // ..\src\Plugin.cs (Plugin.cs no se compila aquí porque usa tipos del juego), así las pruebas siguen al núcleo
    // cuando sube el «nucleoMinimo» de los módulos y no se quedan con un número escrito a mano
    // Core version to resolve the real modules (review of 18-10): read from the VERSION constant in
    // ..\src\Plugin.cs (Plugin.cs is not compiled here because it uses game types), so the tests follow the core
    // when the modules' "nucleoMinimo" goes up and don't get stuck with a hand-written number
    private static string _versionNucleo;
    internal static string VersionNucleo
    {
        get
        {
            if (_versionNucleo != null) return _versionNucleo;
            // Ronda 2 (B-10): se sube carpeta a carpeta desde la de salida hasta dar con src\Plugin.cs, así también funciona
            // compilando con -o o publish (antes solo se subían cuatro carpetas fijas)
            // Round 2 (B-10): walks up folder by folder from the output folder until it finds src\Plugin.cs, so it also works
            // when compiling with -o or publish (before it only went up four fixed folders)
            string plugin = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Plugin.cs");
            for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            {
                string candidato = Path.Combine(d.FullName, "src", "Plugin.cs");
                if (File.Exists(candidato)) { plugin = candidato; break; }
            }
            var m = File.Exists(plugin) ? Regex.Match(File.ReadAllText(plugin), @"const\s+string\s+VERSION\s*=\s*""([^""]+)""") : Match.Empty;
            if (!m.Success) throw new InvalidOperationException("No se pudo leer la versión del núcleo de " + Path.GetFullPath(plugin));
            return _versionNucleo = m.Groups[1].Value;
        }
    }

    // Manifiesto para las pruebas: si se pasa una carpeta de módulos, la fusión de todos (como si todos estuvieran activos)
    // Manifest for the tests: if a modules folder is passed, the merge of all of them (as if all were active)
    private static Manifiesto ManifiestoDe(string ruta, out string motivo)
    {
        motivo = null;
        if (Directory.Exists(ruta))
        {
            var mods = Modulos.Buscar(ruta, Directory.GetDirectories(ruta).Select(Path.GetFileName).ToList());
            var r = Modulos.Resolver(mods, VersionNucleo);
            if (r.Rechazados.Count > 0) motivo = string.Join("; ", r.Rechazados);
            return r.Manifiesto;
        }
        return Manifiesto.Leer(ruta, out motivo);
    }

    // Comprobación simple con mensaje
    // Simple check with message
    private static void Comprobar(bool ok, string que)
    {
        Console.WriteLine((ok ? "OK    " : "FALLO ") + que);
        if (!ok) fallos++;
    }

    // Prueba del lector de WAV: ficheros reales (44,1 kHz mono) y casos sintéticos (estéreo con bloque LIST, 8 bits)
    // WAV reader test: real files (44.1 kHz mono) and synthetic cases (stereo with LIST block, 8-bit)
    private static void PruebaWav(string carpetaAudio)
    {
        string[] wavs = Directory.Exists(carpetaAudio) ? Directory.GetFiles(carpetaAudio, "laser_*.wav") : new string[0];
        Comprobar(wavs.Length >= 5, $"hay al menos 5 WAV laser_* en {Path.GetFullPath(carpetaAudio)} ({wavs.Length})");
        foreach (string w in wavs)
        {
            byte[] b = File.ReadAllBytes(w);
            float[] m = LectorWav.LeerPcm16(b, out int fr);
            float pico = 0; foreach (float v in m) pico = Math.Max(pico, Math.Abs(v));
            Comprobar(fr == 44100 && m.Length == (b.Length - 44) / 2 && pico > 0.05f && pico <= 1f,
                      $"{Path.GetFileName(w)}: {fr} Hz, {m.Length} muestras, pico {pico:0.000}");
        }
        // Estéreo con un bloque LIST de tamaño impar delante de data: debe saltarlo y mezclar a mono
        // Stereo with an odd-sized LIST block before data: it must skip it and mix down to mono
        byte[] bien = WavSintetico(8, new short[] { 16384, 0, -32768, -32768 }, null);
        float[] e = LectorWav.LeerPcm16(bien, out int fe);
        Comprobar(fe == 48000 && e.Length == 2 && Math.Abs(e[0] - 0.25f) < 1e-4 && Math.Abs(e[1] + 1f) < 1e-4, "estéreo con LIST impar: salta el bloque y mezcla a mono");
        // 8 bits: se rechaza con mensaje
        // 8-bit: rejected with a message
        byte[] ocho = (byte[])bien.Clone(); ocho[34] = 8;   // bits por muestra
                                                            // bits per sample
        Comprobar(Rechaza(ocho, "PCM 16"), "un WAV de 8 bits se rechaza (InvalidDataException)");
        // Truncado: el fichero se corta a mitad del bloque data (la cabecera RIFF declara más bytes de los que hay)
        // Truncated: the file is cut off in the middle of the data block (the RIFF header declares more bytes than there are)
        byte[] cortado = new byte[bien.Length - 4]; Array.Copy(bien, cortado, cortado.Length);
        Comprobar(Rechaza(cortado, "truncado"), "WAV truncado (RIFF mayor que el fichero) se rechaza");
        // RIFF inconsistente: la cabecera RIFF cabe en el fichero, pero el bloque data declara más de lo que queda
        // Inconsistent RIFF: the RIFF header fits in the file, but the data block declares more than what is left
        byte[] dataLarga = WavSintetico(16, new short[] { 1, 2, 3, 4 }, null);
        Comprobar(Rechaza(dataLarga, "truncado"), "bloque data que declara más bytes que el RIFF se rechaza");
        // RIFF inconsistente: tamaño RIFF menor que los bloques (data queda fuera del límite RIFF)
        // Inconsistent RIFF: RIFF size smaller than the blocks (data falls outside the RIFF limit)
        byte[] riffCorto = WavSintetico(8, new short[] { 1, 2, 3, 4 }, 30);
        Comprobar(Rechaza(riffCorto, "truncado"), "tamaño RIFF menor que sus bloques se rechaza");
        // Data desalineada: estéreo con 6 bytes (no múltiplo de 4)
        // Misaligned data: stereo with 6 bytes (not a multiple of 4)
        byte[] desalineado = WavSintetico(6, new short[] { 1, 2, 3 }, null);
        Comprobar(Rechaza(desalineado, "múltiplo"), "bloque data no múltiplo de 2 × canales se rechaza");
        // Bytes de sobra después del RIFF (relleno de algunos editores): se ignoran
        // Extra bytes after the RIFF (padding from some editors): ignored
        byte[] conCola = new byte[bien.Length + 6]; Array.Copy(bien, conCola, bien.Length);
        Comprobar(LectorWav.LeerPcm16(conCola, out _).Length == 2, "bytes de sobra tras el RIFF se ignoran");
        bool basura = false; try { LectorWav.LeerPcm16(new byte[100], out _); } catch (InvalidDataException) { basura = true; }
        Comprobar(basura, "un fichero que no es WAV se rechaza");
    }

    // WAV estéreo 48 kHz con un bloque LIST impar; 'tamData' es lo que se declara en data (puede no cuadrar con las
    // muestras escritas) y 'riff' fuerza el tamaño RIFF (null = el correcto, longitud total - 8)
    // 48 kHz stereo WAV with an odd LIST block; 'tamData' is what is declared in data (may not match the
    // written samples) and 'riff' forces the RIFF size (null = the correct one, total length - 8)
    private static byte[] WavSintetico(int tamData, short[] muestras, int? riff)
    {
        var ms = new MemoryStream(); var bw = new BinaryWriter(ms);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); bw.Write(0); bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); bw.Write(16); bw.Write((short)1); bw.Write((short)2); bw.Write(48000); bw.Write(48000 * 4); bw.Write((short)4); bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("LIST")); bw.Write(3); bw.Write(new byte[] { 1, 2, 3, 0 });
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data")); bw.Write(tamData); foreach (short m in muestras) bw.Write(m);
        bw.Flush();
        byte[] b = ms.ToArray();
        BitConverter.GetBytes(riff ?? b.Length - 8).CopyTo(b, 4);
        return b;
    }

    // true si el lector rechaza el WAV con InvalidDataException (y, si se pide, con ese texto en el mensaje)
    // true if the reader rejects the WAV with InvalidDataException (and, if requested, with that text in the message)
    private static bool Rechaza(byte[] wav, string texto)
    {
        try { LectorWav.LeerPcm16(wav, out _); return false; }
        catch (InvalidDataException e) { Console.WriteLine("      motivo: " + e.Message); return texto == null || e.Message.Contains(texto); }
    }

    // 0.7.5: ranura del registro para un texto (en los saves de campaña lleva la partida y el slot: SaneadoSaves.ContextoSave)
    // 0.7.5: registry slot for a text (in campaign saves it carries the run and the slot: SaneadoSaves.ContextoSave)
    internal static string R(string texto, string baseRanura) => ContextoSave.De(texto).Ranura(baseRanura);

    // Prueba de las razas nuevas en el save (F2): ida al disco con la raza sustituta, vuelta con la real, firma que no
    // cuadra (otra ciudad), instantánea aparte y temas de taberna
    // Test of the new races in the save (F2): write to disk with the substitute race, return with the real one, signature that
    // doesn't match (another city), separate snapshot and tavern themes
    private static void PruebaRazas(string campana, string jugador)
    {
        var razas = new TablaRazas(new Dictionary<int, int> { { 40, 0 }, { 41, 0 } });   // sustituta: Iron Legion (0)
                                                                                         // substitute: Iron Legion (0)
        var reg = new RegistroGuardias(null);
        // Ciudad de la facción A (40) con nombre y tamaño, y campo de batalla del Imperio (41) en el save real
        // City of faction A (40) with name and size, and Empire battlefield (41) in the real save
        string conRazas = Regex.Replace(campana, @"(""townName"": )""[^""]*""(,\s*""townSize"": )\d+(,\s*""townRace"": )\d+", "${1}\"Cadia\"${2}2${3}40");
        conRazas = Regex.Replace(conRazas, @"(""battleFieldPreset"": \{\s*""mapRegion"": \{[^{}]*\},\s*""race"": )\d+", "${1}41");
        Comprobar(SaneadoSaves.ContarResiduosRazas(conRazas, razas) == 2, "save con una ciudad 40 y un campo de batalla 41 (2 razas nuevas detectadas)");
        string disco = SaneadoSaves.RazasParaDisco(conRazas, razas, reg);
        Comprobar(SaneadoSaves.ContarResiduosRazas(disco, razas) == 0 && Regex.IsMatch(disco, @"""townRace"": 0") && Regex.IsMatch(disco, @"""mapRegion"": \{[^{}]*\},\s*""race"": 0"),
            "ida al disco: ciudad y campo con la raza sustituta (Iron Legion), sin 40/41");
        Comprobar(reg.RazaDe(R(conRazas, "ciudad"), out int rc, out string fc) && rc == 40 && fc == "Cadia|2" && reg.RazaDe(R(conRazas, "campo"), out int rk, out _) && rk == 41,
            "registro: ciudad 40 con firma «Cadia|2» y campo 41 apuntados");
        Comprobar(SaneadoSaves.RazasDesdeDisco(disco, razas, reg) == conRazas, "vuelta: el texto vuelve exacto (ciudad 40 y campo 41)");
        Comprobar(SaneadoSaves.RazasDesdeDisco(disco, razas, new RegistroGuardias(null)) == disco, "sin registro el save queda con la raza sustituta (carga sin plugin)");
        // 0.6.1: razas apuntadas sin restaurar (se conservan y se describen para el log)
        // 0.6.1: noted races without restoring (they are kept and described for the log)
        Comprobar(SaneadoSaves.RazasPendientes(conRazas, razas, reg).Count == 0, "pendientes: tras restaurar no queda ninguna");
        var pendSinTabla = SaneadoSaves.RazasPendientes(disco, null, reg);
        Comprobar(pendSinTabla.Count == 2 && pendSinTabla[0].Contains("desactivadas") && reg.RazaDe(R(conRazas, "ciudad"), out _, out _),
            "pendientes: sin tabla de razas, ciudad y campo quedan pendientes («facciones nuevas desactivadas») y el registro las conserva");
        // Otra ciudad de Iron Legion de verdad (otro nombre): no se convierte en 40
        // Another genuine Iron Legion city (different name): it is not converted to 40
        string otra = disco.Replace("\"Cadia\"", "\"Altdorf\"");
        var pendFirma = SaneadoSaves.RazasPendientes(otra, razas, reg);
        Comprobar(pendFirma.Count == 2 && pendFirma[0].Contains("firma"), "pendientes: con otra ciudad, la de la ciudad se describe como «la firma no cuadra»");
        Comprobar(SaneadoSaves.RazasDesdeDisco(otra, razas, reg).Contains("\"townRace\": 0"), "una ciudad Iron Legion con otro nombre no se convierte en 40 (firma distinta)");
        // Un save con ciudad de serie olvida la ranura
        // A save with a stock city forgets the slot
        var reg2 = new RegistroGuardias(null);
        SaneadoSaves.RazasParaDisco(conRazas, razas, reg2);
        SaneadoSaves.RazasParaDisco(campana, razas, reg2);
        Comprobar(!reg2.RazaDe(R(campana, "ciudad"), out _, out _), "al guardar después una ciudad de serie se olvida la raza nueva apuntada");
        // Instantánea: ranura aparte
        // Snapshot: separate slot
        string instant = conRazas.Replace("\"snapShot\": false", "\"snapShot\": true");
        var reg3 = new RegistroGuardias(null);
        string dInst = SaneadoSaves.RazasParaDisco(instant, razas, reg3);
        Comprobar(reg3.RazaDe(R(instant, "ciudad"), out int ri, out _) && ri == 40 && !reg3.RazaDe(R(conRazas, "ciudad"), out _, out _)
                  && SaneadoSaves.RazasDesdeDisco(dInst, razas, reg3) == instant, "instantánea: su ciudad se apunta aparte y vuelve exacta");
        // Temas de taberna del save de jugador: se quitan 40/41 y el tema elegido 41 pasa a Special
        // Tavern themes in the player save: 40/41 are removed and the chosen theme 41 becomes Special
        string temas = Regex.Replace(jugador, @"""unlockedTavernThemes"": \[[^\]]*\]", "\"unlockedTavernThemes\": [\n        3,\n        40\n    ]");
        temas = Regex.Replace(temas, @"""activeTavernThemeRace"": \d+", "\"activeTavernThemeRace\": 41");
        string tDisco = SaneadoSaves.RazasParaDisco(temas, razas, new RegistroGuardias(null));
        Comprobar(SaneadoSaves.ContarResiduosRazas(temas, razas) == 2 && SaneadoSaves.ContarResiduosRazas(tDisco, razas) == 0
                  && Regex.IsMatch(tDisco, @"""unlockedTavernThemes"": \[\s*3\s*\]") && tDisco.Contains("\"activeTavernThemeRace\": 8"),
            "temas de taberna: fuera el 40 de los desbloqueados y el tema elegido 41 pasa a Special (tema por defecto)");
        Comprobar(SaneadoSaves.RazasParaDisco(jugador, razas, new RegistroGuardias(null)) == jugador && SaneadoSaves.RazasParaDisco(campana, razas, new RegistroGuardias(null)) == campana,
            "saves sin razas nuevas no se tocan");
        // Registro en disco: las razas apuntadas sobreviven a guardar y cargar
        // Registry on disk: the recorded races survive saving and loading
        string ruta = Path.Combine(Path.GetTempPath(), "zgi_registro_razas.json");
        if (File.Exists(ruta)) File.Delete(ruta);
        var r5 = new RegistroGuardias(ruta); r5.FijarRaza("ciudad", 41, "Mos Espa|1");
        var r6 = new RegistroGuardias(ruta); r6.Cargar();
        Comprobar(r6.RazaDe("ciudad", out int r6r, out string r6f) && r6r == 41 && r6f == "Mos Espa|1", "registro: la raza apuntada sobrevive a guardar y cargar");
        File.Delete(ruta);
    }

    // Prueba 8 (0.6.0): objetos nuevos. Ids de GearID del juego: 1 ArmingSwords, 4 ConscriptionOrders, 6 BallisticCharts,
    // 17 QuantitativeEasingPolicy, 20 ChugJug... (orden del enum en ECS/Components/GearData.cs:7-9)
    // Test 8 (0.6.0): new gear. Game GearID ids: 1 ArmingSwords, 4 ConscriptionOrders, 6 BallisticCharts,
    // 17 QuantitativeEasingPolicy, 20 ChugJug... (enum order in ECS/Components/GearData.cs:7-9)
    private static void PruebaObjetos(string campana, string jugador, string rutaManifiesto)
    {
        const int QEP = 17, ESPADAS = 1, LARGOS = 2, CONSCRIPCION = 4, CARTAS = 6, TOWER = 21;
        var tabla = new TablaObjetos(new Dictionary<int, int> { { 700, QEP }, { 715, CONSCRIPCION }, { 712, 7 }, { 704, TOWER } }, Enumerable.Range(1, 40));
        var reg = new RegistroGuardias(null);
        // Save real con objetos nuevos inyectados en Gear (el 700 choca con un QuantitativeEasingPolicy de verdad), vendidos y rotos
        // Real save with new gear injected into Gear (700 collides with a real QuantitativeEasingPolicy), sold and broken
        string conObjetos = Regex.Replace(campana, @"""Gear"": \[[^\]]*\]", "\"Gear\": [\n        " + QEP + ",\n        700,\n        715\n    ]");
        conObjetos = Regex.Replace(conObjetos, @"""SoldGear"": \[[^\]]*\]", "\"SoldGear\": [712]");
        conObjetos = Regex.Replace(conObjetos, @"""brokenGear"": \[[^\]]*\]", "\"brokenGear\": [715]");
        conObjetos = Regex.Replace(conObjetos, @"""recruitableGear"": \[[^\]]*\]", "\"recruitableGear\": [\n        " + CARTAS + ",\n        704,\n        700\n    ]");
        Comprobar(SaneadoObjetos.ContarResiduos(conObjetos, tabla) == 6, "save de campaña con 6 objetos nuevos (Gear, vendidos, rotos y ofertas)");
        string disco = SaneadoObjetos.ParaDisco(conObjetos, tabla, reg, true);
        Comprobar(SaneadoObjetos.ContarResiduos(disco, tabla) == 0, "tras la ida al disco no queda ningún objeto nuevo");
        var gear = reg.ObjetosPosicion(R(conObjetos, "Gear"));
        Comprobar(gear.Count == 2 && gear[1].real == 700 && gear[1].escrito != QEP && gear[2].real == 715 && gear[2].escrito == CONSCRIPCION,
            $"Gear: el 700 no repite el QuantitativeEasingPolicy que ya se lleva (escrito {gear[1].escrito}) y el 715 va como Conscription Orders");
        var ofertas = reg.ObjetosPosicion(R(conObjetos, "recruitableGear"));
        Comprobar(ofertas.Count == 2 && ofertas[1].escrito == TOWER && ofertas[2].escrito != QEP && ofertas[2].escrito != CARTAS,
            $"ofertas: 704 como Tower Shields y el 700 sin repetir un objeto ya ofrecido ni llevado (escrito {ofertas[2].escrito})");
        Comprobar(reg.ObjetosLista(R(conObjetos, "SoldGear")).SequenceEqual(new[] { 712 }) && reg.ObjetosLista(R(conObjetos, "brokenGear")).SequenceEqual(new[] { 715 }), "vendidos y rotos: se quitan y se apuntan");
        Comprobar(SaneadoObjetos.DesdeDisco(disco, tabla, reg) == conObjetos, "la vuelta restaura el texto exacto (Gear, vendidos, rotos y ofertas)");
        string dosVeces = SaneadoObjetos.DesdeDisco(SaneadoObjetos.DesdeDisco(disco, tabla, reg), tabla, reg);
        Comprobar(dosVeces == conObjetos, "restaurar dos veces no cambia nada (FromJson llama a FromJsonInternal)");
        Comprobar(SaneadoObjetos.DesdeDisco(disco, tabla, new RegistroGuardias(null)) == disco, "sin registro el save queda con objetos de serie (carga sin plugin)");
        Comprobar(SaneadoObjetos.ParaDisco(campana, tabla, new RegistroGuardias(null), true) == campana, "un save sin objetos nuevos no se toca");
        // Sin el plugin el jugador cambió el objeto escrito: la posición no se convierte en 700
        // Without the plugin the player changed the written item: the position is not converted into 700
        string cambiado = Regex.Replace(disco, @"""Gear"": \[[^\]]*\]", "\"Gear\": [" + QEP + ", " + LARGOS + ", " + CONSCRIPCION + "]");
        string vueltaCambiado = SaneadoObjetos.DesdeDisco(cambiado, tabla, reg);
        Comprobar(Regex.IsMatch(vueltaCambiado, @"""Gear"": \[" + QEP + @",\s*" + LARGOS + @",\s*715\]"),"si en la posición ya no está el objeto escrito, se respeta lo que haya");
        // Al guardar después sin objetos nuevos se olvida lo apuntado (objetos activos) o se conserva (objetos no activos)
        // When saving afterwards without new gear, the recorded data is forgotten (active gear) or kept (inactive gear)
        var reg2 = new RegistroGuardias(null);
        SaneadoObjetos.ParaDisco(conObjetos, tabla, reg2, true);
        SaneadoObjetos.ParaDisco(campana, tabla, reg2, false);
        bool conserva = reg2.ObjetosPosicion(R(campana, "Gear")).Count == 2;
        SaneadoObjetos.ParaDisco(campana, tabla, reg2, true);
        Comprobar(conserva && reg2.ObjetosPosicion(R(campana, "Gear")).Count == 0, "sin objetos activos el registro se conserva; con objetos activos y un save sin ellos se olvida");
        // Instantánea del nodo: su propia ranura
        // Node snapshot: its own slot
        string inst = conObjetos.Replace("\"snapShot\": false", "\"snapShot\": true");
        var reg3 = new RegistroGuardias(null);
        string discoInst = SaneadoObjetos.ParaDisco(inst, tabla, reg3, true);
        Comprobar(inst != conObjetos && reg3.ObjetosPosicion(R(inst, "Gear")).Count == 2 && reg3.ObjetosPosicion(R(conObjetos, "Gear")).Count == 0
                  && SaneadoObjetos.DesdeDisco(discoInst, tabla, reg3) == inst, "instantánea del nodo: ranura propia e ida y vuelta exacta");
        // Save del jugador: colección (se quita y se anota) e historial (respaldo sin recuperación)
        // Player save: collection (removed and recorded) and history (backup without recovery)
        string jConObjetos = Regex.Replace(jugador, @"""gearIdsCollected"": \[", "\"gearIdsCollected\": [\n        700,");
        jConObjetos = Regex.Replace(jConObjetos, @"""gear"": \[", "\"gear\": [\n            715,", RegexOptions.None, TimeSpan.FromSeconds(5));
        int enHistorial = Regex.Matches(jugador, @"""gear"": \[").Count;
        string jDisco = SaneadoObjetos.ParaDisco(jConObjetos, tabla, reg, true);
        Comprobar(enHistorial > 0 && SaneadoObjetos.ContarResiduos(jConObjetos, tabla) == 1 + enHistorial && SaneadoObjetos.ContarResiduos(jDisco, tabla) == 0,
            $"save del jugador: colección e historial ({enHistorial} partidas) sin objetos nuevos en disco");
        string jVuelta = SaneadoObjetos.DesdeDisco(jDisco, tabla, reg);
        Comprobar(Regex.IsMatch(jVuelta, @"""gearIdsCollected"": \[[^\]]*700") && reg.ObjetosLista("gearIdsCollected").Contains(700) && !jVuelta.Contains("715,"),
            "colección: el 700 vuelve al leer; historial: queda como su respaldo (solo es texto)");

        // 0.7.2: equipo inicial recordado del save del jugador (escalar lastStartingGearId; la armería ya ofrece los 7xx)
        // 0.7.2: starting gear remembered from the player save (scalar lastStartingGearId; the armory already offers the 7xx)
        Comprobar(Regex.IsMatch(jugador, @"""lastStartingGearId""\s*:\s*\d+"), "el save del jugador real tiene lastStartingGearId");
        string jIni = Regex.Replace(jugador, @"(""lastStartingGearId""\s*:\s*)\d+", "${1}715");
        var regIni = new RegistroGuardias(null);
        Comprobar(SaneadoObjetos.ContarResiduos(jIni, tabla) == SaneadoObjetos.ContarResiduos(jugador, tabla) + 1, "equipo inicial 715: cuenta como residuo antes de sanear");
        string jIniDisco = SaneadoObjetos.ParaDisco(jIni, tabla, regIni, true);
        var apIni = regIni.ObjetosPosicion("lastStartingGearId");
        Comprobar(Regex.IsMatch(jIniDisco, @"""lastStartingGearId""\s*:\s*0\b") && SaneadoObjetos.ContarResiduos(jIniDisco, tabla) == 0
                  && apIni.Count == 1 && apIni[0].real == 715 && apIni[0].escrito == 0,
            "equipo inicial 715: en disco va None (0), no su respaldo, y el real queda apuntado");
        Comprobar(SaneadoObjetos.DesdeDisco(jIniDisco, tabla, regIni) == SaneadoObjetos.DesdeDisco(jIni, tabla, regIni)
                  && Regex.IsMatch(SaneadoObjetos.DesdeDisco(jIniDisco, tabla, regIni), @"""lastStartingGearId""\s*:\s*715\b"),
            "equipo inicial: al leer vuelve el 715");
        Comprobar(SaneadoObjetos.DesdeDisco(jIniDisco, tabla, new RegistroGuardias(null)) == jIniDisco, "equipo inicial sin registro (sin plugin): se queda None");
        // Módulo del 715 apagado (no está en la tabla): no se restaura y lo apuntado se conserva mientras siga None
        // Module of 715 turned off (not in the table): it is not restored and what was noted is kept while it remains None
        var tablaSin715 = new TablaObjetos(new Dictionary<int, int> { { 700, QEP } }, Enumerable.Range(1, 40));
        Comprobar(SaneadoObjetos.DesdeDisco(jIniDisco, tablaSin715, regIni) == jIniDisco, "equipo inicial con su módulo apagado: se queda None al leer");
        SaneadoObjetos.ParaDisco(jIniDisco, tablaSin715, regIni, true);
        Comprobar(regIni.ObjetosPosicion("lastStartingGearId").Count == 1, "equipo inicial con su módulo apagado: lo apuntado se conserva al guardar None");
        // El jugador elige otro objeto de serie: se olvida
        // The player picks another stock item: it is forgotten
        string jOtro = Regex.Replace(jIniDisco, @"(""lastStartingGearId""\s*:\s*)\d+", "${1}" + ESPADAS);
        SaneadoObjetos.ParaDisco(jOtro, tabla, regIni, true);
        Comprobar(regIni.ObjetosPosicion("lastStartingGearId").Count == 0 && SaneadoObjetos.DesdeDisco(jOtro, tabla, regIni) == jOtro,
            "equipo inicial cambiado por uno de serie: se olvida lo apuntado y no se restaura");

        // 0.7.2: lista de gear ampliada de la armería (contexto y unión de ids)
        // 0.7.2: expanded gear list of the armory (context and union of ids)
        ArmeriaObjetos.Reiniciar();
        var deSerieGear = Enumerable.Range(1, 40).ToList();
        int[] unidos = ArmeriaObjetos.Unir(deSerieGear, new[] { 700, 715, 700, 3 });
        Comprobar(unidos != null && unidos.Length == 42 && unidos.Take(40).SequenceEqual(deSerieGear) && unidos[40] == 700 && unidos[41] == 715,
            "armería: los 40 de serie en su orden y después los nuevos, sin repetir ni duplicar uno de serie");
        Comprobar(ArmeriaObjetos.Unir(deSerieGear, new int[0]) == null && ArmeriaObjetos.Unir(deSerieGear, new[] { 5 }) == null, "armería: sin ids nuevos no se cambia la lista");
        bool fuera = ArmeriaObjetos.Ampliando;
        ArmeriaObjetos.Entrar(); ArmeriaObjetos.Entrar();
        bool dentro = ArmeriaObjetos.Ampliando;
        ArmeriaObjetos.Salir(); bool dentroUno = ArmeriaObjetos.Ampliando;
        ArmeriaObjetos.Salir(); ArmeriaObjetos.Salir();
        Comprobar(!fuera && dentro && dentroUno && !ArmeriaObjetos.Ampliando, "armería: el contexto se abre y cierra emparejado (anidado y sin bajar de cero)");

        // Sorteo: proporciones con 22 objetos (7 de la facción A)
        // Draw: proportions with 22 items (7 from faction A)
        var objetos = new List<ObjetoSorteo>();
        foreach (int id in new[] { 700, 701, 702, 703, 704, 705, 706 }) objetos.Add(new ObjetoSorteo { Id = id, Afinidad = "40k" });
        foreach (int id in new[] { 710, 711, 712, 713, 714, 715, 716 }) objetos.Add(new ObjetoSorteo { Id = id, Afinidad = "starwars", Comandante = id == 715 ? 904 : id == 716 ? 903 : 0 });
        string[] razas = { "IronLegion", "Gruntkin", "RavenHost", "TaelindorForest", "SanguineCourt", "SakuraDynasty", "DeepstoneHold", "DrakosaurBrood" };
        for (int r = 0; r < 8; r++) objetos.Add(new ObjetoSorteo { Id = 720 + r, Afinidad = razas[r] });
        int afin = 0, sinAfin = 0;
        for (int i = 0; i < 20000; i++)
        {
            var c = new List<int> { ESPADAS };
            SorteoObjetos.Sustituir(c, objetos, new HashSet<string> { "40k" }, new HashSet<int>(), 1, i, 0.25, 0.05);
            if (c[0] >= 700 && c[0] <= 706) afin++;
            var c2 = new List<int> { ESPADAS };
            SorteoObjetos.Sustituir(c2, objetos, new HashSet<string>(), new HashSet<int>(), 1, i, 0.25, 0.05);
            if (c2[0] >= 700) sinAfin++;
        }
        // Esperado afín: el 25 % más la parte de la facción A del 5 % general (7 de 22 objetos) = 26,19 %
        // Expected affine: the 25% plus faction A's share of the general 5% (7 of 22 items) = 26.19%
        double espAfin = 0.25 + 0.75 * 0.05 * 7 / 22.0;
        Comprobar(Math.Abs(afin / 20000.0 - espAfin) < 0.012 && Math.Abs(sinAfin / 20000.0 - 0.05) < 0.006,
            $"sorteo: afín {afin / 200.0:0.0} % (esperado {espAfin * 100:0.0} %) y sin afinidad {sinAfin / 200.0:0.00} % (esperado 5 %)");
        var mismo1 = new List<int> { ESPADAS, 2, 3 }; var mismo2 = new List<int> { ESPADAS, 2, 3 };
        SorteoObjetos.Sustituir(mismo1, objetos, new HashSet<string> { "40k" }, new HashSet<int>(), 1, 4242, 1, 0);
        SorteoObjetos.Sustituir(mismo2, objetos, new HashSet<string> { "40k" }, new HashSet<int>(), 1, 4242, 1, 0);
        Comprobar(mismo1.SequenceEqual(mismo2) && mismo1.Distinct().Count() == 3, "sorteo: misma semilla, mismas cartas (no cambia al recargar) y sin repetir objeto");
        var unidades = SorteoObjetos.UnidadesDelEjercito(campana);
        Comprobar(unidades.Count >= 3, $"ejército leído del save real: {unidades.Count} escuadras ({string.Join(",", unidades.Take(6))}...)");
        var g = SorteoObjetos.GruposDelEjercito(new[] { 900, 905, 921, 921, 921, 0 }, u => 0, r => razas[r]);
        Comprobar(g.SetEquals(new[] { "starwars" }), "afinidad por ejército: 3 escuadras de Star Wars sí, 2 de 40k y 1 de Legión de Hierro no");

        // JSON de reglas con ListaCalculada y valor de mod en la regla principal
        // Rules JSON with ListaCalculada and mod value in the main rule
        var o700 = new Manifiesto.Objeto { Id = 700, Reglas = new List<Manifiesto.ReglaObjeto> {
            new Manifiesto.ReglaObjeto { Tipo = "stat", Stat = "Leadership", Magnitud = "Flat", Valor = "10", Principal = true,
                Condicion = new Manifiesto.CondicionObjeto { FilterKind = "ListaCalculada", Rarezas = new List<string> { "Common" }, Tamanos = new List<string> { "Infantry" } } },
            new Manifiesto.ReglaObjeto { Tipo = "atributo", Atributo = "Stalwart", Condicion = new Manifiesto.CondicionObjeto { FilterKind = "RarityTier", RequiredRarityTier = "Rare" } } } };
        string json = ReglasObjetos.Json(new[] { o700 }, 901, c => new[] { 0, 900, 3 }, id => id == 700 ? 12 : (int?)null, out int nS, out int nA);
        Comprobar(nS == 1 && nA == 1 && json.Contains("\"heroID\":\"901\"") && json.Contains("\"localizationKey\":\"700Name\"") && json.Contains("\"unitNames\":[\"0\",\"3\",\"900\"]")
                  && json.Contains("\"value\":\"12\"") && json.Contains("\"grantedAttribute\":\"Stalwart\"") && json.Contains("\"requiredRarityTier\":\"Rare\""),
            "reglas de objetos: héroe, clave del nombre, lista calculada numérica, valor de mod en la principal y rasgo");
        Comprobar(ReglasObjetos.CumpleLista(o700.Reglas[0].Condicion, "Common", "Infantry", "Melee") && !ReglasObjetos.CumpleLista(o700.Reglas[0].Condicion, "Common", "Cavalry", "Melee"),
            "ListaCalculada: infantería común sí, caballería común no");

        // Manifiesto real: 22 objetos activos con respaldo, afinidad, icono y reglas
        // Real manifest: 22 active items with fallback, affinity, icon and rules
        if (rutaManifiesto != null)
        {
            var m = ManifiestoDe(rutaManifiesto, out _);
            Comprobar(m.Objetos.Count == 22 && m.Objetos.All(o => o.Activo && o.Reglas.Count > 0 && !string.IsNullOrEmpty(o.Respaldo) && !string.IsNullOrEmpty(o.Icono))
                      && m.Objetos.Count(o => o.Afinidad == "40k") == 7 && m.Objetos.Count(o => o.Afinidad == "starwars") == 7,
                $"manifiesto: {m.Objetos.Count} objetos activos (7 de 40k, 7 de Star Wars y 8 de facción) con reglas, respaldo e icono");
            var icono = Path.IsPathRooted(m.Objetos.Find(o => o.Id == 700)?.Icono ?? "") ? m.Objetos.Find(o => o.Id == 700).Icono
                        : Path.Combine(Path.GetDirectoryName(rutaManifiesto), "objetos", "700.png");
            Comprobar(File.Exists(icono), $"manifiesto: el icono del 700 existe ({icono})");
        }
    }

    private static int Main(string[] args)
    {
        string carpeta = args.Length > 0 ? args[0] : ".";

        // -------- Prueba 0: lector de WAV (F3) con los WAV finales de audio\ y con WAV sintéticos raros
        // -------- Test 0: WAV reader (F3) with the final WAVs in audio\ and with odd synthetic WAVs
        PruebaWav(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "audio"));

        // Tabla de prueba: ordinales de ejemplo de las bases (el plugin los calcula en tiempo de ejecución).
        // 900 -> EisenmannRegiment (70), 907 -> también Eisenmann (dos unidades nuevas con la misma base),
        // 941 -> KaiserCannon (31), 944 -> Oni (95)
        // Test table: example ordinals of the bases (the plugin computes them at runtime).
        // 900 -> EisenmannRegiment (70), 907 -> also Eisenmann (two new units with the same base),
        // 941 -> KaiserCannon (31), 944 -> Oni (95)
        const int EIS = 70, KAISER = 31, ONI = 95;
        var tabla = new TablaUnidades(new Dictionary<int, int> { { 900, EIS }, { 907, EIS }, { 941, KAISER }, { 944, ONI } });

        // -------- Prueba 1: campaignSaveData.json con escuadras convertidas en 900, 944 y 907 y dos ofertas
        // -------- Test 1: campaignSaveData.json with squads converted into 900, 944 and 907 and two offers
        string campana = File.ReadAllText(Path.Combine(carpeta, "campaignSaveData.json"));
        int n = 0;
        string[] nuevas = { "900", "944", "907" };
        string conNuevas = Regex.Replace(campana, @"""UnitName"": \d+,", m => n < 3 ? @"""UnitName"": " + nuevas[n++] + "," : m.Value);
        conNuevas = Regex.Replace(conNuevas, @"(""recruitableUnits"": \[\s*)(\d+)(,\s*)(\d+)", "${1}900${3}941");
        Comprobar(n == 3, "el save de campaña tiene al menos 3 escuadras para la prueba");
        var reg = new RegistroGuardias(null);
        string disco = SaneadoSaves.ParaDisco(conNuevas, tabla, reg);
        Comprobar(SaneadoSaves.ContarResiduos(conNuevas, tabla) >= 2, "el save inyectado tiene unidades nuevas detectables (ofertas y campos de unidad)");
        Comprobar(SaneadoSaves.ContarResiduos(disco, tabla) == 0, "tras la ida al disco no queda ninguna unidad nueva");
        Comprobar(!Regex.IsMatch(disco, @""": (900|907|941|944)\b"), "no aparece ningún ': 9xx' en el texto escrito");
        Comprobar(reg.NumeroGuids == 3, "se apuntan 3 GUID");
        var of = reg.Ofertas(R(conNuevas, "ofertas"));
        Comprobar(of.Count == 2 && of[0] == 900 && of[1] == 941, "se apuntan las ofertas 0 (900) y 1 (941)");
        string vuelta = SaneadoSaves.DesdeDisco(disco, tabla, reg);
        Comprobar(vuelta == conNuevas, "la vuelta restaura el texto exacto (900, 944, 907 y las ofertas)");
        Comprobar(SaneadoSaves.DesdeDisco(disco, tabla, new RegistroGuardias(null)) == disco, "sin registro el save queda con las bases (carga sin plugin)");
        Comprobar(SaneadoSaves.ParaDisco(campana, tabla, new RegistroGuardias(null)) == campana, "un save sin unidades nuevas no se toca");
        // Unidad no disponible (p. ej. mod desactivado): no se restaura
        // Unit not available (e.g. disabled mod): not restored
        var tablaSin944 = new TablaUnidades(tabla.BasePorId.ToDictionary(k => k.Key, k => k.Value)) { Disponible = id => id != 944 };
        string vueltaSin = SaneadoSaves.DesdeDisco(disco, tablaSin944, reg);
        Comprobar(!vueltaSin.Contains("\"UnitName\": 944") && vueltaSin.Contains("\"UnitName\": 900"), "una unidad que ya no existe en el catálogo se queda como su base al leer");

        // -------- Prueba 1b: ejército ENEMIGO por GUID (la raza del rival se lee de su primera escuadra)
        // -------- Test 1b: ENEMY army by GUID (the rival's race is read from its first squad)
        string enemigo = "{\"enemyArmy\":{\"squads\":[{\"UnitName\":944,\"UniqueID\":\"enem-1\"},{\"UnitName\":12,\"UniqueID\":\"enem-2\"}]}," +
                         "\"playerArmy\":{\"squads\":[{\"UnitName\":95,\"UniqueID\":\"oni-de-verdad\"}]}}";
        var regE = new RegistroGuardias(null);
        string eDisco1 = SaneadoSaves.ParaDisco(enemigo, tabla, regE);
        Comprobar(eDisco1.Contains("{\"UnitName\":95,\"UniqueID\":\"enem-1\"}") && regE.IdDeGuid("enem-1") == 944, "escuadra enemiga 944 guardada como Oni con su GUID apuntado");
        string eVuelta = SaneadoSaves.DesdeDisco(eDisco1, tabla, regE);
        Comprobar(eVuelta == enemigo, "la vuelta devuelve la 944 al enemigo y deja el Oni de verdad como Oni");

        // -------- Prueba 2: playerSaveData.json con unidades nuevas en las listas de colección
        // -------- Test 2: playerSaveData.json with new units in the collection lists
        string jugador = File.ReadAllText(Path.Combine(carpeta, "playerSaveData.json"));
        string jConNuevas = Regex.Replace(jugador, @"(""troopsRecruited"": \[)", "${1}\n        900,\n        944,");
        jConNuevas = Regex.Replace(jConNuevas, @"(""troopsAcknowledged"": \[)", "${1}\n        941,");
        var reg2 = new RegistroGuardias(null);
        string jDisco = SaneadoSaves.ParaDisco(jConNuevas, tabla, reg2);
        Comprobar(SaneadoSaves.ContarResiduos(jConNuevas, tabla) == 3, "el save de jugador inyectado tiene 3 unidades nuevas");
        Comprobar(SaneadoSaves.ContarResiduos(jDisco, tabla) == 0, "tras la ida al disco no queda ninguna en el save de jugador");
        Comprobar(jDisco == jugador, "la ida al disco deja el save de jugador idéntico al original");
        string jVuelta = SaneadoSaves.DesdeDisco(jDisco, tabla, reg2);
        Comprobar(Regex.IsMatch(jVuelta, @"""troopsRecruited"": \[[^\]]*900") && Regex.IsMatch(jVuelta, @"""troopsRecruited"": \[[^\]]*944")
                  && Regex.IsMatch(jVuelta, @"""troopsAcknowledged"": \[[^\]]*941"), "la vuelta devuelve 900 y 944 a troopsRecruited y 941 a troopsAcknowledged");
        Comprobar(SaneadoSaves.ContarResiduos(SaneadoSaves.ParaDisco(jVuelta, tabla, reg2), tabla) == 0, "segunda ida tras la vuelta: sin unidades nuevas");

        // -------- Prueba 3: JSON compacto, campo suelto y números parecidos que NO deben tocarse
        // -------- Test 3: compact JSON, loose field and similar numbers that must NOT be touched
        string compacto = "{\"a\":[{\"UnitName\":900,\"UniqueID\":\"abc-1\"}],\"unit\":944,\"gold\":900,\"UnitName2\":1900,\"x\":9440," +
                          "\"recruitableUnits\":[1,900,2],\"troopsRecruited\":[941]}";
        var reg3 = new RegistroGuardias(null);
        string cDisco = SaneadoSaves.ParaDisco(compacto, tabla, reg3);
        Console.WriteLine("      compacto -> " + cDisco);
        Comprobar(cDisco.Contains("\"gold\":900") && cDisco.Contains("1900") && cDisco.Contains("9440") && cDisco.Contains("\"unit\":95"),
            "no toca números 9xx de otras claves ni 1900/9440; el campo suelto 944 pasa a Oni");
        Comprobar(SaneadoSaves.ContarResiduos(cDisco, tabla) == 0, "compacto sin residuos");
        string cVuelta = SaneadoSaves.DesdeDisco(cDisco, tabla, reg3);
        Console.WriteLine("      vuelta   -> " + cVuelta);
        Comprobar(cVuelta.Contains("\"UnitName\":900,\"UniqueID\":\"abc-1\"") && cVuelta.Contains("[1,900,2]") && cVuelta.Contains("\"troopsRecruited\":[941]"),
            "compacto: vuelta correcta (escuadra, oferta y lista)");

        // -------- Prueba 3b: array SquadsInArmy (sin GUID) también queda sin unidades nuevas
        // -------- Test 3b: SquadsInArmy array (without GUID) also ends up without new units
        string ejercito = "{\n    \"SquadsInArmy\": [\n        3,\n        900,\n        941\n    ]\n}";
        string eDisco = SaneadoSaves.ParaDisco(ejercito, tabla, new RegistroGuardias(null));
        Comprobar(SaneadoSaves.ContarResiduos(eDisco, tabla) == 0 && eDisco.Contains("        70,\n        31\n"), "SquadsInArmy: 900 y 941 pasan a su base con el mismo formato");

        // -------- Prueba 4: ofertas nuevas sin unidades nuevas olvidan las posiciones apuntadas
        // -------- Test 4: new offers without new units forget the recorded positions
        string sinNuevas = "{\"recruitableUnits\":[70,3,4]}";
        SaneadoSaves.ParaDisco(sinNuevas, tabla, reg3);
        Comprobar(reg3.Ofertas().Count == 0, "ofertas nuevas sin unidades nuevas: se olvidan las posiciones");
        Comprobar(SaneadoSaves.DesdeDisco(sinNuevas, tabla, reg3) == sinNuevas, "un Eisenmann legítimo en ofertas no se convierte en guardia");

        // -------- Prueba 5: registro en disco (guardar y volver a cargar)
        // -------- Test 5: registry on disk (save and reload)
        string ruta = Path.Combine(Path.GetTempPath(), "zgi_registro_prueba.json");
        foreach (string f in Directory.GetFiles(Path.GetTempPath(), "zgi_registro_prueba.json*")) File.Delete(f);
        var r4 = new RegistroGuardias(ruta);
        r4.ApuntarGuid("g-1", 944); r4.FijarOfertas(new Dictionary<int, int> { { 2, 941 } }); r4.MarcarLista("troopsRecruited", 900);
        var r5 = new RegistroGuardias(ruta);
        r5.Cargar();
        Comprobar(r5.IdDeGuid("g-1") == 944 && r5.Ofertas()[2] == 941 && r5.IdsDeLista("troopsRecruited").Contains(900), "el registro sobrevive a guardar y cargar");

        // -------- Prueba 5b: registro antiguo (versión 1, solo la 900) se convierte solo y deja copia
        // -------- Test 5b: old registry (version 1, only 900) converts itself and leaves a copy
        File.WriteAllText(ruta, "{\"Version\":1,\"Guids\":[\"viejo-1\",\"viejo-2\"],\"IndicesOfertas\":[1],\"ListasMarcadas\":[\"troopsRecruited\"]}");
        var r6 = new RegistroGuardias(ruta);
        r6.Cargar();
        Comprobar(r6.IdDeGuid("viejo-1") == 900 && r6.IdDeGuid("viejo-2") == 900 && r6.Ofertas()[1] == 900 && r6.IdsDeLista("troopsRecruited").Contains(900),
            "registro versión 1: GUID, oferta y lista pasan a la 900");
        Comprobar(Directory.GetFiles(Path.GetTempPath(), "zgi_registro_prueba.json.v1-*").Length == 1, "registro versión 1: queda una copia .v1-<fecha>");
        Comprobar(File.ReadAllText(ruta).Contains("\"Version\": 2"), "registro versión 1: se reescribe en versión 2");
        // Un save con guardias antiguos (Eisenmann + GUID de la versión 1) se restaura como 900
        // A save with old guards (Eisenmann + version 1 GUID) is restored as 900
        string guardado = "{\"UnitName\": 70, \"UniqueID\": \"viejo-1\"}";
        Comprobar(SaneadoSaves.DesdeDisco(guardado, tabla, r6) == "{\"UnitName\": 900, \"UniqueID\": \"viejo-1\"}", "registro versión 1: un guardia antiguo vuelve a ser 900");
        foreach (string f in Directory.GetFiles(Path.GetTempPath(), "zgi_registro_prueba.json*")) File.Delete(f);

        // -------- Prueba 5c (0.4.1): el saneado de un save entero escribe el registro una sola vez
        // -------- Test 5c (0.4.1): sanitizing a whole save writes the registry only once
        var r7 = new RegistroGuardias(null);
        string tresNuevas = "{\"a\":[{\"UnitName\":900,\"UniqueID\":\"l-1\"},{\"UnitName\":944,\"UniqueID\":\"l-2\"},{\"UnitName\":941,\"UniqueID\":\"l-3\"}]," +
                            "\"recruitableUnits\":[900,1],\"troopsRecruited\":[941]}";
        SaneadoSaves.ParaDisco(tresNuevas, tabla, r7);
        Comprobar(r7.NumeroGuids == 3 && r7.Escrituras == 1, $"3 GUID nuevos, oferta y lista en un solo texto: 1 escritura del registro (hubo {r7.Escrituras})");
        SaneadoSaves.ParaDisco(tresNuevas, tabla, r7);
        Comprobar(r7.Escrituras == 1, "repetir el mismo texto no vuelve a escribir el registro");

        // -------- Prueba 5d (0.4.1): ofertas separadas para el save normal y la instantánea del nodo
        // -------- Test 5d (0.4.1): separate offers for the normal save and the node snapshot
        var r8 = new RegistroGuardias(null);
        string normal = "{\"snapShot\": false, \"recruitableUnits\": [900,5,6]}";
        string instant = "{\"snapShot\": true, \"recruitableUnits\": [7,941,8]}";
        string dNormal = SaneadoSaves.ParaDisco(normal, tabla, r8);
        string dInstant = SaneadoSaves.ParaDisco(instant, tabla, r8);
        Comprobar(r8.Ofertas(false).Count == 1 && r8.Ofertas(false)[0] == 900 && r8.Ofertas(true).Count == 1 && r8.Ofertas(true)[1] == 941,
            "la oferta del save normal (900 en 0) y la de la instantánea (941 en 1) se apuntan por separado");
        Comprobar(SaneadoSaves.DesdeDisco(dNormal, tabla, r8) == normal && SaneadoSaves.DesdeDisco(dInstant, tabla, r8) == instant,
            "cada save recupera sus propias ofertas");
        SaneadoSaves.ParaDisco("{\"snapShot\": true, \"recruitableUnits\": [1,2,3]}", tabla, r8);
        Comprobar(r8.Ofertas(true).Count == 0 && r8.Ofertas(false).Count == 1, "una instantánea nueva sin unidades nuevas no borra las ofertas del save normal");

        // -------- Prueba 5e (0.4.1): aviso cuando un GUID apuntado no se puede restaurar, y la entrada se conserva
        // -------- Test 5e (0.4.1): warning when a recorded GUID cannot be restored, and the entry is kept
        var avisos = new List<string>();
        SaneadoSaves.AvisarNoRestaurable = avisos.Add;
        var r9 = new RegistroGuardias(null);
        r9.ApuntarGuid("x-1", 944);
        string baseDistinta = "{\"UnitName\": 70, \"UniqueID\": \"x-1\"}";
        Comprobar(SaneadoSaves.DesdeDisco(baseDistinta, tabla, r9) == baseDistinta && avisos.Count == 1 && r9.IdDeGuid("x-1") == 944,
            "GUID de la 944 sobre un Eisenmann: no se restaura, avisa una vez y conserva la entrada");
        SaneadoSaves.DesdeDisco(baseDistinta, tabla, r9);
        Comprobar(avisos.Count == 1, "el aviso no se repite para el mismo GUID");
        r9.ApuntarGuid("x-2", 900);
        string yaRestaurado = "{\"UnitName\": 900, \"UniqueID\": \"x-2\"}";
        Comprobar(SaneadoSaves.DesdeDisco(yaRestaurado, tabla, r9) == yaRestaurado && avisos.Count == 1,
            "un texto que ya trae la unidad nueva (lectura doble) no avisa");
        Console.WriteLine("      aviso -> " + (avisos.Count > 0 ? avisos[0] : "(ninguno)"));
        SaneadoSaves.AvisarNoRestaurable = null;

        // -------- Prueba 6: manifiesto de la fábrica (el generado de verdad, si se pasa la ruta) y el mínimo interno
        // -------- Test 6: factory manifest (the really generated one, if the path is passed) and the internal minimum
        var porDefecto = Manifiesto.Leer(Path.Combine(Path.GetTempPath(), "no_existe_zgi.json"), out string motivo);
        Comprobar(porDefecto.Unidades.Count == 0 && porDefecto.Razas.Count == 0 && porDefecto.Objetos.Count == 0 && motivo != null,
            "sin fichero: manifiesto VACÍO (0.7.0: sin módulos no se registra nada, ni la 900)");
        if (args.Length > 1)
        {
            var m = ManifiestoDe(args[1], out string motivo2);
            int activas = m.Unidades.Count(u => u.Activa);
            Console.WriteLine($"      manifiesto {args[1]}: {m.Unidades.Count} unidades, {activas} activas, {m.Razas.Count} razas, {m.Heroes.Count} héroes");
            // 18-11-2026 (módulos 1.1.4, núcleo 0.7.13): las 7 caballerías con jinete pasan a activas (jinete probado en batalla)
            // 18-11-2026 (modules 1.1.4, core 0.7.13): the 7 mounted cavalry units become active (rider tested in battle)
            Comprobar(motivo2 == null && m.Unidades.Count == 60 && activas == 60 && m.Razas.Count == 2 && m.Heroes.Count == 4,
                "manifiesto generado: 60 unidades, las 60 activas (también las 7 caballerías con jinete), 2 razas y 4 héroes");
            Comprobar(new[] { 902, 923, 926, 947, 950, 963, 967 }.All(id => { var u = m.Unidades.Find(x => x.Id == id); return u != null && u.Activa && u.Caballeria; }),
                "manifiesto: las 7 caballerías con jinete (902, 923, 926, 947, 950, 963, 967) activas y marcadas como caballería");
            Comprobar(m.Unidades.Where(u => u.Caballeria).Select(u => u.Id).OrderBy(x => x).SequenceEqual(new[] { 902, 923, 926, 947, 950, 963, 967 }),
                "manifiesto: solo esas 7 llevan la marca de caballería con jinete");
            // F2: razas y héroes tipados y activos, con lo prestado y la raza sustituta para el disco
            // F2: typed and active races and heroes, with the borrowed ones and the substitute race for disk
            var r40 = m.Razas.Find(r => r.Id == 40); var r41 = m.Razas.Find(r => r.Id == 41);
            Comprobar(r40 != null && r40.Activa && r40.PasivaPrestada == "IronLegion" && r40.RazaSustituta == "IronLegion" && r40.Colores.Count == 3
                      && r40.Heroes.SequenceEqual(new[] { 901, 902 }) && r40.RazaRegion == "DeepstoneHold" && r40.ActoCampana == 2,
                "manifiesto: raza 40 activa (pasiva Iron Legion, región de Deepstone, acto 2, héroes 901 y 902, sustituta Iron Legion)");
            Comprobar(r41 != null && r41.Activa && r41.PasivaPrestada == "TaelindorForest" && r41.Musica == "RavenHost"
                      && r41.Heroes.SequenceEqual(new[] { 903, 904 }) && r41.ActoCampana == 3,
                "manifiesto: raza 41 activa (pasiva Taelindor, música Raven Host, acto 3, héroes 903 y 904)");
            // 0.7.6 (prueba de Zero del 03-11-2026): las dos razas salen como enemigas en los tres actos
            // 0.7.6 (Zero's test of 03-11-2026): both races appear as enemies in all three acts
            Comprobar(r40 != null && r41 != null && r40.ActosCampana.SequenceEqual(new[] { 1, 2, 3 }) && r41.ActosCampana.SequenceEqual(new[] { 1, 2, 3 })
                      && Enumerable.Range(1, 3).All(a => LogicaCampanaHeroes.SaleEnActo(a, r40.ActoCampana, r40.ActosCampana) && LogicaCampanaHeroes.SaleEnActo(a, r41.ActoCampana, r41.ActosCampana)),
                "manifiesto: las razas 40 y 41 salen como ejército enemigo en los actos 1, 2 y 3 (actosCampana)");
            Comprobar(m.Heroes.All(h => h.Activa && h.PrestadoDe > 0 && h.OroComo > 0 && !string.IsNullOrEmpty(h.Retrato) && h.Muestra.Count == 3)
                      && m.Heroes.Count(h => h.Raza == 40) == 2 && m.Heroes.Count(h => h.Raza == 41) == 2,
                "manifiesto: 4 héroes activos, 2 por raza nueva, con figura prestada, oro, retrato y ejército inicial de 3 unidades (17-10-2026)");
            Comprobar(m.Heroes.All(h => h.Muestra.All(id => m.Unidades.Find(x => x.Id == id)?.Activa == true)),
                "manifiesto: todas las unidades de los ejércitos de muestra de los héroes están activas");
            // 18-11-2026 (módulos 1.1.4): las 27, también las 3 con jinete (902, 923, 926)
            // 18-11-2026 (modules 1.1.4): all 27, the 3 with a rider included (902, 923, 926)
            Comprobar(m.Unidades.Where(u => u.Raza == "40" || u.Raza == "41").Count(u => u.Activa) == 27,
                "manifiesto: las 27 unidades de Astra Militarum e Imperio Galáctico activas (también las 3 con jinete)");
            var u944 = m.Unidades.Find(u => u.Id == 944);
            Comprobar(u944 != null && u944.Base == "Oni" && u944.Raza == "IronLegion" && u944.Activa && u944.Reclutamiento == Manifiesto.ReclutaPorRaza,
                "manifiesto: 944 (Ogros de Asedio) clona Oni, raza IronLegion, activa, por raza");
            var u900 = m.Unidades.Find(u => u.Id == 900);
            Comprobar(u900 != null && u900.Reclutamiento == Manifiesto.ReclutaOfertasJugador && u900.PrestigioPropio && u900.EstelaRoja,
                "manifiesto: la 900 sigue en ofertas del jugador con su prestigio y estela");
        }

        // -------- Prueba 7 (F2): razas nuevas 40/41 en el save: ciudad, campo de batalla y temas de taberna
        // -------- Test 7 (F2): new races 40/41 in the save: city, battlefield and tavern themes
        PruebaRazas(campana, File.ReadAllText(Path.Combine(carpeta, "playerSaveData.json")));

        // -------- Prueba 8 (0.6.0): objetos nuevos (gear) 700-727
        // -------- Test 8 (0.6.0): new gear 700-727
        PruebaObjetos(campana, File.ReadAllText(Path.Combine(carpeta, "playerSaveData.json")), args.Length > 1 ? args[1] : null);

        // -------- Prueba 9 (0.7.0): módulos del núcleo
        // -------- Test 9 (0.7.0): core modules
        PruebasModulos.Todas(Comprobar);
        if (args.Length > 1 && Directory.Exists(args[1])) PruebasModulos.Reales(args[1], Comprobar);
        PruebasRevision.Todas(Comprobar, campana);   // Prueba 10 (0.7.1): arreglos de la revisión (PruebasRevision.cs)
                                                     // Test 10 (0.7.1): review fixes (PruebasRevision.cs)
        PruebasInterfaz.Todas(Comprobar);            // Prueba 11: rejillas con desplazamiento de la interfaz (PruebasInterfaz.cs)
                                                     // Test 11: interface scroll grids (PruebasInterfaz.cs)
        // Prueba 12: pantalla de héroes (casillas) y ejército inicial de los héroes nuevos (PruebasHeroes.cs)
        // Test 12: heroes screen (slots) and starting army of the new heroes (PruebasHeroes.cs)
        PruebasHeroes.Todas(Comprobar, args.Length > 1 ? ManifiestoDe(args[1], out _) : null);
        // Prueba 13: héroes nuevos en el mapa de campaña, assets sin entrada y contador de la Colección (PruebasCampana.cs)
        // Test 13: new heroes on the campaign map, assets without an entry and the Collection counter (PruebasCampana.cs)
        PruebasCampana.Todas(Comprobar, args.Length > 1 ? ManifiestoDe(args[1], out _) : null);
        // Prueba 14 (0.7.2): logros de Steam de serie sin contenido nuevo y condición UnitName del 721 (PruebasLogros.cs)
        // Test 14 (0.7.2): default Steam achievements without new content and UnitName condition of 721 (PruebasLogros.cs)
        PruebasLogros.Todas(Comprobar, args.Length > 1 ? ManifiestoDe(args[1], out _) : null);
        // Prueba 15 (revisión de la 0.7.2): doble pasada del saneado de escritura y victorias de héroes nuevos (PruebasDoblePasada.cs)
        // Test 15 (review of 0.7.2): double pass of the write sanitizing and wins of new heroes (PruebasDoblePasada.cs)
        PruebasDoblePasada.Todas(Comprobar, File.ReadAllText(Path.Combine(carpeta, "playerSaveData.json")));

        // Prueba 16 (F5): formato del modelo 3D propio de los héroes nuevos y sus huesos (PruebasModeloHeroes.cs)
        // Test 16 (F5): format of the custom 3D model of the new heroes and their bones (PruebasModeloHeroes.cs)
        PruebasModeloHeroes.Todas(Comprobar, args.Length > 1 ? args[1] : null);
        // Prueba 17: arreglos de inlining (ParchesInlineados.cs; lógica en LogicaInlineados.cs, PruebasInlineados.cs)
        // Test 17: inlining fixes (ParchesInlineados.cs; logic in LogicaInlineados.cs, PruebasInlineados.cs)
        PruebasInlineados.Todas(Comprobar);
        // Prueba 18: arreglos de la revisión de código del 18-10 (A-1 vigilante, M-1 bajas históricas, M-2 bloqueo por
        // partida, M-3 reescritura segura; PruebasArreglos18.cs)
        // Test 18: fixes from the 18-10 code review (A-1 watcher, M-1 historical casualties, M-2 per-run
        // lock, M-3 safe rewrite; PruebasArreglos18.cs)
        PruebasArreglos18.Todas(Comprobar, File.ReadAllText(Path.Combine(carpeta, "playerSaveData.json")));
        // Prueba 19 (18-10): Tesorería imperial, el x2 solo sobre los primeros 5 de interés (PruebasInteres.cs)
        // Test 19 (18-10): Imperial Treasury, the x2 only on the first 5 of interest (PruebasInteres.cs)
        PruebasInteres.Todas(Comprobar);
        // Prueba 20 (18-10, noche): segunda revisión (M-A registro bloqueado, B-9 campos nuevos, iconos 2:3; PruebasRonda2.cs)
        // Test 20 (18-10, night): second review (M-A locked registry, B-9 new fields, 2:3 icons; PruebasRonda2.cs)
        PruebasRonda2.Todas(Comprobar);
        // Prueba 21 (18-10, noche): revisión externa (P-B telemetría por evento, aviso de versión del juego; PruebasRevisionExterna.cs)
        // Test 21 (18-10, night): external review (P-B per-event telemetry, game version warning; PruebasRevisionExterna.cs)
        PruebasRevisionExterna.Todas(Comprobar);
        // Prueba 22 (18-10): acción propia de los héroes nuevos en «Elige uno» tras la batalla (PruebasAccionesHeroes.cs)
        // Test 22 (18-10): own action of the new heroes in «Elige uno» after the battle (PruebasAccionesHeroes.cs)
        PruebasAccionesHeroes.Todas(Comprobar, args.Length > 1 ? ManifiestoDe(args[1], out _) : null);
        // Prueba 23 (18-10): sonido láser de las armas de energía: tipos, capas, migración y WAV de los módulos (PruebasSonidos.cs)
        // Test 23 (18-10): laser sound of energy weapons: types, layers, migration and WAV files of the modules (PruebasSonidos.cs)
        PruebasSonidos.Todas(Comprobar, args.Length > 1 && Directory.Exists(args[1]) ? ManifiestoDe(args[1], out _) : null);
        // Prueba 24 (01-11, juego 1.11.1): carga tolerante (fases de Load() y tipos por nombre; LogicaCarga.cs, PruebasCarga.cs)
        // Test 24 (01-11, game 1.11.1): tolerant loading (phases of Load() and types by name; LogicaCarga.cs, PruebasCarga.cs)
        PruebasCarga.Todas(Comprobar);
        // Prueba 25 (01-11, juego 1.11.1): arreglos de la revisión de datos (hechizo registrado, rasgos de campaña a False,
        // iconos de 512 x 768, núcleo mínimo y nombres; LogicaHechizos.cs, PruebasDatos1111.cs)
        // Test 25 (01-11, game 1.11.1): fixes from the data review (registered spell, campaign traits set to False,
        // 512 x 768 icons, minimum core and names; LogicaHechizos.cs, PruebasDatos1111.cs)
        PruebasDatos1111.Todas(Comprobar, args.Length > 1 && Directory.Exists(args[1]) ? args[1] : null);
        // Prueba 26 (02-11, núcleo 0.7.4): hechizos propios (validación, plan B, Grand Grimoire, saneado de saves), sorteo por
        // rareza y, con los módulos, los 4 hechizos del manifiesto y sus textos (PruebasHechizos.cs)
        // Test 26 (02-11, core 0.7.4): custom spells (validation, plan B, Grand Grimoire, save sanitizing), draw by
        // rarity and, with the modules, the 4 spells in the manifest and their texts (PruebasHechizos.cs)
        PruebasHechizos.Todas(Comprobar, args.Length > 1 && Directory.Exists(args[1]) ? args[1] : null);
        // Prueba 27 (02-11, núcleo 0.7.5): revisión total (ranuras por partida y slot, arrays nuevos de la 1.11.1, historial y
        // save del jugador real, logros de ejército por partida, omisiones protegidas; PruebasRevisionTotal.cs)
        // Test 27 (02-11, core 0.7.5): full review (slots per run and slot, new arrays from 1.11.1, history and
        // real player save, army achievements per run, protected omissions; PruebasRevisionTotal.cs)
        PruebasRevisionTotal.Todas(Comprobar, args.Length > 1 && Directory.Exists(args[1]) ? args[1] : null);
        // Prueba 28 (03-11, crash al arrancar con la autoprueba): ningún método del juego con ref/out de un struct no blittable
        // se llama por su envoltorio del interop (desborda la pila; escaneo de la DLL compilada, PruebasInterop.cs)
        // Test 28 (03-11, crash on startup with the self-test): no game method with ref/out of a non-blittable struct
        // is called through its interop wrapper (it overflows the stack; scan of the compiled DLL, PruebasInterop.cs)
        PruebasInterop.Todas(Comprobar);

        // Prueba 29 (03-11, núcleo 0.7.6): moral del jugador del módulo «Impertérritos» (PruebasMoral.cs)
        // Test 29 (03-11, core 0.7.6): player morale of the module «Impertérritos» (PruebasMoral.cs)
        PruebasMoral.Todas(Comprobar);
        // Prueba 30 (03-11, núcleo 0.7.7): iconos del estandarte y su elección en el registro (PruebasEstandarte.cs)
        // Test 30 (03-11, core 0.7.7): banner icons and their selection in the registry (PruebasEstandarte.cs)
        PruebasEstandarte.Todas(Comprobar);
        // Prueba 31 (03-11, arreglo de la 0.7.7): ningún enganche de ciclo de vida de Unity llama a FindObjectsOfType*
        // (la 0.7.7 no cargaba por eso: Stack overflow; PruebasCicloVida.cs)
        // Test 31 (03-11, fix for 0.7.7): no Unity lifecycle hook calls FindObjectsOfType*
        // (0.7.7 failed to load because of that: Stack overflow; PruebasCicloVida.cs)
        PruebasCicloVida.Todas(Comprobar);
        // Prueba 32 (17-11, núcleo 0.7.9): ficheros del juego de los modelos propios reconstruidos desde la instalación
        // (con la carpeta de módulos, además, todas las declaraciones reales contra el juego instalado; PruebasFicherosJuego.cs)
        // Test 32 (17-11, core 0.7.9): game files of the custom models rebuilt from the installation
        // (with the modules folder, also all real declarations against the installed game; PruebasFicherosJuego.cs)
        PruebasFicherosJuego.Todas(Comprobar, args.Length > 1 && Directory.Exists(args[1]) ? args[1] : null);
        // Prueba 33 (18-11-2026, núcleo 0.7.11): instalación de los módulos y entradas en línea por línea de comandos (PruebasInstalacion.cs)
        // Test 33 (18-11-2026, core 0.7.11): module installation and online command-line entries (PruebasInstalacion.cs)
        PruebasInstalacion.Todas(Comprobar);
        // Prueba 34 (18-11-2026, núcleo 0.7.13): migración de Batalla.JineteCaballeria a encendida (PruebasJinete.cs)
        // Test 34 (18-11-2026, core 0.7.13): migration of Batalla.JineteCaballeria to on (PruebasJinete.cs)
        PruebasJinete.Todas(Comprobar);

        Console.WriteLine(fallos == 0 ? "TODAS LAS PRUEBAS OK" : $"{fallos} FALLOS");
        return fallos == 0 ? 0 : 1;
    }
}
