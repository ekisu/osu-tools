// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace PerformanceCalculator.Profile
{
    [Command(Name = "profile", Description = "Computes the total performance (pp) of a profile.")]
    public class ProfileCommand : ProcessorCommand
    {
        [UsedImplicitly]
        [Required]
        [Argument(0, Name = "user", Description = "User ID is preferred, but username should also work.")]
        public string ProfileName { get; }

        [UsedImplicitly]
        [Required]
        [Argument(1, Name = "api key", Description = "API Key, which you can get from here: https://osu.ppy.sh/p/api")]
        public string Key { get; }

        [UsedImplicitly]
        [Option(Template = "-r|--ruleset:<ruleset-id>", Description = "The ruleset to compute the profile for. 0 - osu!, 1 - osu!taiko, 2 - osu!catch, 3 - osu!mania. Defaults to osu!.")]
        [AllowedValues("0", "1", "2", "3")]
        public int? Ruleset { get; }

        private const string base_url = "https://osu.ppy.sh";

        class SimpleUserPlayInfo {
            public int? BeatmapID { get; set; } // uhh idk
            public string BeatmapName { get; set; }
            public List<string> Mods { get; set; }
            public double LivePP { get; set; } 
            public double LocalPP { get; set; }
            public double PPDelta { get; set; }
            public int PositionDelta { get; set; }
        }

        class PerformanceResults {
            public string Username { get; set; }
            public double LivePP { get; set; } 
            public double LocalPP { get; set; }
            public double BonusPP { get; set; }
            public List<SimpleUserPlayInfo> DisplayPlays { get; set; }
        }

        public override void Execute()
        {
                        var displayPlays = new List<UserPlayInfo>();

            var ruleset = LegacyHelper.GetRulesetFromLegacyID(Ruleset ?? 0);

            if (!OutputAsJSON ?? false) // Be quiet!
                Console.WriteLine("Getting user data...");
            dynamic userData = getJsonFromApi($"get_user?k={Key}&u={ProfileName}&m={Ruleset}&type=username")[0];

            if (!OutputAsJSON ?? false)
                Console.WriteLine("Getting user top scores...");
            foreach (var play in getJsonFromApi($"get_user_best?k={Key}&u={ProfileName}&m={Ruleset}&limit=100&type=username"))
            {
                try {
                string beatmapID = play.beatmap_id;

                string cachePath = Path.Combine("cache", $"{beatmapID}.osu");
                if (!File.Exists(cachePath))
                {
                    if (!OutputAsJSON ?? false)
                        Console.WriteLine($"Downloading {beatmapID}.osu...");
                    new FileWebRequest(cachePath, $"{base_url}/osu/{beatmapID}").Perform();
                }

                Mod[] mods = ruleset.ConvertLegacyMods((LegacyMods)play.enabled_mods).ToArray();

                var working = new ProcessorWorkingBeatmap(cachePath, (int)play.beatmap_id) { Mods = { Value = mods } };

                var score = new ProcessorScoreParser(working).Parse(new ScoreInfo
                {
                    Ruleset = ruleset.RulesetInfo,
                    MaxCombo = play.maxcombo,
                    Mods = mods,
                    Statistics = new Dictionary<HitResult, int>
                    {
                        { HitResult.Perfect, (int)play.countgeki },
                        { HitResult.Great, (int)play.count300 },
                        { HitResult.Good, (int)play.count100 },
                        { HitResult.Ok, (int)play.countkatu },
                        { HitResult.Meh, (int)play.count50 },
                        { HitResult.Miss, (int)play.countmiss }
                    }
                });

                var thisPlay = new UserPlayInfo
                {
                    Beatmap = working.BeatmapInfo,
                    LocalPP = ruleset.CreatePerformanceCalculator(working, score.ScoreInfo).Calculate(),
                    LivePP = play.pp,
                    Mods = mods.Select(m => m.Acronym).ToList()
                };

                displayPlays.Add(thisPlay);
                } catch (Exception) {}
            }

            var localOrdered = displayPlays.OrderByDescending(p => p.LocalPP).ToList();
            var liveOrdered = displayPlays.OrderByDescending(p => p.LivePP).ToList();

            int index = 0;
            double totalLocalPP = localOrdered.Sum(play => Math.Pow(0.95, index++) * play.LocalPP);
            double totalLivePP = userData.pp_raw;

            index = 0;
            double nonBonusLivePP = liveOrdered.Sum(play => Math.Pow(0.95, index++) * play.LivePP);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPP = (totalLivePP - nonBonusLivePP);
            totalLocalPP += playcountBonusPP;

            if (OutputAsJSON ?? false) {
                OutputJSON(new PerformanceResults
                {
                    Username = userData.username,
                    LivePP = totalLivePP,
                    LocalPP = totalLocalPP,
                    BonusPP = playcountBonusPP,
                    DisplayPlays = localOrdered.Select(item => new SimpleUserPlayInfo
                    {
                        BeatmapID = item.Beatmap.OnlineBeatmapID,
                        BeatmapName = item.Beatmap.ToString(),
                        Mods = item.Mods,
                        LivePP = item.LivePP,
                        LocalPP = item.LocalPP,
                        PPDelta = item.LocalPP - item.LivePP,
                        PositionDelta = liveOrdered.IndexOf(item) - localOrdered.IndexOf(item)
                    }).ToList()
                });
            } else {
                OutputDocument(new Document(
                    new Span($"User:     {userData.username}"), "\n",
                    new Span($"Live PP:  {totalLivePP:F1} (including {playcountBonusPP:F1}pp from playcount)"), "\n",
                    new Span($"Local PP: {totalLocalPP:F1}"), "\n",
                    new Grid
                    {
                        Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                        Children =
                        {
                            new Cell("beatmap"),
                            new Cell("live pp"),
                            new Cell("local pp"),
                            new Cell("pp change"),
                            new Cell("position change"),
                            localOrdered.Select(item => new[]
                            {
                                new Cell($"{item.Beatmap.OnlineBeatmapID} - {item.Beatmap}"),
                                new Cell($"{item.LivePP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP:F1}") { Align = Align.Right },
                                new Cell($"{item.LocalPP - item.LivePP:F1}") { Align = Align.Right },
                                new Cell($"{liveOrdered.IndexOf(item) - localOrdered.IndexOf(item):+0;-0;-}") { Align = Align.Center },
                            })
                        }
                    }
                ));
            }
        }

        private dynamic getJsonFromApi(string request)
        {
            var req = new JsonWebRequest<dynamic>($"{base_url}/api/{request}");
            req.Perform();
            return req.ResponseObject;
        }
    }
}
