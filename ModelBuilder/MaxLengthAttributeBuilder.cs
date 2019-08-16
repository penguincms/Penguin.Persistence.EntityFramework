using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Validation;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class MaxLengthAttributeBuilder : PropertyBuilder<MaxLengthAttribute>
    {
        #region Constructors

        public MaxLengthAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            object propertyConfiguration = this.Property<T>(modelBuilder);

            MethodInfo HasMaxLengthMethod = propertyConfiguration.GetType().GetMethod(nameof(BinaryPropertyConfiguration.HasMaxLength));

            HasMaxLengthMethod.Invoke(propertyConfiguration, new object[] { PersistenceConnectionInfo.ProviderType == ProviderType.SQLCE ? Math.Min(Attribute.Length, 4000) : Attribute.Length });
        }

        #endregion Methods
    }
}