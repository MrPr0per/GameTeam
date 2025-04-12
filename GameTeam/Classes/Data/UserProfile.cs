using GameTeam.Scripts.Controllers;

namespace GameTeam.Classes.Data;

public class UserProfile
{
	public int UserId { get; }

	public string Description { get; }

	public string Skills { get; }
	
	public List<Game> Games { get; }
	
	public List<Availability> Availabilities { get; }

	public UserProfile(int userId, string description, string skills)
	{
		UserId = userId;
		Description = description;
		Skills = skills;
		Games = DatabaseController.GetUserGames(userId);
		Availabilities = DatabaseController.GetUserAvailabilities(userId);
	}
}