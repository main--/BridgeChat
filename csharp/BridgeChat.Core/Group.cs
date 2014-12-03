using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BridgeChat.Core;

namespace BridgeChat.Core
{
    public class Group
    {
        private readonly IList<User> Users = new CopyOnWriteArrayList<User>();
        private readonly IList<Module> BoundModules = new CopyOnWriteArrayList<Module>();
        public string Topic { get; private set; }
        public uint Id { get; private set; }
        private readonly Action<Group> Update, Remove;
        private readonly Action<Binding> Bind, Unbind;

        public Group(Action<Group> update, Action<Group> remove, Action<Binding> bind, Action<Binding> unbind, uint id)
        {
            Update = update;
            Remove = remove;
            Bind = bind;
            Unbind = unbind;
            Id = id;
            Topic = String.Empty;
        }

        public void Destroy()
        {
            // remove all bindings
            foreach (var binding in BoundModules)
                RemoveBinding(binding);

            Remove(this);
        }

        public void SetTopic(string topic, Module sender)
        {
            lock (this) {
                foreach (var module in BoundModules)
                    if (module != sender)
                        module.SetTopic(Id, topic);
                Topic = topic; // ZWO EINS RISIKO ϰ
            }

            Update(this);
        }

        public void SendMessage(Module module, string user, Protocol.ChatMessage message)
        {
            foreach (var mod in BoundModules)
                if (mod != module)
                    mod.SendChatMessage(Id, module, user, message);
        }

        public void AddUser(Module module, string name)
        {
            var newusr = new User { Module = module, Name = name };
            // be graceful and allow them to add users they already added
            if (!Users.Contains(newusr)) {
                Users.Add(newusr);
                foreach (var mod in BoundModules)
                    if (mod != module)
                        mod.AddUser(Id, module, name);
            }
        }

        public void RemoveUser(Module module, string name)
        {
            // be graceful and allow them to remove users they never added
            if (Users.Remove(new User { Module = module, Name = name }))
                foreach (var mod in BoundModules)
                    if (mod != module)
                        mod.RemoveUser(Id, module, name);
        }

        public void RequestBinding(Module mod, string parameter)
        {
            Bind(new Binding { GroupId = Id, Module = mod.ShortName, BindParams = parameter });
            mod.BindGroup(Id, parameter);
        }

        public void AddBinding(Module mod)
        {
            BoundModules.Add(mod);
            mod.SetTopic(Id, Topic); // sync topic

            // sync users
            foreach (var user in Users)
                mod.AddUser(Id, user.Module, user.Name);
        }


        public void RemoveBinding(Module mod)
        {
            Unbind(new Binding { GroupId = Id, Module = mod.ShortName });
            UnloadBinding(mod);
        }

        public void UnloadBinding(Module mod)
        {
            mod.UnbindGroup(Id);
            BoundModules.Remove(mod);

            // clean up their users
            foreach (var user in Users)
                if (user.Module == mod)
                    RemoveUser(mod, user.Name);
        }

        public override bool Equals(object obj)
        {
            if (obj is Group) {
                var other = (Group)obj;
                return (other.Id == Id) && (other.Topic == Topic);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return unchecked((int)Id);
        }
    }
}

