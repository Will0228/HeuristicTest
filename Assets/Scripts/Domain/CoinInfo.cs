using UnityEngine;

namespace Domain
{
    public sealed record CoinInfo(string Id, Vector2Int Position, int Score);
}