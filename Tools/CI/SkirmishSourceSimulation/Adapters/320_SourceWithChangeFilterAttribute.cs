using System;namespace Unity.Entities{
    public sealed class WithChangeFilterAttribute : Attribute
    {
        /// <summary>
        /// Specifies that this IJobEntity should only process a chunk if any of the ComponentTypes found as attributes of the IJobEntity have changed.
        /// </summary>
        /// <param name="types">The component types for which change filtering should be enabled</param>
        public WithChangeFilterAttribute(params Type[] types){}
    }
}
