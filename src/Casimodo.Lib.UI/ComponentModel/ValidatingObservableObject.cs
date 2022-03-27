// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

#nullable enable

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
            PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }
#endif  

    [DataContract]
    public class ValidatingObservableObject : ObservableObject, IDataErrorInfo, INotifyDataErrorInfo
    {
        static readonly MyValidationContext InternalValidationContext = new();

        /// <summary>
        /// Used to register validation rules.
        /// </summary>
        public static readonly ValidationRulesManager ValidationRules = new();

        public virtual bool Validate()
        {
            ClearErrors();

            var propertyNames = ValidationRules.GetProperties(GetType());
            if (propertyNames == null)
                return true;

            var context = InternalValidationContext;
            context.Clear();

            foreach (string propertyName in propertyNames)
            {
                Validate(context, propertyName);
            }

            ProcessValidationResult(context);

            return !HasErrors;
        }

        MyValidationContext CreateValidationContext()
        {
            return new MyValidationContext();
        }

        void ValidateProperty(string propertyName)
        {
            // We will validate the property...
            // 1)    if it is annotated with at least one ValidationAttribute
            // 2) OR if it was registered with a custom validation rule.
            if (!ValidationRules.IsValidationNeeded(GetType(), propertyName))
                return;

            var context = CreateValidationContext();
            context.Clear();

            Validate(context, propertyName);

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
                if (IsDisposed || _allErrors?.Count is not > 0)
                    return false;

                return _allErrors.Values.SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist).Any();
            }
        }

        /// <summary>
        /// Occurs when the validation errors have changed for a property or for the entire object.
        /// Member of INotifyDataErrorInfo.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire object.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to retrieve validation errors for, or null or String.Empty
        /// to retrieve errors for the entire object.
        /// </param>
        /// <returns>the validation errors for the property or object.</returns>
        public IEnumerable GetErrors(string? propertyName = null)
        {
            return GetErrorsCore(propertyName);
        }

        public IEnumerable<string> GetErrorMessages(string? propertyName = null)
        {
            return GetErrorsCore(propertyName)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        IEnumerable<DataErrorInfo> GetErrorsCore(string? propertyName = null)
        {
            if (!HasErrors)
            {
                return Enumerable.Empty<DataErrorInfo>();
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                // Return all data errors of all properties.
                return GetOrCreateErrors().Values
                    .SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist)
                    .DistinctBy(x => x.ErrorCode);
            }
            else
            {
                // Return data errors of a specific property.
                return GetErrorInfoContainer(propertyName, createIfMissing: false)
                    ?? Enumerable.Empty<DataErrorInfo>();
            }
        }

        protected virtual void OnPropertyErrorsChanged(string propertyName)
        {
            RaiseErrorsChanged(propertyName);
        }

        void RaiseErrorsChanged(string property)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
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
                     GetOrCreateErrors().Values
                     .SelectMany(errlist => (IEnumerable<DataErrorInfo>)errlist)
                     .Select(err => err.ErrorMessage)
                     .FirstOrDefault() ?? "";
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
        string IDataErrorInfo.this[string propertyName] =>
            GetErrorInfoContainer(propertyName, createIfMissing: false)?
                .FirstOrDefault()?.ErrorMessage ?? "";


        // IDataErrorInfo End ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected override void OnPropertyChanged(string name)
        {
            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
                return;

            ValidateProperty(name);
        }

        void Validate(MyValidationContext context, string propertyName)
        {
            context.ContextPropertyName = propertyName;
            context.InvolvedPropertyNames = null;

            // Validate using rules per type.
            ValidateUsingRules(context, ValidationRules.GetCustomRulesPerType(GetType(), propertyName));

            // Validate using rules per instance.
            //ValidateUsingRules(context, ValidationRules.GetCustomRulesPerInstance(this, propertyName));

            //// Validate using ValidationAttribute(s).
            //ValidateUsingAttributes(context);
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

                RaisePropertyChanged(HasErrorsChangedEventArgs);
            }
        }

        static readonly PropertyChangedEventArgs HasErrorsChangedEventArgs = new(nameof(HasErrors));

        void ValidateUsingRules(MyValidationContext context, List<CustomRuleInfo> rules)
        {
            if (rules?.Count is not > 0)
                return;

            foreach (CustomRuleInfo rule in rules)
            {
                ApplyRule(context, rule);
            }
        }

        void ApplyRule(MyValidationContext context, CustomRuleInfo rule)
        {
            object errorObj = rule.Validate(this);

            context.InvolvedPropertyNames = rule.PropertyNames;

            if (errorObj == null)
            {
                // An error will be removed if the validation returned null.

                if (rule.ErrorCode == null)
                {
                    throw new Exception(
                        $"The validation for property(ies) '{ExpandPropertyNames(rule.PropertyNames)}' " +
                        "returned NULL, but no ErrorCode was specified. " +
                        "An ErrorCode is needed in order to remove validation errors.");
                }

                RemoveDataErrors(context, rule.ErrorCode);
            }
            else if (errorObj is WrapperDataErrorInfo wrapperDataErrorInfo)
            {
                wrapperDataErrorInfo.ErrorCode = rule.ErrorCode;

                RemoveDataErrors(context, wrapperDataErrorInfo.ErrorCode);
                AddDataErrors(context, wrapperDataErrorInfo);
            }
            else if (errorObj is DataErrorInfo dataError)
            {
                AddDataErrors(context, dataError);
            }
            else if (errorObj is IEnumerable<DataErrorInfo> dataErrorList)
            {
                foreach (DataErrorInfo error in dataErrorList)
                    AddDataErrors(context, error);
            }
            else if (errorObj is string errorMessage)
            {
                // We need an error code in this.
                if (rule.ErrorCode == null)
                {
                    throw new Exception(
                        $"The validation for property(ies) '{ExpandPropertyNames(rule.PropertyNames)}' returned a string, " +
                            "but no ErrorCode was registered.");
                }

                var error = new DataErrorInfo
                {
                    ErrorCode = rule.ErrorCode,
                    ErrorMessage = errorMessage
                };

                AddDataErrors(context, error);
            }
            else throw new Exception(
                $"The validation engine cannot process results of type '{errorObj.GetType()}'.");
        }

        static string ExpandPropertyNames(string[] propertyNames)
        {
            return propertyNames.Aggregate("", (acc, val) => acc + ", " + val);
        }

        void ValidateUsingAttributes(MyValidationContext context)
        {
            string errorMsg;
            object errorCode;
            AttributeRulesInfo rules = ValidationRules.GetAttributeRules(GetType(), context.ContextPropertyName);
            ValidationContext ctx = new(this, null, null);
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
                errorCode = attr.GetType().Name;

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
            var error = new DataErrorInfo
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };

            return error;
        }

        PropertyErrorInfoList? GetErrorInfoContainer(string propertyName, bool createIfMissing = true)
        {
            var errors = GetOrCreateErrors();
            if (!errors.TryGetValue(propertyName, out PropertyErrorInfoList? propertyErrors) && createIfMissing)
            {
                propertyErrors = new PropertyErrorInfoList(propertyName);
                errors.Add(propertyName, propertyErrors);
            }

            return propertyErrors;
        }

        void RemoveDataErrors(MyValidationContext context, object errorCode)
        {
            foreach (var propertyName in context.InvolvedPropertyNames)
            {
                RemoveDataError(context, propertyName, errorCode);
            }
        }

        void RemoveDataError(MyValidationContext context, string propertyName, object errorCode)
        {
            PropertyErrorInfoList? propertyErrors = GetErrorInfoContainer(propertyName, false);
            if (propertyErrors?.Count is not > 0)
            {
                // The given property did not violate any rules, so just leave.
                return;
            }

            // Find an error with matching code.
            DataErrorInfo? error = propertyErrors.FirstOrDefault(e => e.ErrorCode.Equals(errorCode));
            if (error == null)
            {
                // The given property did not violate the given rule, so just leave.
                return;
            }

            propertyErrors.Remove(error);

            // Remove the property's error list if empty.
            if (propertyErrors.Count == 0 && _allErrors != null)
            {
                _allErrors.Remove(propertyName);
            }

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
            if (context.InvolvedPropertyNames == null)
            {
                return;
            }

            foreach (var propertyName in context.InvolvedPropertyNames)
                AddDataError(context, propertyName, error);
        }

        void AddDataError(MyValidationContext context, string propertyName, DataErrorInfo error)
        {
            PropertyErrorInfoList propertyErrors = GetErrorInfoContainer(propertyName, createIfMissing: true)!;

            // Check for duplicates (by error code).
            if (propertyErrors?.Any(e => e.ErrorCode.Equals(error.ErrorCode)) is true)
            {
                // The given property did already violate the given rule, so just leave.
                return;
            }

            // Add the new error.
            propertyErrors.Add(error);

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

            MyValidationContext context = CreateValidationContext();
            context.ContextPropertyName = propertyName;

            // Remove data error.
            RemoveDataError(context, propertyName, errorCode);

            // Deregister the validation rule.
            // Side note: http://english.stackexchange.com/questions/25931/unregister-vs-deregister
            if (CustomRulesPerInstance.TryGetValue(propertyName, out List<CustomRuleInfo>? rules))
            {
                CustomRuleInfo? rule = rules?.FirstOrDefault(x => x.ErrorCode.Equals(errorCode));
                if (rule != null)
                {
                    rules!.Remove(rule);
                }
            }
        }

        internal void RemoveAllValidationRulePerInstance()
        {
            if (_customRulesPerInstance != null)
                _customRulesPerInstance.Clear();
        }

        Dictionary<string, PropertyErrorInfoList>? _allErrors;

        Dictionary<string, PropertyErrorInfoList> GetOrCreateErrors() => _allErrors ??= new();

        void ClearErrors()
        {
            if (_allErrors != null)
            {
                _allErrors.Clear();
            }
        }

        internal Dictionary<string, List<CustomRuleInfo>> CustomRulesPerInstance => _customRulesPerInstance ??= new();

        Dictionary<string, List<CustomRuleInfo>> _customRulesPerInstance;

        internal bool HasCustomRulesPerInstance => (_customRulesPerInstance != null && _customRulesPerInstance.Count != 0);

        protected override void OnDisposed()
        {
            base.OnDisposed();

            ErrorsChanged = null;

            if (_allErrors != null)
                _allErrors.Clear();
            _allErrors = null;

            if (_customRulesPerInstance != null)
                _customRulesPerInstance.Clear();
            _customRulesPerInstance = null;
        }

        class MyValidationContext
        {
            List<string>? _changedPropertyNames;
            public string? ContextPropertyName;
            public string[]? InvolvedPropertyNames;

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

            public IEnumerable<string>? ChangedProperties
            {
                get
                {
                    if (_changedPropertyNames == null || _changedPropertyNames.Count == 0)
                        return null;
                    return _changedPropertyNames;
                }
            }
        }

        class PropertyErrorInfoList : List<DataErrorInfo>
        {
            public PropertyErrorInfoList(string propertyName)
            {
                if (string.IsNullOrWhiteSpace(propertyName))
                    throw new ArgumentNullException(nameof(propertyName));
                PropertyName = propertyName;
            }

            public string PropertyName { get; private set; }
        }
    }
}