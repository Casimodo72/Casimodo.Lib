using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Generates ASP.NET Web API OData controllers.
    /// </summary>
    public class ODataControllerGen : WebPartGenerator
    {
        public ODataControllerGen()
        {
            Scope = "App";

            MojenApp.UsingBy += MojenApp_UsingBy;
        }

        void MojenApp_UsingBy(object source, MojUsedByEventArgs args)
        {
            if (args.UsedType == typeof(ODataControllerGen))
            {
                var type = (MojType)args.UsedByObject;
                // Add implitic OData configuration generator.
                var modelGens = type.UsingGenerators;
                if (!modelGens.Any(x => x.Type == typeof(ODataConfigGen)))
                    modelGens.Add(new MojUsingGeneratorConfig { Type = typeof(ODataConfigGen) });
            }
        }

        WebODataBuildConfig OData { get; set; }

        ODataControllerOptions Options { get; set; }

        ControllerConfig Controller { get; set; }

        public MojType Type { get; set; }

        public Func<string, string> GetDbContextSaveChangesExpression { get; set; } = (db) => $"await {db}.SaveChangesAsync()";

        public override void ProcessOnUse(object usedBy)
        {
            // NOP
        }

        protected override void GenerateCore()
        {
            OData = App.Get<WebODataBuildConfig>();

            var controllers = App.GetItems<ControllerConfig>().Where(x => x.Uses(this)).ToArray();

            foreach (var controller in controllers)
            {
                Controller = controller;
                Type = controller.TypeConfig.RequiredStore;

                string controllerFilePath = Path.Combine(
                    OData.WebODataControllersDirPath,
                    this.GetODataControllerName(controller.TypeConfig) + ".generated.cs");

                PerformWrite(controllerFilePath, () => GenerateController());
            }
        }

        void GenerateController()
        {
            Options = Controller.GetGeneratorConfig<ODataControllerOptions>() ?? new ODataControllerOptions();

            var key = Type.Key;

            var dataContext = App.GetDataLayerConfig(Type.DataContextName);

            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks",
                "System.Net",
                //"System.Net.Http",
                "System.Web.Http",
                "System.Web.Http.Controllers",
                "System.Web.OData",
                "System.Web.OData.Query",
                "System.Web.OData.Routing",
                //"AutoMapper",
                //"AutoMapper.QueryableExtensions",
                "System.Data",
                //"System.Data.Entity",
                //"System.Data.Entity.Infrastructure",
                //"System.Web",
                //"System.Web.Mvc",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                GetAllDataNamespaces()
            );

            ONamespace(OData.WebODataServicesNamespace);

            foreach (var attr in Controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            O($"[ODataRoutePrefix(\"{Type.PluralName}\")]");
            O($"public partial class {this.GetODataControllerName(Type)} : {OData.WebODataControllerBaseClass}");
            Begin();

            // EF DB repository
            var repoName = GetWebRepositoryName(Type);
#if (false)
            
            // KABU TODO: Use this one instead:
            O("{0} Db", repoName);
            Begin();
            O("get {{ return LazyInitializer.EnsureInitialized(ref _db, () => new {0}()); }}", repoName);
            End();
#endif

            O("{0} _db = new {0}();", repoName);

            O();
            O("protected override void Initialize(HttpControllerContext controllerContext)");
            Begin();
            O("base.Initialize(controllerContext);");
            O("_db.Request = Request;");
            O("InitializeExtended();");
            End();

            O();
            O("partial void InitializeExtended();");

            if (!Options.IsEmpty)
            {
                GenerateRead(Type, key, Type.Key.VName);

                if (!Options.IsReadOnly)
                {
                    if (Options.IsCreatable)
                        GenerateCreate();

                    if (Options.IsUpdateable)
                    {
                        GenerateUpdate();
                        GeneratePatch();
                    }

                    if (Options.IsDeletable)
                        GenerateDelete();
                }
            }

            // Dispose
            O();
            O("protected override void Dispose(bool disposing)");
            Begin();
            O("_db.Dispose();");
            O("base.Dispose(disposing);");
            End();

            O();
            O("const AllowedQueryOptions LookupQueryOptions = AllowedQueryOptions.Expand | AllowedQueryOptions.Select | AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Count | AllowedQueryOptions.Skip | AllowedQueryOptions.Top | AllowedQueryOptions.Format;");

            if (Options.UpdateMask)
            {
                foreach (var viewGroup in Controller.GetViewGroups())
                    GenerateUpdateGraphMask(viewGroup);
            }

            End(); // Class
            End(); // Namespace
        }

        // Create ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateCreate()
        {
            GenerateCreateMain();

            var actions = new List<string>();
            foreach (var viewGroup in Controller.GetViewGroups().OrderBy(x => x))
            {
                var editorView = Controller.GetEditorView(viewGroup);
                if (editorView == null)
                    continue;

                var action = editorView.GetODataCreateActionName();
                if (actions.Contains(action))
                    continue;

                actions.Add(action);

                GenerateCreateSingle(editorView);
            }
        }

        void GenerateCreateSingle(MojViewConfig editorView)
        {
            if (editorView.Group == null)
                return;

            var action = editorView.GetODataCreateActionName();
            if (action == "Post")
                return;

            O();
            O("[HttpPost]");
            O($"public async Task<IHttpActionResult> {action}({Type.ClassName} model)");
            Begin();
            O($"return await Post(model);");
            End();
        }

        void GenerateCreateMain()
        {
            O();
            O("[HttpPost]");
            O($"[ODataRoute]");
            O($"public async Task<IHttpActionResult> Post({Type.ClassName} model)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");
            O("_db.ReferenceLoading(false);");
            O();

            // KABU TODO: IMPORTANT: MOVE to repository core layer.
            // Apply DB sequences.
            foreach (var prop in Type.GetProps().Where(x => x.DbAnno.Sequence.IsDbSequence))
            {
                O("model.{0} = _db.{1};", prop.Name, GetDbSequenceFunction(prop));
            }

            O("var item = _db.Add(model);");
            O();
            O($"{GetDbContextSaveChangesExpression("_db")};");
            O();
            O("return Created(item);");

            End();
        }

        // Read ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateRead(MojType type, MojProp key, string keyName)
        {
            // GET: odata/ControllerName
            // ODataQueryOptions: https://aspnet.codeplex.com/SourceControl/latest#Samples/WebApi/OData/v4/ODataQueryableSample/Controllers/OrdersController.cs
            // var settings = new ODataQuerySettings();
            // settings.HandleNullPropagation = HandleNullPropagationOption.Default;
            // settings.EnableConstantParameterization = true;
#if (false)
                O();
                O("[ODataRoute, EnableQuery]");
                O("public IQueryable<{0}> Get()", model.ClassName);
                B();
                O("return _db.Query();");
                E();
                O();
#endif
            // Default query function.
            O();
            O("[HttpGet]");
            O($"[ODataRoute(\"{OData.Ns}.{OData.Query}()\")]");
            Oo("[EnableQuery(");
            if (false)
#pragma warning disable CS0162
                o("PageSize = 20, ");
#pragma warning restore CS0162
            oO($"AllowedQueryOptions = LookupQueryOptions, MaxExpansionDepth = {Options.MaxExpansionDepth})]");
            O($"public System.Web.Http.IHttpActionResult {OData.Query}()");
            Begin();
            O("return Ok(CustomFilter(_db.Query()));");
            End();

            // Distinct by property query function.
            O();
            O("[HttpGet]");
            O($"[ODataRoute(\"{OData.Ns}.{OData.QueryDistinct}(on={{on}})\")]");
            O("[EnableQuery]");
            O($"public System.Web.Http.IHttpActionResult {OData.QueryDistinct}(string on)");
            Begin();
            O($"return Ok(CustomFilter(_db.Query()).GroupBy(ExpressionHelper.GetGroupKey<{type.ClassName}>(on.Trim('\\''))).Select(g => g.FirstOrDefault()));");
            End();

            O();
            O($"Func<IQueryable<{type.ClassName}>, IQueryable<{type.ClassName}>> CustomFilter {{ get; set; }} = (query) => query;");

            O();
            O("[HttpGet]");
            O("[ODataRoute]");
            O("public async Task<IEnumerable<{0}>> Get(ODataQueryOptions<{0}> query)", type.ClassName);
            Begin();
            O("return await _db.SelectAsync(query);");
            End();

            // KABU TODO: IMPORTANT: Should this return null if not found? Currently a not found exception is thrown.
            // GET: odata/ControllerName(x)
            O();
            O("[HttpGet]");
            O("[ODataRoute(\"({{{0}}})\"), EnableQuery]", keyName);
            O("public SingleResult<{0}> Get([FromODataUri] {1} {2})", type.ClassName, key.Type.Name, keyName);
            Begin();
            O("return SingleResult.Create(_db.Get({0}).ToQueryable());", keyName);
            End();
        }

        void GenerateUpdateGraphMask(string group)
        {
            var editorView = Controller.GetEditorView(group);
            if (editorView == null)
                return;

            O();
            Oo($"static readonly MojDataGraphMask {GetUpdateMaskMemberName(editorView)} = MojDataGraphMask.ParseXml(@\"");

            // Build the XML of the graph mask.
            o(Controller.BuildDataGraphMaskForUpdate(group).ToString(SaveOptions.DisableFormatting).Replace("\"", "'"));

            oO("\");");
        }

        string GetUpdateMaskMemberName(MojViewConfig view)
        {
            return $"Update{view.Group ?? ""}GraphMask";
        }

        // Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateUpdate()
        {
            GenerateUpdateSingleCore();

            foreach (var viewGroup in Controller.GetViewGroups())
                GenerateUpdateSingle(viewGroup);
        }

        void GenerateUpdateSingle(string group)
        {
            var editorView = Controller.GetEditorView(group);
            if (editorView == null)
                return;

            var key = Type.Key;

            var action = editorView.GetODataUpdateActionName();

            var mask = GetUpdateMaskMemberName(editorView);

            O();
            if (group != null)
            {
                // NOTE: For updates, grouped views will use OData *actions* (POST) rather than the default PUT method.
                // POST: odata/ControllerName(x)/Ns.MethodName
                // Async
                O("[HttpPost]");
                // NOTE: The ID parameter *must* be named "key" by convention.
                // Otherwise the action won't be found by the OData Web API machinery.
                O($"public async Task<IHttpActionResult> {action}([FromODataUri] {key.Type.Name} key, ODataActionParameters parameters)");
                Begin();
                O($"return await UpdateCore(key, parameters?.Values?.FirstOrDefault() as {Type.ClassName}, {mask}, \"{group}\");");
                End();
            }
            else
            {
                // PUT: odata/ControllerName(x)
                // Async
                O("[HttpPut]");
                O($"[ODataRoute(\"({{{key.VName}}})\")]");
                O($"public async Task<IHttpActionResult> {action}([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model)");
                Begin();
                O($"return await UpdateCore({key.VName}, model, {mask});");
                End();
            }
        }

        void GenerateUpdateSingleCore()
        {
            var key = Type.Key;

            O();
            O($"async Task<IHttpActionResult> UpdateCore([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model, MojDataGraphMask mask, string group = null)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");

            // Disable loading of referenced entities.
            O("_db.ReferenceLoading(false);");

            // Update the item.
            O($"var item = _db.Update({key.VName}, model, mask);");

            O("OnUpdatingExtended(item, group);");

            // Save to DB
            O($"{GetDbContextSaveChangesExpression("_db")};");
            O();
            O("return Updated(item);");
            End();

            O();
            O($"partial void OnUpdatingExtended({Type.ClassName} model, string group);");
        }

        // Patch ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GeneratePatch()
        {
            // KABU TODO: REVISIT: Currently disabled.
            // PATCH, MERGE: odata/ControllerName(x)
            // Async, delta
            if (false)
            {
#pragma warning disable 0162                
                var key = Type.Key;

                O();
                O("[AcceptVerbs(\"PATCH\", \"MERGE\")]");
                O("[ODataRoute(\"({{{0}}})\"), System.Web.Http.AcceptVerbs(\"PATCH\", \"MERGE\")]", key.VName);
                O("public async Task<IHttpActionResult> Patch([FromODataUri] {0} {1}, Delta<{2}> delta, CancellationToken cancellationToken)",
                    key.Type.Name, key.VName, Type.ClassName);
                Begin();
                O("Validate(delta.GetEntity());");
                O("if (!ModelState.IsValid) return BadRequest(ModelState);");
                O("_db.ReferenceLoading(false);");
                O("return Updated(await _db.PatchAsync({0}, delta, save: true, token: cancellationToken));", key.VName);
                End();
#pragma warning restore 0162
            }
        }

        // Delete ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateDelete()
        {
            var key = Type.Key;

            // DELETE: odata/ControllerName(x)
            // Async
            O();
            O("[HttpDelete]");
            O("[ODataRoute(\"({{{0}}})\")]", key.VName);
            O("public async Task<IHttpActionResult> Delete([FromODataUri] {0} {1})", key.Type.Name, key.VName);
            Begin();
            O("_db.ReferenceLoading(false);");
            O();
            // Operate on the entity repository (i.e. not the model repository).
            var repository = Type.Kind == MojTypeKind.Model ? "_db.Entities" : "_db";
            O($"var item = {repository}.Get({key.VName});");
            O();

            if (OData.IsPhysicalDeletionEnabled)
            {
                // WARNING: Only for development purposes.
                GeneratePhysicalDelete();
            }
            else
            {
                GenerateSoftDelete();
            }

            O();
            O($"{GetDbContextSaveChangesExpression(repository)};");
            O();
            O("return StatusCode(HttpStatusCode.NoContent);");
            End();
        }

        void GenerateSoftDelete()
        {
            // Set IsSelfDeleted to true.
            O("// Mark as deleted");
            O($"item.{Type.FindDeletedMarker(MojPropDeletedMarker.Self).Name} = true;");

            var repository = Type.Kind == MojTypeKind.Model ? "_db.Entities" : "_db";
            O($"{repository}.Update(item);");
        }

        void GeneratePhysicalDelete()
        {
            O("// Delete physically");
            var repository = Type.Kind == MojTypeKind.Model ? "_db.Entities" : "_db";
            O($"{repository}.Delete(item);");
        }
    }
}