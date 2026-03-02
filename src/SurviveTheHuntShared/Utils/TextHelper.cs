using System;

namespace SurviveTheHuntShared.Utils
{
    public static class TextHelper
    {
        public static string CamelToKebab(string camel)
        {
            if(camel.Length == 0)
            {
                return camel;
            }

            string kebab = "";

            char prevChar = camel[0];

            kebab += Char.ToLowerInvariant(prevChar);

            for(int i = 1; i < camel.Length; i++)
            {
                char c = camel[i];

                if(Char.IsLower(prevChar) && Char.IsUpper(c))
                {
                    kebab = kebab.Remove(kebab.Length - 1, 1);
                    kebab += $"{prevChar}-{Char.ToLowerInvariant(c)}";
                } else
                {
                    kebab += Char.ToLowerInvariant(c);
                }

                prevChar = c;
            }

            return kebab;
        }
    }
}
