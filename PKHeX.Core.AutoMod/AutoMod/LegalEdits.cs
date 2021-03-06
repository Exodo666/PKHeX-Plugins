﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Suggestion edits that rely on a <see cref="LegalityAnalysis"/> being done.
    /// </summary>
    public static class LegalEdits
    {
        /// <summary>
        /// Set a valid Pokeball based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        public static void SetSuggestedBall(this PKM pk)
        {
            var la = new LegalityAnalysis(pk);
            var report = la.Report();
            if (!report.Contains(LegalityCheckStrings.LBallEncMismatch))
                return;
            if (pk.GenNumber == 5 && pk.Met_Location == 75)
                pk.Ball = (int)Ball.Dream;
            else
                pk.Ball = 4;
        }

        /// <summary>
        /// Sets the <see cref="PKM.RelearnMoves"/> based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk"></param>
        public static void SetSuggestedRelearnMoves(this PKM pk)
        {
            if (pk.Format < 6)
                return;
            pk.ClearRelearnMoves();
            var la = new LegalityAnalysis(pk);

            int[] m = la.GetSuggestedRelearn();
            if (m.All(z => z == 0))
            {
                if (!pk.WasEgg && !pk.WasEvent && !pk.WasEventEgg && !pk.WasLink)
                {
                    if (pk.Version != (int)GameVersion.CXD)
                    {
                        var encounter = la.GetSuggestedMetInfo();
                        if (encounter != null)
                            m = encounter.Relearn;
                    }
                }
            }

            if (pk.RelearnMoves.SequenceEqual(m))
                return;
            if (m.Length > 3)
                pk.RelearnMoves = m;
        }

        /// <summary>
        /// Sets the <see cref="PKM.Met_Location"/> (and other met details) based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk"></param>
        public static void SetSuggestedMetLocation(this PKM pk)
        {
            var la = new LegalityAnalysis(pk);

            var encounter = la.GetSuggestedMetInfo();
            if (encounter == null || (pk.Format >= 3 && encounter.Location < 0))
                return;

            int level = encounter.Level;
            int location = encounter.Location;
            int minlvl = Legal.GetLowestLevel(pk, encounter.Species);
            if (minlvl == 0)
                minlvl = level;

            if (pk.CurrentLevel >= minlvl && pk.Met_Level == level && pk.Met_Location == location)
                return;
            if (minlvl < level)
                level = minlvl;
            pk.Met_Location = location;
            pk.Met_Level = level;
        }

        /// <summary>
        /// Removes all ribbons from the provided <see cref="pk"/>, using reflection to clear one bit at a time.
        /// </summary>
        /// <param name="pk">Pokémon to modify.</param>
        public static void ClearAllRibbons(this PKM pk) => pk.SetRibbonValues(GetRibbonNames(pk), 0, false);

        /// <summary>
        /// Sets all ribbon flags according to a legality report.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        public static void SetSuggestedRibbons(this PKM pk)
        {
            string report = new LegalityAnalysis(pk).Report();
            if (report.Contains(string.Format(LegalityCheckStrings.LRibbonFMissing_0, "")))
            {
                var val = string.Format(LegalityCheckStrings.LRibbonFMissing_0, "");
                var ribbonList = GetRequiredRibbons(report, val);
                var missingRibbons = GetRibbonsRequired(pk, ribbonList);
                SetRibbonValues(pk, missingRibbons, 0, true);
            }
            if (report.Contains(string.Format(LegalityCheckStrings.LRibbonFInvalid_0, "")))
            {
                var val = string.Format(LegalityCheckStrings.LRibbonFInvalid_0, "");
                string[] ribbonList = GetRequiredRibbons(report, val);
                var invalidRibbons = GetRibbonsRequired(pk, ribbonList);
                SetRibbonValues(pk, invalidRibbons, 0, false);
            }
        }

        private static IEnumerable<string> GetRibbonsRequired(PKM pk, string[] ribbonList)
        {
            foreach (var RibbonName in GetRibbonNames(pk))
            {
                string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                if (ribbonList.Contains(v))
                    yield return RibbonName;
            }
        }

        private static string[] GetRequiredRibbons(string Report, string val)
        {
            return Report.Split(new[] { val }, StringSplitOptions.None)[1].Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(new[] { ", " }, StringSplitOptions.None);
        }

        private static void SetRibbonValues(this PKM pk, IEnumerable<string> ribNames, int vRib, bool bRib)
        {
            foreach (string rName in ribNames)
            {
                bool intRib = rName == nameof(PK6.RibbonCountMemoryBattle) || rName == nameof(PK6.RibbonCountMemoryContest);
                ReflectUtil.SetValue(pk, rName, intRib ? (object)vRib : bRib);
            }
        }

        private static IEnumerable<string> GetRibbonNames(PKM pk) => ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
    }
}