# Main menu A / C implementation

Approved direction: A during Campaign; C after the complete Campaign. The existing Campaign completion/ready projection controls the switch. Partial registration or an incomplete Campaign retains A. Full completion presents chapter replay and Story Archive above two enlarged mode cards.

## Live data and navigation

- Skirmish progress counts distinct valid catalog missions with default difficulty medals. The denominator is the actual 120-entry catalog. The next recommendation is the first accessible published mission without a medal, with replay when all released missions are cleared. Planned missions are excluded. Activating the card opens that mission, and the selection is also applied to the actual launch configuration.
- Operations progress counts distinct known first-clear IDs against the 60-entry catalog. Only the current published O001 tactical adapter is recommended. A detached strategic session exercises the same eligibility reducer without writing AP, run, rewards or saves. Pending eligible attempts take precedence. Replays are labeled explicitly.
- Artwork resolves through the same serialized mission-art catalog used by mission selection. S001–S004 reuse the existing approved mission illustrations. Street Signals uses a new 2D illustration based on the actual O001 environment render, with prompt and references recorded in ArtworkProvenance.json. It is mission artwork, not a gameplay screenshot or a new physical map.
- Four existing faceted difficulty stars show Recruit, Regular, Veteran and Commander totals. Newly accepted non-practice Operations victories persist the earned tier atomically with the result; legacy clears do not fabricate historical difficulty awards. Repeated settlements and stale writes do not duplicate medals.
- Mode rank derives from distinct completed missions: Cadet, then Field Officer at 5, Tactician at 15, Veteran at 30, Commander at 60. This is separate from commander XP.

## Presentation

The native prefab owns A/C layout, mission imagery, colored Operations/Skirmish accents, progress bars, medal rows and action buttons. Existing logo, credits, settings, ARIA, commander, Store and Armory remain in the shared frame. English/Persian layout uses the existing fonts and Arabic line-box fitting, with mirrored insets and contained text. Screenshots use isolated fixture profiles so the actual player profile is untouched.

## Evidence boundaries

Visual review, automated projection/persistence checks, native rendering, and normal touch menu navigation are recorded separately in REVIEW_STATUS.json. The saved-state fixtures do not establish a combat win or mission readiness. Real player/device acceptance remains pending.

The existing O001 objective/source-fit load gate is documented in ../ResultScreenReview20261021/Implementation/OPERATIONS_LOAD_GATE.md. This UI feature does not change mission geometry, objectives, budgets or aircraft. A complete normal-input mission/ARIA/result/return journey is not claimed here.

Unrelated font and importer changes in the shared checkout are excluded from this candidate. Validation-only dependency stamp refreshes retain all original strict Play-entry guards and must be restored byte-for-byte before staging.

The requested button/banner polish is recorded in POLISH_NOTES.md. The completion strip now uses a dedicated proportionally cropped epilogue panorama with no separate thumbnail; EndingArtworkProvenance.json records its built-in ImageGen prompt and final asset path.
