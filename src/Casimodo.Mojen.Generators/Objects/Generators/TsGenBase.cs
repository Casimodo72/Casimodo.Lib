﻿namespace Casimodo.Lib.Mojen
{
    public class TsGenBase : DataLayerGenerator
    {
        protected override void OGeneratedFileDeclaration()
        {
            O("// NOTE: This file was generated by the Casimodo.Lib.Mojen tool. Manual changes will be overwritten.");
            O();
        }
    }
}