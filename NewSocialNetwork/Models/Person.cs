namespace NewSocialNetwork.Models
{
    public class Person
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string About { get; set; }
        public string? Email { get; set; }
        public List<Message> SendedMessages { get; set; } = new List<Message>();
        public List<Message> ReceivedMessages { get; set; } = new List<Message>();

        // public List<Person> Followings { get; set; }

        public Person() {
            Name = "Your name";
            About = "About you";
            Email = "Default@gmail.com";
            SendedMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
        }
        public Person(string email)
        {
            Name = "Your name"; 
            About = "About you";
            Email = email;
            SendedMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
        }
        public Person(int iD, string name, string about, string? email)
        {
            ID = iD;
            Name = name;
            About = about;
            Email = email;
            ReceivedMessages = new List<Message>();
            SendedMessages = new List<Message>();
        }
    }
}
