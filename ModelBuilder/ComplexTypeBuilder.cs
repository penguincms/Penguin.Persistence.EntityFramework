using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System;
using System.Data.Entity;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class ComplexTypeBuilder : TypeBuilder<ComplexTypeAttribute>
    {
        public ComplexTypeBuilder(Type m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            _ = modelBuilder.ComplexType<T>();
        }
    }
}