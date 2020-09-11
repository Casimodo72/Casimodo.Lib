// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace Casimodo.Lib.ComponentModel
{
    // KABU TODO: REMOVE: INotifyDataErrorInfo was introduced to WPF 4.5.
#if (false)
    public interface INotifyDataErrorInfo
    {
        bool HasErrors { get; }
        event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        IEnumerable GetErrors(string propertyName);
    }

    public sealed class DataErrorsChangedEventArgs : EventArgs
    {
        public DataErrorsChangedEventArgs(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }
#endif

    internal class ErrorInfoContainer : List<DataErrorInfo>
    {
        public ErrorInfoContainer(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");
            this.PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }

    [DataContract]
    public class ValidatingObservableObject : ObservableObject, IDataErrorInfo, INotifyDataErrorInfo
    {
        static readonly MyValidationContext InternalValidationContext = new MyValidationContext();

        /// <summary>
        /// Used to register validation rules.
        /// </summary>
        public static readonly ValidationRulesManager ValidationRules = new ValidationRulesManager();

        public bool Validate()
        {
            _errors.Clear();

            var propertyNames = ValidationRules.GetProperties(this.GetType());
            if (propertyNames == null)
                return true;

            var context = InternalValidationContext;
            context.Clear();

            foreach (string propertyName in propertyNames)
            {
                ValidateCore(context, propertyName);
            }

            ProcessValidationResult(context);

            return !HasErrors;
        }

        MyValidationContext GetValidationContext()
        {
            return new MyValidationContext();
        }

        void ValidateProperty(string propertyName)
        {
            // We will validate the property...
            // 1)    if it is annotated with at least one ValidationAttribute
            // 2) OR if it was registered with a custom validation rule.
            if (!ValidationRules.IsValidationNeeded(this.GetType(), propertyName))
                return;

            var context = GetValidationContext();
            context.Clear();

            ValidateCore(context, propertyName);

            ProcessValidationResult(context);
        }

        // INotifyDataErrorInfo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Indicates whether the object has validation errors.
        /// Member of INotifyDataErrorInfo.
        /// </summary>
        /// <returns>true if the object has validation errors; otherwise, false.</returns>
        [IgnoreDataMember, NotMapped]
        public bool HasErrors
        {
            get
            {
                if (IsDisposed)
                    return false;

                bool value = _errors.Values.SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist).Any();
                return value;
            }
        }

        /// <summary>
        /// Occurs when the validation errors have changed for a property or for the entire object.
        /// Member of INotifyDataErrorInfo.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire object.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to retrieve validation errors for, or null or String.Empty
        /// to retrieve errors for the entire object.
        /// </param>
        /// <returns>the validation errors for the property or object.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                // Return all data errors of all properties.
                return _errors.Values.SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist);
            }
            else
            {
                // Return data errors of a specific property.

                ErrorInfoContainer errors = GetErrorInfoContainer(propertyName, false);
                if (errors == null)
                    return Enumerable.Empty<DataErrorInfo>();

                return errors;
            }
        }

        protected virtual void OnPropertyErrorsChanged(string propertyName)
        {
            RaiseErrorsChanged(propertyName);
        }

        void RaiseErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
        }

        // IDataErrorInfo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// Member of IDataErrorInfo.
        /// </summary>
        /// <returns>
        /// An error message indicating what is wrong with this object. The default is an empty string ("")
        /// </returns>
        string IDataErrorInfo.Error
        {
            get
            {
                if (!HasErrors)
                    return "";

                // Return the first error message.
                return
                     _errors.Values
                     .SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist)
                     .Select(err => err.ErrorMessage)
                     .FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// Member of IDataErrorInfo.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose error message to get.
        /// </param>
        /// <returns>
        /// The error message for the property. The default is an empty string ("").
        /// </returns>
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                ErrorInfoContainer errors = GetErrorInfoContainer(propertyName, false);
                if (errors != null && errors.Count != 0)
                    return errors[0].ErrorMessage;

                return "";
            }
        }

        // IDataErrorInfo End ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected override void OnPropertyChanged(string name)
        {
            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
                return;

            // We'll validate whenever a property has changed.
            ValidateProperty(name);
        }

        void ValidateCore(MyValidationContext context, string propertyName)
        {
            context.ContextPropertyName = propertyName;
            context.InvolvedPropertyNames = null;

            // Validate using rules per type.
            ValidateUsingRules(context, ValidationRules.GetCustomRulesPerType(this.GetType(), propertyName));

            // Validate using rules per instance.
            ValidateUsingRules(context, ValidationRules.GetCustomRulesPerInstance(this, propertyName));

            // Validate using ValidationAttribute(s).
            ValidateUsingAttributes(context);
        }

        void ProcessValidationResult(MyValidationContext context)
        {
            // Fire notifications if needed.
            var changes = context.ChangedProperties;
            if (changes != null)
            {
                foreach (var changedProperty in changes)
                {
                    RaisePropertyChangedCore(changedProperty);
                    OnPropertyErrorsChanged(changedProperty);
                }
                RaisePropertyChanged(nameof(HasErrors));
            }
        }

        void ValidateUsingRules(MyValidationContext context, List<CustomRuleInfo> rules)
        {
            if (rules == null || rules.Count == 0)
                return;

            foreach (CustomRuleInfo rule in rules)
                ApplyRule(context, rule);
        }

        void ApplyRule(MyValidationContext context, CustomRuleInfo rule)
        {
            object errorObj = rule.Validate(this);

            context.InvolvedPropertyNames = rule.PropertyNames;

            if (errorObj == null)
            {
                // An error will be removed if the validation returned null.

                if (rule.ErrorCode == null)
                    throw new Exception(
                        string.Format(
                            "The validation for property(ies) '{0}' returned NULL, but no ErrorCode was specified. " +
                            "An ErrorCode is needed in order to remove validation errors.",
                            ExpandPropertyNames(rule.PropertyNames)));

                RemoveDataErrors(context, rule.ErrorCode);
            }
            else if (errorObj is WrapperDataErrorInfo)
            {
                var error = (WrapperDataErrorInfo)errorObj;
                error.ErrorCode = rule.ErrorCode;

                RemoveDataErrors(context, error.ErrorCode);
                AddDataErrors(context, error);
            }
            else if (errorObj is DataErrorInfo)
            {
                AddDataErrors(context, (DataErrorInfo)errorObj);
            }
            else if (errorObj is IEnumerable<DataErrorInfo>)
            {
                IEnumerable<DataErrorInfo> collection = (IEnumerable<DataErrorInfo>)errorObj;
                foreach (DataErrorInfo error in collection)
                    AddDataErrors(context, error);
            }
            else if (errorObj is string)
            {
                // We need an error code in this.
                if (rule.ErrorCode == null)
                    throw new Exception(
                        string.Format("The validation for property(ies) '{0}' returned a string, " +
                            "but no ErrorCode was registered.", ExpandPropertyNames(rule.PropertyNames)));

                DataErrorInfo error = new DataErrorInfo();
                error.ErrorCode = rule.ErrorCode;
                error.ErrorMessage = (string)errorObj;

                AddDataErrors(context, error);
            }
            else
                throw new Exception(
                    string.Format(
                        "The validation engine cannot process results of type '{0}'.",
                        errorObj.GetType().Name));
        }

        string ExpandPropertyNames(string[] propertyNames)
        {
            return propertyNames.Aggregate(string.Empty, (acc, val) => acc + ", " + val);
        }

        void ValidateUsingAttributes(MyValidationContext context)
        {
            string errorMsg;
            object errorCode;
            AttributeRulesInfo rules = ValidationRules.GetAttributeRules(this.GetType(), context.ContextPropertyName);
            ValidationContext ctx = new ValidationContext(this, null, null);
            object value = rules.Prop.GetValue(this, null);

            // Process ValidationAttribute(s).
            foreach (ValidationAttribute attr in rules.Attributes)
            {
                errorMsg = null;
                try
                {
                    ctx.MemberName = rules.Prop.Name;
                    attr.Validate(value, ctx);
                }
                catch (Exception ex)
                {
                    errorMsg = attr.ErrorMessage;

                    if (string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = ex.Message;

                        if (string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg =
                                string.Format(
                                    "An unknown validation error occurred using attribute '{0}'.",
                                    attr.GetType().Name);
                        }
                    }
                }

                // Get or create the error code of the validation attribute.
                errorCode = ValidationRules.GetAttributeErrorCode(attr.GetType());

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    // Add a new data error.
                    AddDataError(context, context.ContextPropertyName, CreateDataError(errorCode, errorMsg));
                }
                else
                {
                    // Remove the data error.
                    RemoveDataError(context, context.ContextPropertyName, errorCode);
                }
            }
        }

        DataErrorInfo CreateDataError(object errorCode, string errorMessage)
        {
            var error = new DataErrorInfo();
            error.ErrorCode = errorCode;
            error.ErrorMessage = errorMessage;

            return error;
        }

        ErrorInfoContainer GetErrorInfoContainer(string propertyName, bool createIfMissing = true)
        {
            ErrorInfoContainer propErrors = null;

            if (!_errors.TryGetValue(propertyName, out propErrors) && createIfMissing)
            {
                propErrors = new ErrorInfoContainer(propertyName);
                _errors.Add(propertyName, propErrors);
            }
            return propErrors;
        }

        void RemoveDataErrors(MyValidationContext context, object errorCode)
        {
            foreach (var propertyName in context.InvolvedPropertyNames)
                RemoveDataError(context, propertyName, errorCode);
        }

        void RemoveDataError(MyValidationContext context, string propertyName, object errorCode)
        {
            ErrorInfoContainer errors = GetErrorInfoContainer(propertyName, false);
            if (errors == null)
            {
                // The given property did not violate any rules, so just leave.
                return;
            }

            // Find an error with matching code.
            DataErrorInfo error = errors.FirstOrDefault(e => e.ErrorCode.Equals(errorCode));
            if (error == null)
            {
                // The given property did not violate the given rule, so just leave.
                return;
            }

            // Remove the error.
            errors.Remove(error);

            // Remove the error list if empty.
            if (errors.Count == 0)
                _errors.Remove(propertyName);

            // Register the property's changed error state.
            context.AddChangedProperty(propertyName);

            // KABU: REMOVE: Apparently I don't need the call.
            //// Notify.
            //OnErrorsChanged(propertyName);
        }

        // KABU: REVISIT: Why did I disable this one?
