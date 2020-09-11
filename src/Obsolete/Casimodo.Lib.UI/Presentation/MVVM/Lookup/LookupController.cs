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
    // This is just a real world example of the code needed to use a specific lookup.

#if (false)

    public class LocationLookupScenario
    {
        [Import]
        Lazy<LocationLookupController> LocationLookup { get; set; }

        void LookupLocation(Project project)
        {
            var args = new LocationArgs { Project = project };
            LocationLookup.Value.Execute(args,
                (type) =>
                {
                    // Do something with the lookup-result.
                });
        }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LocationLookupController
    {
        [Import]
        public ExportFactory<ILookupDialogContainerPresenter> DialogFactory { get; set; }

        [Import]
        public ExportFactory<ILocationLookupViewModel> LocationLookupFactory { get; set; }

        public void Execute(object args, Action<Location> resultAction)
        {
            ILookupDialogContainerPresenter dialog = DialogFactory.CreateExport().Value;
            ILocationLookupViewModel lookup = LocationLookupFactory.CreateExport().Value;

            ViewModelBuildHelper.Build(lookup, args);                

            dialog.SetContent(lookup);
            dialog.SetPreferredSize(400, 400);
            dialog.Closed += (s, e) =>
            {
                if (dialog.HasResult)
                    resultAction(lookup.Result.Item);
            };

            dialog.Show();
        }
    }
#endif
}