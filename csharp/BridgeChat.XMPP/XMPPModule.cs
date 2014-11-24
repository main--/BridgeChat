using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

using BridgeChat.PluginBase;

using agsXMPP;
using agsXMPP.protocol.iq.disco;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;

namespace BridgeChat.XMPP
{
    public class XMPPModule : Module
    {
        internal readonly XmppComponentConnection Connection;
        public string Domain { get; private set; }

        private readonly TaskCompletionSource<object> EventResultSource = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> OpenTaskSource = new TaskCompletionSource<object>();
        private readonly ConcurrentDictionary<string, XMPPGroup> Groups = new ConcurrentDictionary<string, XMPPGroup>();

        public XMPPModule(string xmppServer, int xmppPort, string xmppPassword, string xmppDomain, string chatServer, int chatPort)
            : base("XMPP MUC Module", "XMPP", chatServer, chatPort)
        {
            Domain = xmppDomain;

            Connection = new XmppComponentConnection(xmppServer, xmppPort, xmppPassword);

            Connection.OnAuthError += (sender, e) => EventResultSource.TrySetException(new XMPPException(e));
            Connection.OnClose += (sender) => EventResultSource.TrySetResult(null);
            Connection.OnError += (sender, ex) => EventResultSource.TrySetException(ex);
            Connection.OnIq += HandleIq;
            Connection.OnLogin += (sender) => OpenTaskSource.TrySetResult(null);
            Connection.OnMessage += HandleMessage;
            Connection.OnPresence += HandlePresence;
            Connection.OnReadXml += (sender, xml) => Debug.WriteLine(xml, "recv");
            Connection.OnSocketError += (sender, ex) => EventResultSource.TrySetException(ex);
            Connection.OnStreamError += (sender, e) => EventResultSource.TrySetException(new XMPPException(e));
            Connection.OnWriteXml += (sender, xml) => Debug.WriteLine(xml, "send");
            Connection.OnXmppConnectionStateChanged += (sender, state) => Debug.WriteLine(state, "XMPP state changed");
        }

        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(3);

        public override async Task Run()
        {
            Connection.Open();

            var ots = OpenTaskSource.Task;
            var ers = EventResultSource.Task;
            var con = await Task.WhenAny(ots, ers, Task.Delay(ConnectionTimeout));
            if (con == ots) // everything fine
                await Task.WhenAll(base.Run(), ers);
            else if (con == ers)
                await ers; // error, will be thrown here
            else
                throw new TimeoutException();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Connection.Close();
        }

        private void HandleMessage(object sender, agsXMPP.protocol.component.Message msg)
        {
            /*
             * <message xmlns="jabber:component:accept" from="main@ehvag.de/3903262835141677210334701"
             * type="groupchat" id="purpleadc1516c" to="waedfed@test.ehvag.de">
             * <body>chatmsg</body><
             * /message>
             */

            // TODO: implement negative answers to voice requests
            try {
                if (msg.To.Server != Domain)
                    Trace.WriteLine(msg.To, "wtf what server is this");
                else {
                    XMPPGroup group;
                    if (Groups.TryGetValue(msg.To.User, out group)) {
                        if (msg.Type == MessageType.groupchat) {
                            if (msg.HasTag("body"))
                                // simple chat message
                                group.SendMessage(msg.From, msg.Body);
                            else {
                                // subject change
                                group.Topic = msg.Subject;
                                group.RelayNewTopic(msg.Subject);
                            }
                        } else throw new NotImplementedException("maybe implement other message types");
                    } else throw new NotImplementedException("should send delivery failure notification");
                }
            } catch (Exception ex) {
                EventResultSource.TrySetException(ex);
            }
        }

