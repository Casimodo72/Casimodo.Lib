using Casimodo.Mojen.App.Generators.Blazor.Data;

namespace Casimodo.Mojen.App.Generators.Blazor.Blazorise;

public class BlazoriseLookupViewGen : BlazoriseViewGen
{
    protected override void GenerateCore()
    {
        base.GenerateCore();

        foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
            .Where(x => x.Uses(this) && !x.IsCustom))
        {
            var context = new WebViewGenContext
            {
                View = view
            };

            Write(context.View, () => GenerateView(context));
        }
    }

    void GenerateView(WebViewGenContext context)
    {
        var view = context.View;
        // TODO: Use a grid and display all props as columns.
        var vprop = view.Props.FirstOrDefault();
        if (vprop == null)
        {
            throw new MojenException($"No prop defined for lookup view of type '{view.TypeConfig.Name}'.");
        }

        O("@if (!CanShow()) return;");
        O();
        OTag("Modal", "@ref=modalRef", () =>
        {
            OTag("ModalHeader", () =>
            {
                OTag("ModalTitle", () => O(view.Title));
                OTag("CloseButton");
            });

            OTag("ModalContent", "Size=ModalSize.Fullscreen", () =>
            {
                OTag("ModalBody", () =>
                {
                    // TODO: Use a grid and display all props as columns.
                    ORazorTODO("Use a grid instead. This is just temporary stuff.");
                    OTag($"ListView",
                        $"TItem={view.TypeConfig.ClassName}",
                        $"Data=@items TextField=\"(item) => item.{vprop.Name}\"",
                        "Mode = \"ListGroupMode.Selectable\"");
                });
            });

            OTag("ModalFooter", () =>
            {
                OTag("Button",
                    "Color=Color.Secondary",
                    "Clicked=@Cancel",
                    () => O("Close"));

                OTag("Button",
                    "Color=Color.Primary",
                    "Clicked=@Confirm",
                    () => O("OK"));
            });
        });

        OBlazorCode(() =>
        {
            ONullableEnable();

            var hasLookupDataService = App.HasGenerator<BlazorLookupDataService>();

            if (hasLookupDataService)
            {
                O();
                O("[Inject] ILookupDataService LookupDataService { get; set; } = default!;");
            }

            O();
            O($"IReadOnlyList<{context.View.TypeConfig.ClassName}>? items;");

            O();
            O("Modal modalRef = default!;");
            O("bool isVisible;");

            //O();
            //O("protected override async Task OnInitializedAsync()");
            //Begin();          
            //End();

            O("protected override async Task OnAfterRenderAsync(bool firstRender)");
            Begin();

            O("if (CanShow() && modalRef?.Visible == false)");
            Begin();
            O("await modalRef.Show();");
            End();

            End();

            O("bool CanShow() => isVisible && items != null;");

            O();
            O("public async Task Show()");
            Begin();
            if (hasLookupDataService)
            {
                var lookupMethodName = BlazorLookupDataService.BuildLookupMethodName(view);
                O($"items = (await LookupDataService.{lookupMethodName}()).ToList();");
                O("isVisible = true;");
                OStateHasChanged();
            }
            else
            {
                OTODO("No lookup data service defined.");
            }
            End();

            O();
            O("void Confirm()");
            Begin();
            O("Close();");
            End();

            O();
            O("void Cancel()");
            Begin();
            O("Close();");
            End();

            O();
            O("void Close()");
            Begin();
            O("isVisible = false;");
            O("modalRef.Hide();");
            O("items = null;");
            OStateHasChanged();
            End();
        });
    }
}

