// Copyright (c) 2010 Kasimier Buchcik
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Casimodo.Lib.ComponentModel
{
    //public class ValidateAsAttribute : ValidationAttribute
    //{
    //    public Type Type { get; set; }
    //}

    public class MathPrecisionValidationAttribute : ValidationAttribute
    {
        public MathPrecisionValidationAttribute()
        {
            MinIntegerDigits = 0;
            MaxIntegerDigits = int.MaxValue;
            MinFractionalDigits = 0;
            MaxFractionalDigits = int.MaxValue;
        }

        public MathPrecisionValidationAttribute(int integerDigits, int fractionalDigits)
        {
            MinIntegerDigits = integerDigits;
            MaxIntegerDigits = integerDigits;
            MinFractionalDigits = fractionalDigits;
            MaxFractionalDigits = fractionalDigits;
        }

        public MathPrecisionValidationAttribute(int minIntegerDigits, int maxIntegerDigits, int minFractionalDigits, int maxFractionalDigits)
        {
            MinIntegerDigits = minIntegerDigits;
            MaxIntegerDigits = maxIntegerDigits;
            MinFractionalDigits = minFractionalDigits;
            MaxFractionalDigits = maxFractionalDigits;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            string text = value.ToString().Trim();

            return ValidateFloat(text);
        }

        public override string FormatErrorMessage(string name)
        {
            string text1, text2;

            if (MinIntegerDigits == MaxIntegerDigits)
                text1 = "genau " + MinIntegerDigits;
            else
                text1 = MinIntegerDigits + " bis " + MaxIntegerDigits;

            if (MinFractionalDigits == MaxFractionalDigits)
                text2 = "genau " + MinFractionalDigits;
            else
                text2 = MinFractionalDigits + " bis " + MaxFractionalDigits;

            return
                string.Format("Das Feld '{0}' muss {1} Vor- und {2} Nachkommastellen haben.",
                    name, text1, text2);
        }

        public int MinIntegerDigits { get; set; }

        public int MaxIntegerDigits { get; set; }

        public int MinFractionalDigits { get; set; }

        public int MaxFractionalDigits { get; set; }

        bool ValidateFloat(string text)
        {
            int pos = 0, numDigits, length = text.Length;

            // Skip sign.
            if (text[pos] == '-' || text[pos] == '+')
                pos++;

            if (pos >= length)
                return false;

            numDigits = ParseDigits(text, ref pos);

            if (numDigits < MinIntegerDigits || numDigits > MaxIntegerDigits)
                return false;

            // If end of text:
            if (pos >= length)
            {
                // If no fractional digits are expected --> OK.
                if (MinFractionalDigits <= 0)
                    return true;

                // If fractional digits *are* expected --> BAD.
                return false;
            }

            // We expect a decimal separator.
            if (text[pos] != NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator[0])
                return false;
            else
                pos++;

            // If end of text:
            if (pos >= length)
            {
                // If no fractional digits are expected --> OK.
                if (MinFractionalDigits <= 0)
                    return true;

                // If fractional digits *are* expected --> BAD.
                return false;
            }

            numDigits = ParseDigits(text, ref pos);

            if (numDigits < MinFractionalDigits || numDigits > MaxFractionalDigits)
                return false;

            // Invalid if unexpected trailing chars.
            if (pos < text.Length)
                return false;

            return true;
        }

        static int ParseDigits(string text, ref int pos)
        {
            char c;
            int numDigits = 0;
            while (pos < text.Length)
            {
                c = text[pos];

                if (false == (char.IsNumber(c) || char.IsDigit(c)))
                    return numDigits;

                numDigits++;
                pos++;
            }

            return numDigits;
        }
    }

    public enum MathSignDef
    {
        Optional,
        NotAllowed
    }

    public class MathSignValidationAttribute : ValidationAttribute
    {
        public MathSignValidationAttribute()
        {
            Negative = MathSignDef.Optional;
            Positive = MathSignDef.Optional;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            string text = value.ToString().Trim();
            if (text[0] == '-')
            {
                if (Negative == MathSignDef.NotAllowed)
                    return false;
            }
            else if (text[0] == '+')
            {
                if (Positive == MathSignDef.NotAllowed)
                    return false;
            }

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            string text = null;

            if (Negative == MathSignDef.NotAllowed)
                text = " kein negatives Vorzeichen";

            if (Positive == MathSignDef.NotAllowed)
            {
                if (text != null)
                    text += " und";
                text += " kein positives Vorzeichen";
            }

            return string.Format("Das Feld '{0}' darf {1} enthalten", name, text);
        }

        public MathSignDef Negative { get; set; }

        public MathSignDef Positive { get; set; }

        static bool ParseSign(string text, ref int pos)
        {
            return true;
        }
    }
}