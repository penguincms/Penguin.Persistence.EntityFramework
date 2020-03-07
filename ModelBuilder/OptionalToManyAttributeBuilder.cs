using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class OptionalToManyAttributeBuilder : PropertyBuilder<OptionalToManyAttribute>
    {
        public OptionalToManyAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            Mapping mapping = this.Attribute.GetMapping(this.Member);

            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo hasOptionalMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<object>.HasOptional)).MakeGenericMethod(this.Member.PropertyType);

            object optionalNavigationPropertyConfiguration = hasOptionalMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(typeof(T), mapping.Left.Property) });

            //With Many

            //ManyNavigationPropertyConfiguration
            this.WithMany(optionalNavigationPropertyConfiguration, this.Member, mapping);
        }
    }
}