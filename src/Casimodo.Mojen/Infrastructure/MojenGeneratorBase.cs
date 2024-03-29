﻿using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Casimodo.Mojen
{
    public class MojXAttribute(XName name, object value)
        : XAttribute(name, value)
    {
        public string Target { get; set; }
    }

    public static class MojenGeneratorExtensions
    {
        //public static T Parent<T>(this T generator, MojenGeneratorBase gen)
        //    where T : MojenGeneratorBase
        //{
        //    generator.Parent(gen);
        //    return generator;
        //}

        //public static T UseWriter<T>(this T generator, MojenGenerator gen)
        //where T : MojenGenerator
        //{
        //    generator.Initialize(gen.App);
        //    generator.Use(gen);
        //    return generator;
        //}

        public static T SetParent<T>(this T generator, MojenGenerator gen)
            where T : MojenGenerator
        {
            generator.Initialize(gen.App);
            generator.SetParent(gen);
            return generator;
        }
    }

    public class MojenGeneratorBase : MojBase
    {
        const int IndentWidth = 4;

        MojenGeneratorBase _core;
        bool _isChild;

        public string Lang { get; set; } = null;

        public TextWriter Writer
        {
            get
            {
                if (_buffer != null)
                    return _buffer;

                if (_core != null)
                {
                    return _core.Writer;
                }

                return _writer;
            }
        }
        TextWriter _writer;

        protected void StartBuffer()
        {
            _buffer = new StringWriter();
        }

        StringWriter _buffer;

        protected string BufferedText
        {
            get { return _buffer.ToString(); }
        }

        protected void FlushBuffer()
        {
            var buffer = _buffer;
            _buffer = null;
            Writer.Write(buffer.ToString());
        }

        protected void EndBuffer()
        {
            _buffer = null;
        }

        public void Use(TextWriter writer)
        {
            _writer = writer;
        }

        internal void Use(MojenGeneratorBase generator)
        {
            if (generator != this)
                _core = generator;
            else
                _core = null;
        }

        public void Br()
        {
            Write(Environment.NewLine);
        }

        public void Begin()
        {
            O("{");
            PushBlockIndent();
        }

        public void End(string text = "")
        {
            OEnd(text);
        }

        void OEnd(string text = "")
        {
            var block = PopBlockIndent();
            if (string.IsNullOrWhiteSpace(text))
                O(block.EndToken);
            else
            {
                Oo(block.EndToken);
                oO(text);
            }
        }

        public void Oeo(string text = "")
        {
            var block = PopBlockIndent();
            if (string.IsNullOrWhiteSpace(text))
                Oo(block.EndToken);
            else
            {
                Oo(block.EndToken);
                o(text);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public void o(string text)
#pragma warning restore IDE1006 // Naming Styles
        {
            Write(false, text, null);
        }

        public void Oo(string text)
        {
            Write(true, text, null);
        }

        void Write(bool indent, string text, params object[] args)
        {
            if (args?.Length is not > 0)
                Write("".PadLeft(GetIndent(indent) * IndentWidth, ' ') + text);
            else
                Write("".PadLeft(GetIndent(indent) * IndentWidth, ' ') + string.Format(text, args));
        }

#pragma warning disable IDE1006 // Naming Styles
        public void oO(string text)
#pragma warning restore IDE1006 // Naming Styles
        {
            Write(false, text, null);
            Br();
        }

        public void O(string text) // , params object[] args)
        {
            if (text == null) return;
            Write(true, text, null);
            Br();
        }

        public void OFormat(string text, params object[] args)
        {
            if (text == null) return;
            Write(true, text, args);
            Br();
        }

        public void OCommentSection(string text = null)
        {
            var indent = Indent * IndentWidth;
            if (string.IsNullOrWhiteSpace(text))
                O($"// {"".PadRight(79 - indent - 3, '~')}");
            else
                O($"// {text} {"".PadRight(79 - indent - text.Length - 4, '~')}");
        }

        public void OComment(string text = null)
        {
            if (!string.IsNullOrWhiteSpace(text))
                O($"// {text}");
        }

        public XBuilder XP(bool condition, string name, Action value)
        {
            return condition ? XP(name, value) : XBuilder.Null;
        }

        public XBuilder XP(string name, Action value)
        {
            return new XBuilder(name, value);
        }

        public XBuilder XP(string expression)
        {
            var x = GetJsObjectLiteralNameAndValue(expression);

            var builder = new XBuilder(x.Item1, x.Item2);
            builder.Text(false);

            return builder;
        }

        public XBuilder XP(bool condition, string name, params object[] content)
        {
            return condition ? XP(name, content) : XBuilder.Null;
        }

        public XBuilder XP(string name, params object[] content)
        {
            return XPropCore(name, content: content);
        }

        public XBuilder XObj(params object[] content)
        {
            return XPropCore(XBuilder.CreateAnonymous(), content: content);
        }

        XBuilder XPropCore(string name, params object[] content)
        {
            return XPropCore(new XBuilder(name), content);
        }

        XBuilder XPropCore(XBuilder builder, params object[] content)
        {
            string text;
            XBuilder b;
            foreach (var item in content)
            {
                if (item == null) continue;

                text = item as string;
                if (text != null)
                {
                    var x = GetJsObjectLiteralNameAndValue(text);

                    builder.Add(name: x.Item1, value: x.Item2);

                    continue;
                }

                b = item as XBuilder;
                if (b != null)
                {
                    if (b == XBuilder.Null)
                        continue;

                    builder.Elem.Add(b.Elem);

                    continue;
                }

                if (item is bool boolValue)
                {
                    builder.Elem.Add(XmlConvert.ToString(boolValue));
                    continue;
                }

                if (item is int intValue)
                {
                    builder.Elem.Add(XmlConvert.ToString(intValue));
                    continue;
                }

                if (item is decimal decimalValue)
                {
                    builder.Elem.Add(XmlConvert.ToString(decimalValue));
                    continue;
                }

                throw new MojenException($"Syntax error: invalid item type '{item.GetType().Name}' in JS object literal.");
            }

            return builder;
        }

        Tuple<string, string> GetJsObjectLiteralNameAndValue(string expression)
        {
            // TODO: Will break if the name contains a colon.
            var index = expression.IndexOf(':');
            if (index == -1)
                throw new MojenException("Syntax error: a colon was expected in JS object literal.");

            return Tuple.Create(
                expression[0..index].Trim(),
                expression[(index + 1)..].Trim()
            );
        }

        public void OXP(string name, params object[] content)
        {
            var builder = XPropCore(name, content: content);
            builder.Complex();
            OJsObjectLiteral(builder.Elem);
        }

        public void OXArr(string name, Action content)
        {
            var builder = new XBuilder(name);
            builder.Array();
            builder.Add(content);
            OJsObjectLiteral(builder.Elem, isArray: true);
        }

        public void OXArr(string name, params object[] content)
        {
            var builder = XPropCore(name, content: content);
            builder.Array();
            OJsObjectLiteral(builder.Elem, isArray: true);
        }

        public MojenGeneratorBase OB(string text, Action action)
        {
            OB(text);
            action();

            return this;
        }

        /// <summary>
        /// Begins a function or HTML/XML element.
        /// Used by HTML/XML and JavaScript/TypeScript generators.
        /// </summary>
        public void OBegin(string text, Action content = null, string end = null)
        {
            OB(text);
            if (content != null)
            {
                content();
                End(end);
            }
        }

        public void OTag(string tag, params object[] content)
        {
            Oo($"<{tag}");

            bool hasContent = false;
            foreach (var obj in content)
            {
                if (obj is string attr)
                {
                    o(" " + attr);
                }
                else if (obj is Action action)
                {
                    hasContent = true;
                    oO(">");
                    Push();
                    action();
                }
            }
            if (hasContent)
            {
                Pop();
                O($"</{tag}>");
            }
            else
            {
                oO(" />");
            }
        }

        /// <summary>
        /// Begins a function or HTML/XML element.
        /// Used by HTML/XML and JavaScript/TypeScript generators.
        /// </summary>
        public void OB(string text, params object[] args)
        {
            Write(true, text, args);

            if (!text.StartsWith('<'))
            {
                // If no XML element then start block.

                if (Lang == "C#")
                {
                    Br();
                    O("{");
                }
                else if (string.IsNullOrEmpty(text))
                    o("{");
                else
                    o(" {");
            }

            if (Lang != "C#")
                Br();
            PushBlockIndent();
        }

        protected void XB(string text, params object[] args)
        {
            Write(true, text, args);
            Br();
            PushBlockIndent();
        }

        /// <summary>
        /// Begins a function or HTML/XML element.
        /// Used by HTML/XML and JavaScript/TypeScript generators.
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public void ob(string text, params object[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            Guard.ArgNotNull(text, nameof(text));

            Write(false, text, args);

            if (!text.StartsWith('<'))
                // If not XML element, then assume JavaScript and start block.
                o(" {");

            Br();
            PushBlockIndent();
        }

        /// <summary>
        /// Ends an HTML/XML element. Used by HTML/XML generators only.
        /// </summary>
        protected void XE(string tag, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentNullException(nameof(tag));
            if (!tag.StartsWith("</")) throw new ArgumentException("Not an XML end tag.");

            PopBlockIndent();
            Write(true, tag, args);
            Br();
        }

        public void O()
        {
            Writer.WriteLine("");
        }

        void Write(string text)
        {
            Writer.Write(text);
        }

        int GetIndent(bool indent = true)
        {
            if (!indent)
                return 0;

            return Indent;
        }

        public int Indent
        {
            get
            {
                if (_isChild)
                    return _core.Indent;

                return _pushIndent + _blockIndent + _customIndent;
            }
        }

        readonly Stack<int> ExplicitIntends = new();
        int _pushIndent;
        int _customIndent;

        protected void CustomIndent(int indent)
        {
            _customIndent = indent;
        }

        public void Push(int indent = 1)
        {
            if (_isChild)
            {
                _core.Push(indent);
                return;
            }
            _pushIndent += indent;
            ExplicitIntends.Push(indent);
        }

        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public void Pop()
        {
            if (_isChild)
            {
                _core.Pop();
                return;
            }
            var indent = ExplicitIntends.Pop();
            _pushIndent -= indent;
        }

        public class BlockInfo
        {
            public string StartToken { get; set; } = "{";
            public string EndToken { get; set; } = "}";
        }

        static readonly BlockInfo CodeBlock = new();
        public static readonly BlockInfo ArrayBlock = new() { StartToken = "[", EndToken = "]" };

        readonly List<BlockInfo> Blocks = [];
        int _blockIndent;

        void PushBlockIndent(BlockInfo block = null)
        {
            if (_isChild)
            {
                _core.PushBlockIndent(block);
                return;
            }

            block ??= CodeBlock;

            if (Blocks.Count <= _blockIndent)
                Blocks.Add(block);
            else
                Blocks[_blockIndent] = block;

            _blockIndent++;
        }

        BlockInfo PopBlockIndent()
        {
            if (_isChild)
            {
                return _core.PopBlockIndent();
            }

            _blockIndent--;
            if (_blockIndent < 0)
                throw new MojenException("Block tree mismatch.");

            return Blocks[_blockIndent];
        }

        internal void SetParent(MojenGeneratorBase generator)
        {
            _core = generator;
            _isChild = true;
            //_bracketIndent = generator._bracketIndent;
            //_explicitIndent = generator._explicitIndent;
        }

        public string BuildAttr(MojAttr attr)
        {
            return attr.ToString();
        }

        public void OGeneratedFileComment()
        {
            O("// This file was generated.");
        }

        public void OUsing(params object[] namespaces)
        {
            OUsingCore(namespaces);
            O();
        }

        void OUsingCore(IEnumerable<object> namespaces)
        {
            IEnumerable<string> enumstring;
            foreach (var obj in namespaces)
            {
                enumstring = obj as IEnumerable<string>;
                if (enumstring != null)
                {
                    foreach (var ns in enumstring)
                        OUsingSingle(ns);
                }
                else if (obj is string @string)
                {
                    OUsingSingle(@string);
                }
                else if (obj == null)
                {
                    // NOP
                }
                else throw new MojenException($"Unknown namespace type (object: '{obj}').");
            }
        }

        void OUsingSingle(string ns)
        {
            O($"using {ns};");
        }

        public void ORazorGeneratedFileComment()
        {
            O("@* [Casimodo.Mojen:file-origin=generated]");
            O("   This is a GENERATED file. Manual changes will be overwritten. *@");
        }

        public void ORazorComment(string comment)
        {
            O($"@* {comment} *@");
        }

        public void ORazorStyleSection(Action content)
        {
            ORazorSection("Styles", content);
        }

        public void ORazorScriptSection(Action content)
        {
            ORazorSection("BottomScripts", content);
        }

        public void ORazorSection(string name, Action content)
        {
            OB("@section " + name);
            content?.Invoke();
            End();
        }

        public void ORazorUsing(params string[] namespaces)
        {
            ORazorUsing(namespaces.AsEnumerable());
        }

        public void ORazorUsing(IEnumerable<string> namespaces)
        {
            foreach (var ns in namespaces)
                O($"@using {ns}");
        }

        public void ONamespace(string ns, Action content = null)
        {
            O($"namespace {ns}");
            Begin();
            if (content != null)
            {
                content();
                End();
            }
        }

        public void OFileScopedNamespace(string ns, Action content = null)
        {
            O($"namespace {ns};");

            content?.Invoke();
        }

        public void OSummary(IEnumerable<string> text)
        {
            if (text == null || !text.Any() || text.All(x => string.IsNullOrWhiteSpace(x)))
                return;

            O("/// <summary>");
            foreach (var t in text)
                O("/// " + Moj.CollapseWhitespace(t));
            O("/// </summary>");
        }

        public void OSummary(object obj)
        {
            if (obj == null)
                return;

            if (obj is MojSummaryConfig summary)
            {
                if (summary.Descriptions.Count > 0)
                {
                    O("/// <summary>");
                    foreach (var txt in summary.Descriptions)
                        O("/// " + Moj.CollapseWhitespace(txt));
                    O("/// </summary>");
                }
                if (summary.Remarks.Count > 0)
                {
                    O("/// <remarks>");
                    foreach (var txt in summary.Remarks)
                        O("/// " + Moj.CollapseWhitespace(txt));
                    O("/// </remarks>");
                }
            }
            else if (obj is string[] stringArray)
                OSummary(stringArray);
            else if (obj is string @string)
                OSummary(@string);
            else if (obj is IEnumerable<string> enumerableString)
                OSummary(enumerableString);
            else throw new MojenException(string.Format("Unexpected summary object type '{0}'.", obj.GetType().Name));
        }

        public void OSummary(params string[] text)
        {
            if (text == null)
                return;

            OSummary(text.AsEnumerable());
        }

        public string FirstCharToLower(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            var pre = "";
            while (str[pre.Length] == '_')
                pre += "_";

            return pre + char.ToLowerInvariant(str[pre.Length]) + str[(pre.Length + 1)..];
        }

        public void WriteTo(MojenGeneratorBase generator, Action action)
        {
            var prevcore = _core;
            _core = generator;
            action();
            _core = prevcore;
        }

        protected virtual void OGeneratedFileDeclaration()
        {
            // NOP
        }

        public void PerformWrite(string outputFilePath, Action callback)
        {
            PerformWrite(outputFilePath, (stream, writer) => callback());
        }

        public void PerformWrite(string outputFilePath, Action<TextWriter> callback)
        {
            PerformWrite(outputFilePath, (stream, writer) => callback(writer));
        }

        static readonly UTF8Encoding MyUT8Encoding = new(true, true);
        static readonly MemoryStream SharedOutputStream = new(129024);
        static readonly byte[] SharedComparisonBuffer = new byte[4096];

        static readonly List<string> AllOutputFilePaths = [];

        protected void PerformWrite(string outputFilePath, Action<Stream, TextWriter> callback)
        {
            if (outputFilePath.Contains('~'))
                throw new MojenException($"Invalid output file path '{outputFilePath}'.");

            if (!AllOutputFilePaths.Contains(outputFilePath))
                AllOutputFilePaths.Add(outputFilePath);
            else
                System.Diagnostics.Debug.WriteLine($"# CodeGen: Duplicate file output: '{outputFilePath}'");

            var stream = SharedOutputStream;
            byte[] outputData = null;
            int outputLength = 0;

            using (var writer = new StreamWriter(stream, MyUT8Encoding, 8192, leaveOpen: true))
            {
                Use(writer);

                OGeneratedFileDeclaration();

                callback(stream, writer);

                writer.Flush();

                outputData = stream.GetBuffer();
                outputLength = (int)stream.Position;

                stream.Position = 0;
            }

            var exists = File.Exists(outputFilePath);
            if (exists)
            {
                // System.Diagnostics.Debug.WriteLine("# CodeGen: Comparing file: " + Path.GetFileName(outputFilePath));

                if (!FileContentDiffers(outputFilePath, outputLength, outputData))
                {
                    // The file content has not changed.
                    // System.Diagnostics.Debug.WriteLine("# CodeGen: No change: " + Path.GetFileName(outputFilePath));
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("# CodeGen: Changed: " + Path.GetFileName(outputFilePath));
                }
            }

            string outputDirPath = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputDirPath))
                Directory.CreateDirectory(outputDirPath);

            using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs, MyUT8Encoding))
            {
                writer.Write(outputData, 0, outputLength);
            }
        }

        bool FileContentDiffers(string filePaht, int length, byte[] data)
        {
            bool differs = false;
            var buffer = SharedComparisonBuffer;
            int totalBytesRead = 0, bytesRead;
            using (var fs = new FileStream(filePaht, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs, MyUT8Encoding))
            {
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (totalBytesRead + bytesRead > length)
                    {
                        // Existing file is bigger than the new output.
                        differs = true;
                        break;
                    }

                    if (bytesRead < buffer.Length && totalBytesRead + bytesRead != length)
                    {
                        // Existing file is smaller than the new output.
                        differs = true;
                        break;
                    }

                    for (int i = 0, k = totalBytesRead; i < bytesRead; i++, k++)
                    {
                        if (buffer[i] != data[k])
                        {
                            differs = true;
                            break;
                        }
                    }

                    totalBytesRead += bytesRead;
                }
            }

            return differs || (totalBytesRead != length);
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        // See http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net
#if (false)
        static unsafe bool UnsafeCompare(byte[] a1, int aIndex, byte[] a2, int a2Index)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }
