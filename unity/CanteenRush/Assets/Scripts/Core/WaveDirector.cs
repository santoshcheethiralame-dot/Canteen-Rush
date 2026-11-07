using System;
namespace CanteenRush.Core
{
    public sealed class WaveDirector
    {
        readonly GameConfig _config;
        readonly Random _rng;
        float _spawnElapsed;
        int _wave;
        public WaveDirector(GameConfig cfg, Random rng) { _config = cfg; _rng = rng; }
        public RoundState Current => _wave >= 3 ? RoundState.Results : (RoundState)((int)RoundState.Wave1 + _wave);
        public float Elapsed { get; private set; }
        public void Tick(float dt)
        {
            if (dt <= 0 || _wave >= 3) return;
            while (dt > 0 && _wave < 3)
            {
                float boundary = 0;
                for (int i = 0; i <= _wave; i++) boundary += _config.WaveSeconds[i];
                float step = Math.Min(dt, Math.Max(0, boundary - Elapsed));
                Elapsed += step; _spawnElapsed += step; dt -= step;
                if (Elapsed >= boundary) { _wave++; _spawnElapsed = 0; }
                else break;
            }
        }
        public bool ShouldSpawnCustomer(int activeCustomers, int freeSeats)
        {
            if (_wave >= 3 || _spawnElapsed < _config.WaveSpawnInterval[_wave]) return false;
            _spawnElapsed %= _config.WaveSpawnInterval[_wave];
            return freeSeats > 0 && activeCustomers < _config.WaveMaxCustomers[_wave];
        }
        public OrderTemplate NextTemplate()
        {
            if (_wave >= 3) throw new InvalidOperationException("The round has ended.");
            int eligible = 0;
            foreach (var template in _config.Templates)
                if (template != null && template.MinWave <= _wave + 1 && template.Items != null && template.Items.Length >= (_wave == 2 ? 2 : 1) && template.Items.Length <= (_wave == 0 ? 1 : _wave == 1 ? 2 : 3)) eligible++;
            if (eligible == 0) throw new InvalidOperationException("No eligible order templates configured.");
            int choice = _rng.Next(eligible);
            foreach (var template in _config.Templates)
                if (template != null && template.MinWave <= _wave + 1 && template.Items != null && template.Items.Length >= (_wave == 2 ? 2 : 1) && template.Items.Length <= (_wave == 0 ? 1 : _wave == 1 ? 2 : 3) && choice-- == 0) return template;
            throw new InvalidOperationException();
        }
    }
}
