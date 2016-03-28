using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class DataInteractionLayerContext : MojenBuildContext
    {
        public DataInteractionLayerContext()
        { }

        public MiaTypeOperationsBuilder BuildActions(MojType type)
        {
            App.Add(type);
            var builder = new MiaTypeOperationsBuilder { Type = type, App = App };

            return builder;
        }        
    }
}