#endif

        public MojXAttribute XA(string name, object value)
        {
            return new MojXAttribute(name, value);
        }

        public XElement XEl(string name, object content)
        {
            return new XElement(name, content);
        }

        public XElement XEl(string name, params object[] content)
        {
            return new XElement(name, content);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void OJsObjectLiteral(XElement cur, bool isSibling = false,
#pragma warning restore IDE0060 // Remove unused parameter
            bool isArray = false, bool trailingNewline = true, bool trailingComma = true, bool leaveOpen = false)
        {
            if (cur == null)
                return;

            var annotation = cur.Annotation<XBuilder.XAnno>();

            if (annotation?.IsAnonymous != true &&
                annotation?.IsContentPlaceholder != true)
            {
                Oo($"{cur.Name.LocalName}: ");

                if (annotation?.ValueAction != null)
                {
                    annotation.ValueAction();
                    // KABU TODO: IMPORTANT: TRICKY: If the action ends with an End() instruction,
                    // then the comma being generated here afterwards will land
                    // at pos 0, because the End() produces an implicit newline.
                }
                else
                {
                    // Output string or value.
                    var tnodes = cur.Nodes().OfType<XText>();
                    if (tnodes.Any())
                    {
                        bool isText = annotation?.IsText ?? false;
                        if (isText) o("\"");
                        foreach (var t in tnodes) o(t.Value);
                        if (isText) o("\"");
                    }
                }
            }

            // Validation
            if ((XElement)cur.NextNode != null)
            {
                if (leaveOpen)
                    throw new MojenException("Cannot leave the element open, because it has following siblings.");

                if (!trailingComma)
                    throw new MojenException("Cannot omit comma, because the element has following siblings.");
            }

            // Process complex content.
            bool endPerformed = false;

            if (annotation?.IsContentPlaceholder == true)
            {
                if (cur.HasElements)
                {
                    OJsObjectLiteral(cur.Elements().First());
                }
                else if (annotation?.ContentAction != null)
                {
                    annotation.ContentAction();
                }
            }
            else if (isArray || cur.HasElements || annotation?.IsComplex == true || annotation?.ContentAction != null)
            {
                var block = isArray ? MojenGenerator.ArrayBlock : MojenGenerator.CodeBlock;
                oO(block.StartToken);
                PushBlockIndent(block);

                if (cur.HasElements)
                {
                    OJsObjectLiteral(cur.Elements().First());
                }
                else if (annotation?.ContentAction != null)
                {
                    annotation.ContentAction();
                }

                Br();

                if (leaveOpen)
                    return;

                block = PopBlockIndent();
                if (trailingNewline)
                    O(block.EndToken + (trailingComma ? "," : ""));
                else
                    Oo(block.EndToken + (trailingComma ? "," : ""));

                endPerformed = true;
            }

            // Process next sibling.
            if ((cur = (XElement)cur.NextNode) != null)
            {
                if (!endPerformed)
                    oO(",");
                OJsObjectLiteral(cur, true);
            }
        }
    }

    public class XBuilder
    {
        public static readonly XBuilder Null = new("null");

        public class XAnno
        {
            public bool IsAnonymous { get; set; }

            public bool IsText { get; set; }

            public bool IsComplex { get; set; }

            public bool IsArray { get; set; }

            public Action ValueAction { get; set; }

            public Action ContentAction { get; set; }

            public bool IsContentPlaceholder { get; set; }
        }

        public static XBuilder CreateAnonymous()
        {
            var builder = new XBuilder("X");
            builder.Anonymous();
            return builder;
        }

        public static XBuilder CreateContentPlaceholder()
        {
            var builder = new XBuilder("X");
            builder.ContentPlaceholder();
            return builder;
        }

        public XBuilder(string name)
        {
            Elem = new XElement(name);
        }

        public XBuilder(string name, Action value)
        {
            Elem = new XElement(name);
            ValueAction(value);
        }

        public XBuilder(string name, string content)
        {
            Elem = new XElement(name, content);
        }

        //XElement CreateElem(string name, object annotation = null)
        //{
        //    var elem = new XElement(name);
        //    if (annotation != null)
        //        elem.AddAnnotation(annotation);

        //    return elem;
        //}

        public void Text(bool value = true)
        {
            Anno().IsText = value;
        }

        public void Complex(bool value = true)
        {
            Anno().IsComplex = value;
        }

        public void Array(bool value = true)
        {
            Anno().IsArray = value;
        }

        public void ContentAction(Action action)
        {
            Anno().ContentAction = action;
        }

        public void ValueAction(Action action)
        {
            Anno().ValueAction = action;
        }

        public void Anonymous()
        {
            Anno().IsAnonymous = true;
            Elem.Name = "Anonymous-Object";
        }

        public void ContentPlaceholder()
        {
            Anno().IsContentPlaceholder = true;
            Elem.Name = "Content-Placeholder";
        }

        XAnno Anno()
        {
            var annotation = Elem.Annotation<XAnno>();
            if (annotation == null)
            {
                annotation = new XAnno();
                Elem.AddAnnotation(annotation);
            }

            return annotation;
        }

        public XBuilder Parent { get; set; }

        public XElement Elem { get; set; }

        /// <summary>
        /// Adds an anynomous object.
        /// </summary>
        public XBuilder Add(Action content)
        {
            XBuilder b = XBuilder.CreateContentPlaceholder();
            b.Parent = this;
            b.ContentAction(content);
            Elem.Add(b.Elem);

            return b;
        }

        public XBuilder Add(string name, Action value)
        {
            XBuilder b = new(name) { Parent = this };
            b.ValueAction(value);
            Elem.Add(b.Elem);

            return b;
        }

        public XBuilder Add(string name, object value = null, bool text = false)
        {
            if (Elem == null)
            {
                Elem = new XElement("X");
                Anonymous();
            }

            XBuilder b = new(name) { Parent = this };
            b.Value(value, text);

            Elem.Add(b.Elem);

            return b;
        }

        void Value(object value, bool text = false)
        {
            if (value is Action action)
            {
                ValueAction(action);
            }
            else
            {
                Elem.Add(value);
                if (text && value is string)
                    Text(true);
            }
        }

        public XBuilder End()
        {
            return Parent;
        }
    }
}