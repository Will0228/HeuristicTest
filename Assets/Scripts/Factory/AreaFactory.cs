using System.Collections.Generic;
using Domain;
using UnityEngine;

namespace Factory
{
    public sealed class AreaFactory : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Material[] _materials;
        [SerializeField] private ParticleSystem _destroyParticle;

        private List<GameObject> _cachedCubes = new();
        
        public void CreateMap(IReadOnlyList<CoinInfo> coinInfos)
        {
            foreach (var info in coinInfos)
            {
                var go = Instantiate(_prefab, new Vector3(info.Position.x - 9.5f, 0, info.Position.y - 9.5f), Quaternion.identity);
                var renderer = go.GetComponent<Renderer>();
                renderer.material = _materials[info.Score - 1];
                _cachedCubes.Add(go);
            }
        }
        
        public void DestroyCube(int id)
        {
            var effect = Instantiate(_destroyParticle, _cachedCubes[id].transform.position, _destroyParticle.transform.rotation);
            Destroy(_cachedCubes[id]);
            Destroy(effect, 2f);
        }
    }
}