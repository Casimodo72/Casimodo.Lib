using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public class LegacyGfaImportMapGen : DataLayerGenerator
    {
        public LegacyGfaImportMapGen()
        {
            Scope = "Context";
        }

        public LegacyDataMappingConfig Config { get; set; }

        List<MojType> TargetTypes { get; set; } = new List<MojType>();

        List<SourceType> SourceTypes { get; set; } = new List<SourceType>();

        protected override void GenerateCore()
        {
            Config = App.Get<LegacyDataMappingConfig>();
            if (Config == null || Config.OutputDirPath == null)
                return;

            PerformWrite(Path.Combine(Config.OutputDirPath, "ImportMappings.generated.cs"),
                () => GenerateLegacyImportMappings());
        }

        public void GenerateLegacyImportMappings()
        {
            TargetTypes.AddRange(App.AllEntities.Where(x => x.VerMap.Is));

            ReadLegacyTypes();

            O("extern alias old;");
            O("extern alias current;");
            O("using sns = old.Gfa.Data;");
            O("using tns = current.Ga.Data;");
            OUsing("System", "System.Collections.Generic", "System.Linq"); //, entities.First().Namespace);

            ONamespace("Gfa.LegacyData");

            O("public partial class ImportMappings");
            Begin();

            if (Config.SourceDbContextName != null)
            {
                O($"public sns.{Config.SourceDbContextName} SourceDb {{ get; set; }}");
            }

            if (Config.TargetDbContextName != null)
            {
                O($"public tns.{Config.TargetDbContextName} TargetDb {{ get; set; }}");
            }

            foreach (var sourceType in SourceTypes)
                Prepare(sourceType);

            // Remove ignored source types.
            SourceTypes.RemoveWhere(x => x.TargetType == null);

            O();
            O($"partial void OnImportedAny(object target);");

            O();
            foreach (var sourceType in SourceTypes)
                GenerateMapping(sourceType);

            O();
            foreach (var sourceType in SourceTypes)
                GenerateCollectionImport(sourceType);

            O();
            foreach (var sourceType in SourceTypes)
                GenerateImport(sourceType);

            End();
            End();

            O();

            //foreach (var sourceType in SourceTypes.Where(x => x.Props.Any(prop => prop.Name == "ProjectId")))
            //    O($"// Import(db.{sourceType.TargetType.VerMap.SourcePluralName}.Where(x => x.ProjectId == projectId));");

            //foreach (var sourceType in SourceTypes.Where(x => x.Props.Any(prop => prop.Name == "ProjectId")))
            //    O($"// db.Database.ExecuteSqlCommand(\"delete {sourceType.TargetType.PluralName}\");");
        }

        void GenerateCollectionImport(SourceType sourceType)
        {
            var targetType = sourceType.TargetType;

            O();
            O($"public void Import(IEnumerable<sns.{sourceType.Name}> sources)");
            Begin();
            O($"foreach (var source in sources) Import(source);");
            End();
        }

        void GenerateImport(SourceType sourceType)
        {
            var targetType = sourceType.TargetType;

            O();
            O($"public void Import(sns.{sourceType.Name} source)");
            Begin();

            O($"var target = new tns.{targetType.ClassName}();");
            O($"Map(source, target);");            

            if (Config.TargetDbContextName != null)
            {
                O($"TargetDb.Set<tns.{targetType.ClassName}>().Add(target);");
            }

            O($"OnImported(target);");
            O($"OnImportedAny(target);");

            End();

            O();
            O($"partial void OnImported(tns.{targetType.ClassName} {targetType.VName});");
        }

        void GenerateMapping(SourceType sourceType)
        {
            var targetType = sourceType.TargetType;

            O();
            O($"public void Map(sns.{sourceType.Name} source, tns.{targetType.ClassName} target)");
            Begin();

            if (sourceType.MissingTargetProps.Any())
            {
                OError($">> Missing targets:");
                foreach (var missingProp in sourceType.MissingTargetProps)
                    OError($"      {missingProp}");
                O();
            }

            if (sourceType.MissingSourceProps.Any())
            {
                OError($"<< Missing sources:");
                foreach (var missingProp in sourceType.MissingSourceProps)
                    OError($"      {missingProp}");
                O();
            }

            if (sourceType.IgnoredSourceProps.Any())
            {
                OInfo($"<< Ignored sources:");
                foreach (var missingProp in sourceType.IgnoredSourceProps)
                    OInfo($"      {missingProp}");
                O();
            }

            // Target props with no source.
            var newTargetProps = sourceType.TargetProps.Where(x => x.Map.HasSource != true).ToArray();
            if (newTargetProps.Any())
            {
                OInfo(">> New targets:");
                foreach (var newProp in newTargetProps)
                    OInfo($"      {newProp.Name}");
                O();
            }

            // Source to target assignment.
            foreach (var targetProp in sourceType.TargetProps)
            {
                var sourceProp = targetProp.SourceProp;
                if (sourceProp == null)
                    O("// No source");

                O("target.{0} = {1}{2};", targetProp.Name, BuildCast(targetProp, sourceProp), BuildAssignment(targetProp, sourceProp));
            }
            End();
        }

        void Prepare(SourceType sourceType)
        {
            var targetType = sourceType.TargetType = TargetTypes.FirstOrDefault(x => sourceType.Name == (x.VerMap.SourceName ?? x.Name));
            if (targetType == null)
            {
                O();
                if (Config.IgnoredSourceTypes.Contains(sourceType.Name))
                {
                    OInfo($"<< Ignored source type '{sourceType.Name}'");
                }
                else
                {
                    OError($">> Missing target type for '{sourceType.Name}'");
                }
                O();
            }
            else
            {
                // Check if ignored source properties exist.
                foreach (var ignoredSourceProp in targetType.VerMap.IgnoreSourceProps)
                {
                    if (!sourceType.Props.Any(x => x.Name == ignoredSourceProp))
                    {
                        OError($"Type: {targetType.ClassName}: Ignored source prop '{ignoredSourceProp}' not found.");
                    }
                }

                // Get all mapped target properties.
                var targetProps =
                    sourceType.TargetProps =
                        targetType.GetProps().Where(x => x.VerMap.Is)
                            .Select(x => BuildTargetProp(targetType, x))
                            .ToList();

                // Check valid target prop overrides.
                foreach (var targetPropOverride in targetType.VerMap.ToPropOverrides)
                {
                    if (!targetProps.Any(x => x.Prop.Name == targetPropOverride.TargetName))
                        OError($"Type: {targetType.ClassName}: No target prop '{targetPropOverride.TargetName}' found for override.");
                }

                foreach (var sourceProp in sourceType.Props)
                {
                    // Find corresponding target properties.
                    sourceProp.TargetProp = targetProps.SingleOrDefault(prop => MapsTo(sourceProp, prop));

                    if (sourceProp.TargetProp != null)
                    {
                        sourceProp.TargetProp.SourceProp = sourceProp;

                        // If target prop found, then the source prop must not be ignored.
                        if (targetType.VerMap.IgnoreSourceProps.Contains(sourceProp.Name))
                        {
                            OError($"Type: {targetType.ClassName}: Mismatch: target prop '{sourceProp.Name}' has previous but is ignored.");
                        }
                    }
                    else
                    {
                        if (targetType.VerMap.IgnoreSourceProps.Contains(sourceProp.Name))
                        {
                            // This source prop will be ignored.
                            sourceType.IgnoredSourceProps.Add(sourceProp.Name);
                        }
                        else
                        {
                            // Source prop missing.
                            sourceType.MissingTargetProps.Add(sourceProp.Name);
                            OError($"Type: {targetType.ClassName}: >> Missing target prop for '{sourceProp.Name}'");
                        }
                    }
                }

                foreach (var targetProp in targetProps)
                {
                    if (targetProp.SourceProp == null &&
                        targetProp.Map.HasSource == true)
                    {
                        OError($"Type: {targetType.ClassName}: << Missing source prop for '{targetProp.Name}'");
                        sourceType.MissingSourceProps.Add(targetProp.Name);
                    }
                }
            }
        }

        TargetProp BuildTargetProp(MojType type, MojProp prop)
        {
            var map = prop.VerMap;
            var mapOverride = type.VerMap.ToPropOverrides.SingleOrDefault(x => x.TargetName == prop.Name);
            if (mapOverride != null)
            {
                map = MojVersionMapping.CloneFrom(map);
                if (mapOverride.HasSource != null)
                    map.HasSource = mapOverride.HasSource;
                if (map.ValueExpression == null || mapOverride.ValueExpression != null)
                    map.ValueExpression = mapOverride.ValueExpression;
            }

            return new TargetProp
            {
                Type = type,
                Prop = prop,
                Name = prop.Name,
                Map = map
            };
        }

        bool MapsTo(SourceProp sourceProp, TargetProp targetProp)
        {
            if (targetProp.Prop.VerMap.HasSource != true)
                return false;

            var name = targetProp.Prop.VerMap.SourceName ?? targetProp.Prop.Name;

            return name == sourceProp.Name;
        }

        string BuildCast(TargetProp targetProp, SourceProp sourceProp)
        {
            if (sourceProp == null)
                return "";

            var prop = targetProp.Prop;
            if (prop.Type.IsEnum)
                return $"(tns.{prop.Type.Name})(int)";

            // Convert double to decimal by default.
            if (sourceProp.TypeNameNormalized == "double")
                return $"({prop.Type.Name})";

            return "";
        }

        string BuildAssignment(TargetProp targetProp, SourceProp sourceProp)
        {
            MojProp prop = targetProp.Prop;
            var map = targetProp.Map;

            if (sourceProp != null)
            {
                // If the source type was a nullable and the target type not.
                string result = "source." + (map.SourceName ?? prop.Name);

                if (sourceProp.IsNullable && !prop.Type.IsNullableValueType)
                {
                    result += ".Value";
                }

                if (map.ValueExpression != null)
                    result = string.Format(map.ValueExpression, result);

                return result;
            }
            else
            {
                return map.ValueExpression;
            }
        }

        void ReadLegacyTypes()
        {
            var etypes = XElement.Load(Config.SourceTypesDescriptorFilePath);
            foreach (var etype in etypes.Elements("Type"))
            {
                var type = new SourceType();
                type.Name = (string)etype.Attr("Name");
                foreach (var eprop in etype.Elem("Props").Elements("Prop"))
                {
                    var prop = new SourceProp
                    {
                        Name = (string)eprop.Attr("Name"),
                        TypeName = (string)eprop.Attr("TypeName"),
                        IsNullable = (bool?)eprop.Attr("IsNullable", optional: true) ?? false
                    };
                    prop.TypeNameNormalized = (string)eprop.Attr("TypeNameNormalized", optional: true) ?? prop.TypeName;                 

                    type.Props.Add(prop);
                }

                SourceTypes.Add(type);
            }
        }

        void OError(string text)
        {
            O("// ### " + text);
        }

        void OInfo(string text)
        {
            O("// # " + text);
        }

        class SourceType
        {
            public string Name { get; set; }
            public List<SourceProp> Props { get; set; } = new List<SourceProp>();

            public MojType TargetType { get; set; }

            public List<TargetProp> TargetProps { get; set; } = new List<TargetProp>();

            public List<string> MissingTargetProps { get; set; } = new List<string>();
            public List<string> MissingSourceProps { get; set; } = new List<string>();
            public List<string> IgnoredSourceProps { get; set; } = new List<string>();
        }

        class SourceProp
        {
            public string Name { get; set; }
            public bool IsNullable { get; set; }
            public string TypeName { get; set; }
            public string TypeNameNormalized { get; set; }

            public bool IsIgnored { get; set; }

            public TargetProp TargetProp { get; set; }

            //// A single source property might be used multiple times or splitted in to multiple target properties.
            //public List<MojProp> TargetProps { get; set; } = new List<MojProp>();
        }

        class TargetProp
        {
            public MojType Type { get; set; }
            public MojProp Prop { get; set; }
            public string Name { get; set; }
            public MojVersionMapping Map { get; set; }

            public SourceProp SourceProp { get; set; }
        }

    }
}