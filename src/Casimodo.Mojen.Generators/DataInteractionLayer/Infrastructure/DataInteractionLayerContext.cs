namespace Casimodo.Mojen
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
