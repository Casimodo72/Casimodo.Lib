using System.Text.RegularExpressions;

namespace Casimodo.Lib.Mojen
{
    public class ClassGen : DataLayerGenerator
    {
        public ClassGen()
        {
            Scope = "Context";
        }

        bool IsPartialChangeMethodEnabled { get; set; }

        public void GenerateClassHead(MojType type, IList<MojInterface> interfaces = null)
        {
            OSummary(type.Summary);

            O($"[TypeIdentity(\"{type.Id}\")]");

            if (type.IsKeyAccessible)
                O($"[KeyInfo(PropName = \"{type.Key.Name}\")]");

            // Tenant key
            if (type.IsMultitenant)
            {
                var tenantKey = type.FindTenantKey();
                if (tenantKey == null)
                    throw new MojenException("The type is multitenant but no tenant key property exists.");

                O($"[TenantKeyInfo(PropName = \"{tenantKey.Name}\")]");
            }

            if (!type.NoDataContract)
                O("[DataContract]");

            // KABU TODO: REMOVE
            //if (type.Kind == MojTypeKind.Entity && !type.IsAbstract)
            //    O("[Table(\"{0}\")]", type.TableName);

            O($"public{(type.IsAbstract ? " abstract" : (type.IsSealed ? " sealed" : ""))} partial class {type.ClassName}{(type.EffectiveBaseClassName != null ? " : " + type.EffectiveBaseClassName : "")}");
            // Interfaces
            var effectiveInterfaces = interfaces ?? type.Interfaces;
            if (effectiveInterfaces.Any())
            {
                if (type.HasBaseClass)
                    Oo("    , ");
                else
                    Oo("    : ");

                o(effectiveInterfaces.Select(x => x.Name).Join(", "));
                Br();
            }
            Begin();
        }

        public void GenerateInterfaceImpl(MojType type)
        {
            foreach (var iface in type.Interfaces.Where(x => !string.IsNullOrEmpty(x.Implementation)))
            {
                O();
                O(iface.Implementation);
            }
        }

        public void GenerateIKeyAccessorImpl(MojType type)
        {
            // IKeyAccessor<TKey>
            if (type.IsKeyAccessible)
            {
                var key = type.Key;
                O();
                O($"{key.Type.Name} IKeyAccessor<{key.Type.Name}>.GetKey()");
                Begin();
                O($"return {key.Name};");
                End();

                // IKeyAccessor
                O();
                O("object IKeyAccessor.GetKeyObject()");
                Begin();
                O($"return {key.Name};");
                End();
            }
        }

        public void GenerateIGuidGenerateableImpl(MojType type)
        {
            // IGuidGenerateable
            if (type.IsGuidGenerateable)
            {
                var guid = type.Guid;

                O();
                O("void IGuidGenerateable.GenerateGuid()");
                Begin();

                if (guid.Type.TypeNormalized == typeof(Guid))
                {
                    O($"if ({guid.Name} == Guid.Empty) {guid.Name} = Guid.NewGuid();");
                }
                else
                    throw new MojenException(string.Format("Don't know how to generate GUID of type '{0}'.",
                   guid.Type.TypeNormalized.Name));

                End();
            }
        }

        protected string[] BuildNamespaces(MojType type)
        {
            var ns = type.Namespace;
            var dataNs = App.Get<DataLayerConfig>().DataNamespaces.ToArray();
            var props = type.GetProps().ToArray();
            var foreignNs = type.GetProps()
                .Where(x => x.Reference.Is && !x.IsForeignKey && x.Reference.ToType.Namespace != ns)
                .Select(x => x.Reference.ToType.Namespace)
                .Distinct()
                .ToArray();
            return dataNs.Concat(foreignNs).ToArray();
        }

        protected string[] GetFriendNamespaces(MojType type)
        {
            var ns = type.Namespace;
            var foreignNs = type.GetProps()
                .Where(x => x.Reference.Is && !x.IsForeignKey && x.Reference.ToType.Namespace != ns)
                .Select(x => x.Reference.ToType.Namespace)
                .Distinct()
                .ToArray();
            return foreignNs;
        }

