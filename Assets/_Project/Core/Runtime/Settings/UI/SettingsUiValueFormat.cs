using System;
using System.Globalization;

namespace Project.Core.Settings.Ui
{
    public static class SettingsUiValueFormat
    {
        public static string Percent01(float value01)
        {
            var p = (int)Math.Round(value01 * 100f);
            return $"{p}%";
        }

        public static string Seconds(float seconds)
        {
            var s = (int)Math.Round(seconds);
            return $"{s}s";
        }

        public static string Multiplier(float value, int decimals = 1)
        {
            string fmt = decimals <= 0 ? "0" : "0." + new string('0', decimals);
            return value.ToString(fmt, CultureInfo.InvariantCulture) + "x";
        }

        public static string Number(float value, int maxDecimals = 2)
        {
            string fmt = maxDecimals switch
            {
                <= 0 => "0",
                1 => "0.#",
                2 => "0.##",
                3 => "0.###",
                _ => "0." + new string('#', maxDecimals)
            };

            return value.ToString(fmt, CultureInfo.InvariantCulture);
        }
    }
}