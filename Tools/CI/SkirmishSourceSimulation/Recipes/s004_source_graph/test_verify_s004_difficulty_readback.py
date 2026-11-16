import copy
import unittest

import verify_s004_difficulty_readback as verifier


class DifficultyReadbackVerifierTests(unittest.TestCase):
    def setUp(self):
        setup = {'DifficultyId': 1, 'SetupHash': 106654757, 'MeasuredLayoutBound': True, 'MaterialsEach': 900}
        self.result = {'rows':[{'size':'Standard','difficulty':'Recruit','compiled':True,
                                'compiler_exception':None,'seed':104733,'setup_hash':106654757,'compiled_setup':setup}]}
        self.value = {'kind':'S004StandardDifficultySourceCompilerReadback','status':'Collected','difficulty':'Recruit',
                      'source_compiler_receipt':{'path':str(verifier.COMPILER / 'receipt.json'),'sha256':'a'},
                      'source_compiler_result':{'path':str(verifier.COMPILER / 'result.json'),'sha256':'b'},
                      'compiled_setup':copy.deepcopy(setup),'world_initialized':False,'physical_source_fit_verified':False,
                      'runtime_admission':False,'mission_readiness':False}

    def check(self, value): verifier.validate_content(value, 'Recruit', 'a', 'b', self.result)

    def test_exact_compiler_row_is_accepted(self): self.check(self.value)

    def test_modified_data_or_scope_is_rejected(self):
        for name in ('difficulty','compiled_setup','runtime_admission','source_compiler_result'):
            value = copy.deepcopy(self.value)
            if name == 'difficulty': value[name] = 'Regular'
            elif name == 'compiled_setup': value[name]['MaterialsEach'] += 1
            elif name == 'runtime_admission': value[name] = True
            else: value[name]['sha256'] = 'changed'
            with self.subTest(name=name), self.assertRaises(ValueError): self.check(value)

    def test_duplicate_json_keys_rejected(self):
        with self.assertRaises(ValueError): verifier.unique([('a', 1), ('a', 2)])
