namespace NewSocialNetwork.Models
{
    public class Message
    {
        public int ID { get; set; } 
        public string Text { get; set; }
        public Person Sender { get; set; } = new Person();
        public int SenderId { get; set; }
        public Person Receiver { get; set; } = new Person();
        public int ReceiverId { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public bool Reported { get; set; } = false;

        public Message() { }

    }
    
}
    
