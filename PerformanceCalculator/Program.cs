﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Formats;
using PerformanceCalculator.Difficulty;
using PerformanceCalculator.Performance;
using PerformanceCalculator.Profile;
using PerformanceCalculator.Simulate;

namespace PerformanceCalculator
{
    [Command("dotnet PerformanceCalculator.dll")]
    [Subcommand(typeof(DifficultyCommand))]
    [Subcommand(typeof(PerformanceCommand))]
    [Subcommand(typeof(ProfileCommand))]
    [Subcommand(typeof(SimulateListingCommand))]
    [HelpOption("-?|-h|--help")]
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            LegacyDifficultyCalculatorBeatmapDecoder.Register();

            Logger.Enabled = false;
            CommandLineApplication.Execute<Program>(args);
        }

        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}
