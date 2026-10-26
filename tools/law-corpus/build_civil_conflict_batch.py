#!/usr/bin/env python3
"""Replay a fixed original/2005 batch with previously recorded image decisions."""
from copy import deepcopy
import pipeline
import consolidate
from scope_inventory import number_key


def correct_transcription(version, decision):
    if version['versionId'] != decision['versionId'] or pipeline.hash_text(version['text']) != decision['beforeTextSha256']:
        raise ValueError('Transcription correction refuses the wrong historical text')
    updated = deepcopy(version)
    replacement = decision['replacement']
    if set(replacement) - {'text','paragraphs','evidenceIds','imageReview','verification'}:
        raise ValueError('A transcription correction cannot alter legal identity or temporal metadata')
    if replacement['text'] != '\n'.join(replacement['paragraphs']):
        raise ValueError('Correction paragraphs do not reconstruct exact text')
    updated.update(deepcopy(replacement))
    return updated


def main():
    research = pipeline.ROOT / 'data/law/corpus/research'
    initial = pipeline.read_json(research / 'civil-2005-conflict-initial.json')
    plan = pipeline.read_json(research / 'civil-2005-conflict-amendments.json')
    if pipeline.hash_json(initial) != plan['initialSpecSha256']:
        raise ValueError('Initial historical snapshot changed')
    ledger = pipeline.read_json(pipeline.ROOT / 'data/law/corpus/civil-code/ledger.json')
    ledger['scope']['complete'] = False
    ledger['scope']['targetArticles'] = sorted(set(ledger['scope']['targetArticles']) | {op['number'] for op in plan['operations']}, key=number_key)
    spec, audit = consolidate.apply_plan(initial, ledger, plan)
    decisions = pipeline.read_json(research / 'civil-2005-conflict-image-decisions.json')
    correction = decisions['transcriptionCorrection']
    for article in spec['articles']:
        for index, version in enumerate(article['versions']):
            if version['versionId'] == correction['versionId']:
                article['versions'][index] = correct_transcription(version, correction)
    by_id = {v['versionId']:v for a in spec['articles'] for v in a['versions']}
    for decision in decisions['versions']:
        version = by_id[decision['versionId']]
        if pipeline.hash_text(version['text']) != decision['textSha256']:
            raise ValueError('Recorded image decision is bound to different text')
        version['verification'] = deepcopy(decision['verification'])
        if decision.get('imageReview'):
            version['imageReview'] = deepcopy(decision['imageReview'])
    spec['generatedOn'] = plan['generatedOn']
    spec['articles'].sort(key=lambda a:number_key(a['canonicalNumber']))
    pipeline.compose(spec, ledger, pipeline.CACHE, pipeline.ROOT/'data/law/corpus/civil-code')
    pipeline.write_json(research/'priority-specs/civil-code.json', spec)
    pipeline.write_json(research/'civil-2005-conflict-consolidation-audit.json', audit)
    print('Replayed11 explicit amendments/insertions and the separately recorded article7 correction')


if __name__ == '__main__':
    main()
