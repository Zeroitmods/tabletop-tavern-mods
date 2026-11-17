// PruebasRevision.cs
// Prueba 10 (0.7.1): arreglos de la revisión independiente del 17-10-2026, fuera del juego:
//   - héroes nuevos 901-904 en el save de campaña (héroe del jugador y señor de la guerra): se guardan como su héroe
//     prestado y vuelven al leer en la misma partida (runId); no vuelven en otra partida ni con su módulo apagado (se
//     conservan para cuando se active); un 903 que llegue en el disco con el módulo apagado se carga como su prestado y
//     se apunta; la instantánea va aparte; el save del jugador (historial con «heroID») no se toca; pasar dos veces el
//     saneado no olvida nada; partidas antiguas sin runId usan la semilla;
//   - marcas de objetos ancladas a la partida: en la misma partida vuelven, en otra ni vuelven ni se conservan al guardar;
//     las listas de colección (save del jugador) valen para todas;
//   - stats a distancia con varios módulos: manda el mayor (suma de los tres bonus), con aviso.
// Autor: ZeroitMods.
// PruebasRevision.cs
// Test 10 (0.7.1): fixes from the independent review of 10-17-2026, outside the game:
//   - new heroes 901-904 in the campaign save (player hero and warlord): they are saved as their borrowed hero
//     and come back on read in the same run (runId); they do not come back in another run or with their module off (they are
//     kept for when it is enabled); a 903 that arrives on disk with the module off is loaded as its borrowed one and
//     recorded; the snapshot is kept separate; the player's save (history with «heroID») is not touched; running the
//     sanitizing twice forgets nothing; old runs without runId use the seed;
//   - gear marks tied to the run: in the same run they come back, in another they neither come back nor are kept on save;
//     the Collection lists (player save) apply to all;
//   - ranged stats with several modules: the highest wins (sum of the three bonuses), with a warning.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZeroNucleo;

internal static class PruebasRevision
{
    // ---------------------------------------------------------------
    // Ayudantes: textos de save de campaña y del jugador (formato de JsonUtility con prettyPrint)
    // Helpers: campaign and player save texts (JsonUtility format with prettyPrint)
    // ---------------------------------------------------------------
    // Save de campaña mínimo con los campos que miran los saneados (runId null = partida antigua sin runId)
    // Minimal campaign save with the fields the sanitizing looks at (runId null = old run without runId)
    private static string Campana(int heroe, int senor, string runId, bool instantanea = false, string gear = "5,1", string vendidos = "")
    {
        string run = runId == null ? "" : $"    \"runId\": \"{runId}\",\n";
        return "{\n    \"saveVersion\": 1,\n    \"seed\": 123,\n" +
               $"    \"Gear\": [{gear}],\n    \"SoldGear\": [{vendidos}],\n" +
               $"    \"heroID\": {heroe},\n" + run +
               $"    \"enemyWarlordHeroID\": {senor},\n" +
               $"    \"snapShot\": {(instantanea ? "true" : "false")}\n}}";
    }

