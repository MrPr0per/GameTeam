using GameTeam.Scripts.Controllers;

namespace GameTeam.Classes.Data;

public class Application
{
	public int Id { get; set; }
	
	public string Title { get; set; }
	
	public string Description { get; set; }
	
	public string Contacts { get; set; }
	
	public List<Availability> Availabilities { get; set; }
	
	public List<Game> Games { get; set; }
	
	public int PurposeId { get; set; }
	
	public int OwnerId { get; set; }

	public Application(int id, string title, string description, string contacts, int purposeId, int ownerId)
	{
		Id = id;
		Title = title;
		Description = description;
		Contacts = contacts;
		Availabilities = DatabaseController.GetAvailabilities(id, false);
		Games = DatabaseController.GetGames(id, false);
		PurposeId = purposeId;
		OwnerId = ownerId;
	}
}