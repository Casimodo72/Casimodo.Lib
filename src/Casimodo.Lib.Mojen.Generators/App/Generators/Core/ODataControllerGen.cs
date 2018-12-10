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
    public class CoreODataControllerGen : WebPartGenerator
    {
        public CoreODataControllerGen()
        {
            Scope = "App";
        }

        WebODataBuildConfig ODataConfig { get; set; }

        ODataControllerOptions Options { get; set; }

        MojControllerConfig Controller { get; set; }

        public MojType Type { get; set; }

        public Func<string, string> GetDbContextSaveChangesExpression { get; set; } = (db) => $"await {db}.SaveChangesAsync()";

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
                    ODataConfig.WebODataControllersDirPath,
                    this.GetODataControllerName(controller.TypeConfig) + ".generated.cs");

                PerformWrite(controllerFilePath, () => GenerateController());
            }
        }

        string GetWebRepositoryName(MojType type)
        {
            return type.PluralName + "Repository";
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
                //"System.Net",
                //"System.Web.Http",
                //"System.Web.Http.Controllers",
                "Microsoft.AspNet.OData",
                "Microsoft.AspNet.OData.Query",
                "Microsoft.AspNet.OData.Routing",
                "System.Data",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                "Casimodo.Lib.Web.Auth",
                "Microsoft.AspNetCore.Mvc",
                GetAllDataNamespaces()
            );

            ONamespace(ODataConfig.WebODataServicesNamespace);

            foreach (var attr in Controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            O($"[ODataRoutePrefix(\"{Type.PluralName}\")]");
            O($"public partial class {this.GetODataControllerName(Type)} : {ODataConfig.WebODataControllerBaseClass}");
            Begin();

            // EF DB repository
            var repoName = GetWebRepositoryName(Type);

            O("{0} _db = new {0}();", repoName);

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

            // NOTE: No Dipose() method because ASP Core controllers do not have those.

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
            O($"public async Task<IActionResult> {action}(ODataActionParameters parameters)");
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
                O($"public async Task<IActionResult> Post({Type.ClassName} model)");
                Begin();
                O("return await CreateCore(model);");
                End();
            }

            O();
            O($"async Task<IActionResult> CreateCore({Type.ClassName} model)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");
            O("_db.ReferenceLoading(false);");
            O();

            O("OnCreatingExtended(model);");

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

        void GenerateRead(MojType type, MojProp keyProp, string key)
        {
            // GET: odata/Entities
            // ODataQueryOptions: https://aspnet.codeplex.com/SourceControl/latest#Samples/WebApi/OData/v4/ODataQueryableSample/Controllers/OrdersController.cs
            // var settings = new ODataQuerySettings();
            // settings.HandleNullPropagation = HandleNullPropagationOption.Default;
            // settings.EnableConstantParameterization = true;

            // Default query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"{ODataConfig.Ns}.{ODataConfig.Query}()\")]");
            Oo("[EnableQuery(");
            if (false)
#pragma warning disable CS0162
                o("PageSize = 20, ");
#pragma warning restore CS0162
            oO($"AllowedQueryOptions = LookupQueryOptions, MaxExpansionDepth = {Options.MaxExpansionDepth})]");
            O($"public IActionResult {ODataConfig.Query}()");
            Begin();
            O("return Ok(CustomFilter(_db.Query()));");
            End();

            // Distinct by property query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"{ODataConfig.Ns}.{ODataConfig.QueryDistinct}(on={{on}})\")]");
            O("[EnableQuery]");
            O($"public IActionResult {ODataConfig.QueryDistinct}(string on)");
            Begin();
            O($"return Ok(CustomFilter(_db.Query()).GroupBy(ExpressionHelper.GetGroupKey<{type.ClassName}>(on.Trim('\\''))).Select(g => g.FirstOrDefault()));");
            End();

            // GET: odata/Entities(x)
            O();
            OApiActionAuthAttribute(Type, "View");
            O("[HttpGet]");
            O($"[ODataRoute(\"({{{key}}})\"), EnableQuery]");
            O("public SingleResult<{0}> Get([FromODataUri] {1} {2})", type.ClassName, keyProp.Type.Name, key);
            Begin();
            O($"return SingleResult.Create(_db.QuerySingle({key}));");
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
                O($"public async Task<IActionResult> {action}([FromODataUri] {key.Type.Name} key, ODataActionParameters parameters)");
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
                O($"public async Task<IActionResult> {action}([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model)");
                Begin();
                O($"return await UpdateCore({key.VName}, model, {mask});");
                End();
            }
        }

        void GenerateUpdateSingleMain()
        {
            var key = Type.Key;

            O();
            O($"async Task<IActionResult> UpdateCore([FromODataUri] {key.Type.Name} {key.VName}, {Type.ClassName} model, MojDataGraphMask mask, string group = null)");
            Begin();

            O("if (!ModelState.IsValid) return BadRequest(ModelState);");

            // Disable loading of referenced entities.
            O("_db.ReferenceLoading(false);");

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
                O("[ODataRoute(\"({{{0}}})\"), System.Web.Http.AcceptVerbs(\"PATCH\", \"MERGE\")]", key.VName);
                O("public async Task<IActionResult> Patch([FromODataUri] {0} {1}, Delta<{2}> delta, CancellationToken cancellationToken)",
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
            OApiActionAuthAttribute(Type, "Delete");
            O("[HttpDelete]");
            O("[ODataRoute(\"({{{0}}})\")]", key.VName);
            O("public async Task<IActionResult> Delete([FromODataUri] {0} {1})", key.Type.Name, key.VName);
            Begin();
            O("_db.ReferenceLoading(false);");
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
            // KABU TODO: IMPORTANT: Why can't we return Ok() here? Which JS library produces errors when using Ok()?
            O("return NoContent();");
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
            O("[ApiActionAuth(Part = \"{0}\", Group = {1}, Action = \"{2}\")]",
                view.TypeConfig.Name,
                MojenUtils.ToCsValue(view.Group),
                action);
        }

        public void OApiActionAuthAttribute(MojType type, string action)
        {
            O("[ApiActionAuth(Part = \"{0}\", Action = \"{1}\")]",
                type.Name, action);
        }
    }
}