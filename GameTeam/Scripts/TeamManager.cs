namespace GameTeam.Scripts
{
    public static class TeamManager
    {
        private static Dictionary<int, HashSet<int>> pending = new Dictionary<int, HashSet<int>>();

        public static void JoinTeam(int owner, int member)
        {
            HashSet<int> ownerPendings;
            if (pending.TryGetValue(owner, out ownerPendings))
            {
                ownerPendings.Add(member);
            }
            else
            {
                pending[owner] = new HashSet<int>() { member };
            }
        }

        public static void DeleteFromPending(int owner, int member)
        {
            try
            {
                pending[owner].Remove(member);
            }
            catch 
            {
                throw new Exception("Не было такого человека");
            }
        }

        public static HashSet<int> GetPending(int owner)
        {
            return pending[owner];
        }
    }
}
