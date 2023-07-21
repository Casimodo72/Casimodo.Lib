using System.IO;
using System.Xml.Linq;

namespace Casimodo.Mojen
{
    /// <summary>
    /// Generates ASP.NET Web API OData controllers.
    /// </summary>
    public class CoreODataControllerGen : WebPartGenerator
    {
        public CoreODataControllerGen()
        {
            Scope = "App";
            Lang = "C#";
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
                    ODataConfig.WebODataControllerDirPath,
                    this.GetODataControllerName(controller.TypeConfig) + ".generated.cs");

                PerformWrite(controllerFilePath, () => GenerateController());
            }
        }

        public override string GetWebRepositoryName(MojType type)
        {
            return type.PluralName + "Repository";
        }

        string GetControllerBaseClass()
        {
            var dataContext = App.GetDataLayerConfig(Type.DataContextName);
            var repoName = GetWebRepositoryName(Type);

            return $@"StandardODataControllerBase<{repoName}, {dataContext.DbContextName}, {Type.Name}, {Type.Key.Type.Name}>";
        }

        string RepoVar()
        {
            return "_repo";
        }

        void GenerateController()
        {
            Options = Controller.GetGeneratorConfig<ODataControllerOptions>() ?? new ODataControllerOptions();

            var key = Type.Key;

            var dataContext = App.GetDataLayerConfig(Type.DataContextName);
            var dbContextName = dataContext.DbContextName;

            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading",
                "System.Threading.Tasks",
                "Microsoft.AspNetCore.OData",
                "Microsoft.AspNetCore.OData.Query",
                "Microsoft.AspNetCore.OData.Routing",
                // For [FromODataUri]:
                "Microsoft.AspNetCore.OData.Formatter",
                // For SingleResult<T>:
                "Microsoft.AspNetCore.OData.Results",
                "System.Data",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                "Casimodo.Lib.Web.Auth",
                "Microsoft.AspNetCore.Mvc",
                GetAllDataNamespaces()
            );

            ONamespace(ODataConfig.WebODataControllerNamespace);

            foreach (var attr in Controller.Attrs)
            {
                O(BuildAttr(attr));
            }

            var controllerName = this.GetODataControllerName(Type);

            // Generic entity repository
            var repoName = GetWebRepositoryName(Type);

            O($@"[Route({ODataConfig.ControllerRoutePrefixExpression} + ""{Type.PluralName}"")]");
            O($"public partial class {controllerName} : {GetControllerBaseClass()}");
            Begin();

            // Constructor
            O($@"public {controllerName}({dbContextName} db)");
            O($@"    : base(db, new {repoName}(db))");
            O("{");
            Push();

            O("InitializeExtended();");
            Pop();
            O("}");

            O();
            O("partial void InitializeExtended();");

            if (!Options.IsEmpty)
            {
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

            // Let other generators inject further actions.
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
            O($"[HttpPost(\"{action}\")]");
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
                OAttribute(HttpVerb.Post);
                O($"public async Task<IActionResult> Post([FromBody] {Type.ClassName} model)");
                Begin();
                O("return await CreateCore(model);");
                End();
            }
        }

        // Read ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        string GetMethodNs()
        {
            if (ODataConfig.IsMethodNamespaceQualified)
                return ODataConfig.Namespace + ".";
            else
                return "";
        }

        void OEnableQueryAttribute()
        {
            var enableQueryAttrName = ODataConfig.EnableQueryAttributeName ?? "EnableQuery";
            Oo($"[{enableQueryAttrName}(");

            if (Options.MaxPageSize != null)
                o($"PageSize = {Options.MaxPageSize}, ");

            oO($"AllowedQueryOptions = LookupQueryOptions, MaxExpansionDepth = {Options.MaxExpansionDepth})]");
        }

        void GenerateRead(MojType type, MojProp keyProp, string key)
        {
            // GET: odata/Entities
            // ODataQueryOptions: https://aspnet.codeplex.com/SourceControl/latest#Samples/WebApi/OData/v4/ODataQueryableSample/Controllers/OrdersController.cs
            //   var settings = new ODataQuerySettings();
            //   settings.HandleNullPropagation = HandleNullPropagationOption.Default;
            //   settings.EnableConstantParameterization = true;

            // Default query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            OEnableQueryAttribute();
            OAttribute(HttpVerb.Get);
            O($"public IActionResult {ODataConfig.Query}()");
            Begin();
            O($"return Ok(CustomFilter({RepoVar()}.Query()));");
            End();

            // Distinct by property query function.
            O();
            OApiActionAuthAttribute(Type, "View");
            OEnableQueryAttribute();
            OAttribute(HttpVerb.Get, $@"{GetMethodNs()}{ODataConfig.QueryDistinct}(on={{on}})");
            O($"public IActionResult {ODataConfig.QueryDistinct}(string on)");
            Begin();
            O($"return Ok(CustomFilter({RepoVar()}.Query()).GroupBy(ExpressionHelper.GetGroupKey<{type.ClassName}>(on.Trim('\\''))).Select(g => g.FirstOrDefault()));");
            End();

            // GET: odata/Entities(x)
            O();
            OApiActionAuthAttribute(Type, "View");
            OEnableQueryAttribute();
            OAttribute(HttpVerb.Get, $"{{{key}}}");
            O($"public SingleResult<{type.ClassName}> Get([FromODataUri] {keyProp.Type.Name} {key})");
            Begin();
            O($"return SingleResult.Create({RepoVar()}.QuerySingle({key}));");
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
                O($"[HttpPost(\"{action}\")]");
                // NOTE: The ID parameter *must* be named "key" by convention.
                // Otherwise the action won't be found by the OData Web API machinery.
                O($"public async Task<IActionResult> {action}([FromODataUri] {key.Type.Name} key, ODataActionParameters parameters)");
                Begin();
                O($"return await UpdateCore(key, parameters?.Values?.FirstOrDefault() as {Type.ClassName}, {mask}, \"{group}\");");
                End();
            }
            else
            {
                // PUT: odata/ControllerName/x
                // Async
                OApiActionAuthAttribute(editorView, "Modify");
                OAttribute(HttpVerb.Put, $"{{{key.VName}}}");
                O($"public async Task<IActionResult> {action}([FromODataUri] {key.Type.Name} {key.VName}, [FromBody] {Type.ClassName} model)");
                Begin();
                O($"return await UpdateCore({key.VName}, model, {mask});");
                End();
            }
        }

        // Patch ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GeneratePatch()
        {
            // KABU TODO: REVISIT: Currently disabled.
            // PATCH, MERGE: odata/ControllerName(x)
            // Async, delta
            if (false)
            {
#pragma warning disable CS0162 // Unreachable code detected
                var key = Type.Key;
#pragma warning restore CS0162 // Unreachable code detected
                O();
                O("[AcceptVerbs(\"PATCH\", \"MERGE\")]");
                O($"[Route(\"({{{key.VName}}})\"), System.Web.Http.AcceptVerbs(\"PATCH\", \"MERGE\")]");
                O($"public async Task<IActionResult> Patch([FromODataUri] {key.Type.Name} {key.VName}, Delta<{Type.ClassName}> delta, CancellationToken cancellationToken)");
                Begin();
                O("Validate(delta.GetEntity());");
                O("if (!ModelState.IsValid) return BadRequest(ModelState);");
                O($"return Updated(await {RepoVar()}.PatchAsync({key.VName}, delta, save: true, token: cancellationToken));");
                End();
            }
        }

        // Delete ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void GenerateDelete()
        {
            var key = Type.Key;

            // DELETE: odata/ControllerName/x
            // Async
            O();
            OApiActionAuthAttribute(Type, "Delete");
            OAttribute(HttpVerb.Delete, $"{{{key.VName}}}");
            O($"public async Task<IActionResult> Delete([FromODataUri] {key.Type.Name} {key.VName})");
            Begin();
            // Operate on the entity repository (i.e. not the model repository).
            var repository = Type.Kind == MojTypeKind.Model ? $"{RepoVar()}.Entities" : RepoVar();
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

            var repository = Type.Kind == MojTypeKind.Model ? $"{RepoVar()}.Entities" : RepoVar();
            O($"{repository}.Update(item);");
        }

        void GeneratePhysicalDelete()
        {
            O("// Delete physically");
            var repository = Type.Kind == MojTypeKind.Model ? $"{RepoVar()}.Entities" : RepoVar();
            O($"{repository}.Delete(item, isPhysicalDeletionAuthorized: true);");
        }

        public void OApiActionAuthAttribute(MojViewConfig view, string action)
        {
            OFormat("[ApiActionAuth(Part = \"{0}\", Group = {1}, Action = \"{2}\")]",
                view.TypeConfig.Name,
                Moj.CS(view.Group),
                action);
        }

        public void OApiActionAuthAttribute(MojType type, string action)
        {
            O($"[ApiActionAuth(Part = \"{type.Name}\", Action = \"{action}\")]");
        }
    }
}