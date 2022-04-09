using Casimodo.Mojen.App.Generators.Blazor.Core;
using System.IO;

namespace Casimodo.Mojen.App.Generators.Blazor.Data;

public class BlazorLookupDataService : BlazorPartGenerator
{
    protected override void GenerateCore()
    {

        base.GenerateCore();

        if (DataServicesConfig == null ||
            string.IsNullOrEmpty(DataServicesConfig.OutputDirPath))
            return;

        var lookupViews = App.GetItems<MojViewConfig>().Where(x => x.Lookup.Is && !x.IsCustom).ToList();

        PerformWrite(
            Path.Combine(DataServicesConfig.OutputDirPath, "ILookupDataService.generated.cs"),
            () => Generate(lookupViews));
    }

    void Generate(List<MojViewConfig> lookupViews)
    {
        OFileScopedNamespace("Gfa.Data.Services");

        O("public interface ILookupDataService");
        Begin();
        foreach (var view in lookupViews)
        {
            var methodName = BuildLookupMethodName(view);
            O($"Task<IEnumerable<{view.TypeConfig.Name}>> {methodName}();");
        }

        End();
    }

    public static string BuildLookupMethodName(MojViewConfig view)
    {
        return "GetFor" + (view.Alias ?? view.Name) ?? view.TypeConfig.Name + view.MainRoleName;
    }
}
