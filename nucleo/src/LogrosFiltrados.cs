// LogrosFiltrados.cs
// Logros de Steam de serie solo con contenido de serie (decisión de Zero, 17-10-2026; 0.7.2). Las unidades 9xx (también
// las «sueltas» que van en las listas de razas de serie) y los héroes 9xx (901-904) ni suman ni bloquean los logros que
// recorren unidades o héroes. La lógica de cada recuento está en LogicaLogros.cs (probada fuera del juego).
// Qué toca (firmas y umbrales comprobados en el interop y en el binario del juego instalado, 17-10-2026):
//   1) COLECCIONES (las 9xx BLOQUEAN): SaveDataHandler.EvaluateRaceCollection(Race) (descompilado
//      Map/SaveData/SaveDataHandler.cs:1548-1565) pide todas las unidades de GetUnitsOfRace(raza), sueltas incluidas, y
//      AllRaceCollectionsComplete (:1569-1580, «Grand Collector») igual. Un postfix recalcula con la lista sin 9xx y, si
//      está completa, concede el logro de la raza y, si las 8 razas lo están, «Grand Collector». Las razas 40/41 no tienen
//      logro (no están en SaveDataHandler.RaceCollectionAchievements) y no se tocan.
//   2) LOGROS QUE LAS 9xx SUMAN: un prefix de TJ.Achievements.PlayerAchievements.Unlock(AchievementId) (hasta el juego
//      1.10.17 era Memori.Steamworks.SteamAchievements.Unlock; la 1.11.1 la quitó) recuenta sin 9xx y
//      no deja pasar el logro si solo llega a su umbral gracias a lo nuevo:
//        - «Max Difficulty All Heroes» (CampaignSaveManager.CheckGodkingCompletionAchievements, :2489-2508; 16 héroes en
//          Godking) y «Roster Complete» (SaveDataHandler.ApplyVictoryUnlocks, :1754-1762; 16 héroes con victoria). Para
//          Roster Complete se usa el save que está escribiendo ApplyVictoryUnlocks (aún no está en disco).
//        - «Four Unique Factions» (CampaignSaveManager.CheckForFourFactions, :2510-2525; 4 razas en el ejército).
//        - «Grand Alliance», «Legends Assembled» y «Master of Arms» (AchievementRules.ForArmy, llamado desde
//          CampaignSaveManager.EvaluateArmyRunStats, :2333-2363): razas, legendarias y rasgos de prestigio del ejército.
//      Como el filtro está en Unlock, vale también para los logros diferidos que AchievementSync reenvía al volver Steam.
//   3) LOGRO DE OBJETOS (defensa): SaveDataHandler.EvaluateGearCollection cuenta GearData.GetGearIDs(); si alguna pantalla
//      tuviera abierta la lista de gear ampliada (ArmeriaObjetos) mientras se evalúa, pediría también los 7xx. Mientras
//      corre se suspende ese contexto, así el logro de objetos sigue solo con los de serie (40 hasta el juego 1.10.17, 56
//      en el enum de la 1.11.1).
//   4) JUEGO 1.11.1 (revisión del 01-11-2026), LOGROS DE EJÉRCITO SIN CONTEXTO: FourUniqueFactionsRun, GrandAlliance,
//      LegendsAssembled y MasterOfArms fuera de su contexto de recuento, y HiredSwords, GoblinHorde, KoboldSwarm y
//      ShieldSisters (AchievementRules.ForFinalArmy, que llaman ForBattle y AchievementSync.AwardFromRunHistory con todo el
//      historial) no se pueden recontar sin 9xx (los saves guardan las 9xx como su base). 0.7.5 (revisión total del 02-11):
//      antes se bloqueaban mientras el historial guardara UNA partida con contenido nuevo (en la práctica, para siempre). Ahora:
//        - desde el historial (AwardFromRunHistory), se recuenta POR PARTIDA con las reglas del propio juego: un prefix de
//          AchievementRules.FinalBattleArmy apunta qué partida se está mirando y un postfix de ForFinalArmy apunta qué logros
//          da cada una. Se concede un logro si lo da alguna partida de serie; si solo lo dan partidas con contenido nuevo,
//          no. Si esos dos enganches no saltan (inlining en otra versión), vuelve la regla vieja (bloquear si el historial
//          tiene contenido nuevo);
//        - fuera de contexto (en batalla o diferidos), se bloquean si la sesión tiene módulos de Zero (aplicados o activos
//          en el menú) o si la partida en curso del slot activo está anclada a los módulos (si no se sabe el slot, cualquiera
//          de las partidas en curso). El historial ya no cuenta ahí;
//        - el reenvío de los ya ganados (AchievementSync.SendEarned, que vuelve a llamar a Unlock con los de
//          earnedAchievements no confirmados por Steam) los deja pasar. Premisa: earnedAchievements solo se llena desde
//          PlayerAchievements.Unlock (que invoca su aviso antes de la tienda de Steam), así que lo bloqueado por este filtro
//          nunca llega ahí. Desensamblado de SendEarned (notas internas (no publicadas)): IsUnlocked y Unlock.
//      Ante la duda, bloquear.
//   5) 0.7.4, HECHIZOS PROPIOS (los 1900-1999 BLOQUEAN): «Grand Grimoire». En el juego 1.11.1 la lógica está en
//      TJ.Spells.SpellCastRecord (interop: QuestSpells, CastAll, CastCount, Remaining y BuildRemainingTooltip, que pinta la
//      fila de la misión en Registros); el código público (CastEveryRegisteredSpell sobre SpellRegistry.All) es anterior.
//      Si QuestSpells sale del registro, con los hechizos propios registrados pediría también los nuestros (y la fila de la
//      misión los enseñaría como pendientes). Revisión de la 0.7.4 (ronda 2):
//        - postfix de SpellCastRecord.get_QuestSpells y de Remaining: quitan los 19xx de la lista (misión, progreso y
//          tooltip). Si alguno sale copiado dentro de otro método (inlining), queda la red de abajo;
//        - postfix de BattleAchievements.Evaluate (pública; llama a RecordLifetime): solo si la batalla cuenta para logros
//          (BattleAchievements.BattleCounts) y el registro lleva propios, mira SpellCastRecord.Remaining(spellsEverCast)
//          sin los 19xx y, si no queda ninguno de serie, concede el logro. Nunca lo concede de más: usa la misma lista que
//          el juego. Es el único logro que recorre los hechizos.
// NO se filtran (se documenta en el README): el resto de logros de batalla (AchievementRules.ForBattle: Circle of Magi,
// Pantheon, Know Thy Enemy...), porque quitar escuadras 9xx de una batalla cambia el sentido de sus reglas «todas las
// escuadras...», y las marcas de partida que el juego apunta sobre la marcha (sin arqueros, sin magos, sin repetidas...).
// Los cuatro de ForFinalArmy (Hired Swords, Goblin Horde, Kobold Swarm, Shield Sisters) sí, por el punto 4.
// Instancia de Harmony propia (Plugin.cs), con cualquier módulo instalado: los 9xx pueden estar en el save de partidas
// anteriores aunque hoy su módulo esté apagado. Interruptor: Logros.Filtrar. Todo falla en blando: si un recuento falla,
// decide el juego como siempre.
// Autor: ZeroitMods.
// LogrosFiltrados.cs
// Stock Steam achievements with stock content only (Zero's decision, 17-10-2026; 0.7.2). 9xx units (including
// the "loose" ones in the stock race lists) and 9xx heroes (901-904) neither count toward nor block the achievements that
// iterate over units or heroes. The logic of each count is in LogicaLogros.cs (tested outside the game).
// What it touches (signatures and thresholds checked in the interop and in the installed game binary, 17-10-2026):
//   1) COLLECTIONS (the 9xx BLOCK): SaveDataHandler.EvaluateRaceCollection(Race) (decompiled
//      Map/SaveData/SaveDataHandler.cs:1548-1565) asks for all the units from GetUnitsOfRace(race), loose ones included, and
//      AllRaceCollectionsComplete (:1569-1580, "Grand Collector") does the same. A postfix recalculates with the list without 9xx and, if
//      it is complete, grants the race achievement and, if all 8 races are complete, "Grand Collector". Races 40/41 have no
//      achievement (they are not in SaveDataHandler.RaceCollectionAchievements) and are left untouched.
//   2) ACHIEVEMENTS THAT THE 9xx COUNT TOWARD: a prefix of TJ.Achievements.PlayerAchievements.Unlock(AchievementId) (up to game
//      1.10.17 it was Memori.Steamworks.SteamAchievements.Unlock; 1.11.1 removed it) recounts without 9xx and
//      does not let the achievement through if it only reaches its threshold thanks to the new content:
//        - "Max Difficulty All Heroes" (CampaignSaveManager.CheckGodkingCompletionAchievements, :2489-2508; 16 heroes on
//          Godking) and "Roster Complete" (SaveDataHandler.ApplyVictoryUnlocks, :1754-1762; 16 heroes with a victory). For
//          Roster Complete, the save that ApplyVictoryUnlocks is writing is used (it is not on disk yet).
//        - "Four Unique Factions" (CampaignSaveManager.CheckForFourFactions, :2510-2525; 4 races in the army).
//        - "Grand Alliance", "Legends Assembled" and "Master of Arms" (AchievementRules.ForArmy, called from
//          CampaignSaveManager.EvaluateArmyRunStats, :2333-2363): the army's races, legendaries and prestige traits.
//      Since the filter is in Unlock, it also applies to the deferred achievements that AchievementSync resends when Steam comes back.
//   3) GEAR ACHIEVEMENT (defensive): SaveDataHandler.EvaluateGearCollection counts GearData.GetGearIDs(); if any screen
//      had the expanded gear list (ArmeriaObjetos) open while it is being evaluated, it would also ask for the 7xx. While it
//      runs, that context is suspended, so the gear achievement keeps using only the stock ones (40 up to game 1.10.17, 56
//      in the 1.11.1 enum).
//   4) GAME 1.11.1 (review of 01-11-2026), ARMY ACHIEVEMENTS WITHOUT CONTEXT: FourUniqueFactionsRun, GrandAlliance,
//      LegendsAssembled and MasterOfArms outside their counting context, and HiredSwords, GoblinHorde, KoboldSwarm and
//      ShieldSisters (AchievementRules.ForFinalArmy, which call ForBattle and AchievementSync.AwardFromRunHistory with the whole
//      history) cannot be recounted without 9xx (the saves store the 9xx as their base). 0.7.5 (full review of 02-11):
//      previously they were blocked as long as the history kept ONE run with new content (in practice, forever). Now:
//        - from the history (AwardFromRunHistory), it recounts PER RUN using the game's own rules: a prefix of
//          AchievementRules.FinalBattleArmy records which run is being looked at and a postfix of ForFinalArmy records which achievements
//          each one grants. An achievement is granted if any stock run gives it; if only runs with new content give it,
//          it is not. If those two hooks do not fire (inlining in another version), the old rule returns (block if the history
//          has new content);
//        - outside context (in battle or deferred), they are blocked if the session has Zero modules (applied or active
//          in the menu) or if the current run in the active slot is anchored to the modules (if the slot is unknown, any
//          of the current runs). The history no longer counts there;
//        - the resend of those already earned (AchievementSync.SendEarned, which calls Unlock again with the
//          earnedAchievements not confirmed by Steam) lets them through. Premise: earnedAchievements is only filled from
//          PlayerAchievements.Unlock (which invokes its notice before the Steam store), so anything blocked by this filter
//          never gets there. Disassembly of SendEarned (internal notes (unpublished)): IsUnlocked and Unlock.
//      When in doubt, block.
//   5) 0.7.4, CUSTOM SPELLS (the 1900-1999 BLOCK): "Grand Grimoire". In game 1.11.1 the logic is in
//      TJ.Spells.SpellCastRecord (interop: QuestSpells, CastAll, CastCount, Remaining and BuildRemainingTooltip, which draws the
//      quest row in Records); the public code (CastEveryRegisteredSpell over SpellRegistry.All) is older.
//      If QuestSpells comes from the registry, with the custom spells registered it would also ask for ours (and the quest row
//      would show them as pending). Review of 0.7.4 (round 2):
//        - postfix of SpellCastRecord.get_QuestSpells and of Remaining: they remove the 19xx from the list (quest, progress and
//          tooltip). If any ends up copied inside another method (inlining), the safety net below remains;
//        - postfix of BattleAchievements.Evaluate (public; calls RecordLifetime): only if the battle counts for achievements
//          (BattleAchievements.BattleCounts) and the registry holds custom ones, it checks SpellCastRecord.Remaining(spellsEverCast)
//          without the 19xx and, if none of the stock ones remain, grants the achievement. It never over-grants it: it uses the same list as
//          the game. It is the only achievement that iterates over the spells.
// NOT filtered (documented in the README): the rest of the battle achievements (AchievementRules.ForBattle: Circle of Magi,
// Pantheon, Know Thy Enemy...), because removing 9xx squads from a battle changes the meaning of their "all
// squads..." rules, and the run flags that the game records on the fly (no archers, no mages, no repeats...).
// The four from ForFinalArmy (Hired Swords, Goblin Horde, Kobold Swarm, Shield Sisters) are, per point 4.
// Own Harmony instance (Plugin.cs), with any module installed: the 9xx may be in the save from earlier runs
// even if their module is turned off today. Switch: Logros.Filtrar. Everything fails soft: if a count fails,
// the game decides as usual.
// Author: ZeroitMods.

