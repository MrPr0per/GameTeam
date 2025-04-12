using GameTeam.Scripts.Controllers;

namespace GameTeam.Classes.Data;

public class Application
{
	public int Id { get; }
	
	public string Title { get; }
	
	public string Description { get; }
	
	public string Contacts { get; }
	
	public List<Availability> Availabilities { get; }
	
	public List<Game> Games { get; }

	public Application(int id, string title, string description, string contacts)
	{
		Id = id;
		Title = title;
		Description = description;
		Contacts = contacts;
		Availabilities = DatabaseController.GetAvailabilities(id, false);
		Games = DatabaseController.GetGames(id, false);
	}
}