using System;
using System.Collections.Generic;
using TankBattle.Core.Shared;

namespace TankBattle.Core.TurnFlow
{
    /// <summary>
    /// <see cref="ITurnOrderService"/> 的預設實作。
    /// Initialize 時以注入的 <see cref="IRandomSource"/>（Fisher-Yates）將參與者隨機排序一次，
    /// 形成本場戰鬥固定循環的行動順序（US-06）。
    /// </summary>
    public sealed class TurnOrderService : ITurnOrderService
    {
        private List<ITurnParticipant> _order = new List<ITurnParticipant>();
        private int _currentIndex;

        public ITurnParticipant Current => _order.Count == 0 ? null : _order[_currentIndex];

        public IReadOnlyList<ITurnParticipant> CurrentOrderSnapshot => _order.AsReadOnly();

        public void Initialize(IReadOnlyList<ITurnParticipant> participants, IRandomSource random)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            _order = new List<ITurnParticipant>(participants);
            Shuffle(_order, random);
            _currentIndex = 0;
        }

        private static void Shuffle(List<ITurnParticipant> list, IRandomSource random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public ITurnParticipant Advance()
        {
            if (_order.Count == 0)
            {
                return null;
            }

            int startIndex = _currentIndex;
            do
            {
                _currentIndex = (_currentIndex + 1) % _order.Count;
            }
            while (!_order[_currentIndex].State.IsAlive && _currentIndex != startIndex);

            return Current;
        }

        public void RemoveParticipant(int tankId)
        {
            int index = _order.FindIndex(p => p.State.TankId == tankId);
            if (index < 0)
            {
                return;
            }

            _order.RemoveAt(index);

            if (_order.Count == 0)
            {
                _currentIndex = 0;
                return;
            }

            if (index < _currentIndex)
            {
                _currentIndex--;
            }

            if (_currentIndex >= _order.Count)
            {
                _currentIndex = 0;
            }
        }
    }
}