using System;
using System.Collections.Generic;
using Memori.SaveData;
using TJ;
// Juego 1.11.1: Memori.Steamworks.SteamAchievements desapareció; los logros van por TJ.Achievements (PlayerAchievements,
// AchievementId) en TabletopTavern.Platform.dll
// Game 1.11.1: Memori.Steamworks.SteamAchievements is gone; achievements now go through TJ.Achievements (PlayerAchievements,
// AchievementId) in TabletopTavern.Platform.dll
using TJ.Achievements;

namespace ZeroNucleo
{
    // -----------------------------------------------------------------------
    // Sección: parches de los logros filtrados
    // Section: patches for the filtered achievements
    // -----------------------------------------------------------------------
    public static class LogrosFiltrados
    {
        private static readonly HashSet<string> _avisos = new HashSet<string>();

        // Contextos abiertos por los prefix de los métodos que conceden los logros (solo hilo principal)
        // Contexts opened by the prefixes of the methods that grant the achievements (main thread only)
        private static PlayerSaveData _saveVictoria;           // save que está escribiendo ApplyVictoryUnlocks
                                                               // save being written by ApplyVictoryUnlocks
        private static CampaignSaveManager _gestorFacciones;   // CheckForFourFactions en curso
                                                               // CheckForFourFactions in progress
        private static CampaignSaveManager _gestorEjercito;    // EvaluateArmyRunStats en curso
                                                               // EvaluateArmyRunStats in progress
        // Valores anteriores de cada contexto (si una llamada se anidara, el finalizer devuelve el de fuera, no null)
        // Previous values of each context (if a call were nested, the finalizer restores the outer one, not null)
        private static readonly Stack<PlayerSaveData> _antesVictoria = new Stack<PlayerSaveData>();
        private static readonly Stack<CampaignSaveManager> _antesFacciones = new Stack<CampaignSaveManager>();
        private static readonly Stack<CampaignSaveManager> _antesEjercito = new Stack<CampaignSaveManager>();
        private static readonly Stack<int> _suspendidoGear = new Stack<int>();   // contadores de lista ampliada guardados
                                                                                 // saved extended-list counters

