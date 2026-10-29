using System;using System.IO;using System.Linq;
namespace Game.Runtime {
// Finite native artifact pairs only. No caller registration or self-qualified digest.
internal sealed class SourceNativeMetadataEpoch {
 internal readonly string MetadataSha,LogicalSha,CatalogId;
 SourceNativeMetadataEpoch(string catalog,string metadata,string logical){CatalogId=catalog;MetadataSha=metadata;LogicalSha=logical;}
 static readonly SourceNativeMetadataEpoch[] Rows={
  new SourceNativeMetadataEpoch("S026","49824979d1dca97256b42c1f59e9f3cd8fb8311a003cbd36b91c115a4feeab9a","50b57626d7920079d6ddba431dab3bf923fe4d844ea1990b86346f7c7b8eb5bd"),
  new SourceNativeMetadataEpoch("S002","d2c9714d8f8a49cab62de59fe57dceff9cf6ab24b7d176608850a2bfcfd20383",null),
  new SourceNativeMetadataEpoch("S002","d9b2dbfc12a8cfd12e385a3b3fa9266fd96a3d05939bc244992fafc6e21e1e54",null),
  new SourceNativeMetadataEpoch("S026","06ff2cf07f069988a04f9e1740cbc9d29461207014b566fe3f2845c205e7be17",null),
  new SourceNativeMetadataEpoch("S026","6a55787a770c82623030907518e87343fad72b5c35d91ae293a5199128ba986a","a6f7e1040b97c5daf76f86a9a81fcb48101863698c5f1593a2f6b00d1e70bf18")
,
  new SourceNativeMetadataEpoch("S002","8aab4377935fa780fa7d7b54032afde502fefe2c32c6ee488704ecb8dea45136",null),
  new SourceNativeMetadataEpoch("S026","da1383237b7ec9bb0607596be496edc40d6a1cd9c15c12901d7dbee9731b5f37","d0604a13ee34f00c19beb4b03be02850d5aa765592e4e33c0be3688dacd60443")
 };
 internal static SourceNativeMetadataEpoch Require(string sha,string catalog,bool logical){return Rows.SingleOrDefault(r=>r.MetadataSha==sha&&r.CatalogId==catalog&&(r.LogicalSha!=null)==logical)??throw new InvalidDataException("Unknown native artifact/catalog/logical epoch");}
 internal static string RequireLogicalSha(string sha){if(sha==null||!Rows.Any(r=>r.LogicalSha==sha))throw new InvalidDataException("Unknown native logical authoring artifact");return sha;}
}
}
