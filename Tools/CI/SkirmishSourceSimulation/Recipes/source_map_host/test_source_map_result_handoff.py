import importlib.util
import hashlib
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).with_name('generate.py')
spec = importlib.util.spec_from_file_location('source_map_host_generate_for_result_handoff', SCRIPT)
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)
RECEIPT_GENERATOR = SCRIPT.with_name('generate_source_program_receipt.py')
OWNER_SOURCE = SCRIPT.parents[2] / 'Adapters/S026/294-312_PaidSourceOwnerStep.retry.cs'
receipt_spec = importlib.util.spec_from_file_location('source_map_program_receipt_generator_for_test', RECEIPT_GENERATOR)
receipt_generator = importlib.util.module_from_spec(receipt_spec)
receipt_spec.loader.exec_module(receipt_generator)


class SourceMapResultHandoffTests(unittest.TestCase):
    def test_handoff_reports_only_actual_installed_owner_receipt_and_setup(self):
        anchor = 'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);'
        generated = generator.once('prefix;' + anchor + ';suffix', anchor, generator.live_owner_result_handoff())

        self.assertIn('var installedSourceMap=world.SourceDerivedMapReceipt;', generated)
        self.assertIn('["prepared_runtime_present"]=preparedSourceMap!=null', generated)
        self.assertIn('["world_install_receipt_present"]=installedSourceMap!=null', generated)
        self.assertIn('["installed_water_sha256"]=installedSourceMap==null?null:', generated)
        self.assertIn('if(installedSourceMap!=null)result["source_derived_map"]=installedSourceMap;', generated)
        self.assertIn('["setup_hash"]=world.Setup.SetupHash', generated)
        self.assertIn('["resource_preset_input"]=admitted["resource_preset"]?.DeepClone()', generated)
        self.assertIn('["source_owner_supply_day_seconds"]=world.SourceOwnerResourceSecondsPerDay', generated)
        self.assertIn('["source_owner_cadence_initialized"]=true', generated)
        self.assertIn('["initial_cadence_completions"]=startupReadback["initial_cadence_completions"]?.DeepClone()', generated)
        self.assertIn('["preset_cadence_value_readback"]=true', generated)
        self.assertEqual(generated.count('result["source_delivery_events"]='), 1)
        self.assertNotIn('result["source_derived_map"]=run[', generated)

    def test_handoff_refuses_ambiguous_result_anchor(self):
        with self.assertRaisesRegex(ValueError, 'Expected one exact host anchor'):
            generator.once('result["source_derived_map"]=run;',
                           'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);',
                           generator.live_owner_result_handoff())

    def test_manifest_program_generator_reproduces_full_map_handoff_output(self):
        self.assertEqual(generator.PROGRAM_OUTPUT[1],
                         'Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_source_program_receipt.py')
        with tempfile.TemporaryDirectory() as root:
            receipt_out = Path(root) / 'receipt'
            receipt_out.mkdir()
            receipt = receipt_generator.generate(Path.cwd(), receipt_out)
            emitted = (receipt_out / 'SourceSimulationProgram.cs').read_bytes()
            self.assertEqual(receipt['outputs']['SourceSimulationProgram.cs'],
                             hashlib.sha256(emitted).hexdigest())

            map_out = Path(root) / 'map-host'
            map_out.mkdir()
            generator.generate_program_output(Path.cwd(), map_out, RECEIPT_GENERATOR)
            target = map_out / 'SourceSimulationProgram.cs'
            text = target.read_text()
            text = generator.once(text, '  if(projected!=null){if(!deferredClearance',
                                  '  if(preparedSourceMap!=null)clearance=preparedSourceMap.Clearance;\n  else if(projected!=null){if(!deferredClearance')
            text = generator.once(text,
                                  'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);',
                                  generator.live_owner_result_handoff())
            self.assertEqual(text.encode(), emitted)

    def test_cadence_readback_uses_initialized_original_owner_field(self):
        owner = OWNER_SOURCE.read_text()
        self.assertIn('out resourceSecondsPerDay', owner)
        self.assertIn('SourceOwnerResourceSecondsPerDay=>ownersInitialized?resourceSecondsPerDay:throw', owner)
        self.assertIn('["source_owner_supply_day_seconds"]=world.SourceOwnerResourceSecondsPerDay',
                      generator.live_owner_result_handoff())


if __name__ == '__main__':
    unittest.main()
