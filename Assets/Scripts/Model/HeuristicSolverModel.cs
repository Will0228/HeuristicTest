using System.Collections.Generic;
using Domain;
using R3;
using UnityEngine;

namespace Model
{
    /// <summary>
    /// ロジック実装
    /// </summary>
    public sealed class HeuristicSolverModel
    {
        // 冷却
        private const float COOLING = 0.90f;
        
        // 目的地までのデータを管理
        private readonly Stack<Vector2Int> _destinations = new();
        
        private readonly Vector2Int[,] _cachedDistanceMatrix = new Vector2Int[3, 3];

        private Vector2Int _currentPosition;
        private List<CoinInfo> _currentRoutes = new();
        public IReadOnlyList<CoinInfo> RemainedCoins => _currentRoutes;
        private CoinInfo _currentTargetCoinInfo;
        public int CurrentTargetCoinId => int.Parse(_currentTargetCoinInfo.Id);
        public Vector2Int CurrentTargetCoinPosition => _currentTargetCoinInfo.Position;
        private int _currentScore = 0;
        private float _currentTemp = 1000f;

        private int _tryCalculateCount = 0;

        private float _timeLimit;
        private Vector2Int _startPos;
        private int _timeSpeedUpRate;
        
        private readonly Subject<int> _onGetCoinScoreSubject = new();
        public Observable<int> GetCoinScoreAsObservable() => _onGetCoinScoreSubject;
        
        // スレッドセーフなRandom
        private System.Random _rand = new();

        public HeuristicSolverModel(float timeLimit, Vector2Int startPos, int timeSpeedUpRate)
        {
            _timeLimit = timeLimit;
            _startPos = startPos;
            _timeSpeedUpRate = timeSpeedUpRate;
        }
        
        public void SetCoinInfo(List<CoinInfo> coinInfos)
        {
            Solve(_startPos, coinInfos);
        }

        private void Solve(Vector2Int startPos, List<CoinInfo> coinInfos)
        {
            _currentPosition = startPos;
            
            // 貪欲法によるルート検索
            _currentRoutes = GetGreedyRoute(startPos, coinInfos);
            _currentScore = CalculateScore(startPos, _currentRoutes);
        }

        public void SetNextTarget()
        {
            if (_currentTargetCoinInfo != null)
            {
                _onGetCoinScoreSubject.OnNext(_currentTargetCoinInfo.Score);
            }
            _currentTargetCoinInfo = _currentRoutes[0];
            _currentRoutes.RemoveAt(0);
        }

        // 焼きなまし法による最適化
        public void Optimize()
        {
            // _tryCalculateCount++;
            // if (_tryCalculateCount % 1000 == 0)
            // {
            //     Debug.Log($"試行回数 : {_tryCalculateCount}");
            // }
        
            var nextRoute = ReverseRoot(_currentRoutes);
            int nextScore = CalculateScore(_currentPosition, nextRoute);

            if (AcceptanceProbability(nextScore) > _rand.NextDouble())
            {
                _currentRoutes = nextRoute;
                _currentScore = nextScore;
                // Debug.LogWarning($"スコア更新　：　{_currentScore}");
            }
            _currentTemp *= COOLING;
        }

        /// <summary>
        /// スコア計算
        /// </summary>
        private int CalculateScore(Vector2Int startPos, IReadOnlyList<CoinInfo> infos)
        {
            var score = 0;
            var elapsed = 0f;
            var currentPosition = startPos;
            var routeCount = 0;
            
            foreach (var info in infos)
            {
                elapsed += Vector2Int.Distance(currentPosition, info.Position) / _timeSpeedUpRate;
                // 制限時間を超えた場合は強制終了
                if (elapsed >= _timeLimit)
                {
                    break;
                }
                
                score += info.Score;
                currentPosition = info.Position;
                routeCount++;
            }
            // Debug.Log($"ルート探索した場所　：　{routeCount}");
            return score;
        }

        // 変更後のルート受け入れ可能性
        private float AcceptanceProbability(int next)
        {
            if (next > _currentScore) return 1.0f;
            return Mathf.Exp((next - _currentScore) / _currentTemp);
        }

        // 局所探索法によるルート変更
        private List<CoinInfo> ReverseRoot(IReadOnlyList<CoinInfo> currentInfos)
        {
            var next = new List<CoinInfo>(currentInfos);
            int lPos = _rand.Next(0, next.Count);
            int targetNums = _rand.Next(0, next.Count - lPos);
            next.Reverse(lPos, targetNums);
            return next;
        }

        /// <summary>
        /// 貪欲法によるルート探索
        /// </summary>
        private List<CoinInfo> GetGreedyRoute(Vector2Int startPos, List<CoinInfo> coinInfos)
        {
            var tempList = new List<CoinInfo>();
            // 計算用に元のリストをコピー
            var remainingCoins = new List<CoinInfo>(coinInfos);
            CalculateOptimalCoin(startPos, tempList, remainingCoins);
            return tempList;
        }

        /// <summary>
        /// 貪欲法
        /// 得点を最も稼げる順番にコインの情報を並べる
        /// </summary>
        /// <param name="currentPos">現在位置</param>
        /// <param name="resultCoins">並び替えができているコインの情報</param>
        /// <param name="remainingCoins">まだ残っているコイン</param>
        private void CalculateOptimalCoin(Vector2Int currentPos, List<CoinInfo> resultCoins, List<CoinInfo> remainingCoins)
        {
            CoinInfo optimalCoin = null;
            var maxPoint = -1f;
            
            foreach (var info in remainingCoins)
            {
                var point = EvaluationFunc(currentPos, info);
                if (point > maxPoint)
                {
                    maxPoint = point;
                    optimalCoin = info;
                }
            }

            if (optimalCoin != null)
            {
                resultCoins.Add(optimalCoin);
                remainingCoins.Remove(optimalCoin);
                
                // もし他に取得可能なコインがない場合は終了
                if (remainingCoins.Count == 0)
                {
                    return;
                }
                
                // 他に取得可能なコインがある場合は再起的に調査
                CalculateOptimalCoin(optimalCoin.Position, resultCoins, remainingCoins);
            }
        }

        /// <summary>
        /// （スコア / 2地点間の距離）を評価とする
        /// </summary>
        private float EvaluationFunc(Vector2 currentPos, CoinInfo info)
        {
            return info.Score / Vector2.Distance(currentPos, info.Position);
        }
    }
}