        public void GenerateIMultitenantImpl(MojType type)
        {
            // IMultitenant
            if (type.IsMultitenant)
            {
                var key = type.TenantKey;
                O();
                O($"object IMultitenant.GetTenantKey()");
                Begin();
                O($"return {key.Name};");
                End();

                O();
                O($"void IMultitenant.SetTenantKey(object tenantKey)");
                Begin();
                O($"{key.Name} = ({key.Type.Name})tenantKey;");
                End();
            }
        }

        public virtual string GetPropTypeName(MojProp prop, string scenario = null)
        {
            return prop.Type.Name;
        }

        public void GenerateProp(MojType type, MojProp prop, bool store = false)
        {
            var @new = prop.IsNew ? " new" : "";
            var @virtual = prop.IsVirtual && !type.IsSealed ? " virtual" : "";
            var @sealed = prop.IsSealed ? " sealed" : "";
            var @override = prop.IsOverride ? " override" : "";
            var propType = GetPropTypeName(prop, "Property");

            Oo($"public{@new}{@virtual}{@sealed}{@override} {propType} {prop.Name}");

            ValidateObservable(prop);

            if (prop.IsNew)
                OPropNew(prop);
            else if (!prop.IsObservable && !store)
                OPropImplicit(prop);
            else if (prop.IsObservable && store)
                OPropObservableWithStore(prop);
            else if (!prop.IsObservable && store)
                OPropWithStore(prop);
            else if (prop.ProxyOfInheritedProp != null && prop.IsObservable)
                OPropInheritedProxyObservable(prop);
            else if (prop.ProxyOfInheritedProp != null && !prop.IsObservable)
                OPropInheritedProxy(prop);
            else if (prop.IsObservable)
                OPropObservable(prop);
            else
                OPropImplicit(prop);
        }

        public void OPropImplicit(MojProp prop)
        {
            oO($" {{ get;{ImplicitSetter(prop)} }}");
        }

        string ImplicitSetter(MojProp prop)
        {
            return prop.HasSetter ? GetAccessModifierOfSetter(prop) + " set; " : "";
        }

        string GetAccessModifierOfSetter(MojProp prop, bool leading = false)
        {
            string accessModifier = "";
            if (prop.SetterOptions.HasFlag(MojPropGetSetOptions.Protected))
                accessModifier = "protected";
            else if (prop.SetterOptions.HasFlag(MojPropGetSetOptions.ProtectedInternal))
                accessModifier = "protected internal";
            else if (prop.SetterOptions.HasFlag(MojPropGetSetOptions.Internal))
                accessModifier = "internal";
            else if (prop.SetterOptions.HasFlag(MojPropGetSetOptions.Private))
                accessModifier = "private";

            if (accessModifier != "")
            {
                if (leading)
                    accessModifier += " ";
                else
                    accessModifier = " " + accessModifier;
            }

            return accessModifier;
        }

        void OPropGet(string getter)
        {
            O($"get => {getter};");
        }

        void OPropSet(MojProp prop, string target, bool leading = true)
        {
            O($"{GetAccessModifierOfSetter(prop, leading)}set {{ {target} = value; }}");
        }

        public void OPropObservable(MojProp prop)
        {
            Br();
            Begin();

            OPropGet(prop.FieldName);

            if (prop.HasSetter)
            {
                // Setter
                OPropObservableSet($"SetProp(ref {prop.FieldName}, value)", () =>
                {
                    if (prop.IsNavigation)
                    {
                        if (prop.Reference.IsToOne)
                        {
                            OFormat("if (value != null) {0} = value.{1}; else {0} = null;",
                               prop.Reference.ForeignKey.Name,
                               prop.Reference.ToTypeKey.Name);
                        }
                        else if (!prop.Reference.IsToMany)
                            throw new MojenException($"Invalid property reference multiplicity '{prop.Reference.Multiplicity}'.");
                    }
                    OOnChanged(prop);
                });
            }
            End();
            OOnChangedPartial(prop);

            // Member field
            if (!prop.IsOverride)
                O($"protected {GetPropTypeName(prop, "Property")} {prop.FieldName};");
        }

