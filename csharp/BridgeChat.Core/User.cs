using System;

namespace BridgeChat.Core
{
    public struct User
    {
        public Module Module { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is User) {
                var other = (User)obj;
                return (Module == other.Module) && (Name == other.Name);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Module.GetHashCode() * 3 + Name.GetHashCode();
        }
    }
}
