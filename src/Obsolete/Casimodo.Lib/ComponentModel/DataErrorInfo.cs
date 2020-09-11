// Copyright (c) 2010 Kasimier Buchcik

namespace Casimodo.Lib.ComponentModel
{
    /// <summary>
    /// Holds the info of a single data error.
    /// Source: http://mtaulty.com/CommunityServer/blogs/mike_taultys_blog/archive/2009/11/18/silverlight-4-rough-notes-binding-with-inotifydataerrorinfo.aspx
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
    {
    }
}