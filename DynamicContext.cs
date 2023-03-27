using Penguin.Cms.Entities;
using Penguin.Debugging;
using Penguin.DependencyInjection.Abstractions.Attributes;
using Penguin.DependencyInjection.Abstractions.Enums;
using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Attributes;
using Penguin.Persistence.Abstractions.Attributes.Relations;
using Penguin.Persistence.EntityFramework.ModelBuilder;
using Penguin.Reflection;
using Penguin.Reflection.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Loxifi;

namespace Penguin.Persistence.EntityFramework
{
    /// <summary>
    /// A context that populates its own sets at runtime. Currently creates a DbSet for any object inheriting from Penguin.Entities.Entity
    /// Uses Penguin.Persistence.Abstractions as attributes to define relationships between entities
    /// </summary>
    [RegisterThroughMostDerived(typeof(DbContext), ServiceLifetime.Transient)]
    public partial class DynamicContext : DbContext
    {
        /// <summary>
        /// The DB Connection info that was used while creating this object
        /// </summary>
        public PersistenceConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// A unique ID generated at class initialization to track this instance, and help with logging/debugging. Useful when DI gets involved
        /// </summary>
        public Guid ContextId { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Returns a list of all types that will be added as DbSet to the context
        /// </summary>
        public HashSet<Type> DbSetTypes
        {
            get
            {
                lock (SetTypeLock)
                {
                    if (_DbSetTypes is null)
                    {
                        _DbSetTypes = new HashSet<Type>();

                        foreach (Type t in GetDynamicContextTypes())
                        {
                            _ = _DbSetTypes.Add(t);
                        }
                    }

                    return _DbSetTypes;
                }
            }
        }

        private static DbConnection GetDbConnection(PersistenceConnectionInfo connectionInfo) =>
#if NET48
            connectionInfo?.ProviderType == ProviderType.SQLCE ? new System.Data.SqlServerCe.SqlCeConnection(connectionInfo.ConnectionString) : new SqlConnection(connectionInfo.ConnectionString) as DbConnection;
#else
            new SqlConnection(connectionInfo.ConnectionString);

#endif

        /// <summary>
        /// Creates a new instance of this dynamic context using the provided connection info
        /// </summary>
        /// <param name="connectionInfo">The connection info for the database</param>
        public DynamicContext(PersistenceConnectionInfo connectionInfo) : base(GetDbConnection(connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo))), true)
        {
            ConnectionInfo = connectionInfo;

            SetUp();
        }

        /// <summary>
        /// Returns a list of all types that will be added as DbSet to the context
        /// </summary>
        /// <returns>A list of all types that will be added as DbSet to the context</returns>
        public static IEnumerable<Type> GetDynamicContextTypes()
        {
            foreach (Type t in TypeFactory.Default.GetDerivedTypes(typeof(Entity)))
            {
                if (!t.IsAbstract && t.GetCustomAttribute<NotMappedAttribute>() is null)
                {
                    yield return t;
                }
            }
        }

        /// <summary>
        /// Retrieves the state of a given object on the context
        /// </summary>
        /// <param name="entity">The object to check the state of</param>
        /// <returns>The state of the object</returns>
        public EntityState GetState(object entity)
        {
            return entity == null
                ? throw new ArgumentNullException(nameof(entity))
                : ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out ObjectStateEntry entry)
                ? entry.State
                : EntityState.Detached;
        }

        /// <summary>
        /// Attempts to find the DB table name for a given type
        /// </summary>
        /// <param name="t">The type to check for</param>
        /// <returns>The DB table name</returns>
        public string GetTableName(Type t)
        {
            ObjectContext objectContext = ((IObjectContextAdapter)this).ObjectContext;

            MethodInfo CreateObjectSetMethod = typeof(ObjectContext).GetMethods().Single(m => m.Name == nameof(ObjectContext.CreateObjectSet) && !m.GetParameters().Any());

            CreateObjectSetMethod = CreateObjectSetMethod.MakeGenericMethod(t);

            object objectSet = CreateObjectSetMethod.Invoke(objectContext, Array.Empty<object>());

            MethodInfo TraceStringMethod = objectSet.GetType().GetMethods().Single(m => m.Name == nameof(ObjectSet<object>.ToTraceString));

            string sql = TraceStringMethod.Invoke(objectSet, Array.Empty<object>()) as string;

            Regex regex = new("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);

            string table = match.Groups["table"].Value;
            return table;
        }

        /// <summary>
        /// Checks if the object is attached to the context
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>Whether or not the entity is attached</returns>
        public bool IsAttached(object entity)
        {
            return GetState(entity) != EntityState.Detached;
        }

        /// <summary>
        /// Attempts to recursively detach the object. Not reliable on .Net Core
        /// </summary>
        /// <param name="e">The entity to detach</param>
        /// <param name="mode">The mode specifying the requirements for detachment</param>
        /// <param name="Cascade">If true, will detach recursively to children</param>
        /// <param name="Detatched">A list of objects that have already been detached (for recursion). Leave empty</param>
        public void TryDetach(KeyedObject e, DetachModes mode = DetachModes.All, bool Cascade = false, List<KeyedObject> Detatched = null)
        {
            Detatched ??= new List<KeyedObject>();

            if (e is null)
            {
                return;
            }

            if (Detatched.Contains(e))
            {
                return;
            }

            Detatched.Add(e);

            if (Cascade)
            {
                TryDetachChildren(e, mode, Cascade, Detatched);
            }

            //Detaching removes children so it must happen after recursion
            if (IsAttached(e))
            {
                bool PassesMode = true;

                if (mode != DetachModes.All)
                {
                    if (mode.HasFlag(DetachModes.Added))
                    {
                        PassesMode = PassesMode && (GetState(e) == EntityState.Added);
                    }

                    if (mode.HasFlag(DetachModes.Modified))
                    {
                        PassesMode = PassesMode && (GetState(e) == EntityState.Modified);
                    }

                    if (mode.HasFlag(DetachModes.ZeroId))
                    {
                        PassesMode = PassesMode && e._Id == 0;
                    }

                    if (mode.HasFlag(DetachModes.NonZeroId))
                    {
                        PassesMode = PassesMode && e._Id != 0;
                    }
                }

                if (PassesMode)
                {
                    ((IObjectContextAdapter)this).ObjectContext.Detach(e);
                }
            }
        }

        /// <summary>
        /// Attempts to detach only the children of the object given
        /// </summary>
        /// <param name="e">The entity to detach</param>
        /// <param name="mode">The mode specifying the requirements for detachment</param>
        /// <param name="Cascade">If true, will detach recursively to children</param>
        /// <param name="Detatched">A list of objects that have already been detached (for recursion). Leave empty</param>
        public void TryDetachChildren(KeyedObject e, DetachModes mode = DetachModes.All, bool Cascade = false, List<KeyedObject> Detatched = null)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            Detatched ??= new List<KeyedObject>();

            foreach (PropertyInfo p in e.GetType().GetProperties())
            {
                object o = p.GetValue(e);

                if (o is null)
                {
                    continue;
                }

                if (o is KeyedObject)
                {
                    TryDetach(o as KeyedObject, mode, Cascade, Detatched);
                }
                else if (o as IEnumerable is not null)
                {
                    IEnumerable list = o as IEnumerable;
                    foreach (object lo in list.Cast<object>().ToList())
                    {
                        TryDetach(lo as KeyedObject, mode, Cascade, Detatched);
                    }
                }
            }
        }

        internal void MapType<T>(DbModelBuilder modelBuilder, PropertyInfo[] properties) where T : class
        {
            Type t = typeof(T);

            bool isComplexType = false;

            foreach (PersistenceAttribute a in t.GetCustomAttributes<PersistenceAttribute>())
            {
                if (a is ComplexTypeAttribute)
                {
                    isComplexType = true;
                }

                List<Type> matchingTypes = TypeFactory.Default.GetDerivedTypes(typeof(TypeBuilder<>).MakeGenericType(a.GetType())).ToList();

                if (matchingTypes.Count == 0)
                {
                    //MessageBus?.Log($"No TypeBuilder<> found for Persistence attribute {a.GetType()}");
                }

                foreach (Type builderType in matchingTypes)
                {
                    object builder = Activator.CreateInstance(builderType, new object[] { t, ConnectionInfo });

                    MethodInfo buildMethod = builderType.GetMethod(nameof(PropertyBuilder<PersistenceAttribute>.Build));

                    buildMethod = buildMethod.MakeGenericMethod(t);

                    _ = buildMethod.Invoke(builder, new object[] { modelBuilder });
                }
            }

            //Register any entities that might not already be added to the context
            if (!isComplexType && typeof(Entity).IsAssignableFrom(t))
            {
                _ = modelBuilder.Entity<T>();
            }

            foreach (PropertyInfo p in properties)
            {
                foreach (PersistenceAttribute a in p.GetCustomAttributes<PersistenceAttribute>())
                {
                    MapProperty(modelBuilder, t, p, a);
                }
            }

            foreach (PropertyInfo p in t.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (p.PropertyType.IsInterface)
                {
                    continue;
                }

                List<PersistenceAttribute> DefinedAttributes = p.GetCustomAttributes<PersistenceAttribute>().ToList();

                if (p.GetCustomAttribute<MappedAttribute>() is null && !DefinedAttributes.Any())
                {
                    MapProperty(modelBuilder, t, p, new NotMappedAttribute());
                }
                else
                {
                    foreach (PersistenceAttribute a in DefinedAttributes)
                    {
                        MapProperty(modelBuilder, t, p, a);
                    }
                }
            }
        }

        internal void SetUp()
        {
            if (ConnectionInfo.ProviderType != ProviderType.SQLCE)
            {
                try
                {
                    int CommandTimeout = 300;

                    Database.CommandTimeout = CommandTimeout;

                    IObjectContextAdapter adapter = this;
                    //ToDo: Dont use ICollection inheriting properties
                    ObjectContext objectContext = adapter.ObjectContext;

                    objectContext.CommandTimeout = CommandTimeout; // value in seconds
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    //Generally do not approve of Try-Catch but this is so absolutely not important to functionality
                    //That it makes more sense just to swallow the exception to prevent a small issue from completely
                    //shutting down the application
                }
            }
        }

        /// <summary>
        /// Calls the code that dynamically attaches and maps objects
        /// </summary>
        /// <param name="modelBuilder">The provided modelbuilder</param>
        protected override void OnModelCreating(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            List<Type> allTypes = DbSetTypes.ToList();

            int i = 0;

            while (i < allTypes.Count)
            {
                Type toCheck = allTypes.ElementAt(i);

                foreach (PropertyInfo p in toCheck.GetProperties())
                {
                    Type propertyType = p.PropertyType;

                    bool isSupportedCollection = false;

                    Type gType;
                    isSupportedCollection = (gType = p.PropertyType.GetGenericArguments().FirstOrDefault()) != null
                        ? typeof(ICollection<>).MakeGenericType(gType).IsAssignableFrom(p.PropertyType)
                        : typeof(ICollection).IsAssignableFrom(p.PropertyType);

                    if (propertyType.IsCollection())
                    {
                        propertyType = propertyType.IsArray ? propertyType.GetElementType() : gType;

                        if (propertyType is null)
                        {
                            Contract.Assert(propertyType != null);
                        }
                    }

                    if (!allTypes.Contains(propertyType) && propertyType.IsClass && propertyType != typeof(string))
                    {
                        allTypes.Add(propertyType);
                    }
                }

                i++;
            }

            foreach (Type t in allTypes)
            {
                MethodInfo mapType = typeof(DynamicContext).GetMethod(nameof(DynamicContext.MapType), BindingFlags.Instance | BindingFlags.NonPublic);

                mapType = mapType.MakeGenericMethod(t);

                PropertyInfo[] properties = t.GetProperties().Where(p => p.DeclaringType == t || !allTypes.Contains(p.DeclaringType)).ToArray();

                if (StaticLogger.IsListening)
                {
                    StaticLogger.Log($"DC: Found properties on type {t}", StaticLogger.LoggingLevel.Call);
                    foreach (PropertyInfo pi in properties)
                    {
                        StaticLogger.Log($"DC: Found property {pi.Name}", StaticLogger.LoggingLevel.Call);
                    }
                }

                _ = mapType.Invoke(this, new object[] { modelBuilder, properties });
            }

            base.OnModelCreating(modelBuilder);
        }

        private void MapProperty(DbModelBuilder modelBuilder, Type t, PropertyInfo p, PersistenceAttribute a)
        {
            List<Type> matchingTypes = TypeFactory.Default.GetDerivedTypes(typeof(PropertyBuilder<>).MakeGenericType(a.GetType())).ToList();

            foreach (Type builderType in matchingTypes)
            {
                object builder = Activator.CreateInstance(builderType, new object[] { p, ConnectionInfo });

                MethodInfo buildMethod = builderType.GetMethod(nameof(PropertyBuilder<PersistenceAttribute>.Build));

                buildMethod = buildMethod.MakeGenericMethod(t);

                _ = buildMethod.Invoke(builder, new object[] { modelBuilder });
            }
        }

        private static HashSet<Type> _DbSetTypes { get; set; }

        private object SetTypeLock { get; set; } = new object();
    }
}