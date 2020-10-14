using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Penguin.Persistence.EntityFramework.ModelBuilder
{
    internal abstract class PropertyBuilder<TAttribute> : AttributeBuilder<TAttribute, PropertyInfo> where TAttribute : PersistenceAttribute
    {
        public PropertyBuilder(PropertyInfo m, PersistenceConnectionInfo persistenceConnectionInfo) : base(m, persistenceConnectionInfo)
        {
        }

        public static PropertyInfo GetBaseProperty(PropertyInfo derived)
        {
            PropertyInfo declaredProperty = derived.DeclaringType.GetProperty(derived.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Type BaseDeclaringType = declaredProperty.GetGetMethod()?.GetBaseDefinition()?.DeclaringType;

            while (BaseDeclaringType != null && BaseDeclaringType != declaredProperty.ReflectedType)
            {
                declaredProperty = BaseDeclaringType.GetProperty(declaredProperty.Name);

                BaseDeclaringType = declaredProperty.GetGetMethod()?.GetBaseDefinition()?.DeclaringType;
            }

            return declaredProperty;
        }

        public object MapMany<TModel>(System.Data.Entity.DbModelBuilder modelBuilder, PropertyInfo p, Type propertyOverrideType = null) where TModel : class
        {
            Type t = typeof(TModel);

            EntityTypeConfiguration<TModel> entityTypeConfiguration = modelBuilder.Entity<TModel>();

            Type collectionType = (propertyOverrideType ?? p.PropertyType).GetGenericArguments()[0];

            MethodInfo HasManyMethod = entityTypeConfiguration.GetType().GetMethod(nameof(EntityTypeConfiguration<object>.HasMany)).MakeGenericMethod(collectionType);

            return HasManyMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(t, p.Name, typeof(ICollection<>).MakeGenericType(collectionType)) });
        }

        public object Property<TModel>(DbModelBuilder modelBuilder) where TModel : class
        {
            EntityTypeConfiguration<TModel> entityTypeConfiguration = modelBuilder.Entity<TModel>();

            List<MethodInfo> propertyMethods = entityTypeConfiguration.GetType().GetMethods().Where(m => m.Name == nameof(StructuralTypeConfiguration<TModel>.Property)).ToList();
            MethodInfo propertyMethod = null;

            //Grab any methods that accept this property type OR are generic
            propertyMethods = propertyMethods.Where(p =>
            {
                Type t = p.GetParameters()[0].ParameterType.GetGenericArguments()[0].GetGenericArguments()[1];
                return t.IsGenericParameter || t.IsAssignableFrom(this.Member.PropertyType);
            }).ToList();

            //Assuming we get a generic and a nongeneric, we choose the nongeneric
            if (propertyMethods.Count > 1)
            {
                propertyMethods = propertyMethods.Where(p =>
                {
                    Type t = p.GetParameters()[0].ParameterType.GetGenericArguments()[0].GetGenericArguments()[1];
                    return t.IsAssignableFrom(this.Member.PropertyType);
                }).ToList();

                propertyMethod = propertyMethods.Single();
            }
            else
            {
                //Otherwise we assume we only have the generic so we use that
                propertyMethod = propertyMethods.Single().MakeGenericMethod(this.Member.PropertyType);
            }

            return propertyMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(this.Member.ReflectedType, this.Member.Name) });
        }

        public object PropertyMethod<TModel>(DbModelBuilder modelBuilder, string Name) where TModel : class
        {
            EntityTypeConfiguration<TModel> entityTypeConfiguration = modelBuilder.Entity<TModel>();

            List<MethodInfo> propertyMethods = entityTypeConfiguration.GetType().GetMethods().Where(m => m.GetParameters().Length == 1 && m.Name == Name && m.ContainsGenericParameters).ToList();

            MethodInfo propertyMethod = propertyMethods.Single().MakeGenericMethod(this.Member.PropertyType);

            return propertyMethod.Invoke(entityTypeConfiguration, new[] { PropertyExpression(this.Member.ReflectedType, this.Member.Name) });
        }

        protected static LambdaExpression PropertyExpression(Type sourceType, string propertyName, Type returnType = null)
        {
            ParameterExpression param = Expression.Parameter(sourceType);

            MemberExpression prop = Expression.Property(param, propertyName);

            LambdaExpression exp = Expression.Lambda(prop, param);

            if (returnType != null && exp.ReturnType != returnType)
            {
                object[] parameters = new object[] { exp.Body, exp.Parameters };

                MethodInfo LambdaExpressionCreate = typeof(Expression).GetMethods().Where(m => m.Name == nameof(Expression.Lambda))
                                                                                   .Where(m => m.GetGenericArguments().Length == 1)
                                                                                   .Where(m => m.GetParameters().Length == parameters.Length)
                                                                                   .Where(m =>
                                                                                   {
                                                                                       ParameterInfo[] mparams = m.GetParameters();
                                                                                       for (int i = 0; i < parameters.Length; i++)
                                                                                       {
                                                                                           if (!mparams[i].ParameterType.IsAssignableFrom(parameters[i].GetType()))
                                                                                           {
                                                                                               return false;
                                                                                           }
                                                                                       }
                                                                                       return true;
                                                                                   })
                                                                                   .Single();
                Type lambdaFuncType = typeof(Func<,>).MakeGenericType(sourceType, returnType);

                LambdaExpressionCreate = LambdaExpressionCreate.MakeGenericMethod(lambdaFuncType);

                exp = LambdaExpressionCreate.Invoke(null, parameters) as LambdaExpression;
            }

            return exp;
        }

        protected object WithMany(object manyNavigationPropertyConfiguration, PropertyInfo p, Mapping mapping)
        {
            Type sourceType = p.PropertyType;

            if (typeof(ICollection).IsAssignableFrom(sourceType))
            {
                sourceType = sourceType.GetGenericArguments()[0];
            }

            Type returnType = typeof(ICollection<>).MakeGenericType(p.ReflectedType);

            MethodInfo WithManyMethod = manyNavigationPropertyConfiguration.GetType().GetMethods().Single(m => m.Name == nameof(ManyNavigationPropertyConfiguration<object, object>.WithMany) && m.GetParameters().Any());

            return WithManyMethod.Invoke(manyNavigationPropertyConfiguration, new[] { PropertyExpression(sourceType, mapping.Right.Property, returnType) });
        }
    }
}