        // ¿Está activo el filtrado? (interruptor de la configuración)
        // Is filtering active? (config switch)
        private static bool Activo => Plugin.CfgLogrosFiltrar == null || Plugin.CfgLogrosFiltrar.Value;

        /// <summary>
        /// Lista de parches para Plugin.cs: (nombre, tipo, método, argumentos, prefix, postfix, finalizer). nameof comprueba al
        /// compilar que los métodos existen en el interop del juego instalado.
        /// Patch list for Plugin.cs: (name, type, method, arguments, prefix, postfix, finalizer). nameof checks at
        /// compile time that the methods exist in the installed game's interop.
        /// </summary>
        // Juego 1.11.1: tipo y argumentos DIFERIDOS (Func): cada «() => typeof(X)» se evalúa dentro del try de
        // Plugin.Parchear, así que un tipo que el juego quite o mueva solo hace fallar su parche, no la lista ni Load().
        // Game 1.11.1: DEFERRED type and arguments (Func): each «() => typeof(X)» is evaluated inside the try of
        // Plugin.Parchear, so a type that the game removes or moves only makes its patch fail, not the list or Load().
        public static List<(string nombre, Func<Type> tipo, string metodo, Func<Type[]> args, string prefix, string postfix, string finalizer)> Parches()
        {
            Func<Type> tS = () => typeof(SaveDataHandler);
            Func<Type> tC = () => typeof(CampaignSaveManager);
            return new List<(string, Func<Type>, string, Func<Type[]>, string, string, string)>
            {
                // Juego 1.11.1: SteamAchievements.Unlock pasó a PlayerAchievements.Unlock (misma firma con el AchievementId de
                // TJ.Achievements). Comprobado en el binario: CheckForFourFactions, EvaluateRaceCollection, ApplyVictoryUnlocks,
                // CheckGodkingCompletionAchievements, EvaluateArmyRunStats y EvaluateGearCollection la llaman directamente (call),
                // no va copiada dentro (sin inlining)
                // Game 1.11.1: SteamAchievements.Unlock became PlayerAchievements.Unlock (same signature with the AchievementId from
                // TJ.Achievements). Verified in the binary: CheckForFourFactions, EvaluateRaceCollection, ApplyVictoryUnlocks,
                // CheckGodkingCompletionAchievements, EvaluateArmyRunStats and EvaluateGearCollection call it directly (call),
                // it is not inlined (no inlining)
                ("Logros: filtro de los que suman con contenido nuevo (PlayerAchievements.Unlock)", () => typeof(PlayerAchievements), nameof(PlayerAchievements.Unlock),
                    () => new[] { typeof(AchievementId) }, nameof(AntesDeUnlock), null, null),
                ("Logros: colección de raza sin unidades nuevas (SaveDataHandler.EvaluateRaceCollection)", tS, nameof(SaveDataHandler.EvaluateRaceCollection),
                    () => new[] { typeof(Race) }, null, nameof(TrasEvaluateRaceCollection), null),
                ("Logros: save de la victoria para Roster Complete (SaveDataHandler.ApplyVictoryUnlocks)", tS, "ApplyVictoryUnlocks",
                    null, nameof(AntesDeApplyVictoryUnlocks), null, nameof(FinApplyVictoryUnlocks)),
                ("Logros: ejército de Four Unique Factions (CampaignSaveManager.CheckForFourFactions)", tC, nameof(CampaignSaveManager.CheckForFourFactions),
                    () => Type.EmptyTypes, nameof(AntesDeCheckForFourFactions), null, nameof(FinCheckForFourFactions)),
                ("Logros: ejército de Grand Alliance y compañía (CampaignSaveManager.EvaluateArmyRunStats)", tC, nameof(CampaignSaveManager.EvaluateArmyRunStats),
                    () => Type.EmptyTypes, nameof(AntesDeEvaluateArmyRunStats), null, nameof(FinEvaluateArmyRunStats)),
                ("Logros: objetos solo de serie (SaveDataHandler.EvaluateGearCollection)", tS, nameof(SaveDataHandler.EvaluateGearCollection),
                    () => Type.EmptyTypes, nameof(AntesDeEvaluateGearCollection), null, nameof(FinEvaluateGearCollection)),
                // Juego 1.11.1 (revisión del 01-11-2026): AchievementSync.AwardFromRunHistory(PlayerSaveData) recorre TODO el
                // historial de partidas (runHistory) con AchievementRules.FinalBattleArmy + ForFinalArmy y concede HiredSwords,
                // GoblinHorde, KoboldSwarm y ShieldSisters (y AllWeathers por el tiempo). Comprobado en el binario: no llama a
                // ForArmy; solo lo llama OnStatsReceived (call, sin inlining). El contexto deja decidir con el save que recorre
                // Game 1.11.1 (review of 01-11-2026): AchievementSync.AwardFromRunHistory(PlayerSaveData) goes through the ENTIRE
                // run history (runHistory) with AchievementRules.FinalBattleArmy + ForFinalArmy and grants HiredSwords,
                // GoblinHorde, KoboldSwarm and ShieldSisters (and AllWeathers for the time). Verified in the binary: it does not call
                // ForArmy; only OnStatsReceived calls it (call, no inlining). The context lets the patch decide using the save it iterates
                ("Logros: historial de partidas (AchievementSync.AwardFromRunHistory)", () => typeof(AchievementSync), nameof(AchievementSync.AwardFromRunHistory),
                    null, nameof(AntesDeAwardFromRunHistory), null, nameof(FinAwardFromRunHistory)),
                // 0.7.5: recuento por partida dentro de AwardFromRunHistory (las dos llamadas son directas en el binario 1.11.1,
                // notas internas (no publicadas): «call AchievementRules::FinalBattleArmy» y «call ...::ForFinalArmy»)
                // 0.7.5: per-run count inside AwardFromRunHistory (both calls are direct in the 1.11.1 binary,
                // internal notes (unpublished): «call AchievementRules::FinalBattleArmy» and «call ...::ForFinalArmy»)
                ("Logros: partida del historial que se recuenta (AchievementRules.FinalBattleArmy)", () => typeof(AchievementRules), nameof(AchievementRules.FinalBattleArmy),
                    null, nameof(AntesDeFinalBattleArmy), null, null),
                ("Logros: logros que da cada partida del historial (AchievementRules.ForFinalArmy)", () => typeof(AchievementRules), nameof(AchievementRules.ForFinalArmy),
                    null, null, nameof(TrasForFinalArmy), null),
                // 0.7.5: reenvío de los ya ganados (earnedAchievements) cuando Steam vuelve
                // 0.7.5: resend of already earned achievements (earnedAchievements) when Steam comes back
                ("Logros: reenvío de los ya ganados (AchievementSync.SendEarned)", () => typeof(AchievementSync), nameof(AchievementSync.SendEarned),
                    null, nameof(AntesDeSendEarned), null, nameof(FinSendEarned)),
                // 0.7.4: «Grand Grimoire» contando solo los hechizos de serie (los propios 1900-1999 no bloquean)
                // 0.7.6 (Impertérritos): el prefix y el finalizer marcan «evaluando una batalla» para que AntesDeUnlock no conceda
                // logros de batalla con la moral del jugador cambiada
                // 0.7.4: «Grand Grimoire» counting only the base-game spells (custom 1900-1999 ones do not block)
                // 0.7.6 (Impertérritos): the prefix and the finalizer mark «evaluating a battle» so that AntesDeUnlock does not grant
                // battle achievements while the player's morale is changed
                ("Logros: Grand Grimoire sin hechizos propios (BattleAchievements.Evaluate)", () => typeof(BattleAchievements), nameof(BattleAchievements.Evaluate),
                    null, nameof(AntesDeEvaluateBatalla), nameof(TrasEvaluateBatalla), nameof(FinEvaluateBatalla)),
                // Ronda 2 de la revisión de la 0.7.4: lista de la misión y pendientes sin los hechizos propios (TJ.Spells.SpellCastRecord)
                // Round 2 of the 0.7.4 review: quest list and pending spells without the custom spells (TJ.Spells.SpellCastRecord)
                ("Logros: misión Grand Grimoire sin hechizos propios (SpellCastRecord.get_QuestSpells)", () => typeof(TJ.Spells.SpellCastRecord), "get_QuestSpells",
                    () => Type.EmptyTypes, null, nameof(TrasQuestSpells), null),
                ("Logros: pendientes de Grand Grimoire sin hechizos propios (SpellCastRecord.Remaining)", () => typeof(TJ.Spells.SpellCastRecord), nameof(TJ.Spells.SpellCastRecord.Remaining),
                    null, null, nameof(TrasRemaining), null),
            };
        }

