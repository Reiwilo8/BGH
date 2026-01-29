using System.Collections.Generic;
using System.Globalization;
using Project.Core.Input;
using Project.Core.Settings;

namespace Project.Games.Run
{
    public static class GameRunInitialParametersBuilder
    {
        public static IReadOnlyDictionary<string, string> Build(
            ISettingsService settings,
            bool useRandomSeed,
            int? seedValue)
        {
            var dict = new Dictionary<string, string>(capacity: 6);

            dict[GameRunInitialParameterKeys.SeedMode] = useRandomSeed ? "Random" : "Fixed";

            if (seedValue.HasValue)
                dict[GameRunInitialParameterKeys.SeedValue] = seedValue.Value.ToString(CultureInfo.InvariantCulture);

            var hint = ResolveEffectiveHintMode(settings);
            dict[GameRunInitialParameterKeys.ControlHintMode] = hint.ToString();

            float gameVol = 1f;
            try
            {
                if (settings != null && settings.Current != null)
                    gameVol = settings.Current.gameVolume;
            }
            catch { }

            dict[GameRunInitialParameterKeys.GameVolume] = gameVol.ToString("0.###", CultureInfo.InvariantCulture);

            return dict;
        }

        private static ControlHintMode ResolveEffectiveHintMode(ISettingsService settings)
        {
            try
            {
                if (settings == null || settings.Current == null)
                    return ControlHintMode.Auto;

                var mode = settings.Current.controlHintMode;
                if (mode == ControlHintMode.Auto)
                    mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

                return mode;
            }
            catch
            {
                return ControlHintMode.Auto;
            }
        }
    }
}