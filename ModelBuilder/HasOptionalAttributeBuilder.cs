using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class HasOptionalAttributeBuilder : PropertyBuilder<HasOptionalAttribute>
    {
        #region Constructors

        public HasOptionalAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            Mapping mapping = Attribute.GetMapping(Member);

            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            MethodInfo hasOptionalMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<object>.HasOptional)).MakeGenericMethod(Member.PropertyType);

            object optionalNavigationPropertyConfiguration = hasOptionalMethod.Invoke(entityTypeConfiguration, new[] { this.PropertyExpression(typeof(T), mapping.Left.Property) });

            //With Required
            MethodInfo withOptionalDependentMethod = optionalNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.GetParameters().Count() == 1 && m.Name == nameof(OptionalNavigationPropertyConfiguration<object, object>.WithOptionalDependent));

            withOptionalDependentMethod.Invoke(optionalNavigationPropertyConfiguration, new[] { this.PropertyExpression(Member.PropertyType, mapping.Right.Property) });
        }

        #endregion Methods
    }
}