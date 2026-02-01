using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Factory;
using Model;
using R3;
using UnityEngine;

namespace Manager
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("エリアサイズ")]
        [SerializeField] private Vector2Int _areaSize = new Vector2Int(20, 20);
        
        [Header("コインの数")]
        [SerializeField] private int _coinNum = 100;
        
        [Header("時間制限")]
        [SerializeField] private float _timeLimit = 240f;
        
        [Header("開始位置")]
        [SerializeField] private Vector2Int _startPos = new Vector2Int(10, 10);

        [Header("ゲーム開始までにかかる時間")] 
        [SerializeField] private float _preparationTime = 2;
        
        [Header("コイン比較用マップ作成")]
        [SerializeField] private AreaFactory _areaFactory;
        
        private HeuristicSolverModel _heuristicSolverModel;
        private GameModel _gameModel;
        private PlayerModel _playerModel;

        private CancellationTokenSource _cts = new();
        
        void Awake()
        {
            if (_coinNum > _areaSize.x *  _areaSize.y)
            {
                Debug.LogError("エリアのサイズよりもコインの枚数が多いです");
                return;
            }

            SetUp();
            SetUpAsync().Forget();
        }

        /// <summary>
        /// 準備
        /// </summary>
        private void SetUp()
        {
            _heuristicSolverModel = new(_timeLimit, _startPos);
            _gameModel = new();
            _playerModel = new(_startPos);
            var coinInfos = new CoinFactory().CreateCoinInfos(_areaSize, _coinNum);
            _heuristicSolverModel.SetCoinInfo(coinInfos);
            _areaFactory.CreateMap(coinInfos);
            
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _heuristicSolverModel.GetCoinScoreAsObservable()
                .Subscribe(GetCoin)
                .AddTo(_cts.Token);
            
            _playerModel.MovedAsObservable
                .Subscribe(_ => ArrivedTarget())
                .AddTo(_cts.Token);
        }

        /// <summary>
        /// コインを取得した
        /// </summary>
        private void GetCoin(int score)
        {
            _gameModel.AddScore(score);
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
            ObservableSystem.DefaultFrameProvider = UnityFrameProvider.Update;
            // Observable.EveryUpdate()
            //     .Subscribe(_ => TimeDecrease())
            //     .AddTo(_cts.Token);

            _heuristicSolverModel.SetNextTarget();
            _playerModel.MoveAsync(_heuristicSolverModel.CurrentTargetCoinPosition).Forget();
            await OptimizeAsync(_timeLimit);
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
            
                    // ここで Yield はしない（ThreadPoolなので、全力で回してOK）
                }
                sw.Stop();
            });
        }
    }
}