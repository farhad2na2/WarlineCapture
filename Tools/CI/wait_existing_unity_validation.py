"""Wait for an owned live-Editor validation without spawning polling processes."""

import pathlib
import sys
import time


def wait_for_completion(log, project, method, timeout, dispatch_status, poll_interval=1):
    if timeout <= 0:
        print("[UnityInvokeMac] ERROR: live validation timeout exhausted; existing Editor preserved.", file=sys.stderr)
        return 124
    log = pathlib.Path(log)
    receipt = pathlib.Path(str(log) + ".exit")
    expected = "[ExistingEditorValidation] project=" + str(pathlib.Path(project).resolve() / "Assets") + " executeMethod=" + method
    started = time.monotonic()
    deadline = started + timeout
    ownership_deadline = min(deadline, started + 60)
    owned = False
    while True:
        if time.monotonic() >= deadline:
            print("[UnityInvokeMac] ERROR: live validation timed out without a completion receipt; existing Editor preserved.", file=sys.stderr)
            return 124
        try:
            if not owned and log.exists():
                with log.open(encoding="utf-8") as stream:
                    header = stream.readline().rstrip("\r\n")
                if header:
                    observed_at = time.monotonic()
                    if observed_at >= deadline or observed_at >= ownership_deadline:
                        print("[UnityInvokeMac] ERROR: validation ownership deadline expired; existing Editor preserved.", file=sys.stderr)
                        return 124
                    if header != expected:
                        print("[UnityInvokeMac] ERROR: validation log ownership mismatch; existing Editor preserved.", file=sys.stderr)
                        return dispatch_status or 1
                    owned = True
                    if dispatch_status:
                        print("[UnityInvokeMac] Pipeline acknowledgement failed (status " + str(dispatch_status) +
                              "); exact validation log confirms ownership. Completion receipt remains required.")
            if owned and receipt.exists():
                status = receipt.read_text(encoding="utf-8").strip()
                if time.monotonic() >= deadline:
                    print("[UnityInvokeMac] ERROR: completion receipt arrived after timeout; existing Editor preserved.", file=sys.stderr)
                    return 124
                if status == "0":
                    return 0
                if status:
                    print("[UnityInvokeMac] ERROR: live validation failed with status " + repr(status) + ".", file=sys.stderr)
                    return 1
        except OSError as error:
            print("[UnityInvokeMac] ERROR: cannot read validation evidence: " + str(error), file=sys.stderr)
            return 1
        now = time.monotonic()
        if not owned and now >= ownership_deadline:
            print("[UnityInvokeMac] ERROR: no matching validation ownership; existing Editor preserved.", file=sys.stderr)
            return dispatch_status or 124
        if now >= deadline:
            print("[UnityInvokeMac] ERROR: live validation timed out without a completion receipt; existing Editor preserved.", file=sys.stderr)
            return 124
        time.sleep(min(poll_interval, deadline - now, ownership_deadline - now if not owned else deadline - now))


if __name__ == "__main__":
    sys.exit(wait_for_completion(sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4]), int(sys.argv[5])))
