namespace SocialNetworkApp.Models
{
    public class Person
    {
        public int ID { get; set; }
        public string UserId { get; set; } 
        public string Name { get; set; }
        public string About { get; set; }
        public string Email { get; set; } 
        public string Phone { get; set; } 
        public List<Message> SendedMessages { get; set; } = new List<Message>();
        public List<Message> ReceivedMessages { get; set; } = new List<Message>();
        public List<FollowerRecord> Followers { get; set; } = new List<FollowerRecord>();
        public List<FollowerRecord> Followings { get; set; } = new List<FollowerRecord>();

        public Person()
        {
            UserId = "";
            Name = "Your name";
            About = "About you";
            Email = "Default@gmail.com";
            Phone = "No phone number";
            SendedMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
            Followers = new List<FollowerRecord>();
            Followings = new List<FollowerRecord>();
        }
        public Person(string name, string? email)
        {
            UserId="";
            Name = name; 
            About = "About you";
            Email = email != null ? email : "" ;
            Phone = "No phone number";
            SendedMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
            Followers = new List<FollowerRecord>();
            Followings = new List<FollowerRecord>();
        }
        public Person(string userId, string name, string about, string? email, string? phone)
        {
            UserId = userId;
            Name = name;
            About = about;
            Email = email != null ? email : "";
            Phone = phone != null ? phone : "";

            ReceivedMessages = new List<Message>();
            SendedMessages = new List<Message>();
            Followers = new List<FollowerRecord>();
            Followings = new List<FollowerRecord>();
        }
    }
}
