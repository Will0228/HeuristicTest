using System.Collections.Generic;
using Domain;
using UnityEngine;
using Random = System.Random;

namespace Factory
{
    public sealed class CoinFactory
    {
        private Random rand = new Random(1);

        public List<CoinInfo> CreateCoinInfos(Vector2Int areaSize, int coinNum)
        {
            var tempCoinInfos = new List<CoinInfo>();
            var usedPos = new HashSet<Vector2Int>();
            
            for (int i = 0; i < coinNum; i++)
            {
                Vector2Int coinPos;
                while (true)
                {
                    // Debug.LogError($"x Rand : {rand.Next(0, areaSize.x)} , y Rand : {rand.Next(0, areaSize.y)}");
                    coinPos = new Vector2Int(rand.Next(0, areaSize.x), rand.Next(0, areaSize.y));
                    if (usedPos.Add(coinPos))
                    {
                        var info = new CoinInfo(i.ToString(), coinPos, i+1);
                        tempCoinInfos.Add(info);
                        break;
                    }
                }
            }
            
            return tempCoinInfos;
        }
    }
}