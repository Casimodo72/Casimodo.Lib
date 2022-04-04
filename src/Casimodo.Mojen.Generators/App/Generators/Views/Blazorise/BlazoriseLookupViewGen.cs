namespace Casimodo.Mojen.Blazorise;

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

            PerformWrite(context.View, () => GenerateView(context));
        }
    }
   
    void GenerateView(WebViewGenContext context)
    {
        var view = context.View;
        var vprop = view.Props.FirstOrDefault();
        if (vprop == null)
        {
            throw new MojenException($"No prop defined for lookup view of type '{view.TypeConfig.Name}'.");
        }

        OTag("Modal", () =>
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
                    O("@if (items != null)");
                    Begin();

                    OTag($"ListView",
                        $"TItem={view.TypeConfig.ClassName}",
                        $"Data=@items TextField=\"(item) => item.{vprop.Name}\"",
                        "Mode = \"ListGroupMode.Selectable\"");

                    End();
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
            O("[Inject] IDataService DataService { get; set; } = default!;");

            O($"IReadOnlyList<{context.View.TypeConfig.ClassName}>? items;");

            O();
            O("Modal modalRef = default!;");

            //O();
            //O("protected override async Task OnInitializedAsync()");
            //Begin();
            //O($"items = await DataService.Get{context.View.TypeConfig.PluralName}();");
            //End();

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
            O("modalRef.Hide();");
            End();
        });

        /*
         
<ListView TItem="Country"
    Data="Countries"
    TextField="(item) => item.Name"
    Mode="ListGroupMode.Static"
    MaxHeight="300px">
</ListView>
          
         
<Modal @ref="modalRef">
    <ModalContent Centered>
        <ModalHeader>
            <ModalTitle>Employee edit</ModalTitle>
            <CloseButton />
        </ModalHeader>
        <ModalBody>
            <Field>
                <FieldLabel>Name</FieldLabel>
                <TextEdit Placeholder="Enter name..." />
            </Field>
            <Field>
                <FieldLabel>Surname</FieldLabel>
                <TextEdit Placeholder="Enter surname..." />
            </Field>
        </ModalBody>
        <ModalFooter>
            <Button Color="Color.Secondary" Clicked="@HideModal">Close</Button>
            <Button Color="Color.Primary" Clicked="@HideModal">Save Changes</Button>
        </ModalFooter>
    </ModalContent>
</Modal>
        */
    }
}

