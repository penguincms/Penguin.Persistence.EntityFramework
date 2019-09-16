using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Validation;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class StringLengthBuilder : PropertyBuilder<StringLengthAttribute>
    {
        public StringLengthBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            object propertyConfiguration = this.Property<T>(modelBuilder);

            MethodInfo HasMaxLengthMethod = propertyConfiguration.GetType().GetMethod(nameof(StringPropertyConfiguration.HasMaxLength));

            HasMaxLengthMethod.Invoke(propertyConfiguration, new object[] { PersistenceConnectionInfo.ProviderType == ProviderType.SQLCE ? Math.Min(Attribute.Length, 4000) : Attribute.Length });
        }
    }
}