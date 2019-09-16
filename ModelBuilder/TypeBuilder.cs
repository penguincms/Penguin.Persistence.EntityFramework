using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes;
using System;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal abstract class TypeBuilder<T> : AttributeBuilder<T, Type> where T : PersistenceAttribute
    {
        public TypeBuilder(Type m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }
    }
}