using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
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
        public KendoGridEvent Kind { get; set; }

        public string FunctionName { get; set; }

        public string ComponentEventName { get; set; }

        public bool IsModelPart { get; set; }

        public bool IsContainer { get; set; }

        public bool IsCall { get; set; }

        public string Call { get; set; }

        public Action<WebViewGenContext> Body { get; set; }

        public List<KendoWebFunction> BodyFunctions { get; set; } = new List<KendoWebFunction>();
    }

    public class KendoWebGridFuncsConfig
    {
        readonly Dictionary<KendoGridEvent, KendoWebFunction> _componentEventHandlers = new Dictionary<KendoGridEvent, KendoWebFunction>();
        readonly List<KendoWebFunction> _functions = new List<KendoWebFunction>();

        public KendoWebGridFuncsConfig()
        {
            UseComponentEvent(KendoGridEvent.DataBinding, "kendomodo.onGridDataBinding", "DataBinding");
            UseComponentEvent(KendoGridEvent.DataBound, "kendomodo.onGridDataBound", "DataBound");
            UseComponentEvent(KendoGridEvent.Changed, "kendomodo.onGridChanged", "Change");
            UseComponentEvent(KendoGridEvent.Editing, null, "Edit"); // "kendomodo.onGridEditing"
            UseComponentEvent(KendoGridEvent.Saving, "kendomodo.onGridSaving", "Save");
            UseComponentEvent(KendoGridEvent.DetailInit, "kendomodo.onGridDetailInit", "DetailInit");
            UseComponentEvent(KendoGridEvent.DetailExpanding, "kendomodo.onGridDetailExpanding", "DetailExpand");
            UseComponentEvent(KendoGridEvent.DetailCollapsing, "kendomodo.onGridDetailCollapsing", "DetailCollapse");
        }

        public string Component { get; set; }

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
                name += "_" + Component;

            return AddCore(eve, functionName: name, vm: vm);
        }

        public KendoWebFunction AddCall(KendoGridEvent eve, string call)
        {
            var result = AddCore(eve, functionName: null, vm: false);
            result.Call = call;

            return result;
        }

        KendoWebFunction UseComponentEvent(KendoGridEvent eve, string defaultFunction, string componentEvent)
        {
            //Guard.ArgNotNullOrWhitespace(functionName, nameof(functionName));
            Guard.ArgNotNullOrWhitespace(componentEvent, nameof(componentEvent));

            if (_componentEventHandlers.ContainsKey(eve))
                throw new MojenException($"A handler for the component event '{eve}' does already exist.");

            var handler = new KendoWebFunction
            {
                Kind = eve,
                ComponentEventName = componentEvent,
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
            //{
            //    var container = new KendoWebFunction
            //    {
            //        // Compound view model function.
            //        Kind = eve,
            //        FunctionName = $"on{eve}Main",
            //        ComponentEventName = componentHandler.ComponentEventName,
            //        IsContainer = true,
            //        IsMPart = true
            //    };

            //    container.BodyFunctions.Add(componentHandler);
            //    _componentEventHandlers[eve] = container;

            //    componentHandler = container;
            //}

            KendoWebFunction func = new KendoWebFunction
            {
                Kind = eve,
                FunctionName = functionName,
                IsModelPart = vm
            };
            _functions.Add(func);

            componentHandler.BodyFunctions.Add(func);

            return func;
        }
    }
}
