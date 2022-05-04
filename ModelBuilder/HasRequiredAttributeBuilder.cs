using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using Penguin.Persistence.Abstractions.Enums;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
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
            Mapping mapping = this.Attribute.GetMapping(this.Member, RightPropertyRequirement.SingleOrNull);

            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo HasRequiredMethod = entityTypeConfiguration.GetType()
                                                                  .GetMethod(nameof(EntityTypeConfiguration<object>.HasRequired))
                                                                  .MakeGenericMethod(this.Member.PropertyType);

            object optionalNavigationPropertyConfiguration = HasRequiredMethod.Invoke(entityTypeConfiguration,
                                                                                    new[] {
                                                                                        PropertyExpression(typeof(T),
                                                                                        mapping.Left.Property)
                                                                                    });

            if (mapping.Right.PropertyFound)
            {
                //We're calling the build method on a type that doesn't match the declaring property type
                if (PropertyExpression(this.Member.PropertyType, mapping.Right.Property).ReturnType != typeof(T))
                {
                    return;
                }

                //With Required
                MethodInfo withOptionalDependentMethod = optionalNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.GetParameters().Length == 1 && m.Name == nameof(OptionalNavigationPropertyConfiguration<object, object>.WithOptionalDependent));

                _ = withOptionalDependentMethod.Invoke(optionalNavigationPropertyConfiguration, new[] { PropertyExpression(this.Member.PropertyType, mapping.Right.Property) });
            }
        }
    }
}