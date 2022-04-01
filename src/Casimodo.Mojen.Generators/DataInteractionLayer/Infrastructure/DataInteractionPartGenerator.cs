using System.Linq;

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
