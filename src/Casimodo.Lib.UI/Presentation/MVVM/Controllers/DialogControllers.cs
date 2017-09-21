using System.Linq;
using System.ComponentModel.Composition;
using Casimodo.Lib.Presentation;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using Casimodo.Lib.ComponentModel;
using System.Windows;
using System.ComponentModel.Composition;

namespace Casimodo.Lib.Presentation
{
    public class DialogViewDefinition
    {
        public DialogViewDefinition()
        {
            Buttons = MessageBoxButton.OKCancel;
        }

        public string Title { get; set; }
        public bool IsMaximized { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public MessageBoxButton? Buttons { get; set; }
    }

    public enum DialogKind
    {
        Custom,
        WpfWindow
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    public abstract class DialogControllerBase
    {
        public const int MaxDialogWidth = 700;
        public const int MaxDialogHeight = 850;
        public const int HalfMaxDialogHeight = 400;
        public const int ThirdMaxDialogHeight = 270;

        [Import]
        protected ExportFactory<ICustomDialogShellViewModel> CustomDialogShellFactory { get; set; }

        [Import]
        protected ExportFactory<IWpfDialogShellViewModel> WpfDialogShellFactory { get; set; }

        protected void ExecuteCore<TModel>(
            ExportFactory<TModel> contentViewModelFactory,
            DialogViewDefinition contentViewDefinition,
            object args = null,
            Action action = null,
            DialogKind kind = DialogKind.Custom)
            where TModel : IDialogViewModel
        {
            ExecuteCore<TModel, object>(contentViewModelFactory, contentViewDefinition, args, null, action, kind);
        }

        /// <summary>
        /// Executes the lookup view model and calls the specified result action.
        /// </summary>        
        protected void ExecuteCore<TModel, TResult>(
            ExportFactory<TModel> contentViewModelFactory,
            DialogViewDefinition contentViewDefinition,
            object args = null,
            Action<TResult> action = null,
            Action action2 = null,
            DialogKind kind = DialogKind.Custom)
            where TModel : IDialogViewModel
        {
            if (contentViewModelFactory == null)
                throw new ArgumentNullException("contentViewModelFactory");
            if (contentViewDefinition == null)
                throw new ArgumentNullException("contentViewDefinition");

            IDialogShellViewModel dialog;
            TModel contentViewModel;
            BuildDialog(contentViewModelFactory, out dialog, out contentViewModel, contentViewDefinition, args, kind);

            if (action != null || action2 != null)
            {
                dialog.Closed += (s, e) =>
                {
                    if (dialog.HasResult && action != null)
                        action((TResult)contentViewModel.ResultObject);
                    if (action2 != null)
                        action2();
                };
            }

            dialog.Show();
        }

        void BuildDialog<TModel>(
            ExportFactory<TModel> contentViewModelFactory,
            out IDialogShellViewModel dialog,
            out TModel contentViewModel,
            DialogViewDefinition conventViewDefinition,
            object args = null,
            DialogKind kind = DialogKind.Custom)
            where TModel : IDialogViewModel
        {
            if (kind == DialogKind.Custom)
                dialog = CustomDialogShellFactory.CreateExport().Value;
            else
                dialog = WpfDialogShellFactory.CreateExport().Value;

            // We need the tempDialogShell for the lambda expression.
            IDialogShellViewModel tempDialog = dialog;

            contentViewModel = contentViewModelFactory.CreateExport().Value;
            // Set the dialog shell as the parent of the content model.
            contentViewModel.Parent = dialog;
            // If the content model is closed, then close the dialog.
            contentViewModel.Closed += (s, e) => tempDialog.Close();

            ViewModelHelper.Initialize(contentViewModel, args);
            if (!string.IsNullOrWhiteSpace(conventViewDefinition.Title))
                contentViewModel.Title = conventViewDefinition.Title;

            dialog.SetContent(contentViewModel);
            dialog.SetButtons(conventViewDefinition.Buttons);
            dialog.SetPreferredSize(conventViewDefinition.Width, conventViewDefinition.Height);
        }
    }

    // KBU TODO: REMOVE
#if (false)
    public abstract class LookupController<TLookup, TResult>
        where TLookup : ILookupPresenter
        where TResult : ViewModelResult
    {
        [Import]
        protected ExportFactory<IDialogShell> DialogFactory { get; set; }

        [Import]
        public ExportFactory<TLookup> LookupFactory { get; set; }

        protected void ExecuteCore(object args, Action<TResult> resultAction)
        {
            IDialogShell dialog = DialogFactory.CreateExport().Value;
            TLookup lookupViewModel = LookupFactory.CreateExport().Value;

            ViewModelHelper.Initialize(lookupViewModel, args);

            dialog.SetContent(lookupViewModel);
            dialog.SetPreferredSize(700, 500);
            dialog.Closed += (s, e) =>
            {
                if (dialog.HasResult)
                    resultAction((TResult)lookupViewModel.ResultObject);

                // Note that the dialog will dispose itself and its content after it was closed,
                // so no need to do it manually here.   
            };

            dialog.Show();
        }
    }
#endif
}