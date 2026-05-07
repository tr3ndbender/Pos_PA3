using DataModels;

namespace Server
{
    public class MSG
    {
        public enum MessageType {SEARCH, SEARCHRESULT, DETAIL, DETAILRESULT, ALTERNATIVE, ALTERNATIVERESULT} // das hier ist keine Variable der MSG klasse einfach nur ein enum lol
        public MessageType Type { get; set; }
        public String? Search {  get; set; }
        public String? Sex { get; set; }
        public List<String>? Names { get; set; }
        public String ? DetailRequest { get; set; }
        public List<Babyname>? Details { get; set; }
        public List<String>? AlternativeNames { get; set; }
    }
}
