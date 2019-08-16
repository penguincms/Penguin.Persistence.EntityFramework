using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Validation;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class RequiredAttributeBuilder : PropertyBuilder<RequiredAttribute>
    {
        #region Constructors

        public RequiredAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            object propertyConfiguration = this.Property<T>(modelBuilder);

            MethodInfo RequiredMethod = propertyConfiguration.GetType().GetMethod(nameof(PrimitivePropertyConfiguration.IsRequired));

            RequiredMethod.Invoke(propertyConfiguration, new object[] { });
        }

        #endregion Methods
    }
}