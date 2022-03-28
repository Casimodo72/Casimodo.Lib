// Copyright (c) 2010 Kasimier Buchcik
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

#nullable enable

namespace Casimodo.Lib.ComponentModel
{
    // TODO: Rename to ValidatableObservableObject someway when we don't have to support obsolete stuff anymore.
    [DataContract]
    public class ValidatingObservableObject : ObservableObject, IDataErrorInfo, INotifyDataErrorInfo
    {
        /// <summary>
        /// Used to register validation rules.
        /// </summary>
        public static readonly ValidationRulesManager ValidationRules = new();

        Dictionary<string, PropertyErrorInfoList>? _allErrors;

        Dictionary<string, PropertyErrorInfoList> GetOrCreateErrors() => _allErrors ??= new();

        internal Dictionary<string, List<ValidationRule>> CustomRulesPerInstance => _customRulesPerInstance ??= new();

        Dictionary<string, List<ValidationRule>>? _customRulesPerInstance;

        internal bool HasCustomRulesPerInstance => _customRulesPerInstance?.Count > 0;

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

                return _allErrors.Values.SelectMany(errlist => (IEnumerable<ValidationErrorInfo>)errlist).Any();
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
        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
        {
            return GetErrorsCore(propertyName);
        }

        public IEnumerable<string> GetErrorMessages(string? propertyName = null)
        {
            return GetErrorsCore(propertyName)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        IEnumerable<ValidationErrorInfo> GetErrorsCore(string? propertyName = null)
        {
            if (!HasErrors)
            {
                return Enumerable.Empty<ValidationErrorInfo>();
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                // Return all data errors of all properties.
                return GetOrCreateErrors().Values
                    .SelectMany(errlist => (IEnumerable<ValidationErrorInfo>)errlist)
                    .DistinctBy(x => x.ErrorCode);
            }
            else
            {
                // Return data errors of a specific property.
                return GetPropertyErrors(propertyName, createIfMissing: false)
                    ?? Enumerable.Empty<ValidationErrorInfo>();
            }
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
                     .SelectMany(errlist => (IEnumerable<ValidationErrorInfo>)errlist)
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
            GetPropertyErrors(propertyName, createIfMissing: false)?
                .FirstOrDefault()?.ErrorMessage ?? "";

        // IDataErrorInfo End ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public virtual bool Validate()
        {
            var propertyNames = ValidationRules.GetRegisteredPropertyNames(GetType());
            if (propertyNames == null)
                return true;

            var context = CreateValidationContext();

            foreach (string propertyName in propertyNames)
            {
                Validate(context, propertyName);
            }

            ProcessValidationResult(context);

            return !HasErrors;
        }

        static RulesValidationContext CreateValidationContext()
        {
            return new RulesValidationContext();
        }

        public void ValidateProperty(string propertyName)
        {
            // We will validate the property...
            // 1)    if it is annotated with at least one ValidationAttribute
            // 2) OR if it was registered with a custom validation rule.
            if (!ValidationRules.IsRegisteredProperty(GetType(), propertyName))
                return;

            var context = CreateValidationContext();

            Validate(context, propertyName);

            ProcessValidationResult(context);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            if ((_internalFlags & InternalFlags.IsDeserializing) != 0)
                return;

            ValidateProperty(propertyName);
        }

        void Validate(RulesValidationContext context, string propertyName)
        {
            context.ContextPropertyName = propertyName;
            context.InvolvedPropertyNames = null;

            // Validate using rules per type.
            var rulesOfType = ValidationRules.GetRules(GetType(), propertyName);
            Validate(context, rulesOfType.Where(x => x.Kind == ValidationSettingsKind.Custom));

            // Validate using rules per instance.
            Validate(context, ValidationRules.GetRules(this, propertyName));

            // Validate using validation attributes.
            Validate(context, rulesOfType.OfType<ValidationAttributeRule>());
        }

        void ProcessValidationResult(RulesValidationContext context)
        {
            // Notify.
            if (context.ChangedPropertyNames.Any())
            {
                foreach (var changedProperty in context.ChangedPropertyNames)
                {
                    RaisePropertyChangedCore(changedProperty);
                    OnPropertyErrorsChanged(changedProperty);
                }

                RaisePropertyChanged(HasErrorsChangedEventArgs);
            }
        }

        static readonly PropertyChangedEventArgs HasErrorsChangedEventArgs = new(nameof(HasErrors));

        protected virtual void OnPropertyErrorsChanged(string propertyName)
        {
            InvokeErrorsChanged(propertyName);
        }

        void InvokeErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        void Validate(RulesValidationContext context, IEnumerable<ValidationRule> rules)
        {
            foreach (ValidationRule rule in rules)
            {
                ApplyRule(context, rule);
            }
        }

        void ApplyRule(RulesValidationContext context, ValidationRule rule)
        {
            object? errorObj = rule.Validate(this);

            context.InvolvedPropertyNames = rule.PropertyNames;

            if (errorObj == null)
            {
                // An error will be removed if the validation returned null.

                if (rule.ErrorCode == null)
                {
                    throw new Exception(
                        $"The validation for property(ies) '{string.Join(", ", rule.PropertyNames)}' " +
                        "returned NULL, but no ErrorCode was specified. " +
                        "An ErrorCode is needed in order to remove validation errors.");
                }

                RemoveError(context, rule.ErrorCode);
            }
            else if (errorObj is ValidationErrorInfo errorInfo)
            {
                AddError(context, errorInfo);
            }
            else if (errorObj is IEnumerable<ValidationErrorInfo> errorInfoList)
            {
                foreach (ValidationErrorInfo error in errorInfoList)
                {
                    AddError(context, error);
                }
            }
            else if (errorObj is string errorMessage)
            {
                if (rule.ErrorCode == null)
                {
                    throw new Exception(
                        $"The validation for property(ies) '{string.Join(", ", rule.PropertyNames)}' returned a string, " +
                        "but no ErrorCode was specified.");
                }

                AddError(context, CreateError(rule.ErrorCode, errorMessage));
            }
            else throw new Exception(
                $"The validation engine cannot process results of type '{errorObj.GetType()}'.");
        }

        void Validate(RulesValidationContext context, IEnumerable<ValidationAttributeRule> rules)
        {
            var attributeValidationContext = new ValidationContext(this, null, null);

            foreach (var propGroup in rules.GroupBy(x => x.PropertyInfo))
            {
                var propInfo = propGroup.Key;
                var propName = propInfo.Name;
                var propDisplayName = propInfo.GetCustomAttribute<DisplayAttribute>(true)?.GetName() ?? propName;
                object? propValue = propInfo.GetValue(this, null);

                foreach (var rule in propGroup.AsEnumerable())
                {
                    string? errorMsg = null;
                    try
                    {
                        attributeValidationContext.MemberName = propName;
                        attributeValidationContext.DisplayName = propDisplayName;
                        rule.Attribute.Validate(propValue, attributeValidationContext);
                    }
                    catch (Exception ex)
                    {
                        errorMsg = rule.Attribute.ErrorMessage;

                        if (string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg = ex.Message;

                            if (string.IsNullOrWhiteSpace(errorMsg))
                            {
                                errorMsg = $"Unknown attribute validation error using '{rule.Attribute.GetType()}'.";
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        AddError(context, propName, CreateError(rule.ErrorCode, errorMsg));
                    }
                    else
                    {
                        RemoveError(context, propName, rule.ErrorCode);
                    }
                }
            }
        }

        static ValidationErrorInfo CreateError(object errorCode, string errorMessage)
        {
            return new ValidationErrorInfo
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }

        PropertyErrorInfoList? GetPropertyErrors(string propertyName, bool createIfMissing = true)
        {
            var errors = GetOrCreateErrors();
            if (!errors.TryGetValue(propertyName, out PropertyErrorInfoList? propertyErrors) && createIfMissing)
            {
                propertyErrors = new PropertyErrorInfoList(propertyName);
                errors.Add(propertyName, propertyErrors);
            }

            return propertyErrors;
        }

        void RemoveError(RulesValidationContext context, object errorCode)
        {
            if (context.InvolvedPropertyNames == null)
            {
                return;
            }

            foreach (var propertyName in context.InvolvedPropertyNames)
            {
                RemoveError(context, propertyName, errorCode);
            }
        }

        void RemoveError(RulesValidationContext context, string propertyName, object errorCode)
        {
            PropertyErrorInfoList? propertyErrors = GetPropertyErrors(propertyName, createIfMissing: false);
            if (propertyErrors?.Count is not > 0)
            {
                return;
            }

            // Find an error with matching code.
            ValidationErrorInfo? error = propertyErrors.FirstOrDefault(e => e.ErrorCode.Equals(errorCode));
            if (error == null)
            {
                return;
            }

            propertyErrors.Remove(error);

            if (propertyErrors.Count == 0)
            {
                GetOrCreateErrors().Remove(propertyName);
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

        void AddError(RulesValidationContext context, ValidationErrorInfo error)
        {
            if (context.InvolvedPropertyNames?.Length is not > 0)
            {
                return;
            }

            if (error.ErrorCode == null)
            {
                throw new InvalidOperationException($"The ErrorCode of '{nameof(ValidationErrorInfo)}' is missing");
            }

            foreach (var propertyName in context.InvolvedPropertyNames)
            {
                AddError(context, propertyName, error);
            }
        }

        void AddError(RulesValidationContext context, string propertyName, ValidationErrorInfo error)
        {
            PropertyErrorInfoList propertyErrors = GetPropertyErrors(propertyName, createIfMissing: true)!;

            // Check for duplicates (by error code).
            if (propertyErrors.Any(e => e.ErrorCode.Equals(error.ErrorCode)) is true)
            {
                return;
            }

            propertyErrors.Add(error);

            context.AddChangedProperty(propertyName);
        }

        internal void RemoveValidationRulePerInstance(string propertyName, object errorCode)
        {
            if (_customRulesPerInstance == null)
                return;

            var context = CreateValidationContext();
            context.ContextPropertyName = propertyName;

            RemoveError(context, propertyName, errorCode);

            // Remove the validation rule.
            // Side note: http://english.stackexchange.com/questions/25931/unregister-vs-deregister
            if (CustomRulesPerInstance.TryGetValue(propertyName, out List<ValidationRule>? rules))
            {
                ValidationRule? rule = rules?.FirstOrDefault(x => x.ErrorCode.Equals(errorCode));
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

        class RulesValidationContext
        {
            List<string>? _changedPropertyNames;
            public string ContextPropertyName = "";
            public string[]? InvolvedPropertyNames;

            public void AddChangedProperty(string propertyName)
            {
                if (_changedPropertyNames == null)
                    _changedPropertyNames = new List<string>();

                if (_changedPropertyNames.Contains(propertyName))
                    return;

                _changedPropertyNames.Add(propertyName);
            }

            public IReadOnlyList<string> ChangedPropertyNames
                => (IReadOnlyList<string>?)_changedPropertyNames ?? Array.Empty<string>();
        }

        class PropertyErrorInfoList : List<ValidationErrorInfo>
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