using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class NotMappedBuilder : PropertyBuilder<NotMappedAttribute>
    {
        #region Constructors

        public NotMappedBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(GetBaseProperty(m), persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            this.GetType().GetMethod(nameof(NotMappedBuilder.PropertyMethod)).MakeGenericMethod(Member.DeclaringType).Invoke(this, new object[] { modelBuilder, nameof(EntityTypeConfiguration<T>.Ignore) });
        }

        #endregion Methods
    }
}