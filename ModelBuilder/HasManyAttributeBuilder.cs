using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class HasManyAttributeBuilder : PropertyBuilder<HasManyAttribute>
    {
        #region Constructors

        public HasManyAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            this.MapMany<T>(modelBuilder, Member);
        }

        #endregion Methods
    }
}