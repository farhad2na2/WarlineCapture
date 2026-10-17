"""Audit recorded native contacts; terminal disposal is allowed only with explicit evidence."""
import collections
import hashlib
import json
import sys
from pathlib import Path


def audit(rows):
    contacts = collections.defaultdict(list)
    drivers = {}
    errors = []
    phases = collections.Counter()
    completed = collections.Counter()
    cancellations = []
    for row in rows:
        driver = (row["device"], row["generation"])
        drivers[driver] = max(drivers.get(driver, 0), row.get("completed", 0))
        if row.get("unexpected", 0) or row.get("interventions", 0) or (row.get("commandAuditActive") is True and row.get("receiptViolations", 0)):
            errors.append({"driver": driver, "reason": "unexpected input, intervention or receipt violation"})
        if row.get("phase") in ("Runtime", "Manual", "PlayerResume") and row.get("commandAuditActive") is not True:
            errors.append({"driver": driver, "reason": "Runtime contact lacks active command audit"})
        if row["kind"] in ("UnexpectedSample", "PhysicalTakeover"):
            errors.append({"driver": driver, "reason": row["kind"]})
        if row["kind"] in ("Dispatched", "Began", "Ended", "Canceled"):
            contacts[(row["device"], row["generation"], row["dispatched"])].append(row)
    for contact, events in contacts.items():
        kinds = [row["kind"] for row in events]
        phases[events[0].get("phase", "Unattributed")] += 1
        if kinds == ["Dispatched", "Began", "Ended"]:
            completed[contact[:2]] += 1
        elif kinds == ["Dispatched", "Began", "Canceled"]:
            began, canceled = events[1:]
            valid = (began.get("commandAuditActive") is True
                     and canceled.get("commandAuditActive") is True
                     and canceled.get("matchFinished") is True
                     and canceled.get("phase") in ("Runtime", "Manual", "PlayerResume")
                     and "acceptedCommands" in began and "acceptedCommands" in canceled
                     and began["acceptedCommands"] == canceled["acceptedCommands"]
                     and canceled.get("receiptViolations") == 0)
            if valid:
                cancellations.append({"contact": contact, "frame": canceled["frame"],
                                      "acceptedCommands": canceled["acceptedCommands"],
                                      "reason": "Native Finished disposal; no command accepted during contact"})
            else:
                errors.append({"contact": contact, "kinds": kinds, "reason": "Cancellation lacks explicit terminal/no-command evidence"})
        else:
            errors.append({"contact": contact, "kinds": kinds})
    if len(cancellations) > 1:
        errors.append({"reason": "More than one terminal cancellation"})
    if cancellations and phases["Terminal"] == 0:
        errors.append({"reason": "Terminal cancellation without recorded result-return contact"})
    for driver, count in drivers.items():
        if count != completed[driver]:
            errors.append({"driver": driver, "completed": count, "recordedEnded": completed[driver]})
    errors = list({json.dumps(error, sort_keys=True): error for error in errors}.values())
    return dict(recordedContacts=len(contacts), completedContacts=sum(completed.values()),
                phases=dict(phases), terminalCancellations=cancellations, errors=errors,
                strictRecordedContacts=bool(rows) and not errors,
                strictAllContactsEnded=bool(rows) and not errors and not cancellations,
                nativeMenuContactsRecorded=phases["Menu"] > 0,
                terminalContactsRecorded=phases["Terminal"] > 0,
                completeMatchCertified=False,
                note="Input pairing only. At most one native Finished cancellation with explicit no-command evidence; outcome, result-return, source identity and wrapper pass are separate gates.")


if __name__ == "__main__":
    path = Path(sys.argv[1])
    result = audit([json.loads(line) for line in path.read_text().splitlines()])
    result.update(trace=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    print(json.dumps(result, indent=2))
