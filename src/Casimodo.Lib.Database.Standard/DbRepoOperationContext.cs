using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public abstract class DbRepoOperationContext
    {
        public abstract DbContext GetDb();
        public abstract DbRepoContainer GetRepos();

        public DbRepoOp OriginOperation { get; set; } = DbRepoOp.None;
        public DbRepoOp Operation { get; set; } = DbRepoOp.None;
        public bool IsPhysicalDeletionAuthorized { get; set; }

        public MojDataGraphMask UpdateMask { get; set; }

        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// The root entity of the DB operation.
        /// </summary>
        public object Origin { get; set; }

        public object Item { get; set; }

        public object TargetItem { get; set; }

        public PropertyInfo Prop { get; set; }

        public object OldPropValue { get; set; }
        public object NewPropValue { get; set; }

        /// <summary>
        /// Only used for deletion in order to 
        /// </summary>
        public OperationOriginInfo OriginInfo
        {
            get
            {
                if (_originInfo == null)
                {
                    var id = HProp.GetProp<Guid?>(Origin, "Id", null);
                    if (id == null)
                        throw new DbRepositoryException($"The type '{Origin.GetType().Name}' has no ID property.");

                    _originInfo = new OperationOriginInfo
                    {
                        TypeId = Origin.GetType().FindAttr<TypeIdentityAttribute>().Guid,
                        Id = id.Value
                    };
                }

                return _originInfo;
            }
        }
        OperationOriginInfo _originInfo;

        public void Validate(DbRepoOp op)
        {
            if (!Operation.HasFlag(op))
                throw new DbRepositoryException($"Expected was a repository operation context with an operation of '{op}'.");
        }

        public DbRepoOperationContext CreateSubUpdateOperation(object item, MojDataGraphMask mask = null)
        {
            return CreateSubContext(item, DbRepoOp.Update, mask);
        }

        public DbRepoOperationContext CreateSubAddOperation(object item, MojDataGraphMask mask = null)
        {
            return CreateSubContext(item, DbRepoOp.Add, mask);
        }

        public DbRepoOperationContext CreateSubDeleteOperation(object item = null)
        {
            return CreateSubContext(item, DbRepoOp.Delete);
        }

        public DbRepoOperationContext CreateSubRestoreCascadeDeletedOperation(object item)
        {
            return CreateSubContext(item);
        }

        public DbRepoOperationContext CreateSubContext(object item, DbRepoOp? op = null, MojDataGraphMask mask = null)
        {
            var sub = (DbRepoOperationContext)this.MemberwiseClone();

            sub.Item = item;
            sub.UpdateMask = mask;
            if (op != null)
                sub.Operation = op.Value;

            return sub;
        }
    }
}