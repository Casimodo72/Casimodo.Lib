using System;

namespace Casimodo.Lib.Mojen
{
    public class MojHttpRequestConfig
    {
        public MojDataGraphNode[] ReadGraph { get; set; }

        public MojProp[] ModelProps { get; set; }

        public bool IsLocalData { get; set; }

        public string ODataBaseUrl { get; set; }
        public string ODataReadBaseUrl { get; set; }
        public string ODataSelectUrl { get; set; }
        public string ODataCrudUrl { get; set; }
        public string ODataCreateUrl { get; set; }
        public string ODataUpdateUrlTemplate { get; set; }
        public string ODataDeleteUrl { get; set; }
        public string ODataFilterUrl { get; set; }
        public string AjaxUrl { get; set; }
    }
}
