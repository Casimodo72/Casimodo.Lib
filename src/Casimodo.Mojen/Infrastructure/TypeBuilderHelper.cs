using System.Reflection;
using System.Reflection.Emit;

namespace Casimodo.Lib
{
    public static class TypeBuilderHelper
    {
        // Source: http://stackoverflow.com/questions/26749429/anonymous-type-result-from-sql-query-execution-entity-framework

        public static TypeBuilder CreateTypeBuilder(string assemblyName, string moduleName, string typeName)
        {
            TypeBuilder typeBuilder = AssemblyBuilder
                .DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
                .DefineDynamicModule(moduleName)
                .DefineType(typeName, TypeAttributes.Public);

            // TODO: REMOVE
            //TypeBuilder typeBuilder = AppDomain
            //    .CurrentDomain
            //    .DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            //    .DefineDynamicModule(moduleName)
            //    .DefineType(typeName, TypeAttributes.Public);

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            return typeBuilder;
        }

        public static void CreateAutoImplementedProperty(TypeBuilder builder, string propertyName, Type propertyType)
        {
            const string PrivateFieldPrefix = "_";
            const string GetterPrefix = "get_";
            const string SetterPrefix = "set_";
            // Property getter and setter attributes.
            const MethodAttributes PropAccessorAttrs =
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig;

            // Generate the field.
            FieldBuilder fieldBuilder = builder.DefineField(PrivateFieldPrefix + propertyName, propertyType, FieldAttributes.Private);

            // Generate the property
            PropertyBuilder propBuilder = builder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            // Define the getter method.
            MethodBuilder getter = builder.DefineMethod(GetterPrefix + propertyName, PropAccessorAttrs, propertyType, Type.EmptyTypes);

            // Emit the IL code.
            // ldarg.0
            // ldfld,_field
            // ret
            ILGenerator il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            propBuilder.SetGetMethod(getter);

            // Define the setter method.
            MethodBuilder setter = builder.DefineMethod(SetterPrefix + propertyName, PropAccessorAttrs, null, new Type[] { propertyType });

            // Emit the IL code.
            // ldarg.0
            // ldarg.1
            // stfld,_field
            // ret
            il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            propBuilder.SetSetMethod(setter);
        }
    }
}
