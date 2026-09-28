using System;
using System.Collections.Generic;

namespace ImageSearch.Data.Pixabay.DataSource
{
    internal sealed class PixabayRequestWindow
    {
        private readonly Queue<DateTime> _requests = new Queue<DateTime>();

        public bool TryEnter(DateTime utcNow)
        {
            while (_requests.Count > 0 && utcNow - _requests.Peek() >= TimeSpan.FromMinutes(1))
                _requests.Dequeue();
            if (_requests.Count >= 100) return false;
            _requests.Enqueue(utcNow);
            return true;
        }
    }
}
