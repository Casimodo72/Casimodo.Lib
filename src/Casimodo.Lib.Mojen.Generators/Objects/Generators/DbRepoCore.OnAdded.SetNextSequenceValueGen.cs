using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// If a parent object is added then also update its nested-referenced objects.
    /// </summary>
    public class DbRepoCoreOnAddedSetNextSequenceValueGen : DbRepoCoreGenBase
    {
        public DbRepoCoreOnAddedSetNextSequenceValueGen()
        {
            Scope = "DataContext";

            Name = "OnAdded.SetNextSequenceValue";
            OnAnyTypeMethodName = "OnAddedSetNextSequenceValueAny";
            OnTypeMethodName = "OnAddedSetNextSequenceValue";
            ItemName = "item";
            UseRepositoriesContext = false;

            // Select types which have sequence props with Unique.PerTypes.
            SelectTypes = (types) => types.Select(t => new DbRepoCoreGenItem(t)
            {
                Props = SelectProps(t).Where(prop =>
                     prop.DbAnno.Sequence.Is &&
                    !prop.DbAnno.Sequence.IsDbSequence)
                    .ToArray()
            })
            .Where(t => t.Props.Any());
        }

        public override void OProp()
        {
            // Example:
            // bool OnAddedSetNextSequenceValue(ContractEntity contract, DbContext db)
            // {
            //    if (contract == null) return false;
            //    if (contract.RawNumber == null || contract.RawNumber < 10000)
            //        contract.RawNumber = GetNextSequenceValueForContractRawNumber(db, contract);
            //    return true;
            // }

            var item = Current.Item;
            var prop = Current.Prop;
            var method = prop.GetNextSequenceValueMethodName();

            var conditions = new List<string>();

            if (prop.Type.CanBeNull)
                conditions.Add($"{item}.{prop.Name} == null");

            var sequence = prop.DbAnno.Sequence;
            if (sequence.Start != null)
                conditions.Add($"{item}.{prop.Name} < {sequence.Start}");
            else
                conditions.Add($"{item}.{prop.Name} < {sequence.Min}");

            if (conditions.Any())
            {
                O($"if ({conditions.Join(" || ")})");
            }
            O($"    {item}.{prop.Name} = {method}(db, {item});");
        }
    }
}