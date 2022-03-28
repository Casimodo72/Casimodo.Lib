// Copyright (c) 2010 Kasimier Buchcik

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    public class ValidationErrorInfo
    {
        public object ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}