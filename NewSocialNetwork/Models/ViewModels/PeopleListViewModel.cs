namespace NewSocialNetwork.Models.ViewModels
{
    public class PeopleListViewModel
    {
        public IEnumerable<Person> People { get; set; } = new List<Person>();
        public string? Search { get; set; }
    }
}
