namespace Final_Aisle_Server.Network.EventArgs
{
    /// <summary>
    /// Event object related to user interactions.
    /// </summary>
    public sealed class UserEventArgs
    {
        public NetworkUser User { get; set; }

        public UserEventArgs(NetworkUser user) => User = user;
    }
}
