using System;

namespace BridgeChat.Core
{
    public struct Binding
    {
        public uint GroupId { get; set; }
        public string Module { get; set; }
        public string BindParams { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Binding) {
                var other = (Binding)obj;
                return (other.GroupId == GroupId) && (other.Module == Module);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Module.GetHashCode() * 3 + unchecked((int)GroupId);
        }
    }
}

