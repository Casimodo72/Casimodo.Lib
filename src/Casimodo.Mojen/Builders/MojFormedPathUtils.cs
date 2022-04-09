namespace Casimodo.Mojen
{
    public static class MojFormedPathUtils
    {
        public static MojFormedNavigationPath BuildPath(MojType contextType, MexPropSelection selection)
        {
            var contextProp = contextType.FindReferenceWithForeignKey(to: selection.TypeName, required: true).ForeignKey;

            var path = new MojFormedNavigationPath
            {
                IsForeign = true,
                Steps = new List<MojFormedNavigationPathStep>()
            };

            var targetType = contextProp.Reference.ToType;

            path.AddStep(new MojFormedNavigationPathStep
            {
                SourceType = contextType,
                SourceProp = contextProp,
                TargetType = targetType,
                TargetProp = targetType.GetProp(selection.PropName)
            });

            //var formedTargetType = new MojFormedType(path, targetType);

            path.Build();

            return path;
        }
    }
}
