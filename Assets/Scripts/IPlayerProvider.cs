public interface IPlayerProvider
{
    PlayerController Player { get; }
    event System.Action<PlayerController> PlayerChanged;
}