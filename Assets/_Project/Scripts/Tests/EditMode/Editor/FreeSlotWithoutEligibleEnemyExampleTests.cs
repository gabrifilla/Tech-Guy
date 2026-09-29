using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for "a freed slot with no eligible enemy stays free" — task 6.4 of
    /// weapon-gameplay-swarm-rework (Requisito 5.5).
    ///
    /// R5.5: when a slot frees up (a holder is interrupted / stunned / stance-broken) and there is no
    /// waiting enemy to take it, the slot must simply stay free. The system must never fabricate a
    /// holder, and — crucially — must not pull any enemy out of its preferred distance to fill the
    /// gap.
    ///
    /// The pure, scene-free <see cref="AttackSlotPool{TEnemy}"/> encodes this by construction:
    /// <c>Release</c> promotes only from the explicit waiting queue and returns <c>null</c> when that
    /// queue is empty; the pool holds no notion of position, so it can neither invent a holder nor
    /// force anyone in. These examples pin that behavior directly:
    ///  - releasing a holder from a full pool with an empty queue returns null and leaves the slot free,
    ///  - the active count decreases by exactly one and no phantom holder appears, and
    ///  - a non-waiting bystander is never dragged into the freed slot.
    ///
    /// The <see cref="SwarmAttackCoordinator"/> that drives locomotion/telegraphs is a
    /// <see cref="UnityEngine.MonoBehaviour"/> requiring a live scene, so per the design these example
    /// tests target the pure pool where the R5.5 invariant actually lives.
    /// </summary>
    public sealed class FreeSlotWithoutEligibleEnemyExampleTests
    {
        // A minimal reference-type stand-in for an enemy. The pool keys enemies by reference identity,
        // so any distinct class instance is a valid, unambiguous token holder.
        private sealed class FakeEnemy
        {
            public FakeEnemy(string name) => Name = name;
            public string Name { get; }
            public override string ToString() => Name;
        }

        // Feature: weapon-gameplay-swarm-rework, R5.5 example — releasing the only holder with no
        // waiter leaves the slot free (Release returns null, count drops, nothing is fabricated).
        // Validates: Requirement 5.5
        [Test]
        public void ReleasingHolderWithNoWaiter_LeavesSlotFree()
        {
            var pool = new AttackSlotPool<FakeEnemy>(new AttackSlotConfig(1));
            var attacker = new FakeEnemy("attacker");

            Assert.IsTrue(pool.TryAcquire(attacker), "the single slot should be granted to the first requester");
            Assert.IsTrue(pool.IsFull, "a capacity-1 pool with one holder must report full");
            Assert.AreEqual(1, pool.ActiveCount);

            FakeEnemy promoted = pool.Release(attacker);

            Assert.IsNull(promoted, "with no waiting enemy, Release must not promote (or fabricate) anyone (R5.5)");
            Assert.AreEqual(0, pool.ActiveCount, "the freed slot must actually be released, not silently refilled");
            Assert.IsFalse(pool.IsFull, "the slot must stay free when no eligible enemy is waiting (R5.5)");
            Assert.IsFalse(pool.HoldsToken(attacker), "the released enemy must no longer hold a token");
            Assert.AreEqual(0, pool.WaitingCount, "no enemy should have been invented into the waiting queue");
        }

        // Feature: weapon-gameplay-swarm-rework, R5.5 example — a full multi-slot pool that frees one
        // slot with an empty queue keeps that slot open rather than topping itself back up.
        // Validates: Requirement 5.5
        [Test]
        public void ReleasingOneOfSeveralHolders_WithNoWaiter_KeepsFreedSlotOpen()
        {
            var pool = new AttackSlotPool<FakeEnemy>(new AttackSlotConfig(3));
            var a = new FakeEnemy("a");
            var b = new FakeEnemy("b");
            var c = new FakeEnemy("c");

            Assert.IsTrue(pool.TryAcquire(a));
            Assert.IsTrue(pool.TryAcquire(b));
            Assert.IsTrue(pool.TryAcquire(c));
            Assert.IsTrue(pool.IsFull, "three holders in a capacity-3 pool must report full");

            FakeEnemy promoted = pool.Release(b);

            Assert.IsNull(promoted, "no one is waiting, so nothing is promoted into the freed slot (R5.5)");
            Assert.AreEqual(2, pool.ActiveCount, "exactly one slot must free up, and it must stay free");
            Assert.IsFalse(pool.IsFull, "the pool must have room after releasing with no waiter");
            Assert.IsFalse(pool.HoldsToken(b), "the released holder must be gone");
            Assert.IsTrue(pool.HoldsToken(a), "unrelated holders must be untouched");
            Assert.IsTrue(pool.HoldsToken(c), "unrelated holders must be untouched");
        }

        // Feature: weapon-gameplay-swarm-rework, R5.5 example — an enemy that never requested a token
        // (a bystander at its preferred distance) is never dragged into a freed slot.
        // Validates: Requirement 5.5
        [Test]
        public void FreedSlot_DoesNotPullInANonWaitingBystander()
        {
            var pool = new AttackSlotPool<FakeEnemy>(new AttackSlotConfig(1));
            var attacker = new FakeEnemy("attacker");
            var bystander = new FakeEnemy("bystander"); // never enqueued: it is holding its preferred distance

            Assert.IsTrue(pool.TryAcquire(attacker));

            FakeEnemy promoted = pool.Release(attacker);

            Assert.IsNull(promoted, "the freed slot must stay free (R5.5)");
            Assert.IsFalse(pool.HoldsToken(bystander), "a bystander that never asked to attack must not be forced in (R5.5)");
            Assert.IsFalse(pool.IsWaiting(bystander), "the bystander must not appear in the waiting queue");
            Assert.AreEqual(0, pool.ActiveCount, "no phantom holder may occupy the freed slot");
        }
    }
}
