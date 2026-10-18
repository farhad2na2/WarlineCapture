#!/bin/sh
# Disabled review artifact. This script never launches Unity.
printf "%s\n" "S026 plan disabled: missing survey, unsupported raw-pin bridge, stale API and incomplete CC service/profile fit." >&2
exit 64
