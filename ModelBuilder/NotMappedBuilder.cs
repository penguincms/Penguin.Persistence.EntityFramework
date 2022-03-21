using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class NotMappedBuilder : PropertyBuilder<NotMappedAttribute>
    {
        public NotMappedBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(GetBaseProperty(m), persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder) => this.GetType().GetMethod(nameof(NotMappedBuilder.PropertyMethod)).MakeGenericMethod(this.Member.DeclaringType).Invoke(this, new object[] { modelBuilder, nameof(EntityTypeConfiguration<T>.Ignore) });
    }
}