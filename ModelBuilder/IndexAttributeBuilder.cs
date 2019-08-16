using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes.Control;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal class IndexAttributeBuilder : PropertyBuilder<IndexAttribute>
    {
        #region Constructors

        public IndexAttributeBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        #endregion Constructors

        #region Methods

        public override void Build<T>(DbModelBuilder modelBuilder)
        {
            object propertyConfiguration = this.Property<T>(modelBuilder);

            MethodInfo HasColumnAnnotationMethod = propertyConfiguration.GetType().GetMethod(nameof(PrimitivePropertyConfiguration.HasColumnAnnotation));

            HasColumnAnnotationMethod.Invoke(propertyConfiguration, new object[] { "Index", new IndexAnnotation(new System.ComponentModel.DataAnnotations.Schema.IndexAttribute("IX_" + Member.Name) { IsUnique = Attribute.IsUnique }) });
        }

        #endregion Methods
    }
}