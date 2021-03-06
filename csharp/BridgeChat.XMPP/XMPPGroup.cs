﻿using System;
using BridgeChat.PluginBase;
using System.Linq;
using System.Collections.Generic;
using agsXMPP;
using System.Collections.Concurrent;
using agsXMPP.protocol.extensions.nickname;
using agsXMPP.protocol.component;
using agsXMPP.protocol.x.muc;

namespace BridgeChat.XMPP
{
    public class XMPPGroup : Group
    {
        private readonly XMPPModule Module;
        public ConcurrentDictionary<string, Jid> XMPPUsers { get; private set; }
        public string MUCName { get; private set; }
        public string Password { get; private set; }
        public IEnumerable<string> AllMembers {
            get { return RemoteUsers.Select(tup => CoerceName(tup.Item1, tup.Item2)).Concat(MyUsers); }
        }

        public XMPPGroup(XMPPModule mod, uint id, string name, string password)
            : base(mod, id)
        {
            Module = mod;
            MUCName = name;
            Password = password;
            XMPPUsers = new ConcurrentDictionary<string, Jid>();
        }

        public bool TryAddMember(string nick, Jid jid)
        {
            if (MyUsers.Contains(nick) || !XMPPUsers.TryAdd(nick, jid))
                return false;

            MyUsers.Add(nick);
            return true;
        }

        public void RemoveXMPPMember(string nick)
        {
            Jid jid;
            XMPPUsers.TryRemove(nick, out jid);
            MyUsers.Remove(nick);
            RelayUserGone(nick);
        }

        private void RelayUserGone(string nick)
        {
            var synthjid = SynthesizeJid(nick);
            foreach (var member in XMPPUsers.Values) {
                var relay = new Presence();
                relay.Type = agsXMPP.protocol.client.PresenceType.unavailable;
                relay.From = synthjid;
                relay.To = member;
                relay.MucUser = new User {
                    Item = new Item(Affiliation.member, Role.none)
                };
                Module.Connection.Send(relay);
            }
        }

        private Jid SynthesizeJid(string nick)
        {
            return new Jid(MUCName, Module.Domain, nick);
        }

        private void RelayMessage(string nick, string plaintext, string html)
        {
            var synthjid = SynthesizeJid(nick);
            foreach (var pair in XMPPUsers) {
                var msg = new Message(pair.Value, synthjid);
                if (plaintext == null)
                    throw new NotImplementedException();

                msg.Body = plaintext;
                if (html != null)
                    msg.Html = new agsXMPP.protocol.extensions.html.Html { InnerXml = html };
                msg.GenerateId();
                msg.Type = agsXMPP.protocol.client.MessageType.groupchat;
                Module.Connection.Send(msg);
            }
        }

        public void SendMessage(Jid sender, string plaintext, string html) {
            // map Jid to nick
            var nick = XMPPUsers.Where(pair => pair.Value.Equals(sender)).Single().Key;

            RelayMessage(nick, plaintext, html); // send to XMPP clients
            var msg = new Protocol.ChatMessage();
            if (plaintext != null)
                msg.Plaintext = plaintext;
            if (html != null)
                msg.Html = html;
            base.SendMessage(nick, msg); // send out to server
        }

        public override void HandleTopicChange(string topic)
        {
            base.HandleTopicChange(topic);
            RelayNewTopic(topic);
        }

        public void RelayNewTopic(string topic)
        {
            var synthSetter = SynthesizeJid("BridgeChat");
            foreach (var jid in XMPPUsers.Values) {
                var msg = new Message(jid, synthSetter);
                msg.Type = agsXMPP.protocol.client.MessageType.groupchat;
                msg.GenerateId();
                msg.Subject = topic;
                Module.Connection.Send(msg);
            }
        }

        public override void HandleMessage(string module, string username, Protocol.ChatMessage message)
        {
            RelayMessage(CoerceName(module, username), message.PlaintextSpecified ? message.Plaintext : null,
                message.HtmlSpecified ? message.Html : null);
        }

        public void RelayNewUser(Jid jid, string except = null)
        {
            foreach (var pair in XMPPUsers) {
                if (pair.Key == except)
                    continue;
                // this must be the last one
                var presence = new Presence();
                presence.To = pair.Value;
                presence.From = jid;
                presence.GenerateId();
                presence.MucUser = new User {
                    Item = new Item(Affiliation.member, Role.participant)
                };
                Module.Connection.Send(presence);
            }
        }

        public static string CoerceName(string module, string username)
        {
            return String.Format("[{0}] {1}", module, username);
        }

        public override void AddUser(string module, string username)
        {
            base.AddUser(module, username);
            RelayNewUser(SynthesizeJid(CoerceName(module, username)));
        }

        public override void RemoveUser(string module, string username)
        {
            base.RemoveUser(module, username);
            RelayUserGone(CoerceName(module, username));
        }

        public override void Unbind()
        {
            throw new NotImplementedException();
        }
    }
}

