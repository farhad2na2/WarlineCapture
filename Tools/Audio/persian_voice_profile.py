"""Approved conversational Iranian Persian delivery, shared by offline generators."""
PROFILE_ID = 'fa-conversational-v1'
VOICE_SETTINGS = {'stability': 0.5, 'similarity_boost': 0.75}
DIRECTION = '[conversational tone]'


def apply(body: dict, language: str) -> dict:
    if language == 'fa':
        text = body['text']
        # Replace old emotional/narrative direction; never place tags in captions.
        import re
        text = re.sub(r'\[[^\]]+\]\s*', '', text).strip()
        body['text'] = f'{DIRECTION} {text}'
        body['voice_settings'] = dict(VOICE_SETTINGS)
    return body


def metadata(language: str) -> dict:
    return {'deliveryProfile': PROFILE_ID, 'voiceSettings': dict(VOICE_SETTINGS), 'performanceDirection': DIRECTION} if language == 'fa' else {}


def clip_matches(record: dict, text: str, path, language: str) -> bool:
    import hashlib
    return (path.exists() and record.get('text') == text
            and record.get('sha256') == hashlib.sha256(path.read_bytes()).hexdigest()
            and (language != 'fa' or record.get('deliveryProfile') == PROFILE_ID))
