using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class HasManyAttributeBuilder : PropertyBuilder<HasManyAttribute>
    {
        public HasManyAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            MapMany<T>(modelBuilder, this.Member);
        }
    }
}