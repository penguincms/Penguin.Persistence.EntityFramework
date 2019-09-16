using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes;
using System.Data.Entity;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal abstract class AttributeBuilder<TAttribute, TMember> where TAttribute : PersistenceAttribute where TMember : MemberInfo
    {
        public AttributeBuilder(TMember m, PersistenceConnectionInfo persistenceConnectionInfo)
        {
            Attribute = m.GetCustomAttribute<TAttribute>();
            PersistenceConnectionInfo = persistenceConnectionInfo;
            Member = m;
        }

        public abstract void Build<TModel>(DbModelBuilder modelBuilder) where TModel : class;

        protected TAttribute Attribute { get; set; }
        protected TMember Member { get; set; }
        protected PersistenceConnectionInfo PersistenceConnectionInfo { get; set; }
    }
}