using System.IO;
#nullable enable

namespace Casimodo.Mojen;

public class TsXClassGenOptions
{
    public string[]? TypeNames { get; set; }
    public bool OutputSingleFile { get; set; } = true;
    public string? SingleFileName { get; set; }
    public string[]? OutputDirPaths { get; set; }
    public bool GenerateInterfaces { get; set; } = true;
    public bool GeneratePartialInterfaceAliases { get; set; }
    public bool GenerateClasses { get; set; } = true;
    public bool UseODataClasses { get; set; } = true;
    public bool PrefixInterfaces { get; set; } = true;
    public bool UseCamelCase { get; set; } = true;
    public bool UseDefaultValues { get; set; } = true;
    public bool UseStringForByteArray { get; set; }
    public bool InitializeByteArrayToNull { get; set; }
    public Func<MojType, string>? FormatFileName { get; set; }
}

public class TsXClassGen : TsGenBase
{
    readonly TsXClassGenOptions _options;

    public TsXClassGen(TsXClassGenOptions options)
    {
        Guard.ArgNotNull(options);

        _options = options;
        Scope = "Context";
    }

    public WebDataLayerConfig WebConfig { get; set; } = default!;

    public List<string>? IncludedTypeNames { get; set; }

    static int GetInheritanceDepth(MojType type)
    {
        int depth = 0;
        while ((type = type.BaseClass) != null)
            depth++;

        return depth;
    }

    protected override void GenerateCore()
    {
        WebConfig = App.Get<WebDataLayerConfig>();

        var outputDirPaths = new List<string>();
        if (_options.OutputDirPaths?.Length > 0)
        {
            outputDirPaths.AddRange(_options.OutputDirPaths);
        }
        else
        {
            outputDirPaths.Add(WebConfig.TypeScriptDataDirPath);
        }

        IncludedTypeNames = _options.TypeNames?.ToList();

        var types = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
            .Where(x => !x.IsTenant)
            .Where(x => IncludedTypeNames == null || IncludedTypeNames.Contains(x.Name))
            .OrderBy(x => GetInheritanceDepth(x))
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var outputDirPath in outputDirPaths)
        {
            if (_options.OutputSingleFile)
            {
                var fileName = _options.SingleFileName ?? "dataTypes";
                fileName += ".ts";

                PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                {
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
                    var fileName = _options.FormatFileName?.Invoke(type) ?? type.Name + ".generated";
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

        if (_options.GenerateClasses)
            typeNames.Add(type.Name);

        if (_options.GenerateInterfaces && _options.PrefixInterfaces)
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
            .Where(x =>
                !x.IsHiddenCollectionNavigationProp &&
                !x.IsExcludedFromDb)
            .Where(x =>
                !x.Type.IsDirectOrContainedMojType ||
                IncludedTypeNames == null ||
                IncludedTypeNames.Contains(x.Type.DirectOrContainedTypeConfig.Name))
            .ToList();

        if (import)
            foreach (var prop in localProps.Where(x => x.Type.IsMojType))
                OImportType(prop.Type.TypeConfig);

        string? interfaceName = null;

        if (_options.GenerateInterfaces)
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

            if (_options.GeneratePartialInterfaceAliases)
            {
                O();
                var partialInteraceAliasName = $"Partial{type.Name}";
                if (_options.PrefixInterfaces)
                {
                    partialInteraceAliasName = "I" + partialInteraceAliasName;
                }
                O($"export type {partialInteraceAliasName} = Partial<{interfaceName}>;");
            }
        }

        if (_options.GenerateClasses)
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
                    if (_options.UseODataClasses &&
                        (type.IsEntity() ||
                        (type.IsComplex() && type.UsingGenerators.Any(x => x.Type == typeof(ODataConfigGen)))))
                    {
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
                if (_options.GenerateInterfaces == isInterface)
                {
                    OTsDoc(prop);
                }

                if (_options.GenerateInterfaces && !isInterface)
                {
                    O("/** @inheritdoc */");
                }
            }

            var propName = _options.UseCamelCase
                ? prop.VName
                : prop.Name;

            if (prop.Type.IsCollection)
            {
                // This will use Partial<T> for references.
                string propTypeExpression = BuildPropTypeName(prop.Type);

                if (prop.Type.IsByteArray && _options.InitializeByteArrayToNull)
                {
                    propTypeExpression += " | null";
                }

                if (!isInterface)
                {
                    string intializerValue;

                    if (prop.Type.IsByteArray)
                    {
                        intializerValue = _options.InitializeByteArrayToNull
                            ? "null"
                            : _options.UseStringForByteArray
                                ? "\"\""
                                : "new Uint8Array()";
                    }
                    else
                    {
                        intializerValue = Moj.ToJsCollectionInitializer(prop.Type);
                    }
                    O($"{propName}: {propTypeExpression} = {intializerValue};");
                }
                else
                    O($"{propName}: {propTypeExpression};");
            }
            else
            {
                // This will use Partial<T> for references.
                string? propTypeExpression = BuildPropTypeName(prop.Type);

                string defaultValue = "null";

                if (_options.UseDefaultValues)
                {
                    // Don't auto-generate GUIDs for IDs.
                    if (!prop.IsKey)
                    {
                        defaultValue = GetJsDefaultValue(prop);

                        // Omit type in order to satisfy the ESList rule:
                        //   "Type boolean trivially inferred from a boolean literal, remove type annotation"
                        if (!isInterface && defaultValue != "null")
                            propTypeExpression = null;
                    }
                }
                else if (prop.Type.IsBoolean && !prop.Type.IsNullableValueType)
                {
                    // Always initialize non nullable booleans.
                    defaultValue = "false";
                    // Omit type in order to satisfy the ESList rule:
                    //   "Type boolean trivially inferred from a boolean literal, remove type annotation"
                    if (!isInterface)
                        propTypeExpression = null;
                }

                if (defaultValue == "null")
                {
                    propTypeExpression += " | null";
                }

                if (!isInterface)
                    O($"{propName}{(propTypeExpression != null ? $" : {propTypeExpression}" : "")} = {defaultValue};");
                else
                    O($"{propName}: {propTypeExpression};");
            }
        }
    }

    string BuildPropTypeName(MojPropType propType)
    {
        if (propType.IsDirectOrContainedMojType)
        {
            var name = BuildInterfaceName(propType.DirectOrContainedTypeConfig.Name);

            name = $"Partial<{name}>";

            if (propType.IsCollection)
                name += "[]";

            return name;
        }
        else
        {
            if (propType.IsByteArray && _options.UseStringForByteArray)
            {
                return "string";
            }
            else if (propType.DateTimeInfo?.IsDateOnly == true)
            {
                // TODO: Currently serializing DateOnly to string.
                return "string";
            }
            else
            {
                return Moj.ToJsType(propType);
            }
        }
    }

    string BuildInterfaceName(string interfaceName)
    {
        return _options.PrefixInterfaces
            ? "I" + interfaceName
            : interfaceName;
    }

}