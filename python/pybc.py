import socket, struct, thread
from BridgeChat_pb2 import ModuleIntro, GroupMessage, UserEvent, UserStatus

def protosend(sock, msg):
    seri = msg.SerializeToString()
    sock.send(struct.pack('<I', len(seri)))
    sock.send(seri)

def recvall(sock, n):
    buf = ''
    while len(buf) < n:
        buf += sock.recv(n - len(buf))
    return buf

def protorecv(sock):
    serilen, = struct.unpack('<I', recvall(sock, 4))
    msg = GroupMessage()
    msg.ParseFromString(recvall(sock, serilen))
    return msg

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('127.0.0.1', 31337))

intro = ModuleIntro()
intro.long_name = 'Python console client'
intro.short_name = 'PYCON'
protosend(sock, intro)

while True:
    msg = protorecv(sock)
    if msg.HasField('binding_request'):
        break

group = msg.group_id
bindinfo = msg.binding_request.bind_info
print 'BindInfo:', bindinfo

msg = GroupMessage()
msg.group_id = group
msg.binding_response.success = True
protosend(sock, msg)

print '--- bound ---'

def insend():
    global group, sock
    name = raw_input('Your name: ')
    msg = GroupMessage()
    msg.group_id = group
    #msg.user_event = UserEvent()
    msg.user_event.username = name
    #msg.user_event.user_status = UserStatus()
    msg.user_event.user_status.online_status = True
    protosend(sock, msg)
    
    while True:
        line = raw_input()
        msg = GroupMessage()
        msg.group_id = group
        #msg.user_event = UserEvent()
        msg.user_event.username = name
        msg.user_event.chat_message = line
        protosend(sock, msg)

thread.start_new_thread(insend, ())

while True:
    msg = protorecv(sock)
    if msg.HasField('user_event'):
        ue = msg.user_event
        if ue.HasField('user_status'):
            if ue.user_status.online_status:
                print '[%s] %s joined' % (ue.plugin_id, ue.username)
            else:
                print '[%s] %s left' % (ue.plugin_id, ue.username)
        if ue.HasField('chat_message'):
            print '[%s] %s: %s' % (ue.plugin_id, ue.username, ue.chat_message)
