using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.Globalization;

namespace Casimodo.Lib.Code
{
    public static class CompilerHelper
    {
        static readonly CSharpCodeProvider _compiler =
           new CSharpCodeProvider(
               new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } });

        static readonly Dictionary<char, string> GermanReplacements = new Dictionary<char, string>() { { 'ä', "ae" }, { 'ö', "oe" }, { 'ü', "ue" }, { 'Ä', "Ae" }, { 'Ö', "Oe" }, { 'Ü', "Ue" }, { 'ß', "ss" } };
        // Compliant with item 2.4.2 of the C# specification
        static readonly Regex IdentifierRegEx = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");

        // See http://stackoverflow.com/questions/1271567/how-do-i-replace-accents-german-in-net
        public static string NormalizeToCodeIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sb = new StringBuilder();
            char ch;
            bool nextToUpper = true;
            for (int i = 0; i < text.Length; i++)
            {
                ch = text[i];
                if (char.IsWhiteSpace(ch))
                {
                    nextToUpper = true;
                    continue;
                }

                if (!char.IsLetter(ch) && !char.IsNumber(ch))
                    continue;

                if (nextToUpper)
                    sb.Append(char.ToUpper(ch));
                else
                    sb.Append(ch);

                nextToUpper = false;
            }

            text = sb.ToString().Aggregate(new StringBuilder(),
            (b, c) =>
            {
                string r;
                if (GermanReplacements.TryGetValue(c, out r))
                    return b.Append(r);
                else
                    return b.Append(c);
            }).ToString();

            text = RemoveDiacritics(text);

            // Compliant with item 2.4.2 of the C# specification            
            text = IdentifierRegEx.Replace(text, "");

            if (string.IsNullOrEmpty(text))
                return null;

            //The identifier must start with a character or a "_"
            if (!char.IsLetter(text, 0) && !_compiler.IsValidIdentifier(text))
                text = string.Concat("_", text);

            return text;
        }

        // See http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net
        static string RemoveDiacritics(string text)
        {
            text = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(text[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(text[i]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
    }
}