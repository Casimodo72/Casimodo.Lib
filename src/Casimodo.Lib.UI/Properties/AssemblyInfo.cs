using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Resources;
using System.Windows;

[assembly: AssemblyTitle("Casimodo.Lib.UI")]
[assembly: AssemblyDescription("Part of Casimodo's library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Kasimier Buchcik (Casimodo)")]
[assembly: AssemblyProduct("Casimodo.Lib.UI")]
[assembly: AssemblyCopyright("Copyright Kasimier Buchcik 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("68b8ccd3-f40d-4ada-bdb6-960e50998f9d")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]

[assembly: XmlnsDefinition("http://schemas.casimodo.net/lib", "Casimodo.Lib.Presentation")]
[assembly: XmlnsDefinition("http://schemas.casimodo.net/lib", "Casimodo.Lib.Presentation.Controls")]
[assembly: XmlnsDefinition("http://schemas.casimodo.net/lib", "Casimodo.Lib.Presentation.Behaviors")]

#if (SILVERLIGHT)
// Add friendly assembly for testing purposes.
[assembly: InternalsVisibleTo("Casimodo.Lib.Silverlight.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100592a7a3ae273a9b8081b19c1ee6b723d67597711ffd5c62c2b34d98461bbc0d34caf480bfbb00d804c1de6cd1ec7604ec30b64b4e9cf494f986dcf70a8a0e7f30f4763ba59e6f73da7e8fef39d545569953dec976ace1333c74be9b6f8a38da9bc84a5c8ac6dff76d59a46982f4cd21615af38df4f7ec4e90933271a7ad75d8c")]
#endif

[assembly: ThemeInfoAttribute(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: NeutralResourcesLanguageAttribute("de")]
