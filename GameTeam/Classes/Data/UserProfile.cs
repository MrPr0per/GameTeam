using GameTeam.Scripts.Controllers;

namespace GameTeam.Classes.Data;

public class UserProfile
{
	public int UserId { get; }

	public string Description { get; }
	
	public List<Game> Games { get; }
	
	public List<Availability> Availabilities { get; }

	public UserProfile(int userId, string description)
	{
		UserId = userId;
		Description = description;
		Games = DatabaseController.GetGames(userId, true);
		Availabilities = DatabaseController.GetAvailabilities(userId, true);
	}
}