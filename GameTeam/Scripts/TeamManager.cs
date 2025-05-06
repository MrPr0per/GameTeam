namespace GameTeam.Scripts
{
    public static class TeamManager
    {
        private static Dictionary<int, HashSet<PendingRequest>> pending = new Dictionary<int, HashSet<PendingRequest>>();

        public static void JoinTeam(int owner, int member, int applicationId)
        {
            HashSet<PendingRequest> ownerPendings;
            if (pending.TryGetValue(owner, out ownerPendings))
            {
                ownerPendings.Add(new PendingRequest(member, applicationId));
            }
            else
            {
                pending[owner] = new HashSet<PendingRequest>() { new PendingRequest(member, applicationId) };
            }
        }

        public static void DeleteFromPending(int owner, int member, int applicationId)
        {
            try
            {
                pending[owner].Remove(new PendingRequest(member, applicationId));
            }
            catch 
            {
                throw new Exception("Не было такого человека");
            }
        }

        public static HashSet<PendingRequest> GetPending(int owner)
        {
            HashSet<PendingRequest> ownerPendings;
            if (pending.TryGetValue(owner, out ownerPendings))
                return ownerPendings;
            return new HashSet<PendingRequest>();
        }
    }

    public class PendingRequest
    {
        public int UserId { get; set; }
        public int ApplicationId { get; set; }

        public PendingRequest(int userId, int applicationId)
        {
            UserId = userId;
            ApplicationId = applicationId;
        }
    }
}