    // Valor del primer campo numérico «clave» del texto (-999 si no está)
    // Value of the first numeric field «clave» in the text (-999 if absent)
    private static int Campo(string json, string clave)
    {
        var m = Regex.Match(json, "\"" + clave + "\"\\s*:\\s*(-?\\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : -999;
    }

    public static void Todas(Action<bool, string> comprobar, string campanaReal = null)
    {
        PruebaHeroes(comprobar, campanaReal);
        PruebaAnclaObjetos(comprobar);
        PruebaDistancia(comprobar);
    }

    // ---------------------------------------------------------------
    // Héroes nuevos en el save de campaña
    // New heroes in the campaign save
    // ---------------------------------------------------------------
    private static void PruebaHeroes(Action<bool, string> comprobar, string campanaReal)
    {
        // 901 presta a 6 y 903 a 7; «activos» simula qué módulos de facción están aplicados
        // 901 lends 6 and 903 lends 7; «activos» simulates which faction modules are applied
        var activos = new HashSet<int> { 901, 903 };
        var tabla = new TablaHeroes(new Dictionary<int, int> { { 901, 6 }, { 903, 7 } }) { Disponible = activos.Contains };
        var reg = new RegistroGuardias(null);

        // Firma de la partida
        // Run signature
        comprobar(SaneadoSaves.FirmaPartida(Campana(3, 0, "A")) == "A" && SaneadoSaves.FirmaPartida(Campana(3, 0, null)) == "semilla:123"
                  && SaneadoSaves.FirmaPartida("{\"heroID\": 3}") == null,
            "héroes: firma de la partida = runId; sin runId, la semilla; fuera del save de campaña, ninguna");

        // Ida al disco: los dos campos como su prestado, apuntados con la partida
        // Trip to disk: both fields as their borrowed hero, recorded with the run
        string t = Campana(903, 901, "A");
        string d = SaneadoSaves.HeroesParaDisco(t, tabla, reg);
        bool apuntado = reg.HeroeDe(Program.R(t, "heroe"), out int real, out int esc, out string firma) && real == 903 && esc == 7 && firma == "A";
        bool senor = reg.HeroeDe(Program.R(t, "senor"), out int realS, out int escS, out _) && realS == 901 && escS == 6;
        comprobar(Campo(d, "heroID") == 7 && Campo(d, "enemyWarlordHeroID") == 6 && apuntado && senor,
            "héroes: 903 y señor de la guerra 901 se guardan como 7 y 6, con el real y la partida en el registro");
        comprobar(SaneadoSaves.ContarResiduosHeroes(t, tabla) == 2 && SaneadoSaves.ContarResiduosHeroes(d, tabla) == 0,
            "héroes: 2 residuos antes del saneado y 0 después");

        // Dos pasadas (ToJson y WriteAllText sobre el mismo save): mismo texto y el registro sigue apuntando el 903
        // Two passes (ToJson and WriteAllText on the same save): same text and the registry still records 903
        string d2 = SaneadoSaves.HeroesParaDisco(d, tabla, reg);
        comprobar(d2 == d && reg.HeroeDe(Program.R(t, "heroe"), out int real2, out _, out _) && real2 == 903 && reg.HeroeDe(Program.R(t, "senor"), out _, out _, out _),
            "héroes: una segunda pasada del saneado no cambia el texto ni olvida lo apuntado");

        // Vuelta en la misma partida: el texto original
        // Return in the same run: the original text
        comprobar(SaneadoSaves.HeroesDesdeDisco(d, tabla, reg) == t, "héroes: al leer en la misma partida vuelven 903 y 901 (texto idéntico)");

        // Otra partida (otro runId): no se restaura nada
        // Another run (another runId): nothing is restored
        string otra = d.Replace("\"runId\": \"A\"", "\"runId\": \"B\"");
        comprobar(Campo(SaneadoSaves.HeroesDesdeDisco(otra, tabla, reg), "heroID") == 7, "héroes: en otra partida (otro runId) no se restaura el héroe");

        // Módulo del 903 apagado: se queda el prestado, se conserva lo apuntado al guardar y vuelve al activarlo
        // 903's module off: the borrowed hero stays, what was recorded is kept on save and comes back when it is enabled
        activos.Remove(903);
        string leido = SaneadoSaves.HeroesDesdeDisco(d, tabla, reg);
        string guardado = SaneadoSaves.HeroesParaDisco(leido, tabla, reg);
        bool conserva = reg.HeroeDe(Program.R(t, "heroe"), out int realC, out _, out _) && realC == 903;
        activos.Add(903);
        comprobar(Campo(leido, "heroID") == 7 && Campo(leido, "enemyWarlordHeroID") == 901 && guardado == d && conserva
                  && Campo(SaneadoSaves.HeroesDesdeDisco(guardado, tabla, reg), "heroID") == 903,
            "héroes: con el módulo apagado la partida sigue con el prestado (7), se conserva lo apuntado y al activarlo vuelve el 903");

        // Save de antes de la 0.7.1 con el 903 en el disco y su módulo apagado: se carga como 7 y se apunta
        // Save from before 0.7.1 with 903 on disk and its module off: it is loaded as 7 and recorded
        var reg2 = new RegistroGuardias(null);
        activos.Remove(903);
        string viejo = SaneadoSaves.HeroesDesdeDisco(Campana(903, 0, "A"), tabla, reg2);
        bool apuntadoViejo = reg2.HeroeDe(Program.R(t, "heroe"), out int realV, out int escV, out string firmaV) && realV == 903 && escV == 7 && firmaV == "A";
        activos.Add(903);
        comprobar(Campo(viejo, "heroID") == 7 && apuntadoViejo && Campo(SaneadoSaves.HeroesDesdeDisco(viejo, tabla, reg2), "heroID") == 903,
            "héroes: un 903 en el disco con su módulo apagado se carga como su prestado (7), se apunta y vuelve al activar el módulo");

        // Instantánea: ranura aparte (no pisa la del save normal)
        // Snapshot: separate slot (does not overwrite the normal save's)
        string inst = SaneadoSaves.HeroesParaDisco(Campana(901, 0, "A", instantanea: true), tabla, reg);
        comprobar(Campo(inst, "heroID") == 6 && reg.HeroeDe(Program.R(Campana(901, 0, "A", instantanea: true), "heroe"), out int realI, out _, out _) && realI == 901
                  && reg.HeroeDe(Program.R(t, "heroe"), out int realN, out _, out _) && realN == 903,
            "héroes: la instantánea del nodo se apunta en su propia ranura (901) y el save normal sigue con el 903");

        // Save del jugador (0.7.2): el historial con «heroID» no se toca, pero «lastHeroID» se guarda como su prestado
        // Player save (0.7.2): the history with «heroID» is left untouched, but «lastHeroID» is saved as its borrowed hero
        string jugador = "{\n    \"lastHeroID\": 903,\n    \"runHistory\": [ { \"heroID\": 903, \"warlord\": 0 } ]\n}";
        string jugadorDisco = SaneadoSaves.HeroesParaDisco(jugador, tabla, reg);
        comprobar(Campo(jugadorDisco, "lastHeroID") == 7 && Campo(jugadorDisco, "heroID") == 903
                  && SaneadoSaves.ContarResiduosHeroes(jugador, tabla) == 1 && SaneadoSaves.ContarResiduosHeroes(jugadorDisco, tabla) == 0
                  && reg.HeroeDe(SaneadoSaves.RanuraUltimoHeroe, out int realU, out int escU, out string firmaU) && realU == 903 && escU == 7 && firmaU == null,
            "héroes: lastHeroID 903 se guarda como 7 (apuntado sin firma) y el historial sigue con su heroID 903");
        comprobar(SaneadoSaves.HeroesDesdeDisco(jugadorDisco, tabla, reg) == jugador,
            "héroes: al leer el save del jugador, lastHeroID vuelve a 903 (texto idéntico)");
        comprobar(SaneadoSaves.HeroesParaDisco(jugadorDisco, tabla, reg) == jugadorDisco && reg.HeroeDe(SaneadoSaves.RanuraUltimoHeroe, out int realU2, out _, out _) && realU2 == 903,
            "héroes: un save del jugador ya saneado que vuelve a pasar no olvida lo apuntado");
        string otroHeroe = jugador.Replace("\"lastHeroID\": 903", "\"lastHeroID\": 3");
        SaneadoSaves.HeroesParaDisco(otroHeroe, tabla, reg);
        comprobar(!reg.HeroeDe(SaneadoSaves.RanuraUltimoHeroe, out _, out _, out _),
            "héroes: si el jugador empieza con otro héroe de serie, se olvida el último héroe apuntado");
        // Módulo apagado: un lastHeroID 9xx que llega del disco se carga como su prestado y se apunta
        // Module off: a lastHeroID 9xx coming from disk is loaded as its borrowed hero and recorded
        var regU = new RegistroGuardias(null);
        var tablaApagada = new TablaHeroes(new Dictionary<int, int> { { 901, 6 }, { 903, 7 } }) { Disponible = id => false };
        string apagado = SaneadoSaves.HeroesDesdeDisco(jugador, tablaApagada, regU);
        comprobar(Campo(apagado, "lastHeroID") == 7 && regU.HeroeDe(SaneadoSaves.RanuraUltimoHeroe, out int realA, out _, out _) && realA == 903
                  && Campo(SaneadoSaves.HeroesDesdeDisco(apagado, tabla, regU), "lastHeroID") == 903,
            "héroes: con el módulo apagado lastHeroID 903 se lee como 7 y vuelve a 903 al activarlo");

        // Partida nueva con un héroe de serie: no tiene nada apuntado y (0.7.5) NO borra lo de la otra partida (la principal y
        // la diaria pueden estar a medias a la vez)
        // New run with a stock hero: nothing is recorded and (0.7.5) it does NOT erase the data of the other run (the main run and
        // the daily run can both be in progress at the same time)
        SaneadoSaves.HeroesParaDisco(Campana(3, 0, "C"), tabla, reg);
        comprobar(!reg.HeroeDe(Program.R(Campana(3, 0, "C"), "heroe"), out _, out _, out _) && !reg.HeroeDe(Program.R(Campana(3, 0, "C"), "senor"), out _, out _, out _)
                  && reg.HeroeDe(Program.R(t, "heroe"), out int realA2, out _, out _) && realA2 == 903,
            "héroes: una partida nueva con héroe de serie no tiene nada apuntado y no olvida lo de la otra partida (0.7.5)");

        // Partida antigua sin runId: la semilla hace de firma
        // Old run without runId: the seed acts as the signature
        var reg3 = new RegistroGuardias(null);
        string sinRun = SaneadoSaves.HeroesParaDisco(Campana(903, 0, null), tabla, reg3);
        comprobar(reg3.HeroeDe(Program.R(Campana(903, 0, null), "heroe"), out _, out _, out string firmaS) && firmaS == "semilla:123" && Campo(SaneadoSaves.HeroesDesdeDisco(sinRun, tabla, reg3), "heroID") == 903,
            "héroes: una partida antigua sin runId usa la semilla como firma y también vuelve");

        // Sin tabla (ningún módulo con héroes instalado): no se toca
        // No table (no module with heroes installed): left untouched
        comprobar(ReferenceEquals(SaneadoSaves.HeroesParaDisco(t, null, reg), t) && ReferenceEquals(SaneadoSaves.HeroesDesdeDisco(t, null, reg), t),
            "héroes: sin tabla de héroes no se toca el texto");

        // Registro en disco: lo apuntado sobrevive a guardar y volver a cargar
        // Registry on disk: what was recorded survives saving and loading again
        string ruta = Path.Combine(Path.GetTempPath(), "zero_registro_heroes_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var enDisco = new RegistroGuardias(ruta);
            SaneadoSaves.HeroesParaDisco(t, tabla, enDisco);
            var recargado = new RegistroGuardias(ruta);
            recargado.Cargar();
            comprobar(recargado.HeroeDe(Program.R(t, "heroe"), out int realR, out int escR, out string firmaR) && realR == 903 && escR == 7 && firmaR == "A",
                "héroes: lo apuntado se guarda en el fichero del registro y se lee igual");
        }
        finally { try { File.Delete(ruta); } catch { } }

        // Save real (si se pasó la carpeta): con el 903 inyectado, ida sin residuos y vuelta idéntica
        // Real save (if the folder was passed): with 903 injected, round trip out with no leftovers and back identical
        if (!string.IsNullOrEmpty(campanaReal) && SaneadoSaves.EsPartida(campanaReal))
        {
            var reg4 = new RegistroGuardias(null);
            string conNuevo = new Regex("(\"heroID\"\\s*:\\s*)(\\d+)").Replace(campanaReal, m => m.Groups[1].Value + "903", 1);
            string ida = SaneadoSaves.HeroesParaDisco(conNuevo, tabla, reg4);
            comprobar(SaneadoSaves.ContarResiduosHeroes(ida, tabla) == 0 && Campo(ida, "heroID") == 7 && SaneadoSaves.HeroesDesdeDisco(ida, tabla, reg4) == conNuevo,
                "héroes: save de campaña real con el 903: ida sin residuos y vuelta idéntica");
        }
    }

    // ---------------------------------------------------------------
    // Marcas de objetos ancladas a la partida
    // Gear marks anchored to the run
    // ---------------------------------------------------------------
    private static void PruebaAnclaObjetos(Action<bool, string> comprobar)
    {
        var deSerie = Enumerable.Range(1, 40).ToList();
        var tabla = new TablaObjetos(new Dictionary<int, int> { { 700, 3 } }, deSerie);      // módulo con el 700 activo
                                                                                             // module with 700 active
        var otraTabla = new TablaObjetos(new Dictionary<int, int> { { 710, 4 } }, deSerie);  // el del 700 apagado, otro activo
                                                                                             // the one with 700 off, another active
        var reg = new RegistroGuardias(null);

        // Partida A: el 700 en la posición 1 del Gear y en vendidos
        // Run A: 700 at position 1 of the Gear and in sold items
        string a = Campana(3, 0, "A", gear: "5,700,1", vendidos: "700");
        string da = SaneadoObjetos.ParaDisco(a, tabla, reg, true);
        comprobar(SaneadoObjetos.ContarResiduos(da, tabla) == 0 && reg.PartidaObjetos(Program.R(a, "Gear")) == "A" && reg.PartidaObjetos(Program.R(a, "SoldGear")) == "A",
            "objetos: lo apuntado del Gear y de los vendidos se ancla a la partida (runId A)");
        comprobar(SaneadoObjetos.DesdeDisco(da, tabla, reg) == a, "objetos: en la misma partida vuelven el 700 del Gear y el de vendidos (texto idéntico)");

        // Otra partida B con el mismo objeto escrito en la misma posición: no se restaura
        // Another run B with the same item written at the same position: not restored
        string db = da.Replace("\"runId\": \"A\"", "\"runId\": \"B\"");
        comprobar(SaneadoObjetos.DesdeDisco(db, tabla, reg) == db, "objetos: en otra partida no se restauran las marcas de la anterior (Gear ni vendidos)");

        // Módulo del 700 apagado, misma partida: lo apuntado se conserva al guardar (otra sesión del juego: sin la memoria de
        // textos saneados de esta, que devolvería el texto sin mirar nada)
        // 700 module off, same run: what was recorded is kept on save (another game session: without this one's memory of
        // sanitized texts, which would return the text without checking anything)
        reg.OlvidarSalidas();
        SaneadoObjetos.ParaDisco(da, otraTabla, reg, true);
        comprobar(reg.ObjetosPosicion(Program.R(a, "Gear")).Count == 1 && reg.ObjetosLista(Program.R(a, "SoldGear")).Contains(700),
            "objetos: con su módulo apagado, en la misma partida, lo apuntado se conserva al guardar");

        // Módulo apagado y partida nueva: la nueva no recibe nada (sin objetos fantasma) y (0.7.5) lo de la partida A se conserva
        // Module off and new run: the new one receives nothing (no ghost gear) and (0.7.5) the data of run A is kept
        SaneadoObjetos.ParaDisco(db, otraTabla, reg, true);
        comprobar(reg.ObjetosPosicion(Program.R(db, "Gear")).Count == 0 && reg.ObjetosLista(Program.R(db, "SoldGear")).Count == 0 && reg.PartidaObjetos(Program.R(db, "Gear")) == null
                  && reg.ObjetosPosicion(Program.R(a, "Gear")).Count == 1 && SaneadoObjetos.DesdeDisco(db, tabla, reg) == db,
            "objetos: en una partida nueva no aparecen las marcas de otra (sin objetos fantasma) y las de la otra partida se conservan (0.7.5)");

        // Registro de la 0.7.0 (sin ancla): se acepta como antes
        // 0.7.0 registry (no anchor): accepted as before
        var viejo = new RegistroGuardias(null);
        viejo.FijarObjetosPosicion("Gear", new Dictionary<int, (int, int)> { { 1, (700, 2) } });
        string conDos = Campana(3, 0, "A", gear: "5,2,1");
        comprobar(Regex.IsMatch(SaneadoObjetos.DesdeDisco(conDos, tabla, viejo), "\"Gear\": \\[5,700,1\\]"),
            "objetos: lo apuntado sin partida (registro de la 0.7.0) se sigue restaurando");

        // Listas de colección (save del jugador, sin partida): valen para todas las partidas
        // Collection lists (player save, no run): valid for all runs
        var regC = new RegistroGuardias(null);
        string jugador = "{\n    \"gearIdsCollected\": [1,700]\n}";
        string dj = SaneadoObjetos.ParaDisco(jugador, tabla, regC, true);
        comprobar(!dj.Contains("700") && SaneadoObjetos.DesdeDisco(dj, tabla, regC).Contains("700"),
            "objetos: las listas de colección del save del jugador no dependen de la partida");
    }

    // ---------------------------------------------------------------
    // Stats a distancia: manda el mayor
    // Ranged stats: the higher one wins
    // ---------------------------------------------------------------
    private static void PruebaDistancia(Action<bool, string> comprobar)
    {
        ModuloEncontrado Mod(string id, int alcance, int precision, int dano) => new ModuloEncontrado
        {
            Declaracion = new DeclaracionModulo { Id = id, Efectos = new EfectosModulo { DistanciaJugador = new EfectoDistancia { Alcance = alcance, Precision = precision, Dano = dano } } },
            Carpeta = Path.GetTempPath(), NombreCarpeta = id, Activo = true,
        };
        var grande = Mod("Grande", 10, 10, 3);
        var pequeno = Mod("Pequeno", 5, 5, 1);
        var r1 = Modulos.Resolver(new[] { grande, pequeno }, "0.7.1");
        var r2 = Modulos.Resolver(new[] { pequeno, grande }, "0.7.1");
        comprobar(r1.Distancia?.Alcance == 10 && r2.Distancia?.Alcance == 10 && r2.Distancia.Dano == 3,
            "distancia: con dos módulos manda el mayor (suma de los bonus), en cualquier orden");
        comprobar(r2.Avisos.Any(a => a.Contains("manda el mayor")), "distancia: con varios módulos se avisa de que manda el mayor");
        var igualA = Mod("IgualA", 10, 0, 0);
        var igualB = Mod("IgualB", 0, 10, 0);
        var r3 = Modulos.Resolver(new[] { igualA, igualB }, "0.7.1");
        comprobar(r3.Distancia?.Alcance == 10 && r3.Distancia.Precision == 0, "distancia: a igualdad de suma, manda el primero");
    }
}
