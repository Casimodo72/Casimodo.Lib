using Casimodo.Lib;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data.Migrations
{
    // See http://romiller.com/2013/02/27/ef6-writing-your-own-code-first-migration-operations/

    public static class DbMigrationExtensions
    {
        public static void CreateSequence(this DbMigration migration, string name, int start = 1, int inrement = 1, int min = 1, int max = 2147483647)
        {
            ((IDbMigration)migration).AddOperation(new CreateSequenceOperation(name));
        }

        public static void DropSequence(this DbMigration migration, string name)
        {
            ((IDbMigration)migration).AddOperation(new DropSequenceOperation(name));
        }
    }

    public class CustomMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {
        protected override void Generate(MigrationOperation operation)
        {
            var createSequence = operation as CreateSequenceOperation;
            if (createSequence != null)
            {
                using (var writer = Writer())
                {
                    writer.WriteLine(
                        string.Format("CREATE SEQUENCE [dbo].[{0}] AS [int]", createSequence.SequenceName) +
                        @"START WITH 1
                          INCREMENT BY 1
                          MINVALUE 1
                          MAXVALUE 2147483647
                          CACHE;"
                        .CollapseWhitespace());
                    Statement(writer);
                }
            }

            var dropSequence = operation as DropSequenceOperation;
            if (dropSequence != null)
            {
                using (var writer = Writer())
                {
                    writer.WriteLine(string.Format("DROP SEQUENCE [dbo].[{0}];", dropSequence.SequenceName));
                    Statement(writer);
                }
            }
        }
    }

    public class CreateSequenceOperation : MigrationOperation
    {
        public CreateSequenceOperation(string name)
          : base(null)
        {
            if (name == null) throw new ArgumentNullException("name");
            SequenceName = name;
        }

        public string SequenceName { get; private set; }

        public int Start { get; set; }

        public int Increment { get; set; }

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        public override bool IsDestructiveChange
        {
            get { return false; }
        }
    }

    public class DropSequenceOperation : MigrationOperation
    {
        public DropSequenceOperation(string name)
          : base(null)
        {
            if (name == null) throw new ArgumentNullException("name");
            SequenceName = name;
        }

        public string SequenceName { get; private set; }

        public override bool IsDestructiveChange
        {
            get { return false; }
        }
    }
}