#if (false)
        /// <summary>
        /// Remove all errors which have the given error code.
        /// </summary>
        /// <param name="errorCode"></param>
        internal void RemoveDataErrors(object errorCode)
        {
            var errors = _errors.Values.SelectMany(x => x).Where(x => x.ErrorCode.Equals(errorCode));

            foreach (var err in errors)
                RemoveDataError(err.PropertName, err.ErrorCode);
        }
#endif

        void AddDataErrors(MyValidationContext context, DataErrorInfo error)
        {
            foreach (var propertyName in context.InvolvedPropertyNames)
                AddDataError(context, propertyName, error);
        }

        void AddDataError(MyValidationContext context, string propertyName, DataErrorInfo error)
        {
            ErrorInfoContainer errors = GetErrorInfoContainer(propertyName);

            // Check for duplicates (by error code).
            if (errors.Any(e => e.ErrorCode.Equals(error.ErrorCode)))
            {
                // The given property did already violate the given rule, so just leave.
                return;
            }

            // Add the new error.
            errors.Add(error);

            // Register the property's changed error state.
            context.AddChangedProperty(propertyName);

            // KABU: REMOVE: Apparently I don't need the call.
            //// Notify.
            //OnErrorsChanged(propertyName);
        }

        internal void RemoveValidationRulePerInstance(string propertyName, object errorCode)
        {
            if (_customRulesPerInstance == null)
                return;

            MyValidationContext context = GetValidationContext();
            context.ContextPropertyName = propertyName;

            // Remove data error.
            RemoveDataError(context, propertyName, errorCode);

            // Deregister the validation rule.
            // Side note: http://english.stackexchange.com/questions/25931/unregister-vs-deregister
            List<CustomRuleInfo> rules = null;
            if (CustomRulesPerInstance.TryGetValue(propertyName, out rules))
            {
                CustomRuleInfo rule = rules.FirstOrDefault(x => x.ErrorCode.Equals(errorCode));
                if (rule != null)
                    rules.Remove(rule);
            }
        }

        internal void RemoveAllValidationRulePerInstance()
        {
            if (_customRulesPerInstance != null)
                _customRulesPerInstance.Clear();
        }

        /// <summary>
        /// Holds the current data errors.
        /// </summary>
        Dictionary<string, ErrorInfoContainer> _errors = new Dictionary<string, ErrorInfoContainer>();

        internal Dictionary<string, List<CustomRuleInfo>> CustomRulesPerInstance
        {
            get { return _customRulesPerInstance ?? (_customRulesPerInstance = new Dictionary<string, List<CustomRuleInfo>>()); }
        }

        Dictionary<string, List<CustomRuleInfo>> _customRulesPerInstance;

        internal bool HasCustomRulesPerInstance
        {
            get { return (_customRulesPerInstance != null && _customRulesPerInstance.Count != 0); }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            ErrorsChanged = null;

            if (_errors != null)
                _errors.Clear();
            _errors = null;

            if (_customRulesPerInstance != null)
                _customRulesPerInstance.Clear();
            _customRulesPerInstance = null;
        }

        class MyValidationContext
        {
            List<string> _changedPropertyNames;
            public string ContextPropertyName;
            public string[] InvolvedPropertyNames;

            public void AddChangedProperty(string propertyName)
            {
                if (_changedPropertyNames == null)
                    _changedPropertyNames = new List<string>();

                if (_changedPropertyNames.Any(x => x.Equals(propertyName)))
                    return;

                _changedPropertyNames.Add(propertyName);
            }

            public void Clear()
            {
                ContextPropertyName = null;
                InvolvedPropertyNames = null;
                if (_changedPropertyNames != null)
                    _changedPropertyNames.Clear();
            }

            public IEnumerable<string> ChangedProperties
            {
                get
                {
                    if (_changedPropertyNames == null || _changedPropertyNames.Count == 0)
                        return null;
                    return _changedPropertyNames;
                }
            }
        }
    }
}