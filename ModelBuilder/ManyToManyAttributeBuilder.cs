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
            Mapping mapping = Attribute.GetMapping(Member);

            //HasMany
            object manyNavigationPropertyConfiguration = MapMany<T>(modelBuilder, Member);

            //With Many
            object manyToManyNavigationPropertyConfiguration = WithMany(manyNavigationPropertyConfiguration, Member, mapping);

            //Map
            Action<ManyToManyAssociationMappingConfiguration> configurationAction = new((config) =>
            {
                _ = config.MapLeftKey(mapping.Left.Key);
                _ = config.MapRightKey(mapping.Right.Key);
                _ = config.ToTable(mapping.TableName);
            });

            MethodInfo MapMethod = manyToManyNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.Name == nameof(ManyToManyNavigationPropertyConfiguration<object, object>.Map));

            _ = MapMethod.Invoke(manyToManyNavigationPropertyConfiguration, new[] { configurationAction });
        }
    }
}