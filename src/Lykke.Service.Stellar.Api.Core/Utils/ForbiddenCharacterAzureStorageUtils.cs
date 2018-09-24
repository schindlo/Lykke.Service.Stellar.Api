using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lykke.Service.Stellar.Api.Core.Utils
{
    public  static class ForbiddenCharacterAzureStorageUtils
    {
        /*
            The forward slash (/) character
            The backslash (\) character
            The number sign (#) character
            The question mark (?) character
            Control characters from U+0000 to U+001F, including:
            The horizontal tab (\t) character
            The linefeed (\n) character
            The carriage return (\r) character
            Control characters from U+007F to U+009F
         */

        private static HashSet<char> _forbiddenCharacters = new HashSet<char>()
        {
            '/', '\\', '#', '?','\t', '\r','\n'
        };

        static ForbiddenCharacterAzureStorageUtils()
        {
            for (int i = 0; i <= 0x001f; i++)
            {
                char forbidden = (char)i;
                _forbiddenCharacters.Add(forbidden);
            }

            for (int i = 0x007F; i <= 0x009F; i++)
            {
                char forbidden = (char)i;
                _forbiddenCharacters.Add(forbidden);
            }
        }

        public static bool IsValidRowKey(string rowKey)
        {
            return !rowKey?.Any(x => _forbiddenCharacters.Contains(x)) ?? true;
        }
    }
}
