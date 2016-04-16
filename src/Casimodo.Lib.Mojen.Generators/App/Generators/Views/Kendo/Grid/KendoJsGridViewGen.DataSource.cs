using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoJsGridViewGen
    {
        void GenerateDataSourceOptions(WebViewGenContext context)
        {
            if (Options.IsUsingLocalData)
                GenerateLocalDataSource(context);
            else
                GenerateODataV4DataSource(context);
        }

        // KABU TODO: REVISIT: SignalR example: https://github.com/telerik/ui-for-aspnet-mvc-examples/blob/master/grid/signalR-bound-grid/KendoUIMVC5/Views/Home/Index.cshtml

        void GenerateLocalDataSource(WebViewGenContext context)
        {
            O("data: [],");

            // Data schema
            OB("schema:");

            // Data model
            // The data-source's model is created by the view model.
            O($"model: space.vm.createDataModel()");

            End(","); // Schema
        }

        void GenerateODataV4DataSource(WebViewGenContext context)
        {
            var config = new KendoDataSourceConfig
            {
                TypeConfig = context.View.TypeConfig,
                TransportType = DataSourceType,
                TransportConfig = TransportConfig,
                // NOTE: Grouped views will use OData *actions* for updates.
                UseODataActions = context.View.Group != null,
                InitialSortProps = InitialSortProps,
                // The data-source's model is created by the view model.
                ModelFactory = "space.vm.createDataModel()",
                UrlFactory = "space.vm.createRequestUrl()",
                // Reload and refresh the whole grid after an update was performed.
                // We need this because otherwise computed properties won't be updated.
                RequestEndFunction = "kendomodo.onDataSourceRequestEnd",
                CanCreate = CanCreate,
                CanEdit = CanEdit,
                CanDelete = CanDelete,
                PageSize = Options.PageSize,
                IsServerPaging = Options.IsServerPaging
            };

            KendoGen.ODataSource(context, config);
        }
    }
}