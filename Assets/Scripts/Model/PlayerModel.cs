using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Model
{
    public sealed class PlayerModel
    {
        private Vector2Int _currentPosition;
        private Vector2Int _targetPosition;
        
        // 移動し終わったことを伝える
        private readonly Subject<Unit> _onMovedSubject = new();
        public Observable<Unit> MovedAsObservable => _onMovedSubject;
        
        // データ用として初めてターゲットとして認識した場所を取得
        private readonly Subject<Vector2Int> _onFirstMovedSubject = new();
        public Observable<Vector2Int> FirstMovedAsObservable => _onFirstMovedSubject;
        
        private readonly Subject<Unit> _onEndGameSubject = new();
        public Observable<Unit> EndGameAsObservable => _onEndGameSubject;
        
        private bool _isFirstTargetClear;
        
        private int _timeSpeedUpRate;
        private int _timeLimit;

        /// <summary>
        /// 残り時間
        /// </summary>
        private float _currentElapsedTime;

        public PlayerModel(Vector2Int startPosition, int speedUpRate, int timeLimit)
        {
            _currentPosition = startPosition;
            _timeSpeedUpRate = speedUpRate;
            _timeLimit = timeLimit;
        }

        /// <summary>
        /// 移動開始
        /// 移動完了後は次のターゲットをマネージャーから受け取るために結果を発火する
        /// </summary>
        public async UniTask MoveAsync(Vector2Int targetPosition)
        {
            var takeTime = Vector2Int.Distance(_currentPosition, targetPosition) /  _timeSpeedUpRate;
            var elapsedTime = 0f;
            while (elapsedTime < takeTime)
            {
                elapsedTime += Time.deltaTime;
                _currentElapsedTime += Time.deltaTime;

                if (_currentElapsedTime >= _timeLimit)
                {
                    Debug.Log("ゲーム終了");
                    _onEndGameSubject.OnNext(Unit.Default);
                    return;
                }
                
                await UniTask.Yield();
            }
            
            _currentPosition = targetPosition;

            if (!_isFirstTargetClear)
            {
                _isFirstTargetClear = true;
                _onFirstMovedSubject.OnNext(targetPosition);
            }
            
            _onMovedSubject.OnNext(Unit.Default);
        }
        
        public float GetElapsedTime() => _currentElapsedTime;
    }
}