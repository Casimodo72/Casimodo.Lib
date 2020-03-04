// Copyright (c) 2010 Kasimier Buchcik

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel.Composition;

namespace Casimodo.Lib.Presentation
{
    public enum ViewModelStrategy
    {
        ModelFirst,
        ViewFirst
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class MvvmExportAttribute : ExportAttribute
    {
        public MvvmExportAttribute(Type contractType)
            : base(contractType)
        { }

        public MvvmExportAttribute(Type contractType, ViewModelStrategy strategy)
            : base(contractType)
        {
            Strategy = strategy;
        }

        /// <summary>
        /// The strategy to be used; the default is 'ViewFirst'.
        /// </summary>
        public ViewModelStrategy Strategy { get; set; } = ViewModelStrategy.ModelFirst;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ViewExportAttribute : MvvmExportAttribute
    {
        public ViewExportAttribute(Type contractType)
            : base(contractType)
        { }

        public ViewExportAttribute(Type contractType, ViewModelStrategy strategy)
            : base(contractType, strategy)
        { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ViewModelExportAttribute : MvvmExportAttribute
    {
        public ViewModelExportAttribute(Type contractType)
            : base(contractType)
        { }

        public ViewModelExportAttribute(Type contractType, ViewModelStrategy strategy)
            : base(contractType, strategy)
        { }
    }
}