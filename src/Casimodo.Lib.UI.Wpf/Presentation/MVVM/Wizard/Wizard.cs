using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Casimodo.Lib.ComponentModel;
using System.ComponentModel.Composition;

namespace Casimodo.Lib.Presentation
{
    public class WizardPagesHolder
    {
        public WizardPagesHolder()
        {
            Items = new CollectionViewModel<WizPageInfo>();         
        }

        public WizPageInfo First { get; private set; }

        public IWizardPagePresenter CurrentModel
        {
            get { return (Items.CurrentItem != null) ? Items.CurrentItem.Model : null; }
        }

        public void SetFirst(IWizardPagePresenter pageModel, WizardPageArgs args)
        {
            if (pageModel == null)
                throw new ArgumentNullException("pageModel");
            if (First != null)
                throw new InvalidOperationException("The first wizard page view model was already assigned.");

            First = Set(pageModel, args);
            
        }

        public WizPageInfo Set(IWizardPagePresenter pageModel, WizardPageArgs args)
        {
            if (pageModel == null)
                throw new ArgumentNullException("pageModel");

            WizPageInfo info = Items.FirstOrDefault(x => x.Model.Equals(pageModel));
            if (info == null)
            {
                info = new WizPageInfo { Model = pageModel, Args = args };
                Items.Add(info);
            }
            else
                info.Args = args;

            Items.MoveCurrentTo(info);

            return info;
        }

        public CollectionViewModel<WizPageInfo> Items { get; private set; }

        public WizardPageArgs GetArguments(IWizardPagePresenter pageModel)
        {
            return Items.Where(x => x.Model.Equals(pageModel)).Select(x => x.Args).FirstOrDefault();
        }

        public class WizPageInfo
        {
            public IWizardPagePresenter Model { get; set; }
            public WizardPageArgs Args { get; set; }
        }
    }

    // Wizard page ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public interface IWizardPagePresenter : IViewModel
    {
        IWizardShellPresenter WizardShell { get; set; }
        bool PerformTransition(WizardTransition transition);
        // WizardTransitionCollection Transitions { get; }
    }

    public interface IWizardPageViewModel<TView, TResult> : IDialogViewModel<TView, TResult>, IWizardPagePresenter
        where TView : class
        where TResult : ViewModelResult, new()
    { }

    public class WizardPageArgs
    { }

    public abstract class WizardPageViewModel<TView, TViewModel, TParams, TResult> :
        LookupViewModelBase<TView, TViewModel, TParams, TResult>,
        IWizardPagePresenter
        where TViewModel : class, IViewModel<TView>
        where TParams : WizardPageArgs, new()
        where TResult : ViewModelResult, new()
        where TView : class, IDialogViewBase<TViewModel>
    {
        /// <summary>
        /// Main constructor.
        /// </summary>
        [ImportingConstructor]
        public WizardPageViewModel()
        {
            ArgumentPolicy = ViewModelArgumentPolicy.Required;
            //Transitions = new WizardTransitionCollection();
            //Transitions.TransitionRequested += (s, e) => OnTransitionRequested((WizardTransition)s);
        }

        public IWizardShellPresenter WizardShell { get; set; }

        public virtual bool PerformTransition(WizardTransition transition)
        {
            return false;
        }

        //protected virtual void OnTransitionRequested(WizardTransition item)
        //{
        //    // NOP.
        //}

        //public WizardTransitionCollection Transitions { get; private set; }

        protected override void OnDispose()
        {
            base.OnDispose();

            //Transitions.Dispose();
            //Transitions = null;
        }
    }

    // Wizard shell ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public interface IWizardShellPresenter
    {
        WizardPagesHolder Pages { get; }
        void Enable(WizardTransitionKind transitions);
        void Disable(WizardTransitionKind transitions);
        void PerformTransition(IWizardPagePresenter toPage);
        void PerformTransition(IWizardPagePresenter toPage, WizardPageArgs args);
    }

    /// <summary>
    /// Input arguments of the "WizardShell" ViewModel.
    /// </summary>
    public class WizardShellArgs
    {
        public WizardShellArgs()
        {
            Pages = new WizardPagesHolder();
        }

        public WizardPagesHolder Pages { get; private set; }
    }

    // Wizard transitions ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    [Flags]
    public enum WizardTransitionKind
    {
        Next = 1,
        Back = 2,
        Cancel = 4,
        Finish = 8,
        Close = 16,
        All = Next | Back | Cancel | Finish | Close
    }

    public class WizardTransitionCollection : ObservableObject
    {
        public WizardTransitionCollection()
        {
            Items = new CollectionViewModel<WizardTransition>();
        }

        public CollectionViewModel<WizardTransition> Items { get; private set; }

        public event WizardTransitionPerformedEventHandler TransitionRequested;

        public void AddNextTransition()
        {
            Add(new NextWizardTransition());
        }

        public void AddBackTransition()
        {
            Add(new BackWizardTransition());
        }

        public void AddCancelTransition()
        {
            Add(new CancelWizardTransition());
        }

        public void AddFinishTransition()
        {
            Add(new FinishWizardTransition());
        }

        public void Add(WizardTransition item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            Items.Add(item);

            item.Requested += new WizardTransitionPerformedEventHandler(OnTransitionRequested);
        }

        void OnTransitionRequested(object sender, EventArgs args)
        {
            if (TransitionRequested != null)
                TransitionRequested(sender, args);
        }
    }

    public delegate void WizardTransitionPerformedEventHandler(object sender, EventArgs args);

    public abstract class WizardTransition : ObservableObject
    {
        public WizardTransition()
        {
            ExecuteCommand = CommandFactory.Create(() => Execute());
        }

        public event WizardTransitionPerformedEventHandler Requested;

        protected virtual void Execute()
        {
            if (Requested != null)
                Requested(this, EventArgs.Empty);
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProp(ref _isEnabled, value); }
        }
        bool _isEnabled;

        public string Name { get; set; }

        public string Text
        {
            get { return _text; }
            set { SetProperty(TextProperty, ref _text, value); }
        }
        string _text;
        public static readonly ObservablePropertyMetadata TextProperty = ObservablePropertyMetadata.Create("Text");

        public ICommandEx ExecuteCommand { get; private set; }
    }

    public sealed class NextWizardTransition : WizardTransition
    {
        public NextWizardTransition()
        {
            Name = "Next";
            Text = "Weiter";
        }
    }

    public sealed class BackWizardTransition : WizardTransition
    {
        public BackWizardTransition()
        {
            Name = "Back";
            Text = "Zurück";
        }
    }

    public sealed class CancelWizardTransition : WizardTransition
    {
        public CancelWizardTransition()
        {
            Name = "Cancel";
            Text = "Abbrechen";
        }
    }

    public sealed class FinishWizardTransition : WizardTransition
    {
        public FinishWizardTransition()
        {
            Name = "Finish";
            Text = "Fertigstellen";
        }
    }

    public sealed class CloseWizardTransition : WizardTransition
    {
        public CloseWizardTransition()
        {
            Name = "Close";
            Text = "Schließen";
        }
    }
}
