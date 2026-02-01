using System;
using System.Collections.Generic;
using R3;

namespace Model
{
    /// <summary>
    /// ゲームに関するデータを管理
    /// </summary>
    public sealed class GameModel
    {
        private int _score = 0;
        public int Score => _score;
        
        // スコア加算
        public void AddScore(int addScore) => _score += addScore;
    }
}