        // Aviso una sola vez por clave y sesión (estos parches pueden correr a menudo)
        // Warn only once per key and session (these patches may run often)
        private static void AvisoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Aviso(texto);
        }

        // Información una sola vez por clave y sesión
        // Information only once per key and session
        private static void InfoUnaVez(string clave, string texto)
        {
            lock (_avisos) { if (!_avisos.Add(clave)) return; }
            Plugin.Info(texto);
        }

        // ---------------------------------------------------------------
        // 1) Colecciones de raza y «Grand Collector» (las 9xx bloquean)
        // 1) Race Collections and «Grand Collector» (the 9xx ones block)
        // ---------------------------------------------------------------
        /// <summary>
        /// Postfix de SaveDataHandler.EvaluateRaceCollection(Race): si la lista de la raza lleva unidades nuevas, recalcula
        /// sin ellas y, si está completa, concede el logro de la raza y, si las 8 razas lo están, «Grand Collector».
        /// Postfix of SaveDataHandler.EvaluateRaceCollection(Race): if the race's list contains new gear, recount
        /// without it and, if complete, grant the race achievement and, if all 8 races are complete, «Grand Collector».
        /// </summary>
        public static void TrasEvaluateRaceCollection(Race __0)
        {
            if (!Activo) return;
            try
            {
                var data = TabletopTavernData.InstanceIfExists;
                var logros = SaveDataHandler.RaceCollectionAchievements;
                if (data == null || logros == null || !logros.ContainsKey(__0)) return;
                var reclutadas = Reclutadas();
                if (reclutadas == null) return;

                int[] lista = Lista(data, __0);
                if (LogicaLogros.TieneUnidadesNuevas(lista) && LogicaLogros.ColeccionDeSerieCompleta(lista, reclutadas))
                {
                    var id = logros[__0];
                    InfoUnaVez("raza" + (int)__0, $"Logros: colección de {__0} completa con sus unidades de serie (sin contar las nuevas): se concede {id}.");
                    PlayerAchievements.Unlock(id);
                }

                // Grand Collector: solo si filtrar cambia algo (alguna raza con unidades nuevas) y las 8 están completas
                // Grand Collector: only if filtering changes something (some race with new gear) and all 8 are complete
                bool cambia = false, todas = true, alguna = false;
                foreach (Race r in Enum.GetValues(typeof(Race)))
                {
                    if (!logros.ContainsKey(r)) continue;
                    alguna = true;
                    int[] l = Lista(data, r);
                    if (LogicaLogros.TieneUnidadesNuevas(l)) cambia = true;
                    if (!LogicaLogros.ColeccionDeSerieCompleta(l, reclutadas)) { todas = false; break; }
                }
                if (alguna && todas && cambia)
                {
                    InfoUnaVez("grandcollector", "Logros: las colecciones de serie de todas las razas están completas (sin contar las unidades nuevas): se concede GrandCollector.");
                    PlayerAchievements.Unlock(AchievementId.GrandCollector);
                }
            }
            catch (Exception e) { AvisoUnaVez("coleccion", "Logros de colección (decide el juego): " + e.Message); }
        }

        // ---------------------------------------------------------------
        // 5) «Grand Grimoire» sin hechizos propios (0.7.4)
        // 5) «Grand Grimoire» without custom spells (0.7.4)
        // ---------------------------------------------------------------
        /// <summary>
        /// Postfix de BattleAchievements.Evaluate: si el registro lleva hechizos propios y el jugador ha lanzado todos los de
        /// serie (spellsEverCast), concede «Grand Grimoire» (el juego no lo haría porque también pide los propios).
        /// Postfix of BattleAchievements.Evaluate: if the registry contains custom spells and the player has cast all the base-game
        /// ones (spellsEverCast), grant «Grand Grimoire» (the game would not, since it also requires the custom ones).
        /// </summary>
        // 0.7.6: batallas que se están evaluando ahora (prefix suma, finalizer resta; anidadas, por si acaso)
        // 0.7.6: battles currently being evaluated (prefix adds, finalizer subtracts; nested, just in case)
        private static int _evaluandoBatalla;
        public static void AntesDeEvaluateBatalla() => _evaluandoBatalla++;
        public static Exception FinEvaluateBatalla(Exception __exception)
        {
            if (_evaluandoBatalla > 0) _evaluandoBatalla--;
            return __exception;
        }

        public static void TrasEvaluateBatalla()
        {
            if (!Activo) return;
            // 0.7.6: con «Impertérritos» aplicado no se concede Grand Grimoire desde aquí (batalla con la moral cambiada)
            // 0.7.6: with «Impertérritos» applied, Grand Grimoire is not granted from here (battle with changed morale)
            if (Nucleo.Real?.Moral != null) return;
            try
            {
                // Ronda 2: solo las batallas que cuentan para logros (las mismas en las que el juego apunta lanzamientos)
                // Round 2: only battles that count for achievements (the same ones in which the game records casts)
                if (!BatallaCuenta()) return;
                if (!LogicaHechizos.HayPropios(Hechizos.IdsRegistrados())) return;
                var jugador = SaveDataHandler.LoadPlayerSaveData();
                var lanzados = jugador?.spellsEverCast;
                if (lanzados == null) return;
                // Pendientes según el juego (SpellCastRecord.Remaining), sin los propios
                // Pending according to the game (SpellCastRecord.Remaining), without the custom ones
                var pendientes = PendientesDeSerie(lanzados);
                if (pendientes == null || pendientes.Count > 0) return;
                InfoUnaVez("grimorio", "Logros: todos los hechizos de serie lanzados (sin contar los propios de los héroes nuevos): se concede GrandGrimoire.");
                PlayerAchievements.Unlock(AchievementId.GrandGrimoire);
            }
            catch (Exception e) { AvisoUnaVez("grimorio", "Logro Grand Grimoire (decide el juego): " + e.Message); }
        }

        // ¿Cuenta esta batalla para logros? (BattleAchievements.BattleCounts). Aparte y sin inlining: si el juego quita la
        // propiedad, el fallo de JIT sale en esta llamada y lo recoge el try del llamador
        // Does this battle count for achievements? (BattleAchievements.BattleCounts). Separate and not inlined: if the game removes the
        // property, the JIT failure shows up in this call and the caller's try catches it
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool BatallaCuenta() => BattleAchievements.BattleCounts;

