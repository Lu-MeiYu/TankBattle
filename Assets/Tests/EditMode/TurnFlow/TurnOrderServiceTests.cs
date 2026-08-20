using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class TurnOrderServiceTests
    {
        private static List<FakeTurnParticipant> CreateParticipants(int count)
        {
            var list = new List<FakeTurnParticipant>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new FakeTurnParticipant(new FakeTankState(i)));
            }

            return list;
        }

        [Test]
        public void Initialize_WithNullParticipants_Throws()
        {
            var service = new TurnOrderService();
            Assert.Throws<ArgumentNullException>(() =>
                service.Initialize(null, new SeededRandomSource(1)));
        }

        [Test]
        public void Initialize_WithNullRandom_Throws()
        {
            var service = new TurnOrderService();
            IReadOnlyList<ITurnParticipant> participants = CreateParticipants(3).Cast<ITurnParticipant>().ToList();
            Assert.Throws<ArgumentNullException>(() => service.Initialize(participants, null));
        }

        [Test]
        public void Initialize_ProducesPermutationCoveringAllParticipants()
        {
            var participants = CreateParticipants(6);
            var service = new TurnOrderService();

            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(42));

            var snapshotIds = service.CurrentOrderSnapshot.Select(p => p.State.TankId).OrderBy(id => id).ToList();
            var originalIds = participants.Select(p => p.FakeState.TankId).OrderBy(id => id).ToList();
            CollectionAssert.AreEqual(originalIds, snapshotIds);
        }

        [Test]
        public void Initialize_WithSameSeed_IsDeterministic()
        {
            var participantsA = CreateParticipants(8).Cast<ITurnParticipant>().ToList();
            var participantsB = CreateParticipants(8).Cast<ITurnParticipant>().ToList();

            var serviceA = new TurnOrderService();
            var serviceB = new TurnOrderService();

            serviceA.Initialize(participantsA, new SeededRandomSource(1234));
            serviceB.Initialize(participantsB, new SeededRandomSource(1234));

            var orderA = serviceA.CurrentOrderSnapshot.Select(p => p.State.TankId).ToList();
            var orderB = serviceB.CurrentOrderSnapshot.Select(p => p.State.TankId).ToList();

            CollectionAssert.AreEqual(orderA, orderB);
        }

        [Test]
        public void Current_BeforeInitialize_IsNull()
        {
            var service = new TurnOrderService();
            Assert.IsNull(service.Current);
        }

        [Test]
        public void Advance_CyclesThroughAllAliveParticipantsAndWrapsAround()
        {
            var participants = CreateParticipants(3);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            var snapshot = service.CurrentOrderSnapshot;
            var first = service.Current;

            var second = service.Advance();
            var third = service.Advance();
            var wrapped = service.Advance();

            Assert.AreEqual(snapshot[1], second);
            Assert.AreEqual(snapshot[2], third);
            Assert.AreEqual(snapshot[0], wrapped);
            Assert.AreEqual(snapshot[0], first);
        }

        [Test]
        public void Advance_SkipsDeadParticipantsWithoutExplicitRemoval()
        {
            var participants = CreateParticipants(4);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            var snapshot = service.CurrentOrderSnapshot;
            // Kill the tank at index 1 in the shuffled order without removing it from the list.
            ((FakeTurnParticipant)snapshot[1]).FakeState.Kill();

            var next = service.Advance();

            Assert.AreEqual(snapshot[2], next);
        }

        [Test]
        public void Advance_WhenOnlyCurrentAlive_ReturnsSameParticipant()
        {
            var participants = CreateParticipants(3);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            var snapshot = service.CurrentOrderSnapshot;
            ((FakeTurnParticipant)snapshot[1]).FakeState.Kill();
            ((FakeTurnParticipant)snapshot[2]).FakeState.Kill();

            var result = service.Advance();

            Assert.AreEqual(snapshot[0], result);
        }

        [Test]
        public void Advance_OnEmptyOrder_ReturnsNull()
        {
            var service = new TurnOrderService();
            service.Initialize(new List<ITurnParticipant>(), new SeededRandomSource(1));

            Assert.IsNull(service.Advance());
        }

        [Test]
        public void RemoveParticipant_RemovesFromSnapshot()
        {
            var participants = CreateParticipants(4);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            int idToRemove = service.CurrentOrderSnapshot[2].State.TankId;
            service.RemoveParticipant(idToRemove);

            Assert.IsFalse(service.CurrentOrderSnapshot.Any(p => p.State.TankId == idToRemove));
            Assert.AreEqual(3, service.CurrentOrderSnapshot.Count);
        }

        [Test]
        public void RemoveParticipant_WithUnknownId_IsNoOp()
        {
            var participants = CreateParticipants(3);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            Assert.DoesNotThrow(() => service.RemoveParticipant(9999));
            Assert.AreEqual(3, service.CurrentOrderSnapshot.Count);
        }

        [Test]
        public void RemoveParticipant_BeforeCurrentIndex_ShiftsCurrentIndexCorrectly()
        {
            var participants = CreateParticipants(4);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            // Move current to index 2.
            service.Advance();
            service.Advance();
            var currentBeforeRemoval = service.Current;

            // Remove the participant at index 0 (before current index).
            int idAtIndexZero = service.CurrentOrderSnapshot[0].State.TankId;
            service.RemoveParticipant(idAtIndexZero);

            Assert.AreEqual(currentBeforeRemoval, service.Current);
        }

        [Test]
        public void RemoveParticipant_RemovingCurrent_MovesCurrentToNext()
        {
            var participants = CreateParticipants(4);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            var snapshot = service.CurrentOrderSnapshot;
            var expectedNext = snapshot[1];

            service.RemoveParticipant(snapshot[0].State.TankId);

            Assert.AreEqual(expectedNext, service.Current);
        }

        [Test]
        public void RemoveParticipant_RemovingLastRemaining_MakesCurrentNull()
        {
            var participants = CreateParticipants(1);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            service.RemoveParticipant(participants[0].FakeState.TankId);

            Assert.IsNull(service.Current);
            Assert.AreEqual(0, service.CurrentOrderSnapshot.Count);
        }

        [Test]
        public void RemoveParticipant_RemovingLastIndex_WrapsCurrentIndexToZero()
        {
            var participants = CreateParticipants(3);
            var service = new TurnOrderService();
            service.Initialize(participants.Cast<ITurnParticipant>().ToList(), new SeededRandomSource(1));

            // Move current to the last index.
            service.Advance();
            service.Advance();
            var lastId = service.Current.State.TankId;
            var expectedWrappedParticipant = service.CurrentOrderSnapshot[0];

            service.RemoveParticipant(lastId);

            Assert.AreEqual(2, service.CurrentOrderSnapshot.Count);
            Assert.AreEqual(expectedWrappedParticipant, service.Current);
        }
    }
}
