using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class TsXClassGenOptions
    {
        public string OutputDirPath { get; set; }
        public bool OutputSingleFile { get; set; }
        public string[] IncludeTypes { get; set; }

        public bool GenerateInterfaces { get; set; }

        public bool PrefixInterfaces { get; set; }

        public string SingleFileName { get; set; }

        public bool UseDefaultValues { get; set; }

        public Func<MojType, string> FormatFileName { get; set; }
    }

    public class TsXClassGen : DataLayerGenerator
    {
        readonly TsXClassGenOptions _options;

        public TsXClassGen(TsXClassGenOptions options = null)
        {
            _options = options;
            Scope = "Context";
        }

        public WebDataLayerConfig WebConfig { get; set; }

        public List<string> IncludedTypeNames { get; set; }

        protected override void GenerateCore()
        {
            WebConfig = App.Get<WebDataLayerConfig>();
            var outputDirPath = _options?.OutputDirPath ?? WebConfig.TypeScriptModuleEntitiesDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            IncludedTypeNames = _options?.IncludeTypes?.ToList();

            var types = App.GetTypes(MojTypeKind.Entity, MojTypeKind.Complex)
                .Where(x => !x.IsTenant)
                .Where(x => IncludedTypeNames == null || IncludedTypeNames.Contains(x.Name))
                .ToList();

            if (_options?.OutputSingleFile == true)
            {
                var fileName = _options?.SingleFileName ?? "DataTypes.generated";
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
                    var fileName = _options?.FormatFileName?.Invoke(type) ?? type.Name + ".generated";
                    fileName += ".ts";

                    PerformWrite(Path.Combine(outputDirPath, fileName), () =>
                    {
                        OImportType(type.BaseClass);
                        Generate(type, import: true);
                    });
                }
            }
        }

        public void OImportType(MojType type)
        {
            if (type == null)
                return;

            string[] typeNames = _options.GenerateInterfaces && _options.PrefixInterfaces
                ? typeNames = new[] { type.Name, "I" + type.Name }
                : typeNames = new[] { type.Name };

            O($"import {{ {typeNames.Join(", ")} }} from './{type.Name.FirstLetterToLower()}';");
        }

        public void OTsDoc(MojProp prop)
        {
            if (prop.Summary.Descriptions.Count == 0 &&
                prop.Summary.Remarks.Count == 0 &&
                !prop.IsKey && !prop.IsExcludedFromDb)
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

            string iname = null;

            if (_options.GenerateInterfaces)
            {
                iname = _options.PrefixInterfaces ? "I" + type.Name : type.Name;
                var ibaseName = !type.HasBaseClass
                    ? ""
                    : _options.PrefixInterfaces ? "I" + type.BaseClass.Name : type.BaseClass.Name;

                OTsInterface(iname,
                    extends: ibaseName,
                    content: () =>
                    {
                        OClassOrInterfaceProps(type, localProps, isInterface: true);
                    });
            }

            O();
            OTsClass(type.Name,
                extends: type.HasBaseClass ? type.BaseClass.Name : null,
                implements: _options.GenerateInterfaces ? new string[] { iname } : null,
                propertyInitializer: true,
                constructor: () =>
                {
                    // TODO: Find a way to emit this only when used in the context of OData.
                    O($"this['@odata.type'] = '#{WebConfig.ODataNamespace}.{type.ClassName}';");
                },
                content: () =>
                {
                    OClassOrInterfaceProps(type, localProps, isInterface: false);
                });
        }

        void OClassOrInterfaceProps(MojType type, List<MojProp> localProps, bool isInterface)
        {
            var tenantKey = type.FindTenantKey();

            // Local properties                  
            MojProp prop;
            for (int i = 0; i < localProps.Count; i++)
            {
                prop = localProps[i];

                if (prop == tenantKey)
                    // Don't expose tenant information.
                    continue;

                //if (i > 0)
                //    O();

                if (!isInterface)
                    OTsDoc(prop);

                if (prop.Type.IsCollection)
                {
                    // This will use Partial<T> for references.
                    string propTypeName = Moj.ToTsType(prop.Type, partial: true);

                    //if (prop.Type.IsDirectOrContainedMojType)
                    //{
                    //    propTypeName = $"Partial<{prop.Type.DirectOrContainedTypeConfig.Name}>";
                    //}
                    //else
                    //    propTypeName = Moj.ToTsType(prop.Type);

                    if (!isInterface)
                    {
                        var initializer = Moj.ToJsCollectionInitializer(prop.Type);
                        O($"{prop.Name}: {propTypeName} = {initializer};");
                    }
                    else
                        O($"{prop.Name}: {propTypeName};");
                }
                else
                {
                    // This will use Partial<T> for references.
                    string propTypeName = Moj.ToTsType(prop.Type, partial: true);
                    //if (prop.Type.IsMojType)
                    //    propTypeName = $"Partial<{propTypeName}>";

                    string defaultValue = "null";

                    if (_options?.UseDefaultValues == true)
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

                    if (!isInterface)
                        O($"{prop.Name}: {propTypeName} = {defaultValue};");
                    else
                        O($"{prop.Name}: {propTypeName};");
                }
            }
        }
    }
}