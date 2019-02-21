using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class JsClassGen : MojenGenerator
    {
        public void OTsClass(string name, string extends = null,
               bool isstatic = false, bool export = true,
               bool hasconstructor = true,
               string constructorOptions = null,
               bool propertyInitializer = false,
               Action constructor = null,
               Action content = null)
        {
            if (string.IsNullOrWhiteSpace(extends))
                extends = "";

            var isDerived = !string.IsNullOrEmpty(extends);
            var hasOptions = !string.IsNullOrWhiteSpace(constructorOptions);

            OB($"{(export ? "export " : "")}class {name}{(isDerived ? " extends " + extends : "")}");

            if (!hasconstructor && propertyInitializer)
                throw new MojenException("Can't generate a property initializing constructor if there's no constructor.");

            if (hasOptions && propertyInitializer)
                throw new MojenException("Can't generate a property initializing constructor if there the constructor has options.");

            if (hasconstructor)
            {
                // Constructor
                if (propertyInitializer)
                    OB($"constructor(value?: Partial<{name}>)");
                else if (hasOptions)
                    OB($"constructor({constructorOptions})");
                else
                    OB("constructor()");

                if (isDerived)
                    O($"super({(hasOptions ? constructorOptions : "")});");

                if (propertyInitializer)
                    O("if (value) Object.assign(this, value);");

                if (constructor != null)
                {
                    constructor();
                }

                End(); // End of constructor
            }

            if (content != null)
            {
                O();
                content();
            }

            End(); // End of class

            if (isstatic)
                O($"export let {name.FirstLetterToLower()} = new {name}();");
        }

        public void OJsClass(string ns, string name, string extends = null,
               bool isstatic = false, bool export = true,
               string constructorOptions = null,
               Action constructor = null,
               Action content = null)
        {
            OJsClass_ES6(ns, name, extends, isstatic, export, constructorOptions, constructor, content);
        }

        void OJsClass_ES6(string ns, string name, string extends = null,
            bool isStatic = false, bool export = true,
            string constructorOptions = null,
            Action constructor = null,
            Action content = null)
        {
            // KABU TODO: Maybe use the following JS for static class/properties:
            // class MyStaticClass
            // {
            //    constructor() { }
            //    static myStaticMethod() { }
            // }
            // MyStaticClass.myStaticProperty = 42;
            // ns.MyStaticClass = MyStaticClass;

            if (string.IsNullOrWhiteSpace(extends))
                extends = "";

            var isDerived = !string.IsNullOrEmpty(extends);
            var hasOptions = !string.IsNullOrWhiteSpace(constructorOptions);

            OB($"class {name}{(isDerived ? " extends " + extends : "")}");

            // Constructor
            OB($"constructor({(hasOptions ? constructorOptions : "")})");

            if (isDerived)
                O($"super({(hasOptions ? constructorOptions : "")});");

            if (constructor != null)
            {
                constructor();
            }

            End(); // End of constructor

            if (content != null)
            {
                O();
                content();
            }

            End(); // End of class

            if (isStatic)
                O("{0}.{1} = new {1}();", ns, name);
            else if (export)
                O("{0}.{1} = {1};", ns, name);
        }

        /*
        void OJsClass_ES5(string ns, string name, string extends = null,
            bool isStatic = false, bool export = false,
            string constructorOptions = null,
            Action constructor = null,
            Action content = null)
        {
            if (string.IsNullOrWhiteSpace(extends))
                extends = "";

            var isDerived = !string.IsNullOrEmpty(extends);
            var hasOptions = !string.IsNullOrWhiteSpace(constructorOptions);

            OB($"var {name} = (function ({(isDerived ? "_super" : "")})");

            if (isDerived)
                O($"casimodo.__extends({name}, _super);");

            // Constructor
            O();
            OB($"function {name}({(hasOptions ? constructorOptions : "")})");

            if (isDerived)
            {
                O($"_super.call(this{(hasOptions ? ", " + constructorOptions : "")});");
            }

            if (constructor != null)
            {
                O();
                constructor();
            }

            End(); // End of constructor

            O();
            O($"var fn = {name}.prototype;");

            if (content != null)
            {
                O();
                content();
            }

            O();
            O($"return {name};");

            End($")({extends});");

            if (export)
            {
                if (isStatic)
                    O("{0}.{1} = new {1}();", ns, name);
                else
                    O("{0}.{1} = {1};", ns, name);
            }
        }
        */
    }
}
