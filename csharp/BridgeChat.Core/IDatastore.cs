using System;
using System.Collections.Generic;

namespace BridgeChat.Core
{
    public interface IDatastore
    {
        Action<Group> RemovalHandler { set; }
        IEnumerable<Group> SavedGroups { get; }
        Group CreateGroup();

        IEnumerable<Binding> SavedBindings { get; }
    }
}

