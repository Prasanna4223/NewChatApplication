//using Microsoft.AspNetCore.SignalR;

//namespace SignalRChatApplication
//{
//    public class CallHub : Hub
//    {
//        private static Dictionary<string, string> _connections = new Dictionary<string, string>();
//        public async Task SendOffer(string receiverConnectionId, string offer)
//        {

//            await Clients.Client(receiverConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, offer);

//        }
//        public async Task<string> RegisterUser(string username)
//        {
//            _connections[Context.ConnectionId] = username;
//            await Clients.All.SendAsync("UpdateUserList", _connections);
//            return Context.ConnectionId;
//        }

//        public async Task SendAnswer(string callerConnectionId, string answer)
//        {
//            await Clients.Client(callerConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
//        }

//        public async Task SendIceCandidate(string userId, string candidate)
//        {
//            await Clients.Client(userId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
//        }
//        public async Task RejectCall(string callerId)
//        {
//            await Clients.Client(callerId).SendAsync("CallRejected", Context.ConnectionId);
//        }
//        public override Task OnConnectedAsync()
//        {
//            _connections[Context.ConnectionId] = Context.ConnectionId;
//            return base.OnConnectedAsync();
//        }

//        // Track when a user disconnects
//        public override Task OnDisconnectedAsync(Exception exception)
//        {
//            _connections.Remove(Context.ConnectionId);
//            return base.OnDisconnectedAsync(exception);
//        }

//    }
//}

using Microsoft.AspNetCore.SignalR;

namespace SignalRChatApplication
{
 

    public class CallHub : Hub
    {
        private static readonly Dictionary<string, UserInfo> Users = new();

        

        public async Task ToggleDnd(bool enabled)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var info))
            {
                info.IsDoNotDisturb = enabled;
                await BroadcastUserList();
            }
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (!Users.TryGetValue(Context.ConnectionId, out var sender)) return;
            if (!Users.TryGetValue(receiverId, out var receiver)) return;

            var senderName = sender.Name;

            await Clients.Client(receiverId).SendAsync("ReceivePrivateMessage", senderName, message);
            await Clients.Caller.SendAsync("ReceivePrivateMessage", senderName, message); // echo
        }

        public async Task SendOffer(string receiverId, object offer)
        {
            if (offer == null) return;

            if (!Users.TryGetValue(receiverId, out var receiver) ||
                receiver.IsDoNotDisturb ||
                receiver.IsInCall)
            {
                await Clients.Caller.SendAsync("CallRejected", "User is busy or in Do Not Disturb");
                return;
            }

            if (!Users.TryGetValue(Context.ConnectionId, out var caller)) return;

            await Clients.Client(receiverId).SendAsync("IncomingCall", Context.ConnectionId, caller.Name, offer);
        }

        public async Task AcceptCall(string callerId)
        {
            if (!Users.ContainsKey(Context.ConnectionId) || !Users.ContainsKey(callerId)) return;

            Users[Context.ConnectionId].IsInCall = true;
            Users[callerId].IsInCall = true;

            await BroadcastUserList();
            await Clients.Client(callerId).SendAsync("CallAccepted", Context.ConnectionId);
        }

        public async Task RejectCall(string callerId, string reason = "Rejected")
        {
            if (Users.ContainsKey(callerId))
                await Clients.Client(callerId).SendAsync("CallRejected", reason);
        }

        public async Task EndCall(string peerId)
        {
            Users.TryGetValue(Context.ConnectionId, out var me);
            if (me != null) me.IsInCall = false;

            if (Users.TryGetValue(peerId, out var peer))
                peer.IsInCall = false;

            await BroadcastUserList();

            // Safely notify both sides even if one disconnected
            await Clients.Client(peerId).SendAsync("CallEnded");
            await Clients.Caller.SendAsync("CallEnded");
        }

        public async Task SendAnswer(string callerId, object answer)
        {
            if (answer != null && Users.ContainsKey(callerId))
                await Clients.Client(callerId).SendAsync("ReceiveAnswer", answer);
        }

        public async Task SendIceCandidate(string targetId, object candidate)
        {
            if (candidate != null && Users.ContainsKey(targetId))
                await Clients.Client(targetId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            Users.Remove(Context.ConnectionId);
            await BroadcastUserList();
            await base.OnDisconnectedAsync(ex);
        }

       
        // Add this if you don’t already have it (important!)
        public override async Task OnConnectedAsync()
        {
            await BroadcastUserList();
            await base.OnConnectedAsync();
        }

        public async Task<string> Register(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Guest";

            var info = new UserInfo
            {
                Name = name.Trim(),
                IsDoNotDisturb = false,
                IsInCall = false
            };

            Users[Context.ConnectionId] = info;

            await BroadcastUserList();   

            return Context.ConnectionId;
        }

        private async Task BroadcastUserList()
        {
            var list = Users.ToDictionary(
                u => u.Key,
                u => new UserInfo
                {
                    Name = u.Value.Name,
                    IsDoNotDisturb = u.Value.IsDoNotDisturb,
                    IsInCall = u.Value.IsInCall
                });

            await Clients.All.SendAsync("UserListUpdated", list);
        }
    }
}