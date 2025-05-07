using System.Security.Cryptography.Xml;

namespace GameTeam.Scripts
{
    public static class TeamManager
    {
        private static Dictionary<int, Pendings> pending = new Dictionary<int, Pendings>();
        private static Dictionary<int, HashSet<int>> requests = new Dictionary<int, HashSet<int>>();

        public static void JoinTeam(int owner, int member, int applicationId)
        {
            if (owner == member) return;
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

            HashSet<int> userRequests;
            if (requests.TryGetValue(member, out userRequests))
            { 
                userRequests.Add(applicationId);
            }
            else
            {
                requests[member] = new HashSet<int>() { applicationId };
            }
        }

        public static void DeleteFromPending(int owner, int member, int applicationId)
        {
            if (owner == member) return;
            try
            {
                pending[owner].Requests.Remove(new PendingRequest(member, applicationId));
                requests[member].Remove(applicationId);
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

        public static HashSet<int> GetRequests(int userId)
        {
            HashSet<int> userRequests;
            if (requests.TryGetValue(userId, out userRequests))
            {
                return userRequests;
            }
            return new HashSet<int>();
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
        public HashSet<PendingRequest> Requests { get; set; }

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

        public override bool Equals(object? obj)
        {
            if (obj is not PendingRequest other)
                return false;

            return UserId == other.UserId && ApplicationId == other.ApplicationId;
        }

        public override int GetHashCode()
        {
            return UserId * 996617 + ApplicationId;
        }
    }
}
