using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class OptionalToRequiredAttributeBuilder : PropertyBuilder<OptionalToRequiredAttribute>
    {
        public OptionalToRequiredAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            Mapping mapping = Attribute.GetMapping(Member);

            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo hasOptionalMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<object>.HasOptional)).MakeGenericMethod(Member.PropertyType);

            object optionalNavigationPropertyConfiguration = hasOptionalMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(typeof(T), mapping.Left.Property) });

            //With Required
            MethodInfo withRequiredMethod = optionalNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.GetParameters().Count() == 1 && m.Name == nameof(OptionalNavigationPropertyConfiguration<object, object>.WithRequired));

            withRequiredMethod.Invoke(optionalNavigationPropertyConfiguration, new[] { PropertyExpression(Member.PropertyType, mapping.Right.Property) });
        }
    }
}