        /// <summary>
        /// Ids de los hechizos que el juego da por pendientes para Grand Grimoire (SpellCastRecord.Remaining) quitando los
        /// propios 19xx. null si no se puede saber. La usa también la autoprueba.
        /// Ids of the spells the game considers pending for Grand Grimoire (SpellCastRecord.Remaining) with the custom
        /// 19xx ones removed. null if it cannot be determined. The self-test also uses it.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static List<int> PendientesDeSerie(Il2CppSystem.Collections.Generic.List<TJ.Spells.Spell> lanzados)
        {
            var lista = TJ.Spells.SpellCastRecord.Remaining(lanzados.Cast<Il2CppSystem.Collections.Generic.ICollection<TJ.Spells.Spell>>());
            if (lista == null) return null;
            var ids = new List<int>();
            for (int i = 0; i < lista.Count; i++) if (lista[i] != null) ids.Add((int)lista[i].Spell);
            return LogicaHechizos.SinPropios(ids);
        }

        /// <summary>
        /// Postfix de SpellCastRecord.Remaining: quita de los pendientes los hechizos propios (tooltip y recuento). 0.7.5
        /// (hallazgo 32): devuelve una COPIA sin ellos, como TrasQuestSpells (la lista del juego podría ser compartida o del
        /// registro y no se toca).
        /// Postfix of SpellCastRecord.Remaining: removes the custom spells from the pending ones (tooltip and count). 0.7.5
        /// (finding 32): returns a COPY without them, like TrasQuestSpells (the game's list could be shared or belong to the
        /// registry and is not touched).
        /// </summary>
        public static void TrasRemaining(ref Il2CppSystem.Collections.Generic.List<TJ.Spells.SpellData> __result)
        {
            if (!Activo || __result == null) return;
            try
            {
                bool hay = false;
                for (int i = 0; i < __result.Count && !hay; i++) hay = __result[i] != null && LogicaHechizos.EsIdPropio((int)__result[i].Spell);
                if (!hay) return;
                var copia = new Il2CppSystem.Collections.Generic.List<TJ.Spells.SpellData>();
                for (int i = 0; i < __result.Count; i++)
                    if (__result[i] != null && !LogicaHechizos.EsIdPropio((int)__result[i].Spell)) copia.Add(__result[i]);
                __result = copia;
            }
            catch (Exception e) { AvisoUnaVez("remaining", "Pendientes de Grand Grimoire (se quedan los del juego): " + e.Message); }
        }

        /// <summary>
        /// Postfix de SpellCastRecord.get_QuestSpells: si la lista de la misión trae hechizos propios, devuelve una copia sin
        /// ellos (nunca se toca la del juego, que puede ser la del registro). Si no es una List del juego, se deja.
        /// Postfix of SpellCastRecord.get_QuestSpells: if the quest list contains custom spells, returns a copy without
        /// them (the game's list is never touched, as it may be the registry's). If it is not a game List, it is left alone.
        /// </summary>
        public static void TrasQuestSpells(ref Il2CppSystem.Collections.Generic.IReadOnlyList<TJ.Spells.SpellData> __result)
        {
            if (!Activo || __result == null) return;
            try
            {
                var lista = __result.TryCast<Il2CppSystem.Collections.Generic.List<TJ.Spells.SpellData>>();
                if (lista == null) return;
                bool hay = false;
                for (int i = 0; i < lista.Count && !hay; i++) hay = lista[i] != null && LogicaHechizos.EsIdPropio((int)lista[i].Spell);
                if (!hay) return;
                var copia = new Il2CppSystem.Collections.Generic.List<TJ.Spells.SpellData>();
                for (int i = 0; i < lista.Count; i++)
                    if (lista[i] != null && !LogicaHechizos.EsIdPropio((int)lista[i].Spell)) copia.Add(lista[i]);
                __result = copia.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<TJ.Spells.SpellData>>();
                InfoUnaVez("questspells", $"Logros: la misión Grand Grimoire pide {copia.Count} hechizos de serie (se quitan los propios de los héroes nuevos).");
            }
            catch (Exception e) { AvisoUnaVez("questspells", "Misión Grand Grimoire (se queda la lista del juego): " + e.Message); }
        }

        // Lista de unidades de una raza como ids (vacía si la raza no tiene lista)
        // List of a race's units as ids (empty if the race has no list)
        private static int[] Lista(TabletopTavernData data, Race raza)
        {
            try
            {
                var u = data.GetUnitsOfRace(raza);
                if (u == null) return Array.Empty<int>();
                var r = new int[u.Length];
                for (int i = 0; i < u.Length; i++) r[i] = (int)u[i];
                return r;
            }
            catch { return Array.Empty<int>(); }
        }

        // Unidades reclutadas alguna vez (troopsRecruited del save del jugador) como ids
        // Units ever recruited (troopsRecruited from the player's save) as ids
        private static HashSet<int> Reclutadas()
        {
            var l = SaveDataHandler.GetTroopsIDsCollected();
            if (l == null) return null;
            var r = new HashSet<int>();
            for (int i = 0; i < l.Count; i++) r.Add((int)l[i]);
            return r;
        }

