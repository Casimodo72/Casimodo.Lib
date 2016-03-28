using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace Casimodo.Lib.Presentation
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using Casimodo.Lib.ComponentModel;

    #region The "WizardShell" ViewModel and View.

    public interface IWizardShellView : IDialogViewBase<IWizardShellViewModel>
    {
        void SetContent(IWizardPagePresenter contentModel);
    }

    public interface IWizardShellViewModel : IDialogViewModel<IWizardShellView, WizardShellResult>
    { }

    /// <summary>
    /// The result of the "WizardShell" ViewModel.
    /// </summary>
    public class WizardShellResult : ViewModelResult
    {
        public WizardShellResult()
        { }
    }

    #endregion

    /// <summary>
    /// The "WizardShell" ViewModel uses the "model first" strategy.
    /// </summary>
    [ViewModelExport(typeof(IWizardShellViewModel), Strategy = ViewModelStrategy.ModelFirst)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class WizardShellViewModel :
        LookupViewModelBase<IWizardShellView, IWizardShellViewModel, WizardShellArgs, WizardShellResult>,
        IWizardShellViewModel,
        IWizardShellPresenter
    {
        /// <summary>
        /// Main constructor.
        /// </summary>
        [ImportingConstructor]
        public WizardShellViewModel()
        {
            ArgumentPolicy = ViewModelArgumentPolicy.Required;           
        }

        protected override void OnInitialize()
        {
            Pages = Arguments.Pages;

            Transitions = new WizardTransitionCollection();
            // Add default transitions.
            AddTransition(new CancelWizardTransition { IsEnabled = true });
            AddTransition(new BackWizardTransition());
            AddTransition(new NextWizardTransition());
            AddTransition(new FinishWizardTransition());
            AddTransition(new CloseWizardTransition());

            PerformTransitionCommand = CommandFactory.Create<WizardTransition>(
                (transition) => PerformTransition(transition),
                (transition) => CanPerformTransition(transition));

            // NOTE: We must not do this here because this will try to access the View,
            // which is not allowed during initialization.
            //PerformTransition(Pages.First.Model);          
        }

        protected override void OnViewAvailable()
        {
            base.OnViewAvailable();
            Pages = Arguments.Pages;
            PerformTransition(Pages.First.Model); 
        }

        bool CanPerformTransition(WizardTransition transition)
        {
            if (transition == null)
                return false;
            return transition.IsEnabled;
        }

        void PerformTransition(WizardTransition transition)
        {
            if (transition == null)
                return;

            if (transition is CancelWizardTransition)
            {
                Cancel();
                Close();
            }
            else if (transition is CloseWizardTransition)
            {
                Close();
            }           
            else if (Pages.CurrentModel != null)
            {
                // FinishWizardTransition and NextWizardTransition.
                if (!Pages.CurrentModel.PerformTransition(transition))
                {
                    // The page does not know how to perform the desired transition.
                    // TODO: IMPL fallback mechanism.
                }
            }
        }

        public void PerformTransition(IWizardPagePresenter toPage, WizardPageArgs args)
        {
            Pages.Set(toPage, args);
            PerformTransition(toPage);
        }

        public void PerformTransition(IWizardPagePresenter toPage)
        {
            // TODO: Handle previous page.

            if (toPage == null)
            {
                View.SetContent(null);
            }
            else
            {
                toPage.WizardShell = this;
                ViewModelHelper.Initialize(toPage, Arguments.Pages.GetArguments(toPage));
                View.SetContent(toPage);
            }
        }

        public void AddTransition(WizardTransition transition)
        {
            if (transition == null)
                throw new ArgumentNullException("transition");

            Transitions.Items.Add(transition);
        }

        public void Enable(WizardTransitionKind transitions)
        {
            SetTransitions(transitions, true);
        }

        public void Disable(WizardTransitionKind transitions)
        {
            SetTransitions(transitions, false);
        }

        void SetTransitions(WizardTransitionKind transitions, bool isEnabled)
        {
            foreach (var transition in EnumHelper.GetActiveFlags(transitions))
            {
                string transitionName = transition.ToString();
                var trans = Transitions.Items.FirstOrDefault(x => x.Name == transitionName);
                if (trans != null)
                {
                    trans.IsEnabled = isEnabled;
                }
            }

            PerformTransitionCommand.RaiseCanExecuteChanged();
        }

        public WizardTransitionCollection Transitions { get; protected set; }

        protected override void OnBuildResult()
        {
            // Result.Item = 
            HasResult = true;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public WizardPagesHolder Pages { get; private set; }      

        // Commands ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public ICommandEx PerformTransitionCommand { get; private set; }

        // Imports ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // View ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        [Import]
        Lazy<IWizardShellView> LazyView { get; set; }

        protected override IWizardShellView GetLazyView()
        {
            return LazyView.Value;
        }

        // Dispose ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected override void OnDispose()
        {
            base.OnDispose();
            LazyView = null;
        }
    }


    // Design time ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public sealed class WizardShellViewModelDesign : WizardShellViewModel
    {
        public WizardShellViewModelDesign()
        {
            Title = "Wizard";
            Transitions = new WizardTransitionCollection();
            // Add default transitions.
            AddTransition(new CancelWizardTransition { IsEnabled = true });
            AddTransition(new BackWizardTransition());
            AddTransition(new NextWizardTransition());
            AddTransition(new FinishWizardTransition());
            AddTransition(new CloseWizardTransition());
        }
    }
}
