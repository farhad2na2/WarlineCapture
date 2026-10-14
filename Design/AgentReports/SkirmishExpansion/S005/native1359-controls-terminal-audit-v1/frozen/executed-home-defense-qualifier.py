"""Strict external public Touch strategy proof; no fabricated Hold receipt."""
def qualify(rows,attempt):
 if not rows: raise ValueError("Missing native home-defense rows")
 if any(r.get("passed") is not True or r.get("stage")=="Failure" for r in rows): raise ValueError("Native defense failure")
 active=[r for r in rows if r.get("attempt")]
 if not active or any(r["attempt"]!=attempt for r in active): raise ValueError("Foreign/absent native attempt")
 if any(r["unexpected"] or r["interventions"] or r["violations"] for r in rows): raise ValueError("Defense input/audit violation")
 if any(abs(r["timeScale"]-1)>.0001 for r in active if r["stage"] not in ("Closed","ScheduleRestored")): raise ValueError("Defense time scale changed")
 bound=[r for r in rows if r["stage"]=="BoundPublicHomeDefender"]
 held=[r for r in rows if r["stage"]=="NativeHomeHeld"]
 skipped=[r for r in rows if r["stage"]=="ReservedDefenderSkippedOffensiveSweep"]
 if not bound or not held or not skipped: raise ValueError("Actual public defender bind/Move/Hold/reservation missing")
 lease=False
 for r in rows:
  if r["stage"]=="LeaseStarted":
   if lease:raise ValueError("Overlapping defense lease")
   lease=True
  if r["stage"]=="ScheduleRestored":
   if not lease:raise ValueError("Unmatched restored defense lease")
   lease=False
 if lease:raise ValueError("Unclosed defense lease")
 for h in held:
  if not h["group"] or not h["defender"] or h["members"]<=0 or not h["selected"] or not h["held"]: raise ValueError("Hold must conserve actual exclusive living defender cohort")
  accepted=[r for r in rows if r["stage"]=="AcceptedDefenderCommand" and r["reason"]=="HomeMove" and r["group"]==h["group"] and r["frame"]<h["frame"]]
  if not accepted: raise ValueError("Native accepted home Move missing")
  if not any(r["stage"]=="PublicHoldTouch" and r["group"]==h["group"] and accepted[-1]["frame"]<r["frame"]<h["frame"] for r in rows): raise ValueError("Held cohort lacks genuine public Hold control Touch")
 for a in [r for r in rows if r["stage"]=="AcceptedDefenderCommand"]:
  touch=[r for r in rows if r["stage"]=="PublicWorldTouch" and r["group"]==a["group"] and r["queuedFrame"]==a["queuedFrame"] and r["frame"]<=a["frame"]]
  receipts=[r for r in rows if r["stage"]=="NativeReceipt" and r["group"]==a["group"] and r["receipt"]==a["receipt"] and a["queuedFrame"]<=r["frame"]<=a["frame"]]
  if not a["receipt"] or not touch or not receipts or not a["selected"] or a["members"]<=0: raise ValueError("Accepted defender command lacks paired native input/current selected cohort")
  if a["reason"]=="VisibleHomeThreatAttack" and (not a["target"] or any(not r["visible"] or r["target"]!=a["target"] for r in touch)): raise ValueError("Attack must target actual currently visible home threat")
 for a in [r for r in rows if r["stage"]=="AcceptedDefenderCommand" and r["reason"]=="VisibleHomeThreatAttack"]:
  same=lambda r:r["attempt"]==attempt and r["group"]==a["group"] and r["receipt"]==a["receipt"] and r["queuedFrame"]==a["queuedFrame"] and r["target"]==a["target"] and r["defender"]==a["defender"]
  advances=[r for r in rows if same(r) and r["stage"]=="NativeReceipt" and r["reason"]=="AttackMove"]
  bindings=[r for r in rows if same(r) and r["stage"]=="NativeLaterVisibleTargetBinding" and r["frame"]>a["queuedFrame"] and r["visible"] and r["exactEngaged"] and (r["commanded"] or advances) and r["sourceIndex"]>0 and r["targetIndex"]>0]
  effects=[r for r in rows if same(r) and r["stage"]=="NativeQualifiedDefenderEffect" and r["qualified"] and r["sourceKind"]=="DirectFire" and r["damageApplied"]>0 and r["eventId"]>0 and r["eventFrame"]>=a["frame"] and r["shotCounter"]>r["baselineShotCounter"] and r["bindingFrame"]==r["eventFrame"]-1]
  if not bindings or not effects:raise ValueError("Accepted defense requires later actual visible exact-target binding and native attributed positive effect")
  for e in effects:
   before=[r for r in rows if same(r) and r["stage"]=="DefenseFrame" and r["frame"]==e["bindingFrame"] and r["visible"] and r["exactEngaged"] and (r["commanded"] or advances)]
   impact=[r for r in rows if same(r) and r["stage"]=="DefenseFrame" and r["frame"]==e["eventFrame"] and r["shotCounter"]>r["baselineShotCounter"]]
   if len(before)!=1 or len(impact)!=1 or not any(b["frame"]<=e["eventFrame"] for b in bindings):raise ValueError("Actual attributed effect lacks exact native current/prior frame census")
   for f in before+impact:
    if (f["sourceIndex"],f["sourceVersion"],f["targetIndex"],f["targetVersion"])!=(e["sourceIndex"],e["sourceVersion"],e["targetIndex"],e["targetVersion"]):raise ValueError("Effect source/target native entity versions differ from exact frame census")
 for r in [r for r in rows if r["stage"]=="ScheduleRestored"]:
  if not r["restoredExact"] or r["restoredDue"]!=r["originalDue"] or r["completed"]!=r["dispatched"]: raise ValueError("Defense scheduler/Touch not exactly conserved")
 closed=[r for r in rows if r["stage"]=="Closed"]
 if len(closed)!=1 or not closed[0]["restoredExact"]: raise ValueError("Defense ownership unclosed")
 # Every defense Attack must have an actual flat pre/post public native-input census.
 attacks=[r for r in rows if r["stage"]=="PublicWorldTouch" and r["reason"]=="VisibleHomeThreat"]
 for a in attacks:
  before=[r for r in rows if r["stage"]=="BeforePublicAttackTouch" and r["frame"]==a["queuedFrame"] and r["attackAttempt"]==a["attackAttempt"] and r["target"]==a["target"] and r["group"]==a["group"]]
  after=[r for r in rows if r["stage"]=="AfterPairedPublicAttackTouch" and r["queuedFrame"]==a["queuedFrame"] and r["attackAttempt"]==a["attackAttempt"] and r["group"]==a["group"] and r["frame"]>a["queuedFrame"]]
  if len(before)!=1 or len(after)!=1 or not before[0]["visible"] or not before[0]["selected"] or before[0]["commandMode"]!="Attack" or not before[0]["safeArea"] or not before[0]["projectionValid"] or before[0]["uiRaycasts"] or after[0]["busy"] or after[0]["completed"]!=after[0]["dispatched"] or after[0]["completed"]<=a["completed"]:raise ValueError("Defense Attack lacks exact actual pre/post paired public-input census")
  if not(1<=a["attackAttempt"]<=3):raise ValueError("Unbounded defense Attack attempts")
 for retry in [r for r in rows if r["stage"]=="NoReceiptRetry"]:
  match=[r for r in rows if r["stage"]=="NoReceiptDecision" and r["queuedFrame"]==retry["queuedFrame"] and r["attackAttempt"]==retry["attackAttempt"] and r["frame"]==retry["frame"]]
  prior=[a for a in attacks if a["queuedFrame"]==retry["queuedFrame"] and a["attackAttempt"]==retry["attackAttempt"]]
  if len(match)!=1 or len(prior)!=1:raise ValueError("Retry lacks exact native decision/prior Attack")
  d=match[0];a=prior[0]
  if any(r["stage"]=="NativeSelectionResult" and r["queuedFrame"]==retry["queuedFrame"] and r["attackAttempt"]==retry["attackAttempt"] and r["commandResultPresent"] and r["commandResultAccepted"] and r["nativeKind"] in ("Attack","Move") and a["queuedFrame"]<=r["resultFrame"]<=d["frame"] for r in rows):raise ValueError("Native accepted result forbids duplicate retry despite absent audit receipt")
  if d["receipt"] or d["acceptedResultCount"] or d["pendingSelection"] or d["pendingAttack"] or d["busy"] or d["completed"]!=d["dispatched"] or d["completed"]<=a["completed"] or d["frame"]<a["queuedFrame"]+4 or d["elapsed"]<a["elapsed"]+2 or d["attackAttempt"]>=3:raise ValueError("Retry duplicated pending/accepted/unclosed or insufficiently advanced native command")
 if attacks and not any(r["stage"]=="NativeQualifiedDefenderEffect" and r["qualified"] for r in rows):raise ValueError("Actual attempted home defense did not establish positive native attributed effect")
 return {"result":"PassedScopedPublicHomeDefense","boundGroups":sorted({r["group"] for r in bound}),"actualMoveHoldCycles":len(held),"visibleHomeAttackCommands":sum(r["stage"]=="AcceptedDefenderCommand" and r["reason"]=="VisibleHomeThreatAttack" for r in rows),"actualPositiveDefenderEffects":sum(r["stage"]=="NativeQualifiedDefenderEffect" and r["qualified"] for r in rows),"automaticAttackMoveTargetAcquisitionAllowed":True,"holdSelectionOrderReceiptRequired":False,"nativeExecuted":True,"gameplayInjected":False,"terminalVictoryAndReturnQualifiedByParentOnly":True}
