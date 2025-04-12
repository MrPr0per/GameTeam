using NodaTime;

namespace GameTeam.Classes.Data;

public class Availability
{
	public int Id { get; }

	public DayOfWeekEnum DayOfWeek { get; }

	public OffsetTime StartTime { get; }

	public OffsetTime EndTime { get; }
		
	public enum DayOfWeekEnum
	{
		Monday,
		Tuesday,
		Wednesday,
		Thursday, 
		Friday,
		Saturday,
		Sunday,
	}

	public Availability(int id, DayOfWeekEnum dayOfWeek, OffsetTime startTime, OffsetTime endTime)
	{
		Id = id;
		DayOfWeek = dayOfWeek;
		StartTime = startTime;
		EndTime = endTime;
	}
}