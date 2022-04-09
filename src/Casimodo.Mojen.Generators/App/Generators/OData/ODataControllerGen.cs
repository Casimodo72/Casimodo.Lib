using System.IO;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Generates ASP.NET Web API OData controllers.
    /// </summary>
    [Obsolete]
    public class ODataControllerGen : WebPartGenerator
    {
        public ODataControllerGen()
        {
            throw new Exception();
            Scope = "App";
        }

        WebODataBuildConfig ODataConfig { get; set; }

        ODataControllerOptions Options { get; set; }

        MojControllerConfig Controller { get; set; }

        public MojType Type { get; set; }

        public Func<string, string> GetDbContextSaveChangesExpression { get; set; }
            = db => $"await {db}.SaveChangesAsync()";

        public override void ProcessOnUse(object usedBy)
        {
            // NOP
        }

        protected override void GenerateCore()
        {
            ODataConfig = App.Get<WebODataBuildConfig>();

            var controllers = App.GetItems<MojControllerConfig>().Where(x => x.Uses(this)).ToArray();

            foreach (var controller in controllers)
            {
                Controller = controller;
                Type = controller.TypeConfig.RequiredStore;

                string controllerFilePath = Path.Combine(
                    ODataConfig.WebODataControllerDirPath,
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
                "Microsoft.AspNet.OData",
                "Microsoft.AspNet.OData.Query",
                "Microsoft.AspNet.OData.Routing",
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
                "Casimodo.Lib.Web.Auth",
                GetAllDataNamespaces()
            );

            ONamespace(ODataConfig.WebODataControllerNamespace);

            foreach (var attr in Controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            O($"[ODataRoutePrefix(\"{Type.PluralName}\")]");
            O($"public partial class {this.GetODataControllerName(Type)} : {ODataConfig.WebODataControllerBaseClass}");
            Begin();

            // EF DB repository
            var repoName = GetWebRepositoryName(Type);
#if (false)
            
            // KABU TODO: Should we use this one instead?
            O("{0} Db", repoName);
            Begin();
            O("get {{ return LazyInitializer.EnsureInitialized(ref _db, () => new {0}()); }}", repoName);
            End();
#endif

            OFormat("{0} _db = new {0}();", repoName);

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
                GenerateFilters(Type);

                if (Options.IsReadable)
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

            // Let other generaors inject further actions.
            foreach (var gen in App.Generators.OfType<IWebApiODataActionInjector>())
                gen.GenerateWebApiODataActionFor(this, Type);

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
                if (editorView == null || editorView.HasNoApi || !editorView.CanCreate
                    || editorView.IsCustomODataCreateAction)
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
            OApiActionAuthAttribute(editorView, "Create");
            O("[HttpPost]");
            O($"public async Task<IHttpActionResult> {action}(ODataActionParameters parameters)");
            Begin();
            O($"return await CreateCore(parameters?.Values?.FirstOrDefault() as {editorView.TypeConfig.Name});");
            End();
        }

        void GenerateCreateMain()
        {
            // No default create method if no editor exists.
            if (Controller.GetEditorView(null) != null)
            {
                O();
                OApiActionAuthAttribute(Type, "Create");
                O("[HttpPost]");
                O("[ODataRoute]");
                O($"public async Task<IHttpActionResult> Post({Type.ClassName} model)");
                Begin();
                O("return await CreateCore(model);");
                End();
            }

            O();
            O($"async Task<IHttpActionResult> CreateCore({Type.ClassName} model)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");
            O();

            O("OnCreatingExtended(model);");

            // KABU TODO: MOVE to repository core layer.
            // Apply DB sequences.
            foreach (var prop in Type.GetProps().Where(x => x.DbAnno.Sequence.IsDbSequence))
            {
                O($"model.{prop.Name} = _db.{GetDbSequenceFunction(prop)};");
            }

            O("var item = _db.Add(model);");
            O();
            O($"{GetDbContextSaveChangesExpression("_db")};");
            O();
            O("return Created(item);");

            End();

            O();
            O($"partial void OnCreatingExtended({Type.ClassName} model);");
        }

        // Filters

        void GenerateFilters(MojType type)
        {
            O();
            O($"Func<IQueryable<{type.ClassName}>, IQueryable<{type.ClassName}>> CustomFilter {{ get; set; }} = (query) => query;");
        }

        // Read ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateRead(MojType type, MojProp key, string keyName)
        {
            // GET: odata/ControllerName
            // ODataQueryOptions: https://aspnet.codeplex.com/SourceControl/latest#Samples/WebApi/OData/v4/ODataQueryableSample/Controllers/OrdersController.cs
            // var settings = new ODataQuerySettings();
            // settings.HandleNullPropagation = HandleNullPropagationOption.Default;
            // settings.EnableConstantParameterization = true;

            // Default query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"{ODataConfig.Namespace}.{ODataConfig.Query}()\")]");
            Oo("[EnableQuery(");
            if (Options.MaxPageSize != null)
                o($"PageSize = {Options.MaxPageSize}, ");
            oO($"AllowedQueryOptions = LookupQueryOptions, MaxExpansionDepth = {Options.MaxExpansionDepth})]");
            O($"public System.Web.Http.IHttpActionResult {ODataConfig.Query}()");
            Begin();
            O("return Ok(CustomFilter(_db.Query()));");
            End();

            // Distinct by property query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"{ODataConfig.Namespace}.{ODataConfig.QueryDistinct}(on={{on}})\")]");
            O("[EnableQuery]");
            O($"public System.Web.Http.IHttpActionResult {ODataConfig.QueryDistinct}(string on)");
            Begin();
            O($"return Ok(CustomFilter(_db.Query()).GroupBy(ExpressionHelper.GetGroupKey<{type.ClassName}>(on.Trim('\\''))).Select(g => g.FirstOrDefault()));");
            End();

            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O("[ODataRoute]");
            OFormat("public async Task<IEnumerable<{0}>> Get(ODataQueryOptions<{0}> query)", type.ClassName);
            Begin();
            O("return await _db.SelectAsync(query);");
            End();

            // KABU TODO: IMPORTANT: Should this return null if not found? Currently a not found exception is thrown.
            // GET: odata/ControllerName(x)
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"({{{keyName}}})\"), EnableQuery]");
            O($"public SingleResult<{type.ClassName}> Get([FromODataUri] {key.Type.Name} {keyName})");
            Begin();
            O($"return SingleResult.Create(_db.Get({keyName}).ToQueryable());");
            End();
        }

        void GenerateUpdateGraphMask(string group)
        {
            var editorView = Controller.GetEditorView(group);
            if (editorView == null)
                return;

            if (editorView.IsCustomODataUpdateData)
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
            GenerateUpdateSingleMain();

            foreach (var viewGroup in Controller.GetViewGroups())
                GenerateUpdateSingle(viewGroup);
        }

        void GenerateUpdateSingle(string group)
        {
            var editorView = Controller.GetEditorView(group);
            if (editorView == null || editorView.HasNoApi || !editorView.CanModify
                || editorView.IsCustomODataUpdateAction)
                return;

            if (editorView.IsCustomODataUpdateAction)
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
                OApiActionAuthAttribute(editorView, "Modify");
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
                OApiActionAuthAttribute(editorView, "Modify");
                O("[HttpPut]");
                O($"[ODataRoute(\"({{{key.VName}}})\")]");
                O($"public async Task<IHttpActionResult> {action}([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model)");
                Begin();
                O($"return await UpdateCore({key.VName}, model, {mask});");
                End();
            }
        }

        void GenerateUpdateSingleMain()
        {
            var key = Type.Key;

            O();
            O($"async Task<IHttpActionResult> UpdateCore([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model, MojDataGraphMask mask, string group = null)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");

            // TODO: REMOVE: O("OnUpdatingExtended(model, group);");

            // Update the item.
            O($"var item = _db.Update({key.VName}, model, mask);");

            O("OnUpdatedExtended(item, group);");

            // Save to DB
            O($"{GetDbContextSaveChangesExpression("_db")};");
            O();
            O("return Updated(item);");
            End();

            O();
            // TODO: REMOVE: O($"partial void OnUpdatingExtended({Type.ClassName} model, string group);");
            O($"partial void OnUpdatedExtended({Type.ClassName} model, string group);");
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
                O($"[ODataRoute(\"({{{key.VName}}})\"), System.Web.Http.AcceptVerbs(\"PATCH\", \"MERGE\")]");
                O($"public async Task<IHttpActionResult> Patch([FromODataUri] {key.Type.Name} {key.VName}, Delta<{Type.ClassName}> delta, CancellationToken cancellationToken)");
                Begin();
                O("Validate(delta.GetEntity());");
                O("if (!ModelState.IsValid) return BadRequest(ModelState);");
                O($"return Updated(await _db.PatchAsync({key.VName}, delta, save: true, token: cancellationToken));");
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
            OApiActionAuthAttribute(Type, "Delete");
            O("[HttpDelete]");
            O($"[ODataRoute(\"({{{key.VName}}})\")]");
            O($"public async Task<IHttpActionResult> Delete([FromODataUri] {key.Type.Name} {key.VName})");
            Begin();
            O();
            // Operate on the entity repository (i.e. not the model repository).
            var repository = Type.Kind == MojTypeKind.Model ? "_db.Entities" : "_db";
            O($"var item = {repository}.Get({key.VName});");
            O();

            if (Type.IsHardDeleteEnabled || ODataConfig.IsPhysicalDeletionEnabled)
            {
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
            O($"{repository}.Delete(item, isPhysicalDeletionAuthorized: true);");
        }

        public void OApiActionAuthAttribute(MojViewConfig view, string action)
        {
            O($"[ApiActionAuth(Part = \"{view.TypeConfig.Name}\", Group = {Moj.CS(view.Group)}, Action = \"{action}\")]");
        }

        public void OApiActionAuthAttribute(MojType type, string action)
        {
            O($"[ApiActionAuth(Part = \"{type.Name}\", Action = \"{action}\")]");
        }
    }
}