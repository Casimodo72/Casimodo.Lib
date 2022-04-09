namespace Casimodo.Mojen
{
    public abstract class DataInteractionPartGenerator : MojenGenerator
    {
        public DataInteractionLayerContext Context
        {
            get { return App.Contexts.OfType<DataInteractionLayerContext>().First(); }
        }

        
    }
}
