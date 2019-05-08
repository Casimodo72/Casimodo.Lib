using Casimodo.Lib.Data;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ODataControllerPartGetNextSequenceValueGen : ODataControllerPartGenBase
    {
        public ODataControllerPartGetNextSequenceValueGen()
        {
            Name = "GetNextSequenceValue";

            // Process types which have sequence props with Unique.PerTypes.

            // NOTE: DB annotations reside on store properties only.
            SelectProp = prop =>
                prop.StoreOrSelf.DbAnno.Sequence.Is &&
                prop.StoreOrSelf.DbAnno.Unique.HasParams;

            SelectControllers = controllers => controllers.Where(c =>
                c.TypeConfig.GetProps().Any(SelectProp));
        }

        public override void OType(MojType type)
        {
            ONextSequenceValueFunctions(type);
        }

        void ONextSequenceValueFunctions(MojType type)
        {
            // Example:
            // [HttpGet]
            // [ODataRoute("Ga.GetNextSequenceValueForProjectRawNumber(companyId={companyId})")]
            // public System.Web.Http.IHttpActionResult GetNextSequenceValueForProjectRawNumber(Guid companyId)
            // {
            //    return Ok((_db.Core() as GaDbRepositoryCore).GetNextSequenceValueForProjectRawNumber(_db.Db, companyId));
            // }

            string item = type.VClassName;
            string tname = type.ClassName;

            foreach (var sprop in type.GetProps().Where(SelectProp).Select(x => x.RequiredStore))
            {
                O("[HttpGet]");

                // KABU TODO: REVISIT: Apparently the route must not be specified for bound OData functions.
                // var ns = ODataConfig.Ns;
                // var route = prop.GetODataRouteForNextSequenceValue();
                // O($"[ODataRoute(\"{ns}.{route}\")]");
                // userId ={ userId},types ={ types},deletedOn ={ deletedOn}

                var method = sprop.GetNextSequenceValueMethodName();
                // NOTE: Never expose the tenant.
                var parameters = sprop.GetNextSequenceValueMethodParams();
                O($"public System.Web.Http.IHttpActionResult {method}({parameters})");
                Begin();

                var dataConfig = App.GetDataLayerConfig(type.DataContextName);
                var args = sprop.GetNextSequenceValueMethodArgs();
                var tenant = dataConfig.Tenant != null ? "GetTenantId(), " : "";
                O($"return Ok((_db.Core() as {dataConfig.DbRepositoryCoreName}).{method}(_db.Context, {tenant}{args}));");

                End();
                O();
            }
        }
    }
}