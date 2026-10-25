namespace Game.Runtime{}
namespace Unity.Collections{public struct FixedString4096Bytes{string value;public void Append(string text){if(System.Text.Encoding.UTF8.GetByteCount((value??"" )+text)>4093)throw new System.NotSupportedException("Diagnostic fixed string overflow");value+=text;}public override string ToString()=>value??"";}}

namespace Game.Runtime{public struct UnitAirMovementSystem:Unity.Entities.ISystem{}}
