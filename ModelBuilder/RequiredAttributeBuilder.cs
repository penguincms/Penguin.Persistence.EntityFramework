using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Validation;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class RequiredAttributeBuilder : PropertyBuilder<RequiredAttribute>
    {
        public RequiredAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            object propertyConfiguration = this.Property<T>(modelBuilder);

            MethodInfo RequiredMethod = propertyConfiguration.GetType().GetMethod(nameof(PrimitivePropertyConfiguration.IsRequired));

            _ = RequiredMethod.Invoke(propertyConfiguration, System.Array.Empty<object>());
        }
    }
}