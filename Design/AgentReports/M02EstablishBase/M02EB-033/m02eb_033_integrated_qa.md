# M02EB-033 Integrated QA

Date: 2026-08-29
Status: Passed

## Results

| Gate | Result |
|---|---|
| All M02 Establish Base EditMode tests | 246/246 passed, 0 failed |
| Final M02 narrative/comic/voice smoke | 22/22 passed, 0 failed |
| Canonical M01 consolidated contracts | 23/23 suites passed twice |
| Shared Match HUD / ARIA tests | 21/21 passed, including zero-allocation stable application |
| Production source-growth architecture | 17/17 passed |
| Shared operation-map producer queue | 3/3 passed |
| Shared quantity-aware queue consumer | 4/4 passed |
| Shared producer spawn transaction | 3/3 passed |
| Shared production scheduler | 6/6 passed |
| Shared camp production bridge | 6/6 passed |
| Shared production metadata/source state/request | 4/4, 1/1, and 31/31 passed |
| C# project compilation | Succeeded with 0 errors; 36 existing assembly-version warnings |
| Final Unity console error scan | 0 errors/exceptions/asserts after clean smoke |
| Source diff whitespace validation | Passed; Unity-generated `.asset` and `.meta` empty fields retain serializer-owned trailing spaces |

The all-M2 result is preserved at `/private/tmp/warline-m02-final-246.json` for the current machine session. The final 246/246 run covers the authored military-base map binding, comic-before-HUD ordering, tutorial DO IT routes, English/Persian text and voice, Barracks placement, one-order four-soldier production, delayed wave, objectives, settlement, cleanup, and warm placement-policy zero managed allocation.

Two exploratory broad NUnit runs exposed pre-existing runner-fixture behavior: direct M01 settlement tests omit their private fixture, and two gameplay-composition tests assume a default World. Canonical focused owners were then run instead; all affected M1 and shared production boundaries passed. These exploratory results are not product failures and are not used as acceptance evidence.

Android/Samsung validation remains explicitly deferred by the project owner and is not claimed as passed.
