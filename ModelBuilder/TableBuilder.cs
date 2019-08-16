using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class TableBuilder : TypeBuilder<TableAttribute>
    {
        #region Constructors

        public TableBuilder(Type m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            EntityTypeConfiguration<T> entityTypeConfiguration = modelBuilder.Entity<T>();

            modelBuilder.Entity<T>().Map((ec) =>
            {
                if (Attribute.MapInherited)
                {
                    //ec.MapInheritedProperties();
                }

                ec.ToTable(Attribute.Name);
            });
        }

        #endregion Methods
    }
}