namespace GameTeam.Scripts
{
    public static class TeamManager
    {
        private static Dictionary<int, Pendings> pending = new Dictionary<int, Pendings>();

        public static void JoinTeam(int owner, int member, int applicationId)
        {
            Pendings ownerPendings;
            if (pending.TryGetValue(owner, out ownerPendings))
            {
                ownerPendings.HasNew = true;
                ownerPendings.Requests.Add(new PendingRequest(member, applicationId));
            }
            else
            {
                pending[owner] = new Pendings(true, new HashSet<PendingRequest>() { new PendingRequest(member, applicationId) });
            }
        }

        public static void DeleteFromPending(int owner, int member, int applicationId)
        {
            try
            {
                pending[owner].Requests.Remove(new PendingRequest(member, applicationId));
            }
            catch 
            {
                throw new Exception("Не было такого человека");
            }
        }

        public static Pendings GetPending(int owner)
        {
            Pendings ownerPendings;
            if (pending.TryGetValue(owner, out ownerPendings))
            {
                return ownerPendings;
            }
            return new Pendings(false, new HashSet<PendingRequest>());
        }

        public static void ReadPendings(int owner)
        {
            if (pending.ContainsKey(owner))
            {
                pending[owner].HasNew = false;
            }
        }
    }

    public class Pendings
    {
        public bool HasNew { get; set; }
        public HashSet<PendingRequest> Requests {  get; set; }

        public Pendings(bool hasNew, HashSet<PendingRequest> requests)
        { 
            HasNew = hasNew; 
            Requests = requests;
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
