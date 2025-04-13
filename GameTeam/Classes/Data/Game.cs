namespace GameTeam.Classes.Data;

public class Game
{
	public string Name { get; }

	public int Id { get; }

	public Game(int id, string name)
	{
		Name = name;
		Id = id;
	}
}