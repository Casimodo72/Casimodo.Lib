using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class WebBuildConfig : MojenBuildConfig
    {
        public string WebNamespace { get; set; }

        public string WebAppConfigNamespace { get; set; }

        public string WebStartupDirPath { get; set; }

        // Web repositories
        public string WebRepositoriesDirPath { get; set; }

        public string WebPickItemsDirPath { get; set; }

        public int ClientOutputCacheDurationSec { get; set; } = 3600;

        // Web MVC controllers
        public string WebControllersOutputDirPath { get; set; }

        public string WebControllersNamespace { get; set; }

        public string WebDataViewModelsNamespace { get; set; }

        // Web views
        public string WebViewsDirPath { get; set; }

        public string WebViewsJavaScriptDirPath { get; set; }

        public string WebViewsJavaScriptVirtualDirPath { get; set; }

        public bool ThrowIfControllerActionIsMissing { get; set; }

    }
}
