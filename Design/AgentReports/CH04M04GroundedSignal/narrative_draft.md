# Grounded Signal — dialogue draft

Draft for the initial runway-unload / APC-extraction route. Not installed, voiced or player reviewed. Do not use this wording for a selectable airborne route without revising the insertion brief.

## `seq.ch04.m04.brief`

| Line / speaker | English | Conversational Persian |
|---|---|---|
| `grounded-signal-brief-01` / Laila | The military relay sits beside the civilian airfield. Disable the relay, recover its control hardware, and bring both specialists home. The terminal must stay intact. | رلهٔ نظامی کنار فرودگاه غیرنظامیه. رله رو از کار بنداز، تجهیزات کنترلش رو بردار و هر دو متخصص رو سالم برگردون. ترمینال باید سالم بمونه. |
| `grounded-signal-brief-02` / Karim | We'll unload at the apron and leave by APC. Keep the runway clear. Use the service gate to reach the compound, and protect our way back. | توی محوطهٔ توقف پیاده می‌شیم و با نفربر برمی‌گردیم. باند رو باز نگه دار. از درِ خدماتی وارد محوطه شو و مسیر برگشتمون رو حفظ کن. |
| `grounded-signal-brief-03` / Yusuf | I need the control hardware, not rubble. Once the military relay is disabled, get us to the recovery point. We'll verify and secure the device there. | تجهیزات کنترل رو می‌خوام، نه آوارش رو. وقتی رلهٔ نظامی از کار افتاد، ما رو به محل بازیابی برسون. همون‌جا دستگاه رو بررسی و تحویل می‌گیریم. |

## `seq.ch04.m04.comms`

Trigger: both original specialists have actually unloaded alive. Opening camera movement or ARIA guidance does not trigger it.

| Line / speaker | English | Conversational Persian |
|---|---|---|
| `grounded-signal-comms-01` / Yusuf | The interfaces match the Civic Relay specifications. This equipment was prepared for our network. We still need the physical unit to prove it. | اتصال‌ها با مشخصات رلهٔ شهری یکیه. این تجهیزات رو برای شبکهٔ ما آماده کردن. برای اثباتش هنوز باید خودِ دستگاه رو برداریم. |
| `grounded-signal-comms-02` / Laila | Vanguard knows you're there. Keep the specialists together and the service road open. Recover the hardware, board the APC, then reach the guarded exit. | ونگارد فهمیده اونجایین. متخصص‌ها رو کنار هم نگه دار و جادهٔ خدماتی رو باز بذار. تجهیزات رو بردار، سوار نفربر شو و به خروجی محافظت‌شده برس. |

## `seq.ch04.m04.debrief`

Trigger: ordinary validated mission victory with both living specialists, recovered hardware and APC at a secure exit. No success dialogue after loss, abandonment or an incomplete recovery.

| Line / speaker | English | Conversational Persian |
|---|---|---|
| `grounded-signal-debrief-01` / Karim | Both specialists and the hardware are back. The civilian terminal is intact. The service route held long enough to get everyone out. | هر دو متخصص و تجهیزات برگشتن. ترمینال غیرنظامی هم سالمه. مسیر خدماتی تا وقتی همه رو خارج کردیم باز موند. |
| `grounded-signal-debrief-02` / Yusuf | The connectors and control layout fit our Civic Relay. That compatibility was deliberate. They weren't just borrowing an airfield. | اتصال‌ها و چیدمان کنترل با رلهٔ شهری ما جور درمیاد. این سازگاری عمدی بوده. اونا فقط از یه فرودگاه استفاده نمی‌کردن. |
| `grounded-signal-debrief-03` / ARIA | The recovered schedule identifies Vanguard's command group and its next link window. I can mark the approach for your review. You decide when we move. | برنامه‌ای که بازیابی کردیم، گروه فرماندهی ونگارد و زمان اتصال بعدیشون رو مشخص می‌کنه. می‌تونم مسیر نزدیک شدن رو برای بررسی نشون بدم. زمان حرکت رو شما تعیین می‌کنین. |

## Guidance intent

- Unload: select transport → existing passenger drawer → Exit All.
- Recovery: existing Move command takes the specialists to the hardware point; a visible hold shows progress and cancels if they leave or board.
- Extraction: existing Board interaction → Move APC to guarded exit. Keep both specialists aboard during the secure hold.
- ARIA Play/Stop remains visible; Show Me moves only the camera.

Final copy must be checked against the actual authored terminal, service route, unit names and objective triggers before installation or voice generation. No Grounded Signal lines are included in the separately approved 34 recordings for existing missions.
