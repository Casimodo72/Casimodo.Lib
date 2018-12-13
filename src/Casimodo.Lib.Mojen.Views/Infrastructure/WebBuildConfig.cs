﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class WebOutputCacheConfig : MojenBuildConfig
    {
        public bool IsEnabled { get; set; }
        public string CacheProfile { get; set; } = "Default";
        public bool Revalidate { get; set; } = true;
    }

    public class WebResultComponentInfo : MojBase
    {
        public MojViewConfig View { get; set; }
    }

    public class WebResultBuildInfo : MojBase
    {
        public List<WebResultComponentInfo> Components { get; set; } = new List<WebResultComponentInfo>();
    }

    public class WebAppBuildConfig : MojenBuildConfig
    {
        public string WebNamespace { get; set; }
        

        public string WebAppConfigNamespace { get; set; }

        // Web repositories
        public string WebRepositoriesDirPath { get; set; }

        public string WebPickItemsDirPath { get; set; }

        public string WebConfigurationDirPath { get; set; }

        public string WebAuthConfigurationDirPath { get; set; }
        public string WebAuthNamespace { get; set; }

        public WebOutputCacheConfig OutputCache { get; private set; } = new WebOutputCacheConfig();

        // Web MVC controllers
        public string WebMvcControllersOutputDirPath { get; set; }

        public string WebControllersNamespace { get; set; }

        public string WebDataViewModelsNamespace { get; set; }

        // Web views
        public string WebMvcViewsDirPath { get; set; }

        public string WebRegistryTypeScriptDirPath { get; set; }
        public string WebViewsTypeScriptDirPath { get; set; }

        public string WebRegistryJavaScriptDirPath { get; set; }

        public string WebViewsJavaScriptDirPath { get; set; }

        /// <summary>
        /// KABU TODO: REMOVE? Not used
        /// </summary>
        public string WebViewsJavaScriptVirtualDirPath { get; set; }

        public bool ThrowIfControllerActionIsMissing { get; set; }
        public string ScriptNamespace { get; set; }
        public string ScriptUINamespace { get; set; }
    }
}
