using Penguin.Reflection;
using System;
using System.Data.Entity.Infrastructure;

namespace Penguin.Persistence.EntityFramework
{
    /// <summary>
    /// Inherit from this class to provide a context factory that creates the context using the proper DB Connection info
    /// </summary>
    public class DynamicContextFactory : IDbContextFactory<DynamicContext>
    {
        private const string NO_BASE_MESSAGE = "Do not call base method on context factory to create context. Simply return an instance of the dynamic context";

        /// <summary>
        /// Override this method. Do not call the base. It will error
        /// </summary>
        /// <returns>The instantiated context</returns>
        public virtual DynamicContext Create()
        {
            if (GetType() == typeof(DynamicContextFactory))
            {
                Type overriddenType = TypeFactory.GetMostDerivedType(GetType());

                if (overriddenType == typeof(DynamicContextFactory))
                {
                    throw new Exception($"No overrides found for type {GetType().FullName}. Can not create context");
                }
                else
                {
                    DynamicContextFactory factory = Activator.CreateInstance(overriddenType) as DynamicContextFactory;

                    return factory.Create();
                }
            }
            else
            {
                throw new Exception(NO_BASE_MESSAGE);
            }
        }
    }
}