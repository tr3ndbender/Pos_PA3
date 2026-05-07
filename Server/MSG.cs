using DataModels;

namespace Server
{
    public class MSG
    {
        public enum MessageType {GUESS, RESPONSE ,INITIALWORD} // das hier ist keine Variable der MSG klasse einfach nur ein enum lol
        public MessageType? Type { get; set; }
        public String? Wort {  get; set; }
        public List<String>? Results { get; set; }
        public bool? trueOrFalse { get; set; }
    }
}
