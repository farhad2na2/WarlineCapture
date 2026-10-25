using System;namespace Unity.Entities { [AttributeUsage(AttributeTargets.Struct,AllowMultiple=true)]public sealed class WithAllAttribute:Attribute{public WithAllAttribute(params Type[] types){}} }
