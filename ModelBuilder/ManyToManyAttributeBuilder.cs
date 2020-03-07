using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "<Pending>")]
    internal class ManyToManyAttributeBuilder : PropertyBuilder<ManyToManyAttribute>
    {
        public ManyToManyAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            Mapping mapping = this.Attribute.GetMapping(this.Member);

            //HasMany
            object manyNavigationPropertyConfiguration = this.MapMany<T>(modelBuilder, this.Member);

            //With Many
            object manyToManyNavigationPropertyConfiguration = this.WithMany(manyNavigationPropertyConfiguration, this.Member, mapping);

            //Map
            Action<ManyToManyAssociationMappingConfiguration> configurationAction = new Action<ManyToManyAssociationMappingConfiguration>((config) =>
            {
                config.MapLeftKey(mapping.Left.Key);
                config.MapRightKey(mapping.Right.Key);
                config.ToTable(mapping.TableName);
            });

            MethodInfo MapMethod = manyToManyNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.Name == nameof(ManyToManyNavigationPropertyConfiguration<object, object>.Map));

            MapMethod.Invoke(manyToManyNavigationPropertyConfiguration, new[] { configurationAction });
        }
    }
}