        public void OPropWithStore(MojProp prop)
        {
            var name = prop.Store.Name;
            Br();
            Begin();

            OPropGet($"_store.{name}");

            if (prop.HasSetter) OPropSet(prop, $"_store.{name}", leading: true);

            End();
        }

        public void OPropObservableWithStore(MojProp prop)
        {
            var name = prop.Store.Name;
            Br();
            Begin();

            OPropGet($"_store.{name}");

            if (prop.HasSetter)
            {
                // Setter
                OPropObservableSet($"SetProp(_store.{name}, value, () => _store.{name} = value)", () =>
                {
                    if (prop.IsNavigation)
                    {
                        OFormat("if (value != null) {0} = value.{1}; else {0} = null;",
                            prop.Reference.ForeignKey.Name,
                            prop.Reference.ToTypeKey.Name);
                    }
                    OOnChanged(prop);
                });
            }
            End();
            OOnChangedPartial(prop);
        }

        public void OPropNew(MojProp prop)
        {
            OPropInheritedCore(prop);
        }

        public void OPropInheritedProxy(MojProp prop)
        {
            OPropInheritedCore(prop);
        }

        public void OPropInheritedCore(MojProp prop)
        {
            var name = prop.ProxyOfInheritedProp;
            Br();
            Begin();

            OPropGet($"base.{name}");

            if (prop.HasSetter) OPropSet(prop, $"base.{name}");

            End();
        }

        public void OPropInheritedProxyObservable(MojProp prop)
        {
            var name = prop.ProxyOfInheritedProp;
            Br();
            Begin();

            OPropGet($"base.{name}");

            if (prop.HasSetter)
            {
                OPropObservableSet($"SetProp(base.{name}, value, () => base.{name} = value)", () =>
                {
                    OOnChanged(prop);
                });
            }

            End();
            OOnChangedPartial(prop);
        }

        public void GenerateODataOpenTypePropsContainer(MojType type)
        {
            if (!type.IsODataOpenType)
                return;

            // Open Types: http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/use-open-types-in-odata-v4
            {
                var prop = type.GetProps().FirstOrDefault(x => x.IsODataDynamicPropsContainer);
                var propName = prop != null ? prop.Name : "DynamicProperties";

                if (propName != "DynamicProperties")
                {
                    // Implement the OData properties accessor interface explicitely.
                    O();
                    OFormat("IDictionary<string, object> {0}.DynamicProperties {{ get {{ return {1}; }} }}",
                        App.Get<DataLayerConfig>().IODataDynamicPropertiesAccessor,
                        propName);
                }

                O();
                if (type.Kind == MojTypeKind.Entity)
                {
                    O("[NotMapped]");
                    O("[DataMember]");
                }

                OFormat("public IDictionary<string, object> {0} {{ get {{ return _{1} ?? (_{1} = new Dictionary<string, object>()); }} set {{ _{1} = value; }} }}",
                   propName, FirstCharToLower(propName));

                O($"IDictionary<string, object> _{FirstCharToLower(propName)};");
            }
        }

