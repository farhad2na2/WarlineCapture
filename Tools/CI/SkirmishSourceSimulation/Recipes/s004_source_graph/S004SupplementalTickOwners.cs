using System;
using Game.Runtime;
using Unity.Entities;

// Source-only tick subset. Native UI, presentation, and other expanded owners
// remain outside this diagnostic and therefore cannot establish readiness.
internal sealed class S004SupplementalTickOwners : IDisposable
{
    private readonly EntityManager em;
    private UnitAirMovementSystem air; private SystemState airState;
    private UnitTransportDeployOrderSystem deploy; private SystemState deployState;
    private UnitTransportBoardingSystem boarding; private SystemState boardingState;
    private SupportAbilityRequestSystem supportRequest; private SystemState requestState;
    private SupportFlightSystem supportFlight; private SystemState flightState;
    private SupportSmokeSystem smoke; private SystemState smokeState;
    private UnitTransportAirdropSystem airdrop; private SystemState airdropState;
    private UnitTransportDeployAttackSystem deployAttack; private SystemState attackState;
    private UnitAttackOrderRequestSystem attackRequests; private SystemState attackRequestState;
    private SkirmishWorldMovementSystem expandedMovement; private SystemState expandedMovementState;
    private bool disposed;
    private SystemState State() => new SystemState { EntityManager = em, Enabled = true };
    internal S004SupplementalTickOwners(EntityManager manager)
    {
        em = manager ?? throw new ArgumentNullException(nameof(manager));
        airState = State(); airState.Begin(); air.OnCreate(ref airState); airState.End();
        deployState = State(); deployState.Begin(); deploy.OnCreate(ref deployState); deployState.End();
        boardingState = State(); boardingState.Begin(); boarding.OnCreate(ref boardingState); boardingState.End();
        requestState = State(); requestState.Begin(); supportRequest.OnCreate(ref requestState); requestState.End();
        flightState = State();
        smokeState = State(); smokeState.Begin(); smoke.OnCreate(ref smokeState); smokeState.End();
        airdropState = State(); airdropState.Begin(); airdrop.OnCreate(ref airdropState); airdropState.End();
        attackState = State(); attackState.Begin(); deployAttack.OnCreate(ref attackState); attackState.End();
        attackRequestState = State(); attackRequestState.Begin(); attackRequests.OnCreate(ref attackRequestState); attackRequestState.End();
        expandedMovementState = State(); expandedMovementState.Begin(); expandedMovement.OnCreate(ref expandedMovementState); expandedMovementState.End();
    }
    internal void Air() { airState.Begin(); try { if (airState.Enabled && airState.RequiredQueriesPresent) air.OnUpdate(ref airState); } finally { airState.End(); } }
    internal void Deploy() { deployState.Begin(); try { if (deployState.Enabled && deployState.RequiredQueriesPresent) deploy.OnUpdate(ref deployState); } finally { deployState.End(); } }
    internal void Boarding() { boardingState.Begin(); try { if (boardingState.Enabled && boardingState.RequiredQueriesPresent) boarding.OnUpdate(ref boardingState); } finally { boardingState.End(); } }
    internal void SupportRequest() { requestState.Begin(); try { if (requestState.Enabled && requestState.RequiredQueriesPresent) supportRequest.OnUpdate(ref requestState); } finally { requestState.End(); } }
    internal void SupportFlight() { flightState.Begin(); try { if (flightState.Enabled && flightState.RequiredQueriesPresent) supportFlight.OnUpdate(ref flightState); } finally { flightState.End(); } }
    internal void Smoke() { smokeState.Begin(); try { if (smokeState.Enabled && smokeState.RequiredQueriesPresent) smoke.OnUpdate(ref smokeState); } finally { smokeState.End(); } }
    internal void Airdrop() { airdropState.Begin(); try { if (airdropState.Enabled && airdropState.RequiredQueriesPresent) airdrop.OnUpdate(ref airdropState); } finally { airdropState.End(); } }
    internal void DeployAttack() { attackState.Begin(); try { if (attackState.Enabled && attackState.RequiredQueriesPresent) deployAttack.OnUpdate(ref attackState); } finally { attackState.End(); } }
    internal void AttackRequests() { attackRequestState.Begin(); try { if (attackRequestState.Enabled && attackRequestState.RequiredQueriesPresent) attackRequests.OnUpdate(ref attackRequestState); } finally { attackRequestState.End(); } }
    internal void ExpandedMovement() { expandedMovementState.Begin(); try { if (expandedMovementState.Enabled && expandedMovementState.RequiredQueriesPresent) expandedMovement.OnUpdate(ref expandedMovementState); } finally { expandedMovementState.End(); } }
    public void Dispose() { if (disposed) return; smokeState.Begin(); try { smoke.OnDestroy(ref smokeState); } finally { smokeState.End(); disposed = true; } }
}
