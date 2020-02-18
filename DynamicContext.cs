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

namespace Penguin.Persistence.EntityFramework
{
    /// <summary>
    /// A context that populates its own sets at runtime. Currently creates a DbSet for any object inheriting from Penguin.Entities.Entity
    /// Uses Penguin.Persistence.Abstractions as attributes to define relationships between entities
    /// </summary>
    [RegisterThroughMostDerived(typeof(DbContext), ServiceLifetime.Transient)]
    public class DynamicContext : DbContext
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
                            _DbSetTypes.Add(t);
                        }
                    }

                    return _DbSetTypes;
                }
            }
        }

        /// <summary>
        /// When calling to detatch an object this enum specifies the requirement for the object to be detatched.
        /// Not reliable
        /// </summary>
        [Flags]
        public enum DetatchModes
        {
            /// <summary>
            /// Detatches all objects
            /// </summary>
            All = 0,

            /// <summary>
            /// Detatches only objects in the "added" state
            /// </summary>
            Added = 1,

            /// <summary>
            /// Detatches only objects in the "Modified" state
            /// </summary>
            Modified = 2,

            /// <summary>
            /// Detatches only objects with a non-zero ID field
            /// </summary>
            NonZeroId = 4,

            /// <summary>
            /// detatches only objects with a zero ID field
            /// </summary>
            ZeroId = 8
        }

        private static DbConnection GetDbConnection(PersistenceConnectionInfo connectionInfo)
        {
#if NET48
            return connectionInfo?.ProviderType == ProviderType.SQLCE ? new System.Data.SqlServerCe.SqlCeConnection(connectionInfo.ConnectionString) as DbConnection : new SqlConnection(connectionInfo.ConnectionString) as DbConnection;
#else
            return new SqlConnection(connectionInfo.ConnectionString) as DbConnection;
#endif
        }

        /// <summary>
        /// Creates a new instance of this dynamic context using the provided connection info
        /// </summary>
        /// <param name="connectionInfo">The connection info for the database</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public DynamicContext(PersistenceConnectionInfo connectionInfo) : base(GetDbConnection(connectionInfo), true)
        {
            this.ConnectionInfo = connectionInfo;

            this.SetUp();
        }

        /// <summary>
        /// Returns a list of all types that will be added as DbSet to the context
        /// </summary>
        /// <returns>A list of all types that will be added as DbSet to the context</returns>
        public static IEnumerable<Type> GetDynamicContextTypes()
        {
            foreach (Type t in TypeFactory.GetDerivedTypes(typeof(Entity)))
            {
                if (!t.IsAbstract)
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
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out ObjectStateEntry entry))
            {
                return entry.State;
            }

            return EntityState.Detached;
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

            Regex regex = new Regex("FROM (?<table>.*) AS");
            Match match = regex.Match(sql);

            string table = match.Groups["table"].Value;
            return table;
        }

        /// <summary>
        /// Checks if the object is attached to the context
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>Whether or not the entity is attached</returns>
        public bool IsAttached(object entity) => this.GetState(entity) != EntityState.Detached;

        /// <summary>
        /// Attempts to recursively detatch the object. Not reliable on .Net Core
        /// </summary>
        /// <param name="e">The entity to detatch</param>
        /// <param name="mode">The mode specifying the requirements for detatchment</param>
        /// <param name="Cascade">If true, will detatch recursively to children</param>
        /// <param name="Detatched">A list of objects that have already been detatched (for recursion). Leave empty</param>
        public void TryDetach(KeyedObject e, DetatchModes mode = DetatchModes.All, bool Cascade = false, List<KeyedObject> Detatched = null)
        {
            if (Detatched is null)
            {
                Detatched = new List<KeyedObject>();
            }

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
                this.TryDetachChildren(e, mode, Cascade, Detatched);
            }

            //Detaching removes children so it must happen after recursion
            if (this.IsAttached(e))
            {
                bool PassesMode = true;

                if (mode != DetatchModes.All)
                {
                    if (mode.HasFlag(DetatchModes.Added))
                    {
                        PassesMode = PassesMode && (this.GetState(e) == EntityState.Added);
                    }

                    if (mode.HasFlag(DetatchModes.Modified))
                    {
                        PassesMode = PassesMode && (this.GetState(e) == EntityState.Modified);
                    }

                    if (mode.HasFlag(DetatchModes.ZeroId))
                    {
                        PassesMode = PassesMode && e._Id == 0;
                    }

                    if (mode.HasFlag(DetatchModes.NonZeroId))
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
        /// Attempts to detatch only the children of the object given
        /// </summary>
        /// <param name="e">The entity to detatch</param>
        /// <param name="mode">The mode specifying the requirements for detatchment</param>
        /// <param name="Cascade">If true, will detatch recursively to children</param>
        /// <param name="Detatched">A list of objects that have already been detatched (for recursion). Leave empty</param>
        public void TryDetachChildren(KeyedObject e, DetatchModes mode = DetatchModes.All, bool Cascade = false, List<KeyedObject> Detatched = null)
        {
            Contract.Requires(e != null);

            if (Detatched is null)
            {
                Detatched = new List<KeyedObject>();
            }

            foreach (PropertyInfo p in e.GetType().GetProperties())
            {
                object o = p.GetValue(e);

                if (o is null)
                {
                    continue;
                }

                if (o is KeyedObject)
                {
                    this.TryDetach(o as KeyedObject, mode, Cascade, Detatched);
                }
                else if (!((o as IEnumerable) is null))
                {
                    IEnumerable list = o as IEnumerable;
                    foreach (object lo in list.Cast<object>().ToList())
                    {
                        this.TryDetach(lo as KeyedObject, mode, Cascade, Detatched);
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

                List<Type> matchingTypes = TypeFactory.GetDerivedTypes(typeof(TypeBuilder<>).MakeGenericType(a.GetType())).ToList();

                if (matchingTypes.Count == 0)
                {
                    //MessageBus?.Log($"No TypeBuilder<> found for Persistence attribute {a.GetType()}");
                }

                foreach (Type builderType in matchingTypes)
                {
                    object builder = Activator.CreateInstance(builderType, new object[] { t, ConnectionInfo });

                    MethodInfo buildMethod = builderType.GetMethod(nameof(PropertyBuilder<PersistenceAttribute>.Build));

                    buildMethod = buildMethod.MakeGenericMethod(t);

                    buildMethod.Invoke(builder, new object[] { modelBuilder });
                }
            }

            //Register any entities that might not already be added to the context
            if (!isComplexType && typeof(Entity).IsAssignableFrom(t))
            {
                modelBuilder.Entity<T>();
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        internal void SetUp()
        {
            if (this.ConnectionInfo.ProviderType != ProviderType.SQLCE)
            {
                try
                {
                    int CommandTimeout = 300;

                    Database.CommandTimeout = CommandTimeout;

                    IObjectContextAdapter adapter = this;
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
                    if ((gType = p.PropertyType.GetGenericArguments().FirstOrDefault()) != null)
                    {
                        isSupportedCollection = typeof(ICollection<>).MakeGenericType(gType).IsAssignableFrom(p.PropertyType);
                    }
                    else
                    {
                        isSupportedCollection = typeof(ICollection).IsAssignableFrom(p.PropertyType);
                    }

                    if (propertyType.IsCollection())
                    {
                        if (propertyType.IsArray)
                        {
                            propertyType = propertyType.GetElementType();
                        }
                        else
                        {
                            propertyType = gType;
                        }

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

                mapType.Invoke(this, new object[] { modelBuilder, properties });
            }

            base.OnModelCreating(modelBuilder);
        }

        private void MapProperty(DbModelBuilder modelBuilder, Type t, PropertyInfo p, PersistenceAttribute a)
        {
            List<Type> matchingTypes = TypeFactory.GetDerivedTypes(typeof(PropertyBuilder<>).MakeGenericType(a.GetType())).ToList();

            foreach (Type builderType in matchingTypes)
            {
                object builder = Activator.CreateInstance(builderType, new object[] { p, ConnectionInfo });

                MethodInfo buildMethod = builderType.GetMethod(nameof(PropertyBuilder<PersistenceAttribute>.Build));

                buildMethod = buildMethod.MakeGenericMethod(t);

                buildMethod.Invoke(builder, new object[] { modelBuilder });
            }
        }

        private static HashSet<Type> _DbSetTypes { get; set; }
        private object SetTypeLock { get; set; } = new object();
    }
}