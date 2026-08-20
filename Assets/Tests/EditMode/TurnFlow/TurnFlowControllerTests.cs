using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TankBattle.Core.Shared;
using TankBattle.Core.TurnFlow;

namespace TankBattle.Tests.EditMode.TurnFlow
{
    [TestFixture]
    public class TurnFlowControllerTests
    {
        private static TurnOrderService CreateInitializedOrderService(int count)
        {
            var participants = new List<ITurnParticipant>();
            for (int i = 0; i < count; i++)
            {
                participants.Add(new FakeTurnParticipant(new FakeTankState(i)));
            }

            var service = new TurnOrderService();
            service.Initialize(participants, new SeededRandomSource(1));
            return service;
        }

        [Test]
        public void Constructor_WithNullOrderService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TurnFlowController(null, new TurnTimer(30f)));
        }

        [Test]
        public void Constructor_WithNullTimer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TurnFlowController(CreateInitializedOrderService(2), null));
        }

        [Test]
        public void CurrentTurnOwner_DelegatesToOrderService()
        {
            var orderService = CreateInitializedOrderService(3);
            var controller = new TurnFlowController(orderService, new TurnTimer(30f));

            Assert.AreEqual(orderService.Current, controller.CurrentTurnOwner);
        }

        [Test]
        public void BeginTurn_StartsTimerAndRaisesOnTurnStarted()
        {
            var orderService = CreateInitializedOrderService(3);
            var timer = new TurnTimer(30f);
            var controller = new TurnFlowController(orderService, timer);

            ITurnParticipant raised = null;
            controller.OnTurnStarted += p => raised = p;

            timer.Tick(5f);
            controller.BeginTurn();

            Assert.AreEqual(controller.CurrentTurnOwner, raised);
            Assert.AreEqual(30f, timer.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void BeginTurn_WithNoParticipants_Throws()
        {
            var orderService = CreateInitializedOrderService(0);
            var controller = new TurnFlowController(orderService, new TurnTimer(30f));

            Assert.Throws<InvalidOperationException>(() => controller.BeginTurn());
        }

        [Test]
        public void EndTurn_RaisesOnTurnEndedWithReasonAndAdvancesOrder()
        {
            var orderService = CreateInitializedOrderService(3);
            var controller = new TurnFlowController(orderService, new TurnTimer(30f));

            var owner = controller.CurrentTurnOwner;
            (ITurnParticipant participant, TurnEndReason reason)? raised = null;
            controller.OnTurnEnded += (p, r) => raised = (p, r);

            controller.EndTurn(TurnEndReason.Fired);

            Assert.IsTrue(raised.HasValue);
            Assert.AreEqual(owner, raised.Value.participant);
            Assert.AreEqual(TurnEndReason.Fired, raised.Value.reason);
            Assert.AreNotEqual(owner, controller.CurrentTurnOwner);
        }

        [Test]
        public void EndTurn_WithTimedOutReason_StillAdvancesOrder()
        {
            var orderService = CreateInitializedOrderService(2);
            var controller = new TurnFlowController(orderService, new TurnTimer(30f));

            var owner = controller.CurrentTurnOwner;
            controller.EndTurn(TurnEndReason.TimedOut);

            Assert.AreNotEqual(owner, controller.CurrentTurnOwner);
        }
    }
}
