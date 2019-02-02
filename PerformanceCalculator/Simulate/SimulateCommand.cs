// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Simulate
{
    public abstract class SimulateCommand : ProcessorCommand
    {
        public abstract string Beatmap { get; }

        public abstract Ruleset Ruleset { get; }

        [UsedImplicitly]
        public virtual double Accuracy { get; }

        [UsedImplicitly]
        public virtual int? Combo { get; }

        [UsedImplicitly]
        public virtual double PercentCombo { get; }

        [UsedImplicitly]
        public virtual int Score { get; }

        [UsedImplicitly]
        public virtual string[] Mods { get; }

        [UsedImplicitly]
        public virtual int Misses { get; }

        [UsedImplicitly]
        public virtual int? Mehs { get; }

        [UsedImplicitly]
        public virtual int? Goods { get; }

        class SimulationResults {
            public string BeatmapInfo { get; set; }
            public List<String> Mods;
            public Dictionary<string, dynamic> PlayInfo { get; set; }
            public Dictionary<string, double> CategoryAttribs { get; set; }
            public double PP { get; set; }
        }

        public override void Execute()
        {
            var ruleset = Ruleset;

            var mods = getMods(ruleset).ToArray();

            var workingBeatmap = new ProcessorWorkingBeatmap(Beatmap);
            workingBeatmap.Mods.Value = mods;

            var beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo);

            var beatmapMaxCombo = GetMaxCombo(beatmap);
            var maxCombo = Combo ?? (int)Math.Round(PercentCombo / 100 * beatmapMaxCombo);
            var statistics = GenerateHitResults(Accuracy / 100, beatmap, Misses, Mehs, Goods);
            var score = Score;
            var accuracy = GetAccuracy(statistics);

            var scoreInfo = new ScoreInfo
            {
                Accuracy = accuracy,
                MaxCombo = maxCombo,
                Statistics = statistics,
                Mods = mods,
                TotalScore = score
            };

            var categoryAttribs = new Dictionary<string, double>();
            double pp = ruleset.CreatePerformanceCalculator(workingBeatmap, scoreInfo).Calculate(categoryAttribs);

            if (OutputAsJSON ?? false) {
                var playInfo = new Dictionary<string, dynamic>();
                WritePlayInfoToDict(playInfo, scoreInfo, beatmap);

                OutputJSON(new SimulationResults
                {
                    BeatmapInfo = workingBeatmap.BeatmapInfo.ToString(),
                    Mods = mods.Select(m => m.Acronym).ToList(),
                    CategoryAttribs = categoryAttribs,
                    PlayInfo = playInfo,
                    PP = pp
                });
            } else {
                Console.WriteLine(workingBeatmap.BeatmapInfo.ToString());

                WritePlayInfo(scoreInfo, beatmap);

                WriteAttribute("Mods", mods.Length > 0
                    ? mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                    : "None");

                foreach (var kvp in categoryAttribs)
                    WriteAttribute(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

                WriteAttribute("pp", pp.ToString(CultureInfo.InvariantCulture));
            }
        }

        private List<Mod> getMods(Ruleset ruleset)
        {
            var mods = new List<Mod>();
            if (Mods == null)
                return mods;

            var availableMods = ruleset.GetAllMods().ToList();
            foreach (var modString in Mods)
            {
                Mod newMod = availableMods.FirstOrDefault(m => string.Equals(m.Acronym, modString, StringComparison.CurrentCultureIgnoreCase));
                if (newMod == null)
                    throw new ArgumentException($"Invalid mod provided: {modString}");
                mods.Add(newMod);
            }

            return mods;
        }

        protected virtual void WritePlayInfoToDict(Dictionary<string, dynamic> dict, ScoreInfo scoreInfo, IBeatmap beatmap) {}

        protected abstract void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap);

        protected abstract int GetMaxCombo(IBeatmap beatmap);

        protected abstract Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood);

        protected virtual double GetAccuracy(Dictionary<HitResult, int> statistics) => 0;

        protected void WriteAttribute(string name, string value) => Console.WriteLine($"{name.PadRight(15)}: {value}");
    }
}
