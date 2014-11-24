using System;
using System.Threading.Tasks;
using BridgeChat.Protocol;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace BridgeChat.PluginBase
{
    public abstract class Group
    {
        private readonly Module Module;
        public uint Id { get; private set; }
        public ICollection<string> MyUsers { get; private set; }
        /// <summary>
        /// Contains all remote users as tuples of (module, name)
        /// </summary>
        /// <value>The remote users.</value>
        public IEnumerable<Tuple<string, string>> RemoteUsers { get { return _RemoteUsers; } }
        private readonly ISet<Tuple<string, string>> _RemoteUsers;
        private string _Topic = String.Empty;
        public string Topic {
            get { return _Topic; }
            set {
                Module.QueueMessage(new GroupMessage {
                    GroupId = Id,
                    GroupStatusChange = new GroupStatus { Topic = value }
                });
                _Topic = value;
            }
        }

        public Group(Module module, uint id)
        {
            Module = module;
            Id = id;
            var myUsers = new ObservableCollection<string>();
            myUsers.CollectionChanged += HandleCollectionChanged;
            MyUsers = myUsers;

            _RemoteUsers = new HashSet<Tuple<string, string>>();
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action) {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems != null)
                    foreach (string username in e.NewItems)
                        Module.QueueMessage(new GroupMessage {
                            GroupId = Id,
                            UserEvent = new UserEvent {
                                Username = username,
                                UserStatus = new UserStatus { OnlineStatus = true }
                            }
                        });

                if (e.OldItems != null)
                    foreach (string username in e.OldItems)
                        Module.QueueMessage(new GroupMessage {
                            GroupId = Id,
                            UserEvent = new UserEvent {
                                Username = username,
                                UserStatus = new UserStatus { OnlineStatus = false }
                            }
                        });

                break;
            case NotifyCollectionChangedAction.Move:
                break; // nop
            default:
                throw new NotImplementedException("unsupported modification");
            }
        }

        public void SendMessage(string username, string message)
        {
            Module.QueueMessage(new GroupMessage {
                GroupId = Id,
                UserEvent = new UserEvent { Username = username, ChatMessage = message }
            });
        }

        public virtual void HandleTopicChange(string topic)
        {
            _Topic = topic;
        }

        public abstract void HandleMessage(string module, string username, string message);
        public virtual void AddUser(string module, string username)
        {
            _RemoteUsers.Add(Tuple.Create(module, username));
        }

        public virtual void RemoveUser(string module, string username)
        {
            _RemoteUsers.Remove(Tuple.Create(module, username));
        }

        public abstract void Unbind();
    }
}