        private void HandlePresence(object sender, agsXMPP.protocol.component.Presence pres)
        {
            /*
             * Got XML: <presence xmlns="jabber:component:accept"
             * from="main@ehvag.de/34723373821416749452159006"
             * to="user@test.ehvag.de/myname">
             * <priority>1</priority>
             * <c xmlns="http://jabber.org/protocol/caps" hash="sha-1" 
             * node="http://pidgin.im/" ext="voice-v1 camera-v1 video-v1"
             * ver="AcN1/PEN8nq7AHD+9jpxMV4U6YM=" />
             * <x xmlns="http://jabber.org/protocol/muc" />
             * </presence>
             */

            try {
                // TODO: handle nick/presence changes

                var target = pres.To;
                if (!pres.HasAttribute("type")) {
                    if (target.Server == Domain) {
                        var roomname = target.User;
                        var theirnick = target.Resource;
                        string password = null;
                        var mucElement = pres.SelectSingleElement("x");
                        if (mucElement != null)
                            password = mucElement.GetTag("password");
                        Debug.WriteLine("'{0}' would like to join into '{1}' as '{2}'", pres.From, roomname, theirnick);

                        var reply = new Presence();
                        reply.GenerateId();
                        reply.To = pres.From;

                        XMPPGroup group;
                        if (Groups.TryGetValue(roomname, out group)) {
                            reply.From = new Jid(roomname, Domain, theirnick);
                            if (group.Password == password) {//((group.Password == null) || (pres.MucUser.HasTag("password") && (pres.MucUser.Password == group.Password))) {
                                if (group.TryAddMember(theirnick, pres.From)) {
                                    // success
                                    // send presence of existing members
                                    foreach (var name in group.AllMembers) {
                                        var presence = new Presence();
                                        presence.To = pres.From;
                                        presence.From = new Jid(group.MUCName, Domain, name);
                                        presence.GenerateId();
                                        presence.MucUser = new User { Item = new Item(Affiliation.member, Role.participant) };
                                        Connection.Send(presence);
                                    }

                                    var synthjid = new Jid(group.MUCName, Domain, theirnick);
                                    // send presence to existing members
                                    group.RelayNewUser(synthjid, theirnick);

                                    reply.From = synthjid;
                                    reply.MucUser = new User {
                                        Status = new Status(110),
                                        Item = new Item(Affiliation.member, Role.participant)
                                    };
                                    Connection.Send(reply);

                                    // TODO: implement history

                                    var seply = new Message(pres.From, new Jid(roomname, Domain, "BridgeChat"));
                                    seply.GenerateId();
                                    seply.Type = MessageType.groupchat;
                                    seply.Subject = group.Topic;
                                    Connection.Send(seply);
                                    return;
                                } else {
                                    reply.Error = new Error(ErrorType.cancel, ErrorCondition.Conflict);
                                    reply.Error.SetAttribute("by", new Jid(roomname, Domain, null));
                                }
                            } else {
                                reply.Error = new Error(ErrorType.auth, ErrorCondition.NotAuthorized);
                            }
                        } else {
                            // you can't create groups
                            reply.From = new Jid(roomname, Domain, null);
                            reply.Error = new Error(ErrorType.cancel, ErrorCondition.NotAllowed);
                        }

                        reply.Type = PresenceType.error;
                        reply.MucUser = new User();
                        reply.Id = pres.Id;

                        Connection.Send(reply);
                    } else
                        Trace.WriteLine(target, "wtf what host is this");
                } else {
                    switch (pres.Type) {
                    case PresenceType.unavailable:
                        var roomname = target.User;
                        var theirnick = target.Resource;
                        Debug.WriteLine("'{0}' would like to leave '{1}' as '{2}'", pres.From, roomname, theirnick);

                        XMPPGroup group;
                        if (!Groups.TryGetValue(roomname, out group))
                            throw new InvalidOperationException();

                        group.RemoveXMPPMember(theirnick);

                        // notify others
                        var synthjid = new Jid(group.MUCName, Domain, theirnick);
                        foreach (var pair in group.XMPPUsers) {
                            var relay = new Presence();
                            relay.From = synthjid;
                            relay.To = pair.Value;
                            relay.MucUser = new User {
                                Item = new Item(Affiliation.member, Role.none)
                            };
                            Connection.Send(relay);
                        }

                        var reply = new Presence();
                        reply.Type = PresenceType.unavailable;
                        reply.To = pres.From;
                        reply.From = pres.To;
                        reply.MucUser = new User {
                            Status = new Status(110),
                            Item = new Item(Affiliation.member, Role.none) { Jid = pres.From }
                        };
                        Connection.Send(reply);
                        break;
                    default:
                        throw new NotImplementedException(String.Format("Presence type: {0}", pres.Type));
                    }
                }
            } catch (Exception ex) {
                EventResultSource.TrySetException(ex);
            }
        }

        private void HandleIq(object sender, agsXMPP.protocol.component.IQ iq)
        {
            // TODO: implement negative answer to registration attempts
            // TODO: implement negative answer to reserved nick queries
            // TODO: implement kicking/banning ?
            // TODO: implement membership (or rather the lack thereof) ?
            // TODO: implement moderators
            // TODO: implement room configuration (and maybe destruction)
            try {
                var inner = iq.FirstChild;
                if (inner.TagName == "query") {
                    if (inner.Namespace == "http://jabber.org/protocol/disco#info") {
                        var reply = new DiscoInfoIq(IqType.result);
                        reply.To = iq.From;
                        reply.From = iq.To;
                        reply.Id = iq.Id;

                        reply.Query.AddIdentity(new DiscoIdentity("text", "BridgeChat", "conference"));
                        reply.Query.AddFeature(new DiscoFeature("http://jabber.org/protocol/disco#info"));
                        reply.Query.AddFeature(new DiscoFeature("http://jabber.org/protocol/disco#items"));
                        reply.Query.AddFeature(new DiscoFeature("http://jabber.org/protocol/muc"));

                        Connection.Send(reply);
                    } else if (inner.Namespace == "http://jabber.org/protocol/disco#items") {
                        var reply = new DiscoItemsIq(IqType.result);
                        reply.To = iq.From;
                        reply.From = iq.To;
                        reply.Id = iq.Id;

                        foreach (var pair in Groups)
                            reply.Query.AddDiscoItem(new DiscoItem {
                                Jid = new Jid(pair.Value.MUCName, Domain, null),
                                Name = pair.Value.MUCName
                            });

                        Connection.Send(reply);
                    }
                }
            } catch (Exception ex) {
                EventResultSource.TrySetException(ex);
            }
        }

        public override bool TryBindGroup(uint groupid, string parameter, out Group result, out string diagnostics)
        {
            var split = parameter.Split('#');
            if (split.Length > 2) {
                result = null;
                diagnostics = "Illegal parameter format (should be 'room' or 'room#password')";
                return false;
            }

            var name = split[0];
            var group = new XMPPGroup(this, groupid, name, password: (split.Length == 2) ? split[1] : null);
            result = group;
            diagnostics = null;
            return Groups.TryAdd(name, group);
        }
    }
}

