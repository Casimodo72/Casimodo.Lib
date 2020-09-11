using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public abstract class DataInteractionPartGenerator : MojenGenerator
    {
        public DataInteractionLayerContext Context
        {
            get { return App.Contexts.OfType<DataInteractionLayerContext>().First(); }
        }

        
    }
}
