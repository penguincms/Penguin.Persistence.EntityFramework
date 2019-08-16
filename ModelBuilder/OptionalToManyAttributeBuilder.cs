using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class OptionalToManyAttributeBuilder : PropertyBuilder<OptionalToManyAttribute>
    {
        #region Constructors

        public OptionalToManyAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
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

            //With Many

            //ManyNavigationPropertyConfiguration
            this.WithMany(optionalNavigationPropertyConfiguration, Member, mapping);
        }

        #endregion Methods
    }
}