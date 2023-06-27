using System.IO;
#nullable enable

namespace Casimodo.Mojen;

public class TsXClassGenOptions
{
    public string[]? OutputDirPaths { get; set; }
    public bool OutputSingleFile { get; set; } = true;
    public string[]? IncludeTypes { get; set; }

    public bool GenerateInterfaces { get; set; } = true;
    public bool GenerateClasses { get; set; } = true;
    public bool UseODataClasses { get; set; } = true;

    public bool PrefixInterfaces { get; set; } = true;

    public string? SingleFileName { get; set; }

    public bool UseCamelCase { get; set; } = true;

    public bool UseDefaultValues { get; set; } = true;

    public Func<MojType, string>? FormatFileName { get; set; }
}

public class TsXClassGen : TsGenBase
{
    public TsXClassGen()
        : this(new TsXClassGenOptions())
    { }

    public TsXClassGen(TsXClassGenOptions options)
    {
        Guard.ArgNotNull(options);

        Options = options;
        Scope = "Context";
    }

    public TsXClassGenOptions Options { get; }

    public WebDataLayerConfig WebConfig { get; set; } = default!;

    public List<string>? IncludedTypeNames { get; set; }

    protected override void GenerateCore()
    {
        WebConfig = App.Get<WebDataLayerConfig>();

        var outputDirPaths = new List<string>();
        if (Options.OutputDirPaths?.Length > 0)
        {
            outputDirPaths.AddRange(Options.OutputDirPaths);
        }
        else
        {
            outputDirPaths.Add(WebConfig.TypeScriptDataDirPath);
        }

        foreach (var outputDirPath in outputDirPaths)
        {
            IncludedTypeNames = Options.IncludeTypes?.ToList();

            var types = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.IsTenant)
                .Where(x => IncludedTypeNames == null || IncludedTypeNames.Contains(x.Name))
                .ToList();

            if (Options.OutputSingleFile)
            {
                var fileName = Options?.SingleFileName ?? "DataTypes.generated";
                fileName += ".ts";

                PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                {
                    O("/* tslint:disable:no-inferrable-types max-line-length */");

                    var excludedBaseTypes = types.Where(x => x.HasBaseClass && !types.Contains(x.BaseClass))
                        .Select(x => x.BaseClass)
                        .Distinct()
                        .ToList();

                    if (excludedBaseTypes.Any())
                    {
                        foreach (var type in excludedBaseTypes)
                            OImportType(type);
                        O();
                    }

                    foreach (var type in types)
                    {
                        Generate(type, import: false);
                        O();
                    }
                });
            }
            else
            {
                foreach (var type in types)
                {
                    var fileName = Options?.FormatFileName?.Invoke(type) ?? type.Name + ".generated";
                    fileName += ".ts";

                    PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                    {
                        OImportType(type.BaseClass);
                        Generate(type, import: true);
                    });
                }
            }
        }
    }

    public void OImportType(MojType type)
    {
        if (type == null)
            return;

        var typeNames = new List<string>();

        if (Options.GenerateClasses)
            typeNames.Add(type.Name);

        if (Options.GenerateInterfaces && Options.PrefixInterfaces)
            typeNames.Add($"I{type.Name}");

        O($@"import {{ {typeNames.Join(", ")} }} from ""./{type.Name.FirstLetterToLower()}"";");
    }

    private bool HasDoc(MojProp prop)
    {
        return !(prop.Summary.Descriptions.Count == 0 &&
            prop.Summary.Remarks.Count == 0 &&
            !prop.IsKey &&
            !prop.IsExcludedFromDb);
    }

    public void OTsDoc(MojProp prop)
    {
        if (!HasDoc(prop))
            return;

        O("/**");

        var hasContent = prop.Summary.Descriptions.Count != 0;

        foreach (var text in prop.Summary.Descriptions)
            O(" * " + text);

        if (prop.IsKey || prop.IsExcludedFromDb)
        {
            if (hasContent) O();
            if (prop.IsKey) O(" * Is Key");
            if (prop.IsExcludedFromDb) O(" *  Is NotMapped");

            hasContent = true;
        }

        if (prop.Summary.Remarks.Count != 0)
        {
            if (hasContent)
                O();
            O(" * @remarks");
            foreach (var text in prop.Summary.Remarks)
                O(" * " + text);
        }

        O(" */");
    }

    public void Generate(MojType type, bool import)
    {
        var localProps = type.GetLocalProps(custom: false)
            // Exclude hidden EF navigation collection props.
            .Where(x => !x.IsHiddenCollectionNavigationProp)
            .Where(x =>
                !x.Type.IsDirectOrContainedMojType ||
                IncludedTypeNames == null ||
                IncludedTypeNames.Contains(x.Type.DirectOrContainedTypeConfig.Name))
            .ToList();

        if (import)
            foreach (var prop in localProps.Where(x => x.Type.IsMojType))
                OImportType(prop.Type.TypeConfig);

        string? interfaceName = null;

        if (Options.GenerateInterfaces)
        {
            interfaceName = BuildInterfaceName(type.Name);
            var interfaceBaseName = !type.HasBaseClass
                ? ""
                : BuildInterfaceName(type.BaseClass.Name);

            OTsInterface(interfaceName,
                extends: interfaceBaseName,
                content: () =>
                {
                    OClassOrInterfaceProps(type, localProps, isInterface: true);
                });
        }

        if (Options.GenerateClasses)
        {
            O();
            OTsClass(type.Name,
                extends: type.HasBaseClass
                    ? type.BaseClass.Name
                    : null,
                implements: interfaceName != null
                    ? new string[] { interfaceName }
                    : null,
                propertyInitializer: true,
                constructor: () =>
                {
                    if (Options.UseODataClasses &&
                        (type.IsEntity() ||
                        (type.IsComplex() && type.UsingGenerators.Any(x => x.Type == typeof(ODataConfigGen)))))
                    {
                        // TODO: Find a way to emit this only when used in the context of OData.
                        O($@"(this as any)[""@odata.type""] = ""#{WebConfig.ODataNamespace}.{type.ClassName}"";");
                    }
                },
                content: () =>
                {
                    OClassOrInterfaceProps(type, localProps, isInterface: false);
                });
        }
    }

    void OClassOrInterfaceProps(MojType type, List<MojProp> localProps, bool isInterface)
    {
        var tenantKey = type.FindTenantKey();

        // Local properties
        foreach (var prop in localProps)
        {
            if (prop == tenantKey)
                // Don't expose tenant information.
                continue;

            if (HasDoc(prop))
            {
                if (Options.GenerateInterfaces == isInterface)
                {
                    OTsDoc(prop);
                }

                if (Options.GenerateInterfaces && !isInterface)
                {
                    O("/** @inheritdoc */");
                }
            }

            var propName = Options.UseCamelCase
                ? prop.VName
                : prop.Name;

            if (prop.Type.IsCollection)
            {
                // This will use Partial<T> for references.
                string propTypeName = BuildPropTypeName(prop.Type, isInterface);

                if (!isInterface)
                {
                    var initializer = Moj.ToJsCollectionInitializer(prop.Type);
                    O($"{propName}: {propTypeName} = {initializer};");
                }
                else
                    O($"{propName}: {propTypeName};");
            }
            else
            {
                // This will use Partial<T> for references.
                string propTypeName = BuildPropTypeName(prop.Type, isInterface);

                string defaultValue = "null";

                if (Options.UseDefaultValues)
                {
                    // Don't auto-generate GUIDs for IDs.
                    if (!prop.IsKey)
                        defaultValue = GetJsDefaultValue(prop);
                }
                else if (prop.Type.IsBoolean && !prop.Type.IsNullableValueType)
                {
                    // Always initialize non nullable booleans.
                    defaultValue = "false";
                }

                if (defaultValue == "null")
                {
                    propTypeName += " | null";
                }

                if (!isInterface)
                    O($"{propName}: {propTypeName} = {defaultValue};");
                else
                    O($"{propName}: {propTypeName};");
            }
        }
    }

    string BuildPropTypeName(MojPropType propType, bool isInterface)
    {
        if (propType.IsDirectOrContainedMojType)
        {
            var name = BuildInterfaceName(propType.DirectOrContainedTypeConfig.Name);

            name = "Partial<" + name + ">";

            if (propType.IsCollection)
                name += "[]";

            return name;
        }
        else
        {
            return Moj.ToJsType(propType);
        }
    }

    string BuildInterfaceName(string interfaceName)
    {
        return Options.PrefixInterfaces
            ? "I" + interfaceName
            : interfaceName;
    }

}