        // ---------------------------------------------------------------
        // 2) Logros que las 9xx suman: filtro en PlayerAchievements.Unlock (SteamAchievements.Unlock hasta el juego 1.10.17)
        // 2) Achievements that the 9xx ones add up: filter in PlayerAchievements.Unlock (SteamAchievements.Unlock up to game 1.10.17)
        // ---------------------------------------------------------------
        /// <summary>
        /// Prefix de PlayerAchievements.Unlock(AchievementId): para los logros que los contenidos nuevos pueden completar,
        /// recuenta sin ellos y devuelve false (no se concede) si solo llegan al umbral gracias a lo nuevo. El resto, tal cual.
        /// Prefix of PlayerAchievements.Unlock(AchievementId): for achievements that the new content can complete,
        /// recount without it and return false (not granted) if they only reach the threshold thanks to the new content. The rest, as is.
        /// </summary>
        public static bool AntesDeUnlock(AchievementId __0)
        {
            if (!Activo) return true;
            try
            {
                // 0.7.6 (decisión prudente, 2.ª ronda de la revisión): con «Impertérritos» aplicado en la sesión, los logros que
                // concede la evaluación de una batalla no se conceden (las escuadras del jugador no pueden huir)
                // 0.7.6 (cautious decision, 2nd round of the review): with «Impertérritos» applied in the session, the achievements that
                // a battle evaluation grants are not granted (the player's squads cannot flee)
                if (_evaluandoBatalla > 0 && Nucleo.Real?.Moral != null)
                {
                    try { Saves.Registro?.ApuntarLogroBloqueadoMoral((int)__0); } catch { }
                    InfoUnaVez("moral" + (int)__0, $"Logros: {__0} no se concede: batalla con «Impertérritos» activo (las escuadras del jugador no huyen).");
                    return false;
                }
                // 3.ª pasada de la revisión (hallazgo 3): lo que se bloqueó así no vuelve por el reenvío de los ganados
                // 3rd pass of the review (finding 3): what was blocked this way does not come back through the resend of earned ones
                if (_reenviando > 0 && Saves.Registro != null && Saves.Registro.LogroBloqueadoMoral((int)__0))
                {
                    InfoUnaVez("moralReenvio" + (int)__0, $"Logros: {__0} no se reenvía: se bloqueó en una batalla con «Impertérritos».");
                    return false;
                }
                // 0.7.5: reenvío de un logro de ejército ya ganado (earnedAchievements): pasa (se ganó pasando por este filtro)
                // (por nombre del enum o por su número: SendEarned los lee con Enum.TryParse, que acepta los dos)
                // 0.7.5: resend of an already earned army achievement (earnedAchievements): let it through (it was earned by passing this filter)
                // (by enum name or by its number: SendEarned reads them with Enum.TryParse, which accepts both)
                if (_reenviando > 0 && EsLogroDeEjercito(__0) && (_ganados.Contains(__0.ToString()) || _ganados.Contains(((int)__0).ToString())))
                {
                    InfoUnaVez("reenvio" + (int)__0, $"Logros: {__0} ya estaba ganado (earnedAchievements); se reenvía a Steam.");
                    return true;
                }
                string motivo = null;
                switch (__0)
                {
                    case AchievementId.MaxDifficultyAllHeroes:
                        motivo = PorHeroes(SaveDataHandler.LoadPlayerSaveData(), LogicaLogros.ConDificultad((int)TT_Difficulty.Godking),
                                           LogicaLogros.UmbralHeroesGodking, "en Godking");
                        break;
                    case AchievementId.RosterComplete:
                        motivo = PorHeroes(_saveVictoria ?? SaveDataHandler.LoadPlayerSaveData(), LogicaLogros.AlgunaDificultad,
                                           LogicaLogros.UmbralHeroesRoster, "con alguna victoria");
                        break;
                    case AchievementId.FourUniqueFactionsRun:
                        if (_gestorFacciones != null)
                        {
                            var (todas, deSerie) = LogicaLogros.ContarRazas(Escuadras(_gestorFacciones, (u, s) => RazaDe(u)), true, (int)Race.Special);
                            if (LogicaLogros.Bloquear(deSerie, todas, LogicaLogros.UmbralCuatroFacciones))
                                motivo = $"{deSerie} razas con unidades de serie ({todas} contando las nuevas; hacen falta {LogicaLogros.UmbralCuatroFacciones})";
                        }
                        else motivo = SinContexto(__0);   // 1.11.1: fuera de CheckForFourFactions no se puede recontar
                                                          // 1.11.1: outside CheckForFourFactions it cannot be recounted
                        break;
                    case AchievementId.GrandAlliance:
                        if (_gestorEjercito != null)
                        {
                            var (todas, deSerie) = LogicaLogros.ContarRazas(Escuadras(_gestorEjercito, (u, s) => RazaDe(u)), false, (int)Race.Special);
                            int umbral = Umbral(() => TJ.Achievements.AchievementRules.GRAND_ALLIANCE_FACTIONS, LogicaLogros.UmbralFaccionesPorDefecto);
                            if (LogicaLogros.Bloquear(deSerie, todas, umbral))
                                motivo = $"{deSerie} facciones con unidades de serie ({todas} contando las nuevas; hacen falta {umbral})";
                        }
                        else motivo = SinContexto(__0);   // 1.11.1: fuera de EvaluateArmyRunStats no se puede recontar
                                                          // 1.11.1: outside EvaluateArmyRunStats it cannot be recounted
                        break;
                    case AchievementId.LegendsAssembled:
                        if (_gestorEjercito != null)
                        {
                            var (todas, deSerie) = LogicaLogros.ContarEscuadras(Escuadras(_gestorEjercito, (u, s) => EsLegendaria(u)));
                            int umbral = Umbral(() => TJ.Achievements.AchievementRules.LEGENDS_ASSEMBLED_UNITS, LogicaLogros.UmbralLegendariasPorDefecto);
                            if (LogicaLogros.Bloquear(deSerie, todas, umbral))
                                motivo = $"{deSerie} escuadras legendarias de serie ({todas} contando las nuevas; hacen falta {umbral})";
                        }
                        else motivo = SinContexto(__0);
                        break;
                    case AchievementId.MasterOfArms:
                        if (_gestorEjercito != null)
                        {
                            var (todos, deSerie) = LogicaLogros.ContarRasgos(Escuadras(_gestorEjercito, (u, s) => (int)s.PrestigeTrait), (int)UnitAttribute.None);
                            int umbral = Umbral(() => TJ.Achievements.AchievementRules.MASTER_OF_ARMS_TRAITS, LogicaLogros.UmbralRasgosPorDefecto);
                            if (LogicaLogros.Bloquear(deSerie, todos, umbral))
                                motivo = $"{deSerie} rasgos de prestigio en escuadras de serie ({todos} contando las nuevas; hacen falta {umbral})";
                        }
                        else motivo = SinContexto(__0);
                        break;
                    // Juego 1.11.1: logros de «todo el ejército desplegado» (AchievementRules.ForFinalArmy, llamado desde ForBattle
                    // en batalla y desde AchievementSync.AwardFromRunHistory con el historial). No hay recuento seguro sin 9xx
                    // (los saves guardan las 9xx como su unidad base): se bloquean si la partida o el historial tienen contenido nuevo
                    // Game 1.11.1: «whole deployed army» achievements (AchievementRules.ForFinalArmy, called from ForBattle
                    // in battle and from AchievementSync.AwardFromRunHistory with the history). There is no safe count without 9xx
                    // (saves store the 9xx ones as their base unit): they are blocked if the run or the history has new content
                    case AchievementId.HiredSwords:
                    case AchievementId.GoblinHorde:
                    case AchievementId.KoboldSwarm:
                    case AchievementId.ShieldSisters:
                        motivo = SinContexto(__0);
                        break;
                    default:
                        return true;
                }
                if (motivo == null) return true;
                InfoUnaVez("bloqueo" + (int)__0, $"Logros: {__0} no se concede: solo llega contando contenido nuevo ({motivo}).");
                return false;
            }
            catch (Exception e)
            {
                // Juego 1.11.1 (revisión): los logros de ejército, ante la duda, NO se conceden (un logro de Steam concedido en
                // falso no se puede quitar; uno bloqueado se puede ganar después sin módulos). El resto decide el juego
                // Game 1.11.1 (review): army achievements, when in doubt, are NOT granted (a Steam achievement granted
                // wrongly cannot be removed; a blocked one can be earned later without modules). The game decides the rest
                bool ejercito = EsLogroDeEjercito(__0);
                AvisoUnaVez("unlock" + (int)__0, $"Logros: no se pudo recontar {__0} sin contenido nuevo ({(ejercito ? "no se concede, por si acaso" : "decide el juego")}): {e.Message}");
                return !ejercito;
            }
        }

        // ---------------------------------------------------------------
        // Juego 1.11.1 (revisión del 01-11-2026): logros de ejército sin contexto de recuento.
        // 0.7.5 (revisión total del 02-11): recuento por partida del historial y reenvío de los ya ganados
        // Game 1.11.1 (review of 01-11-2026): army achievements without a counting context.
        // 0.7.5 (full review of 02-11): per-run counting of the history and resend of already earned ones
        // ---------------------------------------------------------------
        // Contexto de AchievementSync.AwardFromRunHistory: si su historial tiene contenido nuevo, la partida que se está
        // recontando ahora (FinalBattleArmy) y los logros que da cada clase de partida (ForFinalArmy)
        // Context of AchievementSync.AwardFromRunHistory: whether its history has new content, the run currently being
        // recounted (FinalBattleArmy) and the achievements each kind of run grants (ForFinalArmy)
        private sealed class EstadoHistorial
        {
            public bool Nuevo;                                    // ¿alguna partida del historial con contenido nuevo?
                                                                  // any run in the history with new content?
            public bool? PartidaActual;                           // ¿la partida que se recuenta ahora tiene contenido nuevo?
                                                                  // does the run being recounted now have new content?
            public readonly HashSet<int> Limpios = new HashSet<int>();      // logros que da alguna partida de serie
                                                                            // achievements granted by any vanilla run
            public readonly HashSet<int> Modificados = new HashSet<int>();  // logros que dan partidas con contenido nuevo
                                                                            // achievements granted by runs with new content
            public int Partidas;                                  // partidas recontadas (ForFinalArmy visto)
                                                                  // recounted runs (ForFinalArmy seen)
        }
        private static readonly Stack<EstadoHistorial> _antesHistorial = new Stack<EstadoHistorial>();
        private static EstadoHistorial _historial;   // null = fuera de AwardFromRunHistory
                                                     // null = outside AwardFromRunHistory

        // Contexto de AchievementSync.SendEarned: los ganados (nombres del enum) que se reenvían
        // Context of AchievementSync.SendEarned: the earned ones (enum names) that are resent
        private static int _reenviando;
        private static readonly HashSet<string> _ganados = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>¿Es uno de los 8 logros de composición del ejército que las 9xx pueden alterar?</summary>
        /// Is it one of the 8 army composition achievements that the 9xx ones can alter?
        private static bool EsLogroDeEjercito(AchievementId id) =>
            id == AchievementId.FourUniqueFactionsRun || id == AchievementId.GrandAlliance || id == AchievementId.LegendsAssembled ||
            id == AchievementId.MasterOfArms || id == AchievementId.HiredSwords || id == AchievementId.GoblinHorde ||
            id == AchievementId.KoboldSwarm || id == AchievementId.ShieldSisters;

        /// <summary>
        /// Motivo para no conceder un logro de ejército sin contexto de recuento, o null si se deja pasar. Dentro de
        /// AwardFromRunHistory decide el recuento por partida (LogicaLogros.BloquearDesdeHistorial); fuera, la sesión con
        /// módulos o la partida en curso anclada a los módulos (LogicaLogros.BloquearSinContexto). Si algo falla, se bloquea.
        /// Reason for not granting an army achievement without a recount context, or null if it is let through. Inside
        /// AwardFromRunHistory the per-run recount decides (LogicaLogros.BloquearDesdeHistorial); outside, the session with
        /// modules or the current run anchored to the modules (LogicaLogros.BloquearSinContexto). If anything fails, it is blocked.
        /// </summary>
        private static string SinContexto(AchievementId id)
        {
            try
            {
                var h = _historial;
                if (h != null)
                    return LogicaLogros.BloquearDesdeHistorial((int)id, h.Nuevo, h.Partidas > 0, h.Limpios)
                        ? (h.Partidas > 0 ? "solo lo dan partidas del historial jugadas con contenido nuevo"
                                          : "el historial tiene partidas con contenido nuevo y no se ha podido recontar por partida")
                        : null;
                bool modulos = Nucleo.PartidaModificada;
                var reg = Saves.Registro;
                bool anclada = false;
                if (reg != null)
                {
                    // Partida en curso del slot activo; si no se sabe cuál es, cualquiera de las que haya en disco (ante la duda)
                    // Current run of the active slot; if it is unknown which one it is, any of those on disk (when in doubt)
                    if (Saves.SlotActivoConocido) anclada = reg.PartidaAnclada(Saves.PartidaEnCurso());
                    else foreach (string p in Saves.PartidasEnCurso()) anclada |= reg.PartidaAnclada(p);
                }
                if (!LogicaLogros.BloquearSinContexto(modulos, anclada)) return null;
                return modulos ? "la sesión tiene módulos de Zero activos" : "la partida en curso se juega con módulos de Zero";
            }
            catch (Exception e)
            {
                AvisoUnaVez("sincontexto", "Logros de ejército: no se pudo mirar la sesión ni la partida en curso; no se conceden por si acaso: " + e.Message);
                return "no se pudo comprobar si hay contenido nuevo";
            }
        }

        // ¿Tiene contenido nuevo esta partida del historial? (registro, héroe 9xx o unidades 9xx; ronda 2 de la revisión:
        // también si terminó cuando ya había módulos pero aún no se marcaban las partidas, LogicaLogros.SinMarcasPosibles)
        // Does this run in the history have new content? (registry, 9xx hero or 9xx units; round 2 of the review:
        // also if it ended when modules already existed but runs were not yet being marked, LogicaLogros.SinMarcasPosibles)
        private static bool PartidaConNuevo(RunRecord r)
        {
            if (r == null) return false;
            if (LogicaLogros.SinMarcasPosibles(r.endedAtUtcTicks)) return true;
            var unidades = new List<int>();
            // RunSquad es un struct sin referencias (solo enteros y enums): se puede leer por el genérico de interop
            // RunSquad is a struct with no references (only integers and enums): it can be read through the interop generic
            var escuadras = r.squads;
            for (int j = 0; escuadras != null && j < escuadras.Count; j++) unidades.Add((int)escuadras[j].unit);
            var caidas = r.fallen;
            for (int j = 0; caidas != null && j < caidas.Count; j++) unidades.Add((int)caidas[j].unit);
            var reg = Saves.Registro;
            return LogicaLogros.HistorialConContenidoNuevo(new[] { (r.runUUID, r.heroID, (IEnumerable<int>)unidades) }, p => reg != null && reg.PartidaAnclada(p));
        }

        // Historial de partidas del save (runHistory) con alguna partida con contenido nuevo
        // Save run history (runHistory) with at least one run with new content
        private static bool HistorialConNuevo(PlayerSaveData save)
        {
            var historial = save?.runHistory;
            if (historial == null) return false;
            for (int i = 0; i < historial.Count; i++) if (PartidaConNuevo(historial[i])) return true;
            return false;
        }

        /// <summary>Prefix de AchievementSync.AwardFromRunHistory(PlayerSaveData): abre el contexto del recuento por partida.</summary>
        /// Prefix of AchievementSync.AwardFromRunHistory(PlayerSaveData): opens the per-run recount context.
        public static void AntesDeAwardFromRunHistory(PlayerSaveData __0)
        {
            var estado = new EstadoHistorial();
            try { estado.Nuevo = HistorialConNuevo(__0); }
            catch (Exception e)
            {
                estado.Nuevo = true;   // ante la duda, bloquear
                                       // when in doubt, block
                AvisoUnaVez("historial", "Logros: no se pudo leer el historial de partidas; sus logros de ejército no se conceden por si acaso: " + e.Message);
            }
            _antesHistorial.Push(_historial);
            _historial = estado;
            if (estado.Nuevo) InfoUnaVez("historialnuevo", "Logros: el historial de partidas tiene partidas con contenido nuevo; sus logros de ejército se recuentan por partida (solo cuentan las de serie).");
        }

        /// <summary>Finalizer de AchievementSync.AwardFromRunHistory: cierra el contexto pase lo que pase.</summary>
        /// Finalizer of AchievementSync.AwardFromRunHistory: closes the context no matter what.
        public static Exception FinAwardFromRunHistory(Exception __exception)
        {
            _historial = _antesHistorial.Count > 0 ? _antesHistorial.Pop() : null;
            return __exception;
        }

        /// <summary>Prefix de AchievementRules.FinalBattleArmy(RunRecord, ...) (0.7.5): dentro del historial, apunta qué partida se recuenta.</summary>
        /// Prefix of AchievementRules.FinalBattleArmy(RunRecord, ...) (0.7.5): inside the history, records which run is being recounted.
        public static void AntesDeFinalBattleArmy(RunRecord __0)
        {
            var h = _historial;
            if (h == null) return;
            try { h.PartidaActual = PartidaConNuevo(__0); }
            catch { h.PartidaActual = true; }   // ante la duda, como partida con contenido nuevo
                                                // when in doubt, treat as a run with new content
        }

