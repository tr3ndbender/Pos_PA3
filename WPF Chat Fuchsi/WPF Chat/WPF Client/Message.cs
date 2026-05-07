using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Client
{
    public enum MessageType
    {
        Text,
        Login,
        Registration,
        LoginResult,
        ReceiveMessages,
        RoomText,
        ServerSendMessages
    }

    public class Message
    {
        public MessageType Type { get; set; }
        public String? Title { get; set; }
        public String? TheMessage { get; set; }
        public String? Username { get; set; }
        public String? Password { get; set; }
        public String? Room { get; set; }
        public Boolean? LoginSucceeded { get; set; }
        public List<String>? UserRooms { get; set; }
        public List<Messages>? Messages { get; set; }
    }

    public class Messages
    {
        public String? Title { get; set; }
        public String? TheMessage { get; set; }
        public String? Username { get; set; }
    }
}