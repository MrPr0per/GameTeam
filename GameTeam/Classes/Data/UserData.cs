namespace GameTeam.Classes.Data;

public class UserData
{
	public int UserId { get; }

	public string Email { get; }
	
	public string Username { get; }
	

	public UserData(int userId, string email, string username)
	{
		UserId = userId;
		Email = email;
		Username = username;
	}
}