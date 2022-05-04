using System;

namespace Penguin.Persistence.EntityFramework
{
    public partial class DynamicContext
    {
        /// <summary>
        /// When calling to detach an object this enum specifies the requirement for the object to be detached.
        /// Not reliable
        /// </summary>
        [Flags]
        public enum DetachModes
        {
            /// <summary>
            /// Detaches all objects
            /// </summary>
            All = 0,

            /// <summary>
            /// Detaches only objects in the "added" state
            /// </summary>
            Added = 1,

            /// <summary>
            /// Detaches only objects in the "Modified" state
            /// </summary>
            Modified = 2,

            /// <summary>
            /// Detaches only objects with a non-zero ID field
            /// </summary>
            NonZeroId = 4,

            /// <summary>
            /// detaches only objects with a zero ID field
            /// </summary>
            ZeroId = 8
        }
    }
}
