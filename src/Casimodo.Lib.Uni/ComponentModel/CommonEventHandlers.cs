using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib
{
    public class SimpleTextEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    public delegate void SimpleTextEventHandler(object sender, SimpleTextEventArgs args);

    public class CategorizedTextEventArgs : SimpleTextEventArgs
    {
        public string Category { get; set; }
    }

    public delegate void CategorizedTextEventHandler(object sender, CategorizedTextEventArgs args);

    public class SimpleDataEventArgs<T> : EventArgs
    {
        public T Data { get; set; }
    }

    public delegate void SimpleDataEventHandler<T>(object sender, SimpleDataEventArgs<T> args);
}
