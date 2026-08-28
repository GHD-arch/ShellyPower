using System;

namespace NINA.ShellyPower
{
    /// <summary>
    /// Libellés bilingues du plugin — ANGLais par défaut (en-US/en-GB), français si la
    /// culture UI de NINA est "fr".
    /// </summary>
    public static class ShellyStrings
    {
        private static readonly bool IsEnglish =
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        public static string L(string fr, string en) => IsEnglish ? en : fr;
    }
}