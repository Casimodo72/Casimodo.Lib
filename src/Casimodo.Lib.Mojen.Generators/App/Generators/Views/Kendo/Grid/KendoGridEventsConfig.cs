using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// The events of the KendoGrid.
    /// </summary>
    public enum KendoGridEvent
    {
        Changed,
        BeforeEditing,
        Editing,
        Removing,
        Saving,
        Syncing,
        Cancelling,
        DataBinding,
        DataBound,
        DetailExpanding,
        DetailCollapsing,
        DetailInit
    }

    public class KendoWebFunction
    {
        public KendoGridEvent Event { get; set; }

        public string FunctionName { get; set; }

        public string ComponentEventName { get; set; }

        public bool IsExistent { get; set; }

        /// <summary>
        /// When a function call, then @IsModelPart indicates whether to qualify the call with a preceeding "this." (e.g. "this.FunctionName(e)").
        /// When a function with body, then this will be generated as a view model function.
        /// </summary>

        /// <summary>
        /// Indicates whether this function can have a body (i.e. is not just a function call).
        /// </summary>
        public bool IsContainer { get; set; }

        // KABU TODO: REMOVE
        //public bool IsCall { get; set; }

        public string Call { get; set; }

        /// <summary>
        /// Defines the body code of this function.
        /// </summary>
        public Action<WebViewGenContext> Body { get; set; }

        /// <summary>
        /// Dfines the functions to be called in the body of this function.
        /// </summary>
        public List<KendoWebFunction> Children { get; set; } = new List<KendoWebFunction>();
    }

    public class KendoWebGridEventsConfig
    {
        readonly Dictionary<KendoGridEvent, KendoWebFunction> _eventHandlers = new Dictionary<KendoGridEvent, KendoWebFunction>();
        readonly List<KendoWebFunction> _functions = new List<KendoWebFunction>();

        public string ComponentName { get; set; }

        public string ViewModel { get; set; }

        public KendoWebFunction FindEventHandler(KendoGridEvent eve)
        {
            KendoWebFunction e;
            if (_eventHandlers.TryGetValue(eve, out e))
                return e;

            return null;
        }

        public IEnumerable<KendoWebFunction> EventHandlers
        {
            get { return _eventHandlers.Values; }
        }

        public IEnumerable<KendoWebFunction> Functions
        {
            get { return _functions; }
        }

        public KendoWebFunction Get(KendoGridEvent eve)
        {
            var handler = FindEventHandler(eve);
            if (handler == null)
                throw new MojenException($"Handler not found for event '{eve}'.");

            return handler;
        }

        public KendoWebFunction Add(KendoGridEvent eve, string purpose = null, bool vm = true)
        {
            string name = "on" + eve;

            if (purpose != null)
                name += purpose;

            if (!vm)
                name += "_" + ComponentName;

            return AddCore(eve, functionName: name, vm: vm);
        }

        public KendoWebFunction Use(KendoGridEvent eve, string kendoEventName = null, bool exists = false)
        {
            if (_eventHandlers.ContainsKey(eve))
                throw new MojenException($"The component event '{eve}' has already been registered.");

            if (kendoEventName == null)
                kendoEventName = eve.ToString();

            var handler = new KendoWebFunction
            {
                Event = eve,
                ComponentEventName = kendoEventName,
                FunctionName = $"onComponent{eve}Generated",
                IsExistent = exists,
                IsContainer = true
            };
            _eventHandlers.Add(eve, handler);

            return handler;
        }

        KendoWebFunction AddCore(KendoGridEvent eve, string functionName, bool vm)
        {
            var componentHandler = Get(eve);
            if (!componentHandler.IsContainer)
                throw new MojenException("The component event handler must be a container.");

            KendoWebFunction func = new KendoWebFunction
            {
                Event = eve,
                FunctionName = functionName
            };
            _functions.Add(func);

            componentHandler.Children.Add(func);

            return func;
        }
    }
}
