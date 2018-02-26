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
        Editing,
        Saving,
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

        /// <summary>
        /// When a function call, then @IsModelPart indicates whether to qualify the call with a preceeding "this." (e.g. "this.FunctionName(e)").
        /// When a function with body, then this will be generated as a view model function.
        /// </summary>
        public bool IsModelPart { get; set; }

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
        public List<KendoWebFunction> BodyFunctions { get; set; } = new List<KendoWebFunction>();
    }

    public class KendoWebGridEventsConfig
    {
        readonly Dictionary<KendoGridEvent, KendoWebFunction> _componentEventHandlers = new Dictionary<KendoGridEvent, KendoWebFunction>();
        readonly List<KendoWebFunction> _functions = new List<KendoWebFunction>();

        public KendoWebGridEventsConfig()
        {
            UseComponentEvent(KendoGridEvent.DataBinding);
            UseComponentEvent(KendoGridEvent.DataBound);
            UseComponentEvent(KendoGridEvent.Changed, "Change");
            UseComponentEvent(KendoGridEvent.Editing, "Edit");
            UseComponentEvent(KendoGridEvent.Saving, "Save", "kendomodo.onGridSaving");
            UseComponentEvent(KendoGridEvent.DetailInit, "DetailInit", "kendomodo.onGridDetailInit");
            UseComponentEvent(KendoGridEvent.DetailExpanding, "DetailExpand", "kendomodo.onGridDetailExpanding");
            UseComponentEvent(KendoGridEvent.DetailCollapsing, "DetailCollapse", "kendomodo.onGridDetailCollapsing");
        }

        public string ComponentName { get; set; }

        public string ViewModel { get; set; }

        public KendoWebFunction FindComponentEventHandler(KendoGridEvent eve)
        {
            KendoWebFunction e;
            if (_componentEventHandlers.TryGetValue(eve, out e))
                return e;

            return null;
        }

        public IEnumerable<KendoWebFunction> ComponentEventHandlers
        {
            get { return _componentEventHandlers.Values; }
        }

        public IEnumerable<KendoWebFunction> Functions
        {
            get { return _functions; }
        }

        public KendoWebFunction GetComponentEventHandler(KendoGridEvent eve)
        {
            var handler = FindComponentEventHandler(eve);
            if (handler == null)
                throw new MojenException($"Handler not found for event '{eve}'.");

            return handler;
        }

        public void RemoveComponentEventHandler(KendoGridEvent eve)
        {
            _componentEventHandlers.Remove(eve);
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

        // KABU TODO: REMOVE?
        //public KendoWebFunction AddCall(KendoGridEvent eve, string call)
        //{
        //    var result = AddCore(eve, functionName: null, vm: false);
        //    result.Call = call;

        //    return result;
        //}

        KendoWebFunction UseComponentEvent(KendoGridEvent eve, string name = null, string defaultFunction = null)
        {
            if (_componentEventHandlers.ContainsKey(eve))
                throw new MojenException($"The component event '{eve}' has already been registered.");

            if (name == null)
                name = eve.ToString();

            var handler = new KendoWebFunction
            {
                Event = eve,
                ComponentEventName = name,
                FunctionName = $"onComponent{eve}",
                IsContainer = true,
                IsModelPart = true
            };
            _componentEventHandlers.Add(eve, handler);

            if (defaultFunction != null)
                AddCore(eve, defaultFunction, false);

            return handler;
        }

        KendoWebFunction AddCore(KendoGridEvent eve, string functionName, bool vm)
        {
            var componentHandler = GetComponentEventHandler(eve);
            if (!componentHandler.IsContainer)
                throw new MojenException("The component event handler must be a container.");

            KendoWebFunction func = new KendoWebFunction
            {
                Event = eve,
                FunctionName = functionName,
                IsModelPart = vm
            };
            _functions.Add(func);

            componentHandler.BodyFunctions.Add(func);

            return func;
        }
    }
}
