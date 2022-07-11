namespace SocialNetworkApp.Models
{
    public class FollowerRecord {
        public Person FollowedPerson { get; set; }
        public int FollowedPersonId { get; set; }
        public Person FollowerPerson { get; set; }
        public int FollowerPersonId { get; set; }

        public FollowerRecord() { }

        public FollowerRecord (Person following, int followingId, Person follower, int followerId) 
        { 
            FollowedPerson = following; 
            FollowedPersonId = followingId;
            FollowerPerson = follower;
            FollowerPersonId = followerId;
        }
    }

    
}