        /// <summary>Postfix de AchievementRules.ForFinalArmy (0.7.5): dentro del historial, apunta qué logros da la partida recontada.</summary>
        /// Postfix of AchievementRules.ForFinalArmy (0.7.5): inside the history, records which achievements the recounted run grants.
        public static void TrasForFinalArmy(Il2CppSystem.Collections.Generic.List<AchievementId> __result)
        {
            var h = _historial;
            if (h == null) return;
            try
            {
                bool nuevo = h.PartidaActual ?? true;   // sin FinalBattleArmy visto, ante la duda
                                                        // no FinalBattleArmy seen, when in doubt
                if (__result != null)
                    for (int i = 0; i < __result.Count; i++) (nuevo ? h.Modificados : h.Limpios).Add((int)__result[i]);
                h.Partidas++;
            }
            catch (Exception e) { AvisoUnaVez("forfinalarmy", "Logros: no se pudo apuntar el recuento de una partida del historial (se bloquea por si acaso): " + e.Message); }
            finally { h.PartidaActual = null; }
        }

        /// <summary>Prefix de AchievementSync.SendEarned(PlayerSaveData) (0.7.5): los ya ganados que se reenvían pasan el filtro.</summary>
        /// Prefix of AchievementSync.SendEarned(PlayerSaveData) (0.7.5): the already earned ones that are resent go through the filter.
        public static void AntesDeSendEarned(PlayerSaveData __0)
        {
            _reenviando++;
            try
            {
                var l = __0?.earnedAchievements;
                for (int i = 0; l != null && i < l.Count; i++) if (!string.IsNullOrEmpty(l[i])) _ganados.Add(l[i]);
            }
            catch (Exception e) { AvisoUnaVez("sendearned", "Logros: no se pudo leer la lista de ganados para reenviarlos (los de ejército pasan por el filtro normal): " + e.Message); }
        }

        /// <summary>Finalizer de AchievementSync.SendEarned: cierra el contexto del reenvío pase lo que pase.</summary>
        /// Finalizer of AchievementSync.SendEarned: closes the resend context no matter what.
        public static Exception FinSendEarned(Exception __exception)
        {
            if (_reenviando > 0) _reenviando--;
            if (_reenviando == 0) _ganados.Clear();
            return __exception;
        }

        // Héroes con victoria del save (todos y solo de serie); motivo del bloqueo o null si decide el juego
        // Heroes with a victory in the save (all and only vanilla ones); reason for blocking, or null if the game decides
        private static string PorHeroes(PlayerSaveData save, Func<IReadOnlyCollection<int>, bool> cuenta, int umbral, string que)
        {
            var lista = save?.HeroDifficultiesCompleted;
            if (lista == null) return null;
            var entradas = new List<(int, IReadOnlyCollection<int>)>();
            for (int i = 0; i < lista.Count; i++)
            {
                var h = lista[i];
                if (h == null) continue;
                var d = new List<int>();
                var dl = h.DifficultiesCompleted;
                for (int j = 0; dl != null && j < dl.Count; j++) d.Add(dl[j]);
                entradas.Add((h.HeroID, d));
            }
            var (todos, deSerie) = LogicaLogros.ContarHeroes(entradas, cuenta);
            return LogicaLogros.Bloquear(deSerie, todos, umbral)
                ? $"{deSerie} héroes de serie {que} ({todos} contando los nuevos; hacen falta {umbral})"
                : null;
        }

        // Escuadras del ejército del jugador (las que tienen unidad, como el juego: UnitIndex != -1) con un dato de cada una
        // Squads in the player's army (those with a unit, like the game: UnitIndex != -1) with one piece of data from each
        private static List<(int, T)> Escuadras<T>(CampaignSaveManager gestor, Func<UnitName, SquadToLoad, T> dato)
        {
            var r = new List<(int, T)>();
            var ejercito = gestor?.SaveData?.playerArmy;
            if (ejercito == null) return r;
            for (int i = 0; i < ejercito.Length; i++)
            {
                var s = ejercito[i];
                if (s == null || s.UnitIndex == -1) continue;
                r.Add(((int)s.UnitName, dato(s.UnitName, s)));
            }
            return r;
        }

        // Raza de una unidad según el juego
        // Race of a unit according to the game
        private static int RazaDe(UnitName u) => (int)TabletopTavernData.InstanceIfExists.GetRaceFromUnitName(u);

        // ¿Es legendaria la unidad? (rareza de sus stats, como ForArmy)
        // Is the unit legendary? (rarity of its stats, like ForArmy)
        private static bool EsLegendaria(UnitName u) => TabletopTavernData.InstanceIfExists.GetSquadStats(u).RarityTier == UnitRarity.Legendary;

        // Umbral leído de AchievementRules del juego o, si no se puede, el valor por defecto
        // Threshold read from the game's AchievementRules or, if that is not possible, the default value
        private static int Umbral(Func<int> leer, int porDefecto)
        {
            try { int v = leer(); return v > 0 ? v : porDefecto; }
            catch { return porDefecto; }
        }

        // ---------------------------------------------------------------
        // Contextos: prefix los abre y el finalizer los cierra pase lo que pase (deja pasar la excepción del juego)
        // Contexts: the prefix opens them and the finalizer closes them no matter what (lets the game's exception through)
        // ---------------------------------------------------------------
        public static void AntesDeApplyVictoryUnlocks(PlayerSaveData __0) { _antesVictoria.Push(_saveVictoria); _saveVictoria = __0; }
        public static Exception FinApplyVictoryUnlocks(Exception __exception) { _saveVictoria = _antesVictoria.Count > 0 ? _antesVictoria.Pop() : null; return __exception; }
        public static void AntesDeCheckForFourFactions(CampaignSaveManager __instance) { _antesFacciones.Push(_gestorFacciones); _gestorFacciones = __instance; }
        public static Exception FinCheckForFourFactions(Exception __exception) { _gestorFacciones = _antesFacciones.Count > 0 ? _antesFacciones.Pop() : null; return __exception; }
        public static void AntesDeEvaluateArmyRunStats(CampaignSaveManager __instance) { _antesEjercito.Push(_gestorEjercito); _gestorEjercito = __instance; }
        public static Exception FinEvaluateArmyRunStats(Exception __exception) { _gestorEjercito = _antesEjercito.Count > 0 ? _antesEjercito.Pop() : null; return __exception; }

        // ---------------------------------------------------------------
        // 3) Logro de objetos: sin la lista de gear ampliada mientras se evalúa
        // 3) Gear achievement: without the expanded gear list while it is evaluated
        // ---------------------------------------------------------------
        /// <summary>Prefix de SaveDataHandler.EvaluateGearCollection: suspende la lista de gear ampliada (si hubiera alguna abierta).</summary>
        /// Prefix of SaveDataHandler.EvaluateGearCollection: suspends the expanded gear list (if any is open).
        public static void AntesDeEvaluateGearCollection()
        {
            int antes = -1;
            try { antes = ArmeriaObjetos.Suspender(); } catch { }
            _suspendidoGear.Push(antes);                         // -1 = no se suspendió nada
                                                                 // -1 = nothing was suspended
        }

        /// <summary>Finalizer de SaveDataHandler.EvaluateGearCollection: devuelve la lista ampliada como estaba.</summary>
        /// Finalizer of SaveDataHandler.EvaluateGearCollection: restores the expanded list as it was.
        public static Exception FinEvaluateGearCollection(Exception __exception)
        {
            try
            {
                int antes = _suspendidoGear.Count > 0 ? _suspendidoGear.Pop() : -1;
                if (antes >= 0) ArmeriaObjetos.Reanudar(antes);
            }
            catch { }
            return __exception;
        }
    }
}
