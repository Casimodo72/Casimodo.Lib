using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public class WebT4TemplateBase : T4TemplateBase
    {
        protected string ReturnRedirectToActionIndex()
        {
            return "return RedirectToAction(\"Index\");";
        }

        protected string ActionLink(string text, string action, string controller)
        {
            return string.Format("@Html.ActionLink(\"{0}\", \"{1}\", \"{2}\")", text, action, controller);
        }

        protected string Paragraph(MojViewProp prop)
        {
            if (prop.IsHeader)
                return "h3";
            else
                return "p";
        }

        protected string HrefUrlAction(string action, MojViewConfig view)
        {
            return string.Format("href='@Url.Action(\"{0}\", \"{1}\")'", action, view.TypeConfig.PluralName);
        }

        protected string HrefUrlActionId(string action, MojViewConfig view, bool isModel = false)
        {
            //var prop = view.Model.Entity.Props.FirstOrDefault(x => x.IsPrimaryKey);
            //if (prop == null)
            //    throw new Exception(string.Format("Model '{0}' of view '{1}/{2}' has no key defined.", view.Model.ClassName, view.Controller.Name, view.Action.Name));

            return string.Format("href='@Url.Action(\"{0}\", \"{1}\", new {{ id = {2}.State.{3} }})'", action, view.TypeConfig.PluralName, (isModel ? "Model" : "item"), view.TypeConfig.Key.Name);
        }

        protected string GetMobileListItemProp(MojViewProp prop)
        {
            return string.Format("<{0}>@item.{1}</{0}>", Paragraph(prop), prop.OrigTargetProp.Name);
        }
    }

    public class T4TemplateBase
    {
        bool _endsWithNewline;

        public virtual string TransformText()
        {
            return null;
        }

        public virtual void Initialize()
        { }

        public string BuildAttr(MojAttr attr)
        {
            return attr.ToString();
        }

        public string FirstCharToLower(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || char.IsLower(str, 0))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected StringBuilder GenerationEnvironment
        {
            get { return _writer != null ? _writer : (_writer = new StringBuilder()); }
            set { _writer = value; }
        }

        StringBuilder _writer;

        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public CompilerErrorCollection Errors
        {
            get { return _errors != null ? _errors : (_errors = new CompilerErrorCollection()); }
        }

        CompilerErrorCollection _errors;

        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private List<int> indentLengths
        {
            get { return _indentLengths != null ? _indentLengths : (_indentLengths = new List<int>()); }
        }

        List<int> _indentLengths;

        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get { return _currentIndent; }
        }

        string _currentIndent = "";

        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual IDictionary<string, object> Session
        {
            get { return _session; }
            set { _session = value; }
        }

        IDictionary<string, object> _session;

        protected void Oo(string text, params object[] args)
        {
            Oo(text, args);
        }

        protected void Oo(int indent, string text, params object[] args)
        {
            if (args == null || args.Length == 0)
                Write("".PadLeft(indent * 4, ' ') + text);
            else
                Write("".PadLeft(indent * 4, ' ') + string.Format(text, args));
        }

        public void O(string text, params object[] args)
        {
            Oo(text + Environment.NewLine, args);
        }

        protected void O(int indent, string text, params object[] args)
        {
            O("".PadLeft(indent * 4, ' ') + text, args);
        }

        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if ((GenerationEnvironment.Length == 0) || _endsWithNewline)
            {
                GenerationEnvironment.Append(_currentIndent);
                _endsWithNewline = false;
            }

            // Check if the current text ends with a newline
            if (text.EndsWith(Environment.NewLine, StringComparison.CurrentCulture))
                _endsWithNewline = true;

            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((_currentIndent.Length == 0))
            {
                GenerationEnvironment.Append(text);
                return;
            }

            // Everywhere there is a newline in the text, add an indent after it
            text = text.Replace(Environment.NewLine, (Environment.NewLine + _currentIndent));

            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (_endsWithNewline)
                GenerationEnvironment.Append(text, 0, (text.Length - _currentIndent.Length));
            else
                GenerationEnvironment.Append(text);
        }

        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            Write(textToAppend);
            GenerationEnvironment.AppendLine();
            _endsWithNewline = true;
        }

        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            Write(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            CompilerError error = new CompilerError();
            error.ErrorText = message;
            Errors.Add(error);
        }

        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            CompilerError error = new CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            Errors.Add(error);
        }

        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if (indent == null)
                throw new ArgumentNullException("indent");

            _currentIndent = (_currentIndent + indent);
            indentLengths.Add(indent.Length);
        }

        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if (indentLengths.Count > 0)
            {
                int indentLength = indentLengths[indentLengths.Count - 1];
                indentLengths.RemoveAt(indentLengths.Count - 1);
                if (indentLength > 0)
                {
                    returnValue = _currentIndent.Substring(_currentIndent.Length - indentLength);
                    _currentIndent = _currentIndent.Remove(_currentIndent.Length - indentLength);
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            indentLengths.Clear();
            _currentIndent = "";
        }

        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public IFormatProvider FormatProvider
            {
                get { return _formatProvider; }
                set
                {
                    if (value != null)
                        _formatProvider = value;
                }
            }

            IFormatProvider _formatProvider = CultureInfo.InvariantCulture;

            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object value)
            {
                if (value == null) throw new ArgumentNullException("value");
                Type t = value.GetType();
                MethodInfo method = t.GetMethod("ToString", new System.Type[] { typeof(IFormatProvider) });
                if (method == null)
                    return value.ToString();
                else
                    return (string)(method.Invoke(value, new object[] { _formatProvider }));
            }
        }

        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get { return _toStringHelper; }
        }

        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
    }
}