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

        public PlayerModel(Vector2Int startPosition)
        {
            _currentPosition = startPosition;
        }

        /// <summary>
        /// 移動開始
        /// 移動完了後は次のターゲットをマネージャーから受け取るために結果を発火する
        /// </summary>
        public async UniTask MoveAsync(Vector2Int targetPosition)
        {
            var takeTime = Vector2Int.Distance(_currentPosition, targetPosition);
            var elapsedTime = 0f;
            while (elapsedTime < takeTime)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.Yield();
            }
            
            _currentPosition = targetPosition;
            
            _onMovedSubject.OnNext(Unit.Default);
        }
    }
}