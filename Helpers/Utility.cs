using System.Globalization;

namespace Calkos.Web.Helpers
{
    public static class Utility
    {
        private static readonly CultureInfo _culture = new CultureInfo("it-IT");

        public static string NomeMese(int mese)
        {
            if (mese < 1 || mese > 12)
                return "";

            return _culture.DateTimeFormat.GetMonthName(mese);
        }

        public static string NomeMeseAbbreviato(int mese)
        {
            if (mese < 1 || mese > 12)
                return "";

            return _culture.DateTimeFormat.GetAbbreviatedMonthName(mese);
        }

        public static bool IsNumeric(string? value)
        {
            return int.TryParse(value, out _);
        }

        public static string SafeTrim(string? value)
        {
            return value?.Trim() ?? "";
        }
    }
}
