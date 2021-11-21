// Copyright (c) 2010 Kasimier Buchcik

namespace Casimodo.Lib.ComponentModel
{
    /// <summary>
    /// Holds the info of a single data error.
    /// Source: Copied from Mike Taulty community server blog entry (this blog entry does not exist anymore)
    /// </summary>
    public class DataErrorInfo
    {
        public object ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        // public bool IsWarning { get; set; }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }

    public class WrapperDataErrorInfo : DataErrorInfo
    { }
}