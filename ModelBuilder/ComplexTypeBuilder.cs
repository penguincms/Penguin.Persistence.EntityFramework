using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System;
using System.Data.Entity;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class ComplexTypeBuilder : TypeBuilder<ComplexTypeAttribute>
    {
        #region Constructors

        public ComplexTypeBuilder(Type m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            modelBuilder.ComplexType<T>();
        }

        #endregion Methods
    }
}