        public void GenerateTypeComparisons(MojType type)
        {
            var allPropTypes = type.GetProps().ToArray();

            foreach (var comparison in type.Comparisons)
            {
                O();

                var candidateProps = allPropTypes
                    // Exclude lists.
                    .Where(x => comparison.UseListProps || !x.Type.IsCollection)
                    // Exclude navigation properties.
                    .Where(x => comparison.UseNavitationProps || !x.IsNavigation)
                    // Exclude non-stored properties.
                    .Where(x => comparison.UseNonStoredProps || x.StoreOrSelf.IsEntity() && !x.StoreOrSelf.IsExcludedFromDb)
                    .Select(x => x.Name)
                    .ToArray();

                var props = (comparison.Mode == "all" ? candidateProps : new string[0]).ToList();

                // Include
                var includeProps = comparison.IncludedProps
                    .SelectMany(prop => GetEffectiveComparisonProps(candidateProps, prop)).ToArray();

                if (comparison.Mode == "none")
                    props.AddRange(includeProps);

                // Exclude
                var excludeProps = comparison.ExcludedProps
                    .SelectMany(prop => GetEffectiveComparisonProps(candidateProps, prop)).ToArray();

                if (comparison.Mode == "all")
                    foreach (var prop in excludeProps)
                        props.Remove(prop);

                // Description
                var description = "Returns true if these properties are equal: ";
                if (comparison.Mode == "all")
                {
                    description += "All";
                    if (comparison.ExcludedProps.Any())
                        description += " except: " + excludeProps.Join(", ");
                }
                else
                {
                    description += includeProps.Join(", ");
                }
                OSummary(description);

                // Comparison method
                O($"public bool Equal{comparison.Name}({type.ClassName} item)");
                Begin();

                // Compare properties
                O($"return {props.JoinToString(" && ", prop => $"object.Equals(this.{prop}, item.{prop})")};");

                End();
            }
        }

        IEnumerable<string> GetEffectiveComparisonProps(string[] props, string expression)
        {
            if (!expression.Contains("*"))
            {
                if (!props.Contains(expression))
                    throw new MojenException(string.Format("Property not found '{0}'.", expression));

                return Enumerable.Repeat(expression, 1);
            }

            var regex = new Regex(WildcardToRegex(expression));

            return props.Where(x => regex.IsMatch(x));
        }

        static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        void ValidateObservable(MojProp prop)
        {
            var observable = (prop.IsObservable || prop.OnChangeRaiseProps.Count != 0);

            if (observable && prop.IsNew)
                throw new MojenException("An observable property cannot be declared as new.");

            if (observable && !prop.SetterOptions.HasFlag(MojPropGetSetOptions.Public))
                throw new MojenException("An observable property must have a public setter.");

            if (!prop.IsObservable && prop.OnChangeRaiseProps.Count != 0)
                throw new MojenException("A non-observable property cannot raise change events.");
        }

        void OOnChanged(MojProp prop)
        {
            OOnChangedFireRelated(prop);

            if (IsPartialChangeMethodEnabled)
                O($"On{prop.Name}Changed();");
        }

        void OOnChangedFireRelated(MojProp prop)
        {
            foreach (var propName in prop.OnChangeRaiseProps)
            {
                O($"RaisePropertyChanged(nameof({propName}));");
            }
        }

        void OOnChangedPartial(MojProp prop)
        {
            if (prop.HasSetter && IsPartialChangeMethodEnabled)
                O($"partial void On{prop.Name}Changed();");
        }

        void OPropObservableSet(string expression, Action content)
        {
            CustomIndent(-Indent);
            StartBuffer();
            content();
            var contentText = BufferedText;
            EndBuffer();
            CustomIndent(0);

            if (!string.IsNullOrEmpty(contentText))
            {
                var lines = contentText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 1)
                {
                    O($"set {{ if ({expression}) {{ {lines[0]} }} }} ");
                }
                else
                {
                    O("set");
                    Begin();
                    O($"if ({expression})");
                    Begin();
                    foreach (var line in lines)
                        O(line);

                    End();
                    End();
                }
            }
            else
            {
                O($"set => {expression};");
            }
        }

        public void GenerateNamedAssignFromMethods(MojType type)
        {
            if (!type.AssignFromConfig.Is)
                return;

            foreach (var assignment in type.AssignFromConfig.Items)
            {
                O();
                O($"public void {assignment.Name}({type.ClassName} source)");
                Begin();

                foreach (var prop in assignment.Properties)
                {
                    O($"this.{prop} = source.{prop};");
                }
                End();
            }
        }

        public void GenerateAssignFromMethod(MojType type)
        {
#if (false)
            O();
            O("public void AssignFrom({0} source)", type.ClassName);
            B();
            if (type.HasBaseClass)
                O("base.AssignFrom(source);");

            foreach (var p in type.GetLocalProps(custom: false))
            {
                if (p.IsKey)
                    continue;

                O("{0} = source.{0};", p.Name);
            }
            E();
#endif
        }
    }
}