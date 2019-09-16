using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class HasRequiredAttributeBuilder : PropertyBuilder<HasRequiredAttribute>
    {
        public HasRequiredAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo HasRequiredMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<T>.HasRequired)).MakeGenericMethod(Member.PropertyType);

            HasRequiredMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(Member.ReflectedType, Member.Name) });
        }
    }
}