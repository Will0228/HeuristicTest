using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using Factory;
using Model;
using R3;
using UnityEngine;

namespace Manager
{
    public sealed class GameManager : MonoBehaviour
    {
        private struct LogData
        {
            public int CurrentScore;
            public float CurrentTime;
            
        }
        
        [Header("エリアサイズ")]
        [SerializeField] private Vector2Int _areaSize = new Vector2Int(20, 20);
        
        [Header("コインの数")]
        [SerializeField] private int _coinNum = 100;
        
        [Header("時間制限")]
        [SerializeField] private int _timeLimit = 10;
        
        [Header("時間高速化倍率")]
        [SerializeField] private int _timeSpeedUpRate = 6;
        
        [Header("開始位置")]
        [SerializeField] private Vector2Int _startPos = new Vector2Int(0, 0);

        [Header("ゲーム開始までにかかる時間")] 
        [SerializeField] private float _preparationTime = 2;
        
        [Header("コイン比較用マップ作成")]
        [SerializeField] private AreaFactory _areaFactory;
        
        private HeuristicSolverModel _heuristicSolverModel;
        private GameModel _gameModel;
        private PlayerModel _playerModel;

        private CancellationTokenSource _cts = new();
        
        private readonly List<LogData> _logData = new();
        // データ用
        private Vector2Int _fistTargetPosition;
        
        void Awake()
        {
            if (_coinNum > _areaSize.x *  _areaSize.y)
            {
                Debug.LogError("エリアのサイズよりもコインの枚数が多いです");
                return;
            }

            SetUp();
            SetEvent();
            SetUpAsync().Forget();
        }

        private void SetEvent()
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _heuristicSolverModel.GetCoinScoreAsObservable()
                .Subscribe(GetCoin)
                .AddTo(_cts.Token);
            
            _playerModel.MovedAsObservable
                .Subscribe(_ => ArrivedTarget())
                .AddTo(_cts.Token);
            
            _playerModel.FirstMovedAsObservable
                .Subscribe(pos => _fistTargetPosition = pos)
                .AddTo(_cts.Token);
            
            // _playerModel.EndGameAsObservable
            //     .Subscribe(_ => ExportGreedyDataToCSV())
            //     .AddTo(_cts.Token);
        }

        /// <summary>
        /// 準備
        /// </summary>
        private void SetUp()
        {
            _heuristicSolverModel = new(_timeLimit, _startPos, _timeSpeedUpRate);
            _gameModel = new();
            _playerModel = new(_startPos, _timeSpeedUpRate, _timeLimit);
            
            var coinInfos = new CoinFactory().CreateCoinInfos(_areaSize, _coinNum);
            ExportHighScoreCoinDataCSV(coinInfos, "/DataLog/HighScoreCoinData.csv");
            ExportLowScoreCoinDataCSV(coinInfos, "/DataLog/LowScoreCoinData.csv");
            _heuristicSolverModel.SetCoinInfo(coinInfos);
            _areaFactory.CreateMap(coinInfos);
        }

        /// <summary>
        /// コインを取得した
        /// </summary>
        private void GetCoin(int score)
        {
            _gameModel.AddScore(score);
            _logData.Add(new LogData { CurrentScore = _gameModel.Score, CurrentTime = _playerModel.GetElapsedTime() });
            Debug.Log($"現在のスコア　：　{_gameModel.Score}");
        }

        /// <summary>
        /// プレイヤーが目的地に到着した
        /// </summary>
        private void ArrivedTarget()
        {
            _areaFactory.DestroyCube(_heuristicSolverModel.CurrentTargetCoinId);
            _heuristicSolverModel.SetNextTarget();
            _playerModel.MoveAsync(_heuristicSolverModel.CurrentTargetCoinPosition).Forget();
        }

        /// <summary>
        /// 非同期で準備
        /// </summary>
        private async UniTask SetUpAsync()
        {
            await OptimizeAsync(_preparationTime);
            GameStartAsync().Forget();
        }

        private async UniTask GameStartAsync()
        {
            Debug.Log("GameStart");
            
            _heuristicSolverModel.SetNextTarget();
            _playerModel.MoveAsync(_heuristicSolverModel.CurrentTargetCoinPosition).Forget();
            await OptimizeAsync(_timeLimit);
            
            Debug.Log("GameEnd");
            ExportToCSV();
            // ExportGreedyDataToCSV();
        }

        /// <summary>
        /// 最適化
        /// </summary>
        private async UniTask OptimizeAsync(float timeLimit)
        {
            // 非同期で計算を実行
            await UniTask.RunOnThreadPool(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
        
                // 指定された準備時間の間、全力で計算を回す
                while (sw.Elapsed.TotalSeconds < timeLimit)
                {
                    _heuristicSolverModel.Optimize();
                }
                sw.Stop();
            });
        }

        private void ExportToCSV()
        {
            ExportScoreDataCSV($"/DataLog/ScoreData/data_log{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv");
            ExportRemainedCoinPosDataCSV($"/DataLog/RemainedCoinPosData/data_log{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv");
        }
        
        private void ExportGreedyDataToCSV()
        {
            ExportScoreDataCSV($"/DataLog/ScoreData/Greedy/data_log{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv");
            ExportRemainedCoinPosDataCSV($"/DataLog/RemainedCoinPosData/Greedy/data_log{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv");
        }

        private void ExportScoreDataCSV(string filename)
        {
            string filePath = Application.dataPath + filename;
            StringBuilder sb = new StringBuilder();

            // ヘッダー
            sb.AppendLine("Score, Time, FirstTargetPosX, FirstTargetPosY");

            // データ
            foreach (var data in _logData)
            {
                sb.AppendLine($"{data.CurrentScore},{data.CurrentTime},{_fistTargetPosition.x},{_fistTargetPosition.y}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"スコアデータCSV保存完了: {filePath}");
        }

        private void ExportRemainedCoinPosDataCSV(string filename)
        {
            string filePath = Application.dataPath + filename;
            StringBuilder sb = new StringBuilder();

            // ヘッダー
            sb.AppendLine("PosX, PosY");

            // データ
            foreach (var data in _heuristicSolverModel.RemainedCoins)
            {
                sb.AppendLine($"{data.Position.x},{data.Position.y}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"残りコインデータCSV保存完了: {filePath}");
        }
        
        private void ExportHighScoreCoinDataCSV(IReadOnlyList<CoinInfo> infos, string filename)
        {
            var highScoreCoinPositions = infos.Where(info => info.Score >= 70).Select(info => info.Position).ToList();
            
            string filePath = Application.dataPath + filename;
            StringBuilder sb = new StringBuilder();

            // ヘッダー
            sb.AppendLine("x,y");

            // データ
            foreach (var data in highScoreCoinPositions)
            {
                sb.AppendLine($"{data.x},{data.y}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"スコアデータCSV保存完了: {filePath}");
        }
        
        private void ExportLowScoreCoinDataCSV(IReadOnlyList<CoinInfo> infos, string filename)
        {
            var highScoreCoinPositions = infos.Where(info => info.Score < 30).Select(info => info.Position).ToList();
            
            string filePath = Application.dataPath + filename;
            StringBuilder sb = new StringBuilder();

            // ヘッダー
            sb.AppendLine("x,y");

            // データ
            foreach (var data in highScoreCoinPositions)
            {
                sb.AppendLine($"{data.x},{data.y}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"スコアデータCSV保存完了: {filePath}");
        }
    }
}