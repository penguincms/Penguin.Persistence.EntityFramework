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
    internal class HasOptionalAttributeBuilder : PropertyBuilder<HasOptionalAttribute>
    {
        public HasOptionalAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            Mapping mapping = Attribute.GetMapping(Member);

            //We're calling the build method on a type that doesn't match the declaring property type
            if (PropertyExpression(Member.PropertyType, mapping.Right.Property).ReturnType != typeof(T))
            {
                return;
            }

            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo hasOptionalMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<object>.HasOptional)).MakeGenericMethod(Member.PropertyType);

            object optionalNavigationPropertyConfiguration = hasOptionalMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(typeof(T), mapping.Left.Property) });

            //With Required
            MethodInfo withOptionalDependentMethod = optionalNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.GetParameters().Length == 1 && m.Name == nameof(OptionalNavigationPropertyConfiguration<object, object>.WithOptionalDependent));

            _ = withOptionalDependentMethod.Invoke(optionalNavigationPropertyConfiguration, new[] { PropertyExpression(Member.PropertyType, mapping.Right.Property) });
        }
    }
}