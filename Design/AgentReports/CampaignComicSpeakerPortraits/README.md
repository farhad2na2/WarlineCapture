# Dedicated campaign comic speaker portraits

Replaced gameplay unit portrait bindings for Nadir Qassem, Warrant Officer Karim Daher and Chief Yusuf Darzi with dedicated transparent, chest-up comic illustrations. Existing speaker IDs, text, audio and presentation treatment are preserved. Other speakers already had narrative-specific art and were retained.

Assets: `Assets/Game/Art/UI/Portraits/Generated/Portrait_{Qassem,Karim,Yusuf}_CampaignComic.png` and Unity-generated metadata. PNG sources retain generated RGBA transparency; native sprite imports use a 1024px maximum, clamp wrapping, no mipmaps and no CPU readability.

Generated with built-in ImageGen, using Dalia for style, Lina for transparent portrait composition, and each original gameplay portrait for character identity. Exact prompts are in prompts.json. Original generated files remain in the Codex generated_images library.

Shared FirstLaunchSpeakers catalog now points to the three dedicated sprites. Route Reopened and Grounded Signal authoring builders use the new paths so mission rebuilds retain them.

Validation: the approved macOS repository wrapper ran CampaignComicSpeakerPortraitBuilder.InstallAndValidate successfully. Native catalog/sequence coverage found 14 affected campaign dialogue lines, three speakers and zero unit portrait bindings for those speakers. Six actual prefab renders passed for English and Farsi; portrait visibility and caption overflow were checked. Active scene and dirty state were preserved. Images were visually inspected, including Farsi avatar-right/Next-left layout. Full log and wrapper receipts are retained, along with the initial missing-method and corrected compile failure evidence.

Full normal-input mission playback and device acceptance were not run; these checks establish art integration and native presentation, not mission readiness.

## Corrected identities and Persian names

The original avatar drafts incorrectly used gameplay characters as identity references. Final portraits instead match authored Grounded Signal and Command Node/Split Front comic panels: Karim is the left man in tan field gear, sunglasses and headset; Yusuf is the center man with curly hair, sunglasses and patterned scarf; Qassem has gray temples, a trimmed salt-and-pepper beard and tan uniform with red insignia. Corrected prompt provenance is in corrected-prompts.json; superseded originals remain in the Codex generation library and temporary backup.

Added canonical Persian speaker names and roles to both the narrative locale and shared UI localization catalog: نادر قاسم، کریم ظاهر، یوسف درزی. CampaignComicSpeakerIdentityCopy is consumed by the full V3 localization rebuild and focused installer. Native validation asserts exact Persian names through the runtime resolver, checks Persian role text and renders the actual dialogue view.

The Split Front comms panel now has Dalia's left hand with four raised fingers and the thumb on image-left. The first edit put the thumb on the wrong side and was replaced after owner correction. The final panel retains its original dimensions and byte-identical importer metadata, including authored sprite slices/IDs. Native captures cover the corrected panel in both languages. Art/identity integration does not change voices or mission logic.
