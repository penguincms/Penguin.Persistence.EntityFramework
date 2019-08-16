using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Control;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class KeyBuilder : PropertyBuilder<KeyAttribute>
    {
        #region Constructors

        public KeyBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            this.PropertyMethod<T>(modelBuilder, nameof(EntityTypeConfiguration<T>.HasKey));
        }

        #endregion Methods
    }
}