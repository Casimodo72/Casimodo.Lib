using System.ComponentModel;
using System.Windows;
using System.Diagnostics;
using System;

namespace Casimodo.Lib.Presentation
{
    public static class DesignTimeHelper
    {
        static bool? _isInDesignMode;

        // Source: http://blog.alner.net/archive/2010/05/06/mvvm-viewmodel-locator-via-wpf-converters.aspx
        public static bool IsInDesignTime
        {
            get
            {
                try
                {
#if (SILVERLIGHT)
                return DesignerProperties.IsInDesignTool;
#else
                    if (_isInDesignMode.HasValue)
                        return _isInDesignMode.Value;

                    _isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());

                    // Another strategy.
                    // See https://stackoverflow.com/questions/834283/is-there-a-way-to-check-if-wpf-is-currently-executing-in-design-mode-or-not 
                    // string appname = System.Reflection.Assembly.GetEntryAssembly().FullName;
                    // return appname.Contains("XDesProc");

                    // TODO: REMOVE?
                    // _isInDesignMode = (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;

#if (false)
                DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
                _isInDesignMode =
                    (bool)DependencyPropertyDescriptor
                    .FromProperty(prop, typeof(FrameworkElement))
                    .Metadata.DefaultValue;
#endif

                    // Just to be sure.
                    if (!_isInDesignMode.Value
                        && Process.GetCurrentProcess().ProcessName.StartsWith("devenv", StringComparison.Ordinal))
                    {
                        _isInDesignMode = true;
                    }
#endif
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception while evaluating IsInDesignTime", ex);
                }
                return _isInDesignMode.Value;
            }